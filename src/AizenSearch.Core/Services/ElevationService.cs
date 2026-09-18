using System.Diagnostics;
using System.Security.Principal;

namespace AizenSearch.Core.Services;

public static class ElevationService
{
    public static bool IsElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static void RestartElevated()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch
        {
            // User may have cancelled UAC prompt
        }
    }
}
