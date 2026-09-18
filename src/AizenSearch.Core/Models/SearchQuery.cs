namespace AizenSearch.Core.Models;

public sealed class SearchQuery
{
    public string RawQuery { get; init; } = string.Empty;
    public string Kind { get; init; } = "all";
    public string Ext { get; init; } = string.Empty;
    public int Offset { get; init; } = 0;
    public int Limit { get; init; } = 5000;
    public string Sort { get; init; } = "name";
    public bool Descending { get; init; }
    public bool MatchCase { get; init; }
    public bool WholeWord { get; init; }
    public bool MatchPath { get; init; } = true;
    public bool Regex { get; init; }
    public ulong SearchId { get; init; }

    // Feature: size & date filters
    public long? MinSizeBytes { get; init; }
    public long? MaxSizeBytes { get; init; }
    public DateTime? ModifiedAfter { get; init; }
    public DateTime? ModifiedBefore { get; init; }
    public List<string>? FavoritePaths { get; init; }
}
