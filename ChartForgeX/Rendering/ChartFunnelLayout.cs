using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Resolves proportional funnel marks once in logical coordinates for both static painters.</summary>
internal sealed class ChartFunnelLayout {
    private ChartFunnelLayout(ChartFunnelStageLayout[] stages, ChartFunnelConnectionLayout[] connections) {
        Stages = Array.AsReadOnly(stages);
        Connections = Array.AsReadOnly(connections);
    }

    internal IReadOnlyList<ChartFunnelStageLayout> Stages { get; }
    internal IReadOnlyList<ChartFunnelConnectionLayout> Connections { get; }

    internal static ChartFunnelLayout Compute(IReadOnlyList<ChartPoint> points, ChartRect plot, ChartFunnelOptions options,
        double spacing, bool showLabels) {
        if (points.Count == 0) return new ChartFunnelLayout(Array.Empty<ChartFunnelStageLayout>(), Array.Empty<ChartFunnelConnectionLayout>());
        var horizontal = options.Orientation == ChartOrientation.Horizontal;
        var horizontalLabelRail = showLabels && horizontal && options.Form == ChartFunnelForm.Cone;
        var crossLength = horizontal ? plot.Height : plot.Width;
        var processLength = horizontal ? plot.Width : plot.Height;
        var metrics = showLabels && (points.Count > 1 || horizontalLabelRail) ? crossLength * .27 : 0;
        var metricsGap = metrics > 0 ? Math.Min(spacing, crossLength * .02) : 0;
        var domainLength = Math.Max(0, crossLength - metrics - metricsGap);
        var center = domainLength / 2;
        var slot = processLength / points.Count;
        var gap = Math.Min(spacing, slot * .12);
        var barLength = Math.Max(0, (processLength - gap * (points.Count - 1)) / points.Count);
        var maximum = 0d;
        foreach (var point in points) maximum = Math.Max(maximum, point.Y);

        var geometry = new ChartStageGeometry(plot, options.Orientation);
        ChartPoint Position(double cross, double process) => geometry.Position(cross, process);
        ChartRect Rectangle(double cross, double process, double crossExtent, double processExtent) => geometry.Rectangle(cross, process, crossExtent, processExtent);
        ChartPath Polygon(double firstWidth, double firstProcess, double lastWidth, double lastProcess) =>
            geometry.Polygon(center, firstWidth, firstProcess, lastWidth, lastProcess);

        var stages = new ChartFunnelStageLayout[points.Count];
        var connections = new ChartFunnelConnectionLayout[Math.Max(0, points.Count - 1)];
        for (var index = 0; index < points.Count; index++) {
            var extent = maximum > 0 ? domainLength * (points[index].Y / maximum) : 0;
            var low = options.Form == ChartFunnelForm.Cone ? index * slot : index * (barLength + gap);
            var length = options.Form == ChartFunnelForm.Cone ? slot : barLength;
            var anchor = low + length / 2;
            var labelExtent = points[index].Y > 0 ? extent * .9 : domainLength;
            var labelLength = options.Form == ChartFunnelForm.Cone ? Math.Max(0, length / 2 - gap / 2) : length;
            var outsideStart = center + extent / 2 + metricsGap / 2;
            var labelBounds = Rectangle(center - labelExtent / 2, low, labelExtent, labelLength);
            var outsideLabelBounds = Rectangle(outsideStart, low, Math.Max(0, domainLength - outsideStart), length);
            var metricsBounds = Rectangle(domainLength + metricsGap, low, metrics, length);
            if (horizontalLabelRail) {
                // A category needs the complete process slot; the half-slot above a cone line can be arbitrarily narrow.
                var labelBand = metrics * .4; var railGap = Math.Min(spacing / 4, metrics * .05);
                labelBounds = Rectangle(domainLength + metricsGap, low + gap / 2, labelBand, Math.Max(0, length - gap));
                outsideLabelBounds = labelBounds;
                metricsBounds = Rectangle(domainLength + metricsGap + labelBand + railGap, low + gap / 2,
                    Math.Max(0, metrics - labelBand - railGap), Math.Max(0, length - gap));
            }
            stages[index] = new ChartFunnelStageLayout(index, extent,
                Rectangle(center - extent / 2, low, extent, length),
                Polygon(extent, low, extent, low + length),
                Position(center - extent / 2, anchor), Position(center + extent / 2, anchor),
                Position(center - Math.Min(6, domainLength / 20), anchor), Position(center + Math.Min(6, domainLength / 20), anchor),
                labelBounds, outsideLabelBounds, metricsBounds);
        }
        for (var index = 0; index < connections.Length; index++) {
            var first = stages[index]; var last = stages[index + 1];
            var firstProcess = options.Form == ChartFunnelForm.Cone ? (index + .5) * slot : index * (barLength + gap) + barLength;
            var lastProcess = options.Form == ChartFunnelForm.Cone ? (index + 1.5) * slot : (index + 1) * (barLength + gap);
            var extent = Math.Max(first.Extent, last.Extent);
            connections[index] = new ChartFunnelConnectionLayout(index, index + 1,
                Rectangle(center - extent / 2, firstProcess, extent, Math.Max(0, lastProcess - firstProcess)),
                Polygon(first.Extent, firstProcess, last.Extent, lastProcess));
        }
        return new ChartFunnelLayout(stages, connections);
    }
}

internal sealed class ChartFunnelStageLayout {
    internal ChartFunnelStageLayout(int sourceIndex, double extent, ChartRect bounds, ChartPath bar,
        ChartPoint lineStart, ChartPoint lineEnd, ChartPoint zeroStart, ChartPoint zeroEnd,
        ChartRect labelBounds, ChartRect outsideLabelBounds, ChartRect metricsBounds) {
        SourceIndex = sourceIndex; Extent = extent; Bounds = bounds; Bar = bar;
        LineStart = lineStart; LineEnd = lineEnd; ZeroStart = zeroStart; ZeroEnd = zeroEnd;
        LabelBounds = labelBounds; OutsideLabelBounds = outsideLabelBounds; MetricsBounds = metricsBounds;
    }
    internal int SourceIndex { get; }
    internal double Extent { get; }
    internal ChartRect Bounds { get; }
    internal ChartPath Bar { get; }
    internal ChartPoint LineStart { get; }
    internal ChartPoint LineEnd { get; }
    internal ChartPoint ZeroStart { get; }
    internal ChartPoint ZeroEnd { get; }
    internal ChartRect LabelBounds { get; }
    internal ChartRect OutsideLabelBounds { get; }
    internal ChartRect MetricsBounds { get; }
}

internal sealed class ChartFunnelConnectionLayout {
    internal ChartFunnelConnectionLayout(int fromIndex, int toIndex, ChartRect bounds, ChartPath path) {
        FromIndex = fromIndex; ToIndex = toIndex; Bounds = bounds; Path = path;
    }
    internal int FromIndex { get; }
    internal int ToIndex { get; }
    internal ChartRect Bounds { get; }
    internal ChartPath Path { get; }
}
