using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
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
            frame: new VisualFrame(showLegend: false), font: CarlitoFont());
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

    [Fact]
    public void BroadResolvedFaceRetainsReadableDotPrefixAndActualBadgePadding() {
        const string family = "CFX Topology Broad Compact";
        try {
            // Two 800-unit advances exceed the 11px dot at the preferred 8px size.
            // This stable fixture reproduces Arial/Georgia and macOS fallback metrics.
            FontRegistry.Register(family, OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> {
                ['D'] = OpenTypeTestFonts.Flex, ['C'] = OpenTypeTestFonts.Flex
            }, weight: 700), weight: 700);
            var font = new FontSpec { Family = family };
            var chart = TopologyChart.Create().WithViewport(240, 160, 20).WithLegend(null)
                .AddNode("dc", "Directory server", 90, 70, TopologyNodeKind.Server, TopologyHealthStatus.Healthy,
                    width: 11, height: 11, symbol: "DC").WithNodeDisplay("dc", TopologyNodeDisplayMode.Dot);
            chart.Nodes[0].Badge = "DC";
            var preferred = new VisualSceneTextFace(font, 700).Prepare("DC", 8);
            Assert.True(preferred.Metrics.Width > 11);
            var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(240, 160), 20),
                frame: new VisualFrame(showLegend: false), font: font);
            var options = new TopologyRenderOptions { IncludeLegend = false };
            var prepared = chart.Prepare(context, options);
            var symbol = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-node-symbol");
            Assert.Equal("D", Assert.Single(symbol.Text.Lines).Text);
            Assert.Equal(8, symbol.Text.Size);
            Assert.True(symbol.Text.Metrics.Width <= 11 && symbol.Text.Metrics.Height <= 11);
            Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "topology.label-truncated");
            var badge = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "topology-node-badge-surface");
            var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "topology-node-badge");
            Assert.True(badge.Bounds.Width >= caption.Text.Metrics.Width + 12);
            Assert.Equal("DC", Assert.Single(prepared.SemanticInterchange!.Nodes).Symbol);
            Assert.Equal(TextMeasurementMode.PortableEstimate, options.TextMeasurementMode);
            var png = prepared.ToPng(); var svg = prepared.ToSvg();
            chart.Nodes[0].Symbol = null; chart.Nodes[0].Badge = null; font.Family = "sans-serif";
            Assert.False(png.SequenceEqual(chart.Prepare(context, options).ToPng()));
            Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        } finally {
            FontRegistry.Clear();
        }
    }

    private static FontSpec CarlitoFont() {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert.True(File.Exists(path), "The existing Carlito fixture must be available.");
        return new FontSpec { FilePath = path, Family = "Carlito" };
    }
}
