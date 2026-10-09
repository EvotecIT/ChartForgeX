namespace ChartForgeX.Core;

/// <summary>Chooses which geometric share of a pyramid represents each source value.</summary>
public enum ChartPyramidValueEncoding {
    /// <summary>Segment length along the tip-to-base axis is proportional to value.</summary>
    Height,
    /// <summary>Segment area is proportional to value; lengths follow square-root boundaries.</summary>
    Area
}
