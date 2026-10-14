using System;
using System.Globalization;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Creates neutral artifact envelopes for Visuals-owned chart and factual-block grids.</summary>
public static class VisualGridArtifactRendering {
    /// <summary>Wraps a grid using its native logical layout without rendering its children.</summary>
    /// <remarks>The envelope borrows the mutable grid, as other model artifact adapters do.
    /// Create a new envelope after changing the grid's layout or headings.</remarks>
    public static VisualArtifact ToVisualArtifact(this VisualGrid grid, string? id = null,
        VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = VisualGridLayout.FromGrid(grid);
        var artifact = VisualArtifact.Create(string.IsNullOrWhiteSpace(id) ? "visual-grid" : id!.Trim(),
            VisualArtifactKind.ChartGrid, grid);
        artifact.SourceLanguage = sourceLanguage;
        artifact.Title = grid.Title;
        artifact.Subtitle = grid.Subtitle;
        artifact.NaturalSize = new VisualArtifactSize(layout.Width, layout.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png |
            VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Office;
        artifact.Accessibility.Name = grid.Title.Length == 0 ? "ChartForgeX visual grid" : grid.Title;
        artifact.Metadata["render.model"] = nameof(VisualGrid);
        artifact.Metadata["visual-grid.items"] = grid.Items.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["visual-grid.columns"] = grid.Columns.ToString(CultureInfo.InvariantCulture);
        return artifact;
    }
}
