using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualRadialProgressCompiler {
    private const double CenterFontSizePerRadius = .7 / 1.3;

    private static TextStyle CenterStyle(Chart chart, VisualRenderContext context, ChartColor color, double size, double radius, int weight = 400) =>
        VisualRadialPrimitives.Style(chart, context, color, Math.Min(size, Math.Max(.1, radius * CenterFontSizePerRadius)), weight);

    /// <summary>Reserves the smallest circular hole supporting complete measured rows and the existing typography ceiling.</summary>
    private static double RequestedCenterRadius(VisualSceneBuilder builder, VisualRenderContext context, string value, string caption,
        TextStyle valueStyle, TextStyle captionStyle, VisualSceneTextFace? valueFace = null, VisualSceneTextFace? captionFace = null) {
        var valueMetrics = valueFace == null ? builder.MeasureText(value, valueStyle) : valueFace.Prepare(value, valueStyle.EffectiveFontSize).Metrics;
        var captionMetrics = captionFace == null ? builder.MeasureText(caption, captionStyle) : captionFace.Prepare(caption, captionStyle.EffectiveFontSize).Metrics;
        double lower = 0, upper = Math.Max(valueMetrics.Width, captionMetrics.Width) / 2
            + valueMetrics.Height + captionMetrics.Height + context.Theme.Spacing / 3;
        for (var iteration = 0; iteration < 40; iteration++) {
            var radius = (lower + upper) / 2;
            if (TryCenterRows(valueMetrics, captionMetrics, radius, context.Theme.Spacing, out _, out _)) upper = radius;
            else lower = radius;
        }
        var typographyRadius = Math.Max(context.Theme.Typography.CenterValueSize, context.Theme.Typography.DataLabelSize) / CenterFontSizePerRadius;
        return Math.Max(upper, typographyRadius) + .000001;
    }

    private static void Center(VisualSceneBuilder builder, VisualRenderContext context, double cx, double cy, double radius, string value, string caption,
        TextStyle valueStyle, TextStyle captionStyle, string role, VisualSceneTextFace? valueFace = null, VisualSceneTextFace? captionFace = null) {
        var valueMetrics = valueFace == null ? builder.MeasureText(value, valueStyle) : valueFace.Prepare(value, valueStyle.EffectiveFontSize).Metrics;
        var captionMetrics = captionFace == null ? builder.MeasureText(caption, captionStyle) : captionFace.Prepare(caption, captionStyle.EffectiveFontSize).Metrics;
        var valueHeight = valueMetrics.Height; var captionHeight = captionMetrics.Height;
        if (!TryCenterRows(valueMetrics, captionMetrics, radius, context.Theme.Spacing, out var top, out var gap)) {
            // Constrained holes retain the existing bounded rows and descriptive-source fallback.
            var availableHeight = Math.Max(0, radius * 1.6);
            captionHeight = Math.Min(captionHeight, availableHeight * .45);
            valueHeight = Math.Min(valueHeight, Math.Max(0, availableHeight - captionHeight - gap));
            top = -(valueHeight + gap + captionHeight) / 2;
        }
        Draw(value, cy + top, valueHeight, valueStyle, role + "-value", "series-0-center-value", valueFace);
        Draw(caption, cy + top + valueHeight + gap, captionHeight, captionStyle, role + "-title", "series-0-center-caption", captionFace);

        void Draw(string text, double y, double height, TextStyle style, string textRole, string id, VisualSceneTextFace? face) {
            var offset = Math.Max(Math.Abs(y - cy), Math.Abs(y + height - cy));
            var width = 2 * Math.Sqrt(Math.Max(0, radius * radius - offset * offset));
            VisualRadialPrimitives.Text(builder, text, new ChartRect(cx - width / 2, y, width, height), style, textRole, id, capturedFace: face);
        }
    }

    /// <summary>Finds a bounded vertical shift so each row's outermost corners stay inside the circle.</summary>
    private static bool TryCenterRows(TextMetrics value, TextMetrics caption, double radius, double spacing, out double top, out double gap) {
        gap = Math.Min(spacing / 3, radius * .1);
        top = -(value.Height + gap + caption.Height) / 2;
        if (value.Width > radius * 2 || caption.Width > radius * 2) return false;
        var valueExtent = Math.Sqrt(Math.Max(0, radius * radius - value.Width * value.Width / 4));
        var captionExtent = Math.Sqrt(Math.Max(0, radius * radius - caption.Width * caption.Width / 4));
        var captionOffset = value.Height + gap;
        var lower = Math.Max(-valueExtent, -captionExtent - captionOffset);
        var upper = Math.Min(valueExtent - value.Height, captionExtent - caption.Height - captionOffset);
        if (lower > upper) return false;
        // Reconstructing the circle chord at an exact limit can lose a floating-point ULP.
        var clearance = Math.Min(.000001, (upper - lower) / 2);
        top = Math.Max(lower + clearance, Math.Min(upper - clearance, top));
        return true;
    }
}
