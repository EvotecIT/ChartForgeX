namespace ChartForgeX.Core;

/// <summary>Defines how connected observations form a line or an area boundary.</summary>
public enum ChartInterpolation {
    /// <summary>Connects consecutive observations with straight segments.</summary>
    Linear,
    /// <summary>Connects observations with the shared smooth cubic curve.</summary>
    Smooth,
    /// <summary>Connects observations with horizontal and vertical steps.</summary>
    Step
}
