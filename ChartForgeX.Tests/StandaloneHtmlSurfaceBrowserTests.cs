using ChartForgeX.Core;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class StandaloneHtmlSurfaceBrowserTests {
    [BrowserTheory]
    [InlineData("chart", false)]
    [InlineData("chart", true)]
    [InlineData("chart-grid", false)]
    [InlineData("chart-grid", true)]
    [InlineData("metric-card", false)]
    [InlineData("metric-card", true)]
    [InlineData("visual-grid", false)]
    [InlineData("visual-grid", true)]
    public async Task ReportPagesPaintTheirResolvedSurfaceAtCompactAndWideWidths(string producer, bool dark) {
        Assert.True(Enabled);
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var theme = VisualTheme.Report();
        var colors = theme.Resolve(mode);
        Assert.Equal((byte)0, colors.Background.A);
        var html = Page(producer, theme, mode);
        await using var session = await OpenAsync(html, 360, 800);
        foreach (var width in new[] { 360, 1200 }) {
            await session.Page.SetViewportSizeAsync(width, 800);
            try {
                await session.Page.EvaluateAsync("async () => { await document.fonts.ready; await new Promise(requestAnimationFrame); }");
                var background = await session.Page.EvaluateAsync<string>("() => getComputedStyle(document.body).backgroundColor");
                await AssertCssColorsEqualAsync(session.Page, colors.ElevatedSurface.ToCss(), background);
                var colorScheme = await session.Page.EvaluateAsync<string>("() => getComputedStyle(document.body).colorScheme");
                if (colorScheme != "normal") Assert.Equal(dark ? "dark" : "light", colorScheme);
                AssertNoConsoleErrors(session);
            } finally {
                var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
                if (!string.IsNullOrWhiteSpace(directory)) {
                    if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("CFX_BROWSER_CAPTURE_DIRECTORY must be an absolute output path.");
                    Directory.CreateDirectory(directory);
                    await session.Page.ScreenshotAsync(new() {
                        Path = Path.Combine(directory, "standalone-background-" + producer + "-" + (dark ? "dark" : "light") + "-" + width + ".png"),
                        FullPage = true
                    });
                }
            }
        }
    }

    private static string Page(string producer, VisualTheme theme, VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Observed work")
            .WithTheme(theme, mode).WithXLabels("Mon", "Tue", "Wed")
            .AddLine("Completed", new[] { new ChartPoint(1, 18), new ChartPoint(2, 32), new ChartPoint(3, 27) });
        var card = MetricCard.Create().WithSize(640, 360).WithTheme(theme, mode)
            .WithMetric("Readiness", "74%").WithStatus(VisualStatus.Positive).WithCaption("Completed checks");
        return producer switch {
            "chart" => chart.ToHtmlPage(),
            "chart-grid" => ChartGrid.Create().WithTheme(theme, mode).WithColumns(1).Add(chart).ToHtmlPage(),
            "metric-card" => card.ToHtmlPage(),
            "visual-grid" => VisualGrid.Create().WithTheme(theme, mode).WithColumns(1).Add(card).ToHtmlPage(),
            _ => throw new ArgumentOutOfRangeException(nameof(producer), producer, "Unknown standalone producer.")
        };
    }
}
