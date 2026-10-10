using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    /// <summary>Fits measured horizontal or upright tangential captions within the actual annular segment.</summary>
    private static void SunburstLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartSunburstModel model, ChartSunburstNode node, ChartColor fill, SvgPaint fillPaint) {
        var style = LabelStyle(chart, context, fill, node.Index);
        var series = chart.Series[0];
        var original = node.Index < series.PointLabels.Count && series.PointLabels[node.Index] != null ? series.PointLabels[node.Index]! : node.Label;
        var metrics = builder.MeasureText(original, style);
        var sweep = node.EndAngle - node.StartAngle;
        var mid = node.StartAngle + sweep / 2;
        var radius = node.Depth == 0 ? 0 : (node.InnerRadius + node.OuterRadius) / 2;
        var x = model.CenterX + Math.Cos(mid) * radius;
        var y = model.CenterY + Math.Sin(mid) * radius;
        var tangent = mid * 180 / Math.PI + 90;
        while (tangent > 90) tangent -= 180;
        while (tangent < -90) tangent += 180;
        if (TryCaption(original, metrics)) return;
        // Wrapping preserves a multiword caption before shortening. Geometry checks
        // still use the actual shaped width and combined line height of that result.
        var wrapped = ChartLabelWrapping.BalancedTwoLine(original, style.EffectiveFontSize, metrics.Width * .6,
            (value, _) => builder.MeasureText(value, style).Width);
        var twoLines = string.Join("\n", wrapped);
        if (wrapped.Length > 1 && TryCaption(twoLines, builder.MeasureText(twoLines, style))) return;
        var elements = StringInfo.ParseCombiningCharacters(original);
        for (var count = elements.Length - 1; count > 0; count--) {
            var shortened = original.Substring(0, elements[count]).TrimEnd() + "…";
            if (!TryCaption(shortened, builder.MeasureText(shortened, style))) continue;
            builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A sunburst label was shortened to fit its segment; complete text remains in source semantics."));
            return;
        }
        builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A sunburst label was omitted to fit its segment; complete text remains in source semantics."));

        bool TryCaption(string text, TextMetrics measured) {
            foreach (var degrees in node.Depth == 0 || Math.Abs(tangent) < .001 ? new[] { 0d } : new[] { 0d, tangent }) {
                if (!SunburstCaptionFits(node, x - model.CenterX, y - model.CenterY, measured, degrees)) continue;
                var paint = VisualChartPaint.ExplicitDataLabelColor(chart, node.Index) ? VisualChartPaint.Text(style)
                    : SvgPaint.Contrast(fill, fillPaint);
                style.Alignment = TextAlignment.Center;
                using (Math.Abs(degrees) < .001 ? null : builder.PushRotation(degrees, x, y))
                    builder.Text(text, x, y - measured.Height / 2 + builder.TextAscent(style), style, "sunburst-label",
                        Id("node-label", node.Index), paint);
                return true;
            }
            return false;
        }
    }

    /// <summary>Checks outer corners, the inner-hole clearance, and angular bounds without reducing type size.</summary>
    private static bool SunburstCaptionFits(ChartSunburstNode node, double x, double y, TextMetrics metrics, double degrees) {
        const double clearance = 2;
        var halfWidth = metrics.Width / 2 + clearance;
        var halfHeight = metrics.Height / 2 + clearance;
        var angle = degrees * Math.PI / 180; var cosine = Math.Cos(angle); var sine = Math.Sin(angle);
        var outer = Math.Max(0, node.OuterRadius - clearance);
        var localX = -x * cosine - y * sine;
        var localY = x * sine - y * cosine;
        var closestX = Math.Max(0, Math.Abs(localX) - halfWidth);
        var closestY = Math.Max(0, Math.Abs(localY) - halfHeight);
        if (node.InnerRadius > 0 && closestX * closestX + closestY * closestY < Math.Pow(node.InnerRadius + clearance, 2)) return false;
        var sweep = node.EndAngle - node.StartAngle; var mid = node.StartAngle + sweep / 2;
        foreach (var horizontal in new[] { -halfWidth, halfWidth }) foreach (var vertical in new[] { -halfHeight, halfHeight }) {
            var cornerX = x + horizontal * cosine - vertical * sine;
            var cornerY = y + horizontal * sine + vertical * cosine;
            if (cornerX * cornerX + cornerY * cornerY > outer * outer) return false;
            var offset = Math.Atan2(cornerY, cornerX) - mid;
            while (offset > Math.PI) offset -= Math.PI * 2;
            while (offset < -Math.PI) offset += Math.PI * 2;
            if (Math.Abs(offset) > sweep / 2 + .000001) return false;
        }
        return true;
    }
}
