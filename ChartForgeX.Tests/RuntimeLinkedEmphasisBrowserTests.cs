using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RuntimeLinkedEmphasisBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Line, false, 1440)]
    [InlineData(ChartSeriesKind.Line, true, 390)]
    [InlineData(ChartSeriesKind.Area, false, 390)]
    [InlineData(ChartSeriesKind.Area, true, 1440)]
    [InlineData(ChartSeriesKind.StepLine, false, 390)]
    [InlineData(ChartSeriesKind.StepLine, true, 1440)]
    [InlineData(ChartSeriesKind.StepArea, false, 1440)]
    [InlineData(ChartSeriesKind.StepArea, true, 390)]
    public async Task LinkedEmphasisPaintsConnectedSeriesAndSelectedPointWithoutHoverReset(ChartSeriesKind kind, bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Create(kind, dark).ToInteractiveHtmlPage(Options), width, 680);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.linkedRoot=document.querySelector('.cfx-interactive-chart'); window.linkedController=ChartForgeXCharts.get(linkedRoot); linkedController.highlight({observationIds:['a-1']},{dim:true}); }");
        var highlighted = await Paint(page);
        Assert.Equal(new[] { "1", "0.3" }, highlighted.GetProperty("series").EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Equal("1", highlighted.GetProperty("markerOpacity").GetString());
        Assert.True(highlighted.GetProperty("markerRadius").GetDouble() > 0);

        await MoveToAsync(page, "[data-cfx-observation-id='b-1']");
        var hovered = await Paint(page);
        Assert.Equal("0.3", hovered.GetProperty("series")[1].GetString());
        Assert.Equal("1", hovered.GetProperty("markerOpacity").GetString());
        Assert.Equal(new[] { "1", "0.3" }, hovered.GetProperty("legends").EnumerateArray().Select(value => value.GetString()).ToArray());

        await page.EvaluateAsync("() => { linkedController.clearHighlight(); linkedController.select({observationIds:['a-1']}); }");
        var selected = await Paint(page);
        Assert.Equal(new[] { "1", "0.3" }, selected.GetProperty("series").EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Equal("1", selected.GetProperty("markerOpacity").GetString());
        await Capture(page, "linked-emphasis-" + kind + "-" + dark + "-" + width, selected);

        await page.EvaluateAsync("() => linkedController.clearSelection()");
        await MoveAwayAsync(page);
        Assert.Equal(new[] { "1", "1" }, (await Paint(page)).GetProperty("series").EnumerateArray().Select(value => value.GetString()).ToArray());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectedConnectedSeriesKeepsExplicitLegendMuteUntilRestored(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Create(ChartSeriesKind.Line, dark).ToInteractiveHtmlPage(Options), 740, 680);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.linkedRoot=document.querySelector('.cfx-interactive-chart'); ChartForgeXCharts.get(linkedRoot).select({observationIds:['a-1']}); }");
        var legend = page.Locator("[data-cfx-role='legend-item'][data-cfx-series='0']");
        await legend.ClickAsync();
        Assert.Equal("true", await legend.GetAttributeAsync("data-cfx-muted"));
        Assert.Equal("0.3", (await Paint(page)).GetProperty("series")[0].GetString());
        await legend.ClickAsync();
        Assert.Equal("1", (await Paint(page)).GetProperty("series")[0].GetString());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task LinkedForegroundKeepsExplicitSeriesIsolationUntilRestored(bool dark, bool highlight) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Create(ChartSeriesKind.Line, dark).ToInteractiveHtmlPage(Options), dark ? 390 : 1440, 680);
        var page = session.Page;
        await page.EvaluateAsync("highlight => { window.linkedRoot=document.querySelector('.cfx-interactive-chart'); const controller=ChartForgeXCharts.get(linkedRoot); if(highlight)controller.highlight({observationIds:['a-1']},{dim:true});else controller.select({observationIds:['a-1']}); }", highlight);
        var legend = page.Locator("[data-cfx-role='legend-item'][data-cfx-series='1']");
        await legend.ClickAsync(new Microsoft.Playwright.LocatorClickOptions { Modifiers = new[] { Microsoft.Playwright.KeyboardModifier.Shift } });
        Assert.True(await page.Locator("[data-cfx-role='series'][data-cfx-series='0']").EvaluateAsync<bool>("node=>node.classList.contains('cfx-series-isolated-out')"));
        Assert.Equal("0.1", (await Paint(page)).GetProperty("series")[0].GetString());
        Assert.Equal("0.1", (await Paint(page)).GetProperty("legends")[0].GetString());
        await Capture(page, "linked-isolated-" + dark + "-" + highlight, await Paint(page));
        await legend.ClickAsync(new Microsoft.Playwright.LocatorClickOptions { Modifiers = new[] { Microsoft.Playwright.KeyboardModifier.Shift } });
        Assert.Equal("1", (await Paint(page)).GetProperty("series")[0].GetString());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 1440)]
    [InlineData(true, 390)]
    public async Task HoverPreservesOtherDimmedBarsDuringHostHighlightAndSelection(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Create(ChartSeriesKind.Bar, dark).ToInteractiveHtmlPage(Options), width, 680);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.linkedRoot=document.querySelector('.cfx-interactive-chart'); window.linkedController=ChartForgeXCharts.get(linkedRoot); linkedController.highlight({observationIds:['a-1']},{dim:true}); }");
        await MoveToAsync(page, "[data-cfx-observation-id='b-1']");
        Assert.Equal("0.3", await page.Locator("[data-cfx-observation-id='b-2']").EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        Assert.Equal(new[] { "1", "0.3" }, (await Paint(page)).GetProperty("legends").EnumerateArray().Select(value => value.GetString()).ToArray());
        await page.EvaluateAsync("() => { linkedController.clearHighlight(); linkedController.select({observationIds:['a-1']}); }");
        await MoveToAsync(page, "[data-cfx-observation-id='b-2']");
        Assert.Equal("0.3", await page.Locator("[data-cfx-observation-id='b-1']").EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        Assert.Equal(new[] { "1", "0.3" }, (await Paint(page)).GetProperty("legends").EnumerateArray().Select(value => value.GetString()).ToArray());
        await Capture(page, "linked-bar-hover-" + dark + "-" + width, await page.EvaluateAsync<JsonElement>("() => ({selected:linkedController.getSelection(),opacities:[...linkedRoot.querySelectorAll('.cfx-selection-dim')].map(n=>getComputedStyle(n).opacity)})"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 1440)]
    [InlineData(true, 390)]
    public async Task CartesianPointLegendsKeepObservationForegroundAndExplicitIsolation(bool dark, int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Requests over time").WithXLabels("Mon", "Tue", "Wed").WithPointLegend()
            .AddSeries(new ChartSeries(new ChartSeriesId("api"), "API", ChartSeriesKind.Bar,
                new[] { new ChartPoint(1, 17), new ChartPoint(2, 24), new ChartPoint(3, 21) })
                .WithObservationIds(new[] { new ChartObservationId("a-1"), new ChartObservationId("a-2"), new ChartObservationId("a-3") }));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Options), width, 680);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.linkedRoot=document.querySelector('.cfx-interactive-chart'); window.linkedController=ChartForgeXCharts.get(linkedRoot); linkedController.highlight({observationIds:['a-1']},{dim:true}); }");
        var first = page.Locator("[data-cfx-role='legend-item'][data-cfx-series='0'][data-cfx-point='0']");
        var second = page.Locator("[data-cfx-role='legend-item'][data-cfx-series='0'][data-cfx-point='1']");
        await MoveToAsync(page, "[data-cfx-observation-id='a-3']");
        Assert.Equal("1", await first.EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        Assert.Equal("0.3", await second.EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        await first.ClickAsync(new Microsoft.Playwright.LocatorClickOptions { Modifiers = new[] { Microsoft.Playwright.KeyboardModifier.Shift } });
        Assert.Equal("0.1", await second.EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        await first.ClickAsync(new Microsoft.Playwright.LocatorClickOptions { Modifiers = new[] { Microsoft.Playwright.KeyboardModifier.Shift } });
        Assert.Equal("0.3", await second.EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        await Capture(page, "linked-point-legend-" + dark + "-" + width, await Paint(page));
        await page.EvaluateAsync("() => linkedController.clearHighlight()");
        await first.BlurAsync();
        await MoveAwayAsync(page);
        Assert.Equal("1", await second.EvaluateAsync<string>("node=>getComputedStyle(node).opacity"));
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> Paint(Microsoft.Playwright.IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
          const marker=linkedRoot.querySelector('[data-cfx-observation-id="a-1"] .cfx-prepared-point-marker');
          return {series:[...linkedRoot.querySelectorAll('[data-cfx-role="series"][data-cfx-coordinate-system="cartesian"]')].map(n=>getComputedStyle(n).opacity),
            legends:[...linkedRoot.querySelectorAll('[data-cfx-role="legend-item"]')].map(n=>getComputedStyle(n).opacity),
            markerOpacity:marker?getComputedStyle(marker).opacity:null,markerRadius:marker?parseFloat(getComputedStyle(marker).r):0};
        }
        """);

    private static Chart Create(ChartSeriesKind kind, bool dark) {
        var chart = Chart.Create().WithSize(640, 360).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Requests over time").WithXLabels("Mon", "Tue", "Wed");
        chart.AddSeries(new ChartSeries(new ChartSeriesId("api"), "API", kind, new[] { new ChartPoint(1, 17), new ChartPoint(2, 24), new ChartPoint(3, 21) })
            .WithObservationIds(new[] { new ChartObservationId("a-1"), new ChartObservationId("a-2"), new ChartObservationId("a-3") }));
        chart.AddSeries(new ChartSeries(new ChartSeriesId("worker"), "Worker", kind, new[] { new ChartPoint(1, 8), new ChartPoint(2, 12), new ChartPoint(3, 9) })
            .WithObservationIds(new[] { new ChartObservationId("b-1"), new ChartObservationId("b-2"), new ChartObservationId("b-3") }));
        return chart;
    }

    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.Enable(ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation);
        options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
        options.Highlight.DimOthersWhileSelected = true;
        options.Tooltip.PinOnSelect = false;
        options.Toolbar = HtmlChartToolbarLayout.Compact;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
    }

    private static async Task Capture(Microsoft.Playwright.IPage page, string name, JsonElement state) {
        var output = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions { Path = Path.Combine(output, name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), state.GetRawText());
    }
}
