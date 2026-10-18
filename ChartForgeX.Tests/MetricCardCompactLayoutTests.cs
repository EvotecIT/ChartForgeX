using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MetricCardCompactLayoutTests {
    [Theory]
    [InlineData(VisualStatus.Positive, true)]
    [InlineData(VisualStatus.None, false)]
    public void ShortMetricKeepsItsLabelValueAndUnitInsideTheFixedContent(VisualStatus status, bool unit) {
        var card = Compact(55).WithMetric("Memory", 68, unit: unit ? "%" : null).WithStatus(status);
        var text = Text(card);
        var label = Bounds(text["metric-label"]);
        var value = Bounds(text["metric-value"]);

        Assert.Equal("Memory", text["metric-label"].Value);
        Assert.Equal("68", text["metric-value"].Value);
        Assert.InRange(Number(text["metric-value"], "font-size"), 18, 26);
        Assert.InRange(label.Top, 6.49, 48.51);
        Assert.True(label.Bottom + 2 <= value.Top + .02, "The required metric rows need separate measured line boxes.");
        Assert.True(value.Bottom <= 48.51, "The entire value line must fit above the explicit bottom padding.");
        if (unit) {
            Assert.Equal("%", text["metric-unit"].Value);
            Assert.True(Bounds(text["metric-unit"]).Bottom <= 48.51);
            Assert.Equal(Number(text["metric-value"], "y"), Number(text["metric-unit"], "y"));
        }
        var image = card.RenderRgba();
        Assert.Equal(281, image.Width);
        Assert.Equal(55, image.Height);
    }

    [Fact]
    public void CompactMetricReservesTheCaptionAndTrendBelowTheRequiredFacts() {
        var card = Compact(70).WithMetric("Memory", 68, unit: "%").WithCaption("of installed memory").WithTrend("+2%");
        var text = Text(card);
        var value = Bounds(text["metric-value"]);
        var caption = Bounds(text["metric-caption"]);
        var trend = Bounds(text["metric-trend"]);

        Assert.Equal("of installed memory", text["metric-caption"].Value);
        Assert.Equal("+2%", text["metric-trend"].Value);
        Assert.True(Bounds(text["metric-label"]).Bottom + 2 <= value.Top + .02);
        Assert.True(value.Bottom + 2 <= Math.Min(caption.Top, trend.Top) + .02);
        Assert.True(Math.Max(caption.Bottom, trend.Bottom) <= 63.51);
    }

    [Fact]
    public void ShortInsetMetricKeepsTheValueAndUnitInsideItsTray() {
        var card = Compact(65).WithMetric("Memory", 68, unit: "%").WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset);
        var document = XDocument.Parse(card.ToSvg());
        var tray = Assert.Single(document.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "metric-value-surface");
        var text = Text(card);
        foreach (var role in new[] { "metric-value", "metric-unit" }) {
            var box = Bounds(text[role]);
            Assert.True(box.Top >= Number(tray, "y") - .01);
            Assert.True(box.Bottom <= Number(tray, "y") + Number(tray, "height") + .01);
        }
        Assert.True(Number(tray, "y") + Number(tray, "height") <= 58.51);
    }

    [Fact]
    public void NormalMetricRetainsItsEstablishedLabelAndValueGeometry() {
        var text = Text(Compact(140).WithMetric("Memory", 68, unit: "%").WithStatus(VisualStatus.Positive));
        Assert.Equal(17.5, Number(text["metric-label"], "y"));
        Assert.Equal(11, Number(text["metric-label"], "font-size"));
        Assert.Equal(62.3, Number(text["metric-value"], "y"));
        Assert.Equal(30.8, Number(text["metric-value"], "font-size"));
        Assert.Equal(62.3, Number(text["metric-unit"], "y"));
        Assert.Equal(13, Number(text["metric-unit"], "font-size"));
    }

    [Theory]
    [InlineData(55)]
    [InlineData(70)]
    public void CompactMetricSvgAndNativePixelsShareTheFittedRows(int height) {
        var card = Compact(height).WithMetric("Memory", 68, unit: "%").WithTransparentBackground();
        if (height == 70) card.WithCaption("of installed memory").WithTrend("+2%");
        var raster = SvgRasterizer.Rasterize(card.ToSvg(), 281, height);
        Assert.Empty(raster.Diagnostics);
        var svgInk = InkRows(raster.Image);
        var nativeInk = InkRows(card.RenderRgba());
        Assert.InRange(nativeInk.Top - svgInk.Top, -2, 2);
        Assert.InRange(nativeInk.Bottom - svgInk.Bottom, -2, 2);
        Assert.True(nativeInk.Bottom < height - 5, "The native image must retain visible bottom padding below its text.");
    }

    [Fact]
    public void CompactMetricFitsAnExplicitlyLargerLabelAlongsideItsValue() {
        var card = Compact(75).WithMetric("Memory", 68, unit: "%")
            .WithTheme(VisualTheme.Report().WithTypography(new VisualTypography(family: "Arial", subtitleSize: 28)));
        var text = Text(card);
        var label = Bounds(text["metric-label"]);
        var value = Bounds(text["metric-value"]);
        Assert.Equal("Memory", text["metric-label"].Value);
        Assert.InRange(Number(text["metric-label"], "font-size"), 14, 28);
        Assert.InRange(label.Top, 6.49, 68.51);
        Assert.True(label.Bottom + 2 <= value.Top + .02);
        Assert.True(value.Bottom <= 68.51);
    }

    private static (int Top, int Bottom) InkRows(RgbaImage image) {
        var rows = new List<int>();
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
                if (image.Pixels[(y * image.Width + x) * 4 + 3] > 32) { rows.Add(y); break; }
        Assert.NotEmpty(rows);
        return (rows.Min(), rows.Max());
    }

    private static MetricCard Compact(int height) => MetricCard.Create().WithSize(281, height).WithPadding(6.5)
        .WithCard(false).WithPngOutputScale(1).WithTheme(VisualTheme.Graphite().WithTypography(new VisualTypography(family: "Arial", subtitleSize: 9)));

    private static Dictionary<string, XElement> Text(MetricCard card) => XDocument.Parse(card.ToSvg()).Descendants()
        .Where(element => element.Name.LocalName == "text" && element.Attribute("data-cfx-role") != null)
        .ToDictionary(element => element.Attribute("data-cfx-role")!.Value);

    private static ChartRect Bounds(XElement text) {
        var style = new TextStyle { Font = new FontSpec {
            Family = text.Attribute("font-family")!.Value, Weight = text.Attribute("font-weight") == null ? 400 : (int)Number(text, "font-weight") },
            FontSize = Number(text, "font-size"), LineHeight = 1 };
        var measured = TextLayoutEngine.Measure(text.Value, style);
        var ascent = TypographyFontResolver.ResolveFace(style.Font).Font?.Ascent(style.FontSize) ?? measured.LineHeight;
        return new ChartRect(Number(text, "x"), Number(text, "y") - ascent, measured.Width, measured.Height);
    }

    private static double Number(XElement element, string attribute) =>
        double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
