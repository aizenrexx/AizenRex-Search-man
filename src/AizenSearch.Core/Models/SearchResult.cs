using System.Collections;
using AizenSearch.Core.Models;

namespace AizenSearch.Core.Models;

public sealed class SearchResult : IReadOnlyList<FileEntry>
{
    public required List<FileEntry> Items { get; init; }
    public required int TotalMatches { get; init; }

    public int Count => Items.Count;
    public FileEntry this[int index] => Items[index];
    public IEnumerator<FileEntry> GetEnumerator() => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
}
