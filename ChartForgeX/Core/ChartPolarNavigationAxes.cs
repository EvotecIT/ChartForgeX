using System;

namespace ChartForgeX.Core;

/// <summary>Selects captured continuous Polar coordinates for a native navigation operation.</summary>
[Flags]
public enum ChartPolarNavigationAxes {
    /// <summary>Selects no coordinate. Navigation rejects this value.</summary>
    None = 0,
    /// <summary>Selects the directional sector, in authored radians.</summary>
    Angle = 1,
    /// <summary>Selects the primary source-radius domain.</summary>
    Radius = 2,
    /// <summary>Selects the secondary source-radius domain.</summary>
    SecondaryRadius = 4
}
