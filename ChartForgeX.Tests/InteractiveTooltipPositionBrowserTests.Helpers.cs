using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    private const string Reading = PointTarget + "[data-cfx-series=\"0\"][data-cfx-point=\"0\"]";
    private static Chart Observations(bool dark) => Chart.Create().WithSize(800, 440).WithTitle("Tooltip screen placement")
        .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
        .AddScatter("Reading", new[] { new ChartPoint(3, 5), new ChartPoint(7, 6), new ChartPoint(9.5, 2) });

    private static async Task<double[]> PointerAsync(IPage page, string selector = Reading, double offsetX = 0, double offsetY = 0) {
        var box = await BoxAsync(page, selector);
        var position = new[] { box.X + box.Width / 2 + offsetX, box.Y + box.Height / 2 + offsetY };
        await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        return position;
    }

    private static Task CentreReadableTargetAsync(IPage page, string selector = Reading) => page.Locator(selector).EvaluateAsync("node => { const stage=node.closest('.cfx-stage'), b=node.getBoundingClientRect(), s=stage.getBoundingClientRect(); stage.scrollLeft += b.x+b.width/2-s.x-s.width/2; }");
    private static Task WaitForTipAsync(IPage page) => page.WaitForFunctionAsync("() => !document.querySelector('.cfx-tooltip').hidden", null, new PageWaitForFunctionOptions { Timeout = 5000 });
    private static Task<bool> TipHiddenAsync(IPage page) => page.Locator(".cfx-tooltip").First.EvaluateAsync<bool>("tip => tip.hidden");

    private static Task<JsonElement> GeometryAsync(IPage page, string selector = Reading, bool hosted = false) => page.EvaluateAsync<JsonElement>("""
        ({selector,hosted}) => {
            const node=(hosted?window.positionFocusChart:document).querySelector(selector), root=node.closest('.cfx-interactive-chart'), tip=root.querySelector('.cfx-tooltip');
            const rect=element=>{const b=element.getBoundingClientRect();return {x:b.left,y:b.top,width:b.width,height:b.height};};
            return {tip:rect(tip),node:rect(node),stage:rect(root.querySelector('.cfx-stage')),viewport:{width:innerWidth,height:innerHeight},
                hidden:tip.hidden,pinned:root.dataset.cfxTooltipPinned==='true',text:tip.innerText,css:{left:tip.style.left,top:tip.style.top}};
        }
        """, new { selector, hosted });

    private static async Task<JsonElement> AssertPositionAsync(IPage page, HtmlChartTooltipAnchor anchor, HtmlChartTooltipPlacement[] placements,
        double gap = 14, double offsetX = 0, double offsetY = 0, double[]? pointer = null, string selector = Reading, bool hosted = false) {
        var geometry = await GeometryAsync(page, selector, hosted);
        Assert.False(geometry.GetProperty("hidden").GetBoolean());
        var tip = ScreenRect.Read(geometry.GetProperty("tip"));
        var node = ScreenRect.Read(geometry.GetProperty("node"));
        var bounds = anchor == HtmlChartTooltipAnchor.Chart ? ScreenRect.Read(geometry.GetProperty("stage")) : anchor == HtmlChartTooltipAnchor.Node
            ? node : new ScreenRect(pointer?[0] ?? node.X + node.Width / 2, pointer?[1] ?? node.Y + node.Height / 2, 0, 0);
        var viewport = geometry.GetProperty("viewport");
        var width = viewport.GetProperty("width").GetDouble(); var height = viewport.GetProperty("height").GetDouble();
        var candidates = placements.Select(placement => Candidate(bounds, tip, placement, gap, offsetX, offsetY)).ToArray();
        var chosen = candidates.FirstOrDefault(candidate => candidate.X >= 8 && candidate.Y >= 8 && candidate.X + tip.Width <= width - 8 && candidate.Y + tip.Height <= height - 8, candidates[0]);
        var x = Math.Max(8, Math.Min(Math.Max(8, width - tip.Width - 8), chosen.X));
        var y = Math.Max(8, Math.Min(Math.Max(8, height - tip.Height - 8), chosen.Y));
        Assert.InRange(tip.X, x - .15, x + .15);
        Assert.InRange(tip.Y, y - .15, y + .15);
        return geometry;
    }

    private static (double X, double Y) Candidate(ScreenRect anchor, ScreenRect tip, HtmlChartTooltipPlacement placement, double gap, double x, double y) => placement switch {
        HtmlChartTooltipPlacement.Top => (anchor.X + (anchor.Width - tip.Width) / 2 + x, anchor.Y - tip.Height - gap + y),
        HtmlChartTooltipPlacement.TopRight => (anchor.X + anchor.Width + gap + x, anchor.Y - tip.Height - gap + y),
        HtmlChartTooltipPlacement.Right => (anchor.X + anchor.Width + gap + x, anchor.Y + (anchor.Height - tip.Height) / 2 + y),
        HtmlChartTooltipPlacement.BottomRight => (anchor.X + anchor.Width + gap + x, anchor.Y + anchor.Height + gap + y),
        HtmlChartTooltipPlacement.Bottom => (anchor.X + (anchor.Width - tip.Width) / 2 + x, anchor.Y + anchor.Height + gap + y),
        HtmlChartTooltipPlacement.BottomLeft => (anchor.X - tip.Width - gap + x, anchor.Y + anchor.Height + gap + y),
        HtmlChartTooltipPlacement.Left => (anchor.X - tip.Width - gap + x, anchor.Y + (anchor.Height - tip.Height) / 2 + y),
        _ => (anchor.X - tip.Width - gap + x, anchor.Y - tip.Height - gap + y)
    };

    private readonly record struct ScreenRect(double X, double Y, double Width, double Height) {
        internal static ScreenRect Read(JsonElement value) => new(value.GetProperty("x").GetDouble(), value.GetProperty("y").GetDouble(), value.GetProperty("width").GetDouble(), value.GetProperty("height").GetDouble());
    }

    private static async Task CaptureAsync(IPage page, string name, object observations, HtmlBrowserSession? session = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(observations));
        if (session is not null) await File.WriteAllTextAsync(Path.Combine(directory, name + ".console.json"), JsonSerializer.Serialize(session.ConsoleLog));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}
