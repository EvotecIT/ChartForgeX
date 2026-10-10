using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    private static MatrixLayout MeasureMatrix(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ChartSeries[] rows, double[] columns, bool categorical, Func<double, string> formatColumn) {
        var style = VisualStateSceneTools.TickStyle(chart, context); var gap = context.Theme.Spacing;
        var x = chart.Options.ShowAxes && chart.Options.XAxis.Visible && chart.Options.ShowHeatmapColumnLabels;
        var y = chart.Options.ShowAxes && chart.Options.YAxis.Visible;
        var titleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = context.Theme.Resolve(context.ThemeMode).Foreground });
        var widest = columns.Select(formatColumn)
            .Select(text => builder.MeasureText(text, style).Width).DefaultIfEmpty(0).Max();
        var lineHeight = builder.MeasureText("Mg", style).Height;
        // The shared rotated-label owner already bounds its reach to 40% of the canvas.
        // Apply that same budget to the remaining frame viewport instead of a second,
        // tighter cap that shortens ordinary captions while the cell rows have room.
        var labels = x ? Math.Min(viewport.Height * (ChartHeatmapColumnLabels.IsRotated(chart) ? .4 : .28), ChartHeatmapColumnLabels.Reserve(chart, widest, lineHeight)) : 0;
        var left = y ? Math.Min(viewport.Width * .3, rows.Select(row => builder.MeasureText(row.Name, style).Width).DefaultIfEmpty(0).Max() + gap) : 0;
        var top = y && chart.YAxisTitle.Length > 0 ? builder.MeasureText(chart.YAxisTitle, titleStyle).Height + gap : 0;
        var xTitle = x ? ChartTimeScale.DecorateTitle(chart.Options.XAxis, chart.XAxisTitle) : string.Empty;
        var titleHeight = xTitle.Length > 0 ? builder.MeasureText(xTitle, titleStyle).Height + gap : 0;
        var scaleHeight = !categorical && ScaleVisible(chart, context) ? Math.Min(viewport.Height * .22, lineHeight * 2 + gap) : 0;
        // Only left-slanting captions can use the row-label gutter. The right side has no
        // equivalent gutter inside the common viewport, so reserve its complete reach there.
        var side = 0d;
        if (x && ChartHeatmapColumnLabels.IsRotated(chart)) {
            var hexagonal = rows[0].Kind == ChartSeriesKind.HexbinHeatmap;
            var anchorShare = hexagonal ? (ChartHeatmapColumnLabels.EndsAtColumn(chart) ? .5 : 1) / (columns.Length + .5) : .5 / columns.Length;
            var columnGap = hexagonal ? 0 : Math.Max(0, Math.Min(chart.Options.HeatmapCellGap ?? 2, viewport.Width / columns.Length * .5));
            // An outer caption starts at a cell centre, not at the plot edge. Solve
            // side + anchorShare * remainingPlotWidth = reach, retaining that usable
            // half-cell before sacrificing plot width and thinning readable columns.
            side = Side(left);
            var minimumPitch = ChartHeatmapColumnLabels.MinimumPitch(chart, lineHeight, widest);
            var minimumPlot = hexagonal ? minimumPitch * (columns.Length + .5) : minimumPitch * columns.Length - columnGap;
            var minimumGutter = Math.Min(left, builder.MeasureText("…", style).Width + gap);
            if (minimumPlot > viewport.Width - left - side && minimumPlot <= viewport.Width - minimumGutter - side) {
                // Long row names already use bounded fitting. Give their spare gutter
                // to readable column captions before dropping every second column.
                left = Math.Max(minimumGutter, viewport.Width - side - minimumPlot);
                side = Side(left);
            }

            double Side(double rowGutter) {
                var spaceBeside = ChartHeatmapColumnLabels.EndsAtColumn(chart) ? rowGutter : 0;
                var reach = ChartHeatmapColumnLabels.SideReserve(chart, widest, lineHeight, spaceBeside);
                return Math.Min(viewport.Width * .15, Math.Max(0,
                    (reach - anchorShare * (viewport.Width - rowGutter - columnGap * (columns.Length - 1))) / (1 - anchorShare)));
            }
        }
        var plotLeft = viewport.Left + left + (ChartHeatmapColumnLabels.EndsAtColumn(chart) ? side : 0);
        var plot = new ChartRect(plotLeft, viewport.Top + top, Math.Max(0, viewport.Width - left - side), Math.Max(0, viewport.Height - top - labels - titleHeight - scaleHeight));
        return new MatrixLayout(plot, new ChartRect(plot.Left, viewport.Bottom - scaleHeight, plot.Width, scaleHeight), labels, titleHeight, xTitle, titleStyle);
    }

    private static void MatrixAxes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, ChartRect plot,
        double[] columns, double cellWidth, double gap, MatrixLayout layout, Func<double, string> formatColumn) {
        var style = VisualStateSceneTools.TickStyle(chart, context);
        if (chart.Options.ShowAxes && chart.Options.XAxis.Visible && chart.Options.ShowHeatmapColumnLabels) {
            var angle = ChartHeatmapColumnLabels.Angle(chart);
            var width = columns.Select(value => builder.MeasureText(formatColumn(value), style).Width).DefaultIfEmpty(0).Max();
            var pitch = chart.Series[0].Kind == ChartSeriesKind.HexbinHeatmap ? plot.Width / (columns.Length + .5) : cellWidth + gap;
            var step = Math.Abs(angle) < .001 ? 1 : ChartHeatmapColumnLabels.Step(chart, pitch, builder.MeasureText("Mg", style).Height, width);
            if (step > 1) builder.AddDiagnostic(new VisualDiagnostic("matrix.column-label-density",
                "Some column captions were omitted to keep measured rotated labels separated; all complete captions remain in descriptive regions."));
            for (var index = 0; index < columns.Length; index++) {
                var text = formatColumn(columns[index]);
                var x = chart.Series[0].Kind == ChartSeriesKind.HexbinHeatmap ? plot.Left + (index + .5) * plot.Width / (columns.Length + .5)
                    : plot.Left + index * (cellWidth + gap) + cellWidth / 2;
                if (index % step != 0) {
                    // Density limits only visible labels. Retain the omitted column's complete caption
                    // and descriptive slot just as fitted visible labels retain their full source text.
                    builder.AddRegion(new VisualSemanticRegion("matrix-column-" + index, "heatmap-column-label",
                        new ChartRect(x - cellWidth / 2, plot.Bottom, cellWidth, layout.LabelHeight), text));
                    continue;
                }
                if (Math.Abs(angle) < .001) {
                    VisualStateSceneTools.Text(builder, text, new ChartRect(x - cellWidth / 2, plot.Bottom, cellWidth, layout.LabelHeight), style,
                        "heatmap-column-label", "matrix-column-" + index, TextAlignment.Center);
                } else {
                    var radians = Math.Abs(angle) * Math.PI / 180;
                    var height = builder.MeasureText(text, style).Height;
                    var maxWidth = Math.Max(0, Math.Min(ChartHeatmapColumnLabels.MaximumRotatedLength(chart, height),
                        (layout.LabelHeight - height * Math.Cos(radians) - 8) / Math.Max(.001, Math.Sin(radians))));
                    var horizontal = angle < 0 ? x - viewport.Left : viewport.Right - x;
                    // The height projects towards the opposite side of the anchor; only the text width
                    // reaches this edge. Subtracting both needlessly shortens the outermost caption.
                    maxWidth = Math.Min(maxWidth, Math.Max(0, horizontal / Math.Cos(radians)));
                    var textWidth = Math.Min(maxWidth, builder.MeasureText(text, style).Width);
                    var left = angle < 0 ? x - textWidth : x;
                    using (builder.PushClip(viewport))
                    using (builder.PushRotation(angle, x, plot.Bottom + 8))
                        VisualStateSceneTools.Text(builder, text, new ChartRect(left, plot.Bottom + 8, textWidth, height), style,
                            "heatmap-column-label", "matrix-column-" + index, angle < 0 ? TextAlignment.Right : TextAlignment.Left);
                }
            }
            VisualStateSceneTools.Text(builder, layout.XTitle, new ChartRect(plot.Left, plot.Bottom + layout.LabelHeight, plot.Width, layout.TitleHeight),
                layout.TitleStyle, "heatmap-x-axis-title", "matrix-x-title", TextAlignment.Center);
        }
        if (chart.Options.ShowAxes && chart.Options.YAxis.Visible && chart.YAxisTitle.Length > 0)
            VisualStateSceneTools.Text(builder, chart.YAxisTitle, new ChartRect(viewport.Left, viewport.Top, viewport.Width, Math.Max(0, plot.Top - viewport.Top)),
                layout.TitleStyle, "heatmap-y-axis-title", "matrix-y-title");
    }

    private static void CellLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series, int index,
        ChartRect cellBounds, ChartRect viewport, ChartColorBlend ink, ChartHeatmapCell? cell, string id) {
        var text = VisualStateSceneTools.Value(chart, series, index, series.Points[index].Y);
        if (cell.HasValue && (index >= series.PointLabels.Count || series.PointLabels[index] == null)) text = cell.Value.Text ?? text;
        var style = VisualStateSceneTools.DataStyle(chart, context, series, index, ink.Color);
        style.FontSize = Math.Max(8, style.FontSize);
        var inner = new ChartRect(cellBounds.Left + 3, cellBounds.Top + 3, Math.Max(0, cellBounds.Width - 6), Math.Max(0, cellBounds.Height - 6));
        var measured = builder.MeasureText(text, style);
        var fits = cell.HasValue ? ChartHeatmapSurface.CategoricalLabelFits(cellBounds.Width, cellBounds.Height, measured.Width, measured.Height)
            : measured.Width <= inner.Width && measured.Height <= inner.Height;
        var mode = chart.Options.HeatmapValueTextMode;
        var visible = mode == ChartHeatmapValueTextMode.Always || mode == ChartHeatmapValueTextMode.Auto && (series.ShowDataLabels ?? chart.Options.ShowDataLabels) && fits;
        if (cell.HasValue) visible = cell.Value.Text != null && mode != ChartHeatmapValueTextMode.Hidden && (mode == ChartHeatmapValueTextMode.Always || fits);
        if (!visible || mode == ChartHeatmapValueTextMode.Hidden) return;
        var placement = cell.HasValue ? ChartDataLabelPlacement.Center : series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        if (placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside) {
            VisualStateSceneTools.Text(builder, text, inner, style, "data-label", id + "-label", TextAlignment.Center, mode == ChartHeatmapValueTextMode.Always,
                VisualStateSceneTools.DataPaint(chart, series, index, style, ink.Paint));
            return;
        }
        var gap = context.Theme.Spacing / 2;
        var width = Math.Min(measured.Width, viewport.Width); var height = Math.Min(measured.Height, viewport.Height);
        var left = cellBounds.Left + (cellBounds.Width - width) / 2; var top = cellBounds.Top - gap - height;
        if (placement == ChartDataLabelPlacement.Below) top = cellBounds.Bottom + gap;
        else if (placement is ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside) { left = cellBounds.Right + gap; top = cellBounds.Top + (cellBounds.Height - height) / 2; }
        else if (placement == ChartDataLabelPlacement.Left) { left = cellBounds.Left - gap - width; top = cellBounds.Top + (cellBounds.Height - height) / 2; }
        left = Math.Max(viewport.Left, Math.Min(viewport.Right - width, left)); top = Math.Max(viewport.Top, Math.Min(viewport.Bottom - height, top));
        if (placement is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside)
            VisualStateSceneTools.Connector(builder, chart.Options, new ChartPoint(cellBounds.Left + cellBounds.Width / 2, cellBounds.Top + cellBounds.Height / 2),
                new ChartPoint(left + width / 2, top + height / 2), context.Theme.Resolve(context.ThemeMode).MutedForeground);
        VisualStateSceneTools.Text(builder, text, new ChartRect(left, top, width, height), style, "data-label", id + "-label", TextAlignment.Center, true,
            VisualStateSceneTools.DataPaint(chart, series, index, style, ink.Paint));
    }

    private static void NumericScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect bounds, double min, double max, ChartColor? high) {
        if (bounds.Height <= 0 || bounds.Width <= 0) return;
        var colors = context.Theme.Resolve(context.ThemeMode); var style = VisualStateSceneTools.TickStyle(chart, context);
        var textHeight = builder.MeasureText("Mg", style).Height; var swatch = Math.Min(12, Math.Max(0, bounds.Height - textHeight));
        var values = chart.Series.SelectMany(series => series.Points).Select(point => point.Y).ToArray();
        var zero = min >= 0 && values.Any(value => value == 0);
        var nonZero = values.Where(value => value != 0).ToArray();
        var floor = zero && nonZero.Length > 0 ? nonZero.Min() : min;
        var zeroLabel = ChartNumericFormatter.FormatValue(chart.Options, 0);
        var zeroWidth = zero ? Math.Min(bounds.Width * .3, builder.MeasureText(zeroLabel, style).Width + context.Theme.Spacing) : 0;
        if (nonZero.Length == 0) zeroWidth = bounds.Width;
        // Keep the complete key together: captions belong to the swatches, rather
        // than the unrelated edges of a wide matrix plotting area.
        var rampWidth = nonZero.Length > 0 ? Math.Min(Math.Max(0, bounds.Width - zeroWidth), 160) : 0;
        var scaleLeft = bounds.Left + (bounds.Width - zeroWidth - rampWidth) / 2;
        using (builder.PushGroup("matrix-scale", "heatmap-scale", new Dictionary<string, string> {
            ["data-cfx-min-value"] = VisualStateSceneTools.Number(floor), ["data-cfx-max-value"] = VisualStateSceneTools.Number(max)
        })) {
            if (zero) {
                var blend = ChartHeatmapSurface.CellBlend(chart, colors, high, 0, min, max, VisualChartPaint.SeriesRole(chart.Series[0]));
                var box = new ChartRect(scaleLeft + (zeroWidth - Math.Min(12, zeroWidth)) / 2, bounds.Top, Math.Min(12, zeroWidth), swatch);
                using (VisualStateSceneTools.Mark(builder, "matrix-scale-zero", "heatmap-scale-zero", box, zeroLabel,
                    ScaleMetadata(0, ChartHeatmapSurface.Ratio(chart, 0, min, max)))) builder.Rect(box, blend.Color, radius: 1, paint: VisualChartPaint.Fill(blend.Paint));
                VisualStateSceneTools.Text(builder, zeroLabel, new ChartRect(scaleLeft, bounds.Top + swatch, zeroWidth, textHeight),
                    style, "heatmap-scale-zero-label", "matrix-scale-zero-label", TextAlignment.Center);
            }
            if (nonZero.Length == 0) return;
            var area = new ChartRect(scaleLeft + zeroWidth, bounds.Top, rampWidth, bounds.Height);
            var width = area.Width / 5; var left = area.Left;
            for (var index = 0; index < 5; index++) {
                var value = ChartHeatmapSurface.InterpolateObservedRange(floor, max, index / 4d);
                var blend = ChartHeatmapSurface.CellBlend(chart, colors, high, value, min, max, VisualChartPaint.SeriesRole(chart.Series[0]));
                var box = new ChartRect(left + index * width, bounds.Top, Math.Max(0, width - 2), swatch);
                var label = ChartNumericFormatter.FormatValue(chart.Options, value);
                using (VisualStateSceneTools.Mark(builder, "matrix-scale-" + index, "heatmap-scale-step", box, label,
                    ScaleMetadata(value, ChartHeatmapSurface.Ratio(chart, value, min, max)))) builder.Rect(box, blend.Color, radius: 1, paint: VisualChartPaint.Fill(blend.Paint));
            }
            VisualStateSceneTools.Text(builder, ChartNumericFormatter.FormatValue(chart.Options, floor), new ChartRect(area.Left, bounds.Top + swatch, area.Width / 2, textHeight), style, "heatmap-scale-label", "matrix-scale-low");
            VisualStateSceneTools.Text(builder, ChartNumericFormatter.FormatValue(chart.Options, max), new ChartRect(area.Left + area.Width / 2, bounds.Top + swatch, area.Width / 2, textHeight), style, "heatmap-scale-label", "matrix-scale-high", TextAlignment.Right);
        }
        Dictionary<string, string> ScaleMetadata(double value, double ratio) {
            var metadata = new Dictionary<string, string> { ["data-cfx-value"] = VisualStateSceneTools.Number(value) };
            var status = ChartHeatmapSurface.CellStatus(chart, ratio);
            if (status != null) metadata["data-cfx-status"] = status;
            else metadata["data-cfx-level"] = ChartHeatmapSurface.Level(ratio).ToString();
            return metadata;
        }
    }

    private sealed class MatrixLayout {
        internal MatrixLayout(ChartRect plot, ChartRect scale, double labelHeight, double titleHeight, string xTitle, TextStyle titleStyle) {
            Plot = plot; Scale = scale; LabelHeight = labelHeight; TitleHeight = titleHeight; XTitle = xTitle; TitleStyle = titleStyle;
        }
        internal ChartRect Plot { get; } internal ChartRect Scale { get; }
        internal double LabelHeight { get; } internal double TitleHeight { get; }
        internal string XTitle { get; } internal TextStyle TitleStyle { get; }
    }
}
