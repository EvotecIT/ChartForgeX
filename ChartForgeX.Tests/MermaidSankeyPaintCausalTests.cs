using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Exercises only pre-existing public entry points so the missing source-paint policies can be observed before their implementation.</summary>
public sealed class MermaidSankeyPaintCausalTests {
    [Theory]
    [InlineData("source", "#2864B4")]
    [InlineData("target", "#BB6428")]
    [InlineData("#7954AA", "#7954AA")]
    public void SourceLinkPaintReachesTheExistingNativeRibbon(string policy, string expected) {
        var parsed = Parse("\"linkColor\":\"" + policy + "\",\"nodeColors\":{\"A\":\"#2864B4\",\"B\":\"#BB6428\"}");
        var prepared = SankeyFlowColorTests.Prepare(parsed.Document!.ToChart()); Capture("causal-link-" + (policy.StartsWith("#") ? "literal" : policy), prepared);
        var ribbon = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "sankey-ribbon");
        Assert.Equal(ChartColorMath.WithOpacity(ChartColor.FromHex(expected), .35), ribbon.Fill);
        Assert.Empty(parsed.Diagnostics);
    }

    [Fact]
    public void SourceGradientIsAnActualTwoEndpointNativePaintRatherThanAQuantitativeScale() {
        var parsed = Parse("\"linkColor\":\"gradient\",\"nodeColors\":{\"A\":\"#2864B4\",\"B\":\"#BB6428\"}");
        var prepared = SankeyFlowColorTests.Prepare(parsed.Document!.ToChart()); Capture("causal-gradient", prepared);
        var gradient = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>(), node => node.Role == "sankey-ribbon");
        Assert.Equal(new[] { ChartColor.FromHex("#2864B4"), ChartColor.FromHex("#BB6428") }.Select(color => ChartColorMath.WithOpacity(color, .35)), gradient.Stops.Select(stop => stop.Color));
        Assert.Empty(parsed.Diagnostics);
    }

    [Fact]
    public void ExactDottedBracketAndUnicodeNodeColorKeysReachOnlyTheirOwnNativeNodes() {
        var ids = new[] { "a.b", "a[b]", "A].[B", "A.].[B.", "Žółć 東京" };
        var colors = new[] { "#2864B4", "#BB6428", "#7954AA", "#279473", "#2864B4" };
        var settings = "\"nodeColors\":" + JsonSerializer.Serialize(ids.Zip(colors).ToDictionary(pair => pair.First, pair => pair.Second));
        var body = "sankey-beta\n" + string.Join("\n", ids.Select((id, index) => id + ",Sink," + (index + 1)));
        var parsed = new MermaidParser().ParseSankey("%%{init: {\"sankey\":{" + settings + "}}}%%\n" + body);
        var chart = parsed.Document!.ToChart(); Capture("causal-node-identities", SankeyFlowColorTests.Prepare(chart));
        foreach (var (id, expected) in ids.Zip(colors)) Assert.Equal(ChartColor.FromHex(expected), chart.Series[0].PointColors.ElementAtOrDefault(chart.Series[0].Nodes.ToList().FindIndex(node => node.Id == id)));
        Assert.Empty(parsed.Diagnostics);
    }

    [Theory]
    [InlineData("legacy", false)] [InlineData("outlined", true)]
    public void ExplicitSourceLabelPaintDoesNotKeepTheIncumbentRectangularBackdrop(string style, bool outlined) {
        var parsed = Parse("\"labelStyle\":\"" + style + "\"");
        var prepared = SankeyFlowColorTests.Prepare(parsed.Document!.ToChart()); Capture("causal-label-" + style, prepared);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "sankey-label-backdrop");
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "sankey-node-label"), node => Assert.Equal(outlined ? 4 : 0, node.StrokeWidth));
        Assert.Empty(parsed.Diagnostics);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void UnconfiguredSankeyRetainsItsNativePaintAndLabelDefaults(bool dark) {
        var parsed = new MermaidParser().ParseSankey("sankey-beta\nA,B,6"); Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToChart().WithTheme(ChartForgeX.Themes.VisualTheme.Graphite(), dark ? ChartForgeX.Themes.VisualThemeMode.Dark : ChartForgeX.Themes.VisualThemeMode.Light);
        var prepared = SankeyFlowColorTests.Prepare(chart); Capture("causal-default-" + dark, prepared);
        Assert.Equal(ChartSankeyFlowColorMode.Source, chart.Options.Sankey.FlowColorMode);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "sankey-ribbon");
        // Captions read over flows through a surface halo, not a box (design rule 8).
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "sankey-label-backdrop");
        Assert.Equal(ChartSankeyLabelStyle.Outlined, chart.Options.Sankey.LabelStyle);
    }

    private static MermaidParseResult<MermaidSankeyDocument> Parse(string settings) => new MermaidParser().ParseSankey("%%{init: {\"sankey\":{" + settings + "}}}%%\nsankey-beta\nA,B,6");
    private static void Capture(string name, PreparedVisual prepared) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg()); File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng());
    }
}
