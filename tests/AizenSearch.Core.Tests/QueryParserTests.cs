using AizenSearch.Core.Models;
using AizenSearch.Core.Search;
using Xunit;

namespace AizenSearch.Core.Tests;

public class QueryParserTests
{
    [Fact]
    public void ParsesExtensionAndTypeFilters()
    {
        var query = new SearchQuery
        {
            RawQuery = "ext:py type:file \"alpha script\" !node_modules",
            Kind = "all"
        };

        var parsed = QueryParser.Parse(query);

        Assert.Equal("py", parsed.EffectiveExt);
        Assert.Equal("files", parsed.EffectiveKind);
        Assert.Contains("node_modules", parsed.Excludes);
        Assert.Contains("alpha script", parsed.Phrases);
    }

    [Fact]
    public void DetectsWildcardsCorrectly()
    {
        var query = new SearchQuery { RawQuery = "cherax*.exe" };
        var parsed = QueryParser.Parse(query);

        Assert.NotNull(parsed.WildcardRegex);
        Assert.True(parsed.WildcardRegex.IsMatch("CheraxLoader.exe"));
        Assert.False(parsed.WildcardRegex.IsMatch("CheraxLoader.txt"));
    }
}
