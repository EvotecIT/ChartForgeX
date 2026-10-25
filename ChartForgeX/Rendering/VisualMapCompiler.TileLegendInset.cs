using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    /// <summary>Uses a measured empty grid rectangle when separate keys would shrink ordinary tile codes.</summary>
    internal static TileMapLegendInset? ResolveTileLegendInset(Chart chart, VisualRenderContext context,
        VisualSceneBuilder builder, IReadOnlyList<VisualLegendEntry> entries, ChartColorPresentation presentation) {
        var series = chart.Series[0]; var definition = series.TileMapDefinition;
        if (chart.Series.Count != 1 || series.Kind != ChartSeriesKind.TileMap || definition == null || entries.Count != 1 || !chart.Options.ShowMapLabels
            || !context.Frame.ShowLegend || !presentation.ShowLegend || presentation.HasName
            || chart.Options.MapColorScale != null || chart.Options.MapScaleLegendPosition != ChartMapScaleLegendPosition.Bottom
            || chart.Options.HasExplicitMapScaleLegendPosition
            || ChartHeatmapSurface.MapMidpointLabel(chart) != null || !HasMissingMapValues(chart)
            || !string.IsNullOrEmpty(context.Frame.Title) || !string.IsNullOrEmpty(context.Frame.Subtitle)
            || !string.IsNullOrEmpty(context.Frame.LegendTitle) || series.RenderPoints.Count == 0
            || series.RenderPoints.Any(point => !Finite(point.Y) || point.Y < 0)
            || !Finite(chart.Options.MapRegionStrokeWidth) || chart.Options.MapRegionStrokeWidth < 0) return null;
        // Bound optional occupancy work independently of authored sparse coordinate ranges.
        if (definition.ColumnCount > 128 || definition.RowCount > 128
            || (long)definition.ColumnCount * definition.RowCount > 4096) return null;
        var pad = context.Layout.PaddingEdges; var size = context.Layout.Size;
        var plot = new ChartRect(pad.Left, pad.Top, size.Width - pad.Left - pad.Right, size.Height - pad.Top - pad.Bottom);
        var style = TickStyle(chart, context); style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
        var stroke = chart.Options.MapRegionStrokeWidth; var spacing = context.Theme.Spacing;
        var requiredTile = 0d;
        foreach (var region in definition.Regions) {
            var text = builder.MeasureText(region.Code, style);
            requiredTile = Math.Max(requiredTile, Math.Max(text.Height + stroke,
                text.Width + stroke + 2 * TileHexInsetRatio * (text.Height + stroke)) + .01);
        }
        var legendStyle = context.Frame.LegendStyle ?? new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.LegendSize, LineHeight = 1
        };
        var seriesRow = Math.Max(legendStyle.EffectiveFontSize * 1.5, builder.MeasureText("Mg", legendStyle).Height);
        if (size.Height * context.Frame.LegendMaximumHeightFraction < seriesRow
            || !string.IsNullOrEmpty(entries[0].Value) || !string.IsNullOrEmpty(entries[0].Percentage)) return null;
        var captions = ResolveScaleCaptions(chart, series.RenderPoints.Min(point => point.Y), series.RenderPoints.Max(point => point.Y), presentation);
        var low = captions[2]; var high = captions[0];
        var row = new[] { "Mg", low, high, presentation.NoData, series.Name }.Max(text => builder.MeasureText(text, style).Height);
        var keyHeight = row * 1.5; var separation = spacing / 4;
        if (requiredTile * definition.RowCount + stroke <= plot.Height - seriesRow - spacing - keyHeight - separation) return null;
        var tileGap = TileGap(chart, context, builder, definition, plot, stroke);
        var tile = Math.Min((plot.Width - stroke - tileGap * (definition.ColumnCount - 1)) / definition.ColumnCount,
            (plot.Height - stroke - tileGap * (definition.RowCount - 1)) / definition.RowCount);
        if (tile < requiredTile) return null;
        var gridWidth = tile * definition.ColumnCount + tileGap * (definition.ColumnCount - 1);
        var gridHeight = tile * definition.RowCount + tileGap * (definition.RowCount - 1);
        var grid = new ChartRect(plot.Left + (plot.Width - gridWidth) / 2, plot.Top + (plot.Height - gridHeight) / 2, gridWidth, gridHeight);
        var missingWidth = row * .7 + 4 + builder.MeasureText(presentation.NoData, style).Width;
        var keyWidth = 2 * Math.Max(builder.MeasureText(low, style).Width, builder.MeasureText(high, style).Width) + spacing * 2;
        var seriesWidth = builder.MeasureText(entries[0].Label, legendStyle).Width + 28;
        var inset = Math.Max(2, Math.Max(stroke / 2, separation));
        var minimumWidth = Math.Max(keyWidth, seriesWidth + missingWidth + spacing) + inset * 2;
        var minimumHeight = seriesRow + separation + keyHeight + inset * 2;
        var slot = EmptyTileLegendSlot(definition, grid, tile, tileGap, minimumWidth, minimumHeight);
        if (!slot.HasValue) return null;
        var area = slot.Value;
        var legend = new ChartRect(area.Left + inset, area.Top + inset, area.Width - inset * 2, seriesRow + separation + keyHeight);
        return new TileMapLegendInset(plot, grid, tile, tileGap, legend,
            new ChartRect(legend.Left, legend.Top + seriesRow + separation, legend.Width, keyHeight),
            new ChartRect(legend.Right - missingWidth, legend.Top, missingWidth, Math.Min(row, seriesRow)));
    }

    private static ChartRect? EmptyTileLegendSlot(ChartTileMapDefinition definition, ChartRect grid,
        double tile, double gap, double minimumWidth, double minimumHeight) {
        var occupied = new bool[definition.RowCount, definition.ColumnCount];
        foreach (var region in definition.Regions) occupied[region.Row, region.Column] = true;
        ChartRect? best = null;
        for (var firstRow = 0; firstRow < definition.RowCount; firstRow++) {
            var blocked = new bool[definition.ColumnCount];
            for (var lastRow = firstRow; lastRow < definition.RowCount; lastRow++) {
                var height = (lastRow - firstRow + 1) * (tile + gap) - gap;
                var start = 0;
                for (var column = 0; column <= definition.ColumnCount; column++) {
                    if (column < definition.ColumnCount) blocked[column] |= occupied[lastRow, column];
                    if (column < definition.ColumnCount && !blocked[column]) continue;
                    var width = (column - start) * (tile + gap) - gap;
                    if (height >= minimumHeight && width >= minimumWidth
                        && (!best.HasValue || width * height > best.Value.Width * best.Value.Height))
                        best = new ChartRect(grid.Left + start * (tile + gap), grid.Top + firstRow * (tile + gap), width, height);
                    start = column + 1;
                }
            }
        }
        return best;
    }
}

/// <summary>One resolved map-owned placement for native tiles, their series identity and quantitative key.</summary>
internal sealed class TileMapLegendInset {
    internal TileMapLegendInset(ChartRect map, ChartRect grid, double tileSize, double tileGap, ChartRect legendArea, ChartRect colorKey, ChartRect missingDataKey) {
        Map = map; Grid = grid; TileSize = tileSize; TileGap = tileGap; LegendArea = legendArea; ColorKey = colorKey; MissingDataKey = missingDataKey;
    }
    internal ChartRect Map { get; }
    internal ChartRect Grid { get; }
    internal double TileSize { get; }
    internal double TileGap { get; }
    internal ChartRect LegendArea { get; }
    internal ChartRect ColorKey { get; }
    internal ChartRect MissingDataKey { get; }
}
