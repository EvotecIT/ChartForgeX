using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Absolute color anchors remain complete semantic facts while native keys sample their numeric domain.</summary>
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
    public void IrregularAnchorsKeepCompleteFactsMappedMarksAndNativeNumericKeySamples(string family) {
        var scale = Scale(); var chart = Create(family, scale); var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        AssertMetadata(prepared, document, family, scale); AssertKeyPaint(prepared, document, family, scale);
        AssertMappedMarks(prepared, document, family, scale);
        var swatches = Swatches(prepared, family); var prefix = Prefix(family);
        var captionId = family is "region-map" or "tile-map" ? "map-scale-midpoint" : prefix + "-label-midpoint";
        var caption = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == captionId);
        var expectedX = swatches[0].Bounds.Left + (4d / 3 + .5) * swatches[0].Bounds.Width;
        Assert.Equal(expectedX, caption.X, 6);
        Assert.Contains("Zero", prepared.Regions.Single(region => region.Id == captionId).Label);
        ScatterBubbleColorRenderingTests.Capture(prepared, "positioned-" + family);
    }

    [Theory]
    [InlineData("tile-map")]
    [InlineData("heatmap")]
    [InlineData("scatter")]
    [InlineData("treemap")]
    public void EveryAuthoredRgbaAnchorSurvivesElevenSampleKeysAndDetachedExports(string family) {
        var stops = Enumerable.Range(0, 16).Select(index => new ChartColorStop(index * index - 20, (index % 2 == 0 ? Cold : Hot).WithAlpha((byte)(37 + index * 11)))).ToArray();
        var scale = ChartColorScale.Sequential(stops); var chart = Create(family, scale).WithValueFormatter(value => value.ToString("R", CultureInfo.InvariantCulture) + " kWh");
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var document = XDocument.Parse(svg);
        AssertMetadata(prepared, document, family, scale); AssertKeyPaint(prepared, document, family, scale);
        Assert.Equal(11, Swatches(prepared, family).Length);
        var description = prepared.Regions.Single(region => region.Role == Prefix(family) + "-stops").Label;
        if (family == "scatter") Assert.DoesNotContain("kWh", description); else Assert.Contains("kWh", description);
        SetScale(chart, family, scale.WithValueRange(-40, 300).WithLabels("Changed", null, "Changed"));
        chart.WithValueFormatter(value => "Changed").ConfigureLabels(labels => labels.Color = "Changed");
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
        ScatterBubbleColorRenderingTests.Capture(prepared, "positioned-rgba-" + family);
    }

    [Theory]
    [InlineData("tile-map", 360, true)]
    [InlineData("region-map", 800, false)]
    public void RightMapKeysReverseCanonicalSamplesAndAnchorTheMidpointToItsNumericPosition(string family, int width, bool dark) {
        var scale = Scale(); var chart = Create(family, scale, width, dark).WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        AssertMetadata(prepared, document, family, scale); AssertKeyPaint(prepared, document, family, scale, right: true);
        var swatches = Swatches(prepared, family); Assert.Equal(5, swatches.Length);
        var expectedY = swatches[0].Bounds.Top + (5 - .5 - 4d / 3) * swatches[0].Bounds.Height;
        var caption = prepared.Regions.Single(region => region.Id == "map-scale-label-1");
        Assert.Equal(expectedY, caption.Bounds.Top + caption.Bounds.Height / 2, 6);
        ScatterBubbleColorRenderingTests.Capture(prepared, "positioned-right-" + family);
    }

    [Theory]
    [InlineData("tile-map")]
    [InlineData("heatmap")]
    [InlineData("treemap")]
    public void CompactEndpointAdjacentPivotKeepsItsAnchorAndFullCaption(string family) {
        var scale = ChartColorScale.Diverging(new[] { new ChartColorStop(-20, Cold) }, new ChartColorStop(-19.5, ChartColor.White),
            new[] { new ChartColorStop(-3, Cool), new ChartColorStop(5, Warm), new ChartColorStop(40, Hot) }).WithLabels(null, "Zero", null);
        var chart = Create(family, scale, 360, dark: true);
        Assert.Equal("CFX Positioned Native Carlito", chart.Options.Theme.Typography.Family);
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        AssertMetadata(prepared, document, family, scale); AssertKeyPaint(prepared, document, family, scale);
        var swatches = Swatches(prepared, family); var prefix = Prefix(family);
        var id = family == "tile-map" ? "map-scale-midpoint" : prefix + "-label-midpoint";
        var caption = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == id);
        Assert.Equal(swatches[0].Bounds.Left + (4 * .5 / 60 + .5) * swatches[0].Bounds.Width, caption.X, 6);
        var region = prepared.Regions.Single(item => item.Id == id);
        Assert.Contains("-19.5", region.Label); Assert.Equal(region.Label, string.Join("", caption.Text.Lines.Select(line => line.Text)));
        Assert.True(caption.Text.Size >= 10);
        ScatterBubbleColorRenderingTests.Capture(prepared, "positioned-edge-" + family);
    }

    private static void AssertMappedMarks(PreparedVisual prepared, XDocument document, string family, ChartColorScale scale) {
        var sourceRole = family switch {
            "scatter" or "bubble" => "point", "heatmap" => "heatmap-cell", "hexbin" => "hexbin-cell", "calendar" => "calendar-cell",
            "sunburst" => "sunburst-segment", "treemap" => "treemap-tile", _ => family + "-region-source"
        };
        var markRole = family switch {
            "scatter" => "marker", "bubble" => "bubble", "heatmap" => "heatmap-cell-shape", "hexbin" => "hexbin-cell-shape",
            "calendar" => "calendar-cell-shape", "sunburst" => "sunburst-segment-mark", "treemap" => "treemap-tile-mark", _ => family + "-region"
        };
        var valueName = family is "scatter" or "bubble" or "treemap" or "sunburst" ? "data-cfx-color-value" : "data-cfx-value";
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        foreach (var source in Role(document, sourceRole)) {
            var raw = (string?)source.Attribute(valueName);
            var expected = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? scale.ColorFor(value) : Missing;
            var shape = source.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == markRole);
            Assert.Equal((family == "bubble" ? ChartColorMath.WithOpacity(expected, ChartVisualPrimitives.BubbleFillOpacity) : expected).ToCss(), (string?)shape.Attribute("fill"));
            if (family is "scatter" or "bubble") AssertPixel(image, Number(shape, "cx"), Number(shape, "cy"),
                family == "bubble" ? ChartColorMath.WithOpacity(expected, ChartVisualPrimitives.BubbleFillOpacity) : expected);
            else if (family == "sunburst") {
                var slice = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Single(node => node.Start == Number(source, "data-cfx-start-angle")
                    && node.Sweep == Number(source, "data-cfx-sweep") && node.Inner == Number(source, "data-cfx-inner-radius"));
                var angle = slice.Start + slice.Sweep / 2; var radius = (slice.Inner + slice.Outer) / 2;
                AssertPixel(image, slice.Cx + Math.Cos(angle) * radius, slice.Cy + Math.Sin(angle) * radius, expected);
            } else if (family != "region-map") {
                var id = (string)source.Attribute("data-cfx-source-id")!;
                var box = prepared.Regions.Single(region => region.Id == id).Bounds;
                AssertPixel(image, box.Left + box.Width / 2, box.Top + box.Height / 2, expected);
            }
        }
    }
}
