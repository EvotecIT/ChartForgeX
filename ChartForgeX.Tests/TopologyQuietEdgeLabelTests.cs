using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Ordinary relationship captions and fitted cards share the quiet diagram language in SVG and native pixels.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyQuietEdgeLabelTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RelationshipCaptionChipIsBorderlessNearCanvasAndHugsItsText(bool dark) {
        var chart = Source(dark); var prepared = chart.Prepare(); var svg = XDocument.Parse(prepared.ToSvg());
        var colors = chart.Theme.Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var tone = ChartColorMath.Blend(colors.Background, colors.Foreground, TopologyRenderPrimitives.EdgeLabelQuietTint);
        var surface = Role(svg, "topology-edge-label-surface").Single();
        var reserved = surface.Parent!;
        Assert.True((string?)surface.Attribute("stroke") is null or "none");
        Assert.True(ChartColor.TryParse((string?)surface.Attribute("fill"), out var fill)); Assert.Equal(tone, fill);
        var x = Number(surface, "x"); var y = Number(surface, "y"); var width = Number(surface, "width"); var height = Number(surface, "height");
        Assert.Equal(height / 2, Number(surface, "rx"), 3);
        // The visible chip is tighter than the reserved placement box in both axes.
        Assert.True(width < Number(reserved, "data-label-width") - 4, "The chip should hug the caption, not fill its clearance box.");
        Assert.True(height <= Number(reserved, "data-label-height"));
        Assert.Equal("400", (string?)Role(svg, "topology-edge-label-text").First().Attribute("font-weight") ?? "400");

        // Native pixels: the chip masks the route with its own tone and paints no border.
        var image = prepared.Visual.ToRgba(new VisualRenderOptions(supersampling: 1));
        AssertPixel(image, x + 2.5, y + height / 2, tone);
        AssertPixel(image, x + width / 2, y + 1, tone);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CaptionAndTechnologyLineShareOnePillWithTheSecondaryLineMuted(bool dark) {
        var chart = Source(dark); chart.Edges[0].SecondaryLabel = "HTTPS over the private link";
        var prepared = chart.Prepare(); var svg = XDocument.Parse(prepared.ToSvg());
        var colors = chart.Theme.Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var surface = Role(svg, "topology-edge-label-surface").Single();
        var top = Number(surface, "y"); var bottom = top + Number(surface, "height");
        var texts = Role(svg, "topology-edge-label-text").SelectMany(item => item.DescendantsAndSelf().Where(node => node.Name.LocalName == "text")).ToArray();
        Assert.Equal(2, texts.Length);
        // Both baselines sit inside the single pill; the technology line uses the secondary text colour.
        Assert.All(texts, text => Assert.InRange(Number(text, "y"), top, bottom));
        Assert.True(ChartColor.TryParse((string?)texts[1].Attribute("fill"), out var secondary)); Assert.Equal(colors.MutedForeground, secondary);
        // The wider second line sets the pill width, so the pill is wider than a single-line caption pill.
        var single = Source(dark).Prepare().ToSvg();
        Assert.True(Number(surface, "width") > Number(Role(XDocument.Parse(single), "topology-edge-label-surface").Single(), "width") + 20);
        var image = prepared.Visual.ToRgba(new VisualRenderOptions(supersampling: 1));
        var tone = ChartColorMath.Blend(colors.Background, colors.Foreground, TopologyRenderPrimitives.EdgeLabelQuietTint);
        AssertPixel(image, Number(surface, "x") + Number(surface, "height") / 2 + 1, (top + bottom) / 2, tone);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EndpointMultiplicitiesOnlyMaskTheirLineWithTheCanvasTone(bool dark) {
        var chart = Source(dark); var svg = XDocument.Parse(chart.Prepare().ToSvg());
        var background = chart.Theme.Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light).Background;
        var surfaces = Role(svg, "topology-endpoint-label-surface").ToArray();
        Assert.Equal(2, surfaces.Length);
        Assert.All(surfaces, item => { Assert.True(ChartColor.TryParse((string?)item.Attribute("fill"), out var fill)); Assert.Equal(background, fill); });
        Assert.All(Role(svg, "topology-endpoint-label"), item => Assert.Equal("400", (string?)item.Attribute("font-weight") ?? "400"));
    }

    [Fact]
    public void FittedCardsKeepTheirNaturalWidthBelowTheWrappingLimit() {
        var chart = TopologyChart.Create().WithId("fitted-cards").WithTitle("").WithViewport(900, 400, 24).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Manual).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), VisualThemeMode.Light);
        chart.AddNode("short", "Cache", 40, 100, width: 60, height: 60, subtitle: "Redis");
        chart.AddNode("long", "Order service", 400, 100, width: 60, height: 60, subtitle: "Validates every order, reserves stock and coordinates the payment provider");
        foreach (var node in chart.Nodes) {
            node.Kind = TopologyNodeKind.Generic; node.Shape = TopologyNodeShape.Rounded; node.CornerRadius = DiagramTheme.CardCornerRadius;
            node.FitLabelToContent = true; node.MaximumTextWidth = 190; node.ShowStatusBadge = false;
        }
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, WrapNodeLabels = true, AllowMultilineNodeLabels = true, MaxNodeSubtitleLines = 6 });
        var svg = XDocument.Parse(chart.Prepare().ToSvg());
        double Width(string id) {
            var numbers = SurfaceNumbers(svg, id); var xs = numbers.Where((_, index) => index % 2 == 0).ToArray(); return xs.Max() - xs.Min();
        }
        // Short content is not stretched to the 190-unit wrapping limit; long content wraps at it.
        Assert.InRange(Width("short"), 60, 120);
        Assert.InRange(Width("long"), 190 + 20 - 1, 190 + 20 + 1);
    }

    private static TopologyChart Source(bool dark) {
        var chart = TopologyChart.Create().WithId("quiet-label").WithTitle("").WithViewport(760, 400, 24).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Manual).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        chart.AddNode("a", "Plan", 60, 100, width: 140, height: 80); chart.AddNode("b", "Deliver", 500, 100, width: 140, height: 80);
        foreach (var node in chart.Nodes) { node.Kind = TopologyNodeKind.Generic; node.Shape = TopologyNodeShape.Rectangle; node.ShowStatusBadge = false; }
        chart.AddEdge("handoff", "a", "b", "Transfer", direction: VisualLinkDirection.Forward, routing: TopologyEdgeRouting.Straight);
        var edge = chart.Edges[0]; edge.SourceLabel = "1"; edge.TargetLabel = "many";
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false }); return chart;
    }

    private static IEnumerable<XElement> Role(XDocument svg, string role) => svg.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == role);
    private static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
    private static double[] SurfaceNumbers(XDocument svg, string id) {
        var node = svg.Descendants().First(item => (string?)item.Attribute("data-node-id") == id && (string?)item.Attribute("data-cfx-role") == "topology-node");
        var surface = node.Descendants().First(item => (string?)item.Attribute("data-cfx-role") == "topology-node-surface");
        return ((string)surface.Attribute("d")!).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .Select(token => double.Parse(token, CultureInfo.InvariantCulture)).ToArray();
    }
    private static void AssertPixel(RgbaImage image, double x, double y, ChartColor expected) {
        var offset = ((int)Math.Floor(y) * image.Width + (int)Math.Floor(x)) * 4;
        Assert.InRange(image.Pixels[offset], expected.R - 3, expected.R + 3);
        Assert.InRange(image.Pixels[offset + 1], expected.G - 3, expected.G + 3);
        Assert.InRange(image.Pixels[offset + 2], expected.B - 3, expected.B + 3);
    }
}
