using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    private static Chart Observations(bool dark) => Chart.Create().WithSize(800, 540).WithLegend(true).WithTitle("Pointer readout pacing")
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
        .AddScatter("Reading", new[] { new ChartPoint(3, 5), new ChartPoint(7, 6) })
        .AddScatter("Companion", new[] { new ChartPoint(3, 2), new ChartPoint(7, 4) });

    private static void ConfigureDelay(HtmlChartInteractionOptions options, string range) {
        options.Tooltip.DelayMilliseconds = Delay;
        options.Tooltip.Range = range == "exact" ? HtmlChartTooltipRange.Exact : range == "nearest" ? HtmlChartTooltipRange.Nearest : HtmlChartTooltipRange.WithinDistance(60);
    }

    private static Task<bool> TipHiddenAsync(IPage page) => page.Locator(".cfx-tooltip").First.EvaluateAsync<bool>("tip => tip.hidden");
    private static Task WaitForTipAsync(IPage page) => page.WaitForFunctionAsync("() => !document.querySelector('.cfx-tooltip').hidden", null, new PageWaitForFunctionOptions { Timeout = 5000 });
    private static Task<JsonElement> TraceStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("() => window.delayTrace");

    private static async Task<double[]> PointerAsync(IPage page, string selector) {
        var box = await BoxAsync(page, selector);
        var position = new double[] { box.X + box.Width / 2, box.Y + box.Height / 2 };
        await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        return position;
    }

    private static Task CentreReadableTargetAsync(IPage page, string selector) => page.Locator(selector).EvaluateAsync("node => { const stage=node.closest('.cfx-stage'), b=node.getBoundingClientRect(), s=stage.getBoundingClientRect(); stage.scrollLeft += b.x+b.width/2-s.x-s.width/2; }");

    private static Task TraceAsync(IPage page) => page.EvaluateAsync("""
        () => {
            const root=document.querySelector('.cfx-interactive-chart'), tip=root.querySelector('.cfx-tooltip');
            const trace=window.delayTrace={ entry:[], hover:[], shown:[], guide:[], sync:[], pointer:[] };
            root.querySelectorAll('[data-cfx-target-kind][data-cfx-target-id]').forEach(node => node.addEventListener('pointerenter',e => {
                trace.entry.push({t:performance.now(),id:node.dataset.cfxTargetId,hidden:tip.hidden});
            }));
            document.addEventListener('pointermove',e => trace.pointer=[e.clientX,e.clientY]);
            root.addEventListener('cfxhover',e => trace.hover.push({t:performance.now(),target:e.detail.target}));
            root.addEventListener('cfxcrosshair',e => trace.guide.push({t:performance.now(),target:e.detail.target}));
            root.addEventListener('cfxsync',e => trace.sync.push({t:performance.now(),action:e.detail.action,target:e.detail.target}));
            new MutationObserver(() => { if(!tip.hidden) trace.shown.push({t:performance.now(),text:tip.innerText}); })
                .observe(tip,{attributes:true,attributeFilter:['hidden']});
        }
        """);

    private static void AssertDelay(JsonElement trace) {
        var elapsed = trace.GetProperty("shown")[0].GetProperty("t").GetDouble() - trace.GetProperty("hover")[0].GetProperty("t").GetDouble();
        Assert.InRange(elapsed, Delay - 4, 5000);
    }

    private static async Task CaptureDelayAsync(IPage page, string name, object observations, HtmlBrowserSession? session = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(observations));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
        if (session != null) await File.WriteAllTextAsync(Path.Combine(directory, name + ".console.json"), JsonSerializer.Serialize(session.ConsoleLog));
    }
}
