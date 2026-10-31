using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Playground;

public static partial class PlaygroundEngine {
    private const string PreviewFontFamily = "Carlito, sans-serif";
    private static readonly VisualTheme PreviewTheme = VisualTheme.Graphite().WithFontFamily(PreviewFontFamily);

    // Only replace the host's default face. An authored Mermaid font remains authored intent;
    // the playground does not promise to deliver arbitrary custom font families.
    private static bool UsesPreviewFont(VisualArtifact artifact) => !artifact.Metadata.ContainsKey("mermaid.config.fontFamily");

    private static void ApplyPreviewFont(VisualArtifact artifact) {
        if (!UsesPreviewFont(artifact)) return;
        switch (artifact.Model) {
            case Chart chart:
                chart.WithTheme(chart.Options.Theme.WithFontFamily(PreviewFontFamily));
                break;
            case ChartGrid grid:
                grid.Theme = grid.Theme.WithFontFamily(PreviewFontFamily);
                break;
            case TopologyChart topology:
                topology.WithTheme(topology.Theme.WithFontFamily(PreviewFontFamily));
                break;
            case SequenceArtifact sequence:
                sequence.Theme = sequence.Theme.WithFontFamily(PreviewFontFamily);
                break;
            case IVisualBlock block when artifact.RenderSource == null:
                block.Options.Theme = block.Options.Theme.WithFontFamily(PreviewFontFamily);
                if (block is RailroadDiagramBlock railroad) {
                    // The producer measured before the host changed its font; retain that frame as a minimum.
                    var minimum = railroad.Options.Size;
                    railroad.WithNaturalSize(minimum.Width, minimum.Height);
                    artifact.NaturalSize = new VisualArtifactSize(railroad.Options.Size.Width, railroad.Options.Size.Height);
                }
                break;
        }
    }
}
