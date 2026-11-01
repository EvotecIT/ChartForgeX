using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Public compact numeric keys retain visible color paint as well as captions and missing-data facts.</summary>
public sealed class LegacyColorScaleMissingLayoutTests {
    [Theory]
    [InlineData("compact-color-dimension", false)]
    [InlineData("compact-color-dimension", true)]
    [InlineData("compact-gradient-color-legend", false)]
    [InlineData("compact-gradient-color-legend", true)]
    [InlineData("compact-swatch-color-legend", false)]
    [InlineData("compact-swatch-color-legend", true)]
    public void CompactPublicKeysRetainColorPaintAndCompleteCaptionsWithMissingData(string variant, bool dark) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Heatmap, variant, Mode(dark));
        var prepared = Prepare(chart, variant, dark, 360);
        AssertKey(prepared, variant, missingInHeading: true);
        var document = XDocument.Parse(prepared.ToSvg());
        var cells = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "heatmap-cell").ToArray();
        Assert.Equal(new[] { "-30", "-20", "-3", "0", "2.5", "5", "10", "22.5", "40", "55" },
            cells.Select(node => (string?)node.Attribute("data-cfx-value")));
        Assert.Equal(new[] { "0", "1", "2", "3", "4", "5", "0", "1", "2", "3" },
            cells.Select(node => (string?)node.Attribute("data-cfx-source-point")));
        Assert.Equal("0,0 °C", (string?)cells[3].Attribute("data-cfx-formatted-value"));
        ScatterBubbleColorRenderingTests.Capture(prepared, "legacy-key-" + variant + "-" + (dark ? "dark" : "light"));
    }

    [Fact]
    public void LongHeadingKeepsItsFullMeaningBesideACompleteLocalizedMissingEntry() {
        const string heading = "Temperature measured throughout the regional processing interval";
        const string missing = "Keine Messung vorhanden";
        const string variant = "compact-gradient-color-legend";
        var chart = V2GalleryModels.Create(ChartSeriesKind.Heatmap, variant)
            .ConfigureHeatmapColorDimension(options => options.Name = heading).ConfigureLabels(labels => labels.NoData = missing);
        var prepared = Prepare(chart, variant, false, 360);
        AssertKey(prepared, variant, missingInHeading: true, heading: heading, missing: missing);
        var title = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Role == "heatmap-scale-title");
        Assert.NotEqual(heading, title.Text.Lines[0].Text);
        Assert.EndsWith("…", title.Text.Lines[0].Text);
        Assert.Equal(heading, prepared.Regions.Single(region => region.Role == "heatmap-scale-title").Label);
        ScatterBubbleColorRenderingTests.Capture(prepared, "legacy-key-long-heading-localized-missing");
    }

    [Theory]
    [InlineData("color-dimension", false)]
    [InlineData("gradient-color-legend", true)]
    public void WideKeysKeepTheirSeparateMissingRowAndCompleteHeading(string variant, bool dark) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Heatmap, variant, Mode(dark));
        var prepared = Prepare(chart, variant, dark, 800);
        AssertKey(prepared, variant, missingInHeading: false);
        ScatterBubbleColorRenderingTests.Capture(prepared, "legacy-key-" + variant + "-wide-" + (dark ? "dark" : "light"));
    }

    [Fact]
    public void CompleteLocalizedMissingEntryKeepsASeparateRowWhenVerticalSpaceIsAvailable() {
        const string missing = "Keine Messung für diese Region";
        var chart = V2GalleryModels.Create(ChartSeriesKind.HexbinHeatmap, "compact-color-scale")
            .ConfigureLabels(labels => labels.NoData = missing);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 500)),
            VisualTheme.Graphite(), VisualThemeMode.Light,
            new VisualFrame("Service coverage in hexagonal cells", "Named numeric intervals keep their exact boundaries"), GalleryFont()));
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "heatmap-scale-label").ToArray();
        var bands = labels.Where(node => node.Id!.StartsWith("heatmap-scale-label-band-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(new[] { "Low < 40", "Medium 40–80", "High ≥ 80" }, bands.Select(node => node.Text.Lines[0].Text));
        var noData = Assert.Single(labels, node => node.Id == "heatmap-scale-label-missing");
        Assert.Equal(missing, noData.Text.Lines[0].Text);
        Assert.True(Ink(noData).Top > bands.Max(node => Ink(node).Bottom));
        var key = prepared.Regions.Single(region => region.Role == "heatmap-scale").Bounds;
        Assert.All(labels, label => { Assert.True(label.Text.Size >= 10); AssertInside(Ink(label), key); });
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "heatmap-scale-swatch").ToArray();
        Assert.Equal(3, swatches.Length);
        Assert.True(noData.Text.Metrics.Width + noData.Text.Metrics.Height > key.Width / 2,
            "This complete translation is wider than a hypothetical two-column cell; the available separate row must retain it.");
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.All(swatches, swatch => {
            Assert.Equal(12, swatch.Bounds.Height, 6); AssertInside(swatch.Bounds, key);
            Assert.Equal(swatch.Fill!.Value, Pixel(image, swatch.Bounds.Left + swatch.Bounds.Width / 2, swatch.Bounds.Top + 6));
        });
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "matrix.scale-overflow" or "text.overflow");
        ScatterBubbleColorRenderingTests.Capture(prepared, "legacy-key-localized-missing-separate-row");
    }

    private static FontSpec GalleryFont() {
        const string family = "CFX Proof Carlito";
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(directory, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(family, Path.Combine(directory, "Carlito-Bold.ttf"), 700);
        return FontSpec.FromFamily(family);
    }

    private static PreparedVisual Prepare(Chart chart, string variant, bool dark, int width) {
        var subtitle = variant.Contains("gradient", StringComparison.Ordinal) ? "A continuous key retains every authored color anchor"
            : variant.Contains("swatch", StringComparison.Ordinal) ? "A sampled key keeps the continuous measurement and its color domain"
            : "Temperature captions use their own format and culture";
        return chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, width == 360 ? 360 : 440)),
            VisualTheme.Graphite(), Mode(dark), new VisualFrame("Temperature by grid position", subtitle), GalleryFont()));
    }

    private static VisualThemeMode Mode(bool dark) => dark ? VisualThemeMode.Dark : VisualThemeMode.Light;

    private static void AssertKey(PreparedVisual prepared, string variant, bool missingInHeading,
        string heading = "Temperature", string missing = "Not sampled") {
        var key = prepared.Regions.Single(region => region.Role == "heatmap-scale").Bounds;
        var text = prepared.Scene.Nodes.OfType<VisualSceneText>()
            .Where(node => node.Role is "heatmap-scale-title" or "heatmap-scale-label").ToArray();
        var labels = text.Where(node => node.Role == "heatmap-scale-label").ToArray();
        Assert.Equal(new[] { "Cold · -20,0 °C", "Freezing · 0,0 °C", "Warm · 40,0 °C", missing }.OrderBy(value => value),
            labels.Select(node => node.Text.Lines[0].Text).OrderBy(value => value));
        Assert.Equal(heading, prepared.Regions.Single(region => region.Role == "heatmap-scale-title").Label);
        var title = Assert.Single(text, node => node.Role == "heatmap-scale-title");
        var noData = Assert.Single(labels, node => node.Id == "heatmap-scale-label-missing");
        if (missingInHeading) {
            Assert.Equal(title.Baseline, noData.Baseline, 6);
            Assert.True(Ink(title).Right + 1 < Ink(noData).Left);
        } else {
            Assert.Equal(heading, title.Text.Lines[0].Text);
            Assert.True(Ink(noData).Top > labels.Where(node => node != noData).Max(node => Ink(node).Bottom));
        }
        var ink = text.Select(Ink).ToArray();
        for (var index = 0; index < text.Length; index++) {
            Assert.True(text[index].Text.Size >= 10);
            Assert.InRange(ink[index].Left, key.Left - .01, key.Right);
            Assert.InRange(ink[index].Right, key.Left, key.Right + .01);
            Assert.InRange(ink[index].Top, key.Top - .01, key.Bottom);
            Assert.InRange(ink[index].Bottom, key.Top, key.Bottom + .01);
            for (var other = 0; other < index; other++) Assert.False(Intersects(ink[index], ink[other]), "Numeric key captions must not overlap.");
        }
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        if (variant.Contains("gradient", StringComparison.Ordinal)) {
            var gradient = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
            var strip = Assert.IsType<VisualSceneRectangle>(gradient.Shape).Bounds;
            Assert.Equal(12, strip.Height, 6); AssertInside(strip, key);
            Assert.NotEqual(Pixel(image, strip.Left + 2, strip.Top + strip.Height / 2),
                Pixel(image, strip.Right - 2, strip.Top + strip.Height / 2));
        } else {
            var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "heatmap-scale-swatch").ToArray();
            Assert.Equal(5, swatches.Length);
            Assert.All(swatches, swatch => {
                Assert.Equal(12, swatch.Bounds.Height, 6); AssertInside(swatch.Bounds, key);
                Assert.Equal(swatch.Fill!.Value, Pixel(image, swatch.Bounds.Left + swatch.Bounds.Width / 2, swatch.Bounds.Top + 6));
            });
        }
        var missingSwatch = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "heatmap-scale-missing-swatch");
        Assert.True(missingSwatch.Bounds.Height > 0); AssertInside(missingSwatch.Bounds, key);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "matrix.scale-overflow");
    }

    private static ChartRect Ink(VisualSceneText node) => new(node.LineLeft(node.Text.Lines[0]), node.Baseline - node.Text.Ascent,
        node.Text.Metrics.Width, node.Text.Metrics.Height);

    private static bool Intersects(ChartRect left, ChartRect right) => left.Left < right.Right && left.Right > right.Left
        && left.Top < right.Bottom && left.Bottom > right.Top;

    private static void AssertInside(ChartRect box, ChartRect bounds) {
        Assert.InRange(box.Left, bounds.Left - .01, bounds.Right); Assert.InRange(box.Right, bounds.Left, bounds.Right + .01);
        Assert.InRange(box.Top, bounds.Top - .01, bounds.Bottom); Assert.InRange(box.Bottom, bounds.Top, bounds.Bottom + .01);
    }

    private static ChartColor Pixel(RgbaImage image, double x, double y) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }
}
