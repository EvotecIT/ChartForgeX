using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Downloaded artifacts retain persistent state and host paint without browser hover decoration.</summary>
public sealed class InteractiveChartExportBrowserTests {
    private const string Root = ".cfx-interactive-chart";
    private const string Focus = "[data-cfx-role=point][data-cfx-series-key=focus]";
    private const string Other = "[data-cfx-role=point][data-cfx-series-key=other]";

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StandaloneSvgAndBrowserPngRetainPersistentPaintWithoutHoverOrKeyboardChrome(bool isolated) {
        if (!Enabled) return;
        var authored = ChartColor.FromHex("#E11D48");
        var chart = Chart.Create().WithSize(680, 420).WithTitle("Export current observations").WithLegend()
            .WithDataLabels(false).WithYAxisBounds(0, 12)
            .WithSvgColorVariables(new SvgColorVariables().Add("--export-series", authored, SvgColorRole.Series))
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("focus"), "Current", new[] { new ChartPoint(1, 8) }, authored))
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("muted"), "Comparison", new[] { new ChartPoint(1, 6) }, ChartColor.FromHex("#2563EB")))
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("other"), "Other", new[] { new ChartPoint(1, 10) }, ChartColor.FromHex("#EAB308")));
        var html = chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.PinOnSelect = false;
            options.Interaction.Enable(ChartInteractionFeatures.Export | ChartInteractionFeatures.FocusTrail | ChartInteractionFeatures.Zoom);
        });
        await using var session = await OpenAsync(html, 940, 800);
        var page = session.Page;
        await page.Locator(Root).EvaluateAsync("root=>root.style.setProperty('--export-series','#16A34A')");
        // Seed persistent state without creating pointer, focus or trail presentation in the baseline.
        await page.Locator(Focus).EvaluateAsync("""
            point=>ChartForgeXCharts.get(point.closest('.cfx-interactive-chart')).select({targets:[{
                targetKind:point.dataset.cfxTargetKind,targetId:point.dataset.cfxTargetId,seriesKey:point.dataset.cfxSeriesKey}]})
            """);
        await page.Locator(isolated ? "[data-cfx-role=legend-item][data-cfx-series-key=focus]" : "[data-cfx-role=legend-item][data-cfx-series-key=muted]")
            .EvaluateAsync("(node,isolated)=>node.dispatchEvent(new MouseEvent('click',{bubbles:true,shiftKey:isolated}))", isolated);
        Assert.Equal("true", await page.Locator(Focus).GetAttributeAsync("aria-selected"));
        Assert.Equal(0, await page.Locator(".cfx-hovered,.cfx-trail").CountAsync());
        var expected = await PersistentPaintAsync(page);
        await FocusWithKeyboardAsync(page, page.Locator(Focus));
        await MoveToAsync(page, Focus);
        Assert.True(await page.Locator(Root).EvaluateAsync<bool>("root=>!!root.querySelector('.cfx-hovered')&&!!root.querySelector('.cfx-trail')"));
        await page.Locator("[data-cfx-zoom=in]").EvaluateAsync("button=>button.click()");
        var before = await ExportSourceStateAsync(page);

        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        var svgPath = await svgDownload.PathAsync() ?? throw new InvalidOperationException("SVG export produced no artifact.");
        var svg = await File.ReadAllTextAsync(svgPath);
        Assert.Equal(before, await ExportSourceStateAsync(page));
        var document = XDocument.Parse(svg);
        Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName == "script");
        Assert.DoesNotContain("cfx-hovered", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("cfx-trail", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-keyboard-focus", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("tabindex", svg, StringComparison.Ordinal);

        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").EvaluateAsync("button=>button.click()"));
        var pngPath = await pngDownload.PathAsync() ?? throw new InvalidOperationException("PNG export produced no artifact.");
        var png = await File.ReadAllBytesAsync(pngPath);
        var artifactPage = await page.Context.NewPageAsync();
        try {
            await artifactPage.SetViewportSizeAsync(940, 800);
            // No runtime or adapter stylesheet is present in this independent artifact view.
            await artifactPage.SetContentAsync("<!doctype html><html><body>" + svg + "</body></html>");
            var actual = await PersistentPaintAsync(artifactPage);
            Assert.Equal(expected.GetRawText(), actual.GetRawText());
            Assert.Equal("rgb(22, 163, 74)", actual.GetProperty("focus").GetProperty("fill").GetString());
            Assert.Equal("true", await artifactPage.Locator(Focus).GetAttributeAsync("aria-selected"));
            Assert.Equal("none", await artifactPage.Locator("svg").EvaluateAsync<string>("node=>getComputedStyle(node).transform"));
            var pixels = await artifactPage.EvaluateAsync<JsonElement>("""
                async ({svg,png})=>{
                    const load=src=>new Promise((resolve,reject)=>{const image=new Image();image.onload=()=>resolve(image);image.onerror=reject;image.src=src;});
                    const [vector,raster]=await Promise.all([load('data:image/svg+xml;charset=utf-8,'+encodeURIComponent(svg)),load('data:image/png;base64,'+png)]);
                    const sample=image=>{
                        const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                        const context=canvas.getContext('2d');context.drawImage(image,0,0);
                        return ['focus','muted','other'].map(key=>{
                            const mark=document.querySelector('[data-cfx-role=point][data-cfx-series-key='+key+'] [data-cfx-role=bar]');
                            const box=mark.getBBox(),point=new DOMPoint(box.x+box.width/2,box.y+box.height/2).matrixTransform(mark.getCTM());
                            return [...context.getImageData(Math.floor(point.x),Math.floor(point.y),1,1).data];
                        });
                    };
                    return {svg:sample(vector),png:sample(raster),width:raster.naturalWidth,height:raster.naturalHeight};
                }
                """, new { svg, png = Convert.ToBase64String(png) });
            Assert.Equal(680, pixels.GetProperty("width").GetInt32());
            Assert.Equal(420, pixels.GetProperty("height").GetInt32());
            Assert.Equal(pixels.GetProperty("svg").GetRawText(), pixels.GetProperty("png").GetRawText());
            Assert.Equal(new[] { 22, 163, 74, 255 }, pixels.GetProperty("png")[0].EnumerateArray().Select(value => value.GetInt32()));
            var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(captures)) {
                Directory.CreateDirectory(captures);
                var name = "persistent-export-" + (isolated ? "isolated" : "muted");
                await svgDownload.SaveAsAsync(Path.Combine(captures, name + ".svg"));
                await pngDownload.SaveAsAsync(Path.Combine(captures, name + ".png"));
                await artifactPage.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".artifact.png"), FullPage = true });
                await File.WriteAllTextAsync(Path.Combine(captures, name + ".json"), JsonSerializer.Serialize(new { expected, actual, pixels }));
            }
        } finally { await artifactPage.CloseAsync(); }
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task RootBranchExportsKeepSelectedPaintAndOmitTheActiveBranchOverride() {
        if (!Enabled) return;
        var chart = V2GalleryModels.Create(ChartSeriesKind.Sunburst, "options", VisualThemeMode.Light).WithSize(680, 460);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.PinOnSelect = false;
            options.Interaction.Enable(ChartInteractionFeatures.Export);
            options.Highlight.Mode = HtmlChartHighlightMode.RootBranch;
            options.Highlight.HighlightedItem.Opacity = .4;
            options.Highlight.HighlightedItem.Stroke = ChartColor.FromHex("#00FFFF");
            options.Highlight.HighlightedItem.StrokeWidth = 5;
        }), 900, 760);
        var page = session.Page;
        await page.Locator(Root).EvaluateAsync("root=>ChartForgeXCharts.get(root).select({targets:[{targetKind:'node',targetId:'engineering',seriesKey:'allocation-source'}]})");
        var expected = await BranchPaintAsync(page);
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-node=platform]"));
        Assert.Equal("item", await page.Locator("[data-cfx-node=platform]").GetAttributeAsync("data-cfx-highlight-state"));
        Assert.True(await page.Locator("[data-cfx-highlight-outline]").CountAsync() > 0);
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        var path = await download.PathAsync() ?? throw new InvalidOperationException("Root-branch export produced no artifact.");
        var svg = await File.ReadAllTextAsync(path);
        Assert.DoesNotContain("data-cfx-highlight-outline", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-highlight-state", svg, StringComparison.Ordinal);
        Assert.Equal("item", await page.Locator("[data-cfx-node=platform]").GetAttributeAsync("data-cfx-highlight-state"));
        var artifactPage = await page.Context.NewPageAsync();
        try {
            await artifactPage.SetContentAsync("<!doctype html><html><body>" + svg + "</body></html>");
            Assert.Equal(expected.GetRawText(), (await BranchPaintAsync(artifactPage)).GetRawText());
            Assert.Equal("true", await artifactPage.Locator("[data-cfx-node=engineering]").GetAttributeAsync("aria-selected"));
            var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(captures)) {
                Directory.CreateDirectory(captures);
                await download.SaveAsAsync(Path.Combine(captures, "persistent-export-root-branch.svg"));
                await artifactPage.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "persistent-export-root-branch.artifact.png"), FullPage = true });
            }
        } finally { await artifactPage.CloseAsync(); }
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> PersistentPaintAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>Object.fromEntries(['focus','muted','other'].map(key=>{
            const owner=document.querySelector('[data-cfx-role=series][data-cfx-series-key='+key+']');
            const point=owner.querySelector('[data-cfx-role=point]'),mark=point.querySelector('[data-cfx-role=bar]');
            const styles=getComputedStyle(mark);
            let opacity=1;for(let node=mark;node&&node.localName!=='svg';node=node.parentElement)opacity*=Number(getComputedStyle(node).opacity);
            const pointStyle=getComputedStyle(point);
            return [key,{fill:styles.fill,opacity,filter:getComputedStyle(owner).filter,pointFilter:pointStyle.filter,pointStrokeWidth:pointStyle.strokeWidth}];
        }))
        """);

    private static Task<string> ExportSourceStateAsync(IPage page) => page.Locator(Root).EvaluateAsync<string>("""
        root=>JSON.stringify({hover:root.dataset.cfxHovering,mode:root.dataset.cfxHoverMode,unit:root.dataset.cfxHoverUnit,
            focus:root.querySelector('[data-cfx-keyboard-focus-outline]')?.outerHTML,
            nodes:[...root.querySelectorAll('.cfx-stage svg [class]')].map(node=>[node.getAttribute('class'),node.getAttribute('style')])})
        """);

    private static Task<JsonElement> BranchPaintAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>Object.fromEntries(['engineering','platform','operations'].map(id=>{
            const node=document.querySelector('[data-cfx-node='+id+']'),paint=node.querySelector('[data-cfx-highlight-paint]'),style=getComputedStyle(paint);
            return[id,{opacity:style.opacity,filter:style.filter,fill:getComputedStyle(paint.querySelector('[data-cfx-role=sunburst-segment-mark]')).fill}];
        }))
        """);
}
