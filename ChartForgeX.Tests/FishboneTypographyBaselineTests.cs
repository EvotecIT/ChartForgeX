using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FishboneTypographyBaselineTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(10.5)]
    [InlineData(21)]
    public async Task ClassicRasterChildLabelsShareTheSvgBaselineAtDifferentBodySizes(double size) {
        var theme = VisualTheme.Report();
        theme = theme.WithFontFamily("Arial");
        theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: size, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        theme = theme.Configure(builder => { builder.Light.Background = builder.Dark.Background = builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = builder.Light.Surface = builder.Dark.Surface = ChartColor.Transparent; });
        theme = theme.Configure(builder => { builder.Light.Foreground = builder.Dark.Foreground = ChartColor.Transparent; builder.Light.Axis = builder.Dark.Axis = builder.Light.Grid = builder.Dark.Grid = builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.Transparent; });
        theme = theme.Configure(builder => { builder.Light.Muted = builder.Dark.Muted = ChartColor.FromHex("#FF0000"); });
        theme = theme.Configure(builder => { builder.Light.Palette = builder.Dark.Palette = new[] { ChartColor.Transparent }; });
        var block = FishboneDiagramBlock.Create("Effect").WithSize(800, 400).WithTheme(theme).WithCard(false).WithTransparentBackground();
        block.AddCause("Cause").AddChild("MMMM");
        var native = block.RenderRgba();
        var raster = SvgRasterizer.Rasterize(block.ToSvg(), 800, 400);
        Assert.Empty(raster.Diagnostics);
        var actual = InkRows(native);
        var expected = InkRows(raster.Image);
        Assert.InRange(actual.Min - expected.Min, -2, 2);
        Assert.InRange(actual.Max - expected.Max, -2, 2);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + block.ToSvg() + "</body></html>", 800, 400);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "fishbone-classic-" + size.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), PngWriter.WriteRgba(native));
    }

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
}
