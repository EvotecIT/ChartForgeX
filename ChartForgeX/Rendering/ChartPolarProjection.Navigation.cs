using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

public sealed partial class ChartPolarProjection {
    /// <summary>Crops selected captured coordinates around the visible sector and selected-annulus midpoint.</summary>
    /// <remarks>This toolbar-friendly overload resolves its default anchor entirely from the captured native projection.
    /// It delegates to the anchored overload, including its annular confinement and immutable full-domain reset semantics.</remarks>
    /// <returns>An immutable native request, or null for the complete view.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The factor or selected axes are invalid, or a span is unrepresentable.</exception>
    /// <exception cref="NotSupportedException">A selected radius axis is unavailable.</exception>
    public ChartPolarViewport? Zoom(double factor,
        ChartPolarNavigationAxes axes = ChartPolarNavigationAxes.Angle | ChartPolarNavigationAxes.Radius) {
        ValidateNavigationAxes(axes);
        var inner = 0d;
        if ((axes & ChartPolarNavigationAxes.Radius) != 0) inner = Math.Max(inner, Axis(ChartAxisSide.Primary).LogicalMinimum);
        if ((axes & ChartPolarNavigationAxes.SecondaryRadius) != 0) inner = Math.Max(inner, Axis(ChartAxisSide.Secondary).LogicalMinimum);
        var window = AngleWindow; var angle = (window.Minimum / 2 + window.Maximum / 2) * (AngleReversed ? 1 : -1) + RotationRadians;
        var radius = inner / 2 + OuterRadius / 2;
        return Zoom(factor, new ChartPoint(Center.X + Math.Cos(angle) * radius, Center.Y + Math.Sin(angle) * radius), axes);
    }

    /// <summary>Crops selected captured coordinates around a logical pointer anchor. Factors above one zoom in.</summary>
    /// <remarks>The anchor must lie in the visible directional sector and every selected radius annulus, away from the center.
    /// The directional interval changes without rotating or stretching source curves. Radius confinement uses the captured
    /// scalar transform; the next preparation crops its original curves with the resulting annulus. The anchor selects crop bounds;
    /// uniform annular magnification does not hold an arbitrary source point fixed at that screen position. Unselected windows stay intact.</remarks>
    /// <returns>An immutable native request, or null when all coordinates return to their complete domains.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The factor, anchor or selected axes are invalid, or a span is unrepresentable.</exception>
    /// <exception cref="NotSupportedException">A selected radius axis is unavailable.</exception>
    public ChartPolarViewport? Zoom(double factor, ChartPoint logicalAnchor,
        ChartPolarNavigationAxes axes = ChartPolarNavigationAxes.Angle | ChartPolarNavigationAxes.Radius) {
        ValidateNavigationAxes(axes); ChartPrimitiveGuards.Finite(factor, nameof(factor));
        if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        ChartPrimitiveGuards.Finite(logicalAnchor.X, nameof(logicalAnchor)); ChartPrimitiveGuards.Finite(logicalAnchor.Y, nameof(logicalAnchor));
        var x = logicalAnchor.X - Center.X; var y = logicalAnchor.Y - Center.Y;
        var distance = Math.Sqrt(x * x + y * y);
        if (!ChartMath.IsFinite(distance) || distance <= 0 || distance > OuterRadius)
            throw new ArgumentOutOfRangeException(nameof(logicalAnchor), "A polar navigation anchor must lie inside the visible plot away from its undefined center bearing.");
        var raw = AngleReversed ? Math.Atan2(y, x) - RotationRadians : RotationRadians - Math.Atan2(y, x);
        var window = AngleWindow; var midpoint = window.Minimum / 2 + window.Maximum / 2;
        var anchor = midpoint + Math.IEEERemainder(raw - midpoint, Math.PI * 2);
        if (!ContainsAngle(Viewport?.Angle, anchor)) throw new ArgumentOutOfRangeException(nameof(logicalAnchor), "The anchor is outside the visible angular sector.");
        var angle = Viewport?.Angle; var radius = Viewport?.Radius; var secondary = Viewport?.SecondaryRadius;
        if ((axes & ChartPolarNavigationAxes.Angle) != 0 && factor != 1) {
            var span = (window.Maximum - window.Minimum) / factor;
            angle = span >= Math.PI * 2 ? null : new ChartAxisWindow(anchor - (anchor - window.Minimum) / factor,
                anchor + (window.Maximum - anchor) / factor, window.MaximumInclusive);
        }
        ChartAxisWindow? Crop(ChartAxisSide side) {
            var axis = Axis(side);
            if (distance < axis.LogicalMinimum || distance > axis.LogicalMaximum)
                throw new ArgumentOutOfRangeException(nameof(logicalAnchor), "The anchor is outside a selected radius annulus.");
            if (factor == 1) return side == ChartAxisSide.Primary ? Viewport?.Radius : Viewport?.SecondaryRadius;
            return axis.ConstrainLogical(distance - (distance - axis.LogicalMinimum) / factor,
                distance + (axis.LogicalMaximum - distance) / factor);
        }
        if ((axes & ChartPolarNavigationAxes.Radius) != 0) radius = Crop(ChartAxisSide.Primary);
        if ((axes & ChartPolarNavigationAxes.SecondaryRadius) != 0) secondary = Crop(ChartAxisSide.Secondary);
        return NavigationViewport(angle, radius, secondary);
    }

