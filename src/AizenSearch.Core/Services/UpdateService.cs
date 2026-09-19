using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AizenSearch.Core.Services;

public static class UpdateService
{
    public const string CurrentVersion = "0.3.9";
    public const string RepoOwner = "aizenrexx";
    public const string RepoName = "AizenRex-Search-man";

    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static UpdateService()
    {
        Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AizenRexSearchMan", CurrentVersion));
    }

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            using var response = await Client.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = CurrentVersion,
                    UpdateAvailable = false,
                    Message = $"Update server returned {(int)response.StatusCode} (offline or private repo)."
                };
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() ?? "" : "";
            var latestVerStr = tagName.TrimStart('v', 'V');
            var body = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            var htmlUrl = root.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "";

            // Prefer the first Windows installer asset as the direct download URL.
            var downloadUrl = htmlUrl;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var nameProp))
                    {
                        var assetName = nameProp.GetString() ?? "";
                        if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                            assetName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
                            assetName.Contains("installer", StringComparison.OrdinalIgnoreCase))
                        {
                            if (asset.TryGetProperty("browser_download_url", out var dUrl))
                            {
                                downloadUrl = dUrl.GetString() ?? htmlUrl;
                            }
                            break;
                        }
                    }
                }
            }

            var updateAvailable = false;
            if (Version.TryParse(latestVerStr, out var latestVer) &&
                Version.TryParse(CurrentVersion, out var currVer))
            {
                updateAvailable = latestVer > currVer;
            }

            return new UpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                LatestVersion = latestVerStr,
                UpdateAvailable = updateAvailable,
                ReleaseNotes = body,
                DownloadUrl = downloadUrl,
                Message = updateAvailable ? $"New version v{latestVerStr} is available!" : "AizenRex Search-man is up to date."
            };
        }
        catch
        {
            return new UpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                UpdateAvailable = false,
                Message = "Unable to connect to GitHub releases."
            };
        }
    }
}

public sealed class UpdateCheckResult
{
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public bool UpdateAvailable { get; set; }
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Message { get; set; } = "";
}

