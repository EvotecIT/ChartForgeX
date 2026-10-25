using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>Captures directional angle clipping and independent source-radius windows for continuous Polar series.</summary>
/// <remarks>Angles are authored radians. An increasing interval no wider than one turn clips directions without remapping bearings;
/// 5.5..6.8 crosses the zero ray. Negative and multi-turn source observations retain their original values, order and identity.
/// Equivalent bearings intentionally coincide. Radius windows uniformly magnify captured full-domain geometry and crop an annulus;
/// their nearer projected boundary is an inner aperture. Null windows retain full domains; a null viewport resets the complete view.</remarks>
public sealed class ChartPolarViewport {
    /// <summary>Creates a request with at least one explicit polar coordinate window.</summary>
    /// <param name="angle">An increasing unwrapped interval of at most 2π radians. A full turn includes every direction.</param>
    /// <param name="radius">A source window on the primary radius axis.</param>
    /// <param name="secondaryRadius">A source window on the secondary radius axis.</param>
    /// <exception cref="ArgumentException">Every coordinate window is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The angular span is not finite or exceeds one turn.</exception>
    public ChartPolarViewport(ChartAxisWindow? angle = null, ChartAxisWindow? radius = null, ChartAxisWindow? secondaryRadius = null) {
        if (angle == null && radius == null && secondaryRadius == null) throw new ArgumentException("A polar viewport requires a coordinate window.");
        if (angle != null && (!ChartMath.IsFinite(angle.Maximum - angle.Minimum) || angle.Maximum - angle.Minimum > Math.PI * 2))
            throw new ArgumentOutOfRangeException(nameof(angle), "A directional angular window cannot exceed one turn.");
        Angle = angle; Radius = radius; SecondaryRadius = secondaryRadius;
    }
    /// <summary>Gets the authored directional interval, or null for every bearing.</summary>
    public ChartAxisWindow? Angle { get; }
    /// <summary>Gets the primary radius window, or null for its full source domain.</summary>
    public ChartAxisWindow? Radius { get; }
    /// <summary>Gets the secondary radius window, or null for its full source domain.</summary>
    public ChartAxisWindow? SecondaryRadius { get; }
}
