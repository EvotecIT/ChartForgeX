using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class GaugeScalePrecisionBrowserTests {
    [Theory]
    [InlineData(false, 0d, 360, 360)]
    [InlineData(false, .1, 360, 360)]
    [InlineData(false, .9, 360, 360)]
    [InlineData(false, 1d, 360, 360)]
    [InlineData(true, 0d, 360, 360)]
    [InlineData(true, .1, 360, 360)]
    [InlineData(true, .9, 360, 360)]
    [InlineData(true, 1d, 360, 360)]
    [InlineData(false, 0d, 800, 440)]
    [InlineData(false, .1, 800, 440)]
    [InlineData(false, .9, 800, 440)]
    [InlineData(false, 1d, 800, 440)]
    [InlineData(true, 0d, 800, 440)]
    [InlineData(true, .1, 800, 440)]
    [InlineData(true, .9, 800, 440)]
    [InlineData(true, 1d, 800, 440)]
    public async Task NeedleAtScaleEdgesClearsMeasuredSummaryInRenderedSvg(bool dark, double ratio, int width, int height) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width, height).WithLegend(false).WithTitle("Range")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .AddGauge("Tolerance", 1_000_001 + ratio * 4, 1_000_001, 1_000_005).WithGauge(options => options.Form = ChartGaugeForm.Needle);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = "<!doctype html><html><body style='margin:24px;background:" + (dark ? "#111827" : "#fff") + "'>"
            + prepared.ToSvg() + "<img style='display:block' width='" + width + "' height='" + height + "' src='data:image/png;base64," + Convert.ToBase64String(prepared.ToPng()) + "'></body></html>";
        await using var session = await OpenAsync(html, width + 48, height * 2 + 96);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; await document.querySelector('img').decode(); }");
        var overlap = await session.Page.EvaluateAsync<bool>("""
            () => {
                const svg = document.querySelector('svg'), line = svg.querySelector('[data-cfx-role="gauge-needle"]');
                const x0 = line.x1.baseVal.value, y0 = line.y1.baseVal.value;
                const dx = line.x2.baseVal.value - x0, dy = line.y2.baseVal.value - y0;
                const margin = Number(line.getAttribute('stroke-width')) / 2;
                return ['gauge-label', 'gauge-title'].some(role => {
                    const label = svg.querySelector('[data-cfx-role="' + role + '"]');
                    if (!label || !label.textContent.trim()) throw new Error('A gauge summary row was omitted');
                    for (const text of label.querySelectorAll('text'))
                        if (parseFloat(getComputedStyle(text).fontSize) < 10) throw new Error('A gauge summary row is microscopic');
                    const box = label.getBBox(); let low = 0, high = 1;
                    for (const [p, q] of [[-dx,x0-box.x+margin],[dx,box.x+box.width+margin-x0],[-dy,y0-box.y+margin],[dy,box.y+box.height+margin-y0]]) {
                        if (Math.abs(p) < 1e-10) { if (q < 0) return false; }
                        else { const bound = q / p; if (p < 0) low = Math.max(low,bound); else high = Math.min(high,bound); }
                    }
                    return low <= high;
                });
            }
            """);
        Assert.False(overlap, "The rendered Needle stroke must clear the value and caption glyph boxes.");
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(output, "needle-clearance-" + ratio + "-" + width + "-" + (dark ? "dark" : "light") + ".png"), FullPage = true });
        }
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(ChartGaugeForm.Arc, false, 800, 440)]
    [InlineData(ChartGaugeForm.Needle, false, 800, 440)]
    [InlineData(ChartGaugeForm.Linear, false, 800, 440)]
    [InlineData(ChartGaugeForm.Arc, true, 360, 360)]
    [InlineData(ChartGaugeForm.Needle, true, 360, 360)]
    [InlineData(ChartGaugeForm.Linear, true, 360, 360)]
    public async Task CloseGaugeBoundsRemainDistinctAndContainedInNativeSvgAndPairedPng(ChartGaugeForm form, bool dark, int width, int height) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width, height).WithLegend(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Measured range").AddGauge("Tolerance", 1.003, 1.001, 1.005).WithGauge(options => { options.Form = form; options.Target = 1.0025; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var background = dark ? "#111827" : "#fff";
        var html = "<!doctype html><html><body style=\"margin:24px;background:" + background + ";color:" + (dark ? "#fff" : "#111827") + "\">SVG"
            + prepared.ToSvg() + "PNG<img style=\"display:block\" width=\"" + width + "\" height=\"" + height + "\" src=\"data:image/png;base64," + Convert.ToBase64String(prepared.ToPng()) + "\"></body></html>";
        await using var session = await OpenAsync(html, width + 48, height * 2 + 96);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; await document.querySelector('img').decode(); }");
        var result = await session.Page.EvaluateAsync<string[]>("""
            () => {
                const svg = document.querySelector('svg'), bounds = svg.getBoundingClientRect();
                const image = document.querySelector('img');
                if (image.naturalWidth !== Number(svg.getAttribute('width')) || image.naturalHeight !== Number(svg.getAttribute('height')))
                    throw new Error('The paired native PNG dimensions changed');
                const labels = [...svg.querySelectorAll('[data-cfx-role]')].filter(node => /^gauge-(min-label|max-label|tick-label-[1-3])$/.test(node.dataset.cfxRole));
                for (const label of labels) {
                    const box = label.getBoundingClientRect();
                    if (box.left < bounds.left - 1 || box.right > bounds.right + 1 || box.top < bounds.top - 1 || box.bottom > bounds.bottom + 1)
                        throw new Error('Gauge scale caption escapes its native SVG');
                }
                const caption = svg.querySelector('[data-cfx-role="gauge-title"]');
                if (!caption || !caption.textContent.trim()) throw new Error('The gauge caption is missing');
                for (const text of caption.querySelectorAll('text'))
                    if (parseFloat(getComputedStyle(text).fontSize) < 10) throw new Error('The gauge caption is microscopic');
                return labels.map(label => label.textContent);
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new() { Path = Path.Combine(output, "gauge-" + form.ToString().ToLowerInvariant() + "-" + width + ".png"), FullPage = true });
        }
        Assert.Equal(form == ChartGaugeForm.Linear ? new[] { "1.001", "1.002", "1.003", "1.004", "1.005" } : new[] { "1.001", "1.005" }, result);
        Assert.Equal("1.003", await session.Page.Locator("[data-cfx-role=gauge-label]").TextContentAsync());
        Assert.Equal("1.0025", await session.Page.Locator("[data-cfx-role=gauge-target-label]").TextContentAsync());
        AssertNoConsoleErrors(session);
    }
}
