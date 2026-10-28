using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    /// <summary>Captures matrix axis paths once, independently from source cell values and physical reversal.</summary>
    private sealed class MatrixCategoryGroups {
        private MatrixCategoryGroups(ChartCategoryGroupPlan? rows, ChartCategoryGroupPlan? columns) {
            Rows = rows; Columns = columns;
            RowSpans = rows?.CreateSlotSpans() ?? Array.Empty<ChartCategoryGroupSpan>();
            ColumnSpans = columns?.CreateSlotSpans() ?? Array.Empty<ChartCategoryGroupSpan>();
        }
        internal ChartCategoryGroupPlan? Rows { get; }
        internal ChartCategoryGroupPlan? Columns { get; }
        internal IReadOnlyList<ChartCategoryGroupSpan> RowSpans { get; }
        internal IReadOnlyList<ChartCategoryGroupSpan> ColumnSpans { get; }
        internal static MatrixCategoryGroups? Capture(Chart chart, ChartSeries[] rows, double[] columns) {
            if (rows[0].Kind != ChartSeriesKind.Heatmap) return null;
            var x = ChartCategoryGroupPlan.TryCreateMatrix(chart.Options.XAxis, columns, horizontal: false);
            var y = ChartCategoryGroupPlan.TryCreateMatrix(chart.Options.YAxis,
                Enumerable.Range(1, rows.Length).Select(index => (double)index), horizontal: true);
            return x == null && y == null ? null : new MatrixCategoryGroups(y, x);
        }
        internal string RowText(int index, string fallback) => Rows == null ? fallback : Rows.Bindings[index].Text;
        internal string Describe(int row, int column, string rowName, string columnName) =>
            Path(Rows, row, rowName) + ", " + Path(Columns, column, columnName);
        private static string Path(ChartCategoryGroupPlan? plan, int index, string fallback) => plan == null ? fallback
            : string.Join(" / ", plan.Bindings[index].Groups.Concat(new[] { plan.Bindings[index].Text }));
        internal void CaptureContext(Dictionary<string, string> metadata, int row, int column, string rowName, string columnName) {
            metadata["data-cfx-category-label"] = Describe(row, column, rowName, columnName);
            CaptureAxis(metadata, "row", Rows, row); CaptureAxis(metadata, "column", Columns, column);
        }
        private static void CaptureAxis(Dictionary<string, string> metadata, string name, ChartCategoryGroupPlan? plan, int index) {
            if (plan == null) return;
            var leaf = plan.Bindings[index]; var prefix = "data-cfx-" + name + "-category";
            metadata[prefix] = leaf.Text; metadata[prefix + "-group-count"] = VisualStateSceneTools.Number(leaf.Groups.Count);
            for (var depth = 0; depth < leaf.Groups.Count; depth++)
                metadata[prefix + "-group-" + VisualStateSceneTools.Number(depth)] = leaf.Groups[depth];
        }
    }

    private static MatrixLayout MeasureMatrix(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ChartSeries[] rows, double[] columns, bool categorical, Func<double, string> formatColumn, ChartColorPresentation presentation,
        bool hasDomain, double min, double max, bool missing, MatrixCategoryGroups? groups) {
        if (groups == null) return MeasureFlatMatrix(chart, context, builder, viewport, rows, columns, categorical,
            formatColumn, presentation, hasDomain, min, max, missing);
        var rowLabels = rows.Select((row, index) => groups.RowText(index, row.Name)).ToArray();
        var probe = MeasureFlatMatrix(chart, context, builder, viewport, rows, columns, categorical,
            formatColumn, presentation, hasDomain, min, max, missing, rowLabels);
        var style = VisualStateSceneTools.TickStyle(chart, context); var spacing = context.Theme.Spacing;
        var rowSizes = groups.Rows == null ? Array.Empty<double>()
            : VisualCategoryGroupScene.Measure(builder, groups.Rows, groups.RowSpans, style, spacing, 0, rows.Length,
                probe.Plot.Height, chart.Options.ShowAxes && chart.Options.YAxis.Visible ? viewport.Width * .26 : 0);
        var rowWidth = rowSizes.Sum();
        var columnSizes = ColumnSizes(Math.Max(0, probe.Plot.Width - rowWidth));
        var columnHeight = columnSizes.Sum();
        var flat = Measure(columnHeight);
        var refit = ColumnSizes(flat.Plot.Width);
        if (!refit.SequenceEqual(columnSizes)) { columnSizes = refit; columnHeight = columnSizes.Sum(); flat = Measure(columnHeight); }
        var rowBand = new ChartRect(viewport.Left, flat.Plot.Top, rowWidth, flat.Plot.Height);
        var columnBand = new ChartRect(flat.Plot.Left, flat.Plot.Bottom + flat.LabelHeight, flat.Plot.Width, columnHeight);
        var scale = new ChartRect(flat.Scale.X, flat.Scale.Y + columnHeight, flat.Scale.Width, flat.Scale.Height);
        return new MatrixLayout(flat.Plot, scale, flat.LabelHeight, flat.TitleHeight, flat.XTitle, flat.TitleStyle,
            flat.LabelAngle, flat.ColumnLabels, rowBand, columnBand, rowSizes, columnSizes);

        double[] ColumnSizes(double track) => groups.Columns == null ? Array.Empty<double>()
            : VisualCategoryGroupScene.Measure(builder, groups.Columns, groups.ColumnSpans, style, spacing,
                0, columns.Length, track, chart.Options.ShowAxes && chart.Options.XAxis.Visible && chart.Options.ShowHeatmapColumnLabels ? viewport.Height * .2 : 0);
        MatrixLayout Measure(double height) => MeasureFlatMatrix(chart, context, builder,
            new ChartRect(viewport.Left + rowWidth, viewport.Top, Math.Max(0, viewport.Width - rowWidth), Math.Max(0, viewport.Height - height)),
            rows, columns, categorical, formatColumn, presentation, hasDomain, min, max, missing, rowLabels);
    }

    private static void DrawMatrixCategoryGroups(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        MatrixCategoryGroups? groups, MatrixLayout layout, ChartHeatmapRowLayout rowLayout, int rowCount, int columnCount,
        double cellWidth, double cellHeight, double gap, VisualThemeColors colors) {
        if (groups == null) return;
        if (groups.Columns != null) VisualCategoryGroupScene.Draw(chart, context, builder, groups.Columns, groups.ColumnSpans,
            ColumnBoundary, 0, columnCount, layout.ColumnGroupBand, colors, layout.ColumnGroupSizes,
            diagnosticCode: "matrix.category-group-overflow", coordinateSpace: "source-slot", includeLabels: chart.Options.ShowHeatmapColumnLabels);
        if (groups.Rows != null) VisualCategoryGroupScene.Draw(chart, context, builder, groups.Rows, groups.RowSpans,
            RowBoundary, 0, rowCount, layout.RowGroupBand, colors, layout.RowGroupSizes,
            diagnosticCode: "matrix.category-group-overflow", coordinateSpace: "source-slot");

        double ColumnBoundary(double slot) {
            var x = slot <= 0 ? layout.Plot.Left : slot >= columnCount ? layout.Plot.Right
                : layout.Plot.Left + slot * (cellWidth + gap) - gap / 2;
            return ReverseColumns(chart) ? layout.Plot.Left + layout.Plot.Right - x : x;
        }
        double RowBoundary(double slot) {
            var index = (int)slot;
            var y = index <= 0 ? rowLayout.RowTop(layout.Plot.Top, 0, cellHeight, gap)
                : index >= rowCount ? rowLayout.RowTop(layout.Plot.Top, rowCount - 1, cellHeight, gap) + cellHeight
                : (rowLayout.RowTop(layout.Plot.Top, index - 1, cellHeight, gap) + cellHeight
                    + rowLayout.RowTop(layout.Plot.Top, index, cellHeight, gap)) / 2;
            return ReverseRows(chart) ? layout.Plot.Top + layout.Plot.Bottom - y : y;
        }
    }

    private static bool ReverseColumns(Chart chart) => chart.Series[0].Kind == ChartSeriesKind.Heatmap && chart.Options.XAxis.Reversed;
    private static bool ReverseRows(Chart chart) => chart.Series[0].Kind == ChartSeriesKind.Heatmap && chart.Options.YAxis.Reversed;
    private static double MirrorTop(ChartRect plot, double top, double height) => plot.Top + plot.Bottom - top - height;
}
