using AizenSearch.Core.Models;

namespace AizenSearch.Core.Storage;

public static class BinaryCacheService
{
    private const int MagicV2 = 0x41495A32; // "AIZ2"
    private const int MagicV3 = 0x41495A33; // "AIZ3" — adds SizeBytes + ModifiedUtc

    public static string CacheFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AizenSearch",
        "index.bin");

    public static List<FileEntry> LoadCache(string? customPath = null)
    {
        var path = customPath ?? CacheFilePath;
        if (File.Exists(path))
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 128 * 1024);
                using var reader = new BinaryReader(fs);

                var headerOrCount = reader.ReadInt32();
                if (headerOrCount == MagicV3)
                {
                    // V3 Compact Pool format with size + modified time
                    var dirCount = reader.ReadInt32();
                    var dirTable = new string[dirCount];
                    for (var d = 0; d < dirCount; d++)
                    {
                        dirTable[d] = DirectoryPool.InternDir(reader.ReadString());
                    }

                    var extCount = reader.ReadInt32();
                    var extTable = new string[extCount];
                    for (var e = 0; e < extCount; e++)
                    {
                        extTable[e] = DirectoryPool.InternExt(reader.ReadString());
                    }

                    var count = reader.ReadInt32();
                    var list = new List<FileEntry>(count);
                    for (var i = 0; i < count; i++)
                    {
                        var name = reader.ReadString();
                        var dirIdx = reader.ReadInt32();
                        var extIdx = reader.ReadInt16();
                        var isFolder = reader.ReadBoolean();
                        var sizeBytes = reader.ReadInt64();
                        var modifiedTicks = reader.ReadInt64();

                        var dir = (dirIdx >= 0 && dirIdx < dirCount) ? dirTable[dirIdx] : string.Empty;
                        var ext = (extIdx >= 0 && extIdx < extCount) ? extTable[extIdx] : string.Empty;

                        list.Add(new FileEntry
                        {
                            Name = name,
                            Dir = dir,
                            Ext = ext,
                            IsFolder = isFolder,
                            SizeBytes = sizeBytes,
                            ModifiedUtc = modifiedTicks != 0 ? new DateTime(modifiedTicks, DateTimeKind.Utc) : default
                        });
                    }
                    return list;
                }
                else if (headerOrCount == MagicV2)
                {
                    // V2 Compact Pool format (no size/time)
                    var dirCount = reader.ReadInt32();
                    var dirTable = new string[dirCount];
                    for (var d = 0; d < dirCount; d++)
                    {
                        dirTable[d] = DirectoryPool.InternDir(reader.ReadString());
                    }

                    var extCount = reader.ReadInt32();
                    var extTable = new string[extCount];
                    for (var e = 0; e < extCount; e++)
                    {
                        extTable[e] = DirectoryPool.InternExt(reader.ReadString());
                    }

                    var count = reader.ReadInt32();
                    var list = new List<FileEntry>(count);
                    for (var i = 0; i < count; i++)
                    {
                        var name = reader.ReadString();
                        var dirIdx = reader.ReadInt32();
                        var extIdx = reader.ReadInt16();
                        var isFolder = reader.ReadBoolean();

                        var dir = (dirIdx >= 0 && dirIdx < dirCount) ? dirTable[dirIdx] : string.Empty;
                        var ext = (extIdx >= 0 && extIdx < extCount) ? extTable[extIdx] : string.Empty;

                        list.Add(new FileEntry
                        {
                            Name = name,
                            Dir = dir,
                            Ext = ext,
                            IsFolder = isFolder
                        });
                    }
                    return list;
                }
                else if (headerOrCount > 0)
                {
                    // V1 Legacy format
                    var count = headerOrCount;
                    var list = new List<FileEntry>(count);
                    for (var i = 0; i < count; i++)
                    {
                        var name = reader.ReadString();
                        var fullPath = reader.ReadString();
                        var ext = reader.ReadString();
                        var isFolder = reader.ReadBoolean();

                        list.Add(FileEntry.Create(fullPath, isFolder, name, ext));
                    }
                    return list;
                }
            }
            catch
            {
                // Corrupted cache fallback
            }
        }

        // Only try importing legacy index.json when using the default cache path
        if (customPath == null)
        {
            var legacyJson = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AizenSearch",
                "index.json");

            if (File.Exists(legacyJson))
            {
                var imported = ImportLegacyJson(legacyJson);
                if (imported.Count > 0)
                {
                    try { SaveCache(imported); } catch { }
                    return imported;
                }
            }
        }

        return [];
    }

    private static List<FileEntry> ImportLegacyJson(string jsonPath)
    {
        try
        {
            using var fs = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
            using var doc = System.Text.Json.JsonDocument.Parse(fs);
            var root = doc.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Array) return [];

            var list = new List<FileEntry>(root.GetArrayLength());
            foreach (var elem in root.EnumerateArray())
            {
                var name = elem.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var path = elem.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                var ext = elem.TryGetProperty("ext", out var e) ? e.GetString() ?? "" : "";
                var folder = elem.TryGetProperty("folder", out var f) && f.GetBoolean();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path))
                {
                    list.Add(FileEntry.Create(path, folder, name, ext));
                }
            }
            return list;
        }
        catch
        {
            return [];
        }
    }

    public static void SaveCache(IReadOnlyList<FileEntry> entries, string? customPath = null)
    {
        var targetPath = customPath ?? CacheFilePath;
        var dir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tmpPath = targetPath + ".tmp";

        try
        {
            var dirDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var dirList = new List<string>(50000);

            var extDict = new Dictionary<string, short>(StringComparer.OrdinalIgnoreCase);
            var extList = new List<string>(500);

            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var d = e.Dir;
                if (string.IsNullOrEmpty(d) && !string.IsNullOrEmpty(e.Path))
                {
                    d = Path.GetDirectoryName(e.Path) ?? string.Empty;
                }
                if (!dirDict.ContainsKey(d))
                {
                    dirDict[d] = dirList.Count;
                    dirList.Add(d);
                }

                var ex = e.Ext ?? string.Empty;
                if (!extDict.ContainsKey(ex))
                {
                    extDict[ex] = (short)extList.Count;
                    extList.Add(ex);
                }
            }

            using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(MagicV3);

                writer.Write(dirList.Count);
                for (var d = 0; d < dirList.Count; d++)
                {
                    writer.Write(dirList[d]);
                }

                writer.Write(extList.Count);
                for (var e = 0; e < extList.Count; e++)
                {
                    writer.Write(extList[e]);
                }

                writer.Write(entries.Count);
                for (var i = 0; i < entries.Count; i++)
                {
                    var item = entries[i];
                    writer.Write(item.Name);

                    var d = item.Dir;
                    if (string.IsNullOrEmpty(d) && !string.IsNullOrEmpty(item.Path))
                    {
                        d = Path.GetDirectoryName(item.Path) ?? string.Empty;
                    }
                    var dirIdx = dirDict.TryGetValue(d, out var di) ? di : 0;
                    writer.Write(dirIdx);

                    var ex = item.Ext ?? string.Empty;
                    var extIdx = extDict.TryGetValue(ex, out var ei) ? ei : (short)0;
                    writer.Write(extIdx);

                    writer.Write(item.IsFolder);
                    writer.Write(item.SizeBytes);
                    writer.Write(item.ModifiedUtc == default ? 0L : item.ModifiedUtc.Ticks);
                }
            }

            if (File.Exists(targetPath))
            {
                File.Replace(tmpPath, targetPath, null);
            }
            else
            {
                File.Move(tmpPath, targetPath);
            }
        }
        catch
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
        }
    }
}
