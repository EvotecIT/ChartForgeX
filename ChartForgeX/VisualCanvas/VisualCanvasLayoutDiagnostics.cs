using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Composition;

/// <summary>
/// Severity for visual canvas layout diagnostics.
/// </summary>
public enum VisualCanvasLayoutDiagnosticSeverity {
    /// <summary>The layout is valid, but the report contains useful advisory information.</summary>
    Info,
    /// <summary>The layout can render, but may clip, trim, or overlap important content.</summary>
    Warning,
    /// <summary>The layout contains invalid state that renderers should not accept.</summary>
    Error
}

/// <summary>
/// A single layout diagnostic produced for a visual canvas layer.
/// </summary>
public sealed class VisualCanvasLayoutDiagnostic {
    internal VisualCanvasLayoutDiagnostic(VisualCanvasLayoutDiagnosticSeverity severity, string code, string message, int layerIndex, string layerType, ChartRect bounds) {
        Severity = severity;
        Code = code;
        Message = message;
        LayerIndex = layerIndex;
        LayerType = layerType;
        Bounds = bounds;
    }

    /// <summary>Gets the diagnostic severity.</summary>
    public VisualCanvasLayoutDiagnosticSeverity Severity { get; }
    /// <summary>Gets the stable diagnostic code.</summary>
    public string Code { get; }
    /// <summary>Gets a human-readable diagnostic message.</summary>
    public string Message { get; }
    /// <summary>Gets the zero-based layer index.</summary>
    public int LayerIndex { get; }
    /// <summary>Gets the layer type name.</summary>
    public string LayerType { get; }
    /// <summary>Gets the layer bounds used by the diagnostic.</summary>
    public ChartRect Bounds { get; }
}

/// <summary>
/// Summary of visual canvas layout diagnostics.
/// </summary>
public sealed class VisualCanvasLayoutReport {
    internal VisualCanvasLayoutReport(IReadOnlyList<VisualCanvasLayoutDiagnostic> diagnostics) {
        Diagnostics = diagnostics;
        var hasWarnings = false;
        var hasErrors = false;
        foreach (var diagnostic in diagnostics) {
            hasWarnings |= diagnostic.Severity == VisualCanvasLayoutDiagnosticSeverity.Warning;
            hasErrors |= diagnostic.Severity == VisualCanvasLayoutDiagnosticSeverity.Error;
        }

        HasWarnings = hasWarnings;
        HasErrors = hasErrors;
    }

    /// <summary>Gets diagnostics produced for the canvas.</summary>
    public IReadOnlyList<VisualCanvasLayoutDiagnostic> Diagnostics { get; }
    /// <summary>Gets whether the report contains warnings.</summary>
    public bool HasWarnings { get; }
    /// <summary>Gets whether the report contains errors.</summary>
    public bool HasErrors { get; }
    /// <summary>Gets whether the report contains no warnings or errors.</summary>
    public bool IsClean => !HasWarnings && !HasErrors;
}

internal static class VisualCanvasLayoutAnalyzer {
    public static VisualCanvasLayoutReport Analyze(VisualCanvas canvas) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var diagnostics = new List<VisualCanvasLayoutDiagnostic>();
        var canvasBounds = new ChartRect(0, 0, canvas.DesignWidth, canvas.DesignHeight);
        for (var i = 0; i < canvas.Layers.Count; i++) {
            var layer = canvas.Layers[i];
            var bounds = layer.Bounds;
            if (bounds.X < 0 || bounds.Y < 0 || bounds.Right > canvasBounds.Right || bounds.Bottom > canvasBounds.Bottom) {
                diagnostics.Add(new VisualCanvasLayoutDiagnostic(
                    VisualCanvasLayoutDiagnosticSeverity.Warning,
                    "LayerOutsideCanvas",
                    "Layer bounds extend beyond the visual canvas and may be clipped.",
                    i,
                    layer.GetType().Name,
                    bounds));
            }

            if (layer is VisualCanvasInfoTileLayer tile) AnalyzeInfoTile(tile, i, diagnostics);
        }

        return new VisualCanvasLayoutReport(diagnostics);
    }

    private static void AnalyzeInfoTile(VisualCanvasInfoTileLayer tile, int layerIndex, List<VisualCanvasLayoutDiagnostic> diagnostics) {
        VisualCanvas.ValidateEnum(tile.SurfaceStyle, nameof(tile.SurfaceStyle));
        VisualCanvas.ValidateEnum(tile.IconKind, nameof(tile.IconKind));
        VisualCanvas.ValidateEnum(tile.MiniChartKind, nameof(tile.MiniChartKind));
        VisualCanvas.ValidateEnum(tile.TextFitPolicy, nameof(tile.TextFitPolicy));
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile);
        if (metrics.TextMax <= 28) {
            diagnostics.Add(new VisualCanvasLayoutDiagnostic(
                VisualCanvasLayoutDiagnosticSeverity.Warning,
                "InfoTileTextAreaTooSmall",
                "Information tile leaves too little horizontal space for label and value text.",
                layerIndex,
                nameof(VisualCanvasInfoTileLayer),
                tile.Bounds));
        }

        var layout = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height, metrics.TextX, metrics.TextMax);
        if (layout.HasTruncatedText) {
            diagnostics.Add(new VisualCanvasLayoutDiagnostic(
                VisualCanvasLayoutDiagnosticSeverity.Warning,
                "InfoTileTextTrimmed",
                "Information tile text was fitted with an ellipsis because the tile cannot contain the full label, value, or detail.",
                layerIndex,
                nameof(VisualCanvasInfoTileLayer),
                tile.Bounds));
        }

        if (layout.HasVerticalOverflow || layout.TextHeight > Math.Max(1, metrics.Height - 12)) {
            diagnostics.Add(new VisualCanvasLayoutDiagnostic(
                VisualCanvasLayoutDiagnosticSeverity.Warning,
                "InfoTileTextTooTall",
                "Information tile text requires more vertical space than the tile makes comfortably available.",
                layerIndex,
                nameof(VisualCanvasInfoTileLayer),
                tile.Bounds));
        }
    }
}
