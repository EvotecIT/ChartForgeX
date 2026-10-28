using System;

namespace ChartForgeX.Topology;

/// <summary>Paint at a proper crossing of two final orthogonal topology routes.</summary>
public enum TopologyCrossingPaintStyle {
    /// <summary>Replaces a short straight span with a native semicircular bridge.</summary>
    Arc = 0,
    /// <summary>Leaves a short break in the later painted route; its relationship and hit route remain complete.</summary>
    Gap = 1
}

/// <summary>How crossing paint handles a final native path that cannot represent straight-segment hops.</summary>
public enum TopologyCrossingCurveHandling {
    /// <summary>Reject unsupported interpolation or authored cubic paint before painting.</summary>
    Reject = 0,
    /// <summary>Keep unsupported curves unchanged and exclude them from crossing detection.</summary>
    SkipUnsupported = 1
}

/// <summary>Immutable orthogonal crossing paint retained by the native diagram and portable presentation.</summary>
/// <remarks>The later actual native paint owns a proper crossing. Radius is a maximum in logical diagram units.
/// Every hop retains two logical units from a bend or greater captured endpoint-marker clearance, and is omitted below 60% of
/// the requested radius. Neighboring hops can shrink or exclude each other. Diagonal segments and genuine endpoint/junction
/// meetings receive no hop. Unsupported interpolation/authored cubics reject by default, or remain unchanged with SkipUnsupported. Live force rewrites are unsupported.
/// Null retains ordinary paint. Routes, markers, labels and relationship hits remain true native route facts.</remarks>
public sealed class TopologyCrossingPaint {
    internal const double MaximumPreparedUnits = 4096;
    /// <summary>Creates finite crossing paint in fresh native logical units.</summary>
    /// <param name="style">Native Arc or Gap presentation.</param>
    /// <param name="radius">Positive finite maximum radius, default 6, within the native bounded coordinate domain.</param>
    public TopologyCrossingPaint(TopologyCrossingPaintStyle style = TopologyCrossingPaintStyle.Arc, double radius = 6) : this(style, radius, TopologyCrossingCurveHandling.Reject, 1) { }
    /// <summary>Creates crossing paint with an explicit policy for unsupported final native curves.</summary>
    public TopologyCrossingPaint(TopologyCrossingPaintStyle style, double radius, TopologyCrossingCurveHandling curveHandling) : this(style, radius, curveHandling, 1) { }
    internal TopologyCrossingPaint(TopologyCrossingPaintStyle style, double radius, TopologyCrossingCurveHandling curveHandling, double preparedUnits) {
        if (!Enum.IsDefined(typeof(TopologyCrossingPaintStyle), style)) throw new ArgumentOutOfRangeException(nameof(style));
        if (!Enum.IsDefined(typeof(TopologyCrossingCurveHandling), curveHandling)) throw new ArgumentOutOfRangeException(nameof(curveHandling));
        if (!Finite(radius) || radius <= 0 || radius > TopologyPathGeometry.MaximumCoordinate) throw new ArgumentOutOfRangeException(nameof(radius));
        if (!Finite(preparedUnits) || preparedUnits <= 0 || preparedUnits > MaximumPreparedUnits) throw new ArgumentOutOfRangeException(nameof(preparedUnits));
        if (!Finite(2 * preparedUnits) || 2 * preparedUnits > TopologyPathGeometry.MaximumCoordinate
            || !(preparedUnits * .001 > 0) || !Finite(preparedUnits * .001) || !(preparedUnits * 1e-8 > 0) || !Finite(preparedUnits * 1e-8)) throw new ArgumentOutOfRangeException(nameof(preparedUnits));
        Style = style; CurveHandling = curveHandling; Radius = radius; PreparedUnits = preparedUnits;
    }
    /// <summary>Gets the native arc or gap policy.</summary>
    public TopologyCrossingPaintStyle Style { get; }
    /// <summary>Gets the maximum radius in this captured coordinate domain.</summary>
    public double Radius { get; }
    /// <summary>Gets the explicit handling of unsupported final native curves.</summary>
    public TopologyCrossingCurveHandling CurveHandling { get; }
    internal double PreparedUnits { get; }
    internal double BendClearance => 2 * PreparedUnits;
    internal double NumericEpsilon => 1e-8 * PreparedUnits;
    internal double MinimumRadius => Math.Max(.001 * PreparedUnits, .6 * Radius);
    internal TopologyCrossingPaint Scale(double factor) {
        if (!Finite(factor) || factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        return new TopologyCrossingPaint(Style, Radius * factor, CurveHandling, PreparedUnits * factor);
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

public sealed partial class TopologyChart {
    /// <summary>Gets or sets captured orthogonal crossing paint; null preserves ordinary native paths exactly.</summary>
    public TopologyCrossingPaint? CrossingPaint { get; set; }
}
