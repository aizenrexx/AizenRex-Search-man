using System.Collections.Concurrent;
using AizenSearch.Core.Models;
using AizenSearch.Core.Indexing;

namespace AizenSearch.Core.Search;

public static class SearchEngine
{
    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
    {
        "png", "jpg", "jpeg", "gif", "webp", "bmp", "svg", "ico", "tif", "tiff"
    };

    private static readonly HashSet<string> AudioExts = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "wav", "ogg", "flac", "m4a", "aac", "wma", "opus"
    };

    private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "m4v", "3gp", "ts"
    };

    private static readonly HashSet<string> DocExts = new(StringComparer.OrdinalIgnoreCase)
    {
        "doc", "docx", "pdf", "txt", "rtf", "odt", "xls", "xlsx", "ppt", "pptx", "csv", "md"
    };

    private static readonly HashSet<string> ExeExts = new(StringComparer.OrdinalIgnoreCase)
    {
        "exe", "msi", "bat", "cmd", "ps1", "lnk"
    };

    private static readonly HashSet<string> ArchiveExts = new(StringComparer.OrdinalIgnoreCase)
    {
        "zip", "rar", "7z", "tar", "gz", "bz2", "xz", "iso"
    };

    public static SearchResult Search(IReadOnlyList<FileEntry> index, SearchQuery query, ParsedQuery parsed)
    {
        var offset = Math.Max(0, query.Offset);
        var limit = Math.Clamp(query.Limit, 50, 5000);
        var neededCount = offset + limit;
        var totalCount = index.Count;

        // Fast path for empty search query (browsing all files)
        if (string.IsNullOrWhiteSpace(parsed.CleanNeedle) &&
            parsed.Excludes.Count == 0 &&
            parsed.EffectiveKind == "all" &&
            parsed.EffectiveExts.Count == 0 &&
            !parsed.MinSizeBytes.HasValue &&
            !parsed.MaxSizeBytes.HasValue &&
            !parsed.ModifiedAfter.HasValue &&
            !parsed.ModifiedBefore.HasValue &&
            query.Sort.Equals("name", StringComparison.OrdinalIgnoreCase) &&
            !query.Descending)
        {
            var start = Math.Min(offset, totalCount);
            var initialCount = Math.Min(limit, totalCount - start);
            var initialList = new List<FileEntry>(initialCount);
            for (var i = start; i < start + initialCount; i++)
            {
                initialList.Add(index[i]);
            }
            return new SearchResult { Items = initialList, TotalMatches = totalCount };
        }

        var matchCase = query.MatchCase;
        var matchPath = query.MatchPath;
        var wholeWord = query.WholeWord;
        var forcePath = parsed.ForcePath;
        var effectiveKind = parsed.EffectiveKind;
        var effectiveExt = parsed.EffectiveExt;
        var effectiveExts = parsed.EffectiveExts;
        var excludes = parsed.Excludes;
        var phrases = parsed.Phrases;
        var terms = parsed.Terms;
        var compiledRegex = parsed.CompiledRegex;
        var wildcardRegex = parsed.WildcardRegex;

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var searchPath = forcePath || matchPath;
        var isRelevanceSort = query.Sort.Equals("name", StringComparison.OrdinalIgnoreCase) && !query.Descending;

        var searchText = parsed.SearchText;
        var cleanNeedle = parsed.CleanNeedle;
        var hasSearchText = !string.IsNullOrEmpty(searchText);
        var typedSuffix = parsed.TypedSuffix;

        var arr = index as FileEntry[];
        var lst = arr == null ? index as List<FileEntry> : null;

        var singleChar = terms.Count == 1 && terms[0].Length == 1 ? terms[0][0] : '\0';
        var singleLower = singleChar != '\0' ? char.ToLowerInvariant(singleChar) : '\0';
        var singleUpper = singleChar != '\0' ? char.ToUpperInvariant(singleChar) : '\0';

        // Thread-local collectors: partition work across CPU cores with zero lock contention
        var localResults = new ConcurrentBag<(int count, List<FileEntry> t0, List<FileEntry> t1, List<FileEntry> t2, List<FileEntry> t3, List<FileEntry>? general)>();

        var chunkSize = Math.Max(16384, (totalCount + Environment.ProcessorCount - 1) / Environment.ProcessorCount);
        var partitioner = Partitioner.Create(0, totalCount, chunkSize);
        Parallel.ForEach(partitioner, range =>
        {
            var localMatchCount = 0;
            var t0 = new List<FileEntry>();
            var t1 = new List<FileEntry>();
            var t2 = new List<FileEntry>();
            var t3 = new List<FileEntry>();
            var general = isRelevanceSort ? null : new List<FileEntry>();

            for (var i = range.Item1; i < range.Item2; i++)
            {
                var e = arr != null ? arr[i] : lst != null ? lst[i] : index[i];

                // 1. Kind filter
                if (effectiveKind == "files" && e.IsFolder) continue;
                if (effectiveKind == "folders" && !e.IsFolder) continue;
                if (effectiveKind == "image" && (e.IsFolder || !ImageExts.Contains(e.Ext))) continue;
                if (effectiveKind == "audio" && (e.IsFolder || !AudioExts.Contains(e.Ext))) continue;
                if (effectiveKind == "video" && (e.IsFolder || !VideoExts.Contains(e.Ext))) continue;
                if (effectiveKind == "doc" && (e.IsFolder || !DocExts.Contains(e.Ext))) continue;
                if (effectiveKind == "exe" && (e.IsFolder || !ExeExts.Contains(e.Ext))) continue;
                if (effectiveKind == "archive" && (e.IsFolder || !ArchiveExts.Contains(e.Ext))) continue;

                // 2. Extension filter (supports pipe: ext:mkv|mp4)
                if (effectiveExts.Count > 0)
                {
                    if (e.IsFolder || !effectiveExts.Contains(e.Ext, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
                else if (!string.IsNullOrEmpty(effectiveExt) && (e.IsFolder || !e.Ext.Equals(effectiveExt, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                // 2b. Size filter (SizeBytes==0 means unknown — skip filter for those)
                if (e.IsFolder) { /* skip size checks for folders */ }
                else
                {
                    if (parsed.MinSizeBytes.HasValue && e.SizeBytes > 0 && e.SizeBytes < parsed.MinSizeBytes.Value) continue;
                    if (parsed.MaxSizeBytes.HasValue && e.SizeBytes > 0 && e.SizeBytes > parsed.MaxSizeBytes.Value) continue;
                }

                // 2c. Date filter (ModifiedUtc==default means unknown — skip filter for those)
                if (parsed.ModifiedAfter.HasValue && e.ModifiedUtc != default && e.ModifiedUtc < parsed.ModifiedAfter.Value.ToUniversalTime()) continue;
                if (parsed.ModifiedBefore.HasValue && e.ModifiedUtc != default && e.ModifiedUtc > parsed.ModifiedBefore.Value.ToUniversalTime()) continue;

                // 3. Regex
                if (compiledRegex != null)
                {
                    var target = searchPath ? $"{e.Name} {e.Dir}" : e.Name;
                    if (!compiledRegex.IsMatch(target)) continue;
                }

                // 4. Wildcard (* and ?)
                if (wildcardRegex != null)
                {
                    if (!wildcardRegex.IsMatch(e.Name)) continue;
                }

                // 5. Excludes (!)
                var excluded = false;
                for (var x = 0; x < excludes.Count; x++)
                {
                    var ex = excludes[x];
                    if (e.Name.Contains(ex, comparison) || (searchPath && e.Dir.Contains(ex, comparison)))
                    {
                        excluded = true;
                        break;
                    }
                }
                if (excluded) continue;

                // 6. Phrases ("...")
                var phrasesFailed = false;
                for (var p = 0; p < phrases.Count; p++)
                {
                    var ph = phrases[p];
                    if (!e.Name.Contains(ph, comparison) && (!searchPath || !e.Dir.Contains(ph, comparison)))
                    {
                        phrasesFailed = true;
                        break;
                    }
                }
                if (phrasesFailed) continue;

                // 7. Whole word
                if (wholeWord)
                {
                    var raw = searchPath ? $"{e.Name} {e.Dir}" : e.Name;
                    var parts = raw.Split((char[])[' ', '\t', '\r', '\n', '.', '_', '-', '\\', ':', ';', ',', '(', ')', '[', ']', '{', '}'], StringSplitOptions.RemoveEmptyEntries);
                    var wwFailed = false;
                    for (var t = 0; t < terms.Count; t++)
                    {
                        var term = terms[t];
                        var found = false;
                        for (var j = 0; j < parts.Length; j++)
                        {
                            if (parts[j].Equals(term, comparison))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found) { wwFailed = true; break; }
                    }
                    if (wwFailed) continue;
                }

                // 8. Term presence & name vs path matching
                var termsFailed = false;
                var allInName = true;
                for (var t = 0; t < terms.Count; t++)
                {
                    var term = terms[t];
                    bool inName;
                    if (singleChar != '\0')
                    {
                        inName = e.Name.IndexOf(singleLower) >= 0 || (singleLower != singleUpper && e.Name.IndexOf(singleUpper) >= 0);
                    }
                    else
                    {
                        inName = e.Name.Contains(term, comparison);
                    }

                    if (inName)
                    {
                        // Matched in file/folder name
                    }
                    else if (searchPath && (forcePath || term.Length >= 3) && e.Dir.Contains(term, comparison))
                    {
                        // Matched in parent directory path
                        allInName = false;
                    }
                    else
                    {
                        termsFailed = true;
                        break;
                    }
                }
                if (termsFailed) continue;

                // Match confirmed
                localMatchCount++;

                if (isRelevanceSort)
                {
                    // If candidate tiers are already saturated, skip ranking overhead
                    if ((t0.Count + t1.Count + t2.Count) >= neededCount && (!searchPath || t3.Count >= neededCount))
                    {
                        continue;
                    }

                    // Categorize into bounded tiers with configured source and multi-term gap priority
                    var isConfiguredLibrary = e.Dir.Length >= 13 && (e.Dir[0] == 'R' || e.Dir[0] == 'r') && e.Dir.StartsWith(ConnectedLibrarySeeder.ConnectedRoot, StringComparison.OrdinalIgnoreCase);

                    if (allInName && hasSearchText)
                    {
                        if (e.Name.Equals(cleanNeedle, StringComparison.OrdinalIgnoreCase))
                        {
                            if (t0.Count < neededCount) t0.Add(e);
                        }
                        else if (isConfiguredLibrary)
                        {
                            // Top priority tier for configured library matches
                            if (t1.Count < neededCount) t1.Add(e);
                        }
                        else if (typedSuffix.HasValue && e.Ext.Equals(typedSuffix.Value.Ext, StringComparison.OrdinalIgnoreCase))
                        {
                            if (t1.Count < neededCount) t1.Add(e);
                        }
                        else if (e.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                        {
                            if (t1.Count < neededCount) t1.Add(e);
                        }
                        else if (terms.Count > 1 && e.Name.StartsWith(terms[0], StringComparison.OrdinalIgnoreCase))
                        {
                            // Multi-token gap match (e.g. "superman" ... "2006")
                            if (t1.Count < neededCount) t1.Add(e);
                        }
                        else
                        {
                            if (t2.Count < neededCount) t2.Add(e);
                        }
                    }
                    else if (allInName)
                    {
                        if (isConfiguredLibrary && t1.Count < neededCount) t1.Add(e);
                        else if (t2.Count < neededCount) t2.Add(e);
                    }
                    else
                    {
                        if (isConfiguredLibrary && t2.Count < neededCount) t2.Add(e);
                        else if (t3.Count < neededCount) t3.Add(e);
                    }
                }
                else
                {
                    if (general!.Count < neededCount * 2)
                    {
                        general.Add(e);
                    }
                }
            }

            localResults.Add((localMatchCount, t0, t1, t2, t3, general));
        });

        // Merge thread-local results
        var totalMatches = 0;
        var combinedT0 = new List<FileEntry>();
        var combinedT1 = new List<FileEntry>();
        var combinedT2 = new List<FileEntry>();
        var combinedT3 = new List<FileEntry>();
        var combinedGeneral = isRelevanceSort ? null : new List<FileEntry>();

        foreach (var local in localResults)
        {
            totalMatches += local.count;
            if (isRelevanceSort)
            {
                combinedT0.AddRange(local.t0);
                combinedT1.AddRange(local.t1);
                combinedT2.AddRange(local.t2);
                combinedT3.AddRange(local.t3);
            }
            else if (local.general != null)
            {
                combinedGeneral!.AddRange(local.general);
            }
        }

        List<FileEntry> orderedList;

        if (isRelevanceSort)
        {
            orderedList = new List<FileEntry>(neededCount);

            void ProcessTier(List<FileEntry> source)
            {
                var remaining = neededCount - orderedList.Count;
                if (remaining <= 0 || source.Count == 0) return;
                source.Sort((a, b) => RelevanceRanker.Compare(a, b, parsed, query.Sort, query.Descending));
                var take = Math.Min(remaining, source.Count);
                for (var k = 0; k < take; k++)
                {
                    orderedList.Add(source[k]);
                }
            }

            ProcessTier(combinedT0);
            ProcessTier(combinedT1);
            ProcessTier(combinedT2);
            ProcessTier(combinedT3);
        }
        else
        {
            combinedGeneral!.Sort((a, b) => RelevanceRanker.Compare(a, b, parsed, query.Sort, query.Descending));
            orderedList = combinedGeneral;
        }

        // Slice requested offset and limit
        var startIdx = Math.Min(offset, orderedList.Count);
        var takeCount = Math.Min(limit, orderedList.Count - startIdx);
        var pagedResults = new List<FileEntry>(takeCount);
        for (var i = startIdx; i < startIdx + takeCount; i++)
        {
            pagedResults.Add(orderedList[i]);
        }

        return new SearchResult
        {
            Items = pagedResults,
            TotalMatches = totalMatches
        };
    }

    public static List<FileEntry> Filter(IReadOnlyList<FileEntry> index, SearchQuery query, ParsedQuery parsed)
    {
        return Search(index, query, parsed).Items;
    }
}
