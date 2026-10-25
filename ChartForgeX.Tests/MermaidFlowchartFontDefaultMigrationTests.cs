using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidFlowchartFontDefaultMigrationTests {
    [Theory]
    [InlineData("flowchart LR\nA[Native caption]")]
    [InlineData("swimlane-beta LR\nsubgraph Lane[Lane]\nA[Native caption]\nend")]
    public void OnlyQualifiedFamiliesSelectPinnedSixteenAcrossDirectFenceAndPortable(string source) {
        var options = Options(); var result = MermaidRenderer.Render(source, options); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var before = result.Artifact.ToSvg(); var png = result.Artifact.ToPng(); var json = result.Artifact.ToInterchangeJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var prepared = restored.Prepare();
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); var artifact = Assert.Single(fenced.Artifacts);
        var name = source.StartsWith("flowchart", StringComparison.Ordinal) ? "flowchart" : "swimlane";
        Capture(name + "-direct", before, png, json); Capture(name + "-restored", prepared.ToSvg(), prepared.ToPng(), prepared.ToInterchangeEnvelope().ToJson());
        Capture(name + "-fenced", artifact.ToSvg(), artifact.ToPng(), artifact.ToInterchangeJson());
        Assert.Empty(result.Diagnostics); Assert.Empty(fenced.Diagnostics); Assert.Null(result.Document!.Configuration.FontSize);
        Assert.Equal(16, chart.Theme.Typography.DataLabelSize); Assert.Equal(16, restored.Theme.Typography.DataLabelSize);
        Assert.Equal(before, artifact.ToSvg()); Assert.Equal(png, artifact.ToPng()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(null, 14d, 14d)] [InlineData(19d, 14d, 19d)] [InlineData(19d, null, 19d)]
    public void ExistingLiteralAndHostSizePrecedenceStaysAuthoritative(double? hostSize, double? sourceSize, double expected) {
        var source = (sourceSize.HasValue ? "---\nconfig:\n  themeVariables:\n    fontSize: " + sourceSize + "\n---\n" : "") + "flowchart LR\nA[Caption]";
        var options = Options(); options.Presentation = new MermaidPresentationOptions { FontSize = hostSize, FontFamily = BrowserProofFonts.Family };
        var result = MermaidRenderer.Render(source, options); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Capture("precedence-" + hostSize + "-" + sourceSize, result.Artifact.ToSvg(), result.Artifact.ToPng(), result.Artifact.ToInterchangeJson());
        Assert.Empty(result.Diagnostics); Assert.Equal(expected, chart.Theme.Typography.DataLabelSize);
        Assert.Equal(sourceSize, result.Document!.Configuration.FontSize); Assert.Equal(hostSize, options.Presentation.FontSize);
    }

    [Fact]
    public void ExplicitNativeHostThemeRemainsTheFallbackBeforeImplicitSourceDefault() {
        var options = Options(); options.Presentation = new MermaidPresentationOptions {
            Theme = VisualTheme.Graphite().WithTypography(new VisualTypography(family: BrowserProofFonts.Family, dataLabelSize: 15)) };
        var result = MermaidRenderer.Render("flowchart LR\nA[Caption]", options); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Capture("host-theme", result.Artifact.ToSvg(), result.Artifact.ToPng(), result.Artifact.ToInterchangeJson());
        Assert.Empty(result.Diagnostics); Assert.Equal(15, chart.Theme.Typography.DataLabelSize); Assert.Null(options.Presentation.FontSize);
    }

    [Fact]
    public void UseCaseDoesNotInheritAnUnverifiedFlowchartDefault() {
        var result = MermaidRenderer.Render("usecase-beta\nactor Visitor", new MermaidRenderOptions {
            UseCase = new MermaidFlowchartRenderOptions { FitContent = false, Title = "", Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family } } });
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Capture("usecase-control", result.Artifact.ToSvg(), result.Artifact.ToPng(), result.Artifact.ToInterchangeJson());
        Assert.Empty(result.Diagnostics); Assert.Equal(VisualTheme.Graphite().Typography.DataLabelSize, chart.Theme.Typography.DataLabelSize);
    }

    [Fact]
    public void NativeGraphiteAndOrdinaryManualVersionOneStayExact() {
        var chart = TopologyChart.Create().WithId("native-font-null-control").WithViewport(640, 400, 24).WithLayout(TopologyLayoutMode.Manual)
            .WithTheme(VisualTheme.Graphite()).AddNode("node", "Caption", 120, 100, width: 240, height: 100);
        chart.Nodes[0].Shape = TopologyNodeShape.Rectangle; chart.Nodes[0].ShowStatusBadge = false;
        chart.DefaultRenderOptions = new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = false };
        var prepared = chart.Prepare(); var saved = prepared.ToInterchangeEnvelope(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = saved.ToJson();
        Capture("native-null-control", svg, png, json);
        Assert.Equal(12, chart.Theme.Typography.DataLabelSize); Assert.Equal(1, saved.Version); Assert.DoesNotContain("\"dataLabelSize\"", json);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Equal(json, prepared.ToInterchangeEnvelope().ToJson());
    }
    private static MermaidRenderOptions Options() => new() { Flowchart = new MermaidFlowchartRenderOptions { Width = 640, Height = 400, FitContent = false, Title = "" },
        Swimlane = new MermaidFlowchartRenderOptions { Width = 640, Height = 400, FitContent = false, Title = "" },
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.Natural } };
    private static void Capture(string name, string svg, byte[] png, string json) {
        var output = Environment.GetEnvironmentVariable("CFX_SOURCE_FONT_CAPTURE_DIRECTORY") ?? Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, "source-font-" + name);
        File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png); File.WriteAllText(path + ".json", json);
    }
}
