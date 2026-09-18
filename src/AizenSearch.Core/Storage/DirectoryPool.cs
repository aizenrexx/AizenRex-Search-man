using System.Collections.Concurrent;

namespace AizenSearch.Core.Storage;

public static class DirectoryPool
{
    private static readonly ConcurrentDictionary<string, string> _dirPool = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> _extPool = new(StringComparer.OrdinalIgnoreCase);

    public static string InternDir(string? dir)
    {
        if (string.IsNullOrEmpty(dir)) return string.Empty;
        var normalized = (dir.Length > 3 && dir.EndsWith('\\')) ? dir.TrimEnd('\\') : dir;
        return _dirPool.GetOrAdd(normalized, normalized);
    }

    public static string InternExt(string? ext)
    {
        if (string.IsNullOrEmpty(ext)) return string.Empty;
        var clean = ext.TrimStart('.').ToLowerInvariant();
        return _extPool.GetOrAdd(clean, clean);
    }

    public static void Clear()
    {
        _dirPool.Clear();
        _extPool.Clear();
    }

    public static int UniqueDirectoriesCount => _dirPool.Count;
}
