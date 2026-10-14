using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false, 860)]
    [InlineData(true, 400)]
    public async Task ExecutableLifecycleExampleUpdatesAndRemountsThroughItsHostControls(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(HtmlChartLifecycleExamples.CreatePage(dark), width, 760);
        var page = session.Page;
        await page.Locator(Root).EvaluateAsync("root=>{window.lifecycleSelections=[];root.addEventListener('cfxselect',event=>lifecycleSelections.push(event.detail));}");
        await SelectAsync(page, North);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Update measurements", Exact = true }).ClickAsync();
        Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        Assert.Equal("17", await page.Locator(North).GetAttributeAsync("data-cfx-y"));
        Assert.Equal("1", await page.Locator(North).GetAttributeAsync("data-cfx-series"));
        Assert.Equal("Updated measurements: North is 17", await page.Locator("#host-status").InnerTextAsync());
        await CaptureAsync(page, "executable-demo-updated-" + (dark ? "dark" : "light"));
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Dispose chart", Exact = true }).ClickAsync();
        Assert.False(await page.Locator("#update-measurements").IsEnabledAsync());
        Assert.Equal("Chart disposed", await page.Locator("#host-status").InnerTextAsync());
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Mount again", Exact = true }).ClickAsync();
        Assert.True(await page.Locator("#update-measurements").IsEnabledAsync());
        await page.EvaluateAsync("()=>lifecycleSelections=[]");
        await SelectAsync(page, North);
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>lifecycleSelections.length"));
        Assert.Equal("17", await page.EvaluateAsync<string>("()=>lifecycleSelections[0].target.value"));
        await page.Locator("#update-measurements").ClickAsync();
        Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        Assert.Equal("5", await page.Locator(North).GetAttributeAsync("data-cfx-y"));
        Assert.Equal("0", await page.Locator(North).GetAttributeAsync("data-cfx-series"));
        Assert.Equal("Original measurements", await page.Locator("#host-status").InnerTextAsync());
        await CaptureAsync(page, "executable-demo-remounted-" + (dark ? "dark" : "light"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task HiddenSlottedHostResizesReplacesAndExportsTheCurrentSceneThenReconnectsWithFreshOwnership() {
        if (!Enabled) return;
        await using var session = await OpenAsync(RuntimePage(), 400, 650);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
                const host=document.createElement('div');host.id='chart-host';host.hidden=true;document.body.appendChild(host);
                host.attachShadow({mode:'open'}).innerHTML='<slot name="chart"></slot>';
            }
            """);
        var fragment = Readings(dark: true).ToInteractiveHtmlFragmentWithoutAssets(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Export);
        });
        await page.EvaluateAsync("""
            html => {
                const host=document.querySelector('#chart-host');host.insertAdjacentHTML('beforeend',html);
                const root=host.querySelector('.cfx-interactive-chart');root.slot='chart';
                window.chartController=ChartForgeXCharts.mount(root);host.hidden=false;chartController.resize();
            }
            """, fragment);
        Assert.True(await page.Locator(North).IsVisibleAsync());
        await SelectAsync(page, North);
        Assert.Contains("5", await TooltipTextAsync(page));
        await CaptureAsync(page, "slotted-before");
        await UpdateAsync(page, Readings(dark: true, updated: true).ToInteractiveHtmlFragmentWithoutAssets(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Export);
        }));
        // The slot is host-owned; a generated replacement cannot remove it from the retained root.
        Assert.Equal("chart", await page.Locator(Root).GetAttributeAsync("slot"));
        await page.SetViewportSizeAsync(520, 680);
        await page.EvaluateAsync("()=>chartController.resize()");
        var box = await BoxAsync(page, ".cfx-stage svg");
        Assert.InRange(box.Width, 100, 520);
        Assert.True(box.Height > 100);
        await page.Locator("[data-cfx-export=svg]").FocusAsync();
        await page.Locator(North).FocusAsync();
        Assert.Contains("17", await TooltipTextAsync(page));
        var exports = new Dictionary<string, int>();
        foreach (var format in new[] { "svg", "png" }) {
            var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=" + format + "]").ClickAsync());
            var path = await download.PathAsync() ?? throw new InvalidOperationException("The browser export has no local artifact.");
            var data = await File.ReadAllBytesAsync(path);
            exports.Add(format, data.Length);
            var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(captures)) {
                if (!Path.IsPathRooted(captures)) throw new InvalidOperationException("Capture output must be an absolute path.");
                Directory.CreateDirectory(captures);
                await download.SaveAsAsync(Path.Combine(captures, "lifecycle-updated-export." + format));
            }
            if (format == "png") Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, data.Take(8).ToArray());
            else {
                var svg = XDocument.Parse(System.Text.Encoding.UTF8.GetString(data));
                Assert.Equal("svg", svg.Root!.Name.LocalName);
                var north = Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-observation-id") == "north");
                Assert.Equal("17", (string?)north.Attribute("data-cfx-y"));
            }
        }
        await CaptureAsync(page, "slotted-resize-export", new { box, exports });
        var ownership = await page.EvaluateAsync<JsonElement>("""
            async () => {
                const root=chartController.root, old=chartController;root.remove();
                await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
                document.querySelector('#chart-host').appendChild(root);
                const fresh=ChartForgeXCharts.mount(root);window.chartController=fresh;
                let events=0;root.addEventListener('cfxselect',()=>events++);
                root.querySelector('g[data-cfx-role=point][data-cfx-observation-id=north]').dispatchEvent(new MouseEvent('click',{bubbles:true}));
                return {fresh:fresh!==old,sameRoot:fresh.root===root,events};
            }
            """);
        Assert.True(ownership.GetProperty("fresh").GetBoolean());
        Assert.True(ownership.GetProperty("sameRoot").GetBoolean());
        Assert.Equal(1, ownership.GetProperty("events").GetInt32());
        AssertNoConsoleErrors(session);
    }
}
