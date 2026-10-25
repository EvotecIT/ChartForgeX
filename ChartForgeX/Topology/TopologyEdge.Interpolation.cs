namespace ChartForgeX.Topology;

/// <summary>Selects numeric interpolation over an already resolved native route.</summary>
public enum TopologyEdgeInterpolation {
    /// <summary>Connects consecutive route vertices with straight segments.</summary>
    Linear = 0,
    /// <summary>Uses a clamped uniform cubic B-spline with retained endpoints.</summary>
    Basis = 1,
    /// <summary>Uses midpoint horizontal cubic controls.</summary>
    BumpX = 2,
    /// <summary>Uses midpoint vertical cubic controls.</summary>
    BumpY = 3,
    /// <summary>Uses a zero-tension cardinal spline with stationary endpoint controls.</summary>
    Cardinal = 4,
    /// <summary>Uses centripetal Catmull–Rom interpolation with exponent one half.</summary>
    CatmullRom = 5,
    /// <summary>Uses slope-limited cubic Hermite interpolation along the horizontal axis.</summary>
    MonotoneX = 6,
    /// <summary>Uses slope-limited cubic Hermite interpolation along the vertical axis.</summary>
    MonotoneY = 7,
    /// <summary>Uses a natural cubic spline with zero endpoint second derivatives.</summary>
    Natural = 8,
    /// <summary>Changes ordinate at the midpoint of each horizontal interval.</summary>
    Step = 9,
    /// <summary>Changes ordinate after each horizontal interval.</summary>
    StepAfter = 10,
    /// <summary>Changes ordinate before each horizontal interval.</summary>
    StepBefore = 11,
    /// <summary>Rounds route junctions with a five model-unit quadratic corner parameter, bounded by adjacent lengths.</summary>
    Rounded = 12
}

public sealed partial class TopologyEdge {
    private TopologyEdgeInterpolation? _interpolation;
    /// <summary>Gets or sets interpolation over the resolved route; null retains incumbent route painting exactly.</summary>
    /// <remarks>This is independent of route selection. A non-null value cannot combine with authored PathGeometry.
    /// It is captured through the common version2 numeric path contract.</remarks>
    public TopologyEdgeInterpolation? Interpolation {
        get => _interpolation;
        set { if (value.HasValue) TopologyModelGuards.EnumDefined(value.Value, nameof(value)); _interpolation = value; }
    }
}
