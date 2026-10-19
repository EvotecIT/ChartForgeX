using System.Globalization;
using System.Text.RegularExpressions;
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
        Assert.True(HasDefaultOutlineStroke(svg, raster, "a"), "The current SVG outline paint must remain visible at the plain node boundary.");
    }

    /// <summary>Checks the outer stroke band of a plain circle or box against its SVG default paint.</summary>
    internal static bool HasDefaultOutlineStroke(string svg, RgbaImage raster, string nodeId) {
        var color = Regex.Match(svg, @"stroke:\s*var\(--cfx-node-stroke,\s*#([0-9a-fA-F]{6})\)");
        if (!color.Success) throw new InvalidOperationException("The graph SVG must declare its raster-safe default node stroke.");
        var hex = color.Groups[1].Value;
        var expected = Enumerable.Range(0, 3).Select(index => int.Parse(hex.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
        var document = XDocument.Parse(svg);
        var viewport = Numbers(document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "graph-viewport").Attribute("transform")!.Value);
        var node = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "graph-node" && (string?)element.Attribute("data-node-id") == nodeId);
        var position = Numbers(node.Attribute("transform")!.Value);
        var mark = node.Elements().First(element => element.Name.LocalName is "circle" or "rect");
        var box = Numbers(document.Root!.Attribute("viewBox")!.Value);
        var scale = Math.Min(raster.Width / box[2], raster.Height / box[3]);
        var originX = (raster.Width - box[2] * scale) / 2 - box[0] * scale;
        var originY = (raster.Height - box[3] * scale) / 2 - box[1] * scale;
        var sampled = new HashSet<int>();
        var matches = 0;
        for (var column = -3; column <= 3; column++) {
            double localX, boundary;
            if (mark.Name.LocalName == "circle") {
                var radius = Number(mark, "r");
                localX = column / 3.0 * radius * .15;
                boundary = -Math.Sqrt(radius * radius - localX * localX);
            } else {
                var width = Number(mark, "width");
                localX = Number(mark, "x") + width / 2 + column / 3.0 * width * .075;
                boundary = Number(mark, "y");
            }
            // This band lies outside the fill and away from labels, status dots,
            // rounded corners and the image perimeter. Fill antialiasing alone
            // cannot stand in for the three-unit native outline.
            foreach (var distance in new[] { .65, .85, 1.05, 1.25 }) {
                var x = (int)Math.Round(originX + (viewport[0] + (position[0] + localX) * viewport[2]) * scale);
                var y = (int)Math.Round(originY + (viewport[1] + (position[1] + boundary - distance) * viewport[2]) * scale);
                if (x < 0 || y < 0 || x >= raster.Width || y >= raster.Height) continue;
                var offset = (y * raster.Width + x) * 4;
                if (!sampled.Add(offset)) continue;
                if (raster.Pixels[offset + 3] == 255 && Enumerable.Range(0, 3).All(channel => Math.Abs(raster.Pixels[offset + channel] - expected[channel]) <= 3)) matches++;
            }
        }
        return matches >= 3;

        static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
        static double[] Numbers(string value) => Regex.Matches(value, @"[+-]?(?:\d*\.)?\d+(?:[eE][+-]?\d+)?")
            .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
    }
}
