using ChartForgeX.Accessibility;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class VisualArtifact {
    /// <summary>Copies the artifact's host envelope independently of its semantic model and static presentation.</summary>
    /// <returns>A new artifact with independent metadata, accessibility, regions, and legend items.</returns>
    /// <remarks>
    /// This is an envelope copy, not a deep model or pixel copy. <see cref="Model"/> and
    /// <see cref="RenderSource"/> retain their original references and ordinary producer mutability and lifetime
    /// requirements, including any borrowed image buffers. Host collections, their entries, and producer snapshots
    /// are copied so editing the returned envelope does not change this artifact's host state.
    /// </remarks>
    public VisualArtifact Clone() {
        var copy = new VisualArtifact {
            Id = Id, Kind = Kind, SourceLanguage = SourceLanguage,
            Title = Title, Subtitle = Subtitle, Model = Model, RenderSource = RenderSource,
            ExportFormats = ExportFormats, NaturalSize = NaturalSize, PreserveNaturalSize = PreserveNaturalSize,
            HasModelAccessibilitySnapshot = HasModelAccessibilitySnapshot,
            TopologyNaturalSizeSnapshot = TopologyNaturalSizeSnapshot,
            PreparedInterchangeSnapshot = PreparedInterchangeSnapshot
        };
        CopyAccessibility(Accessibility, copy.Accessibility);
        CopyAccessibility(ModelAccessibilitySnapshot, copy.ModelAccessibilitySnapshot);
        foreach (var pair in Metadata) copy.Metadata.Add(pair.Key, pair.Value);
        foreach (var region in Regions) {
            var copiedRegion = new VisualArtifactRegion {
                Id = region.Id, Kind = region.Kind, Label = region.Label, Bounds = region.Bounds,
                Href = region.Href, AlternativeText = region.AlternativeText
            };
            foreach (var pair in region.Metadata) copiedRegion.Metadata.Add(pair.Key, pair.Value);
            copy.Regions.Add(copiedRegion);
        }
        foreach (var item in Legend) {
            copy.Legend.Add(new VisualArtifactLegendItem { Id = item.Id, Label = item.Label, Color = item.Color });
        }
        return copy;
    }

    private static void CopyAccessibility(VisualAccessibility source, VisualAccessibility target) {
        target.Name = source.Name; target.Description = source.Description;
        target.Language = source.Language; target.IsDecorative = source.IsDecorative;
    }
}
