namespace AizenSearch.Core.Models;

public sealed class IndexProgress
{
    public string Drive { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public int Completed { get; init; }
    public int TotalDrives { get; init; }
    public int Count { get; init; }
    public float Seconds { get; init; }
    public string Engine { get; init; } = string.Empty;
}
