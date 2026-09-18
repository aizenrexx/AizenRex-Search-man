using AizenSearch.Core.Models;
using AizenSearch.Core.Storage;
using Xunit;

namespace AizenSearch.Core.Tests;

public class BinaryCacheTests
{
    [Fact]
    public void CacheSaveAndLoadRoundtripWorks()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"aizen_cache_test_{Guid.NewGuid():N}.bin");
        try
        {
            var sample = new List<FileEntry>
            {
                new() { Name = "test.txt", Path = @"C:\test.txt", Ext = "txt", IsFolder = false },
                new() { Name = "Projects", Path = @"H:\Projects", Ext = "", IsFolder = true }
            };

            BinaryCacheService.SaveCache(sample, tempPath);
            var loaded = BinaryCacheService.LoadCache(tempPath);

            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, e => e.Name == "test.txt" && e.Path == @"C:\test.txt");
            Assert.Contains(loaded, e => e.Name == "Projects" && e.IsFolder);
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }

    [Fact]
    public void V2FormatSharesInternedDirectoryStrings()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"aizen_cache_v2_{Guid.NewGuid():N}.bin");
        try
        {
            var sample = new List<FileEntry>
            {
                FileEntry.Create(@"C:\Windows\System32\cmd.exe", false),
                FileEntry.Create(@"C:\Windows\System32\notepad.exe", false),
                FileEntry.Create(@"C:\Windows\System32\calc.exe", false),
            };

            BinaryCacheService.SaveCache(sample, tempPath);
            var loaded = BinaryCacheService.LoadCache(tempPath);

            Assert.Equal(3, loaded.Count);
            Assert.Same(loaded[0].Dir, loaded[1].Dir);
            Assert.Same(loaded[1].Dir, loaded[2].Dir);
            Assert.Equal(@"C:\Windows\System32", loaded[0].Dir);
        }
        finally
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }
    }

    [Fact]
    public async Task MigrateLiveCacheToV2WithConnectedLibrary()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var cachePath = Path.Combine(localAppData, "AizenSearch", "index.bin");
        if (!File.Exists(cachePath)) return;

        var entries = BinaryCacheService.LoadCache(cachePath);
        if (entries.Count == 0) return;

        // Ensure connected library entries are added
        var seeded = await Indexing.ConnectedLibrarySeeder.CrawlConnectedLibraryIndexAsync();
        var existingPaths = new HashSet<string>(entries.Select(e => e.Path), StringComparer.OrdinalIgnoreCase);
        foreach (var s in seeded)
        {
            if (existingPaths.Add(s.Path))
            {
                entries.Add(s);
            }
        }

        // Save back in compact V2 format
        BinaryCacheService.SaveCache(entries, cachePath);

        // Verify loaded again
        var reloaded = BinaryCacheService.LoadCache(cachePath);
        Assert.True(reloaded.Count >= entries.Count);
        Assert.Equal(entries.Count, reloaded.Count);
    }
}
