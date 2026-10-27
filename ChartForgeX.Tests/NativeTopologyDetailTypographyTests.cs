using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyDetailTypographyTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DetailOverridesUseNativeMeasuredTextAndDetachedClonesForOrdinaryAndFittedShapes(bool fitted) {
        var chart = TopologyChart.Create().WithTheme(ChartForgeX.Themes.VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithViewport(480, 280, 16).WithLegend(null)
            .AddNode("item", "Item", 40, 40, width: 320, height: 170);
        var node = chart.Nodes[0];
        node.Shape = TopologyNodeShape.Rectangle;
        node.FitLabelToContent = fitted;
        node.ShowStatusBadge = false;
        var style = new TextStyleOverride().WithItalic().WithUnderline().WithWeight("700");
        node.Details.Add(new TopologyNodeDetail { Text = "Qualified member", TextStyle = style });
        var options = new TopologyRenderOptions { IncludeLegend = false };
        var prepared = chart.Prepare(options);
        var text = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneText>(), item => item.Role == "topology-node-detail");
        Assert.True(text.Text.Style.Font.Italic);
        Assert.Equal(700, text.Text.Style.Font.Weight);
        Assert.Equal(TextDecorationStyle.Single, text.Text.Style.UnderlineStyle);
        Assert.Equal("Qualified member", Assert.Single(text.Text.Lines).Text);
        Assert.Single(prepared.Visual.Scene.Nodes, item => item.Role == "text-decoration");
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        style.Italic = false; style.Underline = false; style.FontWeight = "400";
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
        var saved = Assert.Single(Assert.Single(prepared.ToInterchangeEnvelope().Nodes).Details).TextStyle;
        Assert.True(saved!.Italic);
        Assert.True(saved.Underline);
        Assert.Equal("700", saved.FontWeight);
        saved.Italic = false;
        Assert.True(Assert.Single(Assert.Single(prepared.ToInterchangeEnvelope().Nodes).Details).TextStyle!.Italic);
        var resized = prepared.WithOutputSize(prepared.Width + 20, prepared.Height + 20);
        Assert.True(Assert.Single(Assert.Single(resized.ToInterchangeEnvelope().Nodes).Details).TextStyle!.Italic);
    }

    [Fact]
    public void AnEmptyDetailOverridePreservesTheDefaultSvgAndPngExactly() {
        var chart = TopologyChart.Create().WithTheme(ChartForgeX.Themes.VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithViewport(440, 240, 16).WithLegend(null)
            .AddNode("item", "Item", 40, 40, width: 280, height: 140)
            .AddNodeDetail("item", "Status", "Ready");
        var originalSvg = chart.ToSvg(); var originalPng = chart.ToPng();
        chart.Nodes[0].Details[0].TextStyle = new TextStyleOverride();
        Assert.Equal(originalSvg, chart.ToSvg());
        Assert.Equal(originalPng, chart.ToPng());
    }
}
