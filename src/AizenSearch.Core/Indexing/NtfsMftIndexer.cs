using System.ComponentModel;
using System.Runtime.InteropServices;
using AizenSearch.Core.Models;
using AizenSearch.Core.Storage;
using Microsoft.Win32.SafeHandles;

namespace AizenSearch.Core.Indexing;

public sealed class NtfsMftIndexer : IIndexEngine
{
    public string Name => "MFT";

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FSCTL_ENUM_USN_DATA = 0x000900b3;
    private const int USN_REASON_FILE_CREATE = 0x00000100;

    [StructLayout(LayoutKind.Sequential)]
    private struct MFT_ENUM_DATA_V0
    {
        public ulong StartFileReferenceNumber;
        public long LowUsn;
        public long HighUsn;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        ref MFT_ENUM_DATA_V0 lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    public Task<List<FileEntry>> IndexVolumeAsync(string driveRoot, IProgress<IndexProgress>? progress = null, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var driveLetter = driveRoot.TrimEnd('\\');
            var volumePath = $@"\\.\{driveLetter}";

            using var volumeHandle = CreateFile(
                volumePath,
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (volumeHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Unable to open volume handle for {volumePath}. Elevation required.");
            }

            var med = new MFT_ENUM_DATA_V0
            {
                StartFileReferenceNumber = 0,
                LowUsn = 0,
                HighUsn = long.MaxValue
            };

            var bufferSize = 64 * 1024;
            var buffer = Marshal.AllocHGlobal(bufferSize);
            var recordMap = new Dictionary<ulong, (string Name, ulong Parent, bool IsFolder)>(1_000_000);

            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var success = DeviceIoControl(
                        volumeHandle,
                        FSCTL_ENUM_USN_DATA,
                        ref med,
                        Marshal.SizeOf(med),
                        buffer,
                        bufferSize,
                        out var bytesReturned,
                        IntPtr.Zero);

                    if (!success)
                    {
                        var error = Marshal.GetLastWin32Error();
                        if (error == 38) // ERROR_HANDLE_EOF
                        {
                            break;
                        }
                        throw new Win32Exception(error, "Failed to read USN journal stream.");
                    }

                    if (bytesReturned <= 8)
                    {
                        break;
                    }

                    var pCurrentRecord = new IntPtr(buffer.ToInt64() + 8);
                    var remaining = (long)bytesReturned - 8;

                    while (remaining > 0)
                    {
                        ct.ThrowIfCancellationRequested();

                        var recordLength = Marshal.ReadInt32(pCurrentRecord);
                        if (recordLength <= 0) break;

                        var frn = (ulong)Marshal.ReadInt64(pCurrentRecord, 8);
                        var pfrn = (ulong)Marshal.ReadInt64(pCurrentRecord, 16);
                        var fileAttributes = (uint)Marshal.ReadInt32(pCurrentRecord, 52);
                        var fileNameLength = Marshal.ReadInt16(pCurrentRecord, 56);
                        var fileNameOffset = Marshal.ReadInt16(pCurrentRecord, 58);

                        var pFileName = new IntPtr(pCurrentRecord.ToInt64() + fileNameOffset);
                        var fileName = Marshal.PtrToStringUni(pFileName, fileNameLength / 2);

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            var isFolder = (fileAttributes & 0x10) != 0;
                            recordMap[frn] = (fileName, pfrn, isFolder);
                        }

                        pCurrentRecord = new IntPtr(pCurrentRecord.ToInt64() + recordLength);
                        remaining -= recordLength;
                    }

                    med.StartFileReferenceNumber = (ulong)Marshal.ReadInt64(buffer);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            // Resolve directory paths with interning
            var pathCache = new Dictionary<ulong, string>(recordMap.Count / 10);
            var results = new List<FileEntry>(recordMap.Count);
            var rootPrefix = DirectoryPool.InternDir(driveRoot.TrimEnd('\\') + "\\");

            string ResolveDir(ulong frn)
            {
                if (pathCache.TryGetValue(frn, out var cached)) return cached;

                if (!recordMap.TryGetValue(frn, out var rec))
                {
                    return rootPrefix;
                }

                if (rec.Parent == frn || rec.Parent == 0)
                {
                    var rootPath = DirectoryPool.InternDir(rootPrefix + rec.Name);
                    pathCache[frn] = rootPath;
                    return rootPath;
                }

                var parentDir = ResolveDir(rec.Parent);
                var full = DirectoryPool.InternDir(Path.Combine(parentDir, rec.Name));
                pathCache[frn] = full;
                return full;
            }

            foreach (var (frn, rec) in recordMap)
            {
                ct.ThrowIfCancellationRequested();
                var parentDir = (rec.Parent == frn || rec.Parent == 0) ? rootPrefix : ResolveDir(rec.Parent);
                var ext = rec.IsFolder ? string.Empty : DirectoryPool.InternExt(Path.GetExtension(rec.Name));

                results.Add(new FileEntry
                {
                    Name = rec.Name,
                    Dir = parentDir,
                    Ext = ext,
                    IsFolder = rec.IsFolder
                });
            }

            return results;
        }, ct);
    }
}
