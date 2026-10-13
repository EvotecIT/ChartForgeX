using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BlockLayoutCircleConnectionTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(BlockLayoutShape.Rectangle, BlockLayoutShape.Circle, 960, 0, false, false)]
    [InlineData(BlockLayoutShape.Circle, BlockLayoutShape.Rectangle, 960, 0, false, false)]
    [InlineData(BlockLayoutShape.Circle, BlockLayoutShape.Circle, 960, 0, false, false)]
    [InlineData(BlockLayoutShape.Circle, BlockLayoutShape.Circle, 320, 0, true, false)]
    [InlineData(BlockLayoutShape.Circle, BlockLayoutShape.Circle, 320, 1, false, false)]
    [InlineData(BlockLayoutShape.Circle, BlockLayoutShape.Circle, 960, 2, false, false)]
    [InlineData(BlockLayoutShape.Rounded, BlockLayoutShape.Circle, 320, 2, false, false)]
    [InlineData(BlockLayoutShape.Circle, BlockLayoutShape.Database, 960, 2, true, false)]
    [InlineData(BlockLayoutShape.Circle, BlockLayoutShape.Circle, 320, 0, false, true)]
    public async Task ConnectorsMeetThePaintedCircleBoundaryInSvgAndPng(BlockLayoutShape firstShape,
        BlockLayoutShape secondShape, int width, int layout, bool reverse, bool graphite) {
        var theme = graphite ? VisualTheme.Graphite() : VisualTheme.Report();
        theme = theme.Configure(builder => { builder.Light.Axis = builder.Dark.Axis = ChartColor.FromHex("#FF0000"); });
        theme = theme.Configure(builder => { builder.Light.Palette = builder.Dark.Palette = new[] { ChartColor.FromHex("#2563EB") }; });
        var block = BlockLayoutBlock.Create().WithSize(width, 300).WithCard(false).WithTheme(theme)
            .WithColumns(layout == 1 ? 1 : 2).AddItem("a", "A", shape: firstShape);
        if (layout == 2) block.AddSpace().AddSpace();
        block.AddItem("b", "B", shape: secondShape).AddEdge(reverse ? "b" : "a", reverse ? "a" : "b");
        var svg = block.ToSvg();
        var image = block.RenderRgba();
        await CaptureAsync(block, svg, width, firstShape + "-" + secondShape + "-" + layout + "-" + width + "-" + reverse + "-" + graphite);
        var document = XDocument.Parse(svg);
        var edge = Assert.Single(document.Descendants(), element => Role(element) == "block-layout-edge");
        var source = Node(document, reverse ? "b" : "a");
        var target = Node(document, reverse ? "a" : "b");
        AssertEndpoint(source, Number(edge, "x1"), Number(edge, "y1"), target, image, width);
        AssertEndpoint(target, Number(edge, "x2"), Number(edge, "y2"), source, image, width);
    }

    [Fact]
    public void RectangularConnectionsKeepTheirExistingSidePorts() {
        var block = BlockLayoutBlock.Create().WithSize(960, 300).WithColumns(2)
            .AddItem("a", "A").AddItem("b", "B", shape: BlockLayoutShape.Rounded).AddEdge("a", "b");
        var document = XDocument.Parse(block.ToSvg());
        var edge = Assert.Single(document.Descendants(), element => Role(element) == "block-layout-edge");
        var source = Node(document, "a"); var target = Node(document, "b");
        Assert.Equal(Number(source, "x") + Number(source, "width"), Number(edge, "x1"), 2);
        Assert.Equal(Number(target, "x"), Number(edge, "x2"), 2);
        Assert.Equal(Number(source, "y") + Number(source, "height") / 2, Number(edge, "y1"), 2);
        Assert.Equal(Number(target, "y") + Number(target, "height") / 2, Number(edge, "y2"), 2);
    }

    private static void AssertEndpoint(XElement node, double x, double y, XElement other, RgbaImage image, int width) {
        if (node.Name.LocalName != "circle") return;
        var cx = Number(node, "cx"); var cy = Number(node, "cy"); var radius = Number(node, "r");
        var dx = x - cx; var dy = y - cy;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        Assert.InRange(Math.Abs(distance - radius), 0, 0.02);
        var otherX = other.Name.LocalName == "circle" ? Number(other, "cx") : Number(other, "x") + Number(other, "width") / 2;
        var otherY = other.Name.LocalName == "circle" ? Number(other, "cy") : Number(other, "y") + Number(other, "height") / 2;
        Assert.True(dx * (otherX - cx) + dy * (otherY - cy) > 0, "The endpoint must face the connected node.");
        // Sample the segment just outside the outline, which is painted over the exact endpoint.
        var scale = (double)image.Width / width;
        var pixelX = (int)Math.Round((cx + dx * (radius + 3) / distance) * scale);
        var pixelY = (int)Math.Round((cy + dy * (radius + 3) / distance) * scale);
        var red = false;
        for (var py = Math.Max(0, pixelY - 2); py <= Math.Min(image.Height - 1, pixelY + 2); py++)
            for (var px = Math.Max(0, pixelX - 2); px <= Math.Min(image.Width - 1, pixelX + 2); px++) {
                var offset = (py * image.Width + px) * 4;
                red |= image.Pixels[offset] > 200 && image.Pixels[offset + 1] < 60 && image.Pixels[offset + 2] < 60 && image.Pixels[offset + 3] > 100;
            }
        Assert.True(red, "Native PNG must paint the connector immediately outside the circle.");
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static XElement Node(XDocument document, string id) => Assert.Single(document.Descendants(),
        element => Role(element) == "block-layout-node" && (string?)element.Attribute("data-block-id") == id);
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);

    private static async Task CaptureAsync(BlockLayoutBlock block, string svg, int width, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(directory, name + "-native.png"), block.ToPng());
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, 300);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (!string.IsNullOrWhiteSpace(directory))
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + "-browser.png"), FullPage = true });
    }
}
