using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>One additional value axis during one preparation: a detached axis policy, its fitted domain and its measured column.</summary>
    private sealed class AdditionalAxisState {
        internal AdditionalAxisState(ChartAdditionalValueAxis definition, ChartRange range, ChartColor color) {
            Definition = definition; Axis = definition.Axis.Clone(); Range = range; DataRange = range.Copy(); Color = color;
        }
        internal ChartAdditionalValueAxis Definition { get; }
        /// <summary>A preparation-owned copy; tick fitting records its clean step here, never on the authored axis.</summary>
        internal ChartAxis Axis { get; }
        internal ChartRange Range { get; }
        internal ChartRange DataRange { get; }
        internal ChartColor Color { get; }
        internal bool End => Definition.Placement == ChartAxisPlacement.End;
        /// <summary>Whether the measured frame had room for this axis's column; an omitted axis still scales its series.</summary>
        internal bool Shown { get; set; } = true;
        internal double LabelWidth { get; set; }
        internal ChartMapper? Map { get; set; }
    }

    /// <summary>Resolves every additional axis that has bound series, in side order (inner to outer).</summary>
    private static IReadOnlyList<AdditionalAxisState> ResolveAdditionalAxes(Chart chart, ChartRange primaryRange, ChartStackLayout stacks, VisualThemeColors colors) {
        if (!chart.UsesAdditionalValueAxes) return Array.Empty<AdditionalAxisState>();
        var axes = new List<AdditionalAxisState>();
        foreach (var definition in chart.AdditionalValueAxes) {
            var first = -1;
            for (var index = 0; index < chart.Series.Count && first < 0; index++)
                if (ReferenceEquals(chart.Series[index].AdditionalValueAxis, definition)) first = index;
            if (first < 0) continue;
            var color = definition.Color ?? Color(chart.Series[first], first, colors);
            axes.Add(new AdditionalAxisState(definition, ChartRange.FromAdditionalValueAxis(chart, definition, primaryRange, stacks), color));
        }
        return axes;
    }

    private static AdditionalAxisState? AdditionalAxis(AxisLabelCache labels, ChartSeries series) =>
        series.AdditionalValueAxis == null ? null : labels.Additional.FirstOrDefault(state => ReferenceEquals(state.Definition, series.AdditionalValueAxis));

    private static void IncludeAdditionalValueTicks(AxisLabelCache labels) {
        foreach (var state in labels.Additional) labels.IncludeValueTicks(state.Axis, state.Range.MinY, state.Range.MaxY);
    }

    private static void ResetAdditionalRanges(AxisLabelCache labels) {
        foreach (var state in labels.Additional) state.Range.SetYBounds(state.DataRange.MinY, state.DataRange.MaxY);
    }

    private static void FitAdditionalAxisTicks(Chart chart, VisualSceneBuilder builder, ChartRect plot, TextStyle style, double spacing, AxisLabelCache labels) {
        foreach (var state in labels.Additional.Where(state => state.Axis.Visible))
            FitGeneratedAxisTicks(builder, state.Axis, state.Range.MinY, state.Range.MaxY, plot.Height, false, style, spacing, chart.Options.ValueFormatter, labels);
    }

    /// <summary>
    /// Reserves the stacked columns outside the primary and secondary gutters. Each column is a gap, the axis rule,
    /// a spacing and its widest caption. Columns are admitted inner to outer while their side stays within 35% of the
    /// viewport and both sides together within half of it; an axis without room is omitted rather than overlapped.
    /// </summary>
    private static (double Left, double Right, double TitleHeight, double HalfHeight) MeasureAdditionalAxes(Chart chart, VisualSceneBuilder builder,
        ChartRect viewport, double left, double right, TextStyle style, TextStyle titleStyle, double spacing, AxisLabelCache labels) {
        var title = 0d; var halfHeight = 0d;
        foreach (var state in labels.Additional) {
            state.Shown = false;
            if (!state.Axis.Visible) continue;
            var metrics = TickMetrics(builder, state.Axis, state.Range.MinY, state.Range.MaxY, style, chart.Options.ValueFormatter, labels);
            state.LabelWidth = metrics.Width;
            var inner = state.End ? right : left;
            var column = (inner > 0 ? spacing : 0) + spacing + metrics.Width;
            if (inner + column > viewport.Width * .35 || left + right + column > viewport.Width * .5) continue;
            state.Shown = true;
            if (state.End) right += column; else left += column;
            halfHeight = Math.Max(halfHeight, metrics.Height / 2);
            if (state.Definition.Title.Length > 0) title = Math.Max(title, builder.MeasureText(state.Definition.Title, titleStyle).Height + spacing);
        }
        return (left, right, title, halfHeight);
    }

    private static void DrawAdditionalAxisGrids(VisualSceneBuilder builder, ChartRect plot, AxisLabelCache labels, double opacity, double width,
        double[]? dash, VisualThemeColors colors) {
        foreach (var state in labels.Additional) {
            // Only the primary axis draws grid lines by default; an additional axis opts in through its own grid style.
            if (state.Axis.Grid?.ShowLines != true || state.Map == null) continue;
            var grid = new CartesianAxisGrid(state.Axis, false, opacity, width, dash, colors);
            var ticks = labels.Ticks(state.Axis, state.Range.MinY, state.Range.MaxY);
            grid.Underlay(builder, ticks, state.Range.MinY, state.Range.MaxY, state.Map.Y, plot, true, "grid-additional-y", false);
            foreach (var tick in ticks) grid.Line(builder, plot.Left, state.Map.Y(tick), plot.Right, state.Map.Y(tick), "grid-additional-y");
        }
    }

    /// <summary>Draws each shown additional axis in its stacked column and its coloured title in the side's title row.</summary>
    private static void DrawAdditionalAxes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRange range,
        ChartRange? secondaryRange, bool secondaryShown, VisualThemeColors colors, ChartRect viewport, AxisLabelCache labels, ChartRect labelBounds, TextStyle style) {
        if (labels.Additional.Count == 0) return;
        if (labels.Additional.Any(state => state.Axis.Visible && !state.Shown))
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.additional-axis-omitted",
                "An additional value axis had no room beside the plot and was omitted; its series keep their own scale."));
        var spacing = context.Theme.Spacing;
        var primaryShown = chart.Options.YAxis.Visible && !chart.Options.YAxis.CrossAt.HasValue;
        var leftOffset = primaryShown ? TickMetrics(builder, chart.Options.YAxis, range.MinY, range.MaxY, style, chart.Options.ValueFormatter, labels).Width + spacing : 0;
        var rightOffset = secondaryShown && secondaryRange != null ? TickMetrics(builder, chart.Options.SecondaryYAxis,
            secondaryRange.MinY, secondaryRange.MaxY, style, chart.Options.ValueFormatter, labels).Width + spacing : 0;
        var columns = new List<(AdditionalAxisState State, double Inner, double Outer)>();
        foreach (var state in labels.Additional.Where(state => state.Shown && state.Map != null)) {
            var offset = state.End ? rightOffset : leftOffset;
            if (offset > 0) offset += spacing;
            var edge = state.End ? plot.Right + offset : plot.Left - offset;
            var axisStyle = style.Clone(); axisStyle.Color = state.Color;
            var showLine = state.Axis.HasExplicitLine ? state.Axis.ShowLine : chart.Options.YAxis.ShowLine;
            if (showLine) builder.Line(edge, plot.Top, edge, plot.Bottom, state.Color, context.Theme.AxisStrokeWidth,
                role: "axis-additional-y", paint: VisualChartPaint.Stroke(state.Color, SvgColorRole.Axis));
            var bounds = state.End ? new ChartRect(edge + spacing, viewport.Top, state.LabelWidth, viewport.Height)
                : new ChartRect(edge - spacing - state.LabelWidth, viewport.Top, state.LabelWidth, viewport.Height);
            DrawAxisLabels(builder, chart.Options, state.Axis, labels.Ticks(state.Axis, state.Range.MinY, state.Range.MaxY), state.Map!.Y, false, state.End,
                bounds, axisStyle, spacing, chart.Options.ValueFormatter, labels, plot.Height, role: "axis-additional-y-label");
            var outer = offset + spacing + state.LabelWidth;
            if (state.End) rightOffset = outer; else leftOffset = outer;
            columns.Add((state, offset, outer));
        }
        DrawAdditionalTitles(chart, context, builder, plot, viewport, labelBounds, colors, columns, secondaryShown);
    }

    /// <summary>
    /// Lays out one title row per side, outermost first from the outer edge inward, so the order of the titles matches
    /// the order of the columns. The primary or secondary title joins its side's row as the innermost entry.
    /// </summary>
    private static void DrawAdditionalTitles(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRect viewport,
        ChartRect labelBounds, VisualThemeColors colors, List<(AdditionalAxisState State, double Inner, double Outer)> columns, bool secondaryShown) {
        var spacing = context.Theme.Spacing;
        var height = Math.Max(0, labelBounds.Top - viewport.Top - spacing);
        var titleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.FontWithWeight(context.Theme.Typography.AxisTitleWeight),
            FontSize = context.Theme.Typography.AxisTitleSize, Color = colors.MutedForeground });
        var titled = new[] {
            chart.Options.YAxis.Visible && chart.YAxisTitle.Length > 0 || columns.Any(column => !column.State.End && column.State.Definition.Title.Length > 0),
            secondaryShown && chart.SecondaryYAxisTitle.Length > 0 || columns.Any(column => column.State.End && column.State.Definition.Title.Length > 0)
        };
        foreach (var end in new[] { false, true }) {
            var side = columns.Where(column => column.State.End == end && column.State.Definition.Title.Length > 0).ToList();
            if (side.Count == 0) continue;
            // Each row may reach the plot centre when the other side also has titles, and the opposite edge otherwise.
            var center = titled[end ? 0 : 1] ? plot.Left + plot.Width / 2 + (end ? spacing / 2 : -spacing / 2) : end ? viewport.Left : viewport.Right;
            var outerEdge = end ? Math.Min(viewport.Right, plot.Right + columns.Where(column => column.State.End).Max(column => column.Outer))
                : Math.Max(viewport.Left, plot.Left - columns.Where(column => !column.State.End).Max(column => column.Outer));
            var entries = side.OrderByDescending(column => column.Outer)
                .Select(column => (Text: column.State.Definition.Title, Color: (ChartColor?)column.State.Color, Role: "axis-additional-y-title")).ToList();
            var inner = end ? secondaryShown ? chart.SecondaryYAxisTitle : string.Empty : chart.Options.YAxis.Visible ? chart.YAxisTitle : string.Empty;
            if (inner.Length > 0) entries.Add((inner, null, end ? "axis-secondary-y-title" : "axis-y-title"));
            var cursor = outerEdge;
            foreach (var entry in entries) {
                var width = builder.MeasureText(entry.Text, titleStyle).Width;
                var limit = end ? cursor - center : center - cursor;
                width = Math.Max(0, Math.Min(width, limit));
                var bounds = end ? new ChartRect(cursor - width, viewport.Top, width, height) : new ChartRect(cursor, viewport.Top, width, height);
                DrawAxisTitle(chart, context, builder, entry.Text, bounds, colors, end ? TextAlignment.Right : TextAlignment.Left, entry.Role, color: entry.Color);
                cursor = end ? cursor - width - spacing * 2 : cursor + width + spacing * 2;
            }
        }
    }

    private static bool HasAdditionalTitles(AxisLabelCache labels, bool end) =>
        labels.Additional.Any(state => state.Shown && state.End == end && state.Definition.Title.Length > 0);
}
