using AizenSearch.Core.Models;

namespace AizenSearch.Core.Indexing;

public interface IIndexEngine
{
    string Name { get; }
    Task<List<FileEntry>> IndexVolumeAsync(string driveRoot, IProgress<IndexProgress>? progress = null, CancellationToken ct = default);
}
