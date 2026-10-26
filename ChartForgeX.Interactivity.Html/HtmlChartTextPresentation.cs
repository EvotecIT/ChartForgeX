namespace ChartForgeX.Interactivity.Html;

/// <summary>Controls live HTML text while native layout and detached export keep their prepared decisions.</summary>
public enum HtmlChartTextPresentation {
    /// <summary>Loads eligible owned single-face fonts from bounded binary snapshots; captured native ink remains visible when loading is unsupported or fails. Non-outline ordinary lines retain bounded native raster ink.</summary>
    Automatic,
    /// <summary>Uses the existing captured native glyph contours without browser font loading.</summary>
    NativeContours,
    /// <summary>Uses the host's browser font system and the incumbent native PNG-export capture.</summary>
    Browser
}
