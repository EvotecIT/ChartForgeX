using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Interactive highlight styles must leave unhighlighted native graph exports unchanged.</summary>
public sealed class GraphStaticHighlightPaintTests {
    [Theory]
    [InlineData(false, 960, 560)]
    [InlineData(true, 1600, 900)]
    public void UnhighlightedFullAndNeighborhoodExportsKeepPlainOutlinesAndWhitePerimeters(bool neighborhood, int width, int height) {
        var graph = GraphScene.Create("plain-static-graph", "Service connections")
            .AddNode("a", "API", node => { node.Shape = GraphNodeShape.Box; node.X = 180; node.Y = 180; })
            .AddNode("b", "Database", node => { node.Shape = GraphNodeShape.Ellipse; node.X = 480; node.Y = 180; })
            .AddNode("c", "Queue", node => { node.Shape = GraphNodeShape.Diamond; node.X = 680; node.Y = 340; })
            .AddEdge("ab", "a", "b").AddEdge("bc", "b", "c");
        var stage = neighborhood ? graph.CreateNeighborhood("b") : null;
        void Configure(GraphSceneStaticRenderOptions options) { options.Width = width; options.Height = height; }
        var svg = graph.ToGraphSvg(stage, Configure);
        var background = XDocument.Parse(svg).Descendants().Single(element => (string?)element.Attribute("class") == "cfx-graph-bg");
        Assert.Equal("fill:var(--cfx-color-paper,#ffffff)", (string?)background.Attribute("style"));
        Assert.Null(background.Attribute("stroke"));
        Assert.DoesNotContain("<script", svg, StringComparison.OrdinalIgnoreCase);
        var raster = RasterImageDecoder.Decode(graph.ToGraphPng(stage, Configure));
        Assert.Equal(width, raster.Width);
        Assert.Equal(height, raster.Height);
        var coloredPerimeter = 0;
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
            if (x != 0 && x != width - 1 && y != 0 && y != height - 1) continue;
            var offset = (y * width + x) * 4;
            if (raster.Pixels[offset] != 255 || raster.Pixels[offset + 1] != 255
                || raster.Pixels[offset + 2] != 255 || raster.Pixels[offset + 3] != 255) coloredPerimeter++;
        }
        Assert.Equal(0, coloredPerimeter);
        // A plain node outline is still painted; a blank white export cannot satisfy this contract.
        Assert.Contains(Enumerable.Range(0, raster.Pixels.Length / 4), index =>
            Math.Abs(raster.Pixels[index * 4] - 96) <= 3 && Math.Abs(raster.Pixels[index * 4 + 1] - 165) <= 3
            && Math.Abs(raster.Pixels[index * 4 + 2] - 250) <= 3);
    }
}
