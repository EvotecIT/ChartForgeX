using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects configured convenience exports used by wallpaper, image and report consumers.</summary>
public sealed class V2ExportRequestTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompactConvenienceExportsMeasureAxesInsideTheSameDefaultFrame(bool graphite) {
        var chart = Chart.Create().WithSize(240, 160)
            .WithTheme(graphite ? ChartTheme.GraphiteLight() : new ChartTheme())
            .AddBar("Capacity", new[] { new ChartPoint(1, 2), new ChartPoint(2, 5), new ChartPoint(3, 3) });
        chart.Options.ShowHeader = false; chart.Options.ShowLegend = false;
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Equal(3, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "point"));
        Assert.Equal(ChartPadding.All(24), chart.Options.Padding);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "axis-x-label" or "axis-y-label").ToArray();
        Assert.NotEmpty(labels);
        Assert.All(labels, label => {
            var top = label.Baseline - label.Text.Ascent;
            Assert.InRange(top, chart.Options.Padding.Top - .001, chart.Options.Size.Height - chart.Options.Padding.Bottom);
            Assert.True(top + label.Text.Metrics.Height <= chart.Options.Size.Height - chart.Options.Padding.Bottom + .001);
            Assert.All(label.Text.Lines, line => {
                Assert.InRange(label.LineLeft(line), chart.Options.Padding.Left - .001, chart.Options.Size.Width - chart.Options.Padding.Right);
                Assert.True(label.LineLeft(line) + line.Width <= chart.Options.Size.Width - chart.Options.Padding.Right + .001);
            });
        });
        var pixels = chart.ToRgbaImage();
        Assert.Equal(240, pixels.Width); Assert.Equal(160, pixels.Height);
    }

    [Fact]
    public void WallpaperExportPreservesTransparencyDensityThemeAndHiddenHeadings() {
        var chart = Chart.Create().WithTitle("Host-owned metric").AddBar("Capacity", new[] {
            new ChartPoint(1, 2), new ChartPoint(2, 5), new ChartPoint(3, 3)
        });
        chart.Options.Size = new ChartSize(240, 150);
        chart.Options.Padding = new ChartPadding(9, 11, 15, 17);
        chart.Options.ShowHeader = false; chart.Options.ShowLegend = false;
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false;
        chart.Options.TransparentBackground = true; chart.Options.PngOutputScale = 2;
        chart.Options.Theme.Palette = new[] { ChartColor.FromHex("#306090") };
        var pixels = chart.ToRgbaImage();
        Assert.Equal(480, pixels.Width); Assert.Equal(300, pixels.Height);
        Assert.Equal(0, pixels.Pixels[3]);
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Equal("Host-owned metric", (string?)svg.Root!.Attribute("aria-label"));
        Assert.DoesNotContain(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "frame-heading");
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("fill") == "#306090");
        Assert.Equal(new ChartPadding(9, 11, 15, 17), chart.Options.Padding);
        Assert.True(chart.Options.TransparentBackground);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void ConvenienceGraphiteThemesUseTheCanonicalPairedColorOwner(VisualThemeMode mode) {
        var expected = VisualTheme.Graphite().Resolve(mode);
        var actual = mode == VisualThemeMode.Light ? ChartTheme.GraphiteLight() : ChartTheme.GraphiteDark();
        Assert.Equal(expected.Palette, actual.Palette);
        Assert.Equal(expected.Foreground, actual.Text);
        Assert.Equal(expected.Surface, actual.PlotBackground);
        Assert.Equal(expected.Status.Pass.Fill, actual.Positive);
        Assert.Equal(VisualTheme.Graphite().Typography.TitleSize, actual.TitleFontSize);
    }

    [Fact]
    public void RasterHintingOverrideRetainsThePreparedSceneAndLogicalViewport() {
        var builder = new VisualSceneBuilder(new VisualSize(160, 50), new FontSpec());
        builder.Text("Small text", 5, 25, 11, ChartColor.Black);
        var prepared = new PreparedVisual(builder.Build());
        var before = prepared.ToSvg();
        var pixels = prepared.ToRgba(new VisualRenderOptions(scale: 2, textHinting: TextHinting.None));
        Assert.Equal(320, pixels.Width); Assert.Equal(100, pixels.Height);
        Assert.Equal(before, prepared.ToSvg());
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualRenderOptions(textHinting: (TextHinting)999));
    }
}
