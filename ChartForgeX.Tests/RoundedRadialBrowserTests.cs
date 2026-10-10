using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RoundedRadialBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.RadialBar, false, false)]
    [InlineData(ChartSeriesKind.RadialBar, false, true)]
    [InlineData(ChartSeriesKind.RadialBar, true, false)]
    [InlineData(ChartSeriesKind.RadialBar, true, true)]
    [InlineData(ChartSeriesKind.RadialColumn, false, false)]
    [InlineData(ChartSeriesKind.RadialColumn, false, true)]
    [InlineData(ChartSeriesKind.RadialColumn, true, false)]
    [InlineData(ChartSeriesKind.RadialColumn, true, true)]
    [InlineData(ChartSeriesKind.Sunburst, false, false)]
    [InlineData(ChartSeriesKind.Sunburst, false, true)]
    [InlineData(ChartSeriesKind.Sunburst, true, false)]
    [InlineData(ChartSeriesKind.Sunburst, true, true)]
    public async Task GalleryRoundedCutawaysAndPaintedInteriorsRetainNativePointerAndKeyboardTargets(ChartSeriesKind kind, bool compact, bool dark) {
        if (!Enabled) return;
        var width = compact ? 360 : 800; var height = compact ? 360 : 440;
        var chart = V2GalleryModels.Create(kind, compact ? "compact-options" : "options", dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(width, height).WithTitle(V2GalleryModels.Title(kind)).WithSubtitle("Rounded sectors retain source values")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        RoundedRadialSeriesTests.UseFixtureFont(chart);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var sunburst = kind == ChartSeriesKind.Sunburst;
        var owner = prepared.Scene.Nodes.OfType<VisualSceneGroup>().First(group => sunburst
            ? group.Role == "sunburst-segment" && group.Metadata["data-cfx-node"] == "platform" : group.Id == "series-0-point-0");
        double Fact(string key) => double.Parse(owner.Metadata[key], CultureInfo.InvariantCulture);
        var outer = Fact("data-cfx-outer-radius"); var inner = Fact("data-cfx-inner-radius");
        var start = Fact("data-cfx-start-angle"); var sweep = Fact(sunburst ? "data-cfx-sweep" : "data-cfx-sweep-angle");
        var bounds = prepared.Regions.Single(region => region.Id == owner.Id).Bounds;
        var relative = ChartSlicePathGeometry.Bounds(0, 0, outer, inner, start, sweep);
        var cx = sunburst ? bounds.Left + outer : bounds.Left - relative.Left;
        var cy = sunburst ? bounds.Top + outer : bounds.Top - relative.Top;
        var middle = start + sweep / 2; var radius = (outer + inner) / 2;
        var interior = new { x = cx + Math.Cos(middle) * radius, y = cy + Math.Sin(middle) * radius };
        var corner = new { x = cx + Math.Cos(start + .25 / outer) * (outer - .25), y = cy + Math.Sin(start + .25 / outer) * (outer - .25) };
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        var font = Convert.ToBase64String(await File.ReadAllBytesAsync(chart.Options.PngFontPath!));
        html = html.Replace("</head>", "<style>@font-face{font-family:'Carlito';src:url(data:font/ttf;base64," + font + ")}</style></head>", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, width + 24, height + 150);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => document.fonts.ready");
        var target = page.Locator(sunburst ? "[data-cfx-role='sunburst-segment'][data-cfx-node='platform']" : Point(0, 0));
        var shape = target.Locator("path[data-cfx-role='" + (sunburst ? "sunburst-segment-mark" : kind == ChartSeriesKind.RadialBar ? "radial-bar" : "radial-column") + "']");
        Assert.True(await shape.EvaluateAsync<bool>("(node, point) => node.isPointInFill(new DOMPoint(point.x,point.y))", interior));
        Assert.False(await shape.EvaluateAsync<bool>("(node, point) => node.isPointInFill(new DOMPoint(point.x,point.y))", corner));
        var cutaway = await shape.EvaluateAsync<double[]>("(node, point) => { const p=new DOMPoint(point.x,point.y).matrixTransform(node.getScreenCTM()); return [p.x,p.y]; }", corner);
        Assert.False(await target.EvaluateAsync<bool>("(node, point) => document.elementFromPoint(point[0],point[1])?.closest('[data-cfx-role=point],[data-cfx-role=sunburst-segment]') === node", cutaway));
        var painted = await shape.EvaluateAsync<double[]>("(node, point) => { const p=new DOMPoint(point.x,point.y).matrixTransform(node.getScreenCTM()); return [p.x,p.y]; }", interior);
        await page.Mouse.MoveAsync((float)painted[0], (float)painted[1]);
        Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
        await page.Mouse.ClickAsync((float)painted[0], (float)painted[1]);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await target.GetAttributeAsync("aria-selected"));
        await page.EvaluateAsync("() => document.activeElement?.blur()"); await MoveAwayAsync(page);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var stem = "rounded-" + kind.ToString().ToLowerInvariant() + "-" + (compact ? "compact" : "wide") + "-" + (dark ? "dark" : "light");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, stem + "-browser.png") });
            await File.WriteAllTextAsync(Path.Combine(capture, stem + ".html"), html);
            await File.WriteAllTextAsync(Path.Combine(capture, stem + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(capture, stem + "-native.png"), prepared.ToPng());
            await File.WriteAllTextAsync(Path.Combine(capture, stem + ".json"), JsonSerializer.Serialize(new { kind = kind.ToString(), compact, dark,
                source = owner.Metadata, interior, corner, pageErrors = errors, consoleErrors = session.ConsoleLog.Where(entry => entry.Type.ToString() == "Error") },
                new JsonSerializerOptions { WriteIndented = true }));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
