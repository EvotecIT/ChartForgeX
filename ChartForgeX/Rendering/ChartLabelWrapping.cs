using System;
using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal static class ChartLabelWrapping {
    /// <summary>Preserves complete words within the available rows, retaining existing balanced one/two-row layouts.</summary>
    internal static bool TryComplete(string label, double fontSize, double maxWidth, int maximumLines,
        Func<string, double, double> measure, out string[] lines) {
        var result = new List<string>();
        var paragraphs = label.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (var paragraph in paragraphs) {
            if (measure(paragraph, fontSize) <= maxWidth) result.Add(paragraph);
            else {
                var balanced = BalancedTwoLine(paragraph, fontSize, maxWidth, measure);
                if (balanced.Length == 2 && measure(balanced[0], fontSize) <= maxWidth && measure(balanced[1], fontSize) <= maxWidth)
                    result.AddRange(balanced);
                else {
                    var words = paragraph.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string row = string.Empty;
                    foreach (var word in words) {
                        if (measure(word, fontSize) > maxWidth) { lines = Array.Empty<string>(); return false; }
                        var candidate = row.Length == 0 ? word : row + " " + word;
                        if (row.Length > 0 && measure(candidate, fontSize) > maxWidth) {
                            result.Add(row); row = word;
                        } else row = candidate;
                        if (result.Count >= maximumLines) { lines = Array.Empty<string>(); return false; }
                    }
                    result.Add(row);
                }
            }
            if (result.Count > maximumLines) { lines = Array.Empty<string>(); return false; }
        }
        lines = result.ToArray();
        return lines.Length > 0;
    }

    public static string[] BalancedTwoLine(string label, double fontSize, double maxWidth, Func<string, double, double> measure) {
        if (measure(label, fontSize) <= maxWidth || label.IndexOf(' ') < 0) return new[] { label };
        var words = label.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return new[] { label };
        var bestSplit = 1;
        var bestScore = double.PositiveInfinity;
        for (var split = 1; split < words.Length; split++) {
            var first = string.Join(" ", words, 0, split);
            var second = string.Join(" ", words, split, words.Length - split);
            var score = Math.Max(measure(first, fontSize), measure(second, fontSize)) + Math.Abs(first.Length - second.Length) * 0.25;
            if (score >= bestScore) continue;
            bestScore = score;
            bestSplit = split;
        }

        return new[] { string.Join(" ", words, 0, bestSplit), string.Join(" ", words, bestSplit, words.Length - bestSplit) };
    }
}
