using System;

namespace ChartForgeX.Rendering;

internal readonly struct RadialBarRingLayout {
    private RadialBarRingLayout(double outerRadius, double strokeWidth, double gap, double centerRadius) {
        OuterRadius = outerRadius;
        StrokeWidth = strokeWidth;
        Gap = gap;
        CenterRadius = centerRadius;
    }

    internal double OuterRadius { get; }
    internal double StrokeWidth { get; }
    internal double Gap { get; }
    internal double CenterRadius { get; }

    internal double RadiusAt(int index) => OuterRadius - StrokeWidth / 2 - index * (StrokeWidth + Gap);

    internal static RadialBarRingLayout Create(double outerRadius, int count, double strokeScale, double requestedCenterRadius = 0) {
        if (count <= 0) return new RadialBarRingLayout(outerRadius, 0, 0, Math.Max(0, outerRadius));

        var preferredGap = Math.Max(5, outerRadius * 0.035);
        var preferredStroke = Math.Max(5, Math.Min(24, ((outerRadius - 18) / count - preferredGap) * strokeScale));
        var defaultCenterRadius = Math.Min(outerRadius * 0.40, Math.Max(26, outerRadius * 0.24));
        var maximumRequestedCenterRadius = Math.Max(0, outerRadius * 0.55);
        var boundedRequestedCenterRadius = Math.Min(maximumRequestedCenterRadius, Math.Max(0, requestedCenterRadius));
        var minimumCenterRadius = Math.Min(Math.Max(0, outerRadius - 2.5), Math.Max(defaultCenterRadius, boundedRequestedCenterRadius));
        var availableBand = Math.Max(0.5, outerRadius - minimumCenterRadius - 2);
        var preferredBand = preferredStroke * count + preferredGap * Math.Max(0, count - 1);

        if (preferredBand <= availableBand) {
            var centerRadius = Math.Max(minimumCenterRadius, outerRadius - preferredBand - 2);
            return new RadialBarRingLayout(outerRadius, preferredStroke, preferredGap, centerRadius);
        }

        var slot = availableBand / count;
        var strokeShare = Math.Max(0.55, Math.Min(0.90, 0.72 * strokeScale));
        var stroke = slot * strokeShare;
        var gap = count > 1 ? Math.Max(0, (availableBand - stroke * count) / (count - 1)) : 0;
        return new RadialBarRingLayout(outerRadius, stroke, gap, minimumCenterRadius);
    }
}
