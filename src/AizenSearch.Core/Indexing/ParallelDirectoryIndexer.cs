using System.Collections.Concurrent;
using System.Threading.Channels;
using AizenSearch.Core.Models;
using AizenSearch.Core.Storage;

namespace AizenSearch.Core.Indexing;

public sealed class ParallelDirectoryIndexer : IIndexEngine
{
    public string Name => "Deep scan";

    public async Task<List<FileEntry>> IndexVolumeAsync(
        string driveRoot,
        Action<List<FileEntry>>? onBatch = null,
        IProgress<IndexProgress>? progress = null,
        CancellationToken ct = default)
    {
        var rootDir = new DirectoryInfo(driveRoot);
        if (!rootDir.Exists)
        {
            return [];
        }

        var isConnectedLocation = driveRoot.StartsWith("R:", StringComparison.OrdinalIgnoreCase);
        // Keep local indexing responsive on low-memory PCs; connected locations are supported,
        // but they must not consume a large worker fan-out or overwhelm the location.
        var workerCount = isConnectedLocation ? 4 : Math.Clamp(Environment.ProcessorCount, 2, 8);
        var maxDepth = isConnectedLocation ? 25 : 40;

        var entries = new ConcurrentBag<FileEntry>();
        var visited = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var batchLock = new object();

        var enumOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            AttributesToSkip = FileAttributes.System
        };

        // Channel for work-stealing directory distribution
        var channel = Channel.CreateUnbounded<(DirectoryInfo dir, int depth)>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false
        });

        var pendingWork = 0;
        var completedLock = new object();

        void EnqueueDir(DirectoryInfo d, int depth)
        {
            var key = d.FullName.TrimEnd('\\');
            if (visited.TryAdd(key, 0))
            {
                Interlocked.Increment(ref pendingWork);
                channel.Writer.TryWrite((d, depth));
            }
        }

        // Atomically decrement pending work and complete the channel only when
        // the count reaches zero. This prevents a race where one worker completes
        // the channel while another is still enqueuing subdirectories.
        void CompleteWorkItem()
        {
            if (Interlocked.Decrement(ref pendingWork) == 0)
            {
                lock (completedLock)
                {
                    if (pendingWork == 0)
                    {
                        channel.Writer.TryComplete();
                    }
                }
            }
        }

        // Process top-level files of the drive root
        try
        {
            var internedRootDir = DirectoryPool.InternDir(rootDir.FullName);
            var topFiles = rootDir.GetFiles("*", enumOptions);
            var initialBatch = new List<FileEntry>(topFiles.Length);
            foreach (var f in topFiles)
            {
                var entry = new FileEntry
                {
                    Name = f.Name,
                    Dir = internedRootDir,
                    Ext = DirectoryPool.InternExt(f.Extension),
                    IsFolder = false,
                    SizeBytes = f.Length,
                    ModifiedUtc = f.LastWriteTimeUtc
                };
                entries.Add(entry);
                initialBatch.Add(entry);
            }

            if (initialBatch.Count > 0 && onBatch != null)
            {
                lock (batchLock)
                {
                    onBatch(initialBatch);
                }
            }

            // Seed root subdirectories into the work channel
            var topDirs = rootDir.GetDirectories("*", enumOptions);
            foreach (var td in topDirs)
            {
                var dirEntry = new FileEntry
                {
                    Name = td.Name,
                    Dir = internedRootDir,
                    Ext = string.Empty,
                    IsFolder = true
                };
                entries.Add(dirEntry);

                EnqueueDir(td, 1);
            }
        }
        catch
        {
            // Root enumeration fallback
        }

        if (pendingWork == 0)
        {
            channel.Writer.TryComplete();
            return entries.ToList();
        }

        // Launch concurrent worker tasks
        var workers = new Task[workerCount];
        for (var i = 0; i < workerCount; i++)
        {
            workers[i] = Task.Run(async () =>
            {
                var localBatch = new List<FileEntry>(128);

                void FlushBatch()
                {
                    if (localBatch.Count == 0 || onBatch == null) return;
                    var copy = new List<FileEntry>(localBatch);
                    localBatch.Clear();
                    lock (batchLock)
                    {
                        onBatch(copy);
                    }
                }

                try
                {
                    while (await channel.Reader.WaitToReadAsync(ct))
                    {
                        while (channel.Reader.TryRead(out var item))
                        {
                            var (dir, depth) = item;
                            try
                            {
                                var internedDir = DirectoryPool.InternDir(dir.FullName);

                                // 1. Files
                                foreach (var file in dir.EnumerateFiles("*", enumOptions))
                                {
                                    var fe = new FileEntry
                                    {
                                        Name = file.Name,
                                        Dir = internedDir,
                                        Ext = DirectoryPool.InternExt(file.Extension),
                                        IsFolder = false,
                                        SizeBytes = file.Length,
                                        ModifiedUtc = file.LastWriteTimeUtc
                                    };
                                    entries.Add(fe);
                                    localBatch.Add(fe);

                                    if (localBatch.Count >= 100)
                                    {
                                        FlushBatch();
                                    }
                                }

                                // 2. Subdirectories
                                if (depth < maxDepth)
                                {
                                    foreach (var sub in dir.EnumerateDirectories("*", enumOptions))
                                    {
                                        var subEntry = new FileEntry
                                        {
                                            Name = sub.Name,
                                            Dir = internedDir,
                                            Ext = string.Empty,
                                            IsFolder = true
                                        };
                                        entries.Add(subEntry);
                                        localBatch.Add(subEntry);

                                        if (localBatch.Count >= 100)
                                        {
                                            FlushBatch();
                                        }

                                        EnqueueDir(sub, depth + 1);
                                    }
                                }
                            }
                            catch
                            {
                                // Catch inaccessible folders or temporary network issues
                            }
                            finally
                            {
                                CompleteWorkItem();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancelled gracefully
                }
                finally
                {
                    FlushBatch();
                }
            }, ct);
        }

        try
        {
            await Task.WhenAll(workers);
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        return entries.ToList();
    }

    public Task<List<FileEntry>> IndexVolumeAsync(string driveRoot, IProgress<IndexProgress>? progress = null, CancellationToken ct = default)
        => IndexVolumeAsync(driveRoot, null, progress, ct);
}
