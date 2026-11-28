using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>
/// Sustained-update soak for the browser data renderer: a two-series, 1,000-point sliding window receives one period
/// per update. By default the updates run back to back (fast-forward). Set <c>CFX_SOAK_REALTIME=1</c> to pace them at
/// one per second, <c>CFX_SOAK_UPDATES</c> to change the count (default 1,800, thirty minutes at 1 Hz) and
/// <c>CFX_SOAK_OUTPUT</c> to keep the measurements as JSON. Timing and memory budgets are local evidence, so this is
/// Category=Performance and normal CI leaves it out.
/// </summary>
public sealed class DataRendererSoakBrowserTests {
    private const int WindowSize = 1000;
    private readonly ITestOutputHelper output;

    public DataRendererSoakBrowserTests(ITestOutputHelper output) => this.output = output;

    [BrowserFact]
    [Trait("Category", "Performance")]
    public async Task SlidingWindowSustainsUpdatesWithBoundedMemoryAndRetainedState() {
        if (!Enabled || Engine != HtmlBrowserEngine.Chromium) return; // Heap and listener metrics come from the Chromium protocol.
        var updates = int.TryParse(Environment.GetEnvironmentVariable("CFX_SOAK_UPDATES"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) && count > 0 ? count : 1800;
        var realtime = Environment.GetEnvironmentVariable("CFX_SOAK_REALTIME") == "1";
        await using var session = await OpenAsync(DataRendererStreamingBrowserTests.Page, 1000, 520);
        var page = session.Page;
        await page.EvaluateAsync(DataRendererStreamingBrowserTests.Window, WindowSize);
        await page.EvaluateAsync("()=>{const mark=document.querySelector(\"[data-cfx-series='b'][data-cfx-category='t999']\");mark.focus();mark.dispatchEvent(new MouseEvent('click',{bubbles:true}));}");
        var cdp = await page.Context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Performance.enable");
        var start = await MetricsAsync(cdp);

        var commits = new double[updates];
        var clock = Stopwatch.StartNew();
        for (var index = 0; index < updates; index++) {
            if (realtime) {
                var due = TimeSpan.FromSeconds(index) - clock.Elapsed;
                if (due > TimeSpan.Zero) await Task.Delay(due);
            }
            // A commit is the append plus the style and layout it forces, measured inside the page.
            commits[index] = await page.EvaluateAsync<double>("index=>{const started=performance.now();chart.append(tick(index),{maximum:"
                + WindowSize + "});document.body.offsetHeight;return performance.now()-started;}", WindowSize + index);
            if ((index + 1) % 300 == 0) await AssertRetainedStateAsync(page, WindowSize + index);
        }
        clock.Stop();
        var end = await MetricsAsync(cdp);

        var sorted = commits.OrderBy(value => value).ToArray();
        var p95 = sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * .95) - 1)];
        var result = new {
            updates, realtime, elapsedSeconds = Math.Round(clock.Elapsed.TotalSeconds, 1),
            commitMedianMs = Math.Round(sorted[sorted.Length / 2], 1), commitP95Ms = Math.Round(p95, 1), commitMaxMs = Math.Round(sorted[^1], 1),
            heapStartMiB = Math.Round(start.Heap / 1048576, 2), heapEndMiB = Math.Round(end.Heap / 1048576, 2),
            domNodesStart = start.Nodes, domNodesEnd = end.Nodes, listenersStart = start.Listeners, listenersEnd = end.Listeners
        };
        var json = JsonSerializer.Serialize(result);
        output.WriteLine(json);
        var keep = Environment.GetEnvironmentVariable("CFX_SOAK_OUTPUT");
        if (!string.IsNullOrWhiteSpace(keep)) File.WriteAllText(keep, json);

        Assert.True(p95 <= 250, $"Commit p95 was {p95:0.0} ms.");
        Assert.Equal(start.Listeners, end.Listeners);
        Assert.InRange(end.Nodes, 0, start.Nodes + 100);
        Assert.True(end.Heap <= start.Heap * 1.5, $"Heap grew from {start.Heap / 1048576:0.0} to {end.Heap / 1048576:0.0} MiB.");
    }

    // Every 300 updates: two series of exactly one window, the newest period last, focus on a DC2 mark and at most one
    // selection, then select the newest DC1 point so the next checks follow a live selection.
    private static async Task AssertRetainedStateAsync(IPage page, int newest) {
        var state = await page.EvaluateAsync<string>("""
            newest => {
              const marks = document.querySelectorAll('#chart [data-cfx-category]'), active = document.activeElement;
              const pressed = [...document.querySelectorAll("#chart [aria-pressed='true']")];
              const selection = chart.selection(), consistent = pressed.length === (selection ? 1 : 0)
                && (!selection || pressed[0].dataset.cfxCategory === selection.categoryId && pressed[0].dataset.cfxSeries === selection.seriesId);
              const state = [marks.length, marks[marks.length - 1].dataset.cfxCategory === 't' + newest,
                active.dataset.cfxSeries, consistent].join('|');
              document.querySelector("[data-cfx-series='a'][data-cfx-category='t" + newest + "']").dispatchEvent(new MouseEvent('click', { bubbles: true }));
              return state;
            }
            """, newest);
        Assert.Equal($"{2 * WindowSize}|true|b|true", state);
    }

    private static async Task<(double Heap, int Nodes, int Listeners)> MetricsAsync(ICDPSession cdp) {
        await cdp.SendAsync("HeapProfiler.collectGarbage");
        var metrics = (await cdp.SendAsync("Performance.getMetrics"))!.Value.GetProperty("metrics").EnumerateArray()
            .ToDictionary(item => item.GetProperty("name").GetString()!, item => item.GetProperty("value").GetDouble());
        return (metrics["JSHeapUsedSize"], (int)metrics["Nodes"], (int)metrics["JSEventListeners"]);
    }
}
