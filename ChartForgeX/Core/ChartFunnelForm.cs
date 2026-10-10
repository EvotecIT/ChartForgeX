namespace ChartForgeX.Core;

/// <summary>Chooses the value-bearing marks of a funnel.</summary>
public enum ChartFunnelForm {
    /// <summary>Uses centered bars whose cross-axis lengths are proportional to each stage value.</summary>
    StageBars,
    /// <summary>Uses proportional stage lines connected by filled regions between adjacent stages.</summary>
    Cone
}
