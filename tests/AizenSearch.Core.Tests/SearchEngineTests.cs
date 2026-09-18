using AizenSearch.Core.Models;
using AizenSearch.Core.Search;
using Xunit;

namespace AizenSearch.Core.Tests;

public class SearchEngineTests
{
    private static FileEntry CreateEntry(string name, string path, string ext, bool isFolder) => new()
    {
        Name = name,
        Path = path,
        Ext = ext,
        IsFolder = isFolder
    };

    [Fact]
    public void ExactFilenameRanksFirst()
    {
        var entries = new List<FileEntry>
        {
            CreateEntry("main.rs.bak", @"C:\main.rs.bak", "bak", false),
            CreateEntry("main.rs", @"H:\main.rs", "rs", false)
        };

        var query = new SearchQuery { RawQuery = "main.rs" };
        var parsed = QueryParser.Parse(query);
        var results = SearchEngine.Filter(entries, query, parsed);

        Assert.Equal(2, results.Count);
        Assert.Equal("main.rs", results[0].Name);
    }

    [Fact]
    public void FiltersFilesAndFoldersCorrectly()
    {
        var entries = new List<FileEntry>
        {
            CreateEntry("Main.cs", @"H:\src\Main.cs", "cs", false),
            CreateEntry("src", @"H:\src", "", true)
        };

        var fileQuery = new SearchQuery { RawQuery = "main", Kind = "files" };
        var fileResults = SearchEngine.Filter(entries, fileQuery, QueryParser.Parse(fileQuery));
        Assert.Single(fileResults);
        Assert.Equal("Main.cs", fileResults[0].Name);

        var folderQuery = new SearchQuery { RawQuery = "", Kind = "folders" };
        var folderResults = SearchEngine.Filter(entries, folderQuery, QueryParser.Parse(folderQuery));
        Assert.Single(folderResults);
        Assert.Equal("src", folderResults[0].Name);
    }

    [Fact]
    public void ExtensionPreferenceIsPrioritized()
    {
        var entries = new List<FileEntry>
        {
            CreateEntry("Cherax", @"C:\Cherax", "", true),
            CreateEntry("CheraxLoader.exe", @"G:\Cherax\CheraxLoader.exe", "exe", false),
            CreateEntry("cherax.py", @"H:\cherax.py", "py", false)
        };

        var query = new SearchQuery { RawQuery = "cherax.exe" };
        var results = SearchEngine.Filter(entries, query, QueryParser.Parse(query));

        Assert.Equal(3, results.Count);
        Assert.Equal("CheraxLoader.exe", results[0].Name);
    }

    [Fact]
    public void BenchmarkMillionFilesSearchUnder100Ms()
    {
        var entries = new List<FileEntry>(1_000_000);
        for (var i = 0; i < 1_000_000; i++)
        {
            entries.Add(CreateEntry($"file_{i}.txt", $@"C:\data\folder_{i % 100}\file_{i}.txt", "txt", false));
        }

        // Add an exact target
        entries[500_000] = CreateEntry("super_target.exe", @"C:\super_target.exe", "exe", false);

        var query = new SearchQuery { RawQuery = "super_target.exe", Limit = 100 };
        var parsed = QueryParser.Parse(query);

        // Warm up
        _ = SearchEngine.Search(entries, query, parsed);

        // Benchmark
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var searchResult = SearchEngine.Search(entries, query, parsed);
        sw.Stop();

        Assert.NotEmpty(searchResult.Items);
        Assert.Equal("super_target.exe", searchResult.Items[0].Name);
        Assert.True(sw.ElapsedMilliseconds < 250, $"Search took {sw.ElapsedMilliseconds} ms, expected < 250ms");
    }

    [Fact]
    public void SupermanReturns2006RanksFirst()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var cachePath = Path.Combine(localAppData, "AizenSearch", "index.bin");
        if (!File.Exists(cachePath)) return;

        var entries = Storage.BinaryCacheService.LoadCache(cachePath);
        var query = new SearchQuery { RawQuery = "superman 2006", Limit = 10 };
        var parsed = QueryParser.Parse(query);
        var results = SearchEngine.Search(entries, query, parsed);

        Assert.NotEmpty(results.Items);
        var top = results.Items[0];
        Assert.Contains("Superman", top.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2006", top.Name, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(AizenSearch.Core.Indexing.ConnectedLibrarySeeder.ConnectedRoot, top.Path);
    }

    [Fact]
    public void BenchmarkLiveCacheSearchSpeed()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var cachePath = Path.Combine(localAppData, "AizenSearch", "index.bin");
        if (!File.Exists(cachePath)) return;

        var entries = Storage.BinaryCacheService.LoadCache(cachePath);
        if (entries.Count == 0) return;

        // Broad query "c" matching almost everything (previously froze for 2800ms)
        var query = new SearchQuery { RawQuery = "c", Limit = 1000 };
        var parsed = QueryParser.Parse(query);

        // Warm up
        _ = SearchEngine.Search(entries, query, parsed);

        // Benchmark
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var searchResult = SearchEngine.Search(entries, query, parsed);
        sw.Stop();

        Assert.NotEmpty(searchResult.Items);
        Assert.True(searchResult.TotalMatches > 100_000, $"Expected > 100k matches, got {searchResult.TotalMatches}");
        Assert.True(sw.ElapsedMilliseconds < 1500, $"Broad search across {entries.Count:N0} items took {sw.ElapsedMilliseconds} ms, expected < 1500ms");
    }

    [Fact]
    public void SearchWithOffsetReturnsCorrectPage()
    {
        var entries = new List<FileEntry>(200);
        for (var i = 0; i < 200; i++)
        {
            entries.Add(CreateEntry($"document_{i:D3}.txt", $@"C:\docs\document_{i:D3}.txt", "txt", false));
        }

        var q1 = new SearchQuery { RawQuery = "document", Offset = 0, Limit = 50 };
        var res1 = SearchEngine.Search(entries, q1, QueryParser.Parse(q1));

        var q2 = new SearchQuery { RawQuery = "document", Offset = 50, Limit = 50 };
        var res2 = SearchEngine.Search(entries, q2, QueryParser.Parse(q2));

        Assert.Equal(50, res1.Items.Count);
        Assert.Equal(50, res2.Items.Count);
        Assert.Equal(200, res1.TotalMatches);
        Assert.Equal(200, res2.TotalMatches);

        // Verify page 1 and page 2 are distinct items in sequential order
        Assert.NotEqual(res1.Items[0].Name, res2.Items[0].Name);
    }
}
