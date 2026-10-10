using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidNotationArtifactTests {
    [Theory]
    [InlineData("class-retained-interactions", "classes")]
    [InlineData("er-retained-styles", "entities")]
    [InlineData("state-retained-note", "states")]
    [InlineData("er-retained-subgraph", "entities")]
    [InlineData("state-floating-note", "states")]
    [InlineData("er-lowercase-direction", "entities")]
    public void SharedReferenceFixturesPreserveNativeFactsAndRetainedNotation(string name, string key) {
        var root = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures");
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, name + ".expected.json")));
        var rendered = MermaidRenderer.Render(File.ReadAllText(Path.Combine(root, name + ".mmd")));
        Assert.False(rendered.HasErrors);
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(expected.RootElement.GetProperty(key).EnumerateArray().Select(item => item.GetString()), chart.Nodes.Select(node => node.Id));
        Assert.Equal(expected.RootElement.GetProperty("nativeRetainedStatements").EnumerateArray().Select(item => item.GetString()),
            rendered.Document!.RawStatements.Select(statement => statement.Text));
        Assert.All(rendered.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, item.Code));
        var direction = rendered.Document switch {
            MermaidClassDocument item => item.Direction, MermaidStateDocument item => item.Direction,
            MermaidEntityRelationshipDocument item => item.Direction, _ => null
        };
        Assert.Equal(expected.RootElement.GetProperty("direction").GetString(), direction);
        var semantics = VisualArtifactInterchangeEnvelope.FromJson(rendered.Artifact.ToInterchangeJson());
        Assert.Equal(chart.Nodes.Select(node => node.Id), semantics.Nodes.Select(node => node.Id));
        Assert.Equal(chart.Edges.Count, semantics.Edges.Count);
    }

    [Theory]
    [InlineData("classDiagram\ndirection RL\nA --> B", "class-rl", 960, "RL")]
    [InlineData("erDiagram\ndirection TB\nA ||--o{ B : contains", "er-tb", 960, "TB")]
    [InlineData("stateDiagram-v2\ndirection BT\nA --> B", "state-bt", 960, "BT")]
    [InlineData("stateDiagram-v2\ndirection RL\nA --> B", "state-rl", 320, "RL")]
    public async Task AuthoredDirectionsProduceObservedNativeGeometry(string source, string name, int width, string direction) {
        if (!InteractiveChartBrowser.Enabled) return;
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 620);
        var first = await session.Page.Locator("[data-cfx-role=\"topology-node\"][data-node-id=\"A\"]").BoundingBoxAsync();
        var second = await session.Page.Locator("[data-cfx-role=\"topology-node\"][data-node-id=\"B\"]").BoundingBoxAsync();
        Assert.NotNull(first);
        Assert.NotNull(second);
        var ax = first!.X + first.Width / 2;
        var ay = first.Y + first.Height / 2;
        var bx = second!.X + second.Width / 2;
        var by = second.Y + second.Height / 2;
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "notation-" + name + "-" + width + ".png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, "notation-" + name + "-native.png"), artifact.ToPng());
        }
        Assert.True(direction switch { "RL" => ax > bx, "BT" => ay > by, "TB" => ay < by, _ => ax < bx }, "The rendered node ordering must follow " + direction + ".");
    }
}
