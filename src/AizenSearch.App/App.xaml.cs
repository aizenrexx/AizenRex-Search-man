using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace AizenSearch.App;

public partial class App : Application
{
    private static Mutex? _mutex;
    private static readonly string PipeName = "AizenSearchSingleInstancePipe";

    /// <summary>Raised when a second instance forwards its command-line arguments.</summary>
    public static event Action<string[]>? SecondInstanceArgs;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception logging
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            const string mutexName = "Local\\AizenSearchSingleInstanceMutex";
            _mutex = new Mutex(true, mutexName, out var createdNew);

            if (!createdNew)
            {
                // Another instance is already running — forward our args to it, then exit.
                ForwardToFirstInstance(e.Args);
                Shutdown();
                return;
            }
        }
        catch
        {
            // If mutex creation fails, allow starting without crashing
        }

        base.OnStartup(e);

        // First instance: listen for args forwarded by later instances.
        StartPipeServer();

        // Feature #19: diagnostics log
        try { AizenSearch.Core.Services.LogService.Info("AizenSearch starting"); } catch { }
    }

    private static void ForwardToFirstInstance(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000);
            var payload = string.Join('\u001F', args); // unit separator between args
            var bytes = Encoding.UTF8.GetBytes(payload);
            client.Write(bytes, 0, bytes.Length);
            client.Flush();
        }
        catch
        {
            // If the pipe is unavailable, the first instance may be starting up — ignore.
        }
    }

    private void StartPipeServer()
    {
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync();
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var payload = await reader.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(payload))
                    {
                        var args = payload.Split('\u001F', StringSplitOptions.RemoveEmptyEntries);
                        if (args.Length > 0)
                        {
                            // Marshal to the UI thread so MainWindow can react safely.
                            _ = Dispatcher.BeginInvoke(() => SecondInstanceArgs?.Invoke(args));
                        }
                    }
                }
                catch
                {
                    // Pipe error — keep listening for the next connection.
                }
            }
        });
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        e.Handled = true; // Prevent app crashing
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogCrash(ex);
        }
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AizenSearch");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, "crash.log");
            File.AppendAllText(logFile, $"[{DateTime.Now}] {ex}\n\n");
        }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch { }

        _mutex?.Dispose();
        base.OnExit(e);
    }
}
