using System;
using System.Globalization;
using ChartForgeX.Accessibility;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Creates visual artifact envelopes for composite and non-chart CFX surfaces.</summary>
public static class CompositeArtifactRendering {
    /// <summary>Wraps a chart grid in a reusable visual artifact.</summary>
    public static VisualArtifact ToVisualArtifact(
        this ChartGrid grid,
        string? id = null,
        VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        ChartGridLayout layout = ChartGridLayout.FromGrid(grid);
        var artifact = Create(
            grid,
            id,
            "chart-grid",
            VisualArtifactKind.ChartGrid,
            grid.Title,
            grid.Subtitle,
            layout.Width,
            layout.Height,
            sourceLanguage);
        artifact.Metadata["chart-grid.charts"] = grid.Charts.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["chart-grid.columns"] = grid.Columns.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }

    /// <summary>Wraps a layered visual canvas in a reusable visual artifact.</summary>
    public static VisualArtifact ToVisualArtifact(
        this VisualCanvas canvas,
        string? id = null,
        VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        var artifact = Create(
            canvas,
            id,
            "visual-canvas",
            VisualArtifactKind.VisualCanvas,
            canvas.Title,
            string.Empty,
            canvas.Width,
            canvas.Height,
            sourceLanguage);
        CopyAccessibility(canvas.Accessibility, artifact.Accessibility);
        artifact.Metadata["visual-canvas.layers"] = canvas.Layers.Count.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }

    /// <summary>Wraps a deterministic visual story in a reusable visual artifact.</summary>
    public static VisualArtifact ToVisualArtifact(
        this VisualStory story,
        string? id = null,
        VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var artifact = Create(
            story,
            id,
            "visual-story",
            VisualArtifactKind.Story,
            story.Title,
            story.Description,
            story.Width,
            story.Height,
            sourceLanguage);
        artifact.Accessibility.Name = story.Title;
        artifact.Accessibility.Description = story.Description;
        artifact.Metadata["visual-story.scenes"] = story.Scenes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["visual-story.outcomes"] = story.Outcomes.Count.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }

    /// <summary>Wraps a non-chart visual block in a reusable visual artifact.</summary>
    public static VisualArtifact ToVisualArtifact(
        this IVisualBlock block,
        string? id = null,
        VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        var artifact = Create(
            block,
            id,
            "visual-block",
            VisualArtifactKind.VisualBlock,
            block.Title,
            block.Subtitle,
            block.Options.Size.Width,
            block.Options.Size.Height,
            sourceLanguage);
        artifact.Accessibility.Name = block.AccessibleName;
        artifact.Metadata["visual-block.type"] = block.GetType().Name;
        return artifact;
    }

    private static VisualArtifact Create(
        object model,
        string? id,
        string defaultId,
        VisualArtifactKind kind,
        string title,
        string subtitle,
        double width,
        double height,
        VisualArtifactSourceLanguage sourceLanguage) {
        var artifact = VisualArtifact.Create(string.IsNullOrWhiteSpace(id) ? defaultId : id!.Trim(), kind, model);
        artifact.SourceLanguage = sourceLanguage;
        artifact.Title = title;
        artifact.Subtitle = subtitle;
        artifact.NaturalSize = new VisualArtifactSize(width, height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png |
                                 VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Office;
        artifact.Metadata["render.model"] = model.GetType().Name;
        return artifact;
    }

    private static void CopyAccessibility(VisualAccessibility source, VisualAccessibility target) {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Language = source.Language;
        target.IsDecorative = source.IsDecorative;
    }
}
