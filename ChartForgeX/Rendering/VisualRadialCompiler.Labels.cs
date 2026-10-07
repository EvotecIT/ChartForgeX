using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    private static void AddLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        List<RadialLabel> labels, RadialSlice slice, double total, double angle, double sx, double sy,
        double cx, double cy, double radius, double inner, ChartDataLabelPlacement placement) {
        var text = FormatLabel(chart, slice, total);
        if (string.IsNullOrWhiteSpace(text)) return;
        var outside = placement is ChartDataLabelPlacement.Outside or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right;
        var vertical = placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
        var colors = context.Theme.Resolve(context.ThemeMode);
        var color = outside || vertical ? colors.Foreground : ChartColorMath.AccessibleTextOnBackground(slice.Color);
        var style = Style(chart, context, slice.PointIndex, color, context.Theme.Typography.DataLabelSize);
        if (outside) {
            var left = placement == ChartDataLabelPlacement.Left || (placement == ChartDataLabelPlacement.Outside && Math.Cos(angle) < 0);
            var labelX = cx + (left ? -1 : 1) * radius * chart.Options.PieOutsideLabelDistanceRatio;
            labels.Add(new RadialLabel(slice, text, angle, labelX, sy + Math.Sin(angle) * radius * 1.1, sx, sy, left, style));
            return;
        }
        var labelRadius = inner > 0 ? (inner + radius) / 2 : radius * 0.66;
        var x = sx + Math.Cos(angle) * labelRadius;
        var y = sy + Math.Sin(angle) * labelRadius;
        if (vertical) {
            x = cx + Math.Cos(angle) * radius;
            y = cy + (placement == ChartDataLabelPlacement.Above ? -1 : 1) * radius * 1.12;
        }
        var maxWidth = Math.Min(radius * 0.9, Math.Max(0, Math.Min(x - plot.Left, plot.Right - x) * 2));
        var fitted = Fit(text, style, maxWidth, builder);
        // A label must fit the angular width of its slice instead of overlapping a neighbouring value.
        var angularWidth = 2 * labelRadius * Math.Sin(Math.Min(Math.PI / 2, slice.Value / total * Math.PI));
        if (!vertical && builder.MeasureText(fitted, style).Width > angularWidth) return;
        if (fitted.Length == 0) return;
        if (vertical) Connector(chart, builder, slice, angle, sx, sy, radius, x, y, false);
        style.Alignment = TextAlignment.Center;
        builder.Text(fitted, x, y - builder.MeasureText(fitted, style).Height / 2 + builder.TextAscent(style), style, "data-label");
    }

    private static void DrawOutsideLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, List<RadialLabel> labels, double radius) {
        foreach (var left in new[] { true, false }) {
            var lane = labels.Where(label => label.Left == left).OrderBy(label => label.Y).ToList();
            if (lane.Count == 0) continue;
            var gap = lane.Max(label => builder.MeasureText(label.Text, label.Style).Height) + context.Theme.Spacing;
            var capacity = Math.Max(0, (int)Math.Floor(plot.Height / gap));
            if (lane.Count > capacity) {
                var keep = lane.OrderByDescending(label => label.Slice.Value).Take(capacity).ToArray();
                lane = lane.Where(keep.Contains).ToList();
                builder.AddDiagnostic(new VisualDiagnostic("radial.label-overflow", "Some radial callout labels were omitted because the available height is too small."));
            }
            if (lane.Count == 0) continue;
            var top = plot.Top + gap / 2;
            var bottom = plot.Bottom - gap / 2;
            for (var i = 0; i < lane.Count; i++) {
                lane[i].Y = Math.Min(bottom, Math.Max(top, lane[i].Y));
                if (i > 0) lane[i].Y = Math.Max(lane[i].Y, lane[i - 1].Y + gap);
            }
            var overflow = lane[lane.Count - 1].Y - bottom;
            if (overflow > 0) foreach (var label in lane) label.Y -= overflow;
            foreach (var label in lane) {
                var width = left ? label.X - plot.Left - 4 : plot.Right - label.X - 4;
                var text = Fit(label.Text, label.Style, width, builder);
                if (text.Length == 0) continue;
                Connector(chart, builder, label.Slice, label.Angle, label.SliceX, label.SliceY, radius, label.X, label.Y, true);
                label.Style.Alignment = left ? TextAlignment.Right : TextAlignment.Left;
                builder.Text(text, label.X, label.Y - builder.MeasureText(text, label.Style).Height / 2 + builder.TextAscent(label.Style),
                    label.Style, "data-label");
            }
        }
    }

    private static void Connector(Chart chart, VisualSceneBuilder builder, RadialSlice slice, double angle,
        double sx, double sy, double radius, double x, double y, bool sideLane) {
        var startX = sx + Math.Cos(angle) * radius;
        var startY = sy + Math.Sin(angle) * radius;
        var side = x < sx ? -1 : 1;
        var endX = sideLane ? x - side * 4 : x;
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(startX, startY) };
        if (chart.Options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Curve) {
            commands.Add(ChartPathCommand.CubicTo(startX + side * radius * 0.16, startY,
                endX - side * radius * 0.16, y, endX, y));
        } else if (chart.Options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Elbow) {
            commands.Add(ChartPathCommand.LineTo(sx + Math.Cos(angle) * radius * 1.08, sy + Math.Sin(angle) * radius * 1.08));
            commands.Add(ChartPathCommand.LineTo(endX - side * 8, y));
            commands.Add(ChartPathCommand.LineTo(endX, y));
        } else commands.Add(ChartPathCommand.LineTo(endX, y));
        var color = ChartColorMath.WithOpacity(chart.Options.DataLabelConnectorColor ?? slice.Color, chart.Options.DataLabelConnectorOpacity);
        builder.Path(new ChartPath(commands), stroke: color, strokeWidth: chart.Options.DataLabelConnectorStrokeWidth, role: "data-label-connector");
    }

    private static void DrawCenter(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, double cx, double cy, double inner, double total) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var value = chart.Options.DonutCenterValue ?? ChartNumericFormatter.FormatValue(chart.Options, total);
        var label = chart.Options.DonutCenterLabel ?? chart.Series[0].Name;
        var valueStyle = Style(chart, context, -1, colors.Foreground, Math.Min(context.Theme.Typography.TitleSize, inner * 0.44), 700);
        var labelStyle = Style(chart, context, -1, colors.MutedForeground, Math.Min(context.Theme.Typography.DataLabelSize, inner * 0.25));
        valueStyle.FontSize = Math.Min(valueStyle.FontSize, inner * 0.44);
        labelStyle.FontSize = Math.Min(labelStyle.FontSize, inner * 0.25);
        valueStyle.Alignment = labelStyle.Alignment = TextAlignment.Center;
        var width = inner * 1.6;
        value = Fit(value, valueStyle, width, builder);
        label = Fit(label, labelStyle, width, builder);
        var valueHeight = builder.MeasureText(value, valueStyle).Height;
        var labelHeight = builder.MeasureText(label, labelStyle).Height;
        var gap = Math.Min(context.Theme.Spacing, inner * 0.1);
        var top = cy - (valueHeight + gap + labelHeight) / 2;
        builder.Text(value, cx, top + builder.TextAscent(valueStyle), valueStyle, "donut-total-label");
        builder.Text(label, cx, top + valueHeight + gap + builder.TextAscent(labelStyle), labelStyle, "donut-title");
    }

    private static string Fit(string text, TextStyle style, double width, VisualSceneBuilder builder) =>
        ChartTextFitting.TrimEnd(text, style.FontSize, Math.Max(0, width), (value, _) => builder.MeasureText(value, style).Width);

    private sealed class RadialLabel {
        internal RadialLabel(RadialSlice slice, string text, double angle, double x, double y, double sx, double sy, bool left, TextStyle style) {
            Slice = slice; Text = text; Angle = angle; X = x; Y = y; SliceX = sx; SliceY = sy; Left = left; Style = style;
        }
        internal RadialSlice Slice { get; }
        internal string Text { get; }
        internal double Angle { get; }
        internal double X { get; }
        internal double Y { get; set; }
        internal double SliceX { get; }
        internal double SliceY { get; }
        internal bool Left { get; }
        internal TextStyle Style { get; }
    }
}
