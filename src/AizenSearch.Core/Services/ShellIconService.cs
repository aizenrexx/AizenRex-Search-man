using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AizenSearch.Core.Services;

public sealed class IconRequestItem
{
    public string Key { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public bool IsFolder { get; init; }
    public string Ext { get; init; } = string.Empty;
}

public static class ShellIconService
{
    private static readonly ConcurrentDictionary<string, string> IconCache = new(StringComparer.OrdinalIgnoreCase);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;
    private const uint SHGFI_SMALLICON = 0x000000001;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Dictionary<string, string> GetIcons(IEnumerable<IconRequestItem> requests)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var req in requests)
        {
            var key = string.IsNullOrEmpty(req.Key) ? (req.IsFolder ? "__folder__" : req.Ext) : req.Key;
            if (string.IsNullOrEmpty(key)) key = "__file__";

            if (IconCache.TryGetValue(key, out var cached))
            {
                result[key] = cached;
                continue;
            }

            string iconBase64 = string.Empty;
            var ext = req.Ext.TrimStart('.').ToLowerInvariant();
            var isExeOrShortcut = !req.IsFolder && (ext == "exe" || ext == "lnk" || ext == "ico");

            if (isExeOrShortcut && !string.IsNullOrEmpty(req.Path) && !req.Path.StartsWith("R:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract TRUE embedded application icon from the local file
                iconBase64 = ExtractRealFileIcon(req.Path);
            }

            if (string.IsNullOrEmpty(iconBase64))
            {
                iconBase64 = ExtractIconAsBase64(req.IsFolder, ext);
            }

            IconCache[key] = iconBase64 ?? string.Empty;
            if (!string.IsNullOrEmpty(iconBase64))
            {
                result[key] = iconBase64;
            }
        }

        return result;
    }

    public static Dictionary<string, string> GetIconsForPaths(IEnumerable<string> paths)
    {
        var items = new List<IconRequestItem>();
        foreach (var p in paths)
        {
            var ext = Path.GetExtension(p).TrimStart('.').ToLowerInvariant();
            var isDir = string.IsNullOrEmpty(ext);
            var key = (!isDir && (ext == "exe" || ext == "lnk" || ext == "ico")) ? p : (isDir ? "__folder__" : ext);
            items.Add(new IconRequestItem { Key = key, Path = p, IsFolder = isDir, Ext = ext });
        }
        return GetIcons(items);
    }

    public static string ExtractRealFileIcon(string path, bool large = false)
    {
        try
        {
            if (File.Exists(path))
            {
                using var icon = Icon.ExtractAssociatedIcon(path);
                if (icon != null)
                {
                    using var bmp = icon.ToBitmap();
                    using var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Png);
                    return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
            }
        }
        catch { }

        return ExtractIconAsBase64(false, "exe", large);
    }

    public static string ExtractIconAsBase64(bool isDir, string key, bool large = false)
    {
        try
        {
            var shinfo = new SHFILEINFO();
            var flags = SHGFI_ICON | (large ? SHGFI_LARGEICON : SHGFI_SMALLICON) | SHGFI_USEFILEATTRIBUTES;
            var attr = isDir ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
            var queryPath = isDir ? "dummy_folder" : (string.IsNullOrEmpty(key) ? "dummy_file" : $".{key}");

            var res = SHGetFileInfo(queryPath, attr, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            if (res != IntPtr.Zero && shinfo.hIcon != IntPtr.Zero)
            {
                using var icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
                DestroyIcon(shinfo.hIcon);

                using var bmp = icon.ToBitmap();
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
            }
        }
        catch { }

        return string.Empty;
    }
}
