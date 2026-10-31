using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>One completed physical-axis envelope shared by alternate native layouts without changing authored axes.</summary>
internal sealed class ChartCartesianFullDomain {
    private readonly (double? X, double? Y, double? SecondaryX, double? SecondaryY) _tickSteps;
    private ChartCartesianFullDomain(ChartAxisWindow x, ChartAxisWindow y, ChartAxisWindow? secondaryX, ChartAxisWindow? secondaryY,
        (double? X, double? Y, double? SecondaryX, double? SecondaryY) tickSteps = default) {
        X = x; Y = y; SecondaryX = secondaryX; SecondaryY = secondaryY;
        _tickSteps = tickSteps;
    }
    internal ChartAxisWindow X { get; }
    internal ChartAxisWindow Y { get; }
    internal ChartAxisWindow? SecondaryX { get; }
    internal ChartAxisWindow? SecondaryY { get; }
    // A completed envelope skips full layout; retain each drawing's already fitted value ticks as well as its bounds.
    internal ChartCartesianFullDomain WithTickStepsFrom(PreparedVisual drawing) => new(X, Y, SecondaryX, SecondaryY,
        (drawing.XProjection?.PreparedValueTickStep, drawing.YProjection?.PreparedValueTickStep,
            drawing.SecondaryXProjection?.PreparedValueTickStep, drawing.SecondaryYProjection?.PreparedValueTickStep));
    internal static ChartCartesianFullDomain Enclose(IReadOnlyList<PreparedVisual> scenes) => new(
        Enclose(scenes, scene => scene.XProjection) ?? throw new InvalidOperationException("Aligned preparation requires an X projection in every drawing."),
        Enclose(scenes, scene => scene.YProjection) ?? throw new InvalidOperationException("Aligned preparation requires a Y projection in every drawing."),
        Enclose(scenes, scene => scene.SecondaryXProjection), Enclose(scenes, scene => scene.SecondaryYProjection));

    private static ChartAxisWindow? Enclose(IReadOnlyList<PreparedVisual> scenes, Func<PreparedVisual, ChartAxisProjection?> select) {
        var minimum = double.PositiveInfinity; var maximum = double.NegativeInfinity;
        foreach (var scene in scenes) {
            var projection = select(scene);
            if (projection == null) return null;
            minimum = Math.Min(minimum, projection.FullMinimum); maximum = Math.Max(maximum, projection.FullMaximum);
        }
        return new ChartAxisWindow(minimum, maximum);
    }
    internal bool Matches(PreparedVisual scene) => Matches(X, scene.XProjection) && Matches(Y, scene.YProjection)
        && Matches(SecondaryX, scene.SecondaryXProjection) && Matches(SecondaryY, scene.SecondaryYProjection);
    private static bool Matches(ChartAxisWindow? window, ChartAxisProjection? projection) => window == null ? projection == null
        : projection != null && projection.FullMinimum == window.Minimum && projection.FullMaximum == window.Maximum;

    internal void Apply(Chart chart, ChartRange range, ChartRange? secondary) {
        chart.Options.XAxis.PreparedValueTickStep = _tickSteps.X;
        chart.Options.YAxis.PreparedValueTickStep = _tickSteps.Y;
        chart.Options.SecondaryXAxis.PreparedValueTickStep = _tickSteps.SecondaryX;
        chart.Options.SecondaryYAxis.PreparedValueTickStep = _tickSteps.SecondaryY;
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        var x = Expanded(chart.Options.XAxis, range.MinX, range.MaxX, X);
        var y = Expanded(chart.Options.YAxis, range.MinY, range.MaxY, Y);
        range.SetXBounds(x.Minimum, x.Maximum); range.SetYBounds(y.Minimum, y.Maximum);
        if (secondary == null) return;
        if (horizontal) {
            var value = Expanded(chart.Options.SecondaryXAxis, secondary.MinX, secondary.MaxX, SecondaryX!);
            secondary.SetXBounds(value.Minimum, value.Maximum); secondary.SetYBounds(y.Minimum, y.Maximum);
        } else {
            var value = Expanded(chart.Options.SecondaryYAxis, secondary.MinY, secondary.MaxY, SecondaryY!);
            secondary.SetYBounds(value.Minimum, value.Maximum); secondary.SetXBounds(x.Minimum, x.Maximum);
        }
    }
    private static ChartAxisWindow Expanded(ChartAxis axis, double minimum, double maximum, ChartAxisWindow domain) {
        if (axis.Minimum.HasValue && domain.Minimum != minimum || axis.Maximum.HasValue && domain.Maximum != maximum)
            throw new InvalidOperationException("Aligned preparation must preserve every authored axis bound exactly.");
        return new ChartAxisWindow(axis.Minimum.HasValue ? minimum : Math.Min(minimum, domain.Minimum),
            axis.Maximum.HasValue ? maximum : Math.Max(maximum, domain.Maximum));
    }
}
