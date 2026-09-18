using System.Text.RegularExpressions;
using AizenSearch.Core.Models;

namespace AizenSearch.Core.Search;

public sealed class ParsedQuery
{
    public List<string> Positives { get; } = [];
    public List<string> Phrases { get; } = [];
    public List<string> Excludes { get; } = [];
    public List<string> Terms { get; } = [];
    public List<string> EffectiveExts { get; } = [];
    public string EffectiveExt { get; set; } = string.Empty;
    public string EffectiveKind { get; set; } = "all";
    public bool ForcePath { get; set; }
    public string CleanNeedle { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public (string Base, string Ext)? TypedSuffix { get; set; }
    public Regex? WildcardRegex { get; set; }
    public Regex? CompiledRegex { get; set; }

    // Feature: size & date filters parsed from query (e.g. size:>1gb, date:after 2024-01-01)
    public long? MinSizeBytes { get; set; }
    public long? MaxSizeBytes { get; set; }
    public DateTime? ModifiedAfter { get; set; }
    public DateTime? ModifiedBefore { get; set; }
}

public static partial class QueryParser
{
    [GeneratedRegex("\"([^\"]+)\"|(\\S+)", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    public static ParsedQuery Parse(SearchQuery query)
    {
        var result = new ParsedQuery
        {
            EffectiveKind = query.Kind,
            EffectiveExt = query.Ext.TrimStart('.')
        };
        if (!string.IsNullOrEmpty(result.EffectiveExt))
        {
            foreach (var part in result.EffectiveExt.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                result.EffectiveExts.Add(part.TrimStart('.').ToLowerInvariant());
            }
        }

        var raw = query.RawQuery.Trim();
        var matches = TokenRegex().Matches(raw);

        foreach (Match match in matches)
        {
            var isQuoted = match.Groups[1].Success;
            var token = isQuoted ? match.Groups[1].Value : match.Groups[2].Value;
            var processed = query.MatchCase ? token : token.ToLowerInvariant();

            if (isQuoted)
            {
                result.Phrases.Add(processed);
            }

            if (processed.StartsWith("ext:", StringComparison.OrdinalIgnoreCase))
            {
                var extVal = processed[4..];
                result.EffectiveExts.Clear();
                foreach (var part in extVal.Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    result.EffectiveExts.Add(part.TrimStart('.').ToLowerInvariant());
                }
                result.EffectiveExt = string.Join('|', result.EffectiveExts);
                continue;
            }

            if (processed.StartsWith("type:", StringComparison.OrdinalIgnoreCase))
            {
                var typeVal = processed[5..];
                result.EffectiveKind = typeVal switch
                {
                    "file" or "files" => "files",
                    "folder" or "folders" or "dir" or "directory" => "folders",
                    "exe" or "app" or "apps" or "application" or "applications" => "exe",
                    "doc" or "docs" or "document" or "documents" => "doc",
                    "pic" or "pics" or "picture" or "pictures" or "image" or "images" or "photo" or "photos" => "image",
                    "audio" or "music" or "sound" => "audio",
                    "video" or "videos" or "movie" or "movies" => "video",
                    "zip" or "archive" or "archives" or "compressed" => "archive",
                    _ => query.Kind
                };
                continue;
            }

            if (processed.StartsWith("path:", StringComparison.OrdinalIgnoreCase))
            {
                result.ForcePath = true;
                var pathVal = processed[5..];
                if (!string.IsNullOrEmpty(pathVal))
                {
                    result.Positives.Add(pathVal);
                }
                continue;
            }

            // Feature: size:>500mb / size:<1gb / size:2gb-4gb
            if (processed.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
            {
                var sizeVal = processed[5..].Trim();
                ParseSizeFilter(sizeVal, result);
                continue;
            }

            // Feature: date:after 2024-01-01 / date:before 2024-06-01 / date:2024
            if (processed.StartsWith("date:", StringComparison.OrdinalIgnoreCase))
            {
                var dateVal = processed[5..].Trim();
                ParseDateFilter(dateVal, result);
                continue;
            }

            if (processed.StartsWith('!') && processed.Length > 1)
            {
                result.Excludes.Add(processed[1..]);
                continue;
            }

            result.Positives.Add(processed);
        }

        // Fall back to query-level filters (UI filters override or merge)
        if (query.MinSizeBytes.HasValue && !result.MinSizeBytes.HasValue)
            result.MinSizeBytes = query.MinSizeBytes;
        if (query.MaxSizeBytes.HasValue && !result.MaxSizeBytes.HasValue)
            result.MaxSizeBytes = query.MaxSizeBytes;
        if (query.ModifiedAfter.HasValue && !result.ModifiedAfter.HasValue)
            result.ModifiedAfter = query.ModifiedAfter;
        if (query.ModifiedBefore.HasValue && !result.ModifiedBefore.HasValue)
            result.ModifiedBefore = query.ModifiedBefore;

        result.CleanNeedle = string.Join(' ', result.Positives);

        // Detect typed suffix like "cherax.exe"
        var lastDot = result.CleanNeedle.LastIndexOf('.');
        if (lastDot > 0 && lastDot < result.CleanNeedle.Length - 1)
        {
            var basePart = result.CleanNeedle[..lastDot];
            var sufPart = result.CleanNeedle[(lastDot + 1)..];
            if (sufPart.Length <= 10 && sufPart.All(char.IsLetterOrDigit))
            {
                result.TypedSuffix = (basePart, sufPart);
            }
        }

        result.SearchText = result.TypedSuffix?.Base ?? result.CleanNeedle;

        // Split search terms (Unicode-aware: keep non-ASCII letters intact)
        var splitChars = new[] { ' ', '\t', '\r', '\n', '_', '-', '\\', ':' };
        var terms = result.SearchText.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
        foreach (var t in terms)
        {
            if (t.Length > 0)
            {
                result.Terms.Add(t);
            }
        }

        var regexOpts = query.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;

        if (query.Regex && !string.IsNullOrWhiteSpace(result.CleanNeedle))
        {
            try
            {
                result.CompiledRegex = new Regex(result.CleanNeedle, regexOpts | RegexOptions.Compiled);
            }
            catch
            {
                result.CompiledRegex = null;
            }
        }
        else if (!query.Regex && (result.CleanNeedle.Contains('*') || result.CleanNeedle.Contains('?')))
        {
            var pattern = "^" + Regex.Escape(result.CleanNeedle).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            try
            {
                result.WildcardRegex = new Regex(pattern, regexOpts | RegexOptions.Compiled);
            }
            catch
            {
                result.WildcardRegex = null;
            }
        }

        return result;
    }

    private static void ParseSizeFilter(string value, ParsedQuery result)
    {
        static long? ParseBytes(string s)
        {
            s = s.Trim().ToLowerInvariant();
            double mult = 1;
            if (s.EndsWith("tb")) { mult = 1024L * 1024 * 1024 * 1024; s = s[..^2]; }
            else if (s.EndsWith("gb")) { mult = 1024L * 1024 * 1024; s = s[..^2]; }
            else if (s.EndsWith("mb")) { mult = 1024L * 1024; s = s[..^2]; }
            else if (s.EndsWith("kb")) { mult = 1024L; s = s[..^2]; }
            else if (s.EndsWith('b')) { s = s[..^1]; }
            if (double.TryParse(s.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                return (long)(num * mult);
            }
            return null;
        }

        if (value.StartsWith('>'))
        {
            result.MinSizeBytes = ParseBytes(value[1..]);
        }
        else if (value.StartsWith('<'))
        {
            result.MaxSizeBytes = ParseBytes(value[1..]);
        }
        else if (value.Contains('-'))
        {
            var parts = value.Split('-', 2);
            result.MinSizeBytes = ParseBytes(parts[0]);
            result.MaxSizeBytes = ParseBytes(parts[1]);
        }
        else
        {
            var b = ParseBytes(value);
            if (b.HasValue)
            {
                result.MinSizeBytes = b;
                result.MaxSizeBytes = b;
            }
        }
    }

    private static void ParseDateFilter(string value, ParsedQuery result)
    {
        var v = value.Trim().ToLowerInvariant();

        if (v.StartsWith("after "))
        {
            result.ModifiedAfter = TryParseDate(v[6..].Trim());
        }
        else if (v.StartsWith("before "))
        {
            result.ModifiedBefore = TryParseDate(v[7..].Trim());
        }
        else if (v.StartsWith("between "))
        {
            var rest = v[8..].Trim();
            var parts = rest.Split(" and ", 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                result.ModifiedAfter = TryParseDate(parts[0]);
                result.ModifiedBefore = TryParseDate(parts[1]);
            }
        }
        else
        {
            // Plain year or date: exact match window (that year / that day)
            var d = TryParseDate(v);
            if (d.HasValue)
            {
                result.ModifiedAfter = d.Value.Date;
                result.ModifiedBefore = d.Value.Date.AddDays(1).AddTicks(-1);
            }
        }
    }

    private static DateTime? TryParseDate(string s)
    {
        s = s.Trim().Trim('"');
        if (DateTime.TryParse(s, out var dt)) return dt;
        if (int.TryParse(s, out var year) && year is >= 1900 and <= 2100)
        {
            return new DateTime(year, 1, 1);
        }
        return null;
    }
}
