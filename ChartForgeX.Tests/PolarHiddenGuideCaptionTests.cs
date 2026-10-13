using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PolarHiddenGuideCaptionTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void HiddenFractionalGuideCaptionsMatchTheSelectedValuePositions(bool polar, bool reversed) {
        var chart = Native(polar, reversed);
        var original = Prepare(chart);
        chart.Options.PolarGridRingCount = 2.5;
        var fractional = Prepare(chart);
        Dictionary<string, (double X, double Y)> Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
            .Where(node => node.Role == (polar ? "polar-radius-label" : "radar-ring-label"))
            .Where(node => node.Text.Lines[0].Text.StartsWith("Count ", StringComparison.Ordinal) || node.Text.Lines[0].Text.StartsWith("Rate ", StringComparison.Ordinal))
            .ToDictionary(node => Assert.Single(node.Text.Lines).Text, node => (node.X, node.Baseline));
        var expected = Captions(original); var actual = Captions(fractional);
        Assert.Equal(4, expected.Count); Assert.Equal(expected.Keys.OrderBy(value => value), actual.Keys.OrderBy(value => value));
        foreach (var pair in expected) {
            Assert.Equal(pair.Value.X, actual[pair.Key].X, 7);
            Assert.Equal(pair.Value.Y, actual[pair.Key].Y, 7);
        }
        Assert.Equal(original.Regions.Where(region => region.Role.EndsWith("-point", StringComparison.Ordinal)).Select(region => region.Bounds),
            fractional.Regions.Where(region => region.Role.EndsWith("-point", StringComparison.Ordinal)).Select(region => region.Bounds));
    }

    [BrowserTheory]
    [InlineData(320, false)]
    [InlineData(960, true)]
    public async Task HiddenFractionalCaptionsRemainVisibleOnBothRadiusAxes(int width, bool reversed) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Native(false, reversed).WithSize(width < 400 ? 320 : 600, 440);
        chart.Options.PolarGridRingCount = 2.5;
        await using var session = await InteractiveChartBrowser.OpenAsync(chart.ToHtmlPage(), width, 620);
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role=radar-ring-label]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("[data-cfx-role=radar-ring]").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "radar-hidden-fractional-captions-" + width;
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
            await File.WriteAllBytesAsync(Path.Combine(captures, name + "-native.png"), chart.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Chart Native(bool polar, bool reversed) {
        var chart = Chart.Create().WithSize(600, 440).WithHeader(false).WithLegend(false).WithGrid(false).WithDataLabels(false)
            .WithYAxisBounds(0, 100).WithSecondaryYAxisBounds(0, 10);
        var values = new[] { new ChartPoint(1, 17), new ChartPoint(2, 31), new ChartPoint(3, 73) };
        var rates = values.Select(point => new ChartPoint(point.X, point.Y / 10)).ToArray();
        if (polar) chart.AddPolar("Counts", values).AddPolar("Rate", rates);
        else chart.AddRadarLine("Counts", values).AddRadarLine("Rate", rates);
        chart.Series[1].UseSecondaryYAxis(); chart.Options.XAxis.Visible = false;
        chart.Options.YAxis.Reversed = reversed; chart.Options.SecondaryYAxis.Reversed = reversed;
        foreach (var value in reversed ? new[] { 20d, 60d } : new[] { 40d, 80d }) {
            chart.Options.YAxis.Labels.Add(new ChartAxisLabel(value, "Count " + value));
            chart.Options.SecondaryYAxis.Labels.Add(new ChartAxisLabel(value / 10, "Rate " + value / 10));
        }
        return chart;
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
