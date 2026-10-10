using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class SankeyLabelPlacementBrowserTests {
    [Theory]
    [InlineData(ChartSankeyLabelPlacement.Left, false, 360)]
    [InlineData(ChartSankeyLabelPlacement.Left, true, 360)]
    [InlineData(ChartSankeyLabelPlacement.Left, false, 800)]
    [InlineData(ChartSankeyLabelPlacement.Left, true, 800)]
    [InlineData(ChartSankeyLabelPlacement.Right, false, 360)]
    [InlineData(ChartSankeyLabelPlacement.Right, true, 360)]
    [InlineData(ChartSankeyLabelPlacement.Right, false, 800)]
    [InlineData(ChartSankeyLabelPlacement.Right, true, 800)]
    [InlineData(ChartSankeyLabelPlacement.Center, false, 360)]
    [InlineData(ChartSankeyLabelPlacement.Center, true, 360)]
    [InlineData(ChartSankeyLabelPlacement.Center, false, 800)]
    [InlineData(ChartSankeyLabelPlacement.Center, true, 800)]
    public async Task MeasuredLabelPoliciesKeepNativeCaptionsAndAuthoredPointerKeyboardFacts(ChartSankeyLabelPlacement placement, bool dark, int width) {
        if (!Enabled) return;
        var chart = V2GalleryModels.Create(ChartSeriesKind.Sankey, width < 500 ? "compact-options" : "options")
            .WithSize(width, width < 500 ? 360 : 440).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Requests across processing stages").WithSubtitle("Measured node captions")
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
            .ConfigureSankey(options => {
                options.LabelPlacement = placement;
                options.EdgeLabelPlacement = placement == ChartSankeyLabelPlacement.Left ? null
                    : placement == ChartSankeyLabelPlacement.Right ? ChartSankeyEdgeLabelPlacement.Inside : ChartSankeyEdgeLabelPlacement.Outside;
            });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync(html, width + 24, width < 500 ? 630 : 640);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        string name = "sankey-label-" + placement + "-" + width + "-" + (dark ? "dark" : "light");
        await Capture(page, prepared, html, name, "chart", errors, session);
        Assert.Equal(8, await page.Locator("[data-cfx-target-kind=node]").CountAsync());
        Assert.Equal(9, await page.Locator("[data-cfx-target-kind=link]").CountAsync());
        Assert.Equal(prepared.Scene.Nodes.Count(node => node.Role == "sankey-node-label"), await page.Locator("[data-cfx-role=sankey-node-label]").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => { const svg=document.querySelector('.cfx-stage svg'), bounds=svg.getBoundingClientRect();
                const labels=Array.from(svg.querySelectorAll('[data-cfx-role=sankey-node-label]')).map(node=>node.getBoundingClientRect());
                return labels.length>0 && labels.every(box=>box.left>=bounds.left-1 && box.right<=bounds.right+1
                    && box.top>=bounds.top-1 && box.bottom<=bounds.bottom+1); }
            """));
        var support = page.Locator("[data-cfx-target-kind=node][data-cfx-target-id=south-support]");
        var caption = page.Locator("[data-cfx-label-for=series-0-node-south-support]");
        await caption.HoverAsync();
        Assert.Contains("Support", await TooltipTextAsync(page)); Assert.Contains("28", await TooltipTextAsync(page));
        await Capture(page, prepared, html, name, "caption", errors, session);
        await MoveAwayAsync(page);
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        var box = (await support.BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        Assert.Equal("true", await support.GetAttributeAsync("aria-selected"));
        Assert.Contains("Support", await TooltipTextAsync(page)); Assert.Contains("28", await TooltipTextAsync(page));
        await Capture(page, prepared, html, name, "pointer", errors, session);
        await caption.ClickAsync();
        Assert.Equal("false", await support.GetAttributeAsync("aria-selected"));
        var flow = page.Locator("[data-cfx-target-kind=link][data-cfx-target-id=north-priority]");
        await flow.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await flow.GetAttributeAsync("aria-selected"));
        Assert.Equal("20", await flow.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("north", await flow.GetAttributeAsync("data-cfx-source"));
        Assert.Equal("north-support", await flow.GetAttributeAsync("data-cfx-target"));
        Assert.Contains("20", await TooltipTextAsync(page));
        await Capture(page, prepared, html, name, "keyboard", errors, session);
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static async Task Capture(IPage page, PreparedVisual prepared, string html, string name, string state, List<string> errors, HtmlBrowserSession session) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        if (state == "chart") {
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), html);
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, name + ".native.png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        }
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + "-" + state + ".png") });
        await File.WriteAllTextAsync(Path.Combine(directory, name + "-" + state + ".runtime.json"),
            JsonSerializer.Serialize(new { errors, console = session.ConsoleLog.Select(entry => entry.Type + ": " + entry.Text) }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
