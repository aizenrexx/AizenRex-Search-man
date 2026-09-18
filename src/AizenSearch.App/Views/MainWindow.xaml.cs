using System.IO;
using System.Windows;
using AizenSearch.App.Ipc;
using AizenSearch.Core.Indexing;
using AizenSearch.Core.Services;
using Microsoft.Web.WebView2.Core;

namespace AizenSearch.App.Views;

public partial class MainWindow : Window
{
    private readonly IndexManager _indexManager = new();
    private IpcBridge? _ipcBridge;
    private RealtimeIndexWatcher? _realtimeWatcher;
    private readonly CancellationTokenSource _driveWatcherCts = new();
    private int _rebuildInProgress;
    private bool _closed;
    private readonly List<string> _pendingSecondInstanceArgs = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        // Single source of truth for the version: UpdateService.CurrentVersion.
        // The window title, the in-app badge and the About dialog all read from it,
        // so they can never disagree with each other again.
        Title = $"AizenRex Search-man v{UpdateService.CurrentVersion} (.NET 9)";

        // Bug #4 fix: a second instance forwards its command-line args to this (first) instance.
        App.SecondInstanceArgs += OnSecondInstanceArgs;
    }

    private void OnSecondInstanceArgs(string[] args)
    {
        lock (_pendingSecondInstanceArgs)
        {
            _pendingSecondInstanceArgs.AddRange(args);
        }
        // If the webview is already up, process immediately; otherwise NavigationCompleted will.
        if (_ipcBridge != null)
        {
            ProcessCommandLineArgs(args);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _closed = true;
        _driveWatcherCts.Cancel();
        _driveWatcherCts.Dispose();
        _realtimeWatcher?.Dispose();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _indexManager.Initialize();

            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AizenSearch",
                "WebView2");
            Directory.CreateDirectory(userDataFolder);

            // Keep WebView2 lightweight on low-memory Windows machines without changing app features.
            var options = new CoreWebView2EnvironmentOptions(
                "--disable-http-cache --disable-background-networking --disable-component-update " +
                "--disable-sync --disable-extensions --disable-default-apps --no-first-run " +
                "--disable-features=Translate,MediaRouter,OptimizationHints --disk-cache-size=1 --media-cache-size=1");
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            await WebViewControl.EnsureCoreWebView2Async(env);

            var webFolder = Path.Combine(AppContext.BaseDirectory, "Web");
            WebViewControl.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.aizen",
                webFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            _ipcBridge = new IpcBridge(WebViewControl, _indexManager);
            WebViewControl.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            WebViewControl.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            WebViewControl.CoreWebView2.Navigate("https://app.aizen/index.html");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "AizenRex Search-man Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_ipcBridge != null)
        {
            try
            {
                var raw = e.WebMessageAsJson;
                if (string.IsNullOrEmpty(raw))
                {
                    raw = e.TryGetWebMessageAsString();
                }

                if (!string.IsNullOrEmpty(raw))
                {
                    await _ipcBridge.HandleMessageAsync(raw);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error receiving web message: {ex.Message}");
            }
        }
    }

    private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Hand the running version to the interface so every label agrees.
        _ipcBridge?.EvalJs("appInfo", new { version = UpdateService.CurrentVersion });

        if (!e.IsSuccess) return;

        var cachedCount = _indexManager.TotalCount;
        var drives = IndexManager.GetAvailableDrives();
        var elevated = ElevationService.IsElevated();

        var bootPayload = new
        {
            count = cachedCount,
            drives,
            cached = cachedCount > 0,
            elevated,
            engine = elevated ? "MFT / USN engine" : "Parallel deep scan fallback"
        };

        _ipcBridge?.EvalJs("boot", bootPayload);

        // Process any args forwarded by a second instance before our own.
        string[] pendingArgs;
        lock (_pendingSecondInstanceArgs)
        {
            pendingArgs = _pendingSecondInstanceArgs.ToArray();
            _pendingSecondInstanceArgs.Clear();
        }
        if (pendingArgs.Length > 0)
        {
            ProcessCommandLineArgs(pendingArgs);
        }

        ProcessCommandLineArgs(Environment.GetCommandLineArgs());

        // Start real-time index watcher for local and virtual drives.
        // Dispose any previous watcher first so a WebView2 reload never leaks a duplicate.
        _realtimeWatcher?.Dispose();
        _realtimeWatcher = new RealtimeIndexWatcher(_indexManager);
        _realtimeWatcher.Start();

        if (cachedCount == 0)
        {
            var startPayload = new
            {
                totalDrives = drives.Count,
                drives,
                cachedCount
            };
            _ipcBridge?.EvalJs("indexStarted", startPayload);

            _ = Task.Run(async () =>
            {
                await _indexManager.StartRebuildAsync(
                    null,
                    progress => _ipcBridge?.EvalJs("indexDriveProgress", progress),
                    summary => _ipcBridge?.EvalJs("indexReady", summary));
            });
        }
        else
        {
            var missingDrives = _indexManager.GetMissingDrives();
            if (missingDrives.Count > 0)
            {
                var startPayload = new
                {
                    totalDrives = missingDrives.Count,
                    drives = missingDrives,
                    cachedCount
                };
                _ipcBridge?.EvalJs("indexStarted", startPayload);

                _ = Task.Run(async () =>
                {
                    await _indexManager.StartRebuildAsync(
                        missingDrives,
                        progress => _ipcBridge?.EvalJs("indexDriveProgress", progress),
                        summary => _ipcBridge?.EvalJs("indexReady", summary));
                });
            }
        }

        // Schedule background memory trim shortly after WebView2 initialization stabilizes
        _ = Task.Run(async () =>
        {
            await Task.Delay(2500);
            MemoryOptimizer.TrimWorkingSet();
        });

        StartDriveWatcher();
    }

    private void StartDriveWatcher()
    {
        _ = Task.Run(async () =>
        {
            while (!_closed && !_driveWatcherCts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(15000, _driveWatcherCts.Token);
                    if (_closed || _driveWatcherCts.IsCancellationRequested) break;

                    var missing = _indexManager.GetMissingDrives();
                    if (missing.Count > 0 && Interlocked.CompareExchange(ref _rebuildInProgress, 1, 0) == 0)
                    {
                        try
                        {
                            var updatedDrives = IndexManager.GetAvailableDrives();
                            _ipcBridge?.EvalJs("drivesUpdated", updatedDrives);

                            await _indexManager.StartRebuildAsync(
                                missing,
                                progress => _ipcBridge?.EvalJs("indexDriveProgress", progress),
                                summary => _ipcBridge?.EvalJs("indexReady", summary));
                        }
                        finally
                        {
                            Interlocked.Exchange(ref _rebuildInProgress, 0);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch { }
            }
        });
    }

    private void ProcessCommandLineArgs(string[] cmdArgs)
    {
        if (cmdArgs == null || _ipcBridge == null) return;

        if (cmdArgs.Any(a => a.Equals("--preview", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("togglePreview", new { });
        }

        if (cmdArgs.Any(a => a.Equals("--changelog", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("toggleChangelog", true);
        }

        if (cmdArgs.Any(a => a.Equals("--about", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("toggleAbout", true);
        }

        if (cmdArgs.Any(a => a.Equals("--updates", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("checkUpdate", new { });
        }

        if (cmdArgs.Any(a => a.Equals("--file-menu", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("toggleMenu", "file");
        }

        if (cmdArgs.Any(a => a.Equals("--edit-menu", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("toggleMenu", "edit");
        }

        if (cmdArgs.Any(a => a.Equals("--view-menu", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("toggleMenu", "view");
        }

        if (cmdArgs.Any(a => a.Equals("--search-menu", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("toggleMenu", "search");
        }

        if (cmdArgs.Any(a => a.Equals("--syntax", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("showSyntaxModal", true);
        }

        if (cmdArgs.Any(a => a.Equals("--card-view", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("setViewMode", "card");
        }

        if (cmdArgs.Any(a => a.Equals("--compact-view", StringComparison.OrdinalIgnoreCase)))
        {
            _ipcBridge.EvalJs("setViewMode", "compact");
        }

        var searchArg = cmdArgs.Skip(1).FirstOrDefault(a => !a.StartsWith("--"));
        if (!string.IsNullOrWhiteSpace(searchArg))
        {
            _ipcBridge.EvalJs("setSearchQuery", searchArg);
        }
    }
}