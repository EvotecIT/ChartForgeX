using ChartForgeX.Accessibility;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class VisualArtifact {
    /// <summary>Copies the host envelope so presentation and metadata can be edited independently.</summary>
    /// <remarks>
    /// The semantic model and producer-owned render source are shared. Metadata, accessibility, regions and
    /// legend items are copied. This does not clone an authored model; prepare it first when detached geometry is required.
    /// </remarks>
    public VisualArtifact CopyEnvelope() {
        var copy = new VisualArtifact {
            Id = Id, Kind = Kind, SourceLanguage = SourceLanguage, Title = Title, Subtitle = Subtitle,
            Model = Model, RenderSource = RenderSource, ExportFormats = ExportFormats,
            NaturalSize = NaturalSize, PreserveNaturalSize = PreserveNaturalSize,
            HasModelAccessibilitySnapshot = HasModelAccessibilitySnapshot,
            TopologyNaturalSizeSnapshot = TopologyNaturalSizeSnapshot,
            PreparedInterchangeSnapshot = PreparedInterchangeSnapshot
        };
        CopyAccessibility(Accessibility, copy.Accessibility);
        CopyAccessibility(ModelAccessibilitySnapshot, copy.ModelAccessibilitySnapshot);
        foreach (var pair in Metadata) copy.Metadata.Add(pair.Key, pair.Value);
        foreach (var region in Regions) {
            var target = new VisualArtifactRegion {
                Id = region.Id, Kind = region.Kind, Label = region.Label, Bounds = region.Bounds,
                Href = region.Href, AlternativeText = region.AlternativeText
            };
            foreach (var pair in region.Metadata) target.Metadata.Add(pair.Key, pair.Value);
            copy.Regions.Add(target);
        }
        foreach (var item in Legend)
            copy.Legend.Add(new VisualArtifactLegendItem { Id = item.Id, Label = item.Label, Color = item.Color });
        return copy;
    }

    private static void CopyAccessibility(VisualAccessibility source, VisualAccessibility target) {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Language = source.Language;
        target.IsDecorative = source.IsDecorative;
    }
}
