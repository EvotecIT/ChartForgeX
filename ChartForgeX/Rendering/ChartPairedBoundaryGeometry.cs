using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Maps authored paired boundaries and constructs their shared closed envelope without reordering either member.</summary>
internal sealed class ChartPairedBoundaryGeometry {
    internal List<ChartPoint> First { get; } = new();
    internal List<ChartPoint> Second { get; } = new();

    internal bool YReversed { get; }
    internal bool NonlinearValueScale { get; }

    internal ChartPairedBoundaryGeometry(ChartSeries series, ChartMapper map, bool yReversed = false, bool nonlinearValueScale = false) {
        YReversed = yReversed; NonlinearValueScale = nonlinearValueScale;
        for (var item = 0; item < series.RenderPoints.Count; item += 2) {
            var first = series.RenderPoints[item]; var second = series.RenderPoints[item + 1];
            var x = map.X(first.X); var split = first.BreakBefore || second.BreakBefore;
            First.Add(new ChartPoint(x, map.Y(first.Y), split));
            Second.Add(new ChartPoint(x, map.Y(second.Y), split));
        }
    }

    /// <summary>Uses the canonical path builder, retaining legacy range defaults and bounding new independent smooth curves to each X interval.</summary>
    internal static ChartPath BoundaryPath(ChartSeries series, IReadOnlyList<ChartPoint> points, bool first) {
        var policy = series.BoundaryInterpolation(first);
        var bounded = series.Kind == ChartSeriesKind.DifferenceArea || series.FirstBoundaryInterpolation.HasValue || series.SecondBoundaryInterpolation.HasValue;
        return ChartPathBuilder.FromPoints(points, policy.Interpolation, policy.StepPosition, preserveXIntervals: bounded);
    }

    internal static ChartPath Envelope(IReadOnlyList<ChartPoint> forward, IReadOnlyList<ChartPoint> backward) {
        var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(forward[0].X, forward[0].Y) };
        for (var index = 1; index < forward.Count; index++) commands.Add(ChartPathCommand.LineTo(forward[index].X, forward[index].Y));
        for (var index = backward.Count - 1; index >= 0; index--) commands.Add(ChartPathCommand.LineTo(backward[index].X, backward[index].Y));
        return new ChartPath(commands);
    }
}
