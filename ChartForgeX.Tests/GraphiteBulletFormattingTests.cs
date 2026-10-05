using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteBulletFormattingTests {
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