    /// <summary>Moves selected captured windows by clockwise screen radians and outward logical drawing units.</summary>
    /// <remarks>Angular motion moves the directional crop; raw source curves and bearings do not rotate. An outward radius drag
    /// shifts the current transformed span toward the inner drawing direction before native full-domain confinement. This does
    /// not promise a nonlinear grab-point warp. No point or source identity is filtered, renumbered or changed.</remarks>
    /// <returns>An immutable native request, or null for the complete view.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A delta, selected axes or resulting span is invalid or unrepresentable.</exception>
    /// <exception cref="NotSupportedException">A selected radius axis is unavailable.</exception>
    public ChartPolarViewport? Pan(double clockwiseAngleDelta, double outwardLogicalRadiusDelta,
        ChartPolarNavigationAxes axes = ChartPolarNavigationAxes.Angle | ChartPolarNavigationAxes.Radius) {
        ValidateNavigationAxes(axes); ChartPrimitiveGuards.Finite(clockwiseAngleDelta, nameof(clockwiseAngleDelta));
        ChartPrimitiveGuards.Finite(outwardLogicalRadiusDelta, nameof(outwardLogicalRadiusDelta));
        var angle = Viewport?.Angle; var radius = Viewport?.Radius; var secondary = Viewport?.SecondaryRadius;
        if ((axes & ChartPolarNavigationAxes.Angle) != 0 && clockwiseAngleDelta != 0) {
            var window = AngleWindow; var shift = AngleReversed ? clockwiseAngleDelta : -clockwiseAngleDelta;
            angle = window.Maximum - window.Minimum == Math.PI * 2 ? null
                : new ChartAxisWindow(window.Minimum + shift, window.Maximum + shift, window.MaximumInclusive);
        }
        ChartAxisWindow? Move(ChartAxisSide side) {
            var axis = Axis(side);
            if (outwardLogicalRadiusDelta == 0) return side == ChartAxisSide.Primary ? Viewport?.Radius : Viewport?.SecondaryRadius;
            return axis.ConstrainLogical(axis.LogicalMinimum - outwardLogicalRadiusDelta, axis.LogicalMaximum - outwardLogicalRadiusDelta);
        }
        if ((axes & ChartPolarNavigationAxes.Radius) != 0) radius = Move(ChartAxisSide.Primary);
        if ((axes & ChartPolarNavigationAxes.SecondaryRadius) != 0) secondary = Move(ChartAxisSide.Secondary);
        return NavigationViewport(angle, radius, secondary);
    }
    private void ValidateNavigationAxes(ChartPolarNavigationAxes axes) {
        const ChartPolarNavigationAxes supported = ChartPolarNavigationAxes.Angle | ChartPolarNavigationAxes.Radius | ChartPolarNavigationAxes.SecondaryRadius;
        if (axes == ChartPolarNavigationAxes.None || (axes & ~supported) != 0) throw new ArgumentOutOfRangeException(nameof(axes));
        if ((axes & ChartPolarNavigationAxes.Radius) != 0) _ = Axis(ChartAxisSide.Primary);
        if ((axes & ChartPolarNavigationAxes.SecondaryRadius) != 0) _ = Axis(ChartAxisSide.Secondary);
    }
    private static ChartPolarViewport? NavigationViewport(ChartAxisWindow? angle, ChartAxisWindow? radius, ChartAxisWindow? secondary) =>
        angle == null && radius == null && secondary == null ? null : new ChartPolarViewport(angle, radius, secondary);
}
