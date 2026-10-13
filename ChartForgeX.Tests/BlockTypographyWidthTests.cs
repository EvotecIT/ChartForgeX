using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BlockTypographyWidthTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task LongHeadingsRemainVisibleWithinTheContentWidth(bool graphite, bool subtitle) {
        var theme = Theme(graphite); theme.TitleFontSize = 25; theme.SubtitleFontSize = 25;
        var packet = Packet(theme).AddField(0, 31, "Header");
        if (subtitle) packet.WithSubtitle(new string('W', 80));
        else packet.WithTitle(new string('W', 80));
        var role = subtitle ? "visual-subtitle" : "visual-title";
        var heading = Assert.Single(XDocument.Parse(packet.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == role);
        Assert.NotEqual("none", (string?)heading.Attribute("display"));
        Assert.EndsWith("...", heading.Value);
        Assert.InRange(Measure(heading), 1, 484);
        await Capture(packet, "packet-long-" + (subtitle ? "subtitle-" : "title-") + (graphite ? "graphite" : "classic"));
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, 520)]
    [InlineData(true, 520)]
    [InlineData(false, 320)]
    [InlineData(true, 320)]
    public async Task LargeBitNumbersFitNarrowFieldsWithoutDroppingTheirValues(bool graphite, int width) {
        var theme = Theme(graphite); theme.DataLabelFontSize = 256;
        var packet = Packet(theme).WithSize(width, 300).AddField(0, 0, "FlagA").AddField(1, 1, "FlagB").AddField(2, 31, "Data");
        var elements = XDocument.Parse(packet.ToSvg()).Descendants().ToArray();
        var fields = elements.Where(element => (string?)element.Attribute("data-cfx-role") == "packet-field").ToArray();
        var labels = elements.Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("packet-bit-", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(new[] { "0", "1", "2", "31" }, labels.Select(label => label.Value));
        foreach (var label in labels) {
            Assert.NotEqual("none", (string?)label.Attribute("display"));
            var field = fields.Single(field => label.Value == (string?)field.Attribute(label.Attribute("text-anchor")?.Value == "end" ? "data-bit-end" : "data-bit-start"));
            var available = Number(field, "width") / (field.Attribute("data-bit-start")!.Value == field.Attribute("data-bit-end")!.Value ? 1 : 2);
            Assert.InRange(Measure(label), .01, available);
        }
        await Capture(packet, "packet-narrow-bits-" + (graphite ? "graphite-" : "classic-") + width);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdjacentFieldBoundaryBitNumbersKeepBothValuesVisible(bool graphite) {
        var theme = Theme(graphite); theme.DataLabelFontSize = 21;
        var packet = Packet(theme).AddField(0, 15, "Source Port").AddField(16, 31, "Destination Port");
        var labels = XDocument.Parse(packet.ToSvg()).Descendants().Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("packet-bit-", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(new[] { "0", "15", "16", "31" }, labels.Select(label => label.Value));
        Assert.All(labels, label => Assert.NotEqual("none", (string?)label.Attribute("display")));
        var previous = labels.Single(label => label.Value == "15"); var next = labels.Single(label => label.Value == "16");
        Assert.True(Number(next, "x") - Number(previous, "x") >= 2, "Adjacent end/start bit labels need the shared scene's collision separation.");
        await Capture(packet, "packet-adjacent-bits-" + (graphite ? "graphite" : "classic"));
    }

    [Trait("Category", "Browser")]
    [Fact]
    public async Task ClassicPacketFieldTextSharesTheSvgBaseline() {
        var theme = Theme(false); theme.DataLabelFontSize = 256;
        theme.Background = theme.CardBackground = theme.PlotBackground = ChartColor.Transparent;
        theme.Text = ChartColor.FromHex("#FF0000"); theme.MutedText = ChartColor.Transparent;
        theme.Palette = new[] { ChartColor.Transparent };
        var packet = Packet(theme).WithTransparentBackground().AddField(0, 31, "MMMM");
        var svg = packet.ToSvg();
        var raster = SvgRasterizer.Rasterize(svg, 520, 300);
        Assert.Empty(raster.Diagnostics);
        var expected = InkRows(raster.Image); var actual = InkRows(packet.RenderRgba());
        Assert.InRange(actual.Min - expected.Min, -2, 2);
        Assert.InRange(actual.Max - expected.Max, -2, 2);
        await Capture(packet, "packet-classic-field-baseline");
    }

    private static ChartTheme Theme(bool graphite) {
        var theme = ChartTheme.Light(); theme.FontFamily = "Arial"; theme.UseGraphiteLayout = graphite;
        return theme;
    }

    private static PacketLayoutBlock Packet(ChartTheme theme) => PacketLayoutBlock.Create().WithSize(520, 300).WithPadding(18, 16, 18, 12).WithCard(false).WithTheme(theme);

    private static double Measure(XElement text) => new LabelPlacementService().Measure(text.Value, new TextStyle {
        Font = new FontSpec { Family = "Arial", Weight = text.Attribute("font-weight") == null ? 400 : (int)Number(text, "font-weight") },
        FontSize = Number(text, "font-size"), LineHeight = 1
    }).Width;

    private static (int Min, int Max) InkRows(RgbaImage image) {
        var rows = new List<int>();
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++) {
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset + 3] > 64 && image.Pixels[offset] > 200 && image.Pixels[offset + 1] < 20) { rows.Add(y); break; }
            }
        Assert.NotEmpty(rows);
        return (rows.Min(), rows.Max());
    }

    private static async Task Capture(PacketLayoutBlock packet, string name) {
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(packet.ToHtmlPage(), 560, 340);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), packet.ToPng());
    }

    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
}
