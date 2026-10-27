using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidCynefinNativeDepthTests {
    internal const string Body = "cynefin-beta\ntitle Service decisions\ncomplex\n\"Discover demand\"\ncomplicated\n\"Analyze capacity\"\nchaotic\n\"Contain outage\"\nclear\n\"Run checklist\"\nconfusion\n\"Investigate context\"\ncomplex --> complicated : \"Learn\"\n";

    [Fact]
    public void OrganicSceneKeepsAllDomainItemsAndTheNativeWidthFloor() {
        var result = MermaidRenderer.Render(Body, BrowserProofFonts.MermaidOptions());
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(5, chart.Nodes.Count); Assert.Single(chart.Edges); Assert.Equal(8, chart.PathDecorations.Count);
        Assert.Equal(960, chart.Viewport.Width);
        Assert.All(chart.Nodes, node => { Assert.False(node.ShowStatusBadge); Assert.True(node.PreserveDisplayModeSize); Assert.Single(node.Details); });
        Assert.Equal("Disorder", chart.Nodes.Single(node => node.Id == "confusion").Subtitle);
        var svg = result.Artifact.ToSvg();
        foreach (var id in new[] { "cynefin-fold", "cynefin-horizontal", "cynefin-cliff", "cynefin-confusion" }) Assert.Contains(id, svg);
        Assert.Contains("Discover demand", svg); Assert.Contains("Disorder", svg); Assert.DoesNotContain("<script", svg);
        Assert.NotEmpty(result.Artifact.ToPng());
        Assert.Equal(8, result.Artifact.ToInterchangeEnvelope().Topology!.PathDecorations.Count);
        var fenced = new MermaidVisualMarkupParser(BrowserProofFonts.MermaidOptions()).Parse("```mermaid\n" + Body + "```\n");
        Assert.False(fenced.HasErrors);
        Assert.Equal(svg, Assert.Single(fenced.Artifacts).ToSvg());
    }

    [Fact]
    public void AuthoredSeedAmplitudeAndPaintReachTheSameNativeOwnersWithoutChangingSourceSpans() {
        const string init = "%%{init: {'cynefin':{'seed':41,'boundaryAmplitude':0,'showDomainDescriptions':false},'themeVariables':{'cynefin':{'complexBg':'#112233','boundaryColor':'#445566','cliffColor':'#778899','arrowColor':'#aabbcc','arrowWidth':3,'textColor':'#123456','labelColor':'#654321'}}}}%%\n";
        var parsed = new MermaidParser().Parse(init + Body);
        Assert.Empty(parsed.Diagnostics);
        var source = Assert.IsType<MermaidCynefinDocument>(parsed.Document);
        var firstStatement = source.RawStatements[0].Span;
        var chart = source.ToTopologyChart();
        Assert.Equal(firstStatement, source.RawStatements[0].Span); Assert.Equal(init + Body, source.SourceText);
        Assert.All(chart.Nodes, node => Assert.Null(node.Subtitle));
        var fold = chart.PathDecorations.Single(path => path.Id == "cynefin-fold");
        Assert.Equal(ChartColor.FromHex("#445566"), fold.Stroke);
        Assert.Equal(ChartColor.FromHex("#112233").WithOpacity(.4), chart.PathDecorations[0].Fill);
        Assert.Equal(ChartColor.FromHex("#778899"), chart.PathDecorations.Single(path => path.Id == "cynefin-cliff").Stroke);
        Assert.Equal("#aabbccff", chart.Edges[0].Color, ignoreCase: true); Assert.Equal(3, chart.Edges[0].StrokeWidth);
        Assert.Equal("#123456ff", chart.Nodes[0].Details[0].Color, ignoreCase: true);
        var changed = MermaidRenderer.Render(init.Replace("'boundaryAmplitude':0", "'boundaryAmplitude':8") + Body);
        Assert.NotEqual(fold.PathData, Assert.IsType<TopologyChart>(changed.Artifact!.Model).PathDecorations.Single(path => path.Id == "cynefin-fold").PathData);
    }

    [Theory]
    [InlineData("boundaryAmplitude", "-1")]
    [InlineData("seed", "false")]
    [InlineData("showDomainDescriptions", "'yes'")]
    public void InvalidGeometryHasLocatedErrorsAndNoArtifact(string name, string value) {
        var result = MermaidRenderer.Render("---\nconfig:\n  cynefin:\n    " + name + ": " + value + "\n---\n" + Body);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, error => error.Code == MermaidDiagnosticCodes.InvalidConfiguration && error.Span.Line == 4);
    }

    [Fact]
    public void AuthoredDomainFontsUseNativeStylesAndDisorderDoesNotBecomeInventedSyntax() {
        var result = MermaidRenderer.Render("%%{init: {'themeVariables':{'cynefin':{'domainFontSize':20}}}}%%\n" + Body);
        Assert.Empty(result.Diagnostics);
        Assert.All(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes, node => Assert.Equal(20, node.TextStyle!.FontSize));
        Assert.True(new MermaidParser().Parse("cynefin-beta\ndisorder\n\"Item\"").HasErrors);
    }
}
