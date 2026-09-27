using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Resolves state keys through <see cref="ChartOptions.StateCategories"/> and lays out the shared categorical legend
/// used by state timelines and categorical heatmaps, so both families and both renderers use one vocabulary.
/// </summary>
internal sealed class ChartStateCategoryLegend {
    public const double Swatch = 10;
    public const double SwatchRadius = 1.5;
    public const double LabelGap = 6;
    public const double ItemGap = 18;
    public const double HatchSpacing = 6;
    public const double HatchOpacity = 0.45;

    private readonly Dictionary<string, ChartStateCategory> _categories = new(StringComparer.Ordinal);
    private readonly ChartColor _fallback;
    private readonly Chart _chart;

    public ChartStateCategoryLegend(Chart chart) {
        _chart = chart;
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

    /// <summary>Wraps and budgets legend rows through the shared legend policy.</summary>
    public IReadOnlyList<ChartStateCategoryLegendItem> Layout(Func<string, double> measure, double left, double width, double availableHeight) {
        var rows = new List<List<(ChartStateCategory Category, double Width)>>();
        var row = new List<(ChartStateCategory Category, double Width)>();
        var rowWidth = 0.0;
        foreach (var state in Categories) {
            var itemWidth = Math.Min(width, Swatch + 6 + measure(state.Label));
            var needed = row.Count == 0 ? itemWidth : rowWidth + ItemGap + itemWidth;
            if (row.Count > 0 && needed > width) {
                rows.Add(row);
                row = new List<(ChartStateCategory Category, double Width)>();
                rowWidth = 0;
            }
            rowWidth = row.Count == 0 ? itemWidth : rowWidth + ItemGap + itemWidth;
            row.Add((state, itemWidth));
        }
        if (row.Count > 0) rows.Add(row);
        var omitted = 0;
        LegendRowBudget.Apply(rows, _chart, entry => entry.Count, count => {
            omitted = count;
            return new List<(ChartStateCategory Category, double Width)>();
        }, availableHeight, ChartLegendPosition.Bottom);
        var items = new List<ChartStateCategoryLegendItem>();
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            row = rows[rowIndex];
            if (row.Count == 0) {
                items.Add(new ChartStateCategoryLegendItem(null, left, rowIndex, omitted));
                continue;
            }
            rowWidth = 0;
            foreach (var entry in row) rowWidth += entry.Width;
            rowWidth += Math.Max(0, row.Count - 1) * ItemGap;
            var x = left + Math.Max(0, (width - rowWidth) / 2);
            foreach (var entry in row) {
                items.Add(new ChartStateCategoryLegendItem(entry.Category, x, rowIndex));
                x += entry.Width + ItemGap;
            }
        }
        return items;
    }

    /// <summary>Returns the categorical cell of a heatmap point, or null for numeric heatmap rows.</summary>
    public static ChartHeatmapCell? HeatmapCell(ChartSeries series, int pointIndex) =>
        pointIndex >= 0 && pointIndex < series.HeatmapCells.Count ? series.HeatmapCells[pointIndex] : null;

    /// <summary>Returns true when the heatmap rows carry categorical cells.</summary>
    public static bool IsCategoricalHeatmap(IReadOnlyList<ChartSeries> rows) => rows.Count > 0 && rows[0].HeatmapCells.Count > 0;

    public static double Height(Chart chart, IReadOnlyList<ChartStateCategoryLegendItem> items) {
        var rows = 0;
        foreach (var item in items) rows = Math.Max(rows, item.Row + 1);
        return rows == 0 ? 0 : rows * LegendRowBudget.RowHeight(chart) + 8;
    }
}

/// <summary>A positioned legend entry; <see cref="Row"/> counts from the top of the legend block.</summary>
internal readonly struct ChartStateCategoryLegendItem {
    public ChartStateCategoryLegendItem(ChartStateCategory? category, double x, int row, int omitted = 0) {
        Category = category;
        X = x;
        Row = row;
        Omitted = omitted;
    }

    public ChartStateCategory? Category { get; }

    public double X { get; }

    public int Row { get; }

    public int Omitted { get; }
}
