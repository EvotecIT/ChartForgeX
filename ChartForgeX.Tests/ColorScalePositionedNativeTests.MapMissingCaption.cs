using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ColorScalePositionedNativeTests {
    [Theory]
    [InlineData("region-map", ChartColorLegendStyle.Auto)]
    [InlineData("tile-map", ChartColorLegendStyle.Auto)]
    [InlineData("region-map", ChartColorLegendStyle.Gradient)]
    [InlineData("region-map", ChartColorLegendStyle.Swatches)]
    [InlineData("tile-map", ChartColorLegendStyle.Gradient)]
    [InlineData("tile-map", ChartColorLegendStyle.Swatches)]
    public void NamedBottomMapKeysKeepMissingCaptionsReadableWithoutMovingNumericAnchors(string family, ChartColorLegendStyle legendStyle) {
        foreach (var width in new[] { 360, 800 }) foreach (var dark in new[] { false, true }) {
            var scale = Scale(); var chart = Create(family, scale, width, dark)
                .ConfigureLabels(labels => labels.NoData = "Not sampled")
                .ConfigureMapColorDimension(options => {
                    options.Name = "Temperature"; options.LegendStyle = legendStyle;
                    options.ValueFormat = ChartValueFormat.Number("0.0 '°C'", CultureInfo.GetCultureInfo("pl-PL"));
                });
            var prepared = PrepareNamedMapKey(chart, width, dark); var document = XDocument.Parse(prepared.ToSvg());
            var caption = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Id == "map-scale-no-data-label");
            var missing = prepared.Regions.Single(region => region.Id == caption.Id);
            var heading = prepared.Regions.Single(region => region.Id == "map-scale-title");
            var key = prepared.Regions.Single(region => region.Role == "map-scale-stops").Bounds;
            Assert.Equal("Not sampled", missing.Label); Assert.Equal(missing.Label, Assert.Single(caption.Text.Lines).Text);
            Assert.Equal(missing.Label, document.Descendants().Single(node => (string?)node.Attribute("data-cfx-source-id") == caption.Id)
                .Descendants().Single(node => node.Name.LocalName == "text").Value);
            Assert.True(caption.Text.Size >= 8); AssertTextInside(caption, missing.Bounds); AssertBoundsInside(missing.Bounds, key);
            if (width == 360) {
                Assert.Equal(heading.Bounds.Top, missing.Bounds.Top, 6);
                Assert.True(heading.Bounds.Right < missing.Bounds.Left);
                AssertTextInside(prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == heading.Id), heading.Bounds);
            } else Assert.True(missing.Bounds.Top > prepared.Regions.Single(region => region.Id == "map-scale-midpoint").Bounds.Top);
            var swatch = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "map-scale-no-data");
            AssertBoundsInside(swatch.Bounds, key);
            var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
            AssertPixel(image, swatch.Bounds.Left + swatch.Bounds.Width / 2, swatch.Bounds.Top + swatch.Bounds.Height / 2, Missing);
            AssertCaptionPixels(image, missing.Bounds);
            AssertMappedMarks(prepared, document, family, scale);
            if (legendStyle == ChartColorLegendStyle.Gradient) AssertGradientPixels(prepared, Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>()), scale);
            else AssertKeyPaint(prepared, document, family, scale);
            ScatterBubbleColorRenderingTests.Capture(prepared, "missing-heading-" + family + "-" + legendStyle.ToString().ToLowerInvariant()
                + "-" + width + "-" + (dark ? "dark" : "light"));

            // An unshareable heading retains the measured omission; only the optional caption placement may differ.
            chart.Options.MapColorDimension.Name = "A complete temperature heading that cannot share its compact row with the missing caption";
            var unshared = PrepareNamedMapKey(chart, width, dark);
            foreach (var region in prepared.Regions.Where(region => region.Role == family + "-region"
                || region.Id is "map-scale-low" or "map-scale-high" or "map-scale-midpoint"))
                Assert.Equal(region.Bounds, unshared.Regions.Single(other => other.Id == region.Id).Bounds);
            Assert.Equal(Swatches(prepared, family).Select(node => node.Bounds), Swatches(unshared, family).Select(node => node.Bounds));
            Assert.Equal(prepared.Scene.Nodes.OfType<VisualSceneGradient>().Select(node => ((VisualSceneRectangle)node.Shape).Bounds),
                unshared.Scene.Nodes.OfType<VisualSceneGradient>().Select(node => ((VisualSceneRectangle)node.Shape).Bounds));
            if (width == 360) {
                Assert.DoesNotContain(unshared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Id == caption.Id);
                Assert.Equal("Not sampled", unshared.Regions.Single(region => region.Id == caption.Id).Label);
                Assert.Contains(unshared.Diagnostics, diagnostic => diagnostic.Code == "radial.text-overflow");
                Assert.Contains("data-cfx-description=\"Not sampled\"", unshared.ToSvg());
                if (dark) ScatterBubbleColorRenderingTests.Capture(unshared, "missing-heading-unshared-" + family + "-" + legendStyle.ToString().ToLowerInvariant());
            } else {
                Assert.Equal(missing.Bounds, unshared.Regions.Single(region => region.Id == caption.Id).Bounds);
                Assert.Equal(swatch.Bounds, unshared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == swatch.Role).Bounds);
            }
        }
    }

    private static PreparedVisual PrepareNamedMapKey(Chart chart, int width, bool dark) {
        const string font = "CFX Map Missing Caption Carlito";
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(font, Path.Combine(directory, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(font, Path.Combine(directory, "Carlito-Bold.ttf"), 700);
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, width == 360 ? 360 : 440)),
            VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light,
            new VisualFrame("Temperature by state", "A sampled key keeps the continuous measurement and its color domain", showLegend: null),
            FontSpec.FromFamily(font)));
    }

    private static void AssertTextInside(VisualSceneText text, ChartRect bounds) {
        var line = Assert.Single(text.Text.Lines);
        AssertBoundsInside(new ChartRect(text.LineLeft(line), text.Baseline - text.Text.Ascent, line.Width, text.Text.Metrics.Height), bounds);
    }

    private static void AssertBoundsInside(ChartRect paint, ChartRect bounds) {
        Assert.InRange(paint.Left, bounds.Left - .000001, bounds.Right);
        Assert.InRange(paint.Right, bounds.Left, bounds.Right + .000001);
        Assert.InRange(paint.Top, bounds.Top - .000001, bounds.Bottom);
        Assert.InRange(paint.Bottom, bounds.Top, bounds.Bottom + .000001);
    }

    private static void AssertCaptionPixels(RgbaImage image, ChartRect bounds) {
        Assert.True(Enumerable.Range((int)Math.Ceiling(bounds.Top), Math.Max(0, (int)Math.Floor(bounds.Bottom) - (int)Math.Ceiling(bounds.Top)))
            .Any(y => Enumerable.Range((int)Math.Ceiling(bounds.Left), Math.Max(0, (int)Math.Floor(bounds.Right) - (int)Math.Ceiling(bounds.Left)))
                .Any(x => { var offset = (y * image.Width + x) * 4; return image.Pixels[offset] != image.Pixels[0]
                    || image.Pixels[offset + 1] != image.Pixels[1] || image.Pixels[offset + 2] != image.Pixels[2]; })),
            "The native missing-caption lane must contain painted glyphs.");
    }
}
