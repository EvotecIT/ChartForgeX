namespace ChartForgeX.Effects;

/// <summary>Selects which captured mask channels determine coverage.</summary>
public enum VisualMaskMode {
    /// <summary>Uses alpha alone; RGB has no effect.</summary>
    Alpha = 0,
    /// <summary>Uses unassociated sRGB luminance (0.2126 R + 0.7152 G + 0.0722 B), multiplied by mask alpha.</summary>
    Luminance = 1
}
