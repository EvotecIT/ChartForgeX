using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Resolves state keys through <see cref="ChartOptions.StateCategories"/> and lays out the shared categorical legend
/// used by state timelines and categorical heatmaps, so both families and both renderers use one vocabulary.
/// </summary>
internal sealed class ChartStateCategoryLegend {
    public const double RowHeight = 20;
    public const double Swatch = 10;
    public const double SwatchRadius = 1.5;
    public const double LabelGap = 6;
    public const double ItemGap = 18;
    public const double HatchSpacing = 6;
    public const double HatchOpacity = 0.45;

    private readonly Dictionary<string, ChartStateCategory> _categories = new(StringComparer.Ordinal);
    private readonly ChartColor _fallback;

    public ChartStateCategoryLegend(Chart chart) {
        foreach (var category in chart.Options.StateCategories) _categories[category.Key] = category;
        _fallback = chart.Options.Theme.MutedText;
        Categories = new List<ChartStateCategory>(chart.Options.StateCategories);
    }

    /// <summary>Gets the registered categories in legend order.</summary>
    public IReadOnlyList<ChartStateCategory> Categories { get; }

    /// <summary>Resolves a key; unregistered keys use the theme's muted colour and show the raw key.</summary>
    public ChartStateCategory Resolve(string? key) {
        var value = string.IsNullOrWhiteSpace(key) ? "?" : key!;
        if (_categories.TryGetValue(value, out var category)) return category;
        category = new ChartStateCategory(value, value, _fallback);
        _categories[value] = category;
        return category;
    }

    /// <summary>Wraps legend entries into centered rows using a renderer-specific label measure.</summary>
    public IReadOnlyList<ChartStateCategoryLegendItem> Layout(Func<string, double> measure, double left, double width) {
        var items = new List<ChartStateCategoryLegendItem>();
        var row = new List<(ChartStateCategory Category, double Width)>();
        var rowWidth = 0.0;
        var rowIndex = 0;
        void Flush() {
            var x = left + Math.Max(0, (width - rowWidth) / 2);
            foreach (var entry in row) {
                items.Add(new ChartStateCategoryLegendItem(entry.Category, x, rowIndex));
                x += entry.Width + ItemGap;
            }

            row.Clear();
            rowWidth = 0;
            rowIndex++;
        }

        foreach (var category in Categories) {
            var itemWidth = Swatch + LabelGap + measure(category.Label);
            if (row.Count > 0 && rowWidth + ItemGap + itemWidth > width) Flush();
            rowWidth = row.Count == 0 ? itemWidth : rowWidth + ItemGap + itemWidth;
            row.Add((category, itemWidth));
        }

        if (row.Count > 0) Flush();
        return items;
    }

    /// <summary>Returns the categorical cell of a heatmap point, or null for numeric heatmap rows.</summary>
    public static ChartHeatmapCell? HeatmapCell(ChartSeries series, int pointIndex) =>
        pointIndex >= 0 && pointIndex < series.HeatmapCells.Count ? series.HeatmapCells[pointIndex] : null;

    /// <summary>Returns true when the heatmap rows carry categorical cells.</summary>
    public static bool IsCategoricalHeatmap(IReadOnlyList<ChartSeries> rows) => rows.Count > 0 && rows[0].HeatmapCells.Count > 0;

    public static double Height(IReadOnlyList<ChartStateCategoryLegendItem> items) {
        var rows = 0;
        foreach (var item in items) rows = Math.Max(rows, item.Row + 1);
        return rows == 0 ? 0 : rows * RowHeight + 8;
    }
}

/// <summary>A positioned legend entry; <see cref="Row"/> counts from the top of the legend block.</summary>
internal readonly struct ChartStateCategoryLegendItem {
    public ChartStateCategoryLegendItem(ChartStateCategory category, double x, int row) {
        Category = category;
        X = x;
        Row = row;
    }

    public ChartStateCategory Category { get; }

    public double X { get; }

    public int Row { get; }
}
