namespace ChartForgeX.Themes;

/// <summary>Identifies a canonical theme color independently of its current RGB value.</summary>
public enum VisualThemeColorToken {
    /// <summary>The outer visual background.</summary>
    Background,
    /// <summary>The main content surface.</summary>
    Surface,
    /// <summary>An elevated card or panel surface.</summary>
    ElevatedSurface,
    /// <summary>The primary text and foreground color.</summary>
    Foreground,
    /// <summary>The secondary text and foreground color.</summary>
    MutedForeground,
    /// <summary>The shared border color.</summary>
    Border,
    /// <summary>The primary accent color.</summary>
    Accent,
    /// <summary>The positive or healthy state color.</summary>
    Positive,
    /// <summary>The warning state color.</summary>
    Warning,
    /// <summary>The negative or failing state color.</summary>
    Negative,
    /// <summary>The neutral state color.</summary>
    Neutral,
    /// <summary>The maintenance state color.</summary>
    Maintenance
}
