using System;

namespace ChartForgeX.Core;

/// <summary>Chooses the numeric columns accompanying native pie and donut legend labels.</summary>
public enum ChartPieLegendContent {
    /// <summary>Shows the category label without numeric columns.</summary>
    Label,
    /// <summary>Shows the category label and its authored numeric value.</summary>
    LabelAndValue
}

public sealed partial class ChartOptions {
}
