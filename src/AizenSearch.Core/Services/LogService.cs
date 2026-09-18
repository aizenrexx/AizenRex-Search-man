using System;
using System.IO;
using System.Threading;

namespace AizenSearch.Core.Services;

/// <summary>
/// Feature #19: lightweight rotating file logger for diagnostics.
/// Writes to %LOCALAPPDATA%\AizenSearch\logs\aizen.log (max ~1 MB, keeps .1 backup).
/// </summary>
public static class LogService
{
    private static readonly object Gate = new();
    private static string? _logDir;
    private static string? _logPath;
    private static long _bytesWritten;
    private static bool _initialized;

    public static string LogDirectory
    {
        get
        {
            EnsureInit();
            return _logDir ?? "";
        }
    }

    private static void EnsureInit()
    {
        if (_initialized) return;
        lock (Gate)
        {
            if (_initialized) return;
            try
            {
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AizenSearch", "logs");
                Directory.CreateDirectory(baseDir);
                _logDir = baseDir;
                _logPath = Path.Combine(baseDir, "aizen.log");
                _initialized = true;
            }
            catch
            {
                _logPath = null;
                _initialized = true;
            }
        }
    }

    /// <summary>Appends a line to the log file. Safe to call from any thread.</summary>
    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null)
    {
        Write("ERROR", ex is null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}");
    }

    private static void Write(string level, string message)
    {
        EnsureInit();
        if (string.IsNullOrEmpty(_logPath)) return;

        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}";
            lock (Gate)
            {
                File.AppendAllText(_logPath, line);
                _bytesWritten += line.Length;
                if (_bytesWritten > 1_000_000)
                {
                    Rotate();
                }
            }
        }
        catch { /* logging must never crash the app */ }
    }

    private static void Rotate()
    {
        try
        {
            if (File.Exists(_logPath))
            {
                var backup = _logPath + ".1";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(_logPath, backup);
            }
            _bytesWritten = 0;
        }
        catch { }
    }

    /// <summary>Opens the log folder in Explorer (IPC helper).</summary>
    public static void OpenLogFolder()
    {
        EnsureInit();
        try
        {
            if (!string.IsNullOrEmpty(_logDir) && Directory.Exists(_logDir))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _logDir,
                    UseShellExecute = true
                });
            }
        }
        catch { }
    }
}
