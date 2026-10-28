using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises actual opt-in gestures and host-prepared native replacements; the browser never edits chart data through paint.</summary>
public sealed class ProgressEditingBrowserTests {
    private const string Fraction = "[data-cfx-role=progress-row][data-cfx-observation-id=fraction]";

    [BrowserTheory]
    [InlineData(390, false)]
    [InlineData(760, true)]
    public async Task KeyboardAndPointerEditsUseTheRowDomainAndNativeIdentityThroughReorder(int width, bool dark) {
        if (!Enabled) return;
        var initial = ChartFor(width, dark, .5, false);
        var keyboard = ChartFor(width, dark, .625, true);
        var pointer = ChartFor(width, dark, .75, true);
        var stale = ChartFor(width, dark, .875, false);
        var renderer = new HtmlInteractiveChartRenderer();
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Options), width, 560);
        var page = session.Page;
        await page.EvaluateAsync("""
            replies => {
              const root = document.querySelector('.cfx-interactive-chart');
              window.progress = ChartForgeXCharts.mount(root); window.requests=[]; window.progressErrors=[];
              root.addEventListener('cfxprogresserror',event=>progressErrors.push(event.detail));
              progress.connectNative(request=>{
                requests.push(request);
                if(request.value===.875)return new Promise(resolve=>window.finishStale=resolve);
                return {html:request.value===.625?replies.keyboard:replies.pointer,basis:'basis-'+requests.length};
              },{basis:'basis-0',axes:null,progressEditing:true});
              window.staleReply=replies.stale;
            }
            """, new {
                keyboard = renderer.PrepareSection(keyboard, configure: Options).Html,
                pointer = renderer.PrepareSection(pointer, configure: Options).Html,
                stale = renderer.PrepareSection(stale, configure: Options).Html
            });
        var fraction = page.Locator(Fraction); await fraction.FocusAsync(); await page.Keyboard.PressAsync("ArrowRight");
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-observation-id=fraction]').dataset.cfxValue==='0.625'");
        Assert.Equal("1", await fraction.GetAttributeAsync("data-cfx-point"));
        Assert.Equal("slider", await fraction.GetAttributeAsync("role")); Assert.Equal("1", await fraction.GetAttributeAsync("aria-valuemax"));
        Assert.Equal("fraction", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxObservationId"));
        Assert.Equal("basis-0", await page.EvaluateAsync<string>("()=>requests[0].basis"));
        var track = await BoxAsync(page, Fraction + " [data-cfx-role=progress-track]");
        var handle = await BoxAsync(page, Fraction + " [data-cfx-role=progress-handle]");
        await page.Mouse.MoveAsync((float)(handle.X + handle.Width / 2), (float)(handle.Y + handle.Height / 2));
        await page.Mouse.DownAsync(); await page.Mouse.MoveAsync((float)(track.X + track.Width * .75), (float)(track.Y + track.Height / 2)); await page.Mouse.UpAsync();
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-observation-id=fraction]').dataset.cfxValue==='0.75'");
        Assert.Equal(.75, await page.EvaluateAsync<double>("()=>requests.at(-1).value"));
        Assert.Equal("delivery", await page.EvaluateAsync<string>("()=>requests.at(-1).seriesKey"));
        Assert.Equal("fraction", await page.EvaluateAsync<string>("()=>requests.at(-1).observationId"));
        Assert.Equal("basis-1", await page.EvaluateAsync<string>("()=>requests.at(-1).basis"));
        var before = await fraction.GetAttributeAsync("data-cfx-value");
        await page.EvaluateAsync("()=>progress.setProgressValue({seriesKey:'delivery',observationId:'fraction'},.875)");
        await page.WaitForFunctionAsync("()=>typeof finishStale==='function'");
        await page.EvaluateAsync("html=>{progress.update(html,{basis:'manual'});finishStale({html:staleReply,basis:'stale'});}", renderer.PrepareSection(pointer, configure: Options).Html);
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(before, await fraction.GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>progressErrors.length"));
        Assert.True(await page.EvaluateAsync<bool>("()=>{try{progress.setProgressValue({seriesKey:'delivery',observationId:'fraction'},2);return false;}catch(error){return error instanceof RangeError;}}"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); var stem = Path.Combine(capture, "progress-editing-" + width + "-" + dark);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".png", FullPage = true });
            await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("()=>JSON.stringify({requests,errors:progressErrors},null,2)"));
            File.WriteAllBytes(stem + ".native.png", renderer.PrepareSection(pointer, configure: Options).PreparedVisual.ToPng());
        }
        await page.EvaluateAsync("()=>progress.dispose()");
        Assert.True(await page.EvaluateAsync<bool>("()=>{try{progress.setProgressValue({seriesKey:'delivery',observationId:'fraction'},.5);return false;}catch{return true;}}"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EditingIsOptInAndWrongNativeRepliesDoNotCommit(bool enabled) {
        if (!Enabled) return;
        var chart = ChartFor(640, false, .5, false);
        void Configure(HtmlChartInteractionOptions options) { Options(options); options.ProgressEditing.Enabled = enabled; }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Configure)); var page = session.Page;
        await page.EvaluateAsync("""
            html=>{window.progress=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));window.errors=[];
              progress.root.addEventListener('cfxprogresserror',event=>errors.push(event.detail));
              progress.connectNative(()=>({html,basis:'wrong'}),{basis:'initial',axes:null,progressEditing:true});}
            """, new HtmlInteractiveChartRenderer().PrepareSection(chart, configure: Configure).Html);
        if (enabled) {
            await page.EvaluateAsync("()=>progress.setProgressValue({seriesKey:'delivery',observationId:'fraction'},.75)");
            await page.WaitForFunctionAsync("()=>errors.length===1");
            Assert.Equal("0.5", await page.Locator(Fraction).GetAttributeAsync("data-cfx-value"));
        } else {
            Assert.NotEqual("slider", await page.Locator(Fraction).GetAttributeAsync("role"));
            Assert.True(await page.EvaluateAsync<bool>("()=>{try{progress.setProgressValue({seriesKey:'delivery',observationId:'fraction'},.75);return false;}catch{return true;}}"));
        }
        AssertNoConsoleErrors(session);
    }

    private static Chart ChartFor(int width, bool dark, double fraction, bool reorder) {
        var first = new ChartProgressItem("Fraction", fraction, 0, 1); var second = new ChartProgressItem("Jobs", 500, 0, 2000);
        var series = ChartSeries.CreateProgressBars(new ChartSeriesId("delivery"), "Delivery", reorder ? new[] { second, first } : new[] { first, second })
            .WithObservationId(reorder ? 1 : 0, new ChartObservationId("fraction"))
            .WithObservationId(reorder ? 0 : 1, new ChartObservationId("jobs"));
        return Chart.Create().WithSize(width, 340).WithTitle("Edit independent progress rows").WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series);
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.ProgressEditing.Enabled = true; options.ProgressEditing.StepRatio = .125;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Tooltip.PinOnSelect = false;
    }
}
