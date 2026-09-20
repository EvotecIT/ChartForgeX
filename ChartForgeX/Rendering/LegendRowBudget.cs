using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class LegendRowBudget {
    internal static List<T> Apply<T>(List<T> rows, Chart chart, Func<T, int> count, Func<int, T> summary, double? availableHeight = null) {
        var maximumRows = MaximumRows(chart, availableHeight);
        if (rows.Count <= maximumRows) return rows;
        var omitted = 0;
        for (var i = maximumRows - 1; i < rows.Count; i++) omitted += count(rows[i]);
        rows.RemoveRange(maximumRows - 1, rows.Count - maximumRows + 1);
        rows.Add(summary(omitted));
        return rows;
    }

    internal static int MaximumRows(Chart chart, double? availableHeight = null) {
        var height = chart.Options.Size.Height * chart.Options.LegendMaximumHeightFraction;
        if (availableHeight.HasValue) height = Math.Min(height, Math.Max(0, availableHeight.Value));
        var fontSize = chart.Options.LegendStyle?.FontSize ?? chart.Options.Theme.LegendFontSize;
        var rowHeight = Math.Max(20, fontSize * 1.1 + 6);
        var maximumRows = Math.Max(1, (int)Math.Floor(Math.Max(0, height - 36) / rowHeight));
        if (chart.Options.LegendMaximumRows.HasValue) maximumRows = Math.Min(maximumRows, chart.Options.LegendMaximumRows.Value);
        return maximumRows;
    }

    internal static double HorizontalItemWidth(string text, double fontSize, double availableWidth, double overhead) {
        var estimated = 0.0;
        foreach (var character in text) {
            if (char.IsWhiteSpace(character)) estimated += fontSize * 0.34;
            else if (character == 'i' || character == 'l' || character == 'I') estimated += fontSize * (character == 'I' ? 0.75 : 0.3);
            else if (character == 'W' || character == 'M' || character == 'w' || character == 'm') estimated += fontSize * 0.9;
            else estimated += fontSize * 0.58;
        }
        return Math.Min(Math.Max(64, availableWidth), Math.Max(64, overhead + estimated));
    }

    internal static int VisibleVerticalEntryCount(Chart chart, int entryCount, double availableHeight) {
        var maximumRows = MaximumRows(chart, availableHeight);
        return entryCount <= maximumRows ? entryCount : Math.Max(0, maximumRows - 1);
    }

    internal static string Summary(int omitted) => "+ " + omitted.ToString(CultureInfo.InvariantCulture) + " more entries";
}
