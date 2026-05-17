using System;

namespace ChartForgeX.Rendering;

internal static class ChartLabelWrapping {
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
