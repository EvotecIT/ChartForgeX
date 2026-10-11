using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

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
    public double SoftShadowStrokeWidth(ChartBarVisualStyle style) => CapThickness + style.CapShadowSpread * 2;
    public double HighlightStrokeWidth => Math.Max(1, CapThickness * ChartVisualPrimitives.SegmentedCapHighlightStrokeRatio);

    public static ChartSegmentedBarGeometry Vertical(ChartBarVisualStyle style, double x, double y, double width, double height, double mappedDirection) {
        var radius = Math.Min(style.CornerRadius, width / 2.0);
        var capInset = Math.Min(style.CapInset, width / 2.0);
        var capThickness = Math.Min(height, Math.Min(style.CapThickness, Math.Max(2, width * 0.35)));
        var capY = mappedDirection >= 0 ? y + capThickness / 2.0 : y + height - capThickness / 2.0;
        var direction = mappedDirection >= 0 ? 1 : -1;
        var highlightY = capY - direction * HighlightOffset(capThickness);
        return new ChartSegmentedBarGeometry(
            radius,
            capThickness,
            new ChartSegmentedLine(x + capInset, capY + direction * (style.CapShadowOffset + style.CapShadowSpread), x + width - capInset, capY + direction * (style.CapShadowOffset + style.CapShadowSpread)),
            new ChartSegmentedLine(x + capInset, capY + direction * style.CapShadowOffset, x + width - capInset, capY + direction * style.CapShadowOffset),
            new ChartSegmentedLine(x + capInset, capY, x + width - capInset, capY),
            new ChartSegmentedLine(x + capInset + capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, highlightY, x + width - capInset - capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, highlightY));
    }

    public static ChartSegmentedBarGeometry Horizontal(ChartBarVisualStyle style, double x, double y, double width, double height, double mappedDirection) {
        var radius = Math.Min(style.CornerRadius, height / 2.0);
        var capInset = Math.Min(style.CapInset, height / 2.0);
        var capThickness = Math.Min(width, Math.Min(style.CapThickness, Math.Max(2, height * 0.35)));
        var capX = mappedDirection >= 0 ? x + width - capThickness / 2.0 : x + capThickness / 2.0;
        var direction = mappedDirection >= 0 ? 1 : -1;
        var highlightX = capX - direction * HighlightOffset(capThickness);
        return new ChartSegmentedBarGeometry(
            radius,
            capThickness,
            new ChartSegmentedLine(capX + direction * (style.CapShadowOffset + style.CapShadowSpread), y + capInset, capX + direction * (style.CapShadowOffset + style.CapShadowSpread), y + height - capInset),
            new ChartSegmentedLine(capX + direction * style.CapShadowOffset, y + capInset, capX + direction * style.CapShadowOffset, y + height - capInset),
            new ChartSegmentedLine(capX, y + capInset, capX, y + height - capInset),
            new ChartSegmentedLine(highlightX, y + capInset + capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, highlightX, y + height - capInset - capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio));
    }

    public static ChartSegmentedBarGeometry RangeCap(ChartBarVisualStyle style, double x, double y, double width, bool horizontal = false) {
        var category = horizontal ? y : x; var value = horizontal ? x : y;
        var capWidth = Math.Max(2, Math.Min(width * 1.7, width + style.CapThickness * 2.2));
        var capThickness = Math.Min(style.CapThickness, Math.Max(2, width * 0.45));
        var highlightInset = Math.Min(capThickness * ChartVisualPrimitives.SegmentedCapHighlightInsetRatio, capWidth / 4);
        // Preserve wide caps while keeping narrow caps and their highlight non-degenerate.
        var capInset = Math.Min(style.CapInset, (capWidth - highlightInset * 2 - .1) / 2);
        var highlight = value - HighlightOffset(capThickness);
        return new ChartSegmentedBarGeometry(
            Math.Min(style.CornerRadius, width / 2.0),
            capThickness,
            Line(category - capWidth / 2.0 + capInset, value + style.CapShadowOffset + style.CapShadowSpread, category + capWidth / 2.0 - capInset, value + style.CapShadowOffset + style.CapShadowSpread),
            Line(category - capWidth / 2.0 + capInset, value + style.CapShadowOffset, category + capWidth / 2.0 - capInset, value + style.CapShadowOffset),
            Line(category - capWidth / 2.0 + capInset, value, category + capWidth / 2.0 - capInset, value),
            Line(category - capWidth / 2.0 + capInset + highlightInset, highlight, category + capWidth / 2.0 - capInset - highlightInset, highlight));

        ChartSegmentedLine Line(double categoryStart, double valueStart, double categoryEnd, double valueEnd) => horizontal
            ? new ChartSegmentedLine(valueStart, categoryStart, valueEnd, categoryEnd)
            : new ChartSegmentedLine(categoryStart, valueStart, categoryEnd, valueEnd);
    }

    /// <summary>Measures the actual round-stroked cap layers and body in category pixels.</summary>
    public static double RangeCategoryFootprint(ChartBarVisualStyle style, double width) =>
        Math.Max(width, RangeCap(style, 0, 0, width).PaintBounds(style).Width);

    /// <summary>Includes both endpoint caps, visible lighting layers and the interval body.</summary>
    public static ChartRect RangePaintBounds(ChartBarVisualStyle style, ChartRect body, bool horizontal) {
        var category = horizontal ? body.Top + body.Height / 2 : body.Left + body.Width / 2;
        var first = RangeCap(style, horizontal ? body.Left : category, horizontal ? category : body.Top,
            horizontal ? body.Height : body.Width, horizontal).PaintBounds(style);
        var last = RangeCap(style, horizontal ? body.Right : category, horizontal ? category : body.Bottom,
            horizontal ? body.Height : body.Width, horizontal).PaintBounds(style);
        var left = Math.Min(body.Left, Math.Min(first.Left, last.Left)); var top = Math.Min(body.Top, Math.Min(first.Top, last.Top));
        return new ChartRect(left, top, Math.Max(body.Right, Math.Max(first.Right, last.Right)) - left,
            Math.Max(body.Bottom, Math.Max(first.Bottom, last.Bottom)) - top);
    }

    private ChartRect PaintBounds(ChartBarVisualStyle style) {
        var left = (Cap.X1 + Cap.X2) / 2; var right = left; var top = (Cap.Y1 + Cap.Y2) / 2; var bottom = top;
        Include(SoftShadow, SoftShadowStrokeWidth(style), style.CapShadowOpacity * .36);
        Include(Shadow, CapThickness, style.CapShadowOpacity);
        Include(Cap, CapThickness, style.CapOpacity);
        Include(Highlight, HighlightStrokeWidth, style.CapHighlightOpacity);
        return new ChartRect(left, top, right - left, bottom - top);

        void Include(ChartSegmentedLine line, double stroke, double opacity) {
            if (opacity <= 0 || stroke <= 0) return;
            left = Math.Min(left, Math.Min(line.X1, line.X2) - stroke / 2); right = Math.Max(right, Math.Max(line.X1, line.X2) + stroke / 2);
            top = Math.Min(top, Math.Min(line.Y1, line.Y2) - stroke / 2); bottom = Math.Max(bottom, Math.Max(line.Y1, line.Y2) + stroke / 2);
        }
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
