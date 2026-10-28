using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>One captured badge rectangle and text placement shared by native and legacy serialization sinks.</summary>
internal sealed class NativeBadgePaintPlan {
    private NativeBadgePaintPlan(ChartRect surface, ChartRect text, double radius, double baseline) { Surface = surface; Text = text; Radius = radius; Baseline = baseline; }
    internal ChartRect Surface { get; }
    internal ChartRect Text { get; }
    internal double Radius { get; }
    internal double Baseline { get; }
    // The table adapter owns its incumbent treatment arithmetic. Capture its exact placement
    // once so scene and legacy SVG serialization cannot make different badge decisions.
    internal static NativeBadgePaintPlan Table(double x, double y, double width, double height, double badgeWidth,
        double badgeHeight, double bodyScale, double presentationScale, TextAlignment alignment) {
        var left = alignment == TextAlignment.Center ? x + (width - badgeWidth) / 2 : alignment == TextAlignment.Right ? x + width - badgeWidth : x;
        var top = y + Math.Max(0, (height - badgeHeight) / 2);
        return new NativeBadgePaintPlan(new ChartRect(left, top, badgeWidth, badgeHeight),
            new ChartRect(left + 7 * bodyScale * presentationScale, top, badgeWidth - 14 * bodyScale * presentationScale, badgeHeight),
            Math.Min(8 * bodyScale * presentationScale, badgeHeight / 2), top + badgeHeight * .66);
    }
    internal static NativeBadgePaintPlan Measured(ChartRect allocation, double measuredWidth, double measuredHeight,
        double ascent, double horizontalPadding, double minimumHeight, double radius, double verticalPadding) {
        var width = Math.Min(allocation.Width, measuredWidth + 2 * horizontalPadding);
        var height = Math.Min(allocation.Height, Math.Max(minimumHeight, measuredHeight + verticalPadding));
        var left = allocation.X + (allocation.Width - width) / 2; var top = allocation.Y;
        var inset = Math.Min(horizontalPadding, width / 2); var textHeight = Math.Min(measuredHeight, height);
        var textTop = top + (height - textHeight) / 2;
        return new NativeBadgePaintPlan(new ChartRect(left, top, width, height),
            new ChartRect(left + inset, textTop, Math.Max(0, width - 2 * inset), textHeight),
            Math.Min(radius, Math.Min(width, height) / 2), textTop + ascent);
    }
    internal void AppendSurfaceTo(VisualSceneBuilder builder, ChartColor? fill, ChartColor? stroke, double strokeWidth,
        string role, VisualScenePaintBinding? paint = null) => builder.Rect(Surface, fill, stroke, strokeWidth, Radius, role, paint: paint);
}
