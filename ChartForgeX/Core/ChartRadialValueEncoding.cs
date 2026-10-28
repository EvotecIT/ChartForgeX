namespace ChartForgeX.Core;

/// <summary>Selects whether a categorical radial column encodes its value interval as radius or sector area.</summary>
public enum ChartRadialValueEncoding {
    /// <summary>Radius changes linearly across the selected value scale. This is the existing numeric radial default.</summary>
    Radius = 0,
    /// <summary>Squared radius changes linearly across the visible value interval, including an authored annular hole.</summary>
    /// <remarks>Requires homogeneous RadialColumn series with linear, nonnegative value domains and observations. Raw-value area proportionality requires a zero baseline; spacing, rounded corners and clipping can change painted pixel area.</remarks>
    Area = 1
}
