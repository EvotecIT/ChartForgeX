namespace ChartForgeX.Core;

/// <summary>Selects the key a legend entry draws beside its label.</summary>
public enum ChartLegendKeyShape {
    /// <summary>The family's own key: a short line for stroked series, a dot for point series, a rounded square for filled marks, with the series marker when one is configured.</summary>
    Auto,
    /// <summary>A rounded square filled with the entry colour and its fill pattern.</summary>
    Square,
    /// <summary>A filled circle.</summary>
    Circle,
    /// <summary>A short horizontal line, dashed when the series or item has a dash pattern.</summary>
    Line,
    /// <summary>A marker shape: the series' resolved marker, or the shape a custom item names.</summary>
    Marker
}
