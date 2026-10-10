namespace ChartForgeX.Core;

/// <summary>Controls the measured label's position relative to a Sankey node bar.</summary>
public enum ChartSankeyLabelPlacement {
    /// <summary>Places the label to the left of the node bar, right aligned.</summary>
    Left,
    /// <summary>Places the label to the right of the node bar, left aligned.</summary>
    Right,
    /// <summary>Centers the label over the node bar with the existing readable backdrop.</summary>
    Center
}

/// <summary>Controls which side of the first and last Sankey columns holds their labels.</summary>
public enum ChartSankeyEdgeLabelPlacement {
    /// <summary>Places first-column labels to the right and last-column labels to the left.</summary>
    Inside,
    /// <summary>Places first-column labels to the left and last-column labels to the right.</summary>
    Outside
}
