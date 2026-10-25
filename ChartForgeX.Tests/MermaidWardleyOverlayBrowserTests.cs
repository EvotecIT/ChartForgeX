using ChartForgeX.VisualArtifacts;
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidWardleyOverlayBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public async Task StaticOverlaysKeepCompleteLegendAndRealChildEndpointsThroughResizeAndDownload(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var rendered = MermaidRenderer.Render(MermaidWardleyOverlayTests.Fixture(dark), MermaidWardleyOverlayTests.Options(width, BrowserProofFonts.Family)); Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var map = (WardleyMapBlock)artifact.Model!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:12px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#172033")
            + "}section{max-width:100%;overflow:auto;margin-bottom:16px}svg,img{display:block;max-width:none}</style><nav>"
            + "<a id='svg-download' download='wardley.svg' href='data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg)) + "'>SVG</a> "
            + "<a id='png-download' download='wardley.png' href='data:image/png;base64," + Convert.ToBase64String(png) + "'>PNG</a></nav>"
            + "<section id='native' tabindex='0'>" + svg + "</section><section><img id='pixels' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 1000); var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("async()=>{await document.fonts.ready;await document.querySelector('#pixels').decode()}");
        var name = "wardley-overlays-" + width + (dark ? "-dark" : "-light"); var first = await Observe(page); await Capture(page, first, svg, png, name);
        Assert.Empty(first.GetProperty("legendClips").EnumerateArray()); Assert.Equal(2, first.GetProperty("annotations").GetInt32());
        var legend = first.GetProperty("legend").EnumerateArray().Select(item => item.GetString()).ToArray();
        Assert.Equal(new[] { "1. Keep the user need visible", "2. Prefer a portable service contract" }, legend);
        Assert.Equal(2, first.GetProperty("children").GetArrayLength()); Assert.Equal(1, first.GetProperty("rails").GetInt32()); Assert.Equal(3, first.GetProperty("marketMembers").GetInt32());
        Assert.True(first.GetProperty("childEndpointAligned").GetBoolean()); Assert.Equal(0, first.GetProperty("activeSvgElements").GetInt32());
        Assert.Equal(width, first.GetProperty("pngWidth").GetInt32()); Assert.Equal(600, first.GetProperty("pngHeight").GetInt32()); Assert.True(first.GetProperty("documentFits").GetBoolean());
        foreach (var hostWidth in new[] { width == 390 ? 1124 : 414, width + 24 }) {
            await page.SetViewportSizeAsync(hostWidth, 1000); var current = await Observe(page); await Capture(page, current, svg, png, name + "-resized-" + hostWidth);
            Assert.Equal(first.GetProperty("geometry").GetRawText(), current.GetProperty("geometry").GetRawText()); Assert.Empty(current.GetProperty("legendClips").EnumerateArray()); Assert.True(current.GetProperty("documentFits").GetBoolean());
        }
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("#svg-download").ClickAsync());
        Assert.Equal(svg, await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("Missing native Wardley SVG download.")));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("#png-download").ClickAsync());
        Assert.Equal(png, await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("Missing native Wardley PNG download.")));
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            await svgDownload.SaveAsAsync(Path.Combine(output, name + ".export.svg")); await pngDownload.SaveAsAsync(Path.Combine(output, name + ".export.png"));
        }
        Assert.Equal(5, map.Nodes.Count); Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> Observe(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
          const svg=document.querySelector('#native svg'),box=svg.querySelector('[data-cfx-role=wardley-annotation-box]'),rect=box.querySelector('rect').getBoundingClientRect(),legendClips=[];
          for(const text of box.querySelectorAll('text')){const b=text.getBoundingClientRect();if(b.width<=0||b.height<=0||b.left<rect.left-.5||b.right>rect.right+.5||b.top<rect.top-.5||b.bottom>rect.bottom+.5)legendClips.push(text.textContent)}
          const children=[...svg.querySelectorAll('[data-cfx-role=wardley-pipeline-component]')].map(n=>({id:n.dataset.nodeId,cx:n.getAttribute('cx'),cy:n.getAttribute('cy')}));
          const scale=children.find(n=>n.id==='Service_Scale'),link=[...svg.querySelectorAll('[data-cfx-role=wardley-link]')].at(-1),png=document.querySelector('#pixels');
          return {legend:[...box.querySelectorAll('[data-cfx-role=wardley-annotation-legend-entry]')].map(n=>[...n.querySelectorAll('text')].map(t=>t.textContent).join(' ').replace(/\s+/g,' ').trim()),legendClips,
            children,annotations:svg.querySelectorAll('[data-cfx-role=wardley-annotation]').length,rails:svg.querySelectorAll('[data-cfx-role=wardley-pipeline-evolution]').length,marketMembers:svg.querySelectorAll('[data-cfx-role=wardley-market-member]').length,
            childEndpointAligned:scale.cx===link.getAttribute('x1')&&scale.cy===link.getAttribute('y1'),pngWidth:png.naturalWidth,pngHeight:png.naturalHeight,
            activeSvgElements:svg.querySelectorAll('script,image,foreignObject,button,[tabindex]').length,documentFits:document.documentElement.scrollWidth<=innerWidth,
            geometry:[...svg.querySelectorAll('rect,circle,line,path')].map(n=>({tag:n.tagName,role:n.dataset.cfxRole,x:n.getAttribute('x'),y:n.getAttribute('y'),cx:n.getAttribute('cx'),cy:n.getAttribute('cy'),d:n.getAttribute('d')}))};
        }
        """);
    private static async Task Capture(IPage page, JsonElement observed, string svg, byte[] png, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
        Directory.CreateDirectory(output); var path = Path.Combine(output, name);
        File.WriteAllText(path + ".json", observed.GetRawText()); File.WriteAllText(path + ".native.svg", svg); File.WriteAllBytes(path + ".native.png", png);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
    }
}
