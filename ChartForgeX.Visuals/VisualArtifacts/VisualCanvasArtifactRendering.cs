using System;
using System.Globalization;
using ChartForgeX.Composition;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Creates neutral artifact envelopes for Visuals-owned layered canvases.</summary>
public static class VisualCanvasArtifactRendering {
    /// <summary>Wraps a canvas and its static producer without changing its authored layers.</summary>
    public static VisualArtifact ToVisualArtifact(this VisualCanvas canvas, string? id = null,
        VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var artifact = VisualArtifact.Create(string.IsNullOrWhiteSpace(id) ? "visual-canvas" : id!.Trim(),
            VisualArtifactKind.VisualCanvas, canvas);
        artifact.SourceLanguage = sourceLanguage;
        artifact.Title = canvas.Title;
        artifact.NaturalSize = new VisualArtifactSize(canvas.Width, canvas.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png |
            VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Office;
        artifact.Accessibility.Name = canvas.Accessibility.Name;
        artifact.Accessibility.Description = canvas.Accessibility.Description;
        artifact.Accessibility.Language = canvas.Accessibility.Language;
        artifact.Accessibility.IsDecorative = canvas.Accessibility.IsDecorative;
        artifact.Metadata["render.model"] = canvas.GetType().Name;
        artifact.Metadata["visual-canvas.layers"] = canvas.Layers.Count.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }
}
