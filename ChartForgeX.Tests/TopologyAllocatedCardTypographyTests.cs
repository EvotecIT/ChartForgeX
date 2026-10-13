using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyAllocatedCardTypographyTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData("Arial")]
    [InlineData("Inter, Segoe UI, system-ui, sans-serif")]
    public async Task CompactPillSubtitleRetainsReadableOrdinaryTypography(string font) {
        var theme = TopologyTheme.Light(); theme.Typography = new VisualTypography(font, dataLabelSize: 12);
        var chart = TopologyChart.Create().WithTheme(theme).WithViewport(240, 160, 16)
            .AddNode("region", "AMER", 50, 60, kind: TopologyNodeKind.Hub, width: 104, height: 34, subtitle: "47 sites")
            .WithNodeDisplay("region", TopologyNodeDisplayMode.Pill);
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false };
        var svg = chart.ToSvg(options); var xml = XDocument.Parse(svg);
        var subtitle = Assert.Single(RoleText(xml, "topology-node-subtitle"));
        Assert.Equal("47 sites", subtitle.Value);
        Assert.True((double)subtitle.Attribute("font-size")! >= 8);
        await Capture(svg, chart.ToPng(options), "ordinary-pill-subtitle-" + (font == "Arial" ? "arial" : "system"));
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(32)]
    [InlineData(256)]
    public async Task GeographicCalloutsRetainTheirFixedHeightText(int size) {
        var theme = TopologyTheme.Light(); theme.Typography = new VisualTypography("Arial", dataLabelSize: size);
        var chart = TopologyChart.Create().WithTheme(theme).WithViewport(720, 420, 24)
            .WithLayout(TopologyLayoutMode.Geographic).WithMapViewport(ChartMapViewport.World())
            .AddGroup("region", "Europe", 0, 0, 100, 80, subtitle: "One site")
            .WithGroupCoordinates("region", 10, 50)
            .AddNode("site", "Site", 0, 0, groupId: "region").WithNodeCoordinates("site", 10, 50);
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, IncludeGroups = false, IncludeGeographicCallouts = true };
        var svg = chart.ToSvg(options); var xml = XDocument.Parse(svg);
        Assert.Contains(RoleText(xml, "topology-callout-title"), element => element.Value == "Europe");
        Assert.NotEmpty(RoleText(xml, "topology-callout-subtitle"));
        Assert.Equal(4, RoleText(xml, "topology-callout-count").Count());
        await Capture(svg, chart.ToPng(options), "geographic-callout-" + size);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, 32)]
    [InlineData(false, 256)]
    [InlineData(true, 32)]
    [InlineData(true, 256)]
    public async Task SourceCardsRetainSubtitleWithoutDetailRows(bool requirement, int size) {
        var source = requirement ? "requirementDiagram\nrequirement API {\n text: HTTP\n}" : "C4Container\nContainer(api, \"API\", \"HTTP\", \"Service\")";
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size + "}}}%%\n" + source);
        Assert.False(result.HasErrors);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var node = Assert.Single(chart.Nodes);
        Assert.Empty(node.Details); Assert.Contains("HTTP", node.Subtitle);
        var svg = chart.ToSvg();
        var xml = XDocument.Parse(svg);
        Assert.Contains(RoleText(xml, "topology-node-label"), element => element.Value.Contains("API"));
        Assert.Contains(RoleText(xml, "topology-node-subtitle"), element => element.Value.Contains("HTTP"));
        await Capture(svg, chart.ToPng(), "subtitle-card-" + requirement + "-" + size);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card, 11)]
    [InlineData(TopologyNodeDisplayMode.Card, 32)]
    [InlineData(TopologyNodeDisplayMode.Card, 256)]
    [InlineData(TopologyNodeDisplayMode.Icon, 256)]
    [InlineData(TopologyNodeDisplayMode.Dot, 256)]
    public async Task ThemeDrivenExportsFitTheCompleteBadgeAndNodeSymbol(TopologyNodeDisplayMode mode, int size) {
        var theme = TopologyTheme.Light();
        theme.Typography = new VisualTypography("Arial", dataLabelSize: size);
        var chart = TopologyChart.Create().WithTheme(theme).WithViewport(360, 240, 16)
            .AddNode("api", "API", 80, 60, width: 120, height: 64, symbol: "A").WithNodeDisplay("api", mode);
        chart.Nodes[0].Badge = "42";
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false };
        var svg = chart.ToSvg(options);
        var xml = XDocument.Parse(svg);
        var badge = Assert.Single(RoleText(xml, "topology-node-badge"));
        Assert.Equal("42", badge.Value);
        var surface = Assert.Single(xml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-node-badge-surface")
            .SelectMany(element => element.DescendantsAndSelf()), element => element.Name.LocalName == "rect");
        var builder = new VisualSceneBuilder(new VisualSize(360, 240), ChartForgeX.Typography.FontSpec.FromFamily("Arial"));
        var metrics = builder.MeasureText(badge.Value, (double)badge.Attribute("font-size")!, 600);
        Assert.True(metrics.LineHeight <= (double)surface.Attribute("height")! + .01);
        Assert.True(metrics.Width <= (double)surface.Attribute("width")! + .01);
        Assert.Contains(RoleText(xml, mode == TopologyNodeDisplayMode.Dot ? "topology-node-symbol" : "topology-node-icon"), element => element.Value == "A");
        await Capture(svg, chart.ToPng(options), "badge-symbol-" + mode + "-" + size);
    }

    private static IEnumerable<XElement> RoleText(XDocument xml, string role) => xml.Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role)
        .SelectMany(element => element.DescendantsAndSelf()).Where(element => element.Name.LocalName == "text");

    private static async Task Capture(string svg, byte[] png, string name) {
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", 960, 720);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png"), FullPage = true });
    }
}
