using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractivePreparedIdentityBrowserTests {
    [Theory]
    [InlineData(ChartForgeX.Core.ChartSeriesKind.Line)]
    [InlineData(ChartForgeX.Core.ChartSeriesKind.Radar)]
    public async Task MarkerFreeSourceFocusRequiresSeriesPaintAtItsOwnNativeClipLocation(ChartSeriesKind kind) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(600, 440).WithLegend(false).WithDataLabels(false).WithLineMarkers(ChartLineMarkerMode.None);
        var points = new[] { new ChartPoint(1, 60), new ChartPoint(2, 80), new ChartPoint(3, 50) };
        if (kind == ChartSeriesKind.Line) chart.AddLine("Observed", points);
        else { chart.AddRadar("Observed", points); chart.Series[0].MarkerRadius = 0; }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page;
        var point = page.Locator("[data-cfx-target-kind='point'][data-cfx-series='0'][data-cfx-point='0']");
        // A host can suppress markers while retaining the native line or polygon as the observation surface.
        await point.EvaluateAsync("node=>node.querySelectorAll('ellipse,circle').forEach(marker=>{marker.style.fill='none';marker.style.stroke='none';})");
        Assert.Equal("0", await point.GetAttributeAsync("tabindex"));
        await point.FocusAsync(); Assert.Contains("60", await TooltipTextAsync(page));
        await point.EvaluateAsync("""
            node=>{
              const svg=node.ownerSVGElement,ns='http://www.w3.org/2000/svg',clip=document.createElementNS(ns,'clipPath'),rect=document.createElementNS(ns,'rect');
              const prepared=JSON.parse(node.closest('.cfx-interactive-chart').dataset.cfxPreparedChart);
              const region=prepared.regions.find(region=>region.id===node.dataset.cfxSourceId),y=region.y+region.height/2+8;
              clip.id='source-location-clip';clip.setAttribute('clipPathUnits','userSpaceOnUse');
              for(const [key,value] of Object.entries({x:0,y,width:svg.viewBox.baseVal.width,height:svg.viewBox.baseVal.height-y}))rect.setAttribute(key,value);
              clip.appendChild(rect);svg.querySelector('defs').appendChild(clip);
              const owner=node.closest('[data-cfx-role="series"],[data-cfx-role="radar-series"]');
              owner.querySelectorAll('[data-cfx-role="line"],[data-cfx-role="radar-outline"],[data-cfx-role="radar-area"]').forEach(layer=>layer.setAttribute('clip-path','url(#source-location-clip)'));
            }
            """);
        Assert.Equal("-1", await point.GetAttributeAsync("tabindex"));
        Assert.False(await point.EvaluateAsync<bool>("node=>document.activeElement===node"));
        await point.EvaluateAsync("node=>node.closest('[data-cfx-role=\"series\"],[data-cfx-role=\"radar-series\"]').querySelectorAll('[clip-path=\"url(#source-location-clip)\"]').forEach(layer=>layer.removeAttribute('clip-path'))");
        await point.FocusAsync(); Assert.Equal("0", await point.GetAttributeAsync("tabindex"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions { Path = Path.Combine(capture, "marker-free-source-clip-" + kind + ".png") });
        }
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LinkedHeatmapCellHasOneKeyboardTargetAndRetainsNativeEnterNavigation(bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTitle("Service evidence")
            .WithTheme(dark ? ChartForgeX.Themes.ChartTheme.GraphiteDark() : ChartForgeX.Themes.ChartTheme.GraphiteLight())
            .WithXLabels("Directory")
            .WithStateCategories(new ChartStateCategory("pass", "Passed", ChartColor.FromHex("#1d8a52")))
            .AddHeatmapCategoryRow("DC01", new ChartHeatmapCell("pass", "3", href: "#evidence", tooltip: "Open evidence"));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        var cell = page.Locator("[data-cfx-role=\"heatmap-cell\"]");
        var link = cell.Locator("a[data-cfx-role=\"heatmap-cell-link\"]");
        Assert.Null(await cell.GetAttributeAsync("tabindex"));
        Assert.Equal(1, await cell.Locator("a[href], [tabindex='0']").CountAsync());
        await link.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await cell.GetAttributeAsync("aria-selected"));
        Assert.Equal("", await page.EvaluateAsync<string>("() => location.hash"));
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForFunctionAsync("() => location.hash === '#evidence'");
        Assert.Equal("#evidence", await page.EvaluateAsync<string>("() => location.hash"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions {
                Path = Path.Combine(capture, "heatmap-link-keyboard-" + (dark ? "dark" : "light") + ".png")
            });
        }
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MarkerFreeDecimatedObservationEmitsOneOriginalSourceSelectionAndSupportsKeyboard() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithLineMarkers(ChartLineMarkerMode.None)
            .AddDecimatedLine("Latency", Enumerable.Range(0, 100).Select(index => new ChartPoint(index, Math.Sin(index / 4d))), 12);
        chart.Series[0].WithInteractionKey("latency-source");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage());
        var page = session.Page;
        await page.EvaluateAsync("() => { window.cfxSelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.cfxSelections.push(event.detail)); }");
        var point = page.Locator(Point(0, 3));
        Assert.Equal("latency-source:" + chart.Series[0].SourcePointIndices[3], await point.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal(1, await point.Locator("[data-cfx-browser-hit-area]").CountAsync());
        await point.ClickAsync();
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        Assert.Equal("point", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.targetKind"));
        Assert.Equal(chart.Series[0].SourcePointIndices[3].ToString(), await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"));
        await point.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await point.GetAttributeAsync("aria-selected"));
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        AssertNoConsoleErrors(session);
    }
}
