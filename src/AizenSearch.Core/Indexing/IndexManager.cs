using System.Collections.Concurrent;
using System.Diagnostics;
using AizenSearch.Core.Models;
using AizenSearch.Core.Search;
using AizenSearch.Core.Storage;

namespace AizenSearch.Core.Indexing;

public sealed class IndexManager
{
    private readonly object _mutationLock = new();
    private List<FileEntry> _entries = [];
    private volatile FileEntry[] _snapshot = [];

    public event Action? OnIndexChanged;

    private readonly NtfsMftIndexer _mftIndexer = new();
    private readonly ParallelDirectoryIndexer _dirIndexer = new();
    private readonly HashSet<string> _fullyIndexedDrives = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _expandedDirectories = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _autoSaveCts;
    private CancellationTokenSource? _currentRebuildCts; // Feature #10: cancel support
    private readonly object _saveLock = new();

    public int TotalCount => _snapshot.Length;

    private string? _customCachePath;

    public void Initialize(string? customPath = null)
    {
        _customCachePath = customPath;
        var cached = BinaryCacheService.LoadCache(_customCachePath);
        if (cached.Count > 0)
        {
            lock (_mutationLock)
            {
                _entries = cached;
                _snapshot = _entries.ToArray();
            }

            var available = GetAvailableDrives();
            var indexed = GetIndexedDriveRoots();
            foreach (var d in available)
            {
                if (indexed.Contains(d))
                {
                    _fullyIndexedDrives.Add(d);
                }
            }

            // Compact managed heap and trim working set after loading multi-million entry cache
            Services.MemoryOptimizer.TrimWorkingSet();
        }

        // Asynchronously seed connected library metadata if the configured location is mounted
        // but contains no indexed entries yet (fresh install / empty cache).
        _ = Task.Run(async () =>
        {
            try
            {
                var snapshot = _snapshot;
                var hasAnyPriorityEntries = false;
                for (var i = 0; i < snapshot.Length; i++)
                {
                    if (snapshot[i].Dir.StartsWith(ConnectedLibrarySeeder.ConnectedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        hasAnyPriorityEntries = true;
                        break;
                    }
                }

                if (!hasAnyPriorityEntries)
                {
                    var seeded = await ConnectedLibrarySeeder.CrawlConnectedLibraryIndexAsync();
                    if (seeded.Count > 0)
                    {
                        lock (_mutationLock)
                        {
                            _entries.AddRange(seeded);
                            _entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                            _snapshot = _entries.ToArray();
                        }
                        ScheduleAutoSave();
                        OnIndexChanged?.Invoke();
                    }
                }
            }
            catch { }
        });
    }

    public static List<string> GetAvailableDrives()
    {
        try
        {
            Services.DosDeviceHelper.EnsureDriveMapped("R:");
        }
        catch { }

        var list = new List<string>();
        for (char c = 'A'; c <= 'Z'; c++)
        {
            var root = $"{c}:\\";
            try
            {
                if (Directory.Exists(root))
                {
                    list.Add(root);
                }
            }
            catch { }
        }
        return list;
    }

    public bool IsDriveFullyIndexed(string driveRoot)
    {
        return _fullyIndexedDrives.Contains(driveRoot);
    }

    public HashSet<string> GetIndexedDriveRoots()
    {
        var snapshot = _snapshot;
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < snapshot.Length; i++)
        {
            var d = snapshot[i].Dir;
            if (d.Length >= 3 && d[1] == ':' && d[2] == '\\')
            {
                set.Add(d.Substring(0, 3).ToUpperInvariant());
            }
        }
        return set;
    }

    public List<string> GetMissingDrives()
    {
        var available = GetAvailableDrives();
        var indexed = GetIndexedDriveRoots();
        return available.Where(d => !indexed.Contains(d) || !_fullyIndexedDrives.Contains(d)).ToList();
    }

