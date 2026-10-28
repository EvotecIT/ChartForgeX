using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Responsive paired/stack contours expose only real observations and retain named raw readouts.</summary>
public sealed class AreaDepthBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.StackedArea, "missing-zero", false)]
    [InlineData(ChartSeriesKind.StackedArea, "missing-gap", true)]
    [InlineData(ChartSeriesKind.RangeArea, "independent-boundaries", false)]
    [InlineData(ChartSeriesKind.RangeArea, "independent-boundaries", true)]
    [InlineData(ChartSeriesKind.DifferenceArea, "smooth-boundaries", false)]
    [InlineData(ChartSeriesKind.DifferenceArea, "smooth-boundaries", true)]
    public async Task NativeAreaOptionsRetainSourceSelectionAndReadoutsAfterResponsiveResize(ChartSeriesKind kind, string variant, bool compactDark) {
        if (!Enabled) return;
        var chart = ExampleRecipes.ForKind(kind, variant).Create(variant, compactDark ? VisualThemeMode.Dark : VisualThemeMode.Light) as Chart;
        Assert.NotNull(chart);
        chart!.WithSize(800, 440).WithTheme(VisualTheme.Graphite(), compactDark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        foreach (var series in chart.Series) series.ConfigureMarkers(markers => markers.Radius = 3);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Tooltip.Mode = HtmlChartTooltipMode.Single;
        }), compactDark ? 390 : 880, 620);
        var page = session.Page; var index = kind == ChartSeriesKind.StackedArea ? 2 : 3;
        var point = page.Locator(Point(0, index));
        var expectedId = kind == ChartSeriesKind.StackedArea ? "observed-zero" : kind == ChartSeriesKind.RangeArea ? "resumed-range" : "resumed-comparison";
        Assert.Equal(expectedId, await point.GetAttributeAsync("data-cfx-observation-id"));
        Assert.Equal(kind == ChartSeriesKind.StackedArea ? 5 : 7, await page.Locator("[data-cfx-role='point'][data-cfx-series='0']").CountAsync());
        if (kind == ChartSeriesKind.StackedArea) {
            Assert.Equal(0, await page.Locator("[data-cfx-role='point'][data-cfx-series='0'][data-cfx-x='3']").CountAsync());
            Assert.Equal("0", await point.GetAttributeAsync("data-cfx-y"));
        }
        await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        if (kind == ChartSeriesKind.DifferenceArea) {
            Assert.Contains("Actual", await TooltipTextAsync(page)); Assert.Contains("Forecast", await TooltipTextAsync(page));
            Assert.Equal("74", await point.GetAttributeAsync("data-cfx-first")); Assert.Equal("58", await point.GetAttributeAsync("data-cfx-second"));
        }
        if (kind == ChartSeriesKind.RangeArea) {
            Assert.Equal("25", await point.GetAttributeAsync("data-cfx-lower")); Assert.Equal("48", await point.GetAttributeAsync("data-cfx-upper"));
        }
        await page.SetViewportSizeAsync(compactDark ? 880 : 390, 620);
        Assert.Equal(expectedId, await point.GetAttributeAsync("data-cfx-observation-id"));
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        var box = await page.Locator(".cfx-interactive-chart").BoundingBoxAsync(); Assert.NotNull(box);
        Assert.True(box!.Width <= (compactDark ? 880 : 390));
        AssertNoConsoleErrors(session);
    }
}
