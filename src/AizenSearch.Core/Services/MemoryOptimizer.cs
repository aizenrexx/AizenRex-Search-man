using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AizenSearch.Core.Services;

public static class MemoryOptimizer
{
    [DllImport("psapi.dll", SetLastError = true)]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    public static void TrimWorkingSet()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            using var currentProcess = Process.GetCurrentProcess();
            EmptyWorkingSet(currentProcess.Handle);
        }
        catch { }
    }
}
