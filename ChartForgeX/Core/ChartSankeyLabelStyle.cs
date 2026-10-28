namespace ChartForgeX.Core;

/// <summary>Controls native Sankey caption paint without changing measured text, node placement or semantic facts.</summary>
public enum ChartSankeyLabelStyle {
    /// <summary>Retains the native default rectangular surface behind each caption.</summary>
    Backdrop,
    /// <summary>Paints the caption without a backdrop or outline. This maps Mermaid's explicit legacy policy.</summary>
    Plain,
    /// <summary>Paints a four-unit surface-colored text outline through the shared native text owner.</summary>
    /// <remarks>Measured label columns and shortening remain native policies; this is not an upstream browser layout clone.</remarks>
    Outlined
}
