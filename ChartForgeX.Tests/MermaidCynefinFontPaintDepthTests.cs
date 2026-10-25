using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidCynefinFontPaintDepthTests {
    private const string Init = "%%{init: {'themeVariables':{'cynefin':{'domainFontSize':22,'itemFontSize':15,'labelColor':'#653412','textColor':'#123456'}}}}%%\n";
    [Fact]
    public void AuthoredFamilyFontsAndIndependentSubtitlePaintReachNativeMeasuredRolesWithoutConfigurationWarnings() {
        var parsed = new MermaidParser().Parse(Init + MermaidCynefinNativeDepthTests.Body); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidCynefinDocument>(parsed.Document); var chart = document.ToTopologyChart();
        Assert.Equal(24, chart.Theme.Typography.TitleSize);
        Assert.All(chart.Nodes, node => {
            Assert.Equal(22, node.TextStyle!.FontSize); Assert.Equal(14, node.SubtitleTextStyle!.FontSize);
            Assert.Equal(ChartColor.FromHex("#123456"), node.SubtitleTextStyle!.Color);
            Assert.Equal(15, node.Details[0].TextStyle!.FontSize); Assert.Equal("#653412ff", node.LabelColor, ignoreCase: true);
        });
        Assert.Equal(14, chart.Edges[0].TextStyle!.FontSize);
        var saved = chart.Prepare().ToInterchangeEnvelope(); var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        Assert.Equal(24, restored.Theme.Typography.TitleSize);
        Assert.All(restored.Nodes, node => { Assert.Equal(22, node.TextStyle!.FontSize); Assert.Equal(15, node.Details[0].TextStyle!.FontSize); Assert.Equal(14, node.SubtitleTextStyle!.FontSize); });
        Assert.Contains("#123456", restored.ToSvg().ToLowerInvariant()); Assert.NotEmpty(restored.ToPng());
        Assert.Equal(Init + MermaidCynefinNativeDepthTests.Body, document.SourceText);
    }
    [Fact]
    public void ExplicitHostFontSizeKeepsPrecedenceAndUnauthoredFamilySizesRetainNativeDefaults() {
        var document = Assert.IsType<MermaidCynefinDocument>(new MermaidParser().Parse(Init + MermaidCynefinNativeDepthTests.Body).Document);
        var host = document.ToTopologyChart(new MermaidTopologyRenderOptions { Presentation = new MermaidPresentationOptions { FontSize = 19 } });
        Assert.All(host.Nodes, node => {
            Assert.Null(node.TextStyle); Assert.Null(node.Details[0].TextStyle);
            var subtitle = Assert.IsType<ChartForgeX.Typography.TextStyleOverride>(node.SubtitleTextStyle);
            Assert.Null(subtitle.FontSize); Assert.True(subtitle.Italic); Assert.Equal(ChartColor.FromHex("#123456"), subtitle.Color);
        });
        Assert.Null(host.Edges[0].TextStyle!.FontSize); Assert.Equal("400", host.Edges[0].TextStyle!.FontWeight);
        var ordinary = Assert.IsType<MermaidCynefinDocument>(new MermaidParser().Parse(MermaidCynefinNativeDepthTests.Body).Document).ToTopologyChart();
        Assert.All(ordinary.Nodes, node => {
            Assert.Null(node.TextStyle); Assert.Null(node.Details[0].TextStyle);
            var subtitle = Assert.IsType<ChartForgeX.Typography.TextStyleOverride>(node.SubtitleTextStyle);
            Assert.Null(subtitle.FontSize); Assert.True(subtitle.Italic); Assert.Null(subtitle.Color);
        });
        Assert.Null(ordinary.Edges[0].TextStyle!.FontSize); Assert.Equal("400", ordinary.Edges[0].TextStyle!.FontWeight);
    }
    [Theory]
    [InlineData("domainFontSize", "0")] [InlineData("domainFontSize", "false")]
    [InlineData("itemFontSize", "1")] [InlineData("itemFontSize", "257")]
    public void InvalidAuthoredRoleSizesHaveLocatedErrors(string name, string value) {
        var result = MermaidRenderer.Render("---\nconfig:\n  themeVariables:\n    cynefin:\n      " + name + ": " + value + "\n---\n" + MermaidCynefinNativeDepthTests.Body);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, error => error.Code == MermaidDiagnosticCodes.InvalidConfiguration && error.Span.Line == 5);
    }
}
