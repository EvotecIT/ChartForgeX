using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    private static void AddLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        List<RadialLabel> labels, RadialSlice slice, string text, double total, double angle, double sx, double sy,
        double cx, double cy, double radius, double inner, ChartDataLabelPlacement placement) {
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
            labels.Add(new RadialLabel(slice, text, angle, x, y, sx, sy, false, style));
            return;
        }
        var maxWidth = Math.Min(radius * 0.9, Math.Max(0, Math.Min(x - plot.Left, plot.Right - x) * 2));
        var fitted = Fit(text, style, maxWidth, builder);
        // A label must fit the angular width of its slice instead of overlapping a neighbouring value.
        var angularWidth = 2 * labelRadius * Math.Sin(Math.Min(Math.PI / 2, slice.Value / total * Math.PI));
        if (builder.MeasureText(fitted, style).Width > angularWidth) return;
        if (fitted.Length == 0) return;
        style.Alignment = TextAlignment.Center;
        builder.Text(fitted, x, y - builder.MeasureText(fitted, style).Height / 2 + builder.TextAscent(style), style, "data-label");
    }

    private static void DrawVerticalLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, List<RadialLabel> labels, double radius, double cy, double maximumOffset, ChartDataLabelPlacement placement) {
        if (labels.Count == 0) return;
        var spacing = context.Theme.Spacing;
        var extent = radius * (1 + maximumOffset);
        var above = placement == ChartDataLabelPlacement.Above;
        var top = above ? plot.Top : Math.Min(plot.Bottom, cy + extent + spacing);
        var bottom = above ? Math.Max(plot.Top, cy - extent - spacing) : plot.Bottom;
        var band = new ChartRect(plot.Left, top, plot.Width, bottom - top);
        // Angular anchors may coincide. Resolve them together with backend measurements, outside every exploded slice.
        var metrics = labels.Select(label => builder.MeasureText(label.Text, label.Style)).ToArray();
        var step = Math.Max(1, Math.Min(band.Width / Math.Max(1, labels.Count), metrics.Max(value => value.Width) + spacing));
        var maximumHeight = metrics.Max(value => value.Height);
        var requests = new List<LabelPlacementRequest>(labels.Count);
        for (var index = 0; index < labels.Count; index++) {
            var label = labels[index];
            var centerY = Math.Min(bottom - maximumHeight / 2, Math.Max(top + maximumHeight / 2, label.Y));
            var candidates = new List<LabelCandidate> { new LabelCandidate(0, 0, .5, .5) };
            for (var shift = 1; shift <= labels.Count; shift++) {
                candidates.Add(new LabelCandidate(shift * step, 0, .5, .5));
                candidates.Add(new LabelCandidate(-shift * step, 0, .5, .5));
            }
            requests.Add(new LabelPlacementRequest(label.Text, new ChartPoint(label.X, centerY), label.Style, candidates) {
                MeasuredSize = metrics[index], AssociatedMarkId = SliceId(label.Slice)
            });
        }
        var placed = new LabelPlacementService().Place(requests, band, null, Math.Max(2, spacing / 2), builder.MeasureText);
        if (placed.Any(label => label.IsDropped || label.IsEllipsized))
            builder.AddDiagnostic(new VisualDiagnostic("radial.label-overflow", "Some radial labels were shortened or omitted to fit their measured positions within the fixed canvas."));
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index];
            if (result.IsDropped) continue;
            var label = labels[index];
            var centerX = result.Bounds.Left + result.Bounds.Width / 2;
            var centerY = result.Bounds.Top + result.Bounds.Height / 2;
            Connector(chart, builder, label.Slice, label.Angle, label.SliceX, label.SliceY, radius, centerX, centerY, false);
            var displayed = label.Style.Clone();
            displayed.FontSize = label.Style.EffectiveFontSize;
            displayed.Baseline = TextBaseline.Normal;
            displayed.TextCase = TextCaseTransform.None;
            displayed.Alignment = TextAlignment.Left;
            builder.Text(result.Text, result.Bounds.Left, result.Bounds.Top + builder.TextAscent(displayed), displayed,
                "data-label", SliceId(label.Slice) + "-label");
        }
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
        // These describe the hole's content, rather than claiming exact hit boxes for fitted text.
        var bounds = new ChartRect(cx - inner, cy - inner, inner * 2, inner * 2);
        builder.AddRegion(new VisualSemanticRegion("series-0-center-value", "donut-total-label", bounds, value));
        builder.AddRegion(new VisualSemanticRegion("series-0-center-caption", "donut-title", bounds, label));
        var valueStyle = Style(chart, context, -1, colors.Foreground, Math.Min(context.Theme.Typography.TitleSize, inner * 0.44), 700);
        var labelStyle = Style(chart, context, -1, colors.MutedForeground, Math.Min(context.Theme.Typography.DataLabelSize, inner * 0.25));
        valueStyle.FontSize = Math.Min(valueStyle.FontSize, inner * 0.44);
        labelStyle.FontSize = Math.Min(labelStyle.FontSize, inner * 0.25);
        valueStyle.Alignment = labelStyle.Alignment = TextAlignment.Center;
        var width = inner * 1.6;
        var fittedValue = Fit(value, valueStyle, width, builder);
        var fittedLabel = Fit(label, labelStyle, width, builder);
        var valueHeight = builder.MeasureText(fittedValue, valueStyle).Height;
        var labelHeight = builder.MeasureText(fittedLabel, labelStyle).Height;
        var gap = Math.Min(context.Theme.Spacing, inner * 0.1);
        var top = cy - (valueHeight + gap + labelHeight) / 2;
        using (builder.PushGroup("series-0-center", "donut-center", new Dictionary<string, string> {
            ["data-cfx-center-value"] = value, ["data-cfx-center-caption"] = label, ["aria-label"] = value + "\n" + label
        })) {
            builder.Text(fittedValue, cx, top + builder.TextAscent(valueStyle), valueStyle, "donut-total-label");
            builder.Text(fittedLabel, cx, top + valueHeight + gap + builder.TextAscent(labelStyle), labelStyle, "donut-title");
        }
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
