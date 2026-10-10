namespace ChartForgeX.Core;

/// <summary>Describes how a numeric color scale selects its colors.</summary>
public enum ChartColorScaleMode {
    /// <summary>Interpolates evenly between ordered color stops.</summary>
    Sequential,
    /// <summary>Interpolates independently on each side of a midpoint stop.</summary>
    Diverging,
    /// <summary>Selects one solid color from ordered exclusive numeric bands.</summary>
    Discrete
}
