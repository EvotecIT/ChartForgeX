using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Imputed geometry identifies a category, without inventing an authored observation.</summary>
public sealed class InteractiveMissingCategoryBrowserTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task ImputedRadarCategoriesHaveIndependentRegionIdentityAndSynchronizeByCategory(bool compactDark, bool hiddenMarkers) {
        if (!Enabled) return;
        var charts = new[] { Create(false, compactDark, hiddenMarkers), Create(true, compactDark, hiddenMarkers) };
        var html = charts.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts;
        });
        await using var session = await OpenAsync(html, compactDark ? 390 : 740, 1100);
        var page = session.Page;
        var roots = page.Locator(".cfx-interactive-chart");
        await page.EvaluateAsync("""
            () => {
                window.missingCategoryEvents = [];
                const root = document.querySelector('.cfx-interactive-chart');
                for (const type of ['cfxhover', 'cfxselect'])
                    root.addEventListener(type, event => window.missingCategoryEvents.push({ type, target: event.detail.target }));
            }
            """);

        var observedZero = roots.Nth(0).Locator(Target(1));
        Assert.Equal("0", await observedZero.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("point", await observedZero.GetAttributeAsync("data-cfx-target-kind"));
        Assert.Equal("0", await observedZero.GetAttributeAsync("data-cfx-source-point"));
        Assert.Null(await observedZero.GetAttributeAsync("data-cfx-source-points"));
        var identities = await roots.Nth(0).Locator("[data-cfx-role='radar-point-source']")
            .EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxTargetKind + ':' + node.dataset.cfxTargetId)");
        Assert.Equal(8, identities.Distinct().Count());

        await observedZero.FocusAsync();
        Assert.Contains("Value 0", await page.Locator(".cfx-tooltip__title").InnerTextAsync(), StringComparison.Ordinal);
        foreach (var category in new[] { 2, 3 }) {
            var local = roots.Nth(0).Locator(Target(category));
            var peer = roots.Nth(1).Locator(Target(category));
            Assert.NotEqual(await local.GetAttributeAsync("data-cfx-source-id"), await peer.GetAttributeAsync("data-cfx-source-id"));
            foreach (var node in new[] { local, peer }) {
                Assert.Equal("region", await node.GetAttributeAsync("data-cfx-target-kind"));
                Assert.Equal("target-source:category:" + category, await node.GetAttributeAsync("data-cfx-target-id"));
                Assert.Equal("", await node.GetAttributeAsync("data-cfx-source-points"));
                Assert.Null(await node.GetAttributeAsync("data-cfx-point"));
                Assert.Null(await node.GetAttributeAsync("data-cfx-source-point"));
                Assert.Null(await node.GetAttributeAsync("data-cfx-value"));
                Assert.Null(await node.GetAttributeAsync("data-cfx-y"));
                Assert.Equal("No observation", await node.GetAttributeAsync("data-cfx-status"));
                if (hiddenMarkers) Assert.Equal(1, await node.Locator("[data-cfx-browser-hit-area='true']").CountAsync());
            }
            await page.Keyboard.PressAsync("ArrowRight");
            Assert.Equal("target-source:category:" + category,
                await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
            await page.Keyboard.PressAsync("Space");
            Assert.Contains("No observation", await page.Locator(".cfx-tooltip__title").InnerTextAsync(), StringComparison.Ordinal);
            Assert.DoesNotContain("Value", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
            Assert.DoesNotContain("Y", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
            foreach (var root in new[] { roots.Nth(0), roots.Nth(1) }) {
                Assert.Equal("true", await root.Locator(Target(category)).GetAttributeAsync("aria-selected"));
                Assert.Equal(category - 1, await root.Locator(TargetMarks + "[aria-selected='true']").CountAsync());
            }
        }
        using (var events = JsonDocument.Parse(await page.EvaluateAsync<string>("() => JSON.stringify(window.missingCategoryEvents)"))) {
            foreach (var category in new[] { 2, 3 }) {
                foreach (var type in new[] { "cfxhover", "cfxselect" }) {
                    var matches = events.RootElement.EnumerateArray().Where(item => item.GetProperty("type").GetString() == type
                        && item.GetProperty("target").GetProperty("targetId").GetString() == "target-source:category:" + category).ToArray();
                    Assert.NotEmpty(matches);
                    foreach (var item in matches) {
                        var target = item.GetProperty("target");
                        Assert.Equal("region", target.GetProperty("targetKind").GetString());
                        Assert.Equal("target-source", target.GetProperty("seriesKey").GetString());
                        Assert.False(target.TryGetProperty("point", out _));
                        Assert.False(target.TryGetProperty("sourcePoint", out _));
                        Assert.Empty(target.GetProperty("sourcePoints").EnumerateArray());
                        Assert.Equal("", target.GetProperty("value").GetString());
                    }
                }
            }
        }
        await roots.Nth(0).Locator(Target(2)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        foreach (var root in new[] { roots.Nth(0), roots.Nth(1) }) {
            Assert.Equal("false", await root.Locator(Target(2)).GetAttributeAsync("aria-selected"));
            Assert.Equal("true", await root.Locator(Target(3)).GetAttributeAsync("aria-selected"));
            Assert.Equal(1, await root.Locator(TargetMarks + "[aria-selected='true']").CountAsync());
        }
        await CaptureAsync(page, compactDark ? "compact-dark-hidden" : "wide-light");
        AssertNoConsoleErrors(session);
    }

    private const string TargetMarks = "[data-cfx-role='radar-point-source'][data-cfx-series-key='target-source']";
    private static string Target(double category) => TargetMarks + "[data-cfx-category='"
        + category.ToString(System.Globalization.CultureInfo.InvariantCulture) + "']";

    private static Chart Create(bool peer, bool dark, bool hiddenMarkers) {
        var observed = new[] { new ChartPoint(1, 3), new ChartPoint(2, 4), new ChartPoint(3, 5), new ChartPoint(4, 4) };
        var target = new[] { new ChartPoint(1, 0), new ChartPoint(4, 8) };
        var chart = Chart.Create().WithSize(620, 430).WithYAxisBounds(0, 10)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle(peer ? "Peer with another category" : "Imputed Radar categories")
            .WithSubtitle("Missing categories retain distinct identities without source observations")
            .AddRadarArea("Actual", peer ? observed.Concat(new[] { new ChartPoint(1.5, 6) }) : observed)
            .AddRadarLine("Target", peer ? target.AsEnumerable().Reverse() : target);
        chart.Series[0].WithInteractionKey("actual-source");
        chart.Series[1].WithInteractionKey("target-source");
        if (hiddenMarkers) chart.Series[1].WithMarkers(markers => markers.Enabled = false);
        return chart;
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions {
            Path = Path.Combine(directory, "radar-imputed-" + name + ".png"), FullPage = true
        });
        await File.WriteAllTextAsync(Path.Combine(directory, "radar-imputed-" + name + "-events.json"),
            await page.EvaluateAsync<string>("() => JSON.stringify(window.missingCategoryEvents, null, 2)"));
    }
}
