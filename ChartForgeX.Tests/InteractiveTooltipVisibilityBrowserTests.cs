using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveTooltipVisibilityBrowserTests {
    [Theory]
    [InlineData("display:none", false, 700, false, false)]
    [InlineData("visibility:hidden", true, 340, false, false)]
    [InlineData("opacity:0", false, 700, false, false)]
    [InlineData("display:none", true, 340, true, false)]
    [InlineData("display:none", false, 700, false, true)]
    public async Task SharedRowsRequireVisiblePrimaryBarPaintAndRefreshAfterHostStyleChanges(string hiddenStyle, bool dark, int width, bool hidePoint, bool wrapMark) {
        if (!Enabled) return;
        var chart = Frame(dark, "Visible bar observations").WithBarStyle(ChartBarStyle.Solid)
            .AddBar("Current", ChartPoints.FromValues(3, 7, 5)).AddBar("Baseline", ChartPoints.FromValues(1, 2, 1));
        chart.Series[1].WithPointColor(1, "#7b61e8").WithPointFillPattern(1, ChartFillPattern.Crosshatch);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Interaction.Disable(ChartInteractionFeatures.Crosshair)), width, 560);
        var page = session.Page;
        var hidden = hidePoint ? Point(1, 1) : Point(1, 1) + " [data-cfx-role='bar']";
        if (wrapMark) {
            await page.Locator(hidden).EvaluateAsync("node => { const wrapper = document.createElementNS('http://www.w3.org/2000/svg', 'g'); wrapper.dataset.tooltipHostWrapper = ''; node.replaceWith(wrapper); wrapper.appendChild(node); }");
            hidden = Point(1, 1) + " [data-tooltip-host-wrapper]";
        }
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = hidden + " { " + hiddenStyle.Replace(":", ": ", StringComparison.Ordinal) + " !important; }" });
        await page.Locator(Point(0, 1)).FocusAsync();
        var rows = await RowsAsync(page);
        var visibility = await page.Locator(hidden).EvaluateAsync<string[]>("node => { const style = getComputedStyle(node); return [style.display, style.visibility, style.opacity]; }");
        await RecordAsync(page, "tooltip-hidden-bar-" + hiddenStyle.Replace(':', '-') + "-" + (dark ? "dark" : "light") + (hidePoint ? "-point" : wrapMark ? "-wrapper" : "-mark"), new { rows, visibility });
        Assert.Contains(hiddenStyle.Split(':')[1], visibility);
        Assert.Equal(new[] { "Current" }, rows.Select(row => row[0]).ToArray());
        Assert.Equal("7", rows[0][1]);
        // Each tooltip render must observe current host CSS rather than retain a stale visibility cache.
        await page.Locator(hidden).EvaluateAsync("node => { node.style.setProperty('display', 'inline', 'important'); node.style.setProperty('visibility', 'visible', 'important'); node.style.setProperty('opacity', '1', 'important'); }");
        await page.Locator(Point(0, 1)).BlurAsync();
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.Equal(new[] { "Current", "Baseline" }, (await RowsAsync(page)).Select(row => row[0]).ToArray());
        var restoredPaint = await page.Locator(Point(1, 1) + " [data-cfx-role='bar']").EvaluateAsync<string>("node => getComputedStyle(document.getElementById(node.getAttribute('fill').slice(5, -1)).querySelector('stop')).stopColor");
        Assert.Equal(restoredPaint, await page.Locator("dt[data-cfx-tooltip-series='1'] .cfx-tooltip__swatch").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.True(await page.Locator(Point(0, 2)).EvaluateAsync<bool>("node => node === document.activeElement"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, 700)]
    [InlineData(true, 340)]
    public async Task DecorativeSvgRetainsSharedPointerTooltipsForVisuallyPaintedObservations(bool dark, int width) {
        if (!Enabled) return;
        var chart = Frame(dark, "Decorative observations").AsDecorative()
            .AddScatter("Current", new[] { new ChartPoint(1, 7), new ChartPoint(2, 5) })
            .AddScatter("Baseline", new[] { new ChartPoint(1, 2), new ChartPoint(2, 1) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Interaction.Disable(ChartInteractionFeatures.Crosshair)), width, 560);
        var page = session.Page;
        await MoveToAsync(page, Point(0, 0));
        var rows = await RowsAsync(page);
        await RecordAsync(page, "tooltip-decorative-" + (dark ? "dark" : "light") + "-" + width, new { rows });
        Assert.Equal("true", await page.Locator("svg").GetAttributeAsync("aria-hidden"));
        Assert.Equal(new[] { "Current", "Baseline" }, rows.Select(row => row[0]).ToArray());
        Assert.Equal(new[] { "7", "2" }, rows.Select(row => row[1]).ToArray());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, false, 700)]
    [InlineData(true, true, 340)]
    public async Task VisiblePrimaryMarksOverrideInheritedHiddenVisibility(bool wrapMark, bool dark, int width) {
        if (!Enabled) return;
        var chart = Frame(dark, "Restored mark visibility")
            .AddScatter("Current", new[] { new ChartPoint(1, 7), new ChartPoint(2, 5) })
            .AddScatter("Baseline", new[] { new ChartPoint(1, 2), new ChartPoint(2, 1) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Interaction.Disable(ChartInteractionFeatures.Crosshair)), width, 560);
        var page = session.Page;
        var mark = Point(1, 0) + " [data-cfx-role='marker']";
        var ancestor = Point(1, 0);
        if (wrapMark) {
            await page.Locator(mark).EvaluateAsync("node => { const wrapper = document.createElementNS('http://www.w3.org/2000/svg', 'g'); wrapper.dataset.tooltipHostWrapper = ''; node.replaceWith(wrapper); wrapper.appendChild(node); }");
            ancestor += " [data-tooltip-host-wrapper]";
        }
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = ancestor + " { visibility:hidden !important; } " + mark + " { visibility:visible !important; }" });
        await page.Locator(Point(0, 0)).FocusAsync();
        var restoredRows = await RowsAsync(page);
        var ancestorVisibility = await page.Locator(ancestor).EvaluateAsync<string>("node => getComputedStyle(node).visibility");
        var markVisibility = await page.Locator(mark).EvaluateAsync<string>("node => getComputedStyle(node).visibility");
        await RecordAsync(page, "tooltip-restored-mark-" + (wrapMark ? "wrapper" : "point") + "-" + (dark ? "dark" : "light"), new { restoredRows, ancestorVisibility, markVisibility });
        Assert.Equal("hidden", ancestorVisibility);
        Assert.Equal("visible", markVisibility);
        Assert.NotNull(await page.Locator(mark).BoundingBoxAsync());
        Assert.Equal(new[] { "Current", "Baseline" }, restoredRows.Select(row => row[0]).ToArray());
        Assert.Equal(new[] { "7", "2" }, restoredRows.Select(row => row[1]).ToArray());
        await page.Locator(mark).EvaluateAsync("node => node.style.setProperty('visibility', 'hidden', 'important')");
        await page.Locator(Point(0, 0)).BlurAsync();
        await page.Locator(Point(0, 0)).FocusAsync();
        Assert.Equal(new[] { "Current" }, (await RowsAsync(page)).Select(row => row[0]).ToArray());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task VisibleLinePathOverridesInheritedHiddenSeriesVisibility() {
        if (!Enabled) return;
        var chart = Frame(true, "Restored connected surface").WithLineMarkers(ChartLineMarkerMode.All)
            .AddLine("Current", ChartPoints.FromValues(3, 7, 5))
            .AddLine("Baseline", ChartPoints.FromValues(1, 2, 1), ChartColor.FromHex("#7b61e8"));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Interaction.Disable(ChartInteractionFeatures.Crosshair)), 340, 560);
        var page = session.Page;
        const string series = "[data-cfx-role='series'][data-cfx-series='1']";
        const string path = series + " [data-cfx-role='line']";
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = series + " { visibility:hidden !important; } " + path + " { visibility:visible !important; } " + series + " [data-cfx-role='marker'] { display:none !important; }" });
        await page.Locator(Point(0, 1)).FocusAsync();
        var restoredRows = await RowsAsync(page);
        await RecordAsync(page, "tooltip-restored-line-series-dark", new { restoredRows });
        Assert.Equal("hidden", await page.Locator(series).EvaluateAsync<string>("node => getComputedStyle(node).visibility"));
        Assert.Equal("visible", await page.Locator(path).EvaluateAsync<string>("node => getComputedStyle(node).visibility"));
        Assert.Equal(new[] { "Current", "Baseline" }, restoredRows.Select(row => row[0]).ToArray());
        Assert.Equal("rgb(123, 97, 232)", restoredRows[1][2]);
        await page.Locator(path).EvaluateAsync("node => node.style.setProperty('visibility', 'hidden', 'important')");
        await page.Locator(Point(0, 1)).BlurAsync();
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.Equal(new[] { "Current" }, (await RowsAsync(page)).Select(row => row[0]).ToArray());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, 700)]
    [InlineData(true, 340)]
    public async Task HiddenLineMarkersRetainVisibleSeriesPaintButHiddenPathsDoNotUseLegendPaint(bool dark, int width) {
        if (!Enabled) return;
        var chart = Frame(dark, "Line surface visibility").WithLineMarkers(ChartLineMarkerMode.All)
            .AddLine("Current", ChartPoints.FromValues(3, 7, 5))
            .AddLine("Baseline", ChartPoints.FromValues(1, 2, 1), ChartColor.FromHex("#7b61e8"));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Interaction.Disable(ChartInteractionFeatures.Crosshair)), width, 560);
        var page = session.Page;
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = Point(1, 1) + " [data-cfx-role='marker'] { display:none !important; }" });
        await page.Locator(Point(0, 1)).FocusAsync();
        var visiblePathRows = await RowsAsync(page);
        await RecordAsync(page, "tooltip-hidden-line-marker-" + (dark ? "dark" : "light") + "-" + width, new { visiblePathRows });
        Assert.Equal(new[] { "Current", "Baseline" }, visiblePathRows.Select(row => row[0]).ToArray());
        Assert.Equal("rgb(123, 97, 232)", visiblePathRows[1][2]);
        Assert.Equal("none", await page.Locator(Point(1, 1) + " [data-cfx-role='marker']").EvaluateAsync<string>("node => getComputedStyle(node).display"));
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "[data-cfx-role='series'][data-cfx-series='1'] [data-cfx-role='line'] { display:none !important; }" });
        await page.Locator(Point(0, 1)).BlurAsync();
        await page.Locator(Point(0, 1)).FocusAsync();
        var hiddenPathRows = await RowsAsync(page);
        await RecordAsync(page, "tooltip-hidden-line-path-" + (dark ? "dark" : "light") + "-" + width, new { hiddenPathRows });
        Assert.Equal(new[] { "Current" }, hiddenPathRows.Select(row => row[0]).ToArray());
        // A visible legend remains independently usable even when this observation has no painted mark.
        await page.Locator(Legend(1)).FocusAsync();
        Assert.Equal("rgb(123, 97, 232)", await page.Locator(".cfx-tooltip__swatch").EvaluateAsync<string>("node => getComputedStyle(node).backgroundColor"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(24)]
    [InlineData(240)]
    public async Task SharedTooltipResolvesStylesOnlyForMatchingXCandidates(int observations) {
        if (!Enabled) return;
        var chart = Frame(false, "Shared tooltip candidate filtering");
        for (var series = 0; series < 3; series++) {
            var offset = series;
            chart.AddScatter("Series " + series, Enumerable.Range(0, observations).Select(index => new ChartPoint(index + 1, 10 + offset * 10 + index % 7)));
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Interaction.Features = ChartInteractionFeatures.Tooltips));
        var page = session.Page;
        var measurement = await page.Locator(Point(0, observations / 2)).EvaluateAsync<StyleMeasurement>("""
            node => {
              const original = window.getComputedStyle;
              const x = Number(node.dataset.cfxX);
              let total = 0, unrelatedPoints = 0;
              window.getComputedStyle = function(element, pseudo) {
                total++;
                if (element.dataset && element.dataset.cfxRole === 'point' && Number(element.dataset.cfxX) !== x) unrelatedPoints++;
                return original.call(this, element, pseudo);
              };
              const box = node.getBoundingClientRect(), start = performance.now();
              try {
                node.dispatchEvent(new PointerEvent('pointerenter', { clientX: box.x + box.width / 2, clientY: box.y + box.height / 2 }));
                return { Total: total, UnrelatedPoints: unrelatedPoints, DurationMilliseconds: performance.now() - start };
              } finally { window.getComputedStyle = original; }
            }
            """);
        var rows = await RowsAsync(page);
        await RecordAsync(page, "tooltip-style-reads-" + observations, new { observations, measurement, rows }, screenshot: false);
        Assert.Equal(3 * observations, await page.Locator(PointTarget).CountAsync());
        Assert.Equal(3, rows.Length);
        Assert.Equal(0, measurement.UnrelatedPoints);
        Assert.InRange(measurement.Total, 1, 60);
        AssertNoConsoleErrors(session);
    }

    private static Chart Frame(bool dark, string title) => Chart.Create().WithSize(596, 338).WithTitle(title)
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());

    private static Task<string[][]> RowsAsync(IPage page) => page.EvaluateAsync<string[][]>("() => Array.from(document.querySelectorAll('.cfx-tooltip dt')).map(row => [row.textContent, row.nextElementSibling.textContent, row.querySelector('.cfx-tooltip__swatch') ? getComputedStyle(row.querySelector('.cfx-tooltip__swatch')).backgroundColor : ''])");

    private static async Task RecordAsync(IPage page, string name, object result, bool screenshot = true) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(result));
        if (screenshot) await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }

    public sealed class StyleMeasurement {
        public int Total { get; set; }
        public int UnrelatedPoints { get; set; }
        public double DurationMilliseconds { get; set; }
    }
}
