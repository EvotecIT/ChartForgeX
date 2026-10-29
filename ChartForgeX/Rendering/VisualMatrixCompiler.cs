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
    // Largest theme corner radius as a share of a heatmap cell's shorter side.
    private const double MaximumThemeCellRounding = .2;

    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) =>
        chart.Options.ShowHeatmapCategoryLegend && chart.Series.Any(series => series.IsCategoricalHeatmapRow)
            ? chart.Options.StateCategories.Select((state, i) => new VisualLegendEntry(state.Label, state.Color, "state-" + i, state: state, pinStateColors: chart.Options.PinStateColorsInForcedColors)).ToArray()
            : Array.Empty<VisualLegendEntry>();

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, ChartColorPresentation? presentation = null) {
        if (viewport.Width <= 0 || viewport.Height <= 0 || chart.Series.Count == 0) return;
        presentation ??= new ChartColorPresentation(chart.Options.HeatmapColorDimension, chart.Options.Labels, chart.Options.Labels.Value, chart.Options.ValueFormat);
        var kind = chart.Series[0].Kind;
        if (chart.Series.Any(series => series.Kind != kind)) throw new InvalidOperationException("Matrix families cannot mix different layout kinds.");
        if (kind == ChartSeriesKind.CalendarHeatmap) { Calendar(chart, context, builder, viewport, presentation); return; }
        var rows = chart.Series.ToArray();
        var columns = rows.SelectMany(row => row.RenderPoints.Select(point => point.X)
            .Concat(Enumerable.Range(1, row.HeatmapColumnCount ?? 0).Select(index => (double)index))).Distinct().OrderBy(value => value).ToArray();
        if (columns.Any(value => !ChartMath.IsFinite(value))) throw new InvalidOperationException("Matrix column coordinates must be finite.");
        if (columns.Length == 0) { NoData(chart, context, builder, viewport); return; }
        var values = rows.SelectMany(row => row.RenderPoints).Select(point => point.Y).ToArray();
        if (values.Any(value => !ChartMath.IsFinite(value))) throw new InvalidOperationException("Matrix values must be finite; omit missing cells rather than supplying NaN.");
        var min = values.Length == 0 ? 0 : values.Min(); var max = values.Length == 0 ? 0 : values.Max();
        var missing = rows.Any(row => row.RenderPoints.Count < columns.Length);
        var hasDomain = ChartColorScaleSurface.HasDomain(chart.Options.HeatmapColorScale, values.Length > 0 ? min : null, values.Length > 0 ? max : null);
        var colors = context.Theme.Resolve(context.ThemeMode);
        var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
        var categorical = ChartStateCategoryLegend.IsCategoricalHeatmap(rows);
        if (rows.Any(row => row.IsCategoricalHeatmapRow != categorical)) throw new InvalidOperationException("Numeric and categorical matrix rows cannot be mixed.");
        var categories = new ChartStateCategoryLegend(chart, colors.MutedForeground);
        var categoryGroups = MatrixCategoryGroups.Capture(chart, rows, columns);
        var formatColumn = ChartAxisValueFormatter.Create(chart.Options.XAxis, columns);
        var layout = MeasureMatrix(chart, context, builder, viewport, rows, columns, categorical, formatColumn, presentation, hasDomain, min, max, missing, categoryGroups);
        var plot = layout.Plot;
        builder.AddRegion(new VisualSemanticRegion("matrix-plot", "matrix-plot", plot));
        var rowLayout = ChartHeatmapRowLayout.Build(rows, plot.Height, context.Theme.Typography.AxisSize);
        var gap = Math.Max(0, Math.Min(chart.Options.HeatmapCellGap ?? 2,
            Math.Min(columns.Length > 1 ? plot.Width / columns.Length * .5 : double.PositiveInfinity,
                rows.Length > 1 ? Math.Max(0, plot.Height - rowLayout.HeadersHeight) / rows.Length * .5 : double.PositiveInfinity)));
        var cellWidth = Math.Max(0, (plot.Width - gap * (columns.Length - 1)) / columns.Length);
        var cellHeight = Math.Max(0, (plot.Height - rowLayout.HeadersHeight - gap * (rows.Length - 1)) / rows.Length);
        var rowPitch = cellHeight + gap;
        var rowLabelStride = rowPitch > 0 ? Math.Max(1, (int)Math.Ceiling(ChartRowText.MinimumFontSize * 1.2 / rowPitch)) : rows.Length;
        if (rowLabelStride > 1 && chart.Options.ShowAxes && chart.Options.YAxis.Visible)
            builder.AddDiagnostic(new VisualDiagnostic("matrix.row-label-density",
                "Some row names were omitted because the frame is too short for readable labels; complete names remain in semantic regions."));
        // Theme rounding is for bars; small cells keep a rounded-square reading instead of becoming dots.
        var radius = Math.Min(chart.Options.HeatmapCellRadius ?? context.Theme.BarRadius, Math.Min(cellWidth, cellHeight) * (chart.Options.HeatmapCellRadius.HasValue ? .5 : MaximumThemeCellRounding));
        var hex = ChartHexbinLayout.Build(plot, rows.Length, columns.Length);
        var hexRadius = Math.Min(hex.Radius, Math.Min(cellHeight / 2, cellWidth / Math.Sqrt(3)));
        var matrixMetadata = new Dictionary<string, string> {
            ["data-cfx-row-count"] = rows.Length.ToString(), ["data-cfx-column-count"] = columns.Length.ToString(),
            ["data-cfx-cell-gap"] = VisualStateSceneTools.Number(gap), ["data-cfx-cell-radius"] = VisualStateSceneTools.Number(radius),
            ["data-cfx-value-text-mode"] = chart.Options.HeatmapValueTextMode.ToString(), ["data-cfx-label-level"] = chart.Options.Labels.Level
        };
        if (values.Length > 0) {
            matrixMetadata["data-cfx-min"] = VisualStateSceneTools.Number(min); matrixMetadata["data-cfx-max"] = VisualStateSceneTools.Number(max);
        }
        using (builder.PushGroup("matrix", kind == ChartSeriesKind.Heatmap ? "heatmap" : "hexbin-heatmap", matrixMetadata)) {
            foreach (var group in rowLayout.Groups) {
                var y = rowLayout.GroupTop(plot.Top, group, cellHeight, gap);
                var ruleY = y;
                if (ReverseRows(chart)) { y = MirrorTop(plot, y, group.Height); ruleY = y + group.Height; }
                if (group.BeforeRow > 0) builder.Line(viewport.Left, ruleY, plot.Right, ruleY, colors.Border, context.Theme.GridStrokeWidth, "heatmap-row-group-rule", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Grid));
                var groupStyle = ChartRowText.Style(builder, VisualStateSceneTools.TickStyle(chart, context), group.Name, group.Height, plot.Right - viewport.Left);
                VisualStateSceneTools.Text(builder, group.Name, ChartRowText.Bounds(builder, groupStyle, group.Name, viewport.Left, plot.Right - viewport.Left, y, group.Height, group.Height),
                    groupStyle, "heatmap-row-group", "heatmap-group-" + group.BeforeRow);
            }
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++) {
                var series = rows[rowIndex]; var y = rowLayout.RowTop(plot.Top, rowIndex, cellHeight, gap);
                if (ReverseRows(chart)) y = MirrorTop(plot, y, cellHeight);
                if (chart.Options.ShowAxes && chart.Options.YAxis.Visible) {
                    // Row labels follow the shared row-text policy: a readable floor, truncation, and the row pitch
                    // (cell plus gap) rather than the painted cell height.
                    var rowName = categoryGroups?.RowText(rowIndex, series.Name) ?? series.Name;
                    var labelLeft = viewport.Left + layout.RowGroupWidth;
                    var labelWidth = Math.Max(0, plot.Left - viewport.Left - layout.RowGroupWidth - context.Theme.Spacing);
                    var labelStyle = ChartRowText.Style(builder, VisualStateSceneTools.TickStyle(chart, context), rowName, rowPitch * rowLabelStride, labelWidth);
                    var labelBounds = ChartRowText.Bounds(builder, labelStyle, rowName, labelLeft, labelWidth, y, cellHeight, rowPitch * rowLabelStride);
                    if (rowIndex % rowLabelStride == 0)
                        VisualStateSceneTools.Text(builder, rowName, labelBounds, labelStyle, "heatmap-row-label", "matrix-row-label-" + rowIndex, TextAlignment.Right);
                    else
                        builder.AddRegion(new VisualSemanticRegion("matrix-row-label-" + rowIndex, "heatmap-row-label", labelBounds, rowName));
                }
                using (builder.PushGroup("series-" + rowIndex, "series", new Dictionary<string, string> {
                    ["data-cfx-series"] = rowIndex.ToString(), ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-series-name"] = series.Name
                })) {
                    for (var renderIndex = 0; renderIndex < series.RenderPoints.Count; renderIndex++) {
                        var point = series.RenderPoints[renderIndex]; var columnIndex = Array.BinarySearch(columns, point.X);
                        var pointIndex = categorical ? (int)point.X - 1 : renderIndex;
                        var cellLeft = plot.Left + columnIndex * (cellWidth + gap);
                        if (ReverseColumns(chart)) cellLeft = plot.Left + plot.Right - cellLeft - cellWidth;
                        var bounds = new ChartRect(cellLeft, y, cellWidth, cellHeight);
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
                        var column = formatColumn(point.X);
                        // Readouts carry the chart's value unit; a format that already writes the unit ("0.0 'ms'") keeps a single copy.
                        var readout = ChartOptions.AppendUnit(presentation.Value(point.Y), chart.Options.ValueUnit);
                        var label = categoryGroups == null ? series.Name + ", " + column + ": " + (state?.Label ?? readout)
                            : categoryGroups.Describe(rowIndex, columnIndex, series.Name, column) + ": " + (state?.Label ?? readout);
                        if (!string.IsNullOrWhiteSpace(cell?.Tooltip)) label += ". " + cell!.Value.Tooltip;
                        var metadata = state == null ? new Dictionary<string, string>() : VisualStateSceneTools.StateMetadata(chart, state);
                        metadata["data-cfx-series"] = rowIndex.ToString(); metadata["data-cfx-point"] = pointIndex.ToString(); metadata["data-cfx-row"] = rowIndex.ToString();
                        metadata["data-cfx-source-point"] = (pointIndex < series.SourcePointIndices.Count ? series.SourcePointIndices[pointIndex] : pointIndex).ToString();
                        metadata["data-cfx-column"] = columnIndex.ToString(); metadata["data-cfx-x"] = VisualStateSceneTools.Number(point.X);
                        metadata["data-cfx-y"] = VisualStateSceneTools.Number(point.Y); metadata["data-cfx-value"] = VisualStateSceneTools.Number(point.Y);
                        if (state == null && presentation.HasOverrides) {
                            metadata["data-cfx-formatted-value"] = readout;
                            metadata["data-cfx-label-value"] = presentation.Name;
                        } else if (state == null && chart.Options.ValueUnit != null) metadata["data-cfx-formatted-value"] = readout;
                        if (state == null && chart.Options.HeatmapColorScale == null) {
                            var ratio = ChartHeatmapSurface.Ratio(chart, point.Y, min, max);
                            var status = ChartHeatmapSurface.CellStatus(chart, ratio);
                            if (status != null) metadata["data-cfx-status"] = status;
                            else metadata["data-cfx-level"] = ChartHeatmapSurface.Level(ratio).ToString();
                            if (status != null && chart.Options.PinStateColorsInForcedColors) metadata["data-cfx-pin-state-colors"] = "true";
                        }
                        categoryGroups?.CaptureContext(metadata, rowIndex, columnIndex, series.Name, column);
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
                                      blend.FromRole ?? SvgColorRole.Ramp, blend), cell, id, presentation, point.Y);
                        }
                    }
                }
            }
            MatrixAxes(chart, context, builder, viewport, plot, columns, cellWidth, gap, layout, formatColumn);
            DrawMatrixCategoryGroups(chart, context, builder, categoryGroups, layout, rowLayout, rows.Length, columns.Length, cellWidth, cellHeight, gap, colors);
            if (!categorical && ScaleVisible(context, presentation)) NumericScale(chart, context, builder, layout.Scale, min, max, rows[0].Color, presentation, hasDomain, missing);
        }
    }

    private static bool ScaleVisible(VisualRenderContext context, ChartColorPresentation presentation) => presentation.ShowLegend && context.Frame.ShowLegend;
    private static void NoData(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect bounds) =>
        VisualStateSceneTools.Text(builder, chart.Options.Labels.NoData, bounds, VisualStateSceneTools.TickStyle(chart, context), "no-data", "matrix-no-data", TextAlignment.Center);
}
