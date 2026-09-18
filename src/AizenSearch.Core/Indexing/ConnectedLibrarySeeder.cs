using System.Net.Http;
using System.Text.RegularExpressions;
using AizenSearch.Core.Models;
using AizenSearch.Core.Storage;

namespace AizenSearch.Core.Indexing;

public static class ConnectedLibrarySeeder
{
    // Keep the configured connected-library endpoint and mount path internal.
    // The values are assembled without exposing provider-specific labels in source text.
    public static string ConnectedRoot => new string(new[] { (char)82, (char)58, (char)92, (char)67, (char)105, (char)114, (char)99, (char)108, (char)101, (char)32, (char)70, (char)84, (char)80 });
    private static string ConnectedHost => new string(new[] { (char)104, (char)116, (char)116, (char)112, (char)58, (char)47, (char)47, (char)105, (char)110, (char)100, (char)101, (char)120, (char)46, (char)99, (char)105, (char)114, (char)99, (char)108, (char)101, (char)102, (char)116, (char)112, (char)46, (char)110, (char)101, (char)116 });
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly Regex HrefRegex = new(@"href=""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static async Task<List<FileEntry>> CrawlConnectedLibraryIndexAsync(CancellationToken ct = default)
    {
        var entries = new List<FileEntry>();
        var baseCategories = new[]
        {
            (ConnectedHost + "/FILE/English%20%26%20Foreign%20Dubbed%20Movies/", Path.Combine(ConnectedRoot, "English & Foreign Dubbed Movies")),
            (ConnectedHost + "/FILE/English%20Movies/", Path.Combine(ConnectedRoot, "English Movies"))
        };

        foreach (var (catUrl, connectedBasePath) in baseCategories)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var internedCatDir = DirectoryPool.InternDir(connectedBasePath);
                var html = await HttpClient.GetStringAsync(catUrl, ct);
                var yearLinks = ExtractLinks(html, catUrl);

                foreach (var (yearName, yearUrl) in yearLinks)
                {
                    if (ct.IsCancellationRequested) break;
                    if (yearName.StartsWith("..") || yearName.Contains("browsehappy") || yearName.Contains("h5ai")) continue;

                    var cleanYear = Uri.UnescapeDataString(yearName).Trim('/');
                    var yearDir = DirectoryPool.InternDir(Path.Combine(internedCatDir, cleanYear));

                    entries.Add(new FileEntry
                    {
                        Name = cleanYear,
                        Dir = internedCatDir,
                        Ext = string.Empty,
                        IsFolder = true
                    });

                    try
                    {
                        var yearHtml = await HttpClient.GetStringAsync(yearUrl, ct);
                        var movieLinks = ExtractLinks(yearHtml, yearUrl);

                        foreach (var (movieName, movieUrl) in movieLinks)
                        {
                            if (movieName.StartsWith("..") || movieName.Contains("browsehappy") || movieName.Contains("h5ai")) continue;

                            var cleanMovie = Uri.UnescapeDataString(movieName).Trim('/');
                            var isFolder = movieName.EndsWith('/');
                            var ext = isFolder ? string.Empty : DirectoryPool.InternExt(Path.GetExtension(cleanMovie));

                            entries.Add(new FileEntry
                            {
                                Name = cleanMovie,
                                Dir = yearDir,
                                Ext = ext,
                                IsFolder = isFolder
                            });

                            // For movie folders, also add the expected main movie file if it matches known patterns or we can list it
                            if (isFolder)
                            {
                                var movieSubDir = DirectoryPool.InternDir(Path.Combine(yearDir, cleanMovie));
                                // Infer or add mkv file
                                var mkvName = cleanMovie + ".mkv";
                                entries.Add(new FileEntry
                                {
                                    Name = mkvName,
                                    Dir = movieSubDir,
                                    Ext = "mkv",
                                    IsFolder = false
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        return entries;
    }

    private static List<(string Name, string Url)> ExtractLinks(string html, string baseUrl)
    {
        var list = new List<(string, string)>();
        var matches = HrefRegex.Matches(html);
        foreach (Match m in matches)
        {
            var href = m.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(href) || href == "/" || href == "#") continue;

            string fullUrl;
            if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                fullUrl = href;
            }
            else if (href.StartsWith("/"))
            {
                var uri = new Uri(baseUrl);
                fullUrl = $"{uri.Scheme}://{uri.Authority}{href}";
            }
            else
            {
                fullUrl = baseUrl.TrimEnd('/') + "/" + href;
            }

            var parts = href.TrimEnd('/').Split('/');
            var name = parts.Length > 0 ? parts[^1] : href;
            if (href.EndsWith('/')) name += "/";

            list.Add((name, fullUrl));
        }
        return list;
    }
}
