namespace ChartForgeX.Composition;

/// <summary>
/// Controls how information tile text is fitted when the tile is narrow or short.
/// </summary>
public enum VisualCanvasTextFitPolicy {
    /// <summary>Use the balanced default: wrap when possible and shrink slightly before truncating.</summary>
    Auto,
    /// <summary>Keep each tile text role on one line and trim with an ellipsis when needed.</summary>
    SingleLineEllipsis,
    /// <summary>Wrap text across the available tile lines and trim only the final overflowing line.</summary>
    Wrap,
    /// <summary>Prefer one-line text and reduce font sizes before trimming with an ellipsis.</summary>
    ShrinkToFit,
    /// <summary>Wrap text first, then reduce font sizes if the tile still cannot contain the text.</summary>
    WrapThenShrink
}
