namespace ChartForgeX.Core;

/// <summary>Selects how unrequested value-axis domains are resolved during Cartesian window preparation.</summary>
public enum ChartValueDomain {
    /// <summary>Retains the resolved full-data value domains.</summary>
    FullData,
    /// <summary>Fits each value axis to geometry intersecting the category window, preserving authored axis bounds.</summary>
    /// <remarks>Continuous segments contribute their clipped boundary values. Discrete intervals contribute their complete
    /// value extent when their category footprint intersects. Full-data stacks stay unchanged. An empty window retains the full domain.</remarks>
    VisibleCategories
}
