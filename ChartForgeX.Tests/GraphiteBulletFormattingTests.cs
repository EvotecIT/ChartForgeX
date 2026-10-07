using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteBulletFormattingTests {
    [Theory]
    [InlineData(false, 60, 1)]
    [InlineData(true, 60, 1)]
    [InlineData(false, 60, 3)]
    [InlineData(true, 60, 3)]
    [InlineData(false, 80, 1)]
    [InlineData(true, 80, 1)]
    public void CompactBulletRowsKeepPositiveMarksInSvgAndPng(bool dark, int height, int rowCount) {
        var chart = Chart.Create().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithSize(200, height).WithPngOutputScale(1);
        for (var row = 0; row < rowCount; row++) chart.AddBullet("Count " + row, 40 + row, 70);
        var nodes = XDocument.Parse(chart.ToSvg()).Descendants().ToArray();
        Assert.Equal(rowCount, Roles(nodes, "bullet-row").Length);
        foreach (var rectangle in nodes.Where(element => element.Name.LocalName == "rect")) {
            Assert.True((double)rectangle.Attribute("width")! >= 0, "SVG rectangle widths must be nonnegative.");
            Assert.True((double)rectangle.Attribute("height")! >= 0, "SVG rectangle heights must be nonnegative.");
        }
        var ranges = Roles(nodes, "bullet-range");
        Assert.NotEmpty(ranges);
        Assert.All(ranges.Concat(Roles(nodes, "bullet-value")), mark => Assert.True((double)mark.Attribute("height")! > 0));
        Assert.All(Roles(nodes, "bullet-target"), mark => Assert.True((double)mark.Attribute("y2")! > (double)mark.Attribute("y1")!));
        var image = PngReader.Decode(chart.ToPng());
        Assert.Equal(200, image.Width);
        Assert.Equal(height, image.Height);
        // The same SVG mark scene drives Graphite raster output. Sample the first range's row
        // against its adjacent card surface so a valid PNG containing only the frame cannot pass.
        var first = ranges[0];
        var x = (int)Math.Ceiling((double)first.Attribute("x")! + (double)first.Attribute("width")! / 2);
        var y = (int)Math.Floor((double)first.Attribute("y")!);
        var outsideX = Math.Max(0, (int)Math.Floor((double)first.Attribute("x")!) - 2);
        var ink = image.Pixels.AsSpan((y * image.Width + x) * 4, 4);
        var surface = image.Pixels.AsSpan((y * image.Width + outsideX) * 4, 4);
        Assert.False(ink.SequenceEqual(surface), "The compact range must remain painted in PNG.");
    }

    [Theory]
    [InlineData(false, 556)]
    [InlineData(true, 556)]
    [InlineData(false, 380)]
    [InlineData(true, 380)]
    public void PercentValuesTargetsAndAxisRetainTheirUnitsWithoutColliding(bool dark, int width) {
        var chart = Chart.Create().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithSize(width, 294).WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + " %")
            .AddBullet("DMARC", 88, 95).AddBullet("DNSSEC", 74, 90);
        var nodes = XDocument.Parse(chart.ToSvg()).Descendants().ToArray();
        var values = Roles(nodes, "bullet-value-label");
        var targets = Roles(nodes, "bullet-target-label");
        Assert.Equal(new[] { "88 %", "74 %" }, values.Select(e => e.Value));
        Assert.Equal(new[] { "of 95 %", "of 90 %" }, targets.Select(e => e.Value));
        Assert.Equal(new[] { "0 %", "25 %", "50 %", "75 %", "100 %" }, Roles(nodes, "bullet-axis-label").Select(e => e.Value));
        for (var i = 0; i < values.Length; i++) {
            Assert.Equal(12.5, (double)values[i].Attribute("font-size")!);
            Assert.Equal(12, (double)targets[i].Attribute("font-size")!);
            Assert.Equal(chart.Options.Theme.MutedText.ToCss(), (string?)targets[i].Attribute("fill"));
            Assert.Equal("placed", (string?)values[i].Attribute("data-cfx-label-status"));
            Assert.Equal("placed", (string?)targets[i].Attribute("data-cfx-label-status"));
            Assert.True((double)values[i].Attribute("data-cfx-label-x")! + (double)values[i].Attribute("data-cfx-label-width")! <= (double)targets[i].Attribute("data-cfx-label-x")!);
        }
        Assert.NotEmpty(chart.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlainTargetsUseOfAndAxisKeepsCompactTicks(bool dark) {
        var chart = Chart.Create().WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithSize(640, 300).AddBullet("Count", 1284, 1900, max: 2000);
        var nodes = XDocument.Parse(chart.ToSvg()).Descendants().ToArray();
        Assert.Equal("1,284", Assert.Single(Roles(nodes, "bullet-value-label")).Value);
        Assert.Equal("of 1,900", Assert.Single(Roles(nodes, "bullet-target-label")).Value);
        Assert.Equal(new[] { "0", "500", "1k", "1.5k", "2k" }, Roles(nodes, "bullet-axis-label").Select(e => e.Value));
    }

    [Fact]
    public void CustomCultureAndMeasureFormatReachAllBulletNumbers() {
        var chart = Chart.Create().WithSize(900, 400)
            .WithValueFormatter(value => value.ToString("N1", CultureInfo.GetCultureInfo("de-DE")) + " ms")
            .AddBullet("Latency", 1284, 1900, max: 2000);
        var nodes = XDocument.Parse(chart.ToSvg()).Descendants().ToArray();
        Assert.Equal("1.284,0 ms", Assert.Single(Roles(nodes, "bullet-value-label")).Value);
        Assert.Equal("of 1.900,0 ms", Assert.Single(Roles(nodes, "bullet-target-label")).Value);
        Assert.Equal(new[] { "0,0 ms", "500,0 ms", "1.000,0 ms", "1.500,0 ms", "2.000,0 ms" }, Roles(nodes, "bullet-axis-label").Select(e => e.Value));
    }

    private static XElement[] Roles(IEnumerable<XElement> nodes, string role) => nodes.Where(e => (string?)e.Attribute("data-cfx-role") == role).ToArray();
}
