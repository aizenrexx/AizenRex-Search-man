using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AizenSearch.Core.Services;

public static class DosDeviceHelper
{
    private const uint DDD_RAW_TARGET_PATH = 0x00000001;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool DefineDosDevice(uint dwFlags, string lpDeviceName, string lpTargetPath);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, uint ucchMax);

    [StructLayout(LayoutKind.Sequential)]
    private struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OBJECT_ATTRIBUTES
    {
        public int Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OBJECT_DIRECTORY_INFORMATION
    {
        public UNICODE_STRING Name;
        public UNICODE_STRING TypeName;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtOpenDirectoryObject(
        out IntPtr DirectoryHandle,
        uint DesiredAccess,
        ref OBJECT_ATTRIBUTES ObjectAttributes);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryDirectoryObject(
        IntPtr DirectoryHandle,
        IntPtr Buffer,
        uint Length,
        bool ReturnSingleEntry,
        bool RestartScan,
        ref uint Context,
        out uint ReturnLength);

    [DllImport("ntdll.dll")]
    private static extern int NtClose(IntPtr Handle);

    public static List<string> FindWinFspDeviceVolumes()
    {
        var result = new List<string>();

        try
        {
            var str = "\\Device";
            var strBuffer = Marshal.StringToHGlobalUni(str);
            var ustr = new UNICODE_STRING
            {
                Length = (ushort)(str.Length * 2),
                MaximumLength = (ushort)((str.Length + 1) * 2),
                Buffer = strBuffer
            };

            var ustrBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(ustr));
            Marshal.StructureToPtr(ustr, ustrBuffer, false);

            var oa = new OBJECT_ATTRIBUTES
            {
                Length = Marshal.SizeOf<OBJECT_ATTRIBUTES>(),
                RootDirectory = IntPtr.Zero,
                ObjectName = ustrBuffer,
                Attributes = 0x40 // OBJ_CASE_INSENSITIVE
            };

            try
            {
                int status = NtOpenDirectoryObject(out var hDir, 0x0001, ref oa);
                if (status == 0 && hDir != IntPtr.Zero)
                {
                    try
                    {
                        uint bufSize = 65536;
                        var buf = Marshal.AllocHGlobal((int)bufSize);
                        try
                        {
                            uint context = 0;
                            int queryStatus = NtQueryDirectoryObject(hDir, buf, bufSize, false, true, ref context, out uint retLen);
                            if (queryStatus == 0)
                            {
                                var curr = buf;
                                while (true)
                                {
                                    var entry = Marshal.PtrToStructure<OBJECT_DIRECTORY_INFORMATION>(curr);
                                    if (entry.Name.Length == 0) break;

                                    var name = Marshal.PtrToStringUni(entry.Name.Buffer, entry.Name.Length / 2);
                                    var type = Marshal.PtrToStringUni(entry.TypeName.Buffer, entry.TypeName.Length / 2);

                                    if (type == "Device" && !string.IsNullOrEmpty(name) && name.StartsWith("Volume{", StringComparison.OrdinalIgnoreCase))
                                    {
                                        result.Add($"\\Device\\{name}");
                                    }

                                    curr = IntPtr.Add(curr, Marshal.SizeOf<OBJECT_DIRECTORY_INFORMATION>());
                                    if (curr.ToInt64() >= buf.ToInt64() + retLen) break;
                                }
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(buf);
                        }
                    }
                    finally
                    {
                        NtClose(hDir);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(strBuffer);
                Marshal.FreeHGlobal(ustrBuffer);
            }
        }
        catch
        {
            // Ignore native API reflection errors
        }

        // Secondary fallback: WinFsp fsptool CLI
        if (result.Count == 0)
        {
            try
            {
                var fspTool = @"C:\Program Files (x86)\WinFsp\bin\fsptool-x64.exe";
                if (File.Exists(fspTool))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = fspTool,
                        Arguments = "lsvol",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        var stdout = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(2000);
                        foreach (var line in stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                        {
                            var trimmed = line.Trim();
                            var idx = trimmed.IndexOf(@"\Device\Volume{", StringComparison.OrdinalIgnoreCase);
                            if (idx >= 0)
                            {
                                result.Add(trimmed[idx..].Trim());
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return result;
    }

    public static bool EnsureDriveMapped(string driveLetter = "R:")
    {
        var cleanLetter = driveLetter.TrimEnd('\\').ToUpperInvariant();
        if (Directory.Exists(cleanLetter + "\\"))
        {
            return true;
        }

        var sb = new StringBuilder(1024);
        if (QueryDosDevice(cleanLetter, sb, 1024) > 0 && sb.Length > 0)
        {
            if (Directory.Exists(cleanLetter + "\\"))
            {
                return true;
            }
        }

        var volumes = FindWinFspDeviceVolumes();
        foreach (var vol in volumes)
        {
            bool ok = DefineDosDevice(DDD_RAW_TARGET_PATH, cleanLetter, vol);
            if (ok && Directory.Exists(cleanLetter + "\\"))
            {
                return true;
            }
        }

        return Directory.Exists(cleanLetter + "\\");
    }
}
