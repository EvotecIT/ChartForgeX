using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal readonly struct ChartSegmentedBarGeometry {
    public ChartSegmentedBarGeometry(double radius, double capThickness, ChartSegmentedLine softShadow, ChartSegmentedLine shadow, ChartSegmentedLine cap, ChartSegmentedLine highlight) {
        Radius = radius;
        CapThickness = capThickness;
        SoftShadow = softShadow;
        Shadow = shadow;
        Cap = cap;
        Highlight = highlight;
    }

    public double Radius { get; }
    public double CapThickness { get; }
    public ChartSegmentedLine SoftShadow { get; }
    public ChartSegmentedLine Shadow { get; }
    public ChartSegmentedLine Cap { get; }
    public ChartSegmentedLine Highlight { get; }

    public static ChartSegmentedBarGeometry Vertical(ChartBarVisualStyle style, double x, double y, double width, double height, double value) {
        var radius = Math.Min(style.CornerRadius, width / 2.0);
        var capInset = Math.Min(style.CapInset, width / 2.0);
        var capThickness = Math.Min(height, Math.Min(style.CapThickness, Math.Max(2, width * 0.35)));
        var capY = value >= 0 ? y + capThickness / 2.0 : y + height - capThickness / 2.0;
        var direction = value >= 0 ? 1 : -1;
        var highlightY = capY - direction * HighlightOffset(capThickness);
        return new ChartSegmentedBarGeometry(
            radius,
            capThickness,
            new ChartSegmentedLine(x + capInset, capY + direction * (style.CapShadowOffset + style.CapShadowSpread), x + width - capInset, capY + direction * (style.CapShadowOffset + style.CapShadowSpread)),
            new ChartSegmentedLine(x + capInset, capY + direction * style.CapShadowOffset, x + width - capInset, capY + direction * style.CapShadowOffset),
            new ChartSegmentedLine(x + capInset, capY, x + width - capInset, capY),
            new ChartSegmentedLine(x + capInset + capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, highlightY, x + width - capInset - capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, highlightY));
    }

    public static ChartSegmentedBarGeometry Horizontal(ChartBarVisualStyle style, double x, double y, double width, double height, double value) {
        var radius = Math.Min(style.CornerRadius, height / 2.0);
        var capInset = Math.Min(style.CapInset, height / 2.0);
        var capThickness = Math.Min(width, Math.Min(style.CapThickness, Math.Max(2, height * 0.35)));
        var capX = value >= 0 ? x + width - capThickness / 2.0 : x + capThickness / 2.0;
        var direction = value >= 0 ? 1 : -1;
        var highlightX = capX - direction * HighlightOffset(capThickness);
        return new ChartSegmentedBarGeometry(
            radius,
            capThickness,
            new ChartSegmentedLine(capX + direction * (style.CapShadowOffset + style.CapShadowSpread), y + capInset, capX + direction * (style.CapShadowOffset + style.CapShadowSpread), y + height - capInset),
            new ChartSegmentedLine(capX + direction * style.CapShadowOffset, y + capInset, capX + direction * style.CapShadowOffset, y + height - capInset),
            new ChartSegmentedLine(capX, y + capInset, capX, y + height - capInset),
            new ChartSegmentedLine(highlightX, y + capInset + capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, highlightX, y + height - capInset - capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio));
    }

    public static ChartSegmentedBarGeometry RangeCap(ChartBarVisualStyle style, double x, double y, double width) {
        var capWidth = Math.Max(2, Math.Min(width * 1.7, width + style.CapThickness * 2.2));
        var capInset = Math.Min(style.CapInset, capWidth / 2.0);
        var capThickness = Math.Min(style.CapThickness, Math.Max(2, width * 0.45));
        var highlightY = y - HighlightOffset(capThickness);
        return new ChartSegmentedBarGeometry(
            Math.Min(style.CornerRadius, width / 2.0),
            capThickness,
            new ChartSegmentedLine(x - capWidth / 2.0 + capInset, y + style.CapShadowOffset + style.CapShadowSpread, x + capWidth / 2.0 - capInset, y + style.CapShadowOffset + style.CapShadowSpread),
            new ChartSegmentedLine(x - capWidth / 2.0 + capInset, y + style.CapShadowOffset, x + capWidth / 2.0 - capInset, y + style.CapShadowOffset),
            new ChartSegmentedLine(x - capWidth / 2.0 + capInset, y, x + capWidth / 2.0 - capInset, y),
            new ChartSegmentedLine(x - capWidth / 2.0 + capInset + capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, highlightY, x + capWidth / 2.0 - capInset - capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, highlightY));
    }

    private static double HighlightOffset(double capThickness) => Math.Max(ChartVisualPrimitives.SegmentedCapHighlightMinOffset, capThickness * ChartVisualPrimitives.SegmentedCapHighlightOffsetRatio);
}

internal readonly struct ChartSegmentedLine {
    public ChartSegmentedLine(double x1, double y1, double x2, double y2) {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    public double X1 { get; }
    public double Y1 { get; }
    public double X2 { get; }
    public double Y2 { get; }
}
