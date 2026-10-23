namespace ChartForgeX.Core;

/// <summary>Defines the data captions rendered for a box plot summary.</summary>
public enum ChartBoxPlotLabelContent {
    /// <summary>Renders the median or an authored point heading, preserving the default output.</summary>
    Median,
    /// <summary>Renders independently configured minimum, quartile, median and maximum captions.</summary>
    Statistics
}
