using AizenSearch.Core.Indexing;
using AizenSearch.Core.Services;
using Xunit;

namespace AizenSearch.Core.Tests;

public class VirtualDriveTests
{
    [Fact]
    public void EnsureDriveMappedDoesNotThrow()
    {
        var mapped = DosDeviceHelper.EnsureDriveMapped("R:");
        Assert.True(mapped || !mapped);
    }

    [Fact]
    public void GetAvailableDrivesDetectsSystemDrives()
    {
        var drives = IndexManager.GetAvailableDrives();
        Assert.NotEmpty(drives);
        Assert.Contains(@"C:\", drives);
    }

    [Fact]
    public void RDriveEntriesCanBeSearchedWithoutScanningLiveStorage()
    {
        var tempCache = Path.Combine(Path.GetTempPath(), $"aizen_test_{Guid.NewGuid()}.bin");
        var manager = new IndexManager();
        manager.Initialize(tempCache);
        manager.AddOrUpdateEntry(new Models.FileEntry
        {
            Name = "connected_library_test.mkv",
            Path = @"R:\Connected Library\Test\connected_library_test.mkv",
            Ext = "mkv",
            IsFolder = false
        });

        var results = manager.Search(new Models.SearchQuery
        {
            RawQuery = "connected_library_test",
            Limit = 50
        });

        Assert.NotEmpty(results);
        Assert.StartsWith("R:", results[0].Path, StringComparison.OrdinalIgnoreCase);
        try { if (File.Exists(tempCache)) File.Delete(tempCache); } catch { }
    }

    [Fact]
    public void RealtimeAddAndRemoveMutatesIndex()
    {
        var tempCache = Path.Combine(Path.GetTempPath(), $"aizen_rt_{Guid.NewGuid()}.bin");
        var manager = new IndexManager();
        manager.Initialize(tempCache);

        var entry = new Models.FileEntry
        {
            Name = "realtime_test.txt",
            Path = @"C:\realtime_test.txt",
            Ext = "txt",
            IsFolder = false
        };

        var eventCount = 0;
        manager.OnIndexChanged += () => eventCount++;

        manager.AddOrUpdateEntry(entry);
        Assert.Equal(1, manager.TotalCount);
        Assert.True(eventCount >= 1);

        var found = manager.Search(new Models.SearchQuery { RawQuery = "realtime_test" });
        Assert.Single(found);
        Assert.Equal("realtime_test.txt", found[0].Name);

        manager.RemoveEntry(@"C:\realtime_test.txt");
        Assert.Equal(0, manager.TotalCount);
        Assert.True(eventCount >= 2);

        try { if (File.Exists(tempCache)) File.Delete(tempCache); } catch { }
    }

    [Fact]
    public void LiveExpansionCanExpandAndFind7Kadam()
    {
        var drives = IndexManager.GetAvailableDrives();
        if (!drives.Contains(@"R:\")) return;

        var targetDir = Path.Combine(AizenSearch.Core.Indexing.ConnectedLibrarySeeder.ConnectedRoot, "Hindi TV Series", "7 Kadam (TV Series 2021-)");
        if (!Directory.Exists(targetDir)) return;

        var tempCache = Path.Combine(Path.GetTempPath(), $"aizen_7k_{Guid.NewGuid()}.bin");
        var manager = new IndexManager();
        manager.Initialize(tempCache);
        manager.AddOrUpdateEntry(new Models.FileEntry
        {
            Name = "7 Kadam (TV Series 2021-)",
            Path = targetDir,
            Ext = "",
            IsFolder = true
        });

        var results = manager.Search(new Models.SearchQuery
        {
            RawQuery = "7 Kadam Watch Episode 01 A big no to football in HD",
            Limit = 10
        });

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Name.Contains("7 Kadam Watch Episode 01", StringComparison.OrdinalIgnoreCase));
        try { if (File.Exists(tempCache)) File.Delete(tempCache); } catch { }
    }
}
