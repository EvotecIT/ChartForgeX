using System;

namespace ChartForgeX.Core;

/// <summary>Immutable geometry shared by numeric radial bars and columns. Angles advance clockwise from the positive horizontal direction.</summary>
public sealed class ChartRadialGeometryOptions {
    /// <summary>Creates a radial viewport with a finite positive sweep of at most one turn.</summary>
    /// <param name="startAngleDegrees">The first angle in degrees; -90 starts at the top.</param>
    /// <param name="endAngleDegrees">The final angle, greater than the start and at most 360 degrees later.</param>
    /// <param name="innerRadiusRatio">The unpainted center as a proportion of the outer radius, from zero to less than one.</param>
    /// <param name="categorySpacing">The unused fraction of each category band, from zero to less than one.</param>
    /// <param name="seriesSpacing">The unused fraction of each grouped series slot, from zero to less than one. Stacks share a slot.</param>
    public ChartRadialGeometryOptions(double startAngleDegrees = -90, double endAngleDegrees = 270,
        double innerRadiusRatio = .2, double categorySpacing = .25, double seriesSpacing = .12) {
        ChartGuards.Finite(startAngleDegrees, nameof(startAngleDegrees));
        ChartGuards.Finite(endAngleDegrees, nameof(endAngleDegrees));
        var sweep = endAngleDegrees - startAngleDegrees;
        if (!(sweep > 0 && sweep <= 360)) throw new ArgumentOutOfRangeException(nameof(endAngleDegrees), endAngleDegrees, "Radial angles must define a positive sweep of at most 360 degrees.");
        ValidateRatio(innerRadiusRatio, nameof(innerRadiusRatio));
        ValidateRatio(categorySpacing, nameof(categorySpacing));
        ValidateRatio(seriesSpacing, nameof(seriesSpacing));
        StartAngleDegrees = startAngleDegrees; EndAngleDegrees = endAngleDegrees;
        InnerRadiusRatio = innerRadiusRatio; CategorySpacing = categorySpacing; SeriesSpacing = seriesSpacing;
    }

    /// <summary>Gets the first angle in degrees.</summary>
    public double StartAngleDegrees { get; }
    /// <summary>Gets the final angle in degrees.</summary>
    public double EndAngleDegrees { get; }
    /// <summary>Gets the unpainted center as a proportion of the outer radius.</summary>
    public double InnerRadiusRatio { get; }
    /// <summary>Gets the unused fraction of each category band.</summary>
    public double CategorySpacing { get; }
    /// <summary>Gets the unused fraction of each series slot within a category.</summary>
    public double SeriesSpacing { get; }

    private static void ValidateRatio(double value, string parameter) {
        ChartGuards.UnitInterval(value, parameter);
        if (value == 1) throw new ArgumentOutOfRangeException(parameter, value, "Radial radius and spacing ratios must be less than one.");
    }
}
