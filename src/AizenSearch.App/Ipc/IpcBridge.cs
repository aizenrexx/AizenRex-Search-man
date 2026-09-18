using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using AizenSearch.Core.Indexing;
using AizenSearch.Core.Models;
using AizenSearch.Core.Services;
using Microsoft.Web.WebView2.Wpf;

namespace AizenSearch.App.Ipc;

public sealed class IpcBridge
{
    private readonly WebView2 _webView;
    private readonly IndexManager _indexManager;

    private CancellationTokenSource? _realtimeDebounceCts;

    public IpcBridge(WebView2 webView, IndexManager indexManager)
    {
        _webView = webView;
        _indexManager = indexManager;
        _indexManager.OnIndexChanged += HandleIndexChanged;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000);
                var res = await UpdateService.CheckForUpdatesAsync();
                if (res.UpdateAvailable)
                {
                    EvalJs("updateCheckResult", res);
                }
            }
            catch { }
        });
    }

    private void HandleIndexChanged()
    {
        _realtimeDebounceCts?.Cancel();
        _realtimeDebounceCts?.Dispose();
        _realtimeDebounceCts = new CancellationTokenSource();
        var token = _realtimeDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(80, token);
                if (token.IsCancellationRequested) return;

                EvalJs("realtimeIndexChanged", new { count = _indexManager.TotalCount });
            }
            catch { }
        }, token);
    }

    public async Task HandleMessageAsync(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            // Handle stringified JSON from postMessage(JSON.stringify(...))
            if (root.ValueKind == JsonValueKind.String)
            {
                var inner = root.GetString();
                if (!string.IsNullOrEmpty(inner))
                {
                    await HandleMessageAsync(inner);
                }
                return;
            }

            if (!root.TryGetProperty("action", out var actionProp)) return;
            var action = actionProp.GetString();

            switch (action)
            {
                case "search":
                    await HandleSearchAsync(root);
                    break;

                case "preview":
                    HandlePreview(root);
                    break;

                case "icons":
                    HandleIcons(root);
                    break;

                case "open":
                    if (root.TryGetProperty("path", out var openPathProp))
                    {
                        ProcessService.OpenPath(openPathProp.GetString() ?? "");
                    }
                    break;

                case "folder":
                    if (root.TryGetProperty("path", out var folderPathProp))
                    {
                        ProcessService.OpenFolder(folderPathProp.GetString() ?? "");
                    }
                    break;

                case "copy":
                    if (root.TryGetProperty("path", out var copyPathProp))
                    {
                        var text = copyPathProp.GetString() ?? "";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            try { Clipboard.SetText(text); } catch { }
                        });
                    }
                    break;

                case "copy_name":
                    if (root.TryGetProperty("name", out var copyNameProp))
                    {
                        var text = copyNameProp.GetString() ?? "";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            try { Clipboard.SetText(text); } catch { }
                        });
                    }
                    break;

                case "rebuild":
                    var drives = IndexManager.GetAvailableDrives();
                    var startPayload = new
                    {
                        totalDrives = drives.Count,
                        drives,
                        cachedCount = _indexManager.TotalCount
                    };
                    EvalJs("indexStarted", startPayload);

                    _ = Task.Run(async () =>
                    {
                        await _indexManager.StartRebuildAsync(
                            null,
                            progress => EvalJs("indexDriveProgress", progress),
                            summary => EvalJs("indexReady", summary));
                    });
                    break;

                case "elevate":
                    ElevationService.RestartElevated();
                    break;

                case "terminal":
                    if (root.TryGetProperty("path", out var termPathProp))
                    {
                        ProcessService.OpenTerminal(termPathProp.GetString() ?? "");
                    }
                    break;

                case "run_admin":
                    if (root.TryGetProperty("path", out var adminPathProp))
                    {
                        ProcessService.RunAsAdmin(adminPathProp.GetString() ?? "");
                    }
                    break;

                case "properties":
                    if (root.TryGetProperty("path", out var propPathProp))
                    {
                        ProcessService.ShowProperties(propPathProp.GetString() ?? "");
                    }
                    break;

                case "copy_hash":
                    if (root.TryGetProperty("path", out var hashPathProp))
                    {
                        var filePath = hashPathProp.GetString() ?? "";
                        _ = Task.Run(() =>
                        {
                            var hash = ProcessService.ComputeSha256(filePath);
                            if (!string.IsNullOrEmpty(hash))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    try { Clipboard.SetText(hash); } catch { }
                                });
                                EvalJs("toast", new { message = $"SHA-256 Hash copied: {hash.Substring(0, Math.Min(16, hash.Length))}..." });
                            }
                        });
                    }
                    break;



                case "export_csv":
                    _ = Task.Run(() =>
                    {
                        var currentQuery = new SearchQuery
                        {
                            RawQuery = root.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "",
                            Kind = root.TryGetProperty("kind", out var k) ? k.GetString() ?? "all" : "all",
                            Ext = root.TryGetProperty("ext", out var e) ? e.GetString() ?? "" : "",
                            Limit = 5000
                        };
                        var searchRes = _indexManager.Search(currentQuery);
                        var exportedPath = ProcessService.ExportToCsv(null, searchRes.Items);
                        if (string.IsNullOrEmpty(exportedPath))
                        {
                            EvalJs("toast", new { message = "Export failed - no results or an error occurred." });
                            return;
                        }
                        EvalJs("toast", new { message = $"Exported {searchRes.Items.Count:N0} results to Desktop" });
                        ProcessService.OpenFolder(exportedPath);
                    });
                    break;

                case "exit_app":
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Application.Current.Shutdown();
                    });
                    break;

                case "check_update":
                    _ = Task.Run(async () =>
                    {
                        var updateInfo = await UpdateService.CheckForUpdatesAsync();
                        EvalJs("updateCheckResult", updateInfo);
                    });
                    break;

                case "cancel_indexing":
                    _indexManager.CancelCurrentRebuild();
                    EvalJs("indexCancelled", new { });
                    break;

                case "open_log_folder":
                    AizenSearch.Core.Services.LogService.OpenLogFolder();
                    break;

                case "find_duplicates":
                    {
                        var dupSearchId = root.TryGetProperty("search_id", out var dupIdProp) ? dupIdProp.GetInt32() : 0;
                        _ = Task.Run(() =>
                        {
                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            var dupRes = _indexManager.FindDuplicates();
                            sw.Stop();
                            EvalJs("showSearchResponse", new
                            {
                                id = dupSearchId,
                                items = dupRes.Items,
                                total = dupRes.TotalMatches,
                                offset = 0,
                                resetScroll = true,
                                ms = sw.Elapsed.TotalMilliseconds
                            });
                        });
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"IPC error: {ex.Message}");
            AizenSearch.Core.Services.LogService.Error("IPC handler error", ex);
        }
    }

    private async Task HandleSearchAsync(JsonElement root)
    {
        var searchId = root.TryGetProperty("search_id", out var idProp) ? idProp.GetUInt64() : 0;
        var requestedLimit = root.TryGetProperty("limit", out var limProp) ? limProp.GetInt32() : 1000;
        var offset = root.TryGetProperty("offset", out var offProp) ? offProp.GetInt32() : 0;
        var isAppend = root.TryGetProperty("append", out var appProp) && appProp.GetBoolean();
        var resetScroll = !root.TryGetProperty("reset_scroll", out var rsProp) || rsProp.GetBoolean();

        var query = new SearchQuery
        {
            SearchId = searchId,
            RawQuery = root.TryGetProperty("query", out var qProp) ? qProp.GetString() ?? "" : "",
            Kind = root.TryGetProperty("kind", out var kProp) ? kProp.GetString() ?? "all" : "all",
            Ext = root.TryGetProperty("ext", out var extProp) ? extProp.GetString() ?? "" : "",
            Offset = Math.Max(0, offset),
            Limit = Math.Clamp(requestedLimit, 50, 2000),
            Sort = root.TryGetProperty("sort", out var sProp) ? sProp.GetString() ?? "name" : "name",
            Descending = root.TryGetProperty("descending", out var dProp) && dProp.GetBoolean(),
            MatchCase = root.TryGetProperty("match_case", out var mcProp) && mcProp.GetBoolean(),
            WholeWord = root.TryGetProperty("whole_word", out var wwProp) && wwProp.GetBoolean(),
            MatchPath = !root.TryGetProperty("match_path", out var mpProp) || mpProp.GetBoolean(),
            Regex = root.TryGetProperty("regex", out var rxProp) && rxProp.GetBoolean(),
            MinSizeBytes = root.TryGetProperty("min_size", out var minSzProp) && minSzProp.ValueKind == System.Text.Json.JsonValueKind.Number ? minSzProp.GetInt64() : (long?)null,
            FavoritePaths = root.TryGetProperty("favorites", out var favProp) && favProp.ValueKind == System.Text.Json.JsonValueKind.Array
                ? favProp.EnumerateArray().Select(f => f.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                : null,
            MaxSizeBytes = root.TryGetProperty("max_size", out var maxSzProp) && maxSzProp.ValueKind == System.Text.Json.JsonValueKind.Number ? maxSzProp.GetInt64() : (long?)null,
            ModifiedAfter = root.TryGetProperty("modified_after", out var maProp) && DateTime.TryParse(maProp.GetString(), out var ma) ? ma : (DateTime?)null,
            ModifiedBefore = root.TryGetProperty("modified_before", out var mbProp) && DateTime.TryParse(mbProp.GetString(), out var mb) ? mb : (DateTime?)null
        };

        var sw = Stopwatch.StartNew();
        var searchResult = await Task.Run(() => _indexManager.Search(query));
        sw.Stop();

        var payload = new
        {
            id = searchId,
            items = searchResult.Items,
            total = searchResult.TotalMatches,
            offset = query.Offset,
            resetScroll,
            ms = sw.Elapsed.TotalMilliseconds
        };

        if (isAppend || query.Offset > 0)
        {
            EvalJs("appendSearchResponse", payload);
        }
        else
        {
            EvalJs("showSearchResponse", payload);
        }
    }

    private void HandlePreview(JsonElement root)
    {
        if (!root.TryGetProperty("path", out var pathProp)) return;
        var path = pathProp.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(path)) return;

        // Show a useful remote metadata card immediately. Connected-location reads
        // may take seconds, and the pane must never appear blank while waiting.
        EvalJs("showPreview", PreviewService.GenerateFallbackPreview(path));
        LogService.Info($"Preview requested: {path}");

        _ = Task.Run(() =>
        {
            try
            {
                var data = PreviewService.GeneratePreview(path);
                LogService.Info($"Preview generated: {path} [{data.Kind}, {data.Content.Length} content chars]");
                EvalJs("showPreview", data);
            }
            catch (Exception ex)
            {
                LogService.Error($"Preview failed: {path}", ex);
                EvalJs("previewError", new { path, message = ex.Message });
            }
        });
    }

    private void HandleIcons(JsonElement root)
    {
        var requests = new List<IconRequestItem>();

        if (root.TryGetProperty("requests", out var reqProp) && reqProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in reqProp.EnumerateArray())
            {
                var key = item.TryGetProperty("key", out var k) ? k.GetString() ?? "" : "";
                var path = item.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                var isFolder = item.TryGetProperty("isFolder", out var f) && f.GetBoolean();
                var ext = item.TryGetProperty("ext", out var e) ? e.GetString() ?? "" : "";
                requests.Add(new IconRequestItem { Key = key, Path = path, IsFolder = isFolder, Ext = ext });
            }
        }
        else if (root.TryGetProperty("paths", out var pathsProp) && pathsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in pathsProp.EnumerateArray())
            {
                var str = item.GetString();
                if (!string.IsNullOrEmpty(str))
                {
                    var ext = Path.GetExtension(str).TrimStart('.').ToLowerInvariant();
                    var isDir = string.IsNullOrEmpty(ext);
                    var key = (!isDir && (ext == "exe" || ext == "lnk" || ext == "ico")) ? str : (isDir ? "__folder__" : ext);
                    requests.Add(new IconRequestItem { Key = key, Path = str, IsFolder = isDir, Ext = ext });
                }
            }
        }

        if (requests.Count == 0) return;

        _ = Task.Run(() =>
        {
            var map = ShellIconService.GetIcons(requests);
            EvalJs("showIcons", map);
        });
    }

    private static readonly JsonSerializerOptions JsonOpts = new();

    public void EvalJs(string functionName, object payload)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                var envelope = new { type = functionName, data = payload };
                var envelopeJson = JsonSerializer.Serialize(envelope, JsonOpts);
                _webView.CoreWebView2?.PostWebMessageAsJson(envelopeJson);
            }
            catch { }
        });
    }
}
