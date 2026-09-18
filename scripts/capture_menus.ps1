param(
    [string]$MenuArg = "--card-view",
    [string]$OutputFile = (Join-Path (Get-Location) "aizen_card_view.png")
)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinHelper {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    public static void ForceForeground(IntPtr hWnd) {
        ShowWindow(hWnd, 9); // SW_RESTORE
        IntPtr fgWnd = GetForegroundWindow();
        uint fgThread = GetWindowThreadProcessId(fgWnd, IntPtr.Zero);
        uint curThread = GetCurrentThreadId();
        AttachThreadInput(curThread, fgThread, true);
        SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002); // HWND_TOPMOST
        SetForegroundWindow(hWnd);
        SwitchToThisWindow(hWnd, true);
        AttachThreadInput(curThread, fgThread, false);
    }
}
"@

Write-Host "Stopping any existing AizenSearch instances..."
Stop-Process -Name AizenSearch.App -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "Starting AizenSearch with $MenuArg ..."
$proc = Start-Process -FilePath (Join-Path (Split-Path -Parent $PSScriptRoot) "Distribution\Portable\AizenSearch.App.exe") -ArgumentList $MenuArg -PassThru

# Wait for window and WebView2 to initialize
Start-Sleep -Seconds 4

for ($i = 0; $i -lt 5; $i++) {
    $p = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
    if ($p -and $p.MainWindowHandle -ne 0) {
        Write-Host "Bringing window to foreground (HWND: $($p.MainWindowHandle))..."
        [WinHelper]::ForceForeground($p.MainWindowHandle)
        $wshell = New-Object -ComObject WScript.Shell
        $wshell.AppActivate($p.Id)
        break
    }
    Start-Sleep -Milliseconds 500
}

Start-Sleep -Seconds 2

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
$bmp.Save($OutputFile, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()

Write-Host "Screenshot captured successfully to: $OutputFile"
