using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>A detached directional and radius projection captured by a usable continuous Polar preparation.</summary>
/// <remarks>Angular windows clip a sector; they never stretch or rewrite source bearings. Each radius window uniformly magnifies
/// captured full-domain geometry and crops an annulus. The nearer projected boundary is an inner aperture, not a nonlinear remapping to the center.
/// Original cubic curves, tangents and closed areas remain intact before the shared SVG/native-raster paint clip.</remarks>
public sealed partial class ChartPolarProjection {
    private readonly ChartViewportAxisProjection? _radius, _secondaryRadius;
    internal ChartPolarProjection(ChartPolarViewport? request, ChartPoint center, double outerRadius, double rotation, bool reversed,
        ChartViewportAxisProjection? radius, ChartViewportAxisProjection? secondaryRadius) {
        Viewport = request; Center = center; OuterRadius = outerRadius; RotationRadians = rotation; AngleReversed = reversed;
        _radius = radius; _secondaryRadius = secondaryRadius;
    }
    /// <summary>Gets the captured immutable request, or null for the full view.</summary>
    public ChartPolarViewport? Viewport { get; }
    /// <summary>Gets the center in logical drawing units.</summary>
    public ChartPoint Center { get; }
    /// <summary>Gets the outer drawing radius in logical units.</summary>
    public double OuterRadius { get; }
    /// <summary>Gets the authored clockwise display rotation in radians.</summary>
    public double RotationRadians { get; }
    /// <summary>Gets whether increasing authored angles run clockwise.</summary>
    public bool AngleReversed { get; }
    /// <summary>Gets the current unwrapped directional interval. The full view is 0..2π.</summary>
    public ChartAxisWindow AngleWindow => Viewport?.Angle ?? new ChartAxisWindow(0, Math.PI * 2);
    internal ChartViewportAxisProjection? Radius => _radius;
    internal ChartViewportAxisProjection? SecondaryRadius => _secondaryRadius;
    /// <summary>Gets the current or full source-radius window for an available selected axis.</summary>
    public ChartAxisWindow GetRadiusWindow(ChartAxisSide axis = ChartAxisSide.Primary, bool fullDomain = false) {
        var projection = Axis(axis);
        return fullDomain ? new ChartAxisWindow(projection.FullMinimum, projection.FullMaximum)
            : new ChartAxisWindow(projection.Minimum, projection.Maximum, projection.MaximumInclusive);
    }
    /// <summary>Confines a requested radius window using the captured axis transform. Null means the full domain.</summary>
    public ChartAxisWindow? ConstrainRadius(ChartAxisWindow window, ChartAxisSide axis = ChartAxisSide.Primary) {
        if (window == null) throw new ArgumentNullException(nameof(window));
        return Axis(axis).ConstrainSource(window);
    }
    /// <summary>Gets the selected radius axis's captured inner aperture in logical drawing units.</summary>
    public double GetInnerRadius(ChartAxisSide axis = ChartAxisSide.Primary) => Axis(axis).LogicalMinimum;
    /// <summary>Gets the selected radius axis's captured outer crop radius in logical drawing units.</summary>
    public double GetOuterRadius(ChartAxisSide axis = ChartAxisSide.Primary) => Axis(axis).LogicalMaximum;
    /// <summary>Maps raw radians and a supported nonnegative radius onto the uniformly magnified full-domain geometry.</summary>
    /// <remarks>Values are constrained only to the original full domain, as in full preparation. An out-of-window source point
    /// can lie inside the aperture or beyond the outer crop; it is retained but is not a visible source mark.</remarks>
    public ChartPoint Project(double angle, double radius, ChartAxisSide axis = ChartAxisSide.Primary) {
        ChartPrimitiveGuards.Finite(angle, nameof(angle));
        ChartPrimitiveGuards.Finite(radius, nameof(radius));
        var projection = Axis(axis);
        if (radius < 0 || projection.Scale == ChartScaleKind.Logarithmic && radius == 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "A polar source radius must be nonnegative and logarithmic radii must be positive.");
        // The captured full-domain radius is nonnegative. Bound only origin roundoff from extrapolating the current annular axis.
        var distance = Math.Max(0, projection.Project(Math.Max(projection.FullMinimum, Math.Min(projection.FullMaximum, radius))));
        var bearing = (AngleReversed ? angle : -angle) + RotationRadians;
        return new ChartPoint(Center.X + Math.Cos(bearing) * distance, Center.Y + Math.Sin(bearing) * distance);
    }
    /// <summary>Converts a nonnegative logical distance from the center into the selected source radius.</summary>
    public double UnprojectRadius(double logicalRadius, ChartAxisSide axis = ChartAxisSide.Primary) {
        ChartPrimitiveGuards.Finite(logicalRadius, nameof(logicalRadius));
        if (logicalRadius < 0) throw new ArgumentOutOfRangeException(nameof(logicalRadius));
        return Axis(axis).Unproject(logicalRadius);
    }
    /// <summary>Tests source-mark membership without filtering or rewriting captured observations.</summary>
    public bool Contains(double angle, double radius, ChartAxisSide axis = ChartAxisSide.Primary) {
        ChartPrimitiveGuards.Finite(angle, nameof(angle)); ChartPrimitiveGuards.Finite(radius, nameof(radius));
        var projection = Axis(axis);
        if (radius < 0 || projection.Scale == ChartScaleKind.Logarithmic && radius == 0) return false;
        var window = axis == ChartAxisSide.Primary ? Viewport?.Radius : Viewport?.SecondaryRadius;
        var full = window != null && window.Minimum == projection.FullMinimum && window.Maximum == projection.FullMaximum && window.MaximumInclusive;
        return ContainsAngle(Viewport?.Angle, angle) && (window == null || full || radius >= window.Minimum
            && (window.MaximumInclusive ? radius <= window.Maximum : radius < window.Maximum));
    }
    internal static bool ContainsAngle(ChartAxisWindow? window, double angle) {
        if (window == null || window.Maximum - window.Minimum == Math.PI * 2) return true;
        double Bearing(double value) {
            var result = Math.IEEERemainder(value, Math.PI * 2);
            return result == Math.PI ? -Math.PI : result;
        }
        var direction = Bearing(angle); var first = Bearing(window.Minimum); var last = Bearing(window.Maximum);
        var afterStart = direction >= first;
        var beforeEnd = window.MaximumInclusive ? direction <= last : direction < last;
        return first < last ? afterStart && beforeEnd : afterStart || beforeEnd;
    }
    private ChartViewportAxisProjection Axis(ChartAxisSide side) {
        if (!Enum.IsDefined(typeof(ChartAxisSide), side)) throw new ArgumentOutOfRangeException(nameof(side));
        return (side == ChartAxisSide.Primary ? _radius : _secondaryRadius)
            ?? throw new NotSupportedException("The prepared polar scene has no selected radius axis.");
    }
}
