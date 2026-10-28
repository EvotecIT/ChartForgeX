using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyNestedHeaderBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public async Task NestedErHeadersPaintEveryAuthoredRowWithNativeGeometryAndComposedContrast(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidClassErCaptionTests.Source("er"), MermaidClassErCaptionTests.Options(dark, width));
        Assert.False(result.HasErrors); var artifact = result.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 720);
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        var group = session.Page.Locator("[data-cfx-role=topology-group][data-group-id='Order group']");
        Assert.Equal(1, await group.CountAsync());
        var native = Assert.IsType<TopologyChart>(artifact.Model).Prepare();
        var expectedRows = native.Visual.Scene.Nodes.OfType<VisualSceneText>()
            .Where(text => text.Role == "topology-group-label" && text.Text.Lines.Any(line => line.Text is "Order" or "records")).ToArray();
        Assert.Equal(2, expectedRows.Length);
        var browserRows = await group.EvaluateAsync<double[][]>("group=>[...group.querySelectorAll('[data-cfx-role=topology-group-label] text')].map(text=>[+text.getAttribute('x'),+text.getAttribute('y')])");
        Assert.Equal(expectedRows.Length, browserRows.Length);
        for (var row = 0; row < expectedRows.Length; row++) {
            Assert.InRange(Math.Abs(expectedRows[row].X - browserRows[row][0]), 0, .01);
            Assert.InRange(Math.Abs(expectedRows[row].Baseline - browserRows[row][1]), 0, .01);
        }
        Assert.True(await group.EvaluateAsync<bool>("""
            group => {
                const surface = group.querySelector('[data-cfx-role="topology-group-surface"]').getBBox();
                const texts = [...group.querySelectorAll('[data-cfx-role="topology-group-label"] text')];
                const rows = texts.map(text => text.textContent);
                if (rows.join('\n') !== 'Order\nrecords') return false;
                const bounds = texts.map(text => text.getBBox());
                return bounds[1].y > bounds[0].y && bounds.every(b => b.x >= surface.x - .1 && b.x + b.width <= surface.x + surface.width + .1
                    && b.y >= surface.y - .1 && b.y + b.height <= surface.y + surface.height + .1);
            }
            """));
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => {
                const rgb = value => (value.match(/[\d.]+/g) || []).map(Number);
                const over = (a,b) => a.slice(0,3).map((v,i) => v*(a[3] ?? 1)+b[i]*(1-(a[3] ?? 1)));
                const luminance = rgb => rgb.slice(0,3).map(v=>v/255).map(v=>v<=.04045?v/12.92:((v+.055)/1.055)**2.4)
                    .reduce((sum,v,i)=>sum+v*[.2126,.7152,.0722][i],0);
                const parent = document.querySelector('[data-cfx-role="topology-group"][data-group-id="Domain: core"]');
                const child = document.querySelector('[data-cfx-role="topology-group"][data-group-id="Order group"]');
                const base = rgb(getComputedStyle(parent.querySelector('[data-cfx-role="topology-group-surface"]')).fill);
                const shell = rgb(getComputedStyle(child.querySelector('[data-cfx-role="topology-group-surface"]')).fill);
                const bg = luminance(over(shell,base));
                return [...child.querySelectorAll('[data-cfx-role="topology-group-label"] text')].every(text=>{
                    const ink=luminance(rgb(getComputedStyle(text).fill)); return (Math.max(ink,bg)+.05)/(Math.min(ink,bg)+.05)>=4.5;
                });
            }
            """));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "nested-er-header-browser-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
}
