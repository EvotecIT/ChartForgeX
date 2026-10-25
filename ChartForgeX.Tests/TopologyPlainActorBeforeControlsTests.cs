using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Retained ordinary Actor output precedes any advanced actor geometry source changes.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyPlainActorBeforeControlsTests {
    [Theory]
    [InlineData(390, false)] [InlineData(900, false)] [InlineData(390, true)] [InlineData(900, true)]
    public void OrdinaryActorKeepsItsNativeFigureCaptionRouteIdentityAndStaticExports(int width, bool dark) {
        var chart = TopologyChart.Create().WithId("plain-actor-before").WithTitle("An ordinary external actor")
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithViewport(width, 420, 24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("operator", "External operator", 50, 50, width: 180, height: 120)
            .AddNode("submit", "Submit request", 320, 65, width: 190, height: 90)
            .AddEdge("request", "operator", "submit", "requests");
        chart.Nodes[0].Kind = TopologyNodeKind.Generic; chart.Nodes[0].Shape = TopologyNodeShape.Actor;
        chart.Nodes[1].Kind = TopologyNodeKind.Generic; chart.Nodes[1].Shape = TopologyNodeShape.Ellipse;
        foreach (var node in chart.Nodes) { node.ShowStatusBadge = false; node.PreserveDisplayModeSize = true; }
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true, WrapNodeLabels = true });
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(2, prepared.NodeCount); Assert.Equal(1, prepared.EdgeCount);
        Assert.Equal(new[] { "operator", "submit" }, envelope.Nodes.Select(node => node.Id));
        Assert.Equal("request", Assert.Single(envelope.Edges).Id); Assert.Equal(TopologyNodeShape.Actor, envelope.Nodes[0].Topology!.Shape);
        var text = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(item => item.Text.Lines.Select(line => line.Text));
        Assert.Contains("External operator", string.Join(" ", text)); Assert.Contains("Submit request", svg);
        Assert.DoesNotContain("<script", svg); Assert.NotEmpty(png);
        var name = "plain-actor-before-" + width + (dark ? "-dark" : "-light");
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
            File.WriteAllText(Path.Combine(output, name + ".json"), envelope.ToJson());
        }
        // Preparing this retained owner again cannot alter the first prepared result.
        chart.Nodes[0].Label = "Edited operator";
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }
}
