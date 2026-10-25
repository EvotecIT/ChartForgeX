using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class SlopeStagesBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    public async Task HorizontalStagesUseNativePhysicalWindowsAndKeepMiddleIdentityInCurrentExports(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width, 400).WithFontFamily(BrowserProofFonts.Family).WithTheme(VisualTheme.Graphite(), mode)
            .WithLegend(false).WithXLabels("First", "Second", "Third", "Fourth")
            .AddSlope("Recorded", new[] { new ChartPoint(1, 20), new ChartPoint(2, 45), new ChartPoint(3, 35, true), new ChartPoint(4, 70) });
        chart.Series[0].UseSecondaryValueAxis().WithObservationIds(new[] { "first", "second", "third", "fourth" }.Select(id => new ChartObservationId(id)));
        chart.Options.SlopeOrientation = ChartOrientation.Horizontal;
        chart.WithXAxisBounds(0, 100).WithYAxisBounds(0, 5).WithSecondaryXAxisBounds(0, 100);
        var renderer = new HtmlInteractiveChartRenderer(); var initial = renderer.PrepareSection(chart, configure: Configure);
        var html = "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body>" + initial.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 750);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var requests = new List<JsonElement>(); HtmlChartSection? accepted = null;
        await page.ExposeFunctionAsync("slopePrepare", (JsonElement request) => {
            requests.Add(request.Clone());
            Assert.Equal(new[] { "y", "secondaryX" }, request.GetProperty("axes").EnumerateArray().Select(axis => axis.GetString()));
            var source = request.GetProperty("sourceViewport");
            var viewport = new ChartCartesianViewport(y: NativeWindowTransport.Read(source.GetProperty("y")),
                secondaryX: NativeWindowTransport.Read(source.GetProperty("secondaryX")));
            accepted = renderer.PrepareSection(chart, viewport, Configure);
            return new { html = accepted.Html, basis = "stages-window" };
        });
        await page.EvaluateAsync("""
            ()=>{window.slopeRoot=document.querySelector('.cfx-interactive-chart');window.slopeController=ChartForgeXCharts.get(slopeRoot);
                window.slopeSelection=null;window.slopeChanges=[];window.slopeErrors=[];
                slopeRoot.addEventListener('cfxselect',event=>slopeSelection=event.detail.target);
                slopeRoot.addEventListener('cfxviewportchange',event=>slopeChanges.push(event.detail));
                slopeRoot.addEventListener('cfxviewporterror',event=>slopeErrors.push(event.detail));
                slopeController.connectNative(request=>slopePrepare(request),{basis:'stages-full',axes:['y','secondaryX']});
                slopeController.setViewport({y:{minimum:1.5,maximum:3.5},secondaryX:{minimum:25,maximum:60}});}
            """);
        await page.WaitForFunctionAsync("()=>slopeChanges.length===1"); Assert.Single(requests); Assert.NotNull(accepted);
        var target = page.Locator("[data-cfx-role=point][data-cfx-observation-id=second]");
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("second", await page.EvaluateAsync<string>("()=>slopeSelection.observationId"));
        Assert.Equal("1", await page.EvaluateAsync<string>("()=>slopeSelection.sourcePoint"));
        Assert.Equal("45", await target.GetAttributeAsync("data-cfx-value"));
        Assert.Equal(4, await page.Locator("[data-cfx-role=point][data-cfx-source-point]").CountAsync());
        Assert.Equal(2, await page.Locator("[data-cfx-role=slope-marker]").CountAsync());
        var defects = await page.EvaluateAsync<string[]>("""
            ()=>{const defects=[],root=slopeRoot,metadata=JSON.parse(root.dataset.cfxPreparedChart);
                if(metadata.secondaryXProjection.minimum!==25||metadata.secondaryXProjection.maximum!==60)defects.push('secondary physical X window');
                if(metadata.yProjection.minimum!==1.5||metadata.yProjection.maximum!==3.5)defects.push('physical Y category window');
                const path=root.querySelector('[data-cfx-role="slope-line"]');
                if(!path||(path.getAttribute('d').match(/M/g)||[]).length!==2)defects.push('explicit source gap bridged');
                for(const id of ['first','fourth'])if(root.querySelector('[data-cfx-observation-id="'+id+'"]')?.dataset.cfxVisible!=='false')defects.push('out-of-window source painted');
                return defects;}
            """);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svgPath = await svgDownload.PathAsync() ?? throw new InvalidOperationException("No slope SVG download.");
        var downloaded = XDocument.Load(svgPath); var expected = XDocument.Parse(accepted!.PreparedVisual.ToSvg());
        string[] Paths(XDocument document) => document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "slope-line")
            .Select(node => (string)node.Attribute("d")!).ToArray();
        Assert.Equal(Paths(expected), Paths(downloaded));
        Assert.Equal(4, downloaded.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "point"));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var pngPath = await pngDownload.PathAsync() ?? throw new InvalidOperationException("No slope PNG download.");
        var image = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(pngPath)); Assert.Equal(width, image.Width); Assert.Equal(400, image.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Slope captures require an absolute task path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "slope-horizontal-stages-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".json", JsonSerializer.Serialize(new { requests, defects, errors, session.ConsoleLog }));
        }
        Assert.Empty(defects); Assert.Empty(errors); Assert.True(await page.EvaluateAsync<bool>("()=>slopeErrors.length===0")); AssertNoConsoleErrors(session);
    }
    private static void Configure(HtmlChartInteractionOptions options) {
        options.IdScope = "slope-stages";
        options.Interaction.Enable(ChartInteractionFeatures.ReportReview | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Export);
    }
}
