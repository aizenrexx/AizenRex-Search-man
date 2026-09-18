using System.Diagnostics;
using System.IO;
using AizenSearch.Core.Indexing;
using AizenSearch.Core.Models;

namespace AizenSearch.Core.Services;

public sealed class RealtimeIndexWatcher : IDisposable
{
    private readonly IndexManager _indexManager;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    private static readonly string[] IgnoredDirectories =
    [
        "$recycle.bin",
        "system volume information",
        @"\appdata\local\temp\",
        @"\.git\",
        @"\node_modules\",
        @"\obj\",
        @"\bin\"
    ];

    public RealtimeIndexWatcher(IndexManager indexManager)
    {
        _indexManager = indexManager;
    }

    public void Start()
    {
        var drives = IndexManager.GetAvailableDrives();
        foreach (var drive in drives)
        {
            SetupLocalWatcher(drive);
        }

        // Start background incremental crawler for virtual/network mounts
        StartVirtualDriveSyncer(_cts.Token);
    }

    private void SetupLocalWatcher(string driveRoot)
    {
        try
        {
            var watcher = new FileSystemWatcher(driveRoot)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                InternalBufferSize = 64 * 1024 // 64KB buffer for high event throughput
            };

            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Changed += OnChanged;
            watcher.Error += (s, e) => Debug.WriteLine($"Watcher error on {driveRoot}: {e.GetException()?.Message}");

            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start watcher on {driveRoot}: {ex.Message}");
        }
    }

    private static bool ShouldIgnore(string path)
    {
        var lower = path.ToLowerInvariant();
        foreach (var ign in IgnoredDirectories)
        {
            if (lower.Contains(ign)) return true;
        }
        return false;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnore(e.FullPath)) return;

        try
        {
            var isFolder = Directory.Exists(e.FullPath);
            var isFile = File.Exists(e.FullPath);
            if (!isFolder && !isFile) return;

            var entry = new FileEntry
            {
                Name = Path.GetFileName(e.FullPath),
                Path = e.FullPath,
                Ext = isFolder ? string.Empty : Path.GetExtension(e.FullPath).TrimStart('.').ToLowerInvariant(),
                IsFolder = isFolder,
                SizeBytes = isFile ? new FileInfo(e.FullPath).Length : 0,
                ModifiedUtc = isFile ? new FileInfo(e.FullPath).LastWriteTimeUtc : (Directory.Exists(e.FullPath) ? new DirectoryInfo(e.FullPath).LastWriteTimeUtc : default)
            };
            _indexManager.AddOrUpdateEntry(entry);
        }
        catch { }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnore(e.FullPath)) return;

        try
        {
            var isFolder = Directory.Exists(e.FullPath);
            var isFile = File.Exists(e.FullPath);
            if (!isFolder && !isFile)
            {
                _indexManager.RemoveEntry(e.FullPath);
                return;
            }

            var entry = new FileEntry
            {
                Name = Path.GetFileName(e.FullPath),
                Path = e.FullPath,
                Ext = isFolder ? string.Empty : Path.GetExtension(e.FullPath).TrimStart('.').ToLowerInvariant(),
                IsFolder = isFolder,
                SizeBytes = isFile ? new FileInfo(e.FullPath).Length : 0,
                ModifiedUtc = isFile ? new FileInfo(e.FullPath).LastWriteTimeUtc : (Directory.Exists(e.FullPath) ? new DirectoryInfo(e.FullPath).LastWriteTimeUtc : default)
            };
            _indexManager.AddOrUpdateEntry(entry);
        }
        catch { }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnore(e.FullPath)) return;

        try
        {
            _indexManager.RemoveEntry(e.FullPath);
        }
        catch { }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldIgnore(e.OldFullPath) || ShouldIgnore(e.FullPath)) return;

        try
        {
            _indexManager.RenameEntry(e.OldFullPath, e.FullPath);
        }
        catch { }
    }

    private void StartVirtualDriveSyncer(CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            // Initial delay to let the app start cleanly
            await Task.Delay(3000, ct);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Check if R:\ is available and needs scanning
                    var drives = IndexManager.GetAvailableDrives();
                    var rDrive = drives.FirstOrDefault(d => d.StartsWith("R:", StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(rDrive) && !_indexManager.IsDriveFullyIndexed(rDrive))
                    {
                        await _indexManager.StartRebuildAsync([rDrive], ct: ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"VirtualDriveSyncer error: {ex.Message}");
                }

                // Periodic check every 2 minutes
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(2), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, ct);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _cts.Dispose();

        foreach (var w in _watchers)
        {
            try
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            catch { }
        }
        _watchers.Clear();
    }
}
