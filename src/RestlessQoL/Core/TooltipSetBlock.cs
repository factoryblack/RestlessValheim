using System;
using System.Collections.Generic;
using System.Text;

namespace RestlessQoL.Core;

// Separate the native set span before the generic tooltip parser sorts rows and
// damage chips. Match the complete header/body; never remove matching stats globally.
internal static class TooltipSetBlock
{
    internal static bool TrySplit(string raw, string header, string body, out string itemCopy)
    {
        itemCopy = raw;
        if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(body)) return false;
        var lines = raw.Replace("\r", "").Split('\n');
        var expected = new List<string>();
        foreach (var line in body.Replace("\r", "").Split('\n'))
            if (!string.IsNullOrWhiteSpace(line)) expected.Add(Key(line));
        if (expected.Count == 0) return false;
        var heading = Key(header);
        for (var start = 0; start < lines.Length; start++)
        {
            if (Key(lines[start]) != heading) continue;
            var end = start + 1;
            var matched = 0;
            while (end < lines.Length && matched < expected.Count)
            {
                var key = Key(lines[end++]);
                if (key.Length == 0) continue;
                if (key != expected[matched]) break;
                matched++;
            }
            if (matched != expected.Count) continue;
            var kept = new List<string>();
            for (var i = 0; i < lines.Length; i++)
                if (i < start || i >= end) kept.Add(lines[i]);
            itemCopy = string.Join("\n", kept);
            return true;
        }
        return false;
    }

    private static string Key(string copy)
    {
        var key = new StringBuilder(copy.Length);
        foreach (var c in copy)
            if (!char.IsWhiteSpace(c)) key.Append(c);
        return key.ToString();
    }
}
