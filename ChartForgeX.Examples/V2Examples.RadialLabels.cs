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
                    var chart = CreateRadialLabels(kind, placement);
                    var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)),
                        VisualTheme.Graphite(), mode, new VisualFrame("Four equal shares", "Labels " + placement.ToString().ToLowerInvariant(), showLegend: false), FontSpec.FromFamily(ProofFont));
                    var prepared = chart.Prepare(context);
                    var svg = prepared.ToSvg(id);
                    if (prepared.Diagnostics.Any(diagnostic => diagnostic.Code == "radial.label-overflow") ||
                        XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "data-label") != 4)
                        throw new InvalidOperationException("The symmetric radial proof lost a label: " + id);
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".svg"), svg);
                    File.WriteAllBytes(Path.Combine(output, id + ".png"), prepared.ToPng());
                    var thumbnail = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)),
                        VisualTheme.Graphite(), mode, new VisualFrame("", "", showLegend: false), FontSpec.FromFamily(ProofFont)));
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".thumbnail.svg"), thumbnail.ToSvg(id + "-thumbnail"));
                    ExampleArtifactWriter.WriteText(Path.Combine(output, id + ".csharp.txt"), RadialLabelSnippet(kind, placement, mode));
                    WritePage(output, id, "Four equal shares", mode);
                    artifacts.Add(new ProofArtifact(id, family, "Four equal shares", variant, mode.ToString().ToLowerInvariant(), 640, 400,
                        prepared.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(), prepared.Regions.Count));
                }
            }
        }
    }

    /// <summary>Creates four equal slices to exercise measured labels with symmetric horizontal anchors.</summary>
    // <radial-label-source>
    public static Chart CreateRadialLabels(ChartSeriesKind kind, ChartDataLabelPlacement placement) {
        var chart = Chart.Create().WithXLabels("A", "B", "C", "D").WithDataLabels()
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.Label).WithDonutCenterLabel(false);
        var points = Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 1));
        if (kind == ChartSeriesKind.Pie) chart.AddPie("Equal shares", points);
        else chart.AddDonut("Equal shares", points);
        chart.Options.DataLabelPlacement = placement;
        return chart;
    }
    // </radial-label-source>

    private static string RadialLabelSnippet(ChartSeriesKind kind, ChartDataLabelPlacement placement, VisualThemeMode mode) =>
        "using System.Linq;\nusing ChartForgeX.Core;\nusing ChartForgeX.Primitives;\nusing ChartForgeX.Rendering;\nusing ChartForgeX.Themes;\nusing ChartForgeX.Typography;\n\n" +
        "FontRegistry.Register(\"" + ProofFont + "\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"" + ProofFont + "\", \"fonts/Carlito-Bold.ttf\", 700);\n" +
        "var chart = CreateRadialLabels(ChartSeriesKind." + kind + ", ChartDataLabelPlacement." + placement + ");\n" +
        "var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 400)), VisualTheme.Graphite(), VisualThemeMode." + mode + ",\n" +
        "    new VisualFrame(\"Four equal shares\", \"Labels " + placement.ToString().ToLowerInvariant() + "\", showLegend: false), FontSpec.FromFamily(\"" + ProofFont + "\"));\n" +
        "var prepared = chart.Prepare(context);\nSystem.IO.File.WriteAllText(\"chart.svg\", prepared.ToSvg());\nSystem.IO.File.WriteAllBytes(\"chart.png\", prepared.ToPng());\n\n" +
        ReadFactorySource("ChartForgeX.Examples.V2RadialLabelSource", "radial-label");
}