    public void AddOrUpdateEntry(FileEntry entry)
    {
        lock (_mutationLock)
        {
            var idx = _entries.FindIndex(e => e.Path.Equals(entry.Path, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                _entries[idx] = entry;
            }
            else
            {
                _entries.Add(entry);
            }
            _snapshot = _entries.ToArray();
        }

        OnIndexChanged?.Invoke();
        ScheduleAutoSave();
    }

    public void RemoveEntry(string fullPath)
    {
        var prefix = fullPath.TrimEnd('\\') + "\\";
        var removed = 0;

        lock (_mutationLock)
        {
            removed = _entries.RemoveAll(e =>
                e.Path.Equals(fullPath, StringComparison.OrdinalIgnoreCase) ||
                e.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (removed > 0)
            {
                _snapshot = _entries.ToArray();
            }
        }

        if (removed > 0)
        {
            OnIndexChanged?.Invoke();
            ScheduleAutoSave();
        }
    }

    public void RenameEntry(string oldPath, string newPath)
    {
        var oldPrefix = oldPath.TrimEnd('\\') + "\\";
        var isFolder = Directory.Exists(newPath);
        var changed = false;

        lock (_mutationLock)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                var item = _entries[i];
                if (item.Path.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
                {
                    _entries[i] = FileEntry.Create(newPath, isFolder);
                    changed = true;
                }
                else if (item.Path.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var rel = item.Path.Substring(oldPrefix.Length);
                    var childNewPath = Path.Combine(newPath, rel);
                    _entries[i] = FileEntry.Create(childNewPath, item.IsFolder, item.Name, item.Ext);
                    changed = true;
                }
            }

            if (changed)
            {
                _snapshot = _entries.ToArray();
            }
        }

        if (changed)
        {
            OnIndexChanged?.Invoke();
            ScheduleAutoSave();
        }
    }

    private void ScheduleAutoSave()
    {
        lock (_saveLock)
        {
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = new CancellationTokenSource();
            var token = _autoSaveCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(3000, token);
                    if (token.IsCancellationRequested) return;

                    var snapshot = _snapshot;
                    BinaryCacheService.SaveCache(snapshot, _customCachePath);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AutoSave error: {ex.Message}");
                }
            }, token);
        }
    }

    /// <summary>Finds groups of files that share the same name + size (probable duplicates). Feature #14.</summary>
    public SearchResult FindDuplicates()
    {
        var snapshot = _snapshot;
        var groups = new Dictionary<(string Name, long Size), List<FileEntry>>();
        foreach (var e in snapshot)
        {
            if (e.IsFolder) continue;
            var key = (e.Name, e.SizeBytes);
            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }
            list.Add(e);
        }

        var items = new List<FileEntry>();
        foreach (var kv in groups)
        {
            if (kv.Value.Count > 1)
            {
                items.AddRange(kv.Value);
            }
        }
        items.Sort((a, b) =>
        {
            var c = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return c != 0 ? c : a.SizeBytes.CompareTo(b.SizeBytes);
        });

        return new SearchResult { Items = items, TotalMatches = items.Count };
    }

    /// <summary>Cancels any in-progress rebuild (Feature #10).</summary>
    public void CancelCurrentRebuild()
    {
        try
        {
            _currentRebuildCts?.Cancel();
        }
        catch { }
    }

    public async Task StartRebuildAsync(
        List<string>? requestedDrives = null,
        Action<IndexProgress>? onProgress = null,
        Action<object>? onComplete = null,
        CancellationToken ct = default)
    {
        // Cancel any previous rebuild and own a fresh token for this one
        try { _currentRebuildCts?.Cancel(); } catch { }
        _currentRebuildCts?.Dispose();
        _currentRebuildCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        ct = _currentRebuildCts.Token;

        var roots = (requestedDrives != null && requestedDrives.Count > 0)
            ? requestedDrives
            : GetAvailableDrives();

        roots = roots.OrderBy(d => d.StartsWith("R:", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                     .ThenBy(d => d)
                     .ToList();

        var totalDrives = roots.Count;
        var sw = Stopwatch.StartNew();
        var engines = new List<string>();
        var completed = 0;

        foreach (var root in roots)
        {
            ct.ThrowIfCancellationRequested();

            bool isNtfs = false;
            try
            {
                var driveInfo = new DriveInfo(root);
                isNtfs = driveInfo.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                isNtfs = false;
            }

            onProgress?.Invoke(new IndexProgress
            {
                Drive = root,
                Stage = isNtfs ? "Reading NTFS metadata" : "Scanning directory",
                Completed = completed,
                TotalDrives = totalDrives,
                Count = TotalCount,
                Seconds = (float)sw.Elapsed.TotalSeconds
            });

            var lowerRoot = root.ToLowerInvariant();
            List<FileEntry> driveEntries;
            string engineUsed;

            var lastSaveSw = Stopwatch.StartNew();
            var lastSnapshotSw = Stopwatch.StartNew();

            Action<List<FileEntry>> batchAppender = batch =>
            {
                if (batch.Count == 0) return;
                lock (_mutationLock)
                {
                    _entries.AddRange(batch);
                    // Debounce snapshot publishing to every 250ms during heavy bulk crawl
                    if (lastSnapshotSw.ElapsedMilliseconds >= 250)
                    {
                        _snapshot = _entries.ToArray();
                        lastSnapshotSw.Restart();
                    }
                }

                onProgress?.Invoke(new IndexProgress
                {
                    Drive = root,
                    Stage = $"Scanning ({TotalCount:N0} objects)",
                    Completed = completed,
                    TotalDrives = totalDrives,
                    Count = TotalCount,
                    Seconds = (float)sw.Elapsed.TotalSeconds
                });

                if (lastSaveSw.ElapsedMilliseconds >= 5000)
                {
                    lastSaveSw.Restart();
                    ScheduleAutoSave();
                }
            };

            if (isNtfs)
            {
                try
                {
                    driveEntries = await _mftIndexer.IndexVolumeAsync(root, null, ct);
                    engineUsed = "MFT";

                    lock (_mutationLock)
                    {
                        _entries.RemoveAll(e => e.Path.StartsWith(lowerRoot, StringComparison.OrdinalIgnoreCase));
                        _entries.AddRange(driveEntries);
                        _entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                        _snapshot = _entries.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    onProgress?.Invoke(new IndexProgress
                    {
                        Drive = root,
                        Stage = "MFT unavailable - deep scanning",
                        Detail = ex.Message,
                        Completed = completed,
                        TotalDrives = totalDrives,
                        Count = TotalCount,
                        Seconds = (float)sw.Elapsed.TotalSeconds
                    });

                    lock (_mutationLock)
                    {
                        _entries.RemoveAll(e => e.Path.StartsWith(lowerRoot, StringComparison.OrdinalIgnoreCase));
                        _snapshot = _entries.ToArray();
                    }

                    driveEntries = await _dirIndexer.IndexVolumeAsync(root, batchAppender, null, ct);
                    engineUsed = "Deep scan";

                    lock (_mutationLock)
                    {
                        _entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                        _snapshot = _entries.ToArray();
                    }
                }
            }
            else
            {
                // Virtual or network-mounted drive
                var existingSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var snapshot = _snapshot;
                for (var i = 0; i < snapshot.Length; i++)
                {
                    var p = snapshot[i].Path;
                    if (p.StartsWith(lowerRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        existingSet.Add(p);
                    }
                }

                Action<List<FileEntry>> safeBatchAppender = batch =>
                {
                    var newOnly = new List<FileEntry>(batch.Count);
                    lock (existingSet)
                    {
                        foreach (var item in batch)
                        {
                            if (existingSet.Add(item.Path))
                            {
                                newOnly.Add(item);
                            }
                        }
                    }
                    if (newOnly.Count > 0)
                    {
                        batchAppender(newOnly);
                    }
                };

                driveEntries = await _dirIndexer.IndexVolumeAsync(root, safeBatchAppender, null, ct);
                engineUsed = "Directory scan";

                lock (_mutationLock)
                {
                    _entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                    _snapshot = _entries.ToArray();
                }
            }

            _fullyIndexedDrives.Add(root);
            completed++;
            engines.Add($"{root} {engineUsed}");

            ScheduleAutoSave();
            OnIndexChanged?.Invoke();

            onProgress?.Invoke(new IndexProgress
            {
                Drive = root,
                Stage = "Complete - searchable now",
                Engine = engineUsed,
                Completed = completed,
                TotalDrives = totalDrives,
                Count = TotalCount,
                Seconds = (float)sw.Elapsed.TotalSeconds
            });
        }

        sw.Stop();
        ScheduleAutoSave();

        onComplete?.Invoke(new
        {
            count = TotalCount,
            seconds = sw.Elapsed.TotalSeconds,
            drives = roots,
            engines,
            fresh = true
        });
    }

    public void ExpandDirectoryIfMatched(string folderPath)
    {
        if (!_expandedDirectories.TryAdd(folderPath, 0)) return;

        try
        {
            var dirInfo = new DirectoryInfo(folderPath);
            if (!dirInfo.Exists) return;

            var newEntries = new List<FileEntry>();
            var queue = new Queue<(DirectoryInfo dir, int depth)>();
            queue.Enqueue((dirInfo, 0));

            while (queue.Count > 0)
            {
                var (cur, depth) = queue.Dequeue();
                if (depth > 1) continue;

                try
                {
                    foreach (var f in cur.EnumerateFiles("*"))
                    {
                        var internedDir = DirectoryPool.InternDir(f.DirectoryName ?? string.Empty);
                        newEntries.Add(new FileEntry
                        {
                            Name = f.Name,
                            Dir = internedDir,
                            Ext = DirectoryPool.InternExt(f.Extension),
                            IsFolder = false,
                            SizeBytes = f.Length,
                            ModifiedUtc = f.LastWriteTimeUtc
                        });
                    }

                    foreach (var d in cur.EnumerateDirectories("*"))
                    {
                        var internedDir = DirectoryPool.InternDir(d.Parent?.FullName ?? string.Empty);
                        newEntries.Add(new FileEntry
                        {
                            Name = d.Name,
                            Dir = internedDir,
                            Ext = string.Empty,
                            IsFolder = true
                        });
                        queue.Enqueue((d, depth + 1));
                    }
                }
                catch { }
            }

            if (newEntries.Count > 0)
            {
                var addedCount = 0;
                lock (_mutationLock)
                {
                    var existingPaths = new HashSet<string>(_entries.Select(e => e.Path), StringComparer.OrdinalIgnoreCase);
                    foreach (var ne in newEntries)
                    {
                        if (existingPaths.Add(ne.Path))
                        {
                            _entries.Add(ne);
                            addedCount++;
                        }
                    }

                    if (addedCount > 0)
                    {
                        _snapshot = _entries.ToArray();
                    }
                }

                if (addedCount > 0)
                {
                    OnIndexChanged?.Invoke();
                    ScheduleAutoSave();
                }
            }
        }
        catch { }
    }

    public SearchResult Search(SearchQuery query)
    {
        var parsed = QueryParser.Parse(query);
        var snapshot = _snapshot;

        // 1. Instant in-memory search with zero locks
        var results = SearchEngine.Search(snapshot, query, parsed);

        // Feature #5: favorites filter — when FavoritePaths is set, return only those entries
        if (query.FavoritePaths is { Count: > 0 })
        {
            var favSet = new HashSet<string>(query.FavoritePaths, StringComparer.OrdinalIgnoreCase);
            var favItems = new List<FileEntry>(favSet.Count);
            foreach (var e in snapshot)
            {
                if (favSet.Contains(e.Path))
                {
                    favItems.Add(e);
                }
            }
            favItems.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            results = new SearchResult
            {
                Items = favItems,
                TotalMatches = favItems.Count
            };
        }

        // 2. Only if no results matched and terms exist, check if a candidate folder on R: needs shallow expansion
        if (results.TotalMatches == 0 && parsed.Terms.Count > 0)
        {
            var expandedAny = false;
            for (var i = 0; i < snapshot.Length; i++)
            {
                var e = snapshot[i];
                if (e.IsFolder && e.Path.StartsWith("R:", StringComparison.OrdinalIgnoreCase))
                {
                    var matches = e.Name.Contains(parsed.Terms[0], StringComparison.OrdinalIgnoreCase);
                    if (matches && parsed.Terms.Count > 1)
                    {
                        matches = e.Name.Contains(parsed.Terms[1], StringComparison.OrdinalIgnoreCase);
                    }

                    if (matches)
                    {
                        ExpandDirectoryIfMatched(e.Path);
                        expandedAny = true;
                        break;
                    }
                }
            }

            if (expandedAny)
            {
                results = SearchEngine.Search(_snapshot, query, parsed);
            }
        }

        return results;
    }
}
