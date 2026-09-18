using System.Diagnostics;
using AizenSearch.Core.Models;

namespace AizenSearch.Core.Services;

public static class PreviewService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "png", "jpg", "jpeg", "gif", "webp", "bmp", "svg", "ico"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "wav", "ogg", "flac", "m4a", "aac"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp4", "webm", "mkv", "avi", "mov", "wmv", "flv", "m4v"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "md", "rs", "py", "js", "ts", "json", "toml", "yaml", "yml", "css",
        "html", "log", "csv", "xml", "ini", "cs", "cpp", "h", "c", "sql", "sh", "bat",
        "ps1", "cmd", "gitignore", "env", "config", "sln", "csproj", "rs", "go", "java"
    };

    private static bool LooksLikeText(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var probe = new byte[Math.Min(4096, fs.Length)];
            if (probe.Length == 0) return true;
            var read = fs.Read(probe, 0, probe.Length);
            for (var i = 0; i < read; i++)
            {
                var b = probe[i];
                // Reject NUL bytes and most control characters (allow tab/newline/CR)
                if (b == 0) return false;
                if (b < 0x09) return false;
                if (b > 0x0D && b < 0x20) return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string FormatHumanSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double val = bytes;
        var i = 0;
        while (val >= 1024.0 && i < units.Length - 1)
        {
            val /= 1024.0;
            i++;
        }
        return i == 0 ? $"{bytes:N0} bytes" : $"{val:F1} {units[i]} ({bytes:N0} bytes)";
    }

    /// <summary>
    /// Fast metadata-only result used while a connected location is slow or unavailable.
    /// The preview pane must remain useful instead of staying blank during a
    /// remote network read.
    /// </summary>
    public static PreviewData GenerateFallbackPreview(string path)
    {
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)) name = path;
        var isDir = Directory.Exists(path);
        var ext = isDir ? string.Empty : Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        var kind = isDir ? "folder"
            : ImageExtensions.Contains(ext) ? "image"
            : VideoExtensions.Contains(ext) ? "video"
            : AudioExtensions.Contains(ext) ? "audio"
            : TextExtensions.Contains(ext) ? "text" : "meta";
        var size = "Remote file";
        try
        {
            if (!isDir && File.Exists(path)) size = FormatHumanSize(new FileInfo(path).Length);
            else if (isDir) size = "Remote folder";
        }
        catch { }
        return new PreviewData
        {
            Path = path,
            Name = name,
            Kind = kind,
            Size = size,
            Modified = "Remote / unavailable metadata",
            Created = "—",
            Extension = ext.ToUpperInvariant(),
            ExtraInfo = "Preview is loading. Click Open if the connected location is slow or unavailable."
        };
    }

    public static PreviewData GeneratePreview(string path)
    {
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)) name = path;

        var isRemote = path.StartsWith(@"R:\", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase);
        var isDir = Directory.Exists(path);
        var ext = isDir ? string.Empty : Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

        var sizeStr = "Unavailable";
        var modStr = "Unknown";
        var createStr = "Unknown";
        var extraInfo = string.Empty;
        var kind = "meta";
        var content = string.Empty;
        var iconBase64 = string.Empty;

        try
        {
            if (isDir)
            {
                var dirInfo = new DirectoryInfo(path);
                if (dirInfo.Exists)
                {
                    modStr = dirInfo.LastWriteTime.ToString("yyyy-MM-dd hh:mm tt");
                    createStr = dirInfo.CreationTime.ToString("yyyy-MM-dd hh:mm tt");
                    kind = "folder";

                    try
                    {
                        var filesCount = dirInfo.EnumerateFiles().Count();
                        var dirsCount = dirInfo.EnumerateDirectories().Count();
                        sizeStr = $"{filesCount} files, {dirsCount} folders";
                        extraInfo = $"Contains {filesCount} files and {dirsCount} subdirectories";
                    }
                    catch
                    {
                        sizeStr = "Folder";
                    }
                }
                iconBase64 = ShellIconService.ExtractIconAsBase64(true, "__folder__", true);
            }
            else if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                sizeStr = FormatHumanSize(fileInfo.Length);
                modStr = fileInfo.LastWriteTime.ToString("yyyy-MM-dd hh:mm tt");
                createStr = fileInfo.CreationTime.ToString("yyyy-MM-dd hh:mm tt");

                // Extract high-res icon
                if (ext == "exe" || ext == "lnk" || ext == "ico")
                {
                    iconBase64 = ShellIconService.ExtractRealFileIcon(path, true);
                }
                else
                {
                    iconBase64 = ShellIconService.ExtractIconAsBase64(false, ext, true);
                }

                // Categorized preview generation
                if (ImageExtensions.Contains(ext))
                {
                    kind = "image";
                    // Mounted locations are exposed as normal Windows files. Read
                    // small remote images exactly like local images; only the
                    // bounded size guard prevents a huge base64 payload.
                    if (fileInfo.Length <= 15 * 1024 * 1024)
                    {
                        try
                        {
                            var fileBytes = File.ReadAllBytes(path);
                            var mime = ext switch
                            {
                                "jpg" or "jpeg" => "image/jpeg",
                                "gif" => "image/gif",
                                "webp" => "image/webp",
                                "bmp" => "image/bmp",
                                "svg" => "image/svg+xml",
                                _ => "image/png"
                            };
                            content = $"data:{mime};base64,{Convert.ToBase64String(fileBytes)}";
                        }
                        catch (Exception ex)
                        {
                            extraInfo = $"Remote image could not be read: {ex.Message}";
                        }
                    }
                    else if (isRemote)
                    {
                        extraInfo = "Remote image is larger than the inline preview limit. Click Open to view it.";
                    }
                }
                else if (AudioExtensions.Contains(ext))
                {
                    kind = "audio";
                    extraInfo = $"{ext.ToUpperInvariant()} Audio File";
                    if (fileInfo.Length <= 25 * 1024 * 1024)
                    {
                        try
                        {
                            var fileBytes = File.ReadAllBytes(path);
                            var mime = ext switch
                            {
                                "wav" => "audio/wav",
                                "ogg" => "audio/ogg",
                                _ => "audio/mpeg"
                            };
                            content = $"data:{mime};base64,{Convert.ToBase64String(fileBytes)}";
                        }
                        catch (Exception ex)
                        {
                            extraInfo = $"Remote audio could not be read: {ex.Message}";
                        }
                    }
                    else if (isRemote)
                    {
                        extraInfo = "Remote audio is larger than the inline preview limit. Click Open to play it.";
                    }
                }
                else if (VideoExtensions.Contains(ext))
                {
                    kind = "video";
                    extraInfo = $"{ext.ToUpperInvariant()} Video Media";
                    if ((ext == "mp4" || ext == "webm") && fileInfo.Length <= 30 * 1024 * 1024)
                    {
                        try
                        {
                            var fileBytes = File.ReadAllBytes(path);
                            content = $"data:video/{ext};base64,{Convert.ToBase64String(fileBytes)}";
                        }
                        catch (Exception ex)
                        {
                            extraInfo = $"Remote video could not be read: {ex.Message}";
                        }
                    }
                    else if (isRemote)
                    {
                        extraInfo = "Video is too large for inline playback. Click Open to play it from the connected location.";
                    }
                }
                else if (ext == "exe")
                {
                    kind = "exe";
                    try
                    {
                        var vi = FileVersionInfo.GetVersionInfo(path);
                        extraInfo = $"{vi.FileDescription ?? vi.ProductName ?? "Application"}\nVersion: {vi.FileVersion ?? "1.0.0.0"}\nCompany: {vi.CompanyName ?? "Unknown"}";
                    }
                    catch { }
                }
                else if (TextExtensions.Contains(ext) || (fileInfo.Length <= 1024 * 1024 && LooksLikeText(path)))
                {
                    try
                    {
                        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        var maxRead = (int)Math.Min(stream.Length, 65536);
                        var buffer = new byte[maxRead];
                        var read = stream.Read(buffer, 0, buffer.Length);
                        kind = "text";
                        content = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                    }
                    catch (Exception ex)
                    {
                        kind = "text";
                        extraInfo = $"Remote text could not be read: {ex.Message}";
                    }
                }
            }
            else
            {
                // Virtual or offline path metadata
                sizeStr = isDir ? "Folder" : "File";
                kind = isDir ? "folder" : (ImageExtensions.Contains(ext) ? "image" : (VideoExtensions.Contains(ext) ? "video" : (AudioExtensions.Contains(ext) ? "audio" : "meta")));
                iconBase64 = ShellIconService.ExtractIconAsBase64(isDir, ext, true);
            }
        }
        catch (Exception ex)
        {
            extraInfo = ex.Message;
        }

        return new PreviewData
        {
            Path = path,
            Name = name,
            Kind = kind,
            Size = sizeStr,
            Modified = modStr,
            Created = createStr,
            Extension = ext.ToUpperInvariant(),
            Content = content,
            IconBase64 = iconBase64,
            ExtraInfo = extraInfo
        };
    }
}
