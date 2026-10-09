using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Prepares matrix, hexagonal and calendar geometry directly into one native scene.</summary>
internal static partial class VisualMatrixCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) =>
        chart.Options.ShowHeatmapScale && chart.Series.Any(series => series.IsCategoricalHeatmapRow)
            ? chart.Options.StateCategories.Select((state, i) => new VisualLegendEntry(state.Label, state.Color, "state-" + i, state: state, pinStateColors: chart.Options.PinStateColorsInForcedColors)).ToArray()
            : Array.Empty<VisualLegendEntry>();

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport) {
        if (viewport.Width <= 0 || viewport.Height <= 0 || chart.Series.Count == 0) return;
        var kind = chart.Series[0].Kind;
        if (chart.Series.Any(series => series.Kind != kind)) throw new InvalidOperationException("Matrix families cannot mix different layout kinds.");
        if (kind == ChartSeriesKind.CalendarHeatmap) { Calendar(chart, context, builder, viewport); return; }
        var rows = chart.Series.ToArray();
        var columns = rows.SelectMany(row => row.Points.Select(point => point.X)
            .Concat(Enumerable.Range(1, row.HeatmapColumnCount ?? 0).Select(index => (double)index))).Distinct().OrderBy(value => value).ToArray();
        if (columns.Any(value => !ChartMath.IsFinite(value))) throw new InvalidOperationException("Matrix column coordinates must be finite.");
        if (columns.Length == 0) { NoData(chart, context, builder, viewport); return; }
        var values = rows.SelectMany(row => row.Points).Select(point => point.Y).ToArray();
        if (values.Any(value => !ChartMath.IsFinite(value))) throw new InvalidOperationException("Matrix values must be finite; omit missing cells rather than supplying NaN.");
        var min = values.Length == 0 ? 0 : values.Min(); var max = values.Length == 0 ? 0 : values.Max();
        var colors = context.Theme.Resolve(context.ThemeMode);
        var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
        var categorical = ChartStateCategoryLegend.IsCategoricalHeatmap(rows);
        if (rows.Any(row => row.IsCategoricalHeatmapRow != categorical)) throw new InvalidOperationException("Numeric and categorical matrix rows cannot be mixed.");
        var categories = new ChartStateCategoryLegend(chart, colors.MutedForeground);
        var layout = MeasureMatrix(chart, context, builder, viewport, rows, columns, categorical);
        var plot = layout.Plot;
        var rowLayout = ChartHeatmapRowLayout.Build(rows, plot.Height, context.Theme.Typography.AxisSize);
        var gap = Math.Max(0, Math.Min(chart.Options.HeatmapCellGap ?? 2,
            Math.Min(columns.Length > 1 ? plot.Width / columns.Length * .5 : double.PositiveInfinity,
                rows.Length > 1 ? Math.Max(0, plot.Height - rowLayout.HeadersHeight) / rows.Length * .5 : double.PositiveInfinity)));
        var cellWidth = Math.Max(0, (plot.Width - gap * (columns.Length - 1)) / columns.Length);
        var cellHeight = Math.Max(0, (plot.Height - rowLayout.HeadersHeight - gap * (rows.Length - 1)) / rows.Length);
        var radius = Math.Min(chart.Options.HeatmapCellRadius ?? context.Theme.BarRadius, Math.Min(cellWidth, cellHeight) / 2);
        var hex = ChartHexbinLayout.Build(plot, rows.Length, columns.Length);
        var hexRadius = Math.Min(hex.Radius, Math.Min(cellHeight / 2, cellWidth / Math.Sqrt(3)));
        using (builder.PushGroup("matrix", kind == ChartSeriesKind.Heatmap ? "heatmap" : "hexbin-heatmap", new Dictionary<string, string> {
            ["data-cfx-row-count"] = rows.Length.ToString(), ["data-cfx-column-count"] = columns.Length.ToString(),
            ["data-cfx-min"] = VisualStateSceneTools.Number(min), ["data-cfx-max"] = VisualStateSceneTools.Number(max),
            ["data-cfx-cell-gap"] = VisualStateSceneTools.Number(gap), ["data-cfx-cell-radius"] = VisualStateSceneTools.Number(radius),
            ["data-cfx-value-text-mode"] = chart.Options.HeatmapValueTextMode.ToString(), ["data-cfx-label-level"] = chart.Options.Labels.Level
        })) {
            foreach (var group in rowLayout.Groups) {
                var y = rowLayout.GroupTop(plot.Top, group, cellHeight, gap);
                if (group.BeforeRow > 0) builder.Line(viewport.Left, y, plot.Right, y, colors.Border, context.Theme.GridStrokeWidth, "heatmap-row-group-rule", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Grid));
                VisualStateSceneTools.Text(builder, group.Name, new ChartRect(viewport.Left, y, plot.Right - viewport.Left, group.Height),
                    VisualStateSceneTools.TickStyle(chart, context), "heatmap-row-group", "heatmap-group-" + group.BeforeRow);
            }
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++) {
                var series = rows[rowIndex]; var y = rowLayout.RowTop(plot.Top, rowIndex, cellHeight, gap);
                if (chart.Options.ShowAxes && chart.Options.YAxis.Visible)
                    VisualStateSceneTools.Text(builder, series.Name, new ChartRect(viewport.Left, y, Math.Max(0, plot.Left - viewport.Left - context.Theme.Spacing), cellHeight),
                        VisualStateSceneTools.TickStyle(chart, context), "heatmap-row-label", "matrix-row-label-" + rowIndex, TextAlignment.Right);
                using (builder.PushGroup("series-" + rowIndex, "series", new Dictionary<string, string> {
                    ["data-cfx-series"] = rowIndex.ToString(), ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-series-name"] = series.Name
                })) {
                    for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                        var point = series.Points[pointIndex]; var columnIndex = Array.BinarySearch(columns, point.X);
                        var bounds = new ChartRect(plot.Left + columnIndex * (cellWidth + gap), y, cellWidth, cellHeight);
                        ChartPath? hexPath = null;
                        if (kind == ChartSeriesKind.HexbinHeatmap) {
                            var cx = plot.Left + (columnIndex + .5 + (rowIndex % 2) * .5) * plot.Width / (columns.Length + .5);
                            var cy = y + cellHeight / 2;
                            hexPath = ChartPathBuilder.FromPoints(ChartHexbinLayout.Points(cx, cy, hexRadius).ToArray(), ChartInterpolation.Linear);
                            bounds = new ChartRect(cx - hexRadius * Math.Sqrt(3) / 2, cy - hexRadius, hexRadius * Math.Sqrt(3), hexRadius * 2);
                        }
                        var cell = ChartStateCategoryLegend.HeatmapCell(series, pointIndex);
                        var state = cell.HasValue ? categories.Resolve(cell.Value.State) : null;
                        var stateMark = state == null ? (ChartStateMark?)null : ChartStateMark.For(state, backdrop);
                        var explicitColor = pointIndex < series.PointColors.Count ? series.PointColors[pointIndex] : null;
                        var blend = explicitColor.HasValue ? ChartColorBlend.Solid(explicitColor.Value, SvgColorRole.Series)
                            : ChartHeatmapSurface.CellBlend(chart, colors, series.Color, point.Y, min, max, VisualChartPaint.SeriesRole(series));
                        var fill = stateMark?.Surface ?? blend.Color;
                        var column = ChartAxisValueFormatter.Format(chart.Options.XAxis, point.X, null, columns);
                        var label = series.Name + ", " + column + ": " + (state?.Label ?? ChartNumericFormatter.FormatValue(chart.Options, point.Y));
                        if (!string.IsNullOrWhiteSpace(cell?.Tooltip)) label += ". " + cell!.Value.Tooltip;
                        var metadata = state == null ? new Dictionary<string, string>() : VisualStateSceneTools.StateMetadata(chart, state);
                        metadata["data-cfx-series"] = rowIndex.ToString(); metadata["data-cfx-point"] = pointIndex.ToString(); metadata["data-cfx-row"] = rowIndex.ToString();
                        metadata["data-cfx-source-point"] = (pointIndex < series.SourcePointIndices.Count ? series.SourcePointIndices[pointIndex] : pointIndex).ToString();
                        metadata["data-cfx-column"] = columnIndex.ToString(); metadata["data-cfx-x"] = VisualStateSceneTools.Number(point.X);
                        metadata["data-cfx-y"] = VisualStateSceneTools.Number(point.Y); metadata["data-cfx-value"] = VisualStateSceneTools.Number(point.Y);
                        if (state == null) {
                            var ratio = ChartHeatmapSurface.Ratio(chart, point.Y, min, max);
                            var status = ChartHeatmapSurface.CellStatus(chart, ratio);
                            if (status != null) metadata["data-cfx-status"] = status;
                            else metadata["data-cfx-level"] = ChartHeatmapSurface.Level(ratio).ToString();
                            if (status != null && chart.Options.PinStateColorsInForcedColors) metadata["data-cfx-pin-state-colors"] = "true";
                        }
                        var id = VisualStateSceneTools.SourceId(rowIndex, pointIndex);
                        using (VisualStateSceneTools.Mark(builder, id, kind == ChartSeriesKind.Heatmap ? "heatmap-cell" : "hexbin-cell", bounds, label, metadata, cell?.Href)) {
                          using (builder.PushClip(plot)) {
                            if (hexPath != null) builder.Path(hexPath, fill, role: "hexbin-cell-shape", close: true, paint: VisualChartPaint.Fill(blend.Paint));
                            else if (stateMark.HasValue) VisualStateSceneTools.StateRect(builder, bounds, stateMark.Value, radius, "heatmap-cell-shape");
                            else {
                                builder.Rect(bounds, fill, radius: radius, role: "heatmap-cell-shape", paint: VisualChartPaint.Fill(blend.Paint));
                                var pattern = pointIndex < series.PointFillPatterns.Count ? series.PointFillPatterns[pointIndex] ?? series.FillPattern : series.FillPattern;
                                builder.Pattern(VisualStateSceneTools.RoundedRect(bounds, radius), pattern, colors.Surface.WithOpacity(.45),
                                    paint: SvgPaint.Of(colors.Surface, SvgColorRole.Surface).WithOpacity(colors.Surface.WithOpacity(.45), .45));
                            }
                          }
                          CellLabel(chart, context, builder, series, pointIndex, bounds, viewport,
                              stateMark.HasValue ? ChartMarkText.OnStateMark(chart, colors, context.Frame, stateMark.Value)
                                  : ChartMarkText.OnHeatmapCell(chart, colors, context.Frame, fill, series.Color, point.Y, min, max,
                                      blend.FromRole ?? SvgColorRole.Ramp, blend), cell, id);
                        }
                    }
                }
            }
            MatrixAxes(chart, context, builder, viewport, plot, columns, cellWidth, gap, layout);
            if (!categorical && ScaleVisible(chart, context)) NumericScale(chart, context, builder, layout.Scale, min, max, rows[0].Color);
        }
    }

    private static bool ScaleVisible(Chart chart, VisualRenderContext context) => chart.Options.ShowHeatmapScale && context.Frame.ShowLegend;
    private static void NoData(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect bounds) =>
        VisualStateSceneTools.Text(builder, chart.Options.Labels.NoData, bounds, VisualStateSceneTools.TickStyle(chart, context), "no-data", "matrix-no-data", TextAlignment.Center);
}
