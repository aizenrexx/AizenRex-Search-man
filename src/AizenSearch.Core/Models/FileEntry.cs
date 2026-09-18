using System.Text.Json.Serialization;
using AizenSearch.Core.Storage;

namespace AizenSearch.Core.Models;

public sealed class FileEntry
{
    public required string Name { get; init; }

    [JsonIgnore]
    public string Dir { get; init; } = string.Empty;

    public string Ext { get; init; } = string.Empty;
    public bool IsFolder { get; init; }

    // Feature: size & date filters
    public long SizeBytes { get; init; }
    public DateTime ModifiedUtc { get; init; }

    public string Path
    {
        get
        {
            if (string.IsNullOrEmpty(Dir)) return Name;
            return Dir.EndsWith('\\') ? Dir + Name : Dir + "\\" + Name;
        }
        init
        {
            if (string.IsNullOrEmpty(Dir) && !string.IsNullOrEmpty(value))
            {
                var d = System.IO.Path.GetDirectoryName(value);
                if (!string.IsNullOrEmpty(d))
                {
                    Dir = DirectoryPool.InternDir(d);
                }
            }
        }
    }

    public static FileEntry Create(string fullPath, bool isFolder, string? name = null, string? ext = null)
    {
        var fileName = name ?? System.IO.Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(fileName)) fileName = fullPath;

        var dir = System.IO.Path.GetDirectoryName(fullPath) ?? string.Empty;
        var internedDir = DirectoryPool.InternDir(dir);

        var extension = ext ?? (isFolder ? string.Empty : System.IO.Path.GetExtension(fullPath));
        var internedExt = DirectoryPool.InternExt(extension);

        long size = 0;
        DateTime modifiedUtc = default;
        try
        {
            if (!isFolder)
            {
                var fi = new System.IO.FileInfo(fullPath);
                if (fi.Exists)
                {
                    size = fi.Length;
                    modifiedUtc = fi.LastWriteTimeUtc;
                }
            }
            else
            {
                var di = new System.IO.DirectoryInfo(fullPath);
                if (di.Exists)
                {
                    modifiedUtc = di.LastWriteTimeUtc;
                }
            }
        }
        catch { }

        return new FileEntry
        {
            Name = fileName,
            Dir = internedDir,
            Ext = internedExt,
            IsFolder = isFolder,
            SizeBytes = size,
            ModifiedUtc = modifiedUtc
        };
    }
}
