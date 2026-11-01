namespace ChartForgeX.Core;

/// <summary>Determines how a rectangular heatmap handles columns that do not fit at a readable width.</summary>
public enum ChartHeatmapColumnWindow {
    /// <summary>Show the latest columns at a readable width and describe the omitted earlier columns.</summary>
    Latest,
    /// <summary>Show every column, even when the frame makes the cells narrow.</summary>
    Squeeze
}
