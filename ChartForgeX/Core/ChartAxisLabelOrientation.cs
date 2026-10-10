namespace ChartForgeX.Core;

/// <summary>Defines how a label's baseline follows its axis before applying <see cref="ChartAxis.LabelAngle"/>.</summary>
public enum ChartAxisLabelOrientation {
    /// <summary>Use the authored angle in fixed screen degrees.</summary>
    Fixed,

    /// <summary>Follow the local axis track: the tangent of an angular axis or the spoke of a radius axis.</summary>
    Parallel,

    /// <summary>Use the direction perpendicular to the local axis track.</summary>
    Perpendicular
}
