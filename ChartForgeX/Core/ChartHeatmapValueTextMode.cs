namespace ChartForgeX.Core;

/// <summary>
/// Controls when heatmap cell values are rendered as text.
/// </summary>
public enum ChartHeatmapValueTextMode {
    /// <summary>Use the standard data-label setting and only draw labels when cells have enough room.</summary>
    Auto,
    /// <summary>Always draw heatmap cell values, even when general data labels are disabled.</summary>
    Always,
    /// <summary>Never draw heatmap cell values.</summary>
    Hidden
}
