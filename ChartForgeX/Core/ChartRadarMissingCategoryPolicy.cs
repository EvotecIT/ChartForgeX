namespace ChartForgeX.Core;

/// <summary>Controls categories present in another radar or radial series but absent from this series.</summary>
public enum ChartRadarMissingCategoryPolicy {
    /// <summary>Preserves the existing derived-zero vertices, distinctly marked as missing rather than authored observations.</summary>
    Zero,
    /// <summary>Breaks the outline at missing categories. Incomplete area forms omit their fill rather than bridge an unknown sector.</summary>
    Gap,
    /// <summary>Connects the authored vertices across missing categories, without creating zero vertices or observations.</summary>
    Connect
}
