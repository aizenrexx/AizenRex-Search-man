using AizenSearch.Core.Models;
using AizenSearch.Core.Indexing;

namespace AizenSearch.Core.Search;

public static class RelevanceRanker
{
    public static int Compare(FileEntry a, FileEntry b, ParsedQuery query, string sortKey, bool descending)
    {
        int cmp;

        if (sortKey.Equals("type", StringComparison.OrdinalIgnoreCase))
        {
            cmp = string.Compare(a.Ext, b.Ext, StringComparison.OrdinalIgnoreCase);
        }
        else if (sortKey.Equals("path", StringComparison.OrdinalIgnoreCase))
        {
            cmp = string.Compare(a.Dir, b.Dir, StringComparison.OrdinalIgnoreCase);
            if (cmp == 0) cmp = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }
        else // default: sort by relevance & name
        {
            cmp = CompareRelevance(a, b, query);
        }

        return descending ? -cmp : cmp;
    }

    private static int CompareRelevance(FileEntry a, FileEntry b, ParsedQuery query)
    {
        var needle = query.CleanNeedle;
        var searchText = query.SearchText;

        // 1. Source Priority: configured priority location > other connected locations > local disks
        var sourceA = GetSourcePriority(a);
        var sourceB = GetSourcePriority(b);
        if (sourceA != sourceB)
        {
            return sourceA.CompareTo(sourceB);
        }

        // 2. Full exact match check
        var exactA = a.Name.Equals(needle, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        var exactB = b.Name.Equals(needle, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        if (exactA != exactB) return exactA.CompareTo(exactB);

        // 3. Typed extension preference
        if (query.TypedSuffix.HasValue)
        {
            var targetExt = query.TypedSuffix.Value.Ext;
            var prefA = a.Ext.Equals(targetExt, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            var prefB = b.Ext.Equals(targetExt, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            if (prefA != prefB) return prefA.CompareTo(prefB);
        }

        // 4. Smart Multi-Term Sequence & Relational Scoring
        var relA = GetSmartRelationScore(a, query);
        var relB = GetSmartRelationScore(b, query);
        if (relA != relB) return relA.CompareTo(relB);

        // 5. Length comparison (shorter name first)
        if (a.Name.Length != b.Name.Length) return a.Name.Length.CompareTo(b.Name.Length);

        // 6. Alphabetical fallback
        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetSourcePriority(FileEntry entry)
    {
        var dir = entry.Dir;
        if (dir.Length > 0 && (dir[0] == 'R' || dir[0] == 'r'))
        {
            if (dir.StartsWith(ConnectedLibrarySeeder.ConnectedRoot, StringComparison.OrdinalIgnoreCase))
                return 0; // Highest priority for the configured library location.
            if (dir.StartsWith(@"R:\", StringComparison.OrdinalIgnoreCase))
                return 1; // High priority for other connected locations.
        }
        return 2; // Standard local disks
    }

    private static int GetSmartRelationScore(FileEntry entry, ParsedQuery query)
    {
        var searchText = query.SearchText;
        if (string.IsNullOrEmpty(searchText)) return 0;

        var name = entry.Name;
        // Exact name match
        if (name.Equals(searchText, StringComparison.OrdinalIgnoreCase)) return 0;

        // Contiguous starts with
        if (name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) return 1;

        // Contiguous substring in name
        if (name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) return 2;

        var terms = query.Terms;
        if (terms.Count > 1)
        {
            // Smart Multi-Token Gap Search (e.g. "superman 2006" in "Superman Returns (2006)")
            int lastIndex = -1;
            bool sequenceMatched = true;
            for (int i = 0; i < terms.Count; i++)
            {
                var idx = name.IndexOf(terms[i], lastIndex + 1, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    sequenceMatched = false;
                    break;
                }
                lastIndex = idx;
            }

            if (sequenceMatched)
            {
                // If the first term starts the name (e.g. "Superman ... 2006"), award top ranking
                if (name.StartsWith(terms[0], StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                return 4; // Sequential match with prefix gap
            }

            // All terms present in name in any order
            bool allInName = true;
            for (int i = 0; i < terms.Count; i++)
            {
                if (!name.Contains(terms[i], StringComparison.OrdinalIgnoreCase))
                {
                    allInName = false;
                    break;
                }
            }
            if (allInName) return 5;
        }

        // Matched only in parent directory path
        if (entry.Dir.Contains(searchText, StringComparison.OrdinalIgnoreCase)) return 6;

        return 7;
    }
}
