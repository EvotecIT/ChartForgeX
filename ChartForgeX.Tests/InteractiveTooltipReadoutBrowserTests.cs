using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects readable data tooltips without stripping host identity or authored metadata.</summary>
public sealed class InteractiveTooltipReadoutBrowserTests {
    [Theory]
    [InlineData("funnel", false)]
    [InlineData("funnel", true)]
    [InlineData("pie", false)]
    [InlineData("pie", true)]
    [InlineData("scatter", false)]
    [InlineData("scatter", true)]
    [InlineData("gauge", false)]
    [InlineData("gauge", true)]
    [InlineData("bullet", false)]
    [InlineData("bullet", true)]
    public async Task ReadoutKeepsDataAndAuthoredMetadataWhileEventsRetainRenderingIdentity(string family, bool dark) {
        if (!Enabled) return;
        var width = dark ? 340 : 700;
        var chart = Chart.Create().WithSize(596, 338).WithXLabels("North", "South")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        switch (family) {
            case "funnel": chart.AddFunnel("Requests", ChartPoints.FromValues(7, 3)); break;
            case "pie": chart.AddPie("Requests", ChartPoints.FromValues(7, 3)); break;
            case "scatter": chart.AddScatter("Observed", new[] { new ChartPoint(1, 0), new ChartPoint(2, 3) }); break;
            case "gauge": chart.AddGauge("Capacity", 0); break;
            case "bullet": chart.AddBullet("Requests", 7, 10); break;
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.TooltipMode = HtmlChartTooltipMode.Single), width, 560);
        var page = session.Page;
        var target = page.Locator("[data-cfx-keyboard-component='data']").First;
        var raw = await target.EvaluateAsync<JsonElement>("node => ({ role: node.dataset.cfxRole, kind: node.dataset.cfxKind, point: node.dataset.cfxPoint, id: node.dataset.cfxTargetId, value: node.dataset.cfxValue ?? node.dataset.cfxY, percent: node.dataset.cfxPercent })");
        await target.FocusAsync();
        var rows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
        Assert.DoesNotContain("Role", rows);
        Assert.DoesNotContain("Point", rows);
        Assert.DoesNotContain("Kind", rows);
        var valueLabel = family == "scatter" ? "Y" : "Value";
        var valueRow = page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = valueLabel }).Locator("+ dd");
        Assert.Equal(family is "scatter" or "gauge" ? "0" : "7", await valueRow.InnerTextAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "tooltip-readout-" + family + "-" + (dark ? "dark-compact" : "light-wide");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + "-default.png") });
        }
        if (family is "pie" or "gauge")
            Assert.Equal(family == "pie" ? "70%" : "0%", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Percent" }).Locator("+ dd").InnerTextAsync());
        if (family == "gauge") {
            Assert.Equal(0, await target.Locator("[data-cfx-role='gauge-value']").CountAsync());
            await AssertScalarSwatchAsync(page, target, "gauge-track", "fill");
        }
        if (family == "bullet") await AssertScalarSwatchAsync(page, target, "bullet-value", "fill");

        // Hosts attach business metadata to the existing native target, without replacing its geometry or identity.
        const string authoredKind = "Receipt <img src=x onerror=alert(1)>";
        await target.EvaluateAsync("(node, kind) => { node.setAttribute('data-cfx-meta-kind', kind); node.setAttribute('data-cfx-meta-source-record', 'record-17'); }", authoredKind);
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.readoutSelection = event.detail.target)");
        await target.BlurAsync();
        await target.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(authoredKind, await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Kind" }).Locator("+ dd").InnerTextAsync());
        Assert.Equal("record-17", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Source Record" }).Locator("+ dd").InnerTextAsync());
        Assert.Equal(0, await page.Locator(".cfx-tooltip img").CountAsync());
        var selected = await page.EvaluateAsync<JsonElement>("() => window.readoutSelection");
        Assert.Equal(raw.GetProperty("role").GetString(), selected.GetProperty("role").GetString());
        Assert.Equal(raw.GetProperty("kind").GetString(), selected.GetProperty("kind").GetString());
        Assert.Equal(raw.GetProperty("id").GetString(), selected.GetProperty("targetId").GetString());
        Assert.Equal(raw.GetProperty("value").GetString(), selected.GetProperty("value").GetString());
        if (raw.TryGetProperty("percent", out var percent))
            Assert.Equal(percent.GetString(), await target.GetAttributeAsync("data-cfx-percent"));
        Assert.Equal(raw.TryGetProperty("point", out var point) ? point.GetString() : null,
            selected.TryGetProperty("point", out var selectedPoint) ? selectedPoint.GetString() : null);
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("node => { const box = node.getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.top >= 0 && box.bottom <= innerHeight; }"));
        if (!string.IsNullOrWhiteSpace(capture)) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png") });
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".runtime.json"), JsonSerializer.Serialize(new { Raw = raw, Selected = selected, Rows = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync() }, new JsonSerializerOptions { WriteIndented = true }));
        }
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("pie", false, "61.54%")]
    [InlineData("pie", true, "61.54%")]
    [InlineData("gauge", false, "100%")]
    [InlineData("gauge", true, "100%")]
    [InlineData("gauge-needle", false, "100%")]
    [InlineData("gauge-needle", true, "100%")]
    [InlineData("gauge-linear", false, "100%")]
    [InlineData("gauge-linear", true, "100%")]
    [InlineData("sunburst", false, "61.54%")]
    [InlineData("polar-area", true, "61.54%")]
    public async Task PercentReadoutUsesReadableUnitsWhileRetainingFractionalMetadata(string family, bool dark, string expected) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(dark ? 316 : 596, 338).WithLegend(false).WithDataLabels(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        switch (family) {
            case "pie": chart.AddPie("Requests", ChartPoints.FromValues(8, 5)); break;
            case "gauge": chart.AddGauge("Capacity", 13, max: 13); break;
            case "gauge-needle": chart.AddGauge("Capacity", 13, max: 13).WithGauge(options => options.Form = ChartGaugeForm.Needle); break;
            case "gauge-linear": chart.AddGauge("Capacity", 13, max: 13).WithGauge(options => options.Form = ChartGaugeForm.Linear); break;
            case "sunburst": chart.AddSunburst("Teams", new[] { new ChartTreeLink("Teams", "Support", 8), new ChartTreeLink("Teams", "Other", 5) }); break;
            case "polar-area": chart.AddPolarArea("Requests", ChartPoints.FromValues(8, 5)); break;
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.TooltipMode = HtmlChartTooltipMode.Single), dark ? 340 : 700, 560);
        var page = session.Page;
        var gauge = family.StartsWith("gauge", StringComparison.Ordinal);
        var rawValue = gauge ? "13" : "8";
        var target = page.Locator("[data-cfx-percent][data-cfx-value='" + rawValue + "']").First;
        var rawPercent = await target.GetAttributeAsync("data-cfx-percent");
        var id = await target.GetAttributeAsync("data-cfx-target-id");
        Assert.Equal(gauge ? 1d : 8d / 13d, await target.EvaluateAsync<double>("node => Number(node.dataset.cfxPercent)"));
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => { window.percentSelection = event.detail.target; window.percentSelectionLabel = event.detail.label; })");
        await target.FocusAsync();
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "tooltip-percent-" + family + "-" + (dark ? "dark-compact" : "light-wide");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png") });
        }
        Assert.True(await target.EvaluateAsync<bool>("node => node === document.activeElement"),
            await target.EvaluateAsync<string>("node => JSON.stringify({ role: node.dataset.cfxRole, source: node.dataset.cfxSourceId, target: node.dataset.cfxTargetId, component: node.dataset.cfxKeyboardComponent, tabindex: node.getAttribute('tabindex') })"));
        var displayed = await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Percent" }).Locator("+ dd").InnerTextAsync();
        Assert.Equal(expected, displayed);
        if (gauge) await AssertScalarSwatchAsync(page, target, family == "gauge-needle" ? "gauge-needle" : "gauge-value", family == "gauge-needle" ? "stroke" : "fill");
        if (family == "pie") {
            var title = await page.Locator(".cfx-tooltip__title").InnerTextAsync();
            Assert.Contains("Slice 1", title);
            Assert.Contains("Value 8", title);
            Assert.DoesNotContain("Point 0", title);
        }
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.percentSelection");
        var eventLabel = await page.EvaluateAsync<string>("() => window.percentSelectionLabel");
        if (family == "pie") Assert.Equal("Slice 1 / Point 0 / Value 8", eventLabel);
        Assert.Equal(id, selected.GetProperty("targetId").GetString());
        Assert.Equal(rawValue, selected.GetProperty("value").GetString());
        Assert.Equal(rawPercent, await target.GetAttributeAsync("data-cfx-percent"));
        if (!string.IsNullOrWhiteSpace(capture))
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".runtime.json"), JsonSerializer.Serialize(new { RawPercent = rawPercent, DisplayedPercent = displayed, EventLabel = eventLabel, Selected = selected }, new JsonSerializerOptions { WriteIndented = true }));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CircleScalarUsesSeriesIdentityForTabPointerAndPercentageReadout(bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(dark ? 316 : 596, 338).WithLegend(true).WithDataLabels(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .AddCircle("Capacity", 8, max: 13);
        chart.Series[0].WithInteractionKey("capacity-source");
        var html = chart.ToInteractiveHtmlPage(options => options.TooltipMode = HtmlChartTooltipMode.Single)
            .Replace("<main ", "<button id='before-chart'>Before chart</button><main ", StringComparison.Ordinal);
        if (dark) {
            var position = html.IndexOf("data-cfx-role=\"circle-chart\"", StringComparison.Ordinal);
            Assert.True(position >= 0);
            html = html.Insert(position, "data-cfx-label=\"Authored capacity\" ");
        }
        await using var session = await OpenAsync(html, dark ? 340 : 700, 560);
        var page = session.Page;
        var target = page.Locator("[data-cfx-role='circle-chart']");
        var root = page.Locator(".cfx-interactive-chart");
        var rawPercent = await target.GetAttributeAsync("data-cfx-percent");
        Assert.Equal(8d / 13d, await target.EvaluateAsync<double>("node => Number(node.dataset.cfxPercent)"));
        await page.EvaluateAsync("() => { const root = document.querySelector('.cfx-interactive-chart'); root.addEventListener('cfxselect', event => window.circleSelection = event.detail.target); root.addEventListener('cfxhover', event => window.circleHover = event.detail.target); }");
        await page.Locator("#before-chart").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        Assert.True(await target.EvaluateAsync<bool>("node => node === document.activeElement"));
        Assert.Equal(1, await page.Locator("[data-cfx-keyboard-component='data'][tabindex='0']").CountAsync());
        Assert.Equal("61.54%", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Percent" }).Locator("+ dd").InnerTextAsync());
        Assert.Contains(dark ? "Authored capacity" : "Capacity", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        await AssertScalarSwatchAsync(page, target, "circle-value", "fill");
        await CaptureCircleAsync(page, dark, "keyboard");
        await page.Keyboard.PressAsync("Space");
        AssertCircleSeries(await page.EvaluateAsync<JsonElement>("() => JSON.parse(JSON.stringify(window.circleSelection))"));
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await page.Keyboard.PressAsync("Space");
        await target.BlurAsync();
        await MoveAwayAsync(page);
        var center = await CircleLocationAsync(target, false);
        await page.Mouse.MoveAsync((float)center[0], (float)center[1]);
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.NotNull(await root.GetAttributeAsync("data-cfx-hover-key"));
        AssertCircleSeries(await page.EvaluateAsync<JsonElement>("() => JSON.parse(JSON.stringify(window.circleHover))"));
        Assert.Equal("61.54%", await page.Locator(".cfx-tooltip dt").Filter(new LocatorFilterOptions { HasText = "Percent" }).Locator("+ dd").InnerTextAsync());
        await AssertScalarSwatchAsync(page, target, "circle-value", "fill");
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await CaptureCircleAsync(page, dark, "hover");
        await page.Mouse.ClickAsync((float)center[0], (float)center[1]);
        var pointerSelection = await page.EvaluateAsync<JsonElement>("() => JSON.parse(JSON.stringify(window.circleSelection))");
        AssertCircleSeries(pointerSelection);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal(rawPercent, await target.GetAttributeAsync("data-cfx-percent"));
        Assert.Equal("8", await target.GetAttributeAsync("data-cfx-value"));
        await CaptureCircleAsync(page, dark, "selected");

        // Clear the pin before checking empty space and the actual legend's muted paint.
        await target.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await target.BlurAsync();
        await MoveAwayAsync(page);
        center = await CircleLocationAsync(target, false);
        await page.Mouse.MoveAsync((float)center[0], (float)center[1]);
        var empty = await CircleLocationAsync(target, true);
        Assert.True(await page.EvaluateAsync<bool>("position => !document.elementFromPoint(...position)?.closest('[data-cfx-target-kind=\"series\"][data-cfx-value]')", empty));
        await page.Mouse.MoveAsync((float)empty[0], (float)empty[1]);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Null(await root.GetAttributeAsync("data-cfx-hover-key"));
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await CaptureCircleAsync(page, dark, "empty");
        await page.Locator(Legend(0)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await page.Locator(Legend(0)).BlurAsync();
        Assert.Equal(0, await page.Locator("[data-cfx-keyboard-component='data'][tabindex='0']").CountAsync());
        await MoveAwayAsync(page);
        center = await CircleLocationAsync(target, false);
        await page.Mouse.MoveAsync((float)center[0], (float)center[1]);
        await CaptureCircleAsync(page, dark, "muted");
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Null(await root.GetAttributeAsync("data-cfx-hover-key"));
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await page.Locator(Legend(0)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await page.Locator(Legend(0)).BlurAsync();
        await MoveAwayAsync(page);
        await target.EvaluateAsync("node => node.setAttribute('aria-hidden', 'true')");
        center = await CircleLocationAsync(target, false);
        await page.Mouse.MoveAsync((float)center[0], (float)center[1]);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Null(await root.GetAttributeAsync("data-cfx-hover-key"));
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await CaptureCircleAsync(page, dark, "hidden");
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture))
            await File.WriteAllTextAsync(Path.Combine(capture, "tooltip-circle-" + (dark ? "dark-compact" : "light-wide") + ".runtime.json"), JsonSerializer.Serialize(new { RawPercent = rawPercent, DisplayedPercent = "61.54%", Selected = pointerSelection }, new JsonSerializerOptions { WriteIndented = true }));
        AssertNoConsoleErrors(session);
    }

    private static void AssertCircleSeries(JsonElement target) {
        Assert.Equal("series", target.GetProperty("targetKind").GetString());
        Assert.Equal("capacity-source", target.GetProperty("targetId").GetString());
        Assert.Equal("capacity-source", target.GetProperty("seriesKey").GetString());
        Assert.Equal("8", target.GetProperty("value").GetString());
        Assert.False(target.TryGetProperty("point", out _));
        Assert.False(target.TryGetProperty("sourcePoint", out _));
    }

    private static Task<double[]> CircleLocationAsync(ILocator target, bool empty) => target.EvaluateAsync<double[]>("(node, empty) => { const svg = node.closest('svg'); const center = node.querySelector('[data-cfx-role=\"circle-center\"]'); const point = svg.createSVGPoint(); point.x = empty ? 8 : Number(center.getAttribute('cx')); point.y = empty ? svg.viewBox.baseVal.height - 8 : Number(center.getAttribute('cy')); const screen = point.matrixTransform(svg.getScreenCTM()); return [screen.x, screen.y]; }", empty);

    private static async Task AssertScalarSwatchAsync(IPage page, ILocator target, string role, string paint) {
        var native = await target.Locator("[data-cfx-role='" + role + "']").First.EvaluateAsync<string>("(node, property) => getComputedStyle(node)[property]", paint);
        Assert.Equal(native, await page.Locator(".cfx-tooltip__title .cfx-tooltip__swatch").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
    }

    private static async Task CaptureCircleAsync(IPage page, bool dark, string state) {
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("node => { if (node.hidden) return true; const box = node.getBoundingClientRect(); return box.left >= 0 && box.right <= innerWidth && box.top >= 0 && box.bottom <= innerHeight; }"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, "tooltip-circle-" + (dark ? "dark-compact" : "light-wide") + "-" + state + ".png") });
        }
    }
}
