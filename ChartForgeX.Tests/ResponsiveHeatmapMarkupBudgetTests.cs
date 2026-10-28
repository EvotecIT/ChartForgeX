using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using Xunit.Abstractions;

namespace ChartForgeX.Tests;

public sealed class ResponsiveHeatmapMarkupBudgetTests {
    private readonly ITestOutputHelper output;
    public ResponsiveHeatmapMarkupBudgetTests(ITestOutputHelper output) => this.output = output;

    private static Chart Fixture(int rows) {
        var colors = VisualTheme.Graphite().Resolve(VisualThemeMode.Light);
        var days = Enumerable.Range(0, 30).Select(day => new DateTime(2026, 9, 29).AddDays(day).ToString("yyyy-MM-dd")).ToArray();
        string[] groups = { "Domain controllers", "DNS", "Certificates" };
        var chart = Chart.Create().WithSize(1100, 240 + rows * 48 + 3 * 30).WithAxes().WithYAxisVisible().WithPadding(24, 40, 20, 70)
            .WithStateCategories(new ChartStateCategory("up", "Up", colors.Status.Pass.Fill), new ChartStateCategory("degraded", "Degraded", colors.Status.Medium.Fill, ChartStatePattern.Hatched),
                new ChartStateCategory("down", "Down", colors.Status.Critical.Fill, ChartStatePattern.CrossHatched), new ChartStateCategory("maintenance", "Maintenance", colors.Status.Info.Fill))
            .WithXLabels(days);
        for (var row = 0; row < rows; row++) {
            var target = $"dc{row + 1:00}.ad.evotec.xyz \u00b7 LDAP bind";
            var cells = Enumerable.Range(0, 30).Select(day => (row * 7 + day) % 13 == 0 ? (ChartHeatmapCell?)null
                : new ChartHeatmapCell((row + day) % 11 == 0 ? "down" : (row * day) % 9 == 4 ? "degraded" : day == 17 ? "maintenance" : "up",
                    tooltip: $"{days[day]} \u00b7 {((row + day) % 11 == 0 ? "42.0" : "100.0")}% available", href: $"#target-{row}-day-{day}"));
            chart.AddHeatmapCategoryRow(target, cells, groups[(row * 3 / rows) % 3]);
        }
        return chart;
    }

    [Fact]
    public void SerializedFixtureStaysWithinBudget() {
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(Fixture(12), 240, 1600);
        Assert.True(Encoding.UTF8.GetByteCount(fragment) <= 256 * 1024);
        Assert.Contains("chartforgex/layout-2", fragment);
    }

