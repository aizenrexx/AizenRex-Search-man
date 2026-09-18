using System.Diagnostics;
using AizenSearch.Core.Models;

namespace AizenSearch.Core.Services;

public static class ProcessService
{
    public static void OpenPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch { }
    }

    public static void OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
            }
            else if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }

    public static void OpenTerminal(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var dir = Directory.Exists(path) ? path : (Path.GetDirectoryName(path) ?? "");
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                WorkingDirectory = dir,
                UseShellExecute = true
            });
        }
        catch { }
    }

    public static void RunAsAdmin(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                Verb = "runas",
                UseShellExecute = true
            });
        }
        catch { }
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private struct SHELLEXECUTEINFO
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        public string lpVerb;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        public string lpFile;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        public string lpParameters;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    private const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;

    public static void ShowProperties(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var info = new SHELLEXECUTEINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = path;
            info.nShow = 5; // SW_SHOW
            info.fMask = SEE_MASK_INVOKEIDLIST;
            ShellExecuteEx(ref info);
        }
        catch { }
    }

    public static string ComputeSha256(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return string.Empty;
        try
        {
            // Guard against hashing huge files (e.g. multi-GB movies on R:),
            // which would otherwise block the background thread for a long time.
            const long MaxHashBytes = 2L * 1024 * 1024 * 1024; // 2 GB
            var info = new FileInfo(path);
            if (info.Length > MaxHashBytes)
            {
                return $"Error: File too large to hash ({info.Length / (1024.0 * 1024.0):N0} MB)";
            }

            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var hash = sha.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public static string ExportToCsv(string? customTargetFolder, IEnumerable<FileEntry> items)
    {
        try
        {
            var folder = (!string.IsNullOrEmpty(customTargetFolder) && Directory.Exists(customTargetFolder))
                ? customTargetFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"AizenSearch_Export_{timestamp}.csv";
            var fullPath = Path.Combine(folder, filename);

            using var writer = new StreamWriter(fullPath, false, System.Text.Encoding.UTF8);
            writer.WriteLine("Name,Path,Type,IsFolder");
            foreach (var item in items)
            {
                var name = item.Name.Replace("\"", "\"\"");
                var p = item.Path.Replace("\"", "\"\"");
                writer.WriteLine($"\"{name}\",\"{p}\",\"{item.Ext}\",{(item.IsFolder ? "Folder" : "File")}");
            }
            return fullPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CSV export error: {ex.Message}");
            return string.Empty;
        }
    }
}
