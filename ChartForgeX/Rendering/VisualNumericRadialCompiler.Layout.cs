using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualNumericRadialCompiler {
    private static RadialSeriesGeometry Layout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        IReadOnlyList<string> categories, Dictionary<ChartAxisSide, string[]> valueLabels, bool bars) {
        var style = TickStyle(chart, context);
        var texts = chart.Options.ShowAxes
            ? (chart.Options.XAxis.Visible ? categories : Array.Empty<string>())
                .Concat(valueLabels.Where(pair => Axis(chart, pair.Key).Visible).SelectMany(pair => pair.Value)).ToArray()
            : Array.Empty<string>();
        var width = texts.Select(text => builder.MeasureText(text, style).Width).DefaultIfEmpty(0).Max();
        var height = texts.Select(text => builder.MeasureText(text, style).Height).DefaultIfEmpty(0).Max();
        var radius = Math.Max(0, Math.Min(plot.Width / 2 - Math.Min(plot.Width * .22, width) - context.Theme.Spacing,
            plot.Height / 2 - Math.Min(plot.Height * .18, height) - context.Theme.Spacing));
        // Each visible radial-bar scale gets a measured perimeter lane. Reserving the
        // widest caption's diagonal separates its ink at every tick angle and keeps
        // the outer lane inside the same SVG/PNG viewport, including styled text.
        var lane = bars && chart.Options.ShowAxes && valueLabels.Count > 1 && valueLabels.Keys.All(side => Axis(chart, side).Visible)
            ? valueLabels.Values.SelectMany(labels => labels).Select(text => builder.MeasureText(text, style))
                .Select(metrics => Math.Sqrt(metrics.Width * metrics.Width + metrics.Height * metrics.Height)).DefaultIfEmpty(0).Max() + context.Theme.Spacing
            : 0;
        radius = Math.Max(0, radius - lane);
        var options = chart.Options.RadialGeometry;
        return new RadialSeriesGeometry(plot.Left + plot.Width / 2, plot.Top + plot.Height / 2, radius,
            radius * options.InnerRadiusRatio, options.StartAngleDegrees % 360 * Math.PI / 180,
            (options.EndAngleDegrees - options.StartAngleDegrees) * Math.PI / 180, lane);
    }

    private static RadialSeriesMark Mark(Chart chart, RadialSeriesGeometry geometry, RadialValueScale scale, double baseline, double end,
        int category, int categoryCount, (int Count, int Position) slot, bool bars) {
        var options = chart.Options.RadialGeometry;
        var physical = chart.Options.XAxis.Reversed ? categoryCount - 1 - category : category;
        var band = (bars ? geometry.Outer - geometry.Inner : geometry.Sweep) / categoryCount;
        var occupied = band * (1 - options.CategorySpacing);
        var stride = occupied / slot.Count;
        var width = stride * (1 - options.SeriesSpacing);
        var position = physical * band + (band - occupied) / 2 + slot.Position * stride + (stride - width) / 2;
        var baseRatio = scale.Normalize(baseline); var endRatio = scale.Normalize(end);
        var start = bars ? geometry.Start + Math.Min(baseRatio, endRatio) * geometry.Sweep : geometry.Start + position;
        var sweep = bars ? Math.Abs(endRatio - baseRatio) * geometry.Sweep : width;
        var inner = bars ? geometry.Inner + position : geometry.Inner + Math.Min(baseRatio, endRatio) * (geometry.Outer - geometry.Inner);
        var outer = bars ? inner + width : geometry.Inner + Math.Max(baseRatio, endRatio) * (geometry.Outer - geometry.Inner);
        var endAngle = bars ? geometry.Start + endRatio * geometry.Sweep : start + sweep / 2;
        var endRadius = bars ? (inner + outer) / 2 : geometry.Inner + endRatio * (geometry.Outer - geometry.Inner);
        var center = On(geometry, start + sweep / 2, (inner + outer) / 2);
        var direction = endRatio >= baseRatio ? 1 : -1;
        var offset = bars ? new ChartPoint(-Math.Sin(endAngle) * direction, Math.Cos(endAngle) * direction)
            : new ChartPoint(Math.Cos(endAngle) * direction, Math.Sin(endAngle) * direction);
        return new RadialSeriesMark(start, sweep, inner, outer, On(geometry, endAngle, endRadius), center, offset,
            baseline < scale.Minimum || baseline > scale.Maximum || end < scale.Minimum || end > scale.Maximum,
            ChartSlicePathGeometry.HasEncodedFillArea(geometry.Cx, geometry.Cy, outer, inner, start, sweep, options.CornerRadius));
    }

    private static void Grid(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, RadialSeriesGeometry geometry,
        string[] categoryLabels, ChartBarCoordinateKey[] categories, Dictionary<ChartAxisSide, RadialValueScale> scales,
        Dictionary<ChartAxisSide, string[]> tickLabels, bool bars, List<RadialSeriesLabel> labels) {
        var colors = context.Theme.Resolve(context.ThemeMode); var style = TickStyle(chart, context); var gap = context.Theme.Spacing;
        foreach (var pair in scales) {
            var axis = Axis(chart, pair.Key); var scale = pair.Value;
            using (builder.PushGroup(null, "radial-value-axis", new Dictionary<string, string> {
                ["data-cfx-axis"] = pair.Key.ToString().ToLowerInvariant(), ["data-cfx-min"] = N(scale.Minimum), ["data-cfx-max"] = N(scale.Maximum),
                ["data-cfx-reversed"] = axis.Reversed ? "true" : "false"
            })) {
                for (var index = 0; index < scale.Ticks.Count; index++) {
                    var value = scale.Ticks[index]; var ratio = scale.Normalize(value);
                    if (bars && geometry.Sweep == Math.PI * 2 && value == scale.Maximum) continue;
                    var angle = geometry.Start + (bars ? ratio * geometry.Sweep : pair.Key == ChartAxisSide.Secondary ? geometry.Sweep : 0);
                    var radius = bars ? geometry.Outer + (pair.Key == ChartAxisSide.Secondary ? geometry.TickLane : 0)
                        : geometry.Inner + ratio * (geometry.Outer - geometry.Inner);
                    if (chart.Options.ShowGrid && pair.Key == ChartAxisSide.Primary) {
                        if (bars) {
                            var first = On(geometry, angle, geometry.Inner); var last = On(geometry, angle, geometry.Outer);
                            builder.Line(first.X, first.Y, last.X, last.Y, colors.Grid, context.Theme.GridStrokeWidth, "radial-value-grid", paint: VisualChartPaint.Stroke(colors.Grid, SvgColorRole.Grid));
                        } else VisualRadialPrimitives.Arc(builder, geometry.Cx, geometry.Cy, radius, context.Theme.GridStrokeWidth,
                            geometry.Start, geometry.Sweep, colors.Grid, "radial-value-grid", paint: SvgPaint.Of(colors.Grid, SvgColorRole.Grid));
                    }
                    if (chart.Options.ShowAxes && axis.Visible) {
                        var anchor = On(geometry, angle, radius + (bars ? gap : 0));
                        var text = tickLabels[pair.Key][index];
                        var direction = pair.Key == ChartAxisSide.Secondary ? 1 : -1;
                        var dx = -Math.Sin(angle) * direction; var dy = Math.Cos(angle) * direction;
                        AddLabel(builder, labels, text, anchor, style, "radial-value-label-" + pair.Key + "-" + index, "radial-value-label", plot,
                            bars ? new LabelCandidate(0, 0, Math.Cos(angle) > .3 ? 0 : Math.Cos(angle) < -.3 ? 1 : .5, Math.Sin(angle) > .3 ? 0 : Math.Sin(angle) < -.3 ? 1 : .5)
                                : new LabelCandidate(dx * gap / 2, dy * gap / 2, dx > .3 ? 0 : dx < -.3 ? 1 : .5, dy > .3 ? 0 : dy < -.3 ? 1 : .5), 90);
                    }
                }
                if (chart.Options.ShowAxes && axis.Visible && axis.ShowLine) {
                    if (bars) VisualRadialPrimitives.Arc(builder, geometry.Cx, geometry.Cy,
                        geometry.Outer + (pair.Key == ChartAxisSide.Secondary ? geometry.TickLane : 0), context.Theme.AxisStrokeWidth,
                        geometry.Start, geometry.Sweep, colors.Axis, "radial-value-rule", paint: SvgPaint.Of(colors.Axis, SvgColorRole.Axis));
                    else {
                        var angle = geometry.Start + (pair.Key == ChartAxisSide.Secondary ? geometry.Sweep : 0);
                        var first = On(geometry, angle, geometry.Inner); var last = On(geometry, angle, geometry.Outer);
                        builder.Line(first.X, first.Y, last.X, last.Y, colors.Axis, context.Theme.AxisStrokeWidth, "radial-value-rule", paint: VisualChartPaint.Stroke(colors.Axis, SvgColorRole.Axis));
                    }
                }
            }
        }
        if (!chart.Options.ShowAxes || !chart.Options.XAxis.Visible) return;
        for (var index = 0; index < categories.Length; index++) {
            var physical = chart.Options.XAxis.Reversed ? categories.Length - 1 - index : index;
            var angle = bars ? geometry.Start : geometry.Start + (physical + .5) * geometry.Sweep / categories.Length;
            var radius = bars ? geometry.Inner + (physical + .5) * (geometry.Outer - geometry.Inner) / categories.Length : geometry.Outer + gap;
            var anchor = On(geometry, angle, radius);
            AddLabel(builder, labels, categoryLabels[index], anchor, style, "radial-category-label-" + index, "radial-category-label", plot,
                bars ? BeforeStartRay(angle, gap)
                    : new LabelCandidate(0, 0, Math.Cos(angle) > .3 ? 0 : Math.Cos(angle) < -.3 ? 1 : .5, Math.Sin(angle) > .3 ? 0 : Math.Sin(angle) < -.3 ? 1 : .5), 100);
        }
    }

    private static ChartPoint On(RadialSeriesGeometry geometry, double angle, double radius) =>
        new(geometry.Cx + Math.Cos(angle) * radius, geometry.Cy + Math.Sin(angle) * radius);
    private static LabelCandidate BeforeStartRay(double angle, double gap) {
        var dx = Math.Sin(angle); var dy = -Math.Cos(angle);
        return new LabelCandidate(dx * gap, dy * gap, dx > .3 ? 0 : dx < -.3 ? 1 : .5, dy > .3 ? 0 : dy < -.3 ? 1 : .5);
    }
    private static TextStyle TickStyle(Chart chart, VisualRenderContext context) => chart.Options.TickLabelStyle.Resolve(new TextStyle {
        Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = context.Theme.Resolve(context.ThemeMode).MutedForeground
    });

    private sealed class RadialSeriesGeometry {
        internal RadialSeriesGeometry(double cx, double cy, double outer, double inner, double start, double sweep, double tickLane) {
            Cx = cx; Cy = cy; Outer = outer; Inner = inner; Start = start; Sweep = sweep; TickLane = tickLane;
        }
        internal double Cx { get; } internal double Cy { get; } internal double Outer { get; }
        internal double Inner { get; } internal double Start { get; } internal double Sweep { get; }
        internal double TickLane { get; }
    }
    private sealed class RadialSeriesMark {
        internal RadialSeriesMark(double start, double sweep, double inner, double outer, ChartPoint end, ChartPoint center, ChartPoint offset, bool clipped, bool painted) {
            Start = start; Sweep = sweep; Inner = inner; Outer = outer; End = end; Center = center; Offset = offset; Clipped = clipped; Painted = painted;
        }
        internal double Start { get; } internal double Sweep { get; } internal double Inner { get; } internal double Outer { get; }
        internal ChartPoint End { get; } internal ChartPoint Center { get; } internal ChartPoint Offset { get; } internal bool Clipped { get; }
        internal bool Painted { get; }
    }
}