    [BrowserTheory]
    [InlineData(12)]
    [InlineData(150)]
    public async Task MountedFixturePreservesLinksNamesAndResponsiveLayout(int rows) {
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(Fixture(rows), 240, 1600);
        output.WriteLine($"{rows * 30} positions serialized: {Encoding.UTF8.GetByteCount(fragment)} bytes");
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body style='margin:0'><div id='panel' style='width:1440px'>" + fragment + "</div></body></html>", 1440, 900);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-responsive-chart]').cfxResponsive", null, new() { Timeout = 120000 });
        var metrics = await page.EvaluateAsync<int[]>("()=>{const host=document.querySelector('[data-cfx-responsive-chart]'),cells=[...host.querySelectorAll('[data-cfx-role=heatmap-cell]')];return [new TextEncoder().encode(host.outerHTML).length,cells.length,cells.filter(c=>c.matches('a[href]')&&c.querySelector(':scope > title')?.textContent.trim()).length]}");
        output.WriteLine($"{rows * 30} positions mounted: {metrics[0]} bytes; links and titles: {metrics[1]}/{metrics[2]}");
        Assert.Equal(rows * 30 - Enumerable.Range(0, rows).Sum(row => Enumerable.Range(0, 30).Count(day => (row * 7 + day) % 13 == 0)), metrics[1]);
        Assert.Equal(metrics[1], metrics[2]);
        if (rows == 12) {
            Assert.True(metrics[0] <= 256 * 1024, $"Mounted markup is {metrics[0]} bytes.");
            var named = page.GetByRole(Microsoft.Playwright.AriaRole.Link, new() { Name = "dc01.ad.evotec.xyz \u00b7 LDAP bind, 2026-09-30: Up. 2026-09-30 \u00b7 100.0% available", Exact = true });
            output.WriteLine("Links exposed: " + await page.GetByRole(Microsoft.Playwright.AriaRole.Link).CountAsync());
            output.WriteLine("First link accessibility: " + await page.Locator("[data-cfx-role=heatmap-cell]").First.AriaSnapshotAsync());
            Assert.Equal(1, await named.CountAsync());
            await named.FocusAsync();
            Assert.True(await named.EvaluateAsync<bool>("e=>document.activeElement===e"));
        }
        foreach (var width in new[] { 390, 1024, 1440 }) {
            await page.EvaluateAsync("w=>document.querySelector('#panel').style.width=w+'px'", width);
            await page.WaitForFunctionAsync("w=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===w", width, new() { Timeout = 120000 });
            if (rows == 12) {
                var clipped = await page.EvaluateAsync<int>("""
                    ()=>[...document.querySelectorAll('[data-cfx-role=heatmap-column-label] text')].filter(text=>{
                      if(getComputedStyle(text.closest('[data-cfx-role=heatmap-column-label]')).display==='none')return false;
                      const owner=text.closest('[clip-path]');if(!owner)return false;
                      const clip=text.ownerSVGElement.getElementById(owner.getAttribute('clip-path').slice(5,-1))?.querySelector('rect');
                      if(!clip)return false;const b=text.getBBox(),x=Number(clip.getAttribute('x')),w=Number(clip.getAttribute('width'));
                      return b.x<x-.5||b.x+b.width>x+w+.5;
                    }).length
                    """);
                Assert.True(clipped == 0, $"{clipped} column labels are clipped at {width}px.");
                if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } directory) {
                    Directory.CreateDirectory(directory);
                    await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "heatmap-responsive-" + width + ".png"), FullPage = true });
                }
            }
        }
        if (rows == 12 && Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } capture) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new() { Path = Path.Combine(capture, "heatmap-responsive-1440.png"), FullPage = true });
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RotatedCaptionsAndGeometryRemainStableAfterResizeAndRemount(bool reversed) {
        var chart = Fixture(12); chart.Options.XAxis.Reversed = reversed;
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600);
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body style='margin:0'><div id='panel' style='width:390px'>" + fragment + "</div></body></html>", 1440, 1000);
        var page = session.Page;
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===390");
        const string snapshot = """
            ()=>{const host=document.querySelector('[data-cfx-responsive-chart]'),cells=host.querySelectorAll('[data-cfx-role=heatmap-cell]');
              const box=cells[0].getBoundingClientRect();
              const labels=[...host.querySelectorAll('[data-cfx-role=heatmap-column-label]')].filter(e=>getComputedStyle(e).display!=='none');
              const bounds=labels.map(e=>e.getBoundingClientRect()).sort((a,b)=>a.left-b.left);
              return {geometry:[box.x,box.y,box.width,box.height], names:labels.map(e=>e.dataset.cfxSourceId),
                overlap:bounds.some((b,i)=>i&&b.left<bounds[i-1].right+7)};
            }
            """;
        var first = await page.EvaluateAsync<System.Text.Json.JsonElement>(snapshot);
        Assert.True(first.GetProperty("names").GetArrayLength() >= 2);
        Assert.False(first.GetProperty("overlap").GetBoolean());
        await page.EvaluateAsync("()=>document.querySelector('#panel').style.width='1440px'");
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===1440");
        await page.EvaluateAsync("()=>document.querySelector('#panel').style.width='390px'");
        await page.WaitForFunctionAsync("()=>Number(document.querySelector('[data-cfx-responsive-chart]').dataset.cfxLayoutWidth)===390");
        var resized = await page.EvaluateAsync<System.Text.Json.JsonElement>(snapshot);
        Assert.Equal(first.GetProperty("names").ToString(), resized.GetProperty("names").ToString());
        await page.EvaluateAsync("async()=>{const host=document.querySelector('[data-cfx-responsive-chart]');host.cfxResponsive.dispose();await window.ChartForgeXResponsive.mount(host);}");
        var remounted = await page.EvaluateAsync<System.Text.Json.JsonElement>(snapshot);
        Assert.Equal(first.GetProperty("names").ToString(), remounted.GetProperty("names").ToString());
        var before = first.GetProperty("geometry").EnumerateArray().Select(n=>n.GetDouble()).ToArray();
        var after = remounted.GetProperty("geometry").EnumerateArray().Select(n=>n.GetDouble()).ToArray();
        for (var i=0; i<before.Length; i++) Assert.Equal(before[i], after[i], .1);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ScriptFreePrintCanShrinkBelowTheScreenMinimum() {
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(Fixture(12), 480, 1600, printWidth: 1100);
        fragment = System.Text.RegularExpressions.Regex.Replace(fragment, "<script[^>]*>.*?</script>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body style='margin:0'><div style='width:320px'>" + fragment + "</div></body></html>", 400, 1000);
        await session.Page.EmulateMediaAsync(new() { Media = Microsoft.Playwright.Media.Print });
        var width = await session.Page.Locator("svg").EvaluateAsync<double>("s=>s.getBoundingClientRect().width");
        Assert.Equal(320, width, 1);
    }

    [BrowserFact]
    public async Task CompactPaintMatchesTheUnmodifiedBaseSvg() {
        var chart = Fixture(12);
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(chart, 240, 1600);
        chart.Options.XAxis = chart.Options.XAxis.Clone(); chart.Options.XAxis.LabelDensity = ChartLabelDensity.All;
        chart.Options.YAxis = chart.Options.YAxis.Clone(); chart.Options.YAxis.LabelDensity = ChartLabelDensity.All;
        chart.WithSize(1600, chart.Options.Size.Height);
        var raw = chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg(new VisualSvgOptions(null, chart.Options.SvgColorVariables).WithPatternTiles());
        var compact = System.Text.RegularExpressions.Regex.Match(fragment, "<svg.*?</svg>", System.Text.RegularExpressions.RegexOptions.Singleline).Value;
        const string style = "<style>body{margin:0}svg{display:block;width:1100px;height:auto}</style>";
        await using var session = await InteractiveChartBrowser.OpenAsync(style + raw, 1100, 1000);
        var expected = await session.Page.Locator("svg").ScreenshotAsync();
        await session.Page.SetContentAsync(style + compact);
        var actual = await session.Page.Locator("svg").ScreenshotAsync();
        var comparison = PixelBaseline.Compare("compact-heatmap", actual, expected, Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"));
        Assert.Null(comparison.Failure);
    }

    [BrowserFact]
    public async Task ScriptFreeFallbackFitsPrintWidthAndKeepsCellLinks() {
        var fragment = new HtmlResponsiveChartRenderer().RenderFragment(Fixture(12), 240, 1600);
        fragment = System.Text.RegularExpressions.Regex.Replace(fragment, "<script[^>]*>.*?</script>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        await using var session = await InteractiveChartBrowser.OpenAsync("<html><body style='margin:0'><div style='width:1440px'>" + fragment + "</div></body></html>", 1440, 1000);
        await session.Page.EmulateMediaAsync(new() { Media = Microsoft.Playwright.Media.Print });
        var size = await session.Page.Locator("svg").EvaluateAsync<double[]>("s=>{const r=s.getBoundingClientRect(), t=s.querySelector('text');return [r.width,r.height,parseFloat(getComputedStyle(t).fontSize)*s.getScreenCTM().a];}");
        Assert.Equal(1100, size[0], 1);
        Assert.True(size[2] >= 8, "The configured fallback scales its base text at the print width.");
        Assert.Equal(332, await session.Page.Locator("a[data-cfx-role=heatmap-cell] > title").CountAsync());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } capture) {
            Directory.CreateDirectory(capture);
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(capture, "heatmap-fallback-print.png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(capture, "heatmap-native.png"), Fixture(12).ToPng());
        }
    }
}
