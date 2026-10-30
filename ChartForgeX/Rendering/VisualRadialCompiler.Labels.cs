using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialCompiler {
    private static void AddLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        List<RadialLabel> labels, RadialSlice slice, string text, double percent, double angle, double sx, double sy,
        double cx, double cy, double radius, double inner, double envelopeRadius, ChartDataLabelPlacement placement) {
        var outside = placement is ChartDataLabelPlacement.Outside or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right;
        var vertical = placement is ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
        var colors = context.Theme.Resolve(context.ThemeMode);
        var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
        var paintedColor = SlicePaintColor(chart, slice.Color);
        var composed = ChartColorMath.Blend(backdrop, ChartColor.FromRgb(paintedColor.R, paintedColor.G, paintedColor.B), paintedColor.A / 255d);
        var color = outside || vertical ? colors.Foreground : ChartColorMath.AccessibleTextOnBackground(composed);
        var style = Style(chart, context, slice.SeriesIndex, slice.PointIndex, color, context.Theme.Typography.DataLabelSize);
        if (outside) {
            var left = placement == ChartDataLabelPlacement.Left || (placement == ChartDataLabelPlacement.Outside && Math.Cos(angle) < 0);
            var labelX = cx + (left ? -1 : 1) * envelopeRadius * chart.Options.PieOutsideLabelDistanceRatio;
            labels.Add(new RadialLabel(slice, text, angle, labelX, sy + Math.Sin(angle) * envelopeRadius * 1.1, sx, sy, left, style, radius, placement));
            return;
        }
        var labelRadius = chart.Options.PieInsideLabelRadiusRatio.HasValue ? radius * chart.Options.PieInsideLabelRadiusRatio.Value
            : inner > 0 ? (inner + radius) / 2 : radius * 0.66;
        var x = sx + Math.Cos(angle) * labelRadius;
        var y = sy + Math.Sin(angle) * labelRadius;
        if (vertical) {
            x = cx + Math.Cos(angle) * envelopeRadius;
            y = cy + (placement == ChartDataLabelPlacement.Above ? -1 : 1) * envelopeRadius * 1.12;
            labels.Add(new RadialLabel(slice, text, angle, x, y, sx, sy, false, style, radius, placement));
            return;
        }
        var maxWidth = Math.Min(radius * 0.9, Math.Max(0, Math.Min(x - plot.Left, plot.Right - x) * 2));
        // A label must fit the angular width of its slice instead of overlapping a neighbouring value.
        var angularWidth = 2 * labelRadius * Math.Sin(Math.Min(Math.PI / 2, percent * Math.PI));
        var oversized = style.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize;
        bool FitsSlice(TextMetrics metrics) {
            var dx = x - sx; var dy = y - sy;
            var farX = Math.Abs(dx) + metrics.Width / 2; var farY = Math.Abs(dy) + metrics.Height / 2;
            var nearX = Math.Max(0, Math.Abs(dx) - metrics.Width / 2); var nearY = Math.Max(0, Math.Abs(dy) - metrics.Height / 2);
            if (farX * farX + farY * farY > radius * radius || nearX * nearX + nearY * nearY < inner * inner) return false;
            for (var horizontal = -1; horizontal <= 1; horizontal += 2)
                for (var vertical = -1; vertical <= 1; vertical += 2) {
                    var delta = Math.Atan2(dy + vertical * metrics.Height / 2, dx + horizontal * metrics.Width / 2) - angle;
                    delta = (delta + Math.PI * 3) % (Math.PI * 2) - Math.PI;
                    if (Math.Abs(delta) > percent * Math.PI) return false;
                }
            return true;
        }
        style = FitLabelStyle(builder, text, style, Math.Min(maxWidth, angularWidth),
            Math.Min(radius * .5, Math.Max(0, Math.Min(y - plot.Top, plot.Bottom - y) * 2)), FitsSlice);
        var fitted = Fit(text, style, maxWidth, builder);
        var reducedRadius = slice.RadiusRatio.GetValueOrDefault(1) < 1;
        var boundedBand = chart.Series.Count > 1 || chart.Series[slice.SeriesIndex].RadialBand != null || chart.Options.RadialBandAllocation != null || chart.Options.PieInsideLabelRadiusRatio.HasValue || reducedRadius;
        if ((oversized || boundedBand) && !FitsSlice(builder.MeasureText(fitted, style))) {
            if (boundedBand)
                builder.AddDiagnostic(new VisualDiagnostic("radial.band-label-omitted", chart.Options.RadialBandAllocation != null || reducedRadius
                    ? "A radial label cannot fit completely within its resolved slice; its full text remains in semantic metadata."
                    : "A radial label cannot fit completely within its explicit band; its full text remains in semantic metadata."));
            return;
        }
        if (builder.MeasureText(fitted, style).Width > angularWidth) return;
        if (fitted.Length == 0) return;
        style.Alignment = TextAlignment.Center;
        using (builder.PushGroup(SliceId(slice) + "-label-source", "radial-data-label-source", VisualMarkLabel.Metadata(text, SliceId(slice))))
            builder.Text(fitted, x, y - builder.MeasureText(fitted, style).Height / 2 + builder.TextAscent(style), style, "data-label",
                paint: VisualChartPaint.ExplicitDataLabelColor(chart, slice.PointIndex, seriesIndex: slice.SeriesIndex) ? VisualChartPaint.Text(style)
                    : paintedColor.A == 255 ? SvgPaint.Contrast(paintedColor, VisualChartPaint.SeriesRole(chart.Series[slice.SeriesIndex], slice.PointIndex)) : SvgPaint.Literal(style.Color));
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
        foreach (var label in labels) label.Style = FitLabelStyle(builder, label.Text, label.Style,
            Math.Max(0, band.Width / labels.Count - spacing), band.Height);
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
        var overflow = placed.Any(label => label.IsDropped || label.IsEllipsized);
        var leftCount = Enumerable.Range(0, placed.Count).Count(index => !placed[index].IsDropped && Math.Cos(labels[index].Angle) < 0);
        var rightCount = placed.Count(label => !label.IsDropped) - leftCount;
        var leftLane = 0; var rightLane = 0;
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index];
            if (result.IsDropped) continue;
            var label = labels[index];
            var left = Math.Cos(label.Angle) < 0;
            var lane = left ? leftLane++ : rightLane++;
            if (!VerticalConnector(chart, context, builder, plot, label, result.Bounds, radius, cy, extent, above,
                lane, left ? leftCount : rightCount)) { overflow = true; continue; }
            var displayed = label.Style.Clone();
            displayed.FontSize = label.Style.EffectiveFontSize;
            displayed.Baseline = TextBaseline.Normal;
            displayed.TextCase = TextCaseTransform.None;
            displayed.Alignment = TextAlignment.Left;
            using (builder.PushGroup(SliceId(label.Slice) + "-label-source", "radial-data-label-source", VisualMarkLabel.Metadata(label.Text, SliceId(label.Slice))))
                builder.Text(result.Text, result.Bounds.Left, result.Bounds.Top + builder.TextAscent(displayed), displayed,
                    "data-label", SliceId(label.Slice) + "-label", paint: VisualChartPaint.Text(displayed));
        }
        if (overflow)
            builder.AddDiagnostic(new VisualDiagnostic("radial.label-overflow", "Some radial labels were shortened or omitted to fit their measured text and external leaders within the fixed canvas."));
    }

    private static bool VerticalConnector(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        RadialLabel label, ChartRect bounds, double radius, double cy, double extent, bool above, int lane, int laneCount) {
        var halfStroke = chart.Options.DataLabelConnectorStrokeWidth / 2;
        var clearance = Math.Max(2 + halfStroke, context.Theme.Spacing / 2);
        var outerClearance = context.Theme.Spacing - halfStroke - 1;
        if (outerClearance < clearance) return false;
        var cx = plot.Left + plot.Width / 2;
        var cos = Math.Cos(label.Angle); var sin = Math.Sin(label.Angle);
        var side = cos < 0 ? -1 : 1;
        var routeRadius = extent + clearance + (outerClearance - clearance) * (lane + 1) / (laneCount + 1);
        var sideX = cx + side * routeRadius;
        var railY = cy + (above ? -1 : 1) * routeRadius;
        var endX = bounds.Left + bounds.Width / 2;
        var endY = above ? bounds.Bottom : bounds.Top;
        var points = new[] {
            new ChartPoint(label.SliceX + cos * label.Radius, label.SliceY + sin * label.Radius),
            new ChartPoint(cx + cos * (extent + clearance), cy + sin * (extent + clearance)),
            new ChartPoint(sideX, cy + sin * (extent + clearance)),
            new ChartPoint(sideX, railY), new ChartPoint(endX, railY), new ChartPoint(endX, endY)
        };
        // The initial segment exits its own slice; every bypass segment stays outside the exploded envelope.
        if (points.Any(point => point.X - halfStroke < plot.Left || point.X + halfStroke > plot.Right ||
            point.Y - halfStroke < plot.Top || point.Y + halfStroke > plot.Bottom)) return false;
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(points[0].X, points[0].Y) };
        for (var index = 1; index < points.Length - 1; index++) {
            var corner = points[index]; var previous = points[index - 1]; var next = points[index + 1];
            var incomingLength = Distance(previous, corner); var outgoingLength = Distance(corner, next);
            var cut = chart.Options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Curve
                ? Math.Min(Math.Max(0, (clearance - halfStroke - 1) / 2), Math.Min(incomingLength, outgoingLength) / 2) : 0;
            if (cut <= 0) { commands.Add(ChartPathCommand.LineTo(corner.X, corner.Y)); continue; }
            var incoming = new ChartPoint(corner.X + (previous.X - corner.X) * cut / incomingLength,
                corner.Y + (previous.Y - corner.Y) * cut / incomingLength);
            var outgoing = new ChartPoint(corner.X + (next.X - corner.X) * cut / outgoingLength,
                corner.Y + (next.Y - corner.Y) * cut / outgoingLength);
            commands.Add(ChartPathCommand.LineTo(incoming.X, incoming.Y));
            commands.Add(ChartPathCommand.CubicTo(incoming.X + (corner.X - incoming.X) * 2 / 3,
                incoming.Y + (corner.Y - incoming.Y) * 2 / 3, outgoing.X + (corner.X - outgoing.X) * 2 / 3,
                outgoing.Y + (corner.Y - outgoing.Y) * 2 / 3, outgoing.X, outgoing.Y));
        }
        commands.Add(ChartPathCommand.LineTo(endX, endY));
        var color = ChartColorMath.WithOpacity(chart.Options.DataLabelConnectorColor ?? label.Slice.Color, chart.Options.DataLabelConnectorOpacity);
        builder.Path(new ChartPath(commands), stroke: color, strokeWidth: chart.Options.DataLabelConnectorStrokeWidth,
            role: "data-label-connector", id: SliceId(label.Slice) + "-connector", paint: ConnectorPaint(chart, label.Slice, color));
        return true;
    }

    private static double Distance(ChartPoint first, ChartPoint second) =>
        Math.Sqrt((second.X - first.X) * (second.X - first.X) + (second.Y - first.Y) * (second.Y - first.Y));

    private static void DrawOutsideLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, List<RadialLabel> labels, double radius) {
        foreach (var left in new[] { true, false }) {
            var lane = labels.Where(label => label.Left == left).OrderBy(label => label.Y).ToList();
            if (lane.Count == 0) continue;
            foreach (var label in lane) label.Style = FitLabelStyle(builder, label.Text, label.Style,
                left ? label.X - plot.Left - 4 : plot.Right - label.X - 4,
                Math.Max(0, plot.Height / lane.Count - context.Theme.Spacing));
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
                Connector(chart, builder, label.Slice, label.Angle, label.SliceX, label.SliceY, label.Radius, label.X, label.Y, true);
                label.Style.Alignment = left ? TextAlignment.Right : TextAlignment.Left;
                using (builder.PushGroup(SliceId(label.Slice) + "-label-source", "radial-data-label-source", VisualMarkLabel.Metadata(label.Text, SliceId(label.Slice))))
                    builder.Text(text, label.X, label.Y - builder.MeasureText(text, label.Style).Height / 2 + builder.TextAscent(label.Style),
                        label.Style, "data-label", paint: VisualChartPaint.Text(label.Style));
            }
        }
    }

    private static void Connector(Chart chart, VisualSceneBuilder builder, RadialSlice slice, double angle,
        double sx, double sy, double radius, double x, double y, bool sideLane) {
        var path = ConnectorPath(chart, angle, sx, sy, radius, x, y, sideLane);
        var color = ChartColorMath.WithOpacity(chart.Options.DataLabelConnectorColor ?? slice.Color, chart.Options.DataLabelConnectorOpacity);
        builder.Path(path, stroke: color, strokeWidth: chart.Options.DataLabelConnectorStrokeWidth, role: "data-label-connector", id: slice.RadiusRatio.HasValue ? SliceId(slice) + "-connector" : null, paint: ConnectorPaint(chart, slice, color));
    }

    private static ChartPath ConnectorPath(Chart chart, double angle, double sx, double sy, double radius,
        double x, double y, bool sideLane, bool vertical = false) {
        if (vertical) { (sx, sy) = (sy, sx); (x, y) = (y, x); angle = Math.PI / 2 - angle; }
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
        var path = new ChartPath(commands);
        return vertical ? path.Transpose() : path;
    }

    private static VisualScenePaintBinding ConnectorPaint(Chart chart, RadialSlice slice, ChartColor actual) {
        var source = chart.Options.DataLabelConnectorColor.HasValue ? SvgPaint.Of(chart.Options.DataLabelConnectorColor.Value, SvgColorRole.Series)
            : SlicePaint(chart, slice);
        return VisualChartPaint.Stroke(source.WithOpacity(actual, chart.Options.DataLabelConnectorOpacity));
    }

    private static void DrawCenter(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, double cx, double cy, double inner, double? total, int seriesIndex) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var value = chart.Options.DonutCenterValue ?? (total.HasValue ? ChartNumericFormatter.FormatValue(chart.Options, total.Value) : OverflowValueText);
        var overflowCaption = chart.Options.DonutCenterValue == null && !total.HasValue;
        var displayValue = overflowCaption ? FormatOverflowCenter(false) : value;
        var label = chart.Options.DonutCenterLabel ?? chart.Series[seriesIndex].Name;
        // These describe the hole's content, rather than claiming exact hit boxes for fitted text.
        var bounds = new ChartRect(cx - inner, cy - inner, inner * 2, inner * 2);
        builder.AddRegion(new VisualSemanticRegion("series-" + seriesIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-center-value", "donut-total-label", bounds, value));
        builder.AddRegion(new VisualSemanticRegion("series-" + seriesIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-center-caption", "donut-title", bounds, label));
        var valueStyle = Style(chart, context, seriesIndex, -1, colors.Foreground, Math.Min(context.Theme.Typography.CenterValueSize, inner * 0.44), 700);
        var labelStyle = Style(chart, context, seriesIndex, -1, colors.MutedForeground, Math.Min(context.Theme.Typography.DataLabelSize, inner * 0.25));
        valueStyle.FontSize = Math.Min(valueStyle.FontSize, inner * 0.44);
        labelStyle.FontSize = Math.Min(labelStyle.FontSize, inner * 0.25);
        valueStyle.Alignment = labelStyle.Alignment = TextAlignment.Center;
        var width = inner * 1.6;
        if (overflowCaption) {
            valueStyle.FontSize = ChartTextFitting.FitFontSize(Math.Min(valueStyle.FontSize, VisualTextFitting.MinimumOrdinaryFontSize),
                valueStyle.FontSize, size => {
                    valueStyle.FontSize = size;
                    return builder.MeasureText(displayValue, valueStyle).Width <= width;
                });
            if (builder.MeasureText(displayValue, valueStyle).Width > width) displayValue = FormatOverflowCenter(true);
            if (builder.MeasureText(displayValue, valueStyle).Width > width) {
                displayValue = string.Empty;
                builder.AddDiagnostic(new VisualDiagnostic("radial.center-omitted", "The overflowing total's lower bound cannot fit legibly in the donut hole; its complete value remains in semantic metadata."));
            }
        }
        // A truncated exponent would change the apparent magnitude. Keep overflow bounds complete or omit them.
        var fittedValue = overflowCaption ? displayValue : Fit(displayValue, valueStyle, width, builder);
        var fittedLabel = Fit(label, labelStyle, width, builder);
        var valueHeight = builder.MeasureText(fittedValue, valueStyle).Height;
        var labelHeight = builder.MeasureText(fittedLabel, labelStyle).Height;
        var gap = Math.Min(context.Theme.Spacing, inner * 0.1);
        var top = cy - (valueHeight + gap + labelHeight) / 2;
        using (builder.PushGroup("series-" + seriesIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-center", "donut-center", new Dictionary<string, string> {
            ["data-cfx-center-value"] = value, ["data-cfx-center-caption"] = label, ["aria-label"] = value + "\n" + label
        })) {
            builder.Text(fittedValue, cx, top + builder.TextAscent(valueStyle), valueStyle, "donut-total-label", paint: VisualChartPaint.Text(valueStyle));
            builder.Text(fittedLabel, cx, top + valueHeight + gap + builder.TextAscent(labelStyle), labelStyle, "donut-title", paint: VisualChartPaint.Text(labelStyle));
        }
    }

    private static string Fit(string text, TextStyle style, double width, VisualSceneBuilder builder) =>
        ChartTextFitting.TrimEnd(text, style.FontSize, Math.Max(0, width), (value, _) => builder.MeasureText(value, style).Width);

    private static TextStyle FitLabelStyle(VisualSceneBuilder builder, string text, TextStyle style, double width, double height,
        Func<TextMetrics, bool>? contains = null) =>
        style.EffectiveFontSize <= VisualTextFitting.MaximumOrdinaryFontSize || width <= 0 || height <= 0 ? style :
        VisualTextFitting.FitOversized(builder, style, height, fitted => {
            var metrics = builder.MeasureText(text, fitted);
            return metrics.Width <= width && metrics.Height <= height && (contains == null || contains(metrics));
        });

    private sealed class RadialLabel {
        internal RadialLabel(RadialSlice slice, string text, double angle, double x, double y, double sx, double sy, bool left, TextStyle style, double radius, ChartDataLabelPlacement placement = ChartDataLabelPlacement.Outside) {
            Placement = placement; Slice = slice; Text = text; Angle = angle; X = x; Y = y; SliceX = sx; SliceY = sy; Left = left; Style = style; Radius = radius;
        }
        internal ChartDataLabelPlacement Placement { get; }
        internal RadialSlice Slice { get; }
        internal double Radius { get; }
        internal string Text { get; }
        internal double Angle { get; }
        internal double X { get; }
        internal double Y { get; set; }
        internal double SliceX { get; }
        internal double SliceY { get; }
        internal bool Left { get; }
        internal TextStyle Style { get; set; }
    }
}
