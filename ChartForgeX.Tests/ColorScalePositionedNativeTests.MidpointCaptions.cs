using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ColorScalePositionedNativeTests {
    [Theory]
    [InlineData("region-map")]
    [InlineData("tile-map")]
    [InlineData("heatmap")]
    [InlineData("hexbin")]
    [InlineData("calendar")]
    [InlineData("scatter")]
    [InlineData("bubble")]
    [InlineData("treemap")]
    [InlineData("sunburst")]
    public void GradientEndpointAndNearEdgeMidpointCaptionsStayReadableAtTheirNumericAnchor(string family) {
        foreach (var midpoint in new[] { -20d, -19.4, 39.4, 40 }) {
            var scale = ChartColorScale.Diverging(Cold, ChartColor.White, Hot, midpoint)
                .WithValueRange(-20, 40).WithNoDataColor(Missing).WithLabels("Cold", "Pivot", "Hot");
            var chart = Create(family, scale, 360, dark: midpoint > 0);
            Dimension(chart, family).LegendStyle = ChartColorLegendStyle.Gradient;
            var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
            var gradient = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
            var key = ((VisualSceneRectangle)gradient.Shape).Bounds;
            var id = family is "region-map" or "tile-map" ? "map-scale-midpoint" : Prefix(family) + "-label-midpoint";
            var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Id == id);
            var region = prepared.Regions.Single(item => item.Id == id);
            var value = midpoint.ToString("0.#", CultureInfo.InvariantCulture);
            var full = family is "region-map" or "tile-map" ? "Pivot " + value : "Pivot · " + value;
            Assert.Equal(full, Assert.Single(caption.Text.Lines).Text); Assert.Equal(full, region.Label);
            Assert.Equal(full, document.Descendants().Single(node => (string?)node.Attribute("data-cfx-source-id") == id)
                .Descendants().Single(node => node.Name.LocalName == "text").Value);
            Assert.True(caption.Text.Size >= 8); Assert.True(region.Bounds.Width > 0); Assert.True(region.Bounds.Height > 0);
            Assert.Equal(midpoint < 0 ? TextAlignment.Left : TextAlignment.Right, caption.Alignment);
            Assert.Equal(key.Left + key.Width * ((midpoint + 20) / 60), caption.X, 6);
            Assert.InRange(caption.LineLeft(caption.Text.Lines[0]), region.Bounds.Left - .000001, region.Bounds.Right);
            Assert.InRange(caption.LineLeft(caption.Text.Lines[0]) + caption.Text.Metrics.Width, region.Bounds.Left, region.Bounds.Right + .000001);
            Assert.InRange(region.Bounds.Left, key.Left - .000001, key.Right);
            Assert.InRange(region.Bounds.Right, key.Left, key.Right + .000001);
            Assert.Equal(value, (string?)Role(document, Prefix(family)).Single().Attribute("data-cfx-midpoint-value"));
            AssertMappedMarks(prepared, document, family, scale); AssertGradientPixels(prepared, gradient, scale);
            var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            var bounds = region.Bounds;
            Assert.True(Enumerable.Range((int)Math.Ceiling(bounds.Top), Math.Max(0, (int)Math.Floor(bounds.Bottom) - (int)Math.Ceiling(bounds.Top)))
                .Any(y => Enumerable.Range((int)Math.Ceiling(bounds.Left), Math.Max(0, (int)Math.Floor(bounds.Right) - (int)Math.Ceiling(bounds.Left)))
                    .Any(x => { var offset = (y * image.Width + x) * 4; return image.Pixels[offset] != image.Pixels[0]
                        || image.Pixels[offset + 1] != image.Pixels[1] || image.Pixels[offset + 2] != image.Pixels[2]; })),
                "The native midpoint caption lane must contain painted glyphs.");
            ScatterBubbleColorRenderingTests.Capture(prepared, "midpoint-" + family + "-" + value + "-360-" + (midpoint > 0 ? "dark" : "light"));
        }
    }
}
