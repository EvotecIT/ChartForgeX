using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

public static partial class V2Examples {
    private static readonly ChartSeriesKind[] SparseFamilies = {
        ChartSeriesKind.Line, ChartSeriesKind.Area, ChartSeriesKind.Scatter, ChartSeriesKind.RangeArea, ChartSeriesKind.Heatmap, ChartSeriesKind.CalendarHeatmap
    };
    private static readonly ChartSeriesKind[] RelationshipOptionFamilies = {
        ChartSeriesKind.Tree, ChartSeriesKind.Sunburst, ChartSeriesKind.Sankey, ChartSeriesKind.Chord
    };
    private static readonly ChartSeriesKind[] OptionFamilies = {
        ChartSeriesKind.Bar, ChartSeriesKind.Area, ChartSeriesKind.HorizontalBar, ChartSeriesKind.Pie, ChartSeriesKind.Donut, ChartSeriesKind.Gauge, ChartSeriesKind.Bullet,
        ChartSeriesKind.RadialBar, ChartSeriesKind.Heatmap, ChartSeriesKind.RegionMap, ChartSeriesKind.TileMap, ChartSeriesKind.Pictorial, ChartSeriesKind.ProgressBar
    };

    private static void WriteFamilies(string output, ICollection<ProofArtifact> artifacts, bool curated) {
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var variants = new List<string> { "wide", "compact" };
            if (RelationshipOptionFamilies.Contains(kind)) variants.Add("options");
            if (!curated) {
                if (SparseFamilies.Contains(kind)) variants.Add("sparse");
                if (OptionFamilies.Contains(kind)) variants.Add("options");
            }
            foreach (var variant in variants) {
                var chart = V2GalleryModels.Create(kind, variant, mode); var title = V2GalleryModels.Title(kind);
                if (!chart.Series.Any(series => series.Kind == kind)) throw new InvalidOperationException("The gallery factory did not create its declared chart kind: " + kind);
                var family = FamilyName(kind); var id = "family-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                var width = variant == "compact" ? 360 : 800; var height = variant == "compact" ? 360 : 440;
                // Let the shared policy decide whether a legend adds information. The indicator
                // options example intentionally demonstrates an explicitly requested legend.
                bool? legend = variant == "options" && kind is ChartSeriesKind.Gauge or ChartSeriesKind.Bullet ? true : null;
                var subtitle = variant switch { "sparse" => "Missing observations remain visible as gaps", "options" => "Explore configured marks, scales and labels", "compact" => "The same data in a compact view", _ => "Explore the data, then download the chart" };
                WriteModel(output, artifacts, chart, id, family, title, variant, subtitle, mode, width, height, legend,
                    "V2GalleryModels.Create(ChartSeriesKind." + kind + ", " + Literal(variant) + ", VisualThemeMode." + mode + ")", chart.Series.Select(series => series.Kind.ToString()).Distinct().ToArray());
            }
        }
        WriteExpandedDiagrams(output, artifacts, curated);
    }

    private static void WriteModel(string output, ICollection<ProofArtifact> artifacts, IVisualRenderable model, string id, string family, string title,
        string variant, string subtitle, VisualThemeMode mode, int width, int height, bool? legend, string expression, string[]? kinds = null) {
        var context = GalleryContext(width, height, mode, title, subtitle, legend);
        var prepared = model.Prepare(context);
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
        File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
        WriteThumbnail(output, model, id, title, family, mode, legend);
        var artifactKind = family is "topology" or "flow" or "sequence" ? char.ToUpperInvariant(family[0]) + family.Substring(1) : "Chart";
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"), ModelSnippet(expression, title, subtitle, mode, width, height, legend, artifactKind));
        artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
            prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count, kinds,
            prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray()));
    }

    private static VisualRenderContext GalleryContext(int width, int height, VisualThemeMode mode, string title, string subtitle, bool? legend) =>
        new(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode, new VisualFrame(title, subtitle, showLegend: legend), FontSpec.FromFamily(ProofFont));
    private static string FamilyName(ChartSeriesKind kind) => System.Text.RegularExpressions.Regex.Replace(kind.ToString(), "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
}
