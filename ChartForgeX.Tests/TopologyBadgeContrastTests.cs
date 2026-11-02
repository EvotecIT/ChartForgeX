using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyBadgeContrastTests {
    [Theory]
    [InlineData(false, VisualThemeMode.Light)]
    [InlineData(false, VisualThemeMode.Dark)]
    [InlineData(true, VisualThemeMode.Light)]
    [InlineData(true, VisualThemeMode.Dark)]
    public void DefaultWarningBadgeUsesReadableNativeInkAndRetainsItsPairedSvgPaint(bool enclosed, VisualThemeMode mode) {
        var chart = TopologyChart.Create().WithViewport(400, 280, 20);
        chart.Nodes.Add(new TopologyNode { Id = "approval", Label = "Approved?", Badge = "?", Status = TopologyHealthStatus.Warning,
            X = 40, Y = 40, Width = 220, Height = enclosed ? 120 : 80, PreserveDisplayModeSize = true,
            Shape = enclosed ? TopologyNodeShape.Diamond : TopologyNodeShape.Rectangle,
            Subtitle = enclosed ? "Human gate" : null, FitLabelToContent = enclosed });
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 280), 20), VisualTheme.Graphite(), mode,
            frame: new VisualFrame(showLegend: false), font: new FontSpec { Family = "Carlito",
                FilePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf") });
        var options = new TopologyRenderOptions { IncludeLegend = false, WrapNodeLabels = true };
        var literal = chart.Prepare(context, options);
        var badge = Assert.Single(literal.Scene.Nodes.OfType<VisualSceneRectangle>(), mark => mark.Role == "topology-node-badge-surface");
        var text = Assert.Single(literal.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-badge");
        Assert.Equal("?", Assert.Single(text.Text.Lines).Text);
        Assert.Equal(9, text.Text.Size);
        Assert.True(ChartColorMath.ContrastRatio(text.Color, badge.Fill!.Value) >= 4.5,
            "Small default badge text must retain readable contrast against its actual native fill.");
        var svg = XDocument.Parse(literal.ToSvg());
        Assert.Equal(text.Color.ToCss(), BadgeText(svg).Attribute("fill")!.Value);
        ScatterBubbleColorRenderingTests.Capture(literal, "badge-contrast-" + (enclosed ? "enclosed-" : "card-") + mode);

        options.SvgColorVariables = new SvgColorVariables().Add("--warning-badge", badge.Fill.Value, SvgColorRole.Status)
            .AddInk("--warning-badge-ink", badge.Fill.Value, text.Color, SvgColorRole.Status);
        var bound = chart.Prepare(context, options);
        var themedSvg = XDocument.Parse(bound.ToSvg());
        var surface = Assert.Single(themedSvg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-node-badge-surface");
        Assert.Contains("var(--warning-badge,", surface.Attribute("fill")!.Value);
        Assert.Contains("var(--warning-badge-ink,", BadgeText(themedSvg).Attribute("fill")!.Value);
        Assert.Equal(literal.ToPng(), bound.ToPng());
    }

    private static XElement BadgeText(XDocument svg) => Assert.Single(
        Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-node-badge")
            .Descendants(), element => element.Name.LocalName == "text");
}
