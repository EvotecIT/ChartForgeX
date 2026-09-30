using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>
/// Places heatmap rows with optional group headers: a header row precedes the first row of each group, and a small
/// gap closes a group when ungrouped rows follow. Shared by the SVG and PNG renderers so rows line up in both.
/// </summary>
internal sealed class ChartHeatmapRowLayout {
    public const double SeparatorHeight = 8;
    /// <summary>Headers and separators together take at most this share of the plot height; beyond it they shrink.</summary>
    private const double MaximumHeaderShare = 0.4;
    private readonly double[] _headerHeightBefore;

    private ChartHeatmapRowLayout(double[] headerHeightBefore, List<ChartHeatmapRowGroup> groups, double headersHeight) {
        _headerHeightBefore = headerHeightBefore;
        Groups = groups;
        HeadersHeight = headersHeight;
    }

    /// <summary>Gets the group header rows and separators, top to bottom.</summary>
    public IReadOnlyList<ChartHeatmapRowGroup> Groups { get; }

    /// <summary>Gets the total height taken by group headers and separators.</summary>
    public double HeadersHeight { get; }

    /// <summary>
    /// Builds the layout. The header height comes from the theme tick font, not from a renderer's text metrics, so
    /// SVG and PNG place rows identically.
    /// </summary>
    public static ChartHeatmapRowLayout Build(Chart chart, IReadOnlyList<ChartSeries> rows, double plotHeight) {
        var wanted = Measure(rows, chart.Options.Theme.TickLabelFontSize * 1.2 + 10, SeparatorHeight);
        if (wanted.HeadersHeight <= plotHeight * MaximumHeaderShare || wanted.HeadersHeight <= 0) return wanted;
        var scale = plotHeight * MaximumHeaderShare / wanted.HeadersHeight;
        return Measure(rows, (chart.Options.Theme.TickLabelFontSize * 1.2 + 10) * scale, SeparatorHeight * scale);
    }

    private static ChartHeatmapRowLayout Measure(IReadOnlyList<ChartSeries> rows, double headerHeight, double separatorHeight) {
        var before = new double[rows.Count];
        var groups = new List<ChartHeatmapRowGroup>();
        var total = 0.0;
        string? current = null;
        for (var i = 0; i < rows.Count; i++) {
            var group = rows[i].LaneGroup;
            if (group != null && !string.Equals(group, current, StringComparison.Ordinal)) {
                groups.Add(new ChartHeatmapRowGroup(group, i, total, headerHeight));
                total += headerHeight;
            } else if (group == null && current != null) {
                groups.Add(new ChartHeatmapRowGroup(string.Empty, i, total, separatorHeight));
                total += separatorHeight;
            }

            current = group;
            before[i] = total;
        }

        return new ChartHeatmapRowLayout(before, groups, total);
    }

    /// <summary>Returns the cell height that fits every row, gap, and header into the plot.</summary>
    public double CellHeight(double plotHeight, double gap, int rowCount) =>
        Math.Max(1, (plotHeight - HeadersHeight - gap * (rowCount - 1)) / Math.Max(1, rowCount));

    public double RowTop(double plotTop, int rowIndex, double cellHeight, double gap) => plotTop + _headerHeightBefore[rowIndex] + rowIndex * (cellHeight + gap);

    /// <summary>Returns the top of a group header row or separator.</summary>
    public double GroupTop(double plotTop, ChartHeatmapRowGroup group, double cellHeight, double gap) => plotTop + group.HeadersBefore + group.BeforeRow * (cellHeight + gap);
}

/// <summary>A heatmap group header row, or a separator (empty name) that closes a group.</summary>
internal readonly struct ChartHeatmapRowGroup {
    public ChartHeatmapRowGroup(string name, int beforeRow, double headersBefore, double height) {
        Name = name;
        BeforeRow = beforeRow;
        HeadersBefore = headersBefore;
        Height = height;
    }

    public string Name { get; }

    /// <summary>Gets the index of the row the header sits above.</summary>
    public int BeforeRow { get; }

    /// <summary>Gets the height of the headers and separators above this one.</summary>
    public double HeadersBefore { get; }

    public double Height { get; }
}