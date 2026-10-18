using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RuntimeLinkedForegroundHoverBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Donut, true, false, 1440)]
    [InlineData(ChartSeriesKind.Donut, true, true, 390)]
    [InlineData(ChartSeriesKind.Pie, true, false, 390)]
    [InlineData(ChartSeriesKind.Pie, true, true, 1440)]
    [InlineData(ChartSeriesKind.Donut, false, false, 1440)]
    [InlineData(ChartSeriesKind.Donut, false, true, 390)]
    [InlineData(ChartSeriesKind.Bar, true, false, 1440)]
    [InlineData(ChartSeriesKind.Bar, true, true, 390)]
    public async Task SelectedAndHighlightedMarksKeepNativeForegroundWhenAnotherMarkIsHovered(
        ChartSeriesKind kind, bool graphite, bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Create(kind, graphite, dark).ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation);
            options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
            options.Highlight.DimOthersWhileSelected = true;
            options.Tooltip.PinOnSelect = false;
            options.Toolbar = HtmlChartToolbarLayout.Compact;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        }), width, 680);
        var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("""
            () => {
                window.foregroundRoot=document.querySelector('.cfx-interactive-chart');
                window.foregroundController=ChartForgeXCharts.get(foregroundRoot);
                window.foregroundTargets=['a','b','c'].map(id=>foregroundController.resolveTargets({observationIds:[id]})[0]);
                window.foregroundPaint=foregroundRoot.querySelector('[data-cfx-point="1"]')
                    .querySelector('[data-cfx-role="bar"],[data-cfx-role="donut-slice"],[data-cfx-role="pie-slice"]');
            }
            """);
        var nativePaint = await NativePaint(page);
        Assert.Equal("rgba(40,120,200,0.651)", nativePaint[0]);
        await page.EvaluateAsync("()=>{foregroundController.select({observationIds:['a']});foregroundController.highlight({observationIds:['b']},{dim:true});}");
        var identities = await page.EvaluateAsync<string[]>("()=>foregroundTargets.map(target=>target.targetId)");
        Assert.Equal(new[] { "1", "1", "0.3" }, await Opacities(page));
        await MoveToAsync(page, Mark(2));
        var hovered = await State(page);
        await Capture(session, kind, graphite, dark, width, "hover", hovered, errors);
        Assert.Contains("cfx-hovered", hovered.GetProperty("marks")[2].GetProperty("classes").GetString()!.Split(' '));
        Assert.Equal(new[] { "1", "1", "0.3" }, await Opacities(page));
        Assert.Equal(identities[0], Assert.Single(hovered.GetProperty("selection").EnumerateArray()).GetString());
        Assert.Equal(identities[1], Assert.Single(hovered.GetProperty("highlights").EnumerateArray()).GetString());
        Assert.Equal(nativePaint, await NativePaint(page));

        await MoveAwayAsync(page);
        await page.EvaluateAsync("()=>{foregroundController.clearSelection();foregroundController.clearHighlight();}");
        Assert.Equal(new[] { "1", "1", "1" }, await Opacities(page));
        Assert.Equal(nativePaint, await NativePaint(page));
        Assert.Empty(await page.EvaluateAsync<string[]>("()=>foregroundController.getSelection().map(target=>target.targetId)"));
        Assert.Equal(0, await page.Locator("[data-cfx-highlighted=true],.cfx-selection-dim,.cfx-linked-dim").CountAsync());
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);
    }

    private static Chart Create(ChartSeriesKind kind, bool graphite, bool dark) {
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", kind,
            new[] { new ChartPoint(1, 7), new ChartPoint(2, 3), new ChartPoint(3, 2) })
            .WithPointColor(1, ChartColor.FromRgb(40, 120, 200).WithOpacity(.65))
            .WithObservationIds(new[] { new ChartObservationId("a"), new ChartObservationId("b"), new ChartObservationId("c") });
        var chart = Chart.Create().WithSize(640, 360).WithTitle("Linked foreground")
            .WithTheme(graphite ? VisualTheme.Graphite() : VisualTheme.Report(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXLabels("A", "B", "C").WithPointLegend().AddSeries(series);
        return kind == ChartSeriesKind.Bar ? chart.WithBarStyle(ChartBarStyle.Flat) : chart;
    }

    private static string Mark(int index) => "[data-cfx-target-kind='point'][data-cfx-series='0'][data-cfx-point='" + index + "']";
    private static Task<string[]> Opacities(IPage page) => page.EvaluateAsync<string[]>("""
        () => [0,1,2].map(index=>getComputedStyle(foregroundRoot.querySelector(
            '[data-cfx-target-kind="point"][data-cfx-series="0"][data-cfx-point="'+index+'"]')).opacity)
        """);
    private static Task<string[]> NativePaint(IPage page) => page.EvaluateAsync<string[]>(
        "()=>[foregroundPaint.getAttribute('fill'),getComputedStyle(foregroundPaint).fill]");
    private static Task<JsonElement> State(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => ({selection:foregroundController.getSelection().map(target=>target.targetId),
            highlights:[...foregroundRoot.querySelectorAll('[data-cfx-highlighted=true]')].map(node=>node.dataset.cfxTargetId),
            marks:[0,1,2].map(index=>{const node=foregroundRoot.querySelector('[data-cfx-target-kind="point"][data-cfx-series="0"][data-cfx-point="'+index+'"]');
                return{id:foregroundTargets[index].targetId,opacity:getComputedStyle(node).opacity,authoredOpacity:node.getAttribute('opacity'),classes:node.getAttribute('class')};}),
            nativePaint:{fill:foregroundPaint.getAttribute('fill'),computedFill:getComputedStyle(foregroundPaint).fill},
            hoverMode:foregroundRoot.dataset.cfxHoverMode,hoverUnit:foregroundRoot.dataset.cfxHoverUnit})
        """);

    private static async Task Capture(HtmlBrowserSession session, ChartSeriesKind kind, bool graphite, bool dark, int width,
        string phase, JsonElement state, List<string> pageErrors) {
        var output = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var stem = Path.Combine(output, "linked-foreground-" + kind + "-" + graphite + "-" + dark + "-" + width + "-" + phase);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".json", JsonSerializer.Serialize(new { state, pageErrors,
            console = session.ConsoleLog.Select(entry => new { type = entry.Type.ToString(), entry.Text }) }));
    }
}
