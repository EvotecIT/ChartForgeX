using ChartForgeX.Core;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Checks compact static fragments without the complete document's stylesheet.</summary>
public sealed class StaticFragmentBrowserTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChartAndGridFragmentsScaleInsideCompactHostsWithoutPageCss(bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Capacity")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .AddLine("Used", new[] { new ChartPoint(0, 2), new ChartPoint(1, 5), new ChartPoint(2, 3) });
        var grid = ChartGrid.Create().WithColumns(2).WithPanelSize(320, 200).Add(chart).Add(chart);
        var html = "<!doctype html><html><body style=\"margin:0;background:" + chart.Options.Theme.Background.ToCss() + "\">"
            + "<div id=\"chart-host\" style=\"width:280px\">" + chart.ToHtmlFragment("compact-chart") + "</div>"
            + "<div id=\"grid-host\" style=\"width:280px\">" + grid.ToHtmlFragment("compact-grid") + "</div></body></html>";
        await using var session = await OpenAsync(html, 320, 700);
        foreach (var host in new[] { "chart-host", "grid-host" }) {
            var dimensions = await session.Page.Locator("#" + host + " svg").EvaluateAsync<double[]>(
                "svg => { const b = svg.getBoundingClientRect(); const v = svg.viewBox.baseVal; return [b.width,b.height,v.width,v.height]; }");
            Assert.InRange(dimensions[0], 279.9, 280.1);
            Assert.InRange(Math.Abs(dimensions[1] / dimensions[0] - dimensions[3] / dimensions[2]), 0, .001);
        }
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(directory, dark ? "static-fragments-compact-dark.png" : "static-fragments-compact-light.png"), FullPage = true
            });
        }
        AssertNoConsoleErrors(session);
    }
}
