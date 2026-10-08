using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyCaptionFootprintTests {
    [Fact]
    public void DetailedCardWithoutRoomForACompleteDetailLineDoesNotPaintAnOrphanDivider() {
        var chart = TopologyChart.Create().WithViewport(360, 260, 20).WithLegend(null)
            .AddNode("small", "A", 40, 40, width: 160, height: 40)
            .AddNodeDetail("small", "Status", "Ready");
        var baseTheme = VisualTheme.Graphite();
        var theme = new VisualTheme(baseTheme.Resolve(VisualThemeMode.Light).ToTokens(), baseTheme.Resolve(VisualThemeMode.Dark).ToTokens(),
            new VisualTypography(dataLabelSize: 40));
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 260), 20), theme,
            frame: new VisualFrame(showLegend: false)), new TopologyRenderOptions { IncludeLegend = false });
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "A");
        Assert.DoesNotContain(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-node-detail-separator");
        Assert.DoesNotContain(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-node-detail");
        Assert.Equal("Ready", Assert.Single(Assert.Single(prepared.SemanticInterchange!.Nodes).Details).Value);
    }

    [Theory]
    [InlineData(TextMeasurementMode.PortableEstimate)]
    [InlineData(TextMeasurementMode.InstalledFonts)]
    public void LargeThemeIconCaptionReservesItsPaintedPlateForLayoutAndRelationshipLabels(TextMeasurementMode measurement) {
        var chart = TopologyChart.Create().WithId("large-icon-caption").WithViewport(700, 380, 20).WithLegend(null)
            .AddNode("left", "Left", 40, 130, width: 120, height: 70)
            .AddNode("right", "Right", 530, 130, width: 120, height: 70)
            .AddNode("owner", "Payments Platform Owner", 290, 88, width: 48, height: 42)
            .WithNodeDisplay("owner", TopologyNodeDisplayMode.Icon)
            .AddEdge("link", "left", "right", "64 ms", routing: TopologyEdgeRouting.Straight);
        var baseTheme = VisualTheme.Graphite();
        var theme = new VisualTheme(baseTheme.Resolve(VisualThemeMode.Light).ToTokens(), baseTheme.Resolve(VisualThemeMode.Dark).ToTokens(),
            new VisualTypography(dataLabelSize: 28));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(700, 380), 20), theme,
            frame: new VisualFrame(showLegend: false));
        var prepared = chart.Prepare(context, new TopologyRenderOptions { IncludeLegend = false, IncludeIconLabels = true, TextMeasurementMode = measurement });
        var svg = XDocument.Parse(prepared.ToSvg());
        var plate = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-node-icon-label");
        var x = (double)plate.Attribute("x")!; var y = (double)plate.Attribute("y")!;
        var width = (double)plate.Attribute("width")!; var height = (double)plate.Attribute("height")!;
        Assert.True(width > 34 && height > 20);
        Assert.True(x >= 20 && y >= 20 && x + width <= 680 && y + height <= 360);
        var label = Assert.Single(prepared.SemanticInterchange!.Edges).ResolvedLabelBounds!.Value;
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "64 ms"
            && Math.Abs((double)element.Attribute("font-size")! - 28) < .001);
        Assert.True(label.Right <= x || label.Left >= x + width || label.Bottom <= y || label.Top >= y + height,
            "An edge caption must avoid the complete plate drawn at the selected typography scale.");
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Ancestors().Any(parent => (string?)parent.Attribute("data-cfx-role") == "topology-node-label")
            && Math.Abs((double)element.Attribute("font-size")! - 28 * 10.5 / 11) < .001);
        Assert.NotEmpty(prepared.ToPng());
        Assert.Equal(88, chart.Nodes.Single(node => node.Id == "owner").Y);
    }
}
