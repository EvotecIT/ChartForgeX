using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Shares measured axis text fitting and painting across Cartesian and numeric radial layouts.</summary>
internal static class VisualAxisText {
    /// <summary>Resolves equivalent finite angles before trigonometry and scene painting.</summary>
    internal static double Angle(double degrees) => degrees % 360;

    /// <summary>Returns the axis-aligned footprint of the text rotated about its center.</summary>
    internal static TextMetrics RotatedMetrics(TextMetrics metrics, double degrees) {
        var angle = Angle(degrees);
        if (angle == 0) return metrics;
        var radians = angle * Math.PI / 180;
        var cosine = Math.Abs(Math.Cos(radians)); var sine = Math.Abs(Math.Sin(radians));
        return new TextMetrics(metrics.Width * cosine + metrics.Height * sine,
            metrics.Width * sine + metrics.Height * cosine, metrics.LineHeight);
    }

    /// <summary>Paints the displayed text within the same footprint used for full and shortened placement.</summary>
    internal static void Draw(VisualSceneBuilder builder, PlacedLabel label, string role, string? id = null, SvgPaint? paint = null) {
        if (label.IsDropped) return;
        var style = label.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize;
        style.Baseline = TextBaseline.Normal; style.TextCase = TextCaseTransform.None; style.Alignment = TextAlignment.Left;
        var angle = Angle(label.Request.RotationDegrees);
        paint ??= VisualChartPaint.Text(style);
        if (angle == 0) {
            builder.Text(label.Text, label.Bounds.Left, label.Bounds.Top + builder.TextAscent(style), style, role, id, paint: paint);
            return;
        }
        var metrics = builder.MeasureText(label.Text, style);
        var cx = label.Bounds.Left + label.Bounds.Width / 2;
        var cy = label.Bounds.Top + label.Bounds.Height / 2;
        using (builder.PushRotation(angle, cx, cy))
            builder.Text(label.Text, cx - metrics.Width / 2, cy - metrics.Height / 2 + builder.TextAscent(style), style, role, id, paint: paint);
    }

    /// <summary>Fits a title inside its reserved strip while retaining its complete descriptive text.</summary>
    internal static void Title(VisualSceneBuilder builder, string text, ChartRect bounds, TextStyle style, string role,
        string? id, string diagnosticCode, string diagnosticMessage) {
        builder.AddRegion(new VisualSemanticRegion(id ?? role, role, bounds,
            TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture)));
        var fraction = style.Alignment == TextAlignment.Right ? 1 : style.Alignment == TextAlignment.Center ? .5 : 0;
        var request = new LabelPlacementRequest(text, new ChartPoint(bounds.Left + bounds.Width * fraction, bounds.Top), style,
            new[] { new LabelCandidate(0, 0, fraction, 0) }) { MeasuredSize = builder.MeasureText(text, style) };
        var label = new LabelPlacementService().Place(new[] { request }, bounds, null, 2, builder.MeasureText)[0];
        if (label.IsDropped || label.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic(diagnosticCode, diagnosticMessage));
        Draw(builder, label, role, id);
    }
}
