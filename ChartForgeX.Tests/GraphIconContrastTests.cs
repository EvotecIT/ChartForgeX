using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphIconContrastTests {
    [Theory]
    [InlineData(true, "#ffffff", HtmlGraphExplorerTheme.Light)]
    [InlineData(true, "#0f172a", HtmlGraphExplorerTheme.Light)]
    [InlineData(false, "#ffffff", HtmlGraphExplorerTheme.Light)]
    [InlineData(true, "#ffffff40", HtmlGraphExplorerTheme.Dark)]
    public void NativeIconsLeaveContrastingInkOnTheirAuthoredMark(bool card, string fill, HtmlGraphExplorerTheme theme) {
        var graph = GraphScene.Create("icons", "Icons").AddNode("n", "Node", node => {
            node.X = 480; node.Y = 280; node.Fixed = true; node.Size = 60; node.IconText = "DOM";
            node.Style.BackgroundColor = fill;
            if (card) { node.Shape = GraphNodeShape.Box; node.Metadata["topology.card"] = "true"; }
        });
        void Configure(GraphSceneStaticRenderOptions options) { options.Width = 960; options.Height = 560; options.Theme = theme; }
        var svg = XDocument.Parse(graph.ToGraphSvg(configure: Configure));
        var image = RasterImageDecoder.Decode(graph.ToGraphPng(configure: Configure));
        graph.Nodes[0].IconText = null;
        var referenceSvg = XDocument.Parse(graph.ToGraphSvg(configure: Configure));
        var reference = RasterImageDecoder.Decode(graph.ToGraphPng(configure: Configure));
        var viewport = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "graph-viewport");
        Assert.Equal(viewport.Attribute("transform")!.Value,
            referenceSvg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "graph-viewport").Attribute("transform")!.Value);
        var transform = Regex.Matches(viewport.Attribute("transform")!.Value, @"-?\d+(?:\.\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        var x = transform[0] + (480 + (card ? -60 * 1.45 + 28 : 0)) * transform[2];
        var y = transform[1] + 280 * transform[2];
        var contrast = 1.0; var changed = 0;
        for (var py = Math.Max(0, (int)y - 16); py < Math.Min(image.Height, (int)y + 16); py++) {
            for (var px = Math.Max(0, (int)x - 35); px < Math.Min(image.Width, (int)x + 35); px++) {
                var offset = (py * image.Width + px) * 4;
                var before = ChartColor.FromRgb(reference.Pixels[offset], reference.Pixels[offset + 1], reference.Pixels[offset + 2]);
                var after = ChartColor.FromRgb(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2]);
                if (before.Equals(after)) continue;
                changed++; contrast = Math.Max(contrast, ChartColorMath.ContrastRatio(before, after));
            }
        }
        Assert.True(changed > 20, "The native icon must leave actual glyph ink on the same mark geometry.");
        Assert.True(contrast >= 4.5, "The opaque interior of the native icon must contrast with its composited surface.");
    }
}
