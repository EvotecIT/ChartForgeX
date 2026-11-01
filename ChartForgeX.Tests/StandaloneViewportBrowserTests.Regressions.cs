using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    [BrowserFact]
    public async Task SynchronizedResetRestoresBothPeerSourceWindowsAndClearsItsResetControl() {
        if (!Enabled) return;
        Chart Data() => Chart.Create().WithSize(680, 340).WithXAxisBounds(0, 12).WithYAxisBounds(0, 100)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("readings"), "Readings",
                new[] { new ChartPoint(0, 10), new ChartPoint(6, 50), new ChartPoint(12, 90) }));
        var html = new[] { Data(), Data() }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.GroupName = "linked-windows";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.SynchronizedCharts);
        });
        await using var session = await OpenAsync(html, 980, 1400);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
                [window.controller, window.peer] = [...document.querySelectorAll('.cfx-interactive-chart')].map(root => ChartForgeXCharts.get(root));
                controller.setViewport({x:{minimum:2,maximum:10},y:{minimum:20,maximum:80}});
            }
            """);
        Assert.Equal(2, await page.EvaluateAsync<double>("()=>peer.getXWindow().minimum"));
        Assert.Equal(20, await page.EvaluateAsync<double>("()=>peer.getYWindow().minimum"));
        await page.Locator(Root).First.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal(0, await page.EvaluateAsync<double>("()=>peer.getXWindow().minimum"));
        Assert.Equal(12, await page.EvaluateAsync<double>("()=>peer.getXWindow().maximum"));
        Assert.Equal(0, await page.EvaluateAsync<double>("()=>peer.getYWindow().minimum"));
        Assert.Equal(100, await page.EvaluateAsync<double>("()=>peer.getYWindow().maximum"));
        Assert.True(await page.Locator(Root).Nth(1).Locator("[data-cfx-reset]").IsHiddenAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposalAndReaderUpgradeRestoreOwnedVisibilityAndReleasePlotWrappers(bool reader) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(ChartSeriesKind.Line, reference: true,
            runtime: reader ? HtmlChartRuntime.Standalone : HtmlChartRuntime.Full), 980, 900);
        var page = session.Page;
        var clips = await page.Locator(".cfx-stage clipPath").CountAsync();
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]").ClickAsync();
        var paint = page.Locator("[data-cfx-role=series][data-cfx-series-key=reference] [data-cfx-role=line]");
        Assert.Equal("hidden", await paint.EvaluateAsync<string>("node=>getComputedStyle(node).visibility"));
        if (reader) await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = HtmlInteractiveAssetFiles.ChartScript.Content });
        else await page.EvaluateAsync("()=>{const root=document.querySelector('.cfx-interactive-chart');ChartForgeXCharts.get(root).dispose();ChartForgeXCharts.mount(root);}");
        await MountAsync(page);
        Assert.Equal("visible", await paint.EvaluateAsync<string>("node=>getComputedStyle(node).visibility"));
        AssertWindow((await ViewportAsync(page)).GetProperty("y"), 0, 100);
        Assert.Equal(clips, await page.Locator(".cfx-stage clipPath").CountAsync());
        await page.EvaluateAsync("()=>{const root=controller.root;controller.dispose();window.controller=ChartForgeXCharts.mount(root);}");
        Assert.Equal(clips, await page.Locator(".cfx-stage clipPath").CountAsync());
        Assert.Equal("visible", await paint.EvaluateAsync<string>("node=>getComputedStyle(node).visibility"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Bar, 20, 60)]
    [InlineData(ChartSeriesKind.Candlestick, 71, 73)]
    public async Task VisibleBarAndCandleExtentsRetainTooltipAndSelectionOutsideTheirRepresentativeValue(ChartSeriesKind kind, double minimum, double maximum) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(kind), 980, 900);
        var page = session.Page;
        await MountAsync(page);
        await page.EvaluateAsync("range=>controller.setViewport({x:{minimum:8,maximum:12},y:{minimum:range.minimum,maximum:range.maximum}})", new { minimum, maximum });
        var observation = page.Locator("[data-cfx-role=point][data-cfx-series-key=samples][data-cfx-observation-id=sample-10]");
        Assert.Equal("true", await observation.GetAttributeAsync("data-cfx-visible"));
        var pointer = await PlotPointerAsync(page, .5, .5);
        await page.Mouse.MoveAsync(pointer.X, pointer.Y);
        Assert.True(await page.Locator(".cfx-tooltip").IsVisibleAsync());
        Assert.Contains("Measurements", await TooltipTextAsync(page));
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const target=controller.resolveTargets().find(target=>target.observationId==='sample-10');
                if(!target)return false; controller.select({targets:[target]});
                return controller.getSelection().some(target=>target.observationId==='sample-10');
            }
            """));
        AssertNoConsoleErrors(session);
    }
}
