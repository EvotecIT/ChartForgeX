using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using System.Xml.Linq;

public static partial class V2Examples {
    private static void WriteRadialLabels(string output, ICollection<ProofArtifact> artifacts) {
        foreach (var kind in new[] { ChartSeriesKind.Pie, ChartSeriesKind.Donut }) {
            foreach (var placement in new[] { ChartDataLabelPlacement.Above, ChartDataLabelPlacement.Below }) {
                foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                    var family = kind.ToString().ToLowerInvariant();
                    var variant = placement.ToString().ToLowerInvariant() + "-labels";
                    var id = family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                    var recipe = ExampleRecipes.Named("equal-" + family);
                    var chart = (Chart)recipe.Create(variant, mode);
                    var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)),
                        VisualTheme.Graphite(), mode, new VisualFrame("Four equal shares", "Labels " + placement.ToString().ToLowerInvariant(), showLegend: false), FontSpec.FromFamily(ProofFont));
                    var prepared = chart.Prepare(context);
                    var svg = prepared.ToSvg(id);
                    if (prepared.Diagnostics.Any(diagnostic => diagnostic.Code == "radial.label-overflow") ||
                        XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "data-label") != 4)
                        throw new InvalidOperationException("The symmetric radial proof lost a label: " + id);
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), svg);
                    File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                    WriteThumbnail(output, chart, id, "Four equal shares", family, mode, legend: false);
                    WriteRecipe(output, id, recipe, variant, "Four equal shares", "Labels " + placement.ToString().ToLowerInvariant(), mode, 640, 400, false);
                    artifacts.Add(new ProofArtifact(id, family, "Four equal shares", variant, mode.ToString().ToLowerInvariant(), 640, 400,
                        prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count,
                        DiagnosticMessages: prepared.Diagnostics.Select(diagnostic => diagnostic.Message).ToArray()));
                }
            }
        }
    }

}
