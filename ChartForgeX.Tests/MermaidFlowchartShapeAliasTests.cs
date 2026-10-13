using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartShapeAliasTests {
    [Theory]
    [InlineData("fr-rect", TopologyNodeShape.Subroutine)]
    [InlineData("framed-rectangle", TopologyNodeShape.Subroutine)]
    [InlineData("question", TopologyNodeShape.Diamond)]
    [InlineData("lean-right", TopologyNodeShape.Parallelogram)]
    [InlineData("lean-left", TopologyNodeShape.ParallelogramAlt)]
    [InlineData("manual", TopologyNodeShape.TrapezoidAlt)]
    [InlineData("trapezoid-top", TopologyNodeShape.TrapezoidAlt)]
    [InlineData("priority", TopologyNodeShape.Trapezoid)]
    [InlineData("trapezoid-bottom", TopologyNodeShape.Trapezoid)]
    [InlineData("odd", TopologyNodeShape.Asymmetric)]
    public void DocumentedAliasesUseTheExistingNativeShape(string alias, TopologyNodeShape shape) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA@{ shape: " + alias + ", label: \"Caption\" }");
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(alias, Assert.Single(document.Nodes).Properties["shape"]);
        var node = Assert.Single(document.ToTopologyChart().Nodes);
        Assert.Equal(shape, node.Shape);
        Assert.Equal("Caption", node.Label);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VersionedAliasFixtureRetainsShapesInNativeArtifacts(bool dark) {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "flowchart-native-shape-aliases.mmd");
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(path, ".expected.json")));
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + File.ReadAllText(path);
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()), chart.Nodes.Select(node => node.Id));
        Assert.Equal(expected.RootElement.GetProperty("nativeShapes").EnumerateArray().Select(shape => shape.GetString()), chart.Nodes.Select(node => node.Shape.ToString()));
        var svg = result.Artifact.ToSvg(); var png = result.Artifact.ToPng();
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.Combine(captures, dark ? "shape-aliases-dark" : "shape-aliases-light");
        File.WriteAllText(name + ".svg", svg); File.WriteAllBytes(name + ".native.png", png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = name + ".browser.png", FullPage = true });
    }
}
