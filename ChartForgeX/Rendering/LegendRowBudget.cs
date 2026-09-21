using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static class LegendRowBudget {
    private const double PortableLineHeightEm = 1.2;

    internal static List<T> Apply<T>(List<T> rows, Chart chart, Func<T, int> count, Func<int, T> summary, double? availableHeight = null) {
        var maximumRows = MaximumRows(chart, availableHeight);
        if (maximumRows <= 0) {
            rows.Clear();
            return rows;
        }
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
        var rowHeight = RowHeight(chart);
        if (height < rowHeight) return 0;
        var fixedSpacing = IsHorizontal(chart.Options.LegendPosition)
            ? Math.Min(18 + ChartVisualPrimitives.LegendPlotGap, Math.Max(0, height - rowHeight))
            : Math.Min(18, Math.Max(0, height - rowHeight));
        var maximumRows = Math.Max(1, (int)Math.Floor(Math.Max(0, height - fixedSpacing) / rowHeight));
        if (chart.Options.LegendMaximumRows.HasValue) maximumRows = Math.Min(maximumRows, chart.Options.LegendMaximumRows.Value);
        return maximumRows;
    }

    internal static double RowHeight(Chart chart) {
        return RowHeight(chart, MeasureLegendTextHeight(chart));
    }

    internal static double RowHeight(Chart chart, double measuredTextHeight) => Math.Max(20, Math.Max(0, measuredTextHeight) + 6);

    internal static double HorizontalReserve(Chart chart, int rowCount, double? availableHeight = null) {
        if (rowCount <= 0) return 0;
        var maximumHeight = chart.Options.Size.Height * chart.Options.LegendMaximumHeightFraction;
        if (availableHeight.HasValue) maximumHeight = Math.Min(maximumHeight, Math.Max(0, availableHeight.Value));
        var rowHeight = RowHeight(chart);
        var fixedSpacing = Math.Min(18 + ChartVisualPrimitives.LegendPlotGap, Math.Max(0, maximumHeight - rowHeight));
        return Math.Min(maximumHeight, fixedSpacing + rowCount * rowHeight);
    }

    internal static double HorizontalAvailableHeight(Chart chart) {
        var top = chart.Options.ShowHeader ? 98 : 44;
        return Math.Max(0, chart.Options.Size.Height - top - 4);
    }

    internal static double HorizontalItemWidth(string text, double fontSize, double availableWidth, double overhead) {
        var estimated = 0.0;
        foreach (var character in text) {
            if (char.IsWhiteSpace(character)) estimated += fontSize * 0.34;
            else if (character == 'i' || character == 'l' || character == 'I') estimated += fontSize * (character == 'I' ? 0.75 : 0.3);
            else if (character == 'W' || character == 'M' || character == 'w' || character == 'm') estimated += fontSize * 0.9;
            else estimated += fontSize * 0.58;
        }
        return Math.Min(Math.Max(0, availableWidth), Math.Max(64, overhead + estimated));
    }

    internal static int VisibleVerticalEntryCount(Chart chart, int entryCount, double availableHeight) {
        var maximumRows = MaximumRows(chart, availableHeight);
        return entryCount <= maximumRows ? entryCount : Math.Max(0, maximumRows - 1);
    }

    internal static string Summary(int omitted) => "+ " + omitted.ToString(CultureInfo.InvariantCulture) + " more entries";

    private static double MeasureLegendTextHeight(Chart chart) {
        var style = chart.Options.LegendStyle;
        var fontSize = style?.FontSize ?? chart.Options.Theme.LegendFontSize;
        if (style?.Baseline is TextBaseline.Superscript or TextBaseline.Subscript) fontSize *= 0.65;
        var height = fontSize * PortableLineHeightEm;
        var underline = style?.UnderlineStyle ?? (style?.Underline == true ? TextDecorationStyle.Single : TextDecorationStyle.None);
        if (underline != TextDecorationStyle.None) {
            var thickness = Math.Max(1, fontSize / 13.0);
            height = Math.Max(height, fontSize + 2 + TextDecorationMetrics.OuterExtent(underline, thickness));
        }
        if (style?.Baseline == TextBaseline.Superscript) height += fontSize * 0.35;
        else if (style?.Baseline == TextBaseline.Subscript) height += fontSize * 0.22;
        return height;
    }

    private static bool IsHorizontal(ChartLegendPosition position) =>
        position != ChartLegendPosition.Left && position != ChartLegendPosition.Right;
}
