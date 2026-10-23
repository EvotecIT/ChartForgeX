using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadarCurveCompositionTests {
    [Fact]
    public void StandaloneCurveFittingUsesTheSelectedSecondaryDomain() {
        var chart = Chart.Create().WithSize(320, 240).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false)
            .WithYAxisBounds(0, 100).WithSecondaryYAxisBounds(0, 10).AddRadarLine("Target", Values(100));
        chart.Series[0].Radar.CurveTension = 1;
        var primary = Prepare(chart);
        chart.Series[0].UseSecondaryValueAxis();
        for (var index = 0; index < chart.Series[0].Points.Count; index++) chart.Series[0].Points[index] = new ChartPoint(index + 1, 10);
        var secondary = Prepare(chart);
        Assert.Equal(primary.Regions.Select(region => region.Bounds), secondary.Regions.Select(region => region.Bounds));
        Assert.Equal(primary.ToPng(), secondary.ToPng());
    }

    [Theory]
    [InlineData(ChartLineAreaForm.Line, 0)]
    [InlineData(ChartLineAreaForm.Line, .3)]
    [InlineData(ChartLineAreaForm.Area, 0)]
    [InlineData(ChartLineAreaForm.Area, .3)]
    public void MixedCurvesFitWithColumnsAndRetainTheAnnularHole(ChartLineAreaForm form, double innerRatio) {
        var chart = Mixed(form, innerRatio);
        var straight = Prepare(chart);
        chart.Series[1].Radar.CurveTension = 0;
        Assert.Equal(straight.ToPng(), Prepare(chart).ToPng());
        chart.Series[1].Radar.CurveTension = 1;
        var prepared = Prepare(chart);
        var outline = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-outline");
        Assert.Contains(" C", prepared.ToSvg());
        Assert.NotEqual(straight.ToPng(), prepared.ToPng());
        var plot = prepared.Scene.Nodes.OfType<VisualSceneGroup>().First(group => group.Clip.HasValue).Clip!.Value;
        var bounds = new ChartPath(outline.Commands).Bounds();
        Assert.InRange(bounds.Left, plot.Left + outline.StrokeWidth / 2, plot.Right);
        Assert.InRange(bounds.Right, plot.Left, plot.Right - outline.StrokeWidth / 2);
        Assert.InRange(bounds.Top, plot.Top + outline.StrokeWidth / 2, plot.Bottom);
        Assert.InRange(bounds.Bottom, plot.Top, plot.Bottom - outline.StrokeWidth / 2);
        var column = prepared.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role == "radial-column");
        var point = prepared.Regions.Single(region => region.Id == "series-1-point-0").Bounds;
        var angle = column.Start + column.Sweep / 2;
        Assert.Equal(column.Cx + Math.Cos(angle) * column.Outer, point.Left + point.Width / 2, 7);
        Assert.Equal(column.Cy + Math.Sin(angle) * column.Outer, point.Top + point.Height / 2, 7);
        Assert.Equal(innerRatio, column.Inner / column.Outer, 7);
        Assert.All(chart.Series[0].Points, point => Assert.Equal(100, point.Y));
        Assert.All(chart.Series[1].Points, point => Assert.Equal(10, point.Y));
        if (innerRatio > 0) {
            var image = RasterImageDecoder.Decode(prepared.ToPng());
            var offset = ((int)column.Cy * image.Width + (int)column.Cx) * 4;
            Assert.Equal(0, image.Pixels[offset + 3]);
        }
    }

    [BrowserTheory]
    [InlineData(320, false)]
    [InlineData(960, true)]
    public async Task MixedCurvesRemainVisibleInCompactAndWideStaticHosts(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = V2GalleryModels.CreateMixedRadial(curveTension: 1).WithSize(width < 400 ? 360 : 800, width < 400 ? 360 : 440)
            .WithHeader(true).WithTitle("Regional counts and targets")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        await using var session = await InteractiveChartBrowser.OpenAsync(chart.ToHtmlPage(), width, 620);
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role=radial-column]").CountAsync());
        var outlines = await session.Page.Locator("[data-cfx-role=radar-outline]").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.getAttribute('d'))");
        Assert.Equal(2, outlines.Length); Assert.All(outlines, path => Assert.Contains("C", path));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "radar-curves-mixed-" + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
            await File.WriteAllBytesAsync(Path.Combine(captures, name + "-native.png"), chart.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Chart Mixed(ChartLineAreaForm form, double innerRatio) {
        var chart = Chart.Create().WithSize(320, 240).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false)
            .WithYAxisBounds(0, 100).WithSecondaryYAxisBounds(0, 10).WithRadialGeometry(new ChartRadialGeometryOptions(innerRadiusRatio: innerRatio))
            .AddRadialColumn("Counts", Values(100)).AddRadar("Target", Values(10));
        chart.Series[1].UseSecondaryValueAxis(); chart.Series[1].Radar.Form = form;
        chart.Options.TransparentBackground = true; chart.Options.ShowPlotBackground = false; chart.Options.ShowCard = false;
        return chart;
    }

    private static ChartPoint[] Values(double value) => Enumerable.Range(1, 4).Select(index => new ChartPoint(index, value)).ToArray();
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
