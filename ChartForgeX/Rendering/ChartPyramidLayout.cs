using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Partitions one triangle in normalized coordinates before applying orientation, reversal and viewport fitting.</summary>
internal sealed class ChartPyramidLayout {
    private ChartPyramidLayout(ChartRect triangle, ChartPyramidStageLayout[] stages, double total, ChartDataLabelPlacement labelSide) {
        Triangle = triangle; Stages = Array.AsReadOnly(stages); Total = total; LabelSide = labelSide;
    }

    internal ChartRect Triangle { get; }
    internal IReadOnlyList<ChartPyramidStageLayout> Stages { get; }
    internal double Total { get; }
    internal ChartDataLabelPlacement LabelSide { get; }

    internal static ChartPyramidLayout Compute(IReadOnlyList<ChartPoint> points, ChartRect plot, ChartPyramidOptions options,
        double labelRail, ChartDataLabelPlacement placement, double spacing) {
        var total = ChartPyramidWeights.Total(points);
        var horizontal = options.Orientation == ChartOrientation.Horizontal;
        var side = placement is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below
            ? placement : horizontal ? ChartDataLabelPlacement.Below : ChartDataLabelPlacement.Right;
        var sideRail = side is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right;
        var railLength = sideRail ? plot.Width : plot.Height;
        labelRail = Math.Min(labelRail, railLength * .4);
        var gap = labelRail > 0 ? Math.Min(spacing, railLength * .03) : 0;
        var available = new ChartRect(plot.Left + (side == ChartDataLabelPlacement.Left ? labelRail + gap : 0),
            plot.Top + (side == ChartDataLabelPlacement.Above ? labelRail + gap : 0),
            Math.Max(0, plot.Width - (sideRail ? labelRail + gap : 0)),
            Math.Max(0, plot.Height - (sideRail ? 0 : labelRail + gap)));
        var rail = side switch {
            ChartDataLabelPlacement.Left => new ChartRect(plot.Left, plot.Top, labelRail, plot.Height),
            ChartDataLabelPlacement.Right => new ChartRect(plot.Right - labelRail, plot.Top, labelRail, plot.Height),
            ChartDataLabelPlacement.Above => new ChartRect(plot.Left, plot.Top, plot.Width, labelRail),
            _ => new ChartRect(plot.Left, plot.Bottom - labelRail, plot.Width, labelRail)
        };
        var crossLength = horizontal ? available.Height : available.Width;
        var processLength = horizontal ? available.Width : available.Height;
        if (options.AspectRatio.HasValue && crossLength > 0 && processLength > 0) {
            var ratio = options.AspectRatio.Value;
            // Choose the limiting axis before multiplication; extreme finite ratios cannot overflow the viewport.
            if (ratio >= crossLength / processLength) processLength = crossLength / ratio;
            else crossLength = processLength * ratio;
        }
        var triangle = new ChartRect(available.Left + (available.Width - (horizontal ? processLength : crossLength)) / 2,
            available.Top + (available.Height - (horizontal ? crossLength : processLength)) / 2,
            horizontal ? processLength : crossLength, horizontal ? crossLength : processLength);
        var geometry = new ChartStageGeometry(triangle, options.Orientation);
        var center = crossLength / 2;
        var stages = new ChartPyramidStageLayout[points.Count];
        var cumulative = 0d; var previous = 0d;
        double Process(double normalized) => processLength * (options.Reversed ? 1 - normalized : normalized);
        for (var index = 0; index < points.Count; index++) {
            cumulative += points[index].Y;
            var share = total > 0 ? points[index].Y / total : 0;
            var next = total > 0 ? Math.Min(1, cumulative / total) : 0;
            if (options.ValueEncoding == ChartPyramidValueEncoding.Area) next = Math.Sqrt(next);
            var first = Process(previous); var last = Process(next);
            var length = next - previous;
            var middle = previous + length / 2;
            var midProcess = Process(middle);
            var innerFirst = previous + length / 4; var innerLast = next - length / 4;
            var innerWidth = crossLength * innerFirst;
            var innerTop = Math.Min(Process(innerFirst), Process(innerLast));
            var bounds = geometry.Rectangle(center - crossLength * next / 2, Math.Min(first, last), crossLength * next,
                processLength * length);
            var inside = geometry.Rectangle(center - innerWidth / 2, innerTop, innerWidth, processLength * length / 2);
            var slotIndex = options.Reversed ? points.Count - 1 - index : index;
            var outside = sideRail ? new ChartRect(rail.Left, rail.Top + rail.Height * slotIndex / points.Count,
                rail.Width, rail.Height / points.Count) : new ChartRect(rail.Left + rail.Width * slotIndex / points.Count,
                rail.Top, rail.Width / points.Count, rail.Height);
            var crossLeft = geometry.Position(center - crossLength * middle / 2, midProcess);
            var crossRight = geometry.Position(center + crossLength * middle / 2, midProcess);
            var processFirst = geometry.Position(center, Math.Min(first, last));
            var processLast = geometry.Position(center, Math.Max(first, last));
            var anchor = side switch {
                ChartDataLabelPlacement.Left => horizontal ? processFirst : crossLeft,
                ChartDataLabelPlacement.Right => horizontal ? processLast : crossRight,
                ChartDataLabelPlacement.Above => horizontal ? crossLeft : processFirst,
                _ => horizontal ? crossRight : processLast
            };
            stages[index] = new ChartPyramidStageLayout(index, share, previous, next, bounds,
                geometry.Polygon(center, crossLength * previous, first, crossLength * next, last), inside, outside, anchor);
            previous = next;
        }
        return new ChartPyramidLayout(triangle, stages, total, side);
    }
}

internal sealed class ChartPyramidStageLayout {
    internal ChartPyramidStageLayout(int sourceIndex, double valueFraction, double start, double end, ChartRect bounds,
        ChartPath segment, ChartRect insideLabel, ChartRect outsideLabel, ChartPoint labelAnchor) {
        SourceIndex = sourceIndex; ValueFraction = valueFraction; Start = start; End = end;
        Bounds = bounds; Segment = segment; InsideLabel = insideLabel; OutsideLabel = outsideLabel; LabelAnchor = labelAnchor;
    }
    internal int SourceIndex { get; }
    internal double ValueFraction { get; }
    internal double Start { get; }
    internal double End { get; }
    internal double LengthFraction => End - Start;
    internal double AreaFraction => (End - Start) * (End + Start);
    internal bool HasGeometry => LengthFraction > 0 && Bounds.Right > Bounds.Left && Bounds.Bottom > Bounds.Top;
    internal ChartRect Bounds { get; }
    internal ChartPath Segment { get; }
    internal ChartRect InsideLabel { get; }
    internal ChartRect OutsideLabel { get; }
    internal ChartPoint LabelAnchor { get; }
}
