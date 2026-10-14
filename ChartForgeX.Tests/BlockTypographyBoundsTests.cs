using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BlockTypographyBoundsTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task LargestAcceptedPacketTypographyKeepsBothRowsAndLabelsVisible(bool headings, bool dark, bool card) {
        var result = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\npacket-beta\n0-31: \"Header\"\n32-63: \"Payload\"");
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var packet = Assert.IsType<PacketLayoutBlock>(artifact.Model);
        packet.Options.ShowCard = card;
        packet.Options.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        if (!headings) packet.WithTitle("").WithSubtitle("");
        var svg = XDocument.Parse(packet.ToSvg());
        var fields = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "packet-field").ToArray();
        Assert.Equal(2, fields.Length);
        foreach (var field in fields) {
            var y = Number(field, "y"); var height = Number(field, "height");
            Assert.InRange(y, packet.Options.Padding.Top, packet.Options.Size.Height - packet.Options.Padding.Bottom - height);
            Assert.True(height > 0);
        }
        var text = svg.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        Assert.Contains("Header", text); Assert.Contains("Payload", text);
        foreach (var number in new[] { "0", "31", "32", "63" }) Assert.Contains(number, text);
        if (headings) {
            var firstField = fields.Min(field => Number(field, "y"));
            foreach (var heading in svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") is "visual-title" or "visual-subtitle")) {
                Assert.True(heading.Attribute("data-cfx-label-y") != null, heading.ToString());
                Assert.True(Number(heading, "data-cfx-label-y") + Number(heading, "data-cfx-label-height") < firstField, "The heading must stay above the first packet row after measured scene placement.");
            }
        }
        var image = packet.RenderRgba();
        foreach (var field in fields) {
            var x = (int)(Number(field, "x") + 10); var y = (int)(Number(field, "y") + Number(field, "height") * .2);
            var offset = (y * image.Width + x) * 4;
            Assert.True(image.Pixels[offset] < 250 || image.Pixels[offset + 1] < 250 || image.Pixels[offset + 2] < 250);
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), 960, 600);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "packet-font-256-" + (headings ? "headings-" : "body-") + (dark ? "dark-" : "light-") + (card ? "card" : "bare");
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), PngWriter.WriteRgba(image));
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(10.5)]
    [InlineData(21)]
    public async Task ClassicWardleyNotesUseTheSameBaselineInSvgAndPng(double size) {
        var theme = VisualTheme.Report().WithFontFamily("Arial"); theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: size, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        theme = theme.Configure(builder => { builder.Light.Background = builder.Dark.Background = builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = builder.Light.Surface = builder.Dark.Surface = ChartColor.Transparent; });
        theme = theme.Configure(builder => { builder.Light.Foreground = builder.Dark.Foreground = ChartColor.Transparent; builder.Light.Axis = builder.Dark.Axis = builder.Light.Grid = builder.Dark.Grid = builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.Transparent; });
        theme = theme.Configure(builder => { builder.Light.Muted = builder.Dark.Muted = ChartColor.FromHex("#FF0000"); }); theme = theme.Configure(builder => { builder.Light.Palette = builder.Dark.Palette = new[] { ChartColor.Transparent }; });
        var map = WardleyMapBlock.Create().WithSize(800, 400).WithTheme(theme).WithCard(false).WithTransparentBackground();
        map.AddNode("n", "Node", .9, .1); map.AddNote("MMMM", .5, .5);
        var svg = map.ToSvg();
        var note = Assert.Single(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName == "text" && element.Value == "MMMM");
        var x = (int)Number(note, "x"); var baseline = (int)Number(note, "y");
        var actual = InkRows(map.RenderRgba(), x, baseline);
        var raster = SvgRasterizer.Rasterize(svg, 800, 400);
        Assert.Empty(raster.Diagnostics);
        var expected = InkRows(raster.Image, x, baseline);
        Assert.InRange(actual.Min - expected.Min, -2, 2);
        Assert.InRange(actual.Max - expected.Max, -2, 2);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(map.ToHtmlPage(), 840, 440);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "wardley-classic-note-" + size.ToString(CultureInfo.InvariantCulture);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), map.ToPng());
    }

    [Theory]
    [InlineData("18PX")]
    [InlineData("18pX")]
    public void CssPixelUnitsAreCaseInsensitive(string size) {
        var rendered = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':'" + size + "'}}}%%\nflowchart LR\nA --> B");
        Assert.Empty(rendered.Diagnostics);
        Assert.NotNull(rendered.Artifact);
        Assert.NotEmpty(rendered.Artifact.ToSvg());
    }

    private static (int Min, int Max) InkRows(RgbaImage image, int left, int baseline) {
        var rows = new List<int>();
        for (var y = Math.Max(0, baseline - 60); y < Math.Min(image.Height, baseline + 60); y++)
            for (var x = left; x < Math.Min(image.Width, left + 150); x++) {
                var offset = (y * image.Width + x) * 4;
                if (image.Pixels[offset + 3] > 64 && image.Pixels[offset] > 200 && image.Pixels[offset + 1] < 20) { rows.Add(y); break; }
            }
        Assert.NotEmpty(rows);
        return (rows.Min(), rows.Max());
    }

    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
}
