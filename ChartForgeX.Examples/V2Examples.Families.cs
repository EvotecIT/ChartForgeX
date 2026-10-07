using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using System.Xml.Linq;

public static partial class V2Examples {
    private static readonly ChartSeriesKind[] CompactFamilies = {
        ChartSeriesKind.Bubble, ChartSeriesKind.BoxPlot, ChartSeriesKind.Heatmap, ChartSeriesKind.CalendarHeatmap, ChartSeriesKind.RegionMap,
        ChartSeriesKind.RadialBar, ChartSeriesKind.Tree, ChartSeriesKind.Sunburst, ChartSeriesKind.GanttLane, ChartSeriesKind.Pictorial
    };
    private static readonly ChartSeriesKind[] SparseFamilies = {
        ChartSeriesKind.Line, ChartSeriesKind.Area, ChartSeriesKind.Scatter, ChartSeriesKind.RangeArea, ChartSeriesKind.Heatmap, ChartSeriesKind.CalendarHeatmap
    };
    private static readonly ChartSeriesKind[] OptionFamilies = {
        ChartSeriesKind.Bar, ChartSeriesKind.Area, ChartSeriesKind.HorizontalBar, ChartSeriesKind.Pie, ChartSeriesKind.Donut, ChartSeriesKind.Gauge,
        ChartSeriesKind.RadialBar, ChartSeriesKind.Heatmap, ChartSeriesKind.RegionMap, ChartSeriesKind.TileMap, ChartSeriesKind.Pictorial, ChartSeriesKind.ProgressBar
    };

    private static void WriteFamilies(string output, ICollection<ProofArtifact> artifacts, bool curated) {
        foreach (var kind in Enum.GetValues<ChartSeriesKind>()) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var variants = new List<string> { "wide" };
            if (!curated) {
                if (CompactFamilies.Contains(kind)) variants.Add("compact");
                if (SparseFamilies.Contains(kind)) variants.Add("sparse");
                if (OptionFamilies.Contains(kind)) variants.Add("options");
            }
            foreach (var variant in variants) {
                var chart = V2GalleryModels.Create(kind, variant, mode); var title = V2GalleryModels.Title(kind);
                if (!chart.Series.Any(series => series.Kind == kind)) throw new InvalidOperationException("The gallery factory did not create its declared chart kind: " + kind);
                var family = FamilyName(kind); var id = "family-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                var width = variant == "compact" ? 360 : 800; var height = variant == "compact" ? 300 : 440;
                var legend = chart.Series.Count > 1 || kind is ChartSeriesKind.Pie or ChartSeriesKind.Donut or ChartSeriesKind.RadialBar or ChartSeriesKind.LayeredRadial;
                var subtitle = variant switch { "sparse" => "Sparse observations retain gaps", "options" => "Configured marks, scales or labels", "compact" => "A fixed compact viewport", _ => "SVG and native PNG from one prepared scene" };
                WriteModel(output, artifacts, chart, id, family, title, variant, subtitle, mode, width, height, legend,
                    "V2GalleryModels.Create(ChartSeriesKind." + kind + ", " + Literal(variant) + ", VisualThemeMode." + mode + ")", chart.Series.Select(series => series.Kind.ToString()).Distinct().ToArray());
            }
        }
        WriteExpandedDiagrams(output, artifacts, curated);
    }

    private static void WriteModel(string output, ICollection<ProofArtifact> artifacts, IVisualRenderable model, string id, string family, string title,
        string variant, string subtitle, VisualThemeMode mode, int width, int height, bool legend, string expression, string[]? kinds = null) {
        var context = GalleryContext(width, height, mode, title, subtitle, legend);
        var prepared = model.Prepare(context);
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), prepared.ToSvg(id));
        File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
        // Sequence steps retain their full logical viewport; the host scales this SVG into the uniform tile.
        var thumbnail = model.Prepare(GalleryContext(family == "sequence" ? width : 640, family == "sequence" ? height : 400, mode, "", "", legend));
        var thumbnailSvg = thumbnail.ToSvg(new VisualSvgOptions(idPrefix: id + "-thumbnail", responsive: false));
        if (family == "sequence") {
            var document = XDocument.Parse(thumbnailSvg, LoadOptions.PreserveWhitespace);
            var root = document.Root ?? throw new InvalidOperationException("The sequence thumbnail requires an SVG root.");
            root.SetAttributeValue("width", 640); root.SetAttributeValue("height", 400);
            root.SetAttributeValue("preserveAspectRatio", "xMidYMid meet");
            thumbnailSvg = document.ToString(SaveOptions.DisableFormatting);
        }
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".thumbnail.svg"), thumbnailSvg);
        var artifactKind = family is "topology" or "flow" or "sequence" ? char.ToUpperInvariant(family[0]) + family.Substring(1) : "Chart";
        ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"), ModelSnippet(expression, title, subtitle, mode, width, height, legend, artifactKind));
        WritePage(output, id, title, mode);
        artifacts.Add(new ProofArtifact(id, family, title, variant, mode.ToString().ToLowerInvariant(), width, height,
            prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count, kinds));
    }

    private static VisualRenderContext GalleryContext(int width, int height, VisualThemeMode mode, string title, string subtitle, bool legend) =>
        new(new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode, new VisualFrame(title, subtitle, showLegend: legend), FontSpec.FromFamily(ProofFont));
    private static string FamilyName(ChartSeriesKind kind) => System.Text.RegularExpressions.Regex.Replace(kind.ToString(), "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
}
