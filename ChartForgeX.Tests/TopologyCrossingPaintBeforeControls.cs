using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyCrossingPaintBeforeControls {
    [Theory]
    [InlineData("X", false)] [InlineData("X", true)] [InlineData("T", false)] [InlineData("T", true)]
    [InlineData("shared", false)] [InlineData("shared", true)] [InlineData("collinear", false)] [InlineData("collinear", true)]
    [InlineData("diagonal", false)] [InlineData("diagonal", true)] [InlineData("near-bend", false)] [InlineData("near-bend", true)]
    [InlineData("crowded", false)] [InlineData("crowded", true)] [InlineData("cubic", false)] [InlineData("cubic", true)]
    public void OrdinaryNativeRoutesRemainDeterministicBeforeAnyCrossingPolicy(string form, bool fit) {
        var chart = CrossingPaintControlFixture.Create(form); var options = CrossingPaintControlFixture.Options(fit);
        var prepared = fit ? chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 240), 12)), options) : chart.Prepare(options).Visual;
        CrossingPaintControlFixture.Capture(prepared, "crossing-before-native-" + form + "-" + fit);
        Assert.NotEmpty(prepared.ToPng()); var again = fit ? chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 240), 12)), options) : chart.Prepare(options).Visual;
        Assert.Equal(prepared.ToSvg(), again.ToSvg()); Assert.Equal(prepared.ToPng(), again.ToPng());
        Assert.Equal(prepared.ToArtifact().ToInterchangeJson(), again.ToArtifact().ToInterchangeJson());
    }
    [Theory] [InlineData("null")] [InlineData("true")] [InlineData("\"gap\"")]
    public void ExistingStrictSourceConsumerAcceptsSupportedFiniteLineHopConfiguration(string value) {
        var source = CrossingPaintControlFixture.Swimlane(value); var result = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "crossing-source-strict-causal-" + value.Replace("\"", ""));
            File.WriteAllText(path + ".source.mmd", source); File.WriteAllText(path + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics));
            if (result.Artifact != null) { File.WriteAllText(path + ".svg", result.Artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", result.Artifact.ToPng()); File.WriteAllText(path + ".portable.json", result.Artifact.ToInterchangeJson());
                File.WriteAllText(path + ".facts.json", JsonSerializer.Serialize(new { result.Artifact.Id, source })); }
        }
        Assert.NotNull(result.Artifact); Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("absent")] [InlineData("null")] [InlineData("false")]
    [InlineData("true")] [InlineData("\"arc\"")] [InlineData("\"gap\"")]
    public void SourceDefaultMigrationRetainsBeforeInkAndLocatedConfigurationFacts(string value) {
        var source = CrossingPaintControlFixture.Swimlane(value); var parsed = new MermaidParser().ParseFlowchart(source);
        var rendered = MermaidRenderer.Render(source); var artifact = rendered.Artifact;
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var name = "crossing-before-source-" + value.Replace("\"", ""); var path = Path.Combine(output, name);
            File.WriteAllText(path + ".source.mmd", source); File.WriteAllText(path + ".diagnostics.json", JsonSerializer.Serialize(rendered.Diagnostics));
            File.WriteAllText(path + ".settings.json", JsonSerializer.Serialize(parsed.Document?.Configuration.Settings));
            if (artifact != null) { File.WriteAllText(path + ".svg", artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", artifact.ToPng()); File.WriteAllText(path + ".portable.json", artifact.ToInterchangeJson()); }
        }
        Assert.NotNull(parsed.Document); Assert.NotNull(artifact); Assert.Equal(source, parsed.Document.SourceText);
        if (value != "absent") Assert.Contains(parsed.Document.Configuration.Settings, setting => setting.Path == "swimlane.lineHops");
    }
}

internal static class CrossingPaintControlFixture {
    internal static TopologyChart Create(string form = "X") {
        var chart = TopologyChart.Create().WithId("crossing-control").WithViewport(400, 320, 24).WithLayout(TopologyLayoutMode.Manual).WithLegend(null);
        chart.AddNode("left", "Left", 20, 100, width: 40, height: 40).AddNode("right", "Right", 300, 100, width: 40, height: 40)
            .AddNode("top", "Top", 150, 20, width: 40, height: 40).AddNode("bottom", "Bottom", 150, 240, width: 40, height: 40);
        foreach (var node in chart.Nodes) { node.Kind = TopologyNodeKind.Generic; node.ShowStatusBadge = false; }
        chart.AddEdge("horizontal", "left", "right", direction: VisualLinkDirection.None).AddEdge("vertical", "top", "bottom", direction: VisualLinkDirection.None);
        chart.Edges[0].PathGeometry = new TopologyPathGeometry("M 60 120 L 300 120");
        chart.Edges[1].PathGeometry = new TopologyPathGeometry(form switch {
            "T" => "M 170 120 L 170 240", "shared" => "M 300 120 L 300 240",
            "collinear" => "M 100 120 L 250 120", "diagonal" => "M 100 60 L 240 200",
            "near-bend" => "M 170 114 L 170 200 L 250 200", "crowded" => "M 170 60 L 170 240",
            "cubic" => "M 170 60 C 260 80 80 200 170 240", _ => "M 170 60 L 170 240"
        });
        if (form == "crowded") { chart.AddEdge("neighbor", "left", "right", direction: VisualLinkDirection.None); chart.Edges[2].PathGeometry = new TopologyPathGeometry("M 60 125 L 300 125"); }
        return chart;
    }
    internal static TopologyRenderOptions Options(bool fit = false) => new() { IncludeLegend = false, IncludeNodeLabels = false,
        IncludeEdgeLabels = false, EdgeCornerStyle = TopologyEdgeCornerStyle.Sharp, FitContentToViewport = fit };
    internal static string Swimlane(string setting = "absent", string? curve = null) {
        var config = setting == "absent" ? "" : "\"swimlane\":{\"lineHops\":" + setting + "}";
        if (curve != null) config += (config.Length == 0 ? "" : ",") + "\"flowchart\":{\"curve\":\"" + curve + "\"}";
        return (config.Length == 0 ? "" : "%%{init: {" + config + "}}%%\n") + "swimlane-beta LR\n subgraph A[Inbound]\n a[Accept]\n b[Build]\n end\n subgraph B[Outbound]\n c[Check]\n d[Deliver]\n end\n a --> d\n b --> c\n";
    }
    internal static void Capture(PreparedVisual prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".svg", prepared.ToSvg());
        File.WriteAllBytes(path + ".native.png", prepared.ToPng()); File.WriteAllText(path + ".portable.json", prepared.ToArtifact().ToInterchangeJson());
    }
}
