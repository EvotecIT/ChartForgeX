using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedTopologyDotTypographyTests {
    [Theory]
    [InlineData(11, 11, 8)]
    [InlineData(22, 22, 16)]
    [InlineData(22, 11, 8)]
    [InlineData(11, 6, 48d / 11)]
    public void DotSymbolsRetainReadableDefaultAndScaleWithinTheirOwnBudget(double labelSize, double diameter, double expectedSize) {
        var chart = TopologyChart.Create().WithViewport(240, 160, 20).WithLegend(null)
            .AddNode("dc", "Directory server", 90, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy,
                width: diameter, height: diameter, symbol: "DC")
            .WithNodeDisplay("dc", TopologyNodeDisplayMode.Dot);
        var theme = new VisualTheme(new VisualDesignTokens(), new VisualDesignTokens(),
            typography: new VisualTypography(dataLabelSize: labelSize));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(240, 160), 20), theme,
            frame: new VisualFrame(showLegend: false));
        var options = new TopologyRenderOptions { IncludeLegend = false, NodeDisplayMode = TopologyNodeDisplayMode.Dot }
            .WithMonitoringDashboardStyle();
        var prepared = chart.Prepare(context, options);
        var symbol = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-node-symbol");
        Assert.Equal(expectedSize, symbol.Text.Size, 8);
        Assert.Equal("DC", Assert.Single(symbol.Text.Lines).Text);
        Assert.True(symbol.Text.Metrics.Width <= diameter);
        Assert.True(symbol.Text.Metrics.Height <= diameter);
        Assert.Equal(diameter, chart.Nodes[0].Width);
        Assert.Equal(diameter, chart.Nodes[0].Height);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Nodes[0].Symbol = null;
        Assert.False(png.SequenceEqual(chart.Prepare(context, options).ToPng()), "The retained symbol must contribute native raster ink.");
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }
}
