using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FlowchartCurveRecipeTests {
    [Theory]
    [InlineData("flowchart", "compact", false)] [InlineData("flowchart", "compact", true)]
    [InlineData("flowchart", "wide", false)] [InlineData("flowchart", "wide", true)]
    [InlineData("swimlane", "compact", false)] [InlineData("swimlane", "compact", true)]
    [InlineData("swimlane", "wide", false)] [InlineData("swimlane", "wide", true)]
    public void EmittedProgramAndGalleryUseOneCompiledNativeRecipe(string family, string variant, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var artifact = FlowchartCurveRecipe.Create(family, variant, mode, BrowserProofFonts.Family);
        var program = ExampleRecipeSource.EmitFlowchartCurves(family, variant, mode);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var json = artifact.ToInterchangeJson();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "curve-program-" + family + "-" + variant + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".Program.cs", program); File.WriteAllText(path + ".expected.svg", svg); File.WriteAllBytes(path + ".expected.png", png);
            File.WriteAllText(path + ".expected.html", html); File.WriteAllText(path + ".expected.json", json); File.WriteAllText(path + ".mmd", FlowchartCurveRecipe.Source(family));
        }
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal(6, chart.Nodes.Count); Assert.Equal(2, chart.Groups.Count); Assert.Equal(6, chart.Edges.Count);
        Assert.Equal(TopologyEdgeInterpolation.BumpX, chart.Edges[0].Interpolation);
        Assert.All(chart.Edges.Skip(1), edge => Assert.Equal(family == "swimlane" ? TopologyEdgeInterpolation.Rounded : TopologyEdgeInterpolation.Basis, edge.Interpolation));
        Assert.Equal(16, chart.Theme.Typography.DataLabelSize); Assert.Equal(2, artifact.ToInterchangeEnvelope().Version);
        Assert.Contains("var artifact=FlowchartCurveRecipe.Create", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ChartForgeX.Examples", program, StringComparison.Ordinal); Assert.DoesNotContain("node_modules", program, StringComparison.Ordinal);
        Assert.Contains("ToInterchangeJson", program, StringComparison.Ordinal); Assert.NotEmpty(png);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("foreignObject", svg, StringComparison.Ordinal);
    }
}
