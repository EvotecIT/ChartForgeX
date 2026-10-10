namespace ChartForgeX.Core;

/// <summary>Defines the shape of concentric guides on radar and polar line charts.</summary>
public enum ChartPolarGridShape {
    /// <summary>Uses polygon guides for radar and circular guides for polar charts.</summary>
    Automatic,
    /// <summary>Uses circular guides without changing the series geometry.</summary>
    Circle,
    /// <summary>Joins axis spokes with closed polygon guides.</summary>
    Polygon
}
