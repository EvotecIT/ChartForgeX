using System.Text.Json;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Required")]
public sealed class TopologyHierarchyGalleryBrowserTests {
    [BrowserTheory(RequiresGallery = true)]
    [InlineData("service-regions", 1440, false)]
    [InlineData("service-regions", 1440, true)]
    [InlineData("service-regions", 390, true)]
    [InlineData("timing-hierarchy", 1440, false)]
    [InlineData("timing-hierarchy", 390, true)]
    [InlineData("directory-topology", 1440, false)]
    [InlineData("directory-topology", 1440, true)]
    [InlineData("directory-topology", 390, true)]
    public async Task GalleryKeepsReadableNamesAndLinkedViewEvents(string name, int width, bool dark) {
        var stem = name + "-" + width + (dark ? "-dark" : "-light");
        var url = new Uri(new Uri(BrowserTestPrerequisites.RequireGalleryUrl()), stem + ".html").AbsoluteUri;
        await using var session = await HtmlBrowser.OpenSessionAsync(url, new HtmlBrowserLaunchOptions {
            Browser = Engine, Headless = true, ViewportWidth = width, ViewportHeight = 900, Timeout = 15000
        });
        var page = session.Page;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var root = page.Locator(".cfx-topology-wrapper");
        Assert.Equal("true", await root.GetAttributeAsync("data-cfx-runtime-bound"));
        if (name != "timing-hierarchy") Assert.True(await page.Locator(".cfx-region-chart svg:visible").CountAsync() == 1,
            await page.EvaluateAsync<string>("()=>JSON.stringify([...document.querySelectorAll('.cfx-region-chart,.cfx-region-desktop,.cfx-region-mobile,.cfx-region-print,svg')].map(e=>({tag:e.tagName,cls:e.getAttribute('class'),display:getComputedStyle(e).display,width:e.getBoundingClientRect().width,height:e.getBoundingClientRect().height})))"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, stem + ".png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
        }
        await page.EvaluateAsync("""
            () => {
              window.topologyEvents = [];
              for (const name of ['cfx-topology-select', 'cfx-topology-hover'])
                document.addEventListener(name, event => topologyEvents.push({name, id:event.detail.id,
                  label:event.detail.label, subtitle:event.detail.subtitle, healthRing:event.detail.healthRing}));
            }
            """);
        var node = page.Locator("[data-cfx-role=topology-node]:visible").First;
        var id = await node.GetAttributeAsync("data-node-id");
        var label = await node.GetAttributeAsync("data-node-label");
        Assert.Contains(label!, await node.GetAttributeAsync("aria-label"));
        if (name != "timing-hierarchy") {
            var svg = page.Locator(".cfx-region-chart svg:visible");
            Assert.Equal("0", await svg.GetAttributeAsync("tabindex"));
            await svg.FocusAsync();
            await svg.PressAsync("Enter");
        } else {
            Assert.Equal("0", await node.GetAttributeAsync("tabindex"));
            await node.PressAsync("Enter");
        }
        Assert.Equal("true", await node.GetAttributeAsync("aria-selected"));
        await MoveToAsync(page, "[data-cfx-role=topology-node]:visible");
        var events = await page.EvaluateAsync<JsonElement>("()=>topologyEvents");
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("name").GetString() == "cfx-topology-select" && item.GetProperty("id").GetString() == id);
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("name").GetString() == "cfx-topology-hover" && item.GetProperty("id").GetString() == id);
        if (name != "timing-hierarchy") {
            Assert.Equal(5, await page.Locator(".cfx-node:visible .core").CountAsync());
            Assert.Contains(events.EnumerateArray(), item => item.GetProperty("healthRing").GetBoolean());
        }
        await node.PressAsync("Escape");
        await node.ClickAsync();
        Assert.Equal("true", await node.GetAttributeAsync("aria-selected"));
        await node.PressAsync("ArrowRight");
        Assert.NotEqual(id, await page.Locator(":focus").GetAttributeAsync("data-node-id"));
        Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth"));
        if (!string.IsNullOrWhiteSpace(capture)) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, stem + "-keyboard.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
        }
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory(RequiresGallery = true)]
    [InlineData("service-regions")]
    [InlineData("directory-topology")]
    public async Task PrintUsesLightTokensAndIncludesAccessibleDetails(string name) {
        var url = new Uri(new Uri(BrowserTestPrerequisites.RequireGalleryUrl()), name + "-1440-dark.html").AbsoluteUri;
        await using var session = await HtmlBrowser.OpenSessionAsync(url, new HtmlBrowserLaunchOptions {
            Browser = Engine, Headless = true, ViewportWidth = 1440, ViewportHeight = 900, Timeout = 15000
        });
        await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print, ReducedMotion = ReducedMotion.Reduce });
        Assert.Equal(1, await session.Page.Locator(".cfx-region-print:visible").CountAsync());
        Assert.Equal(2, await session.Page.Locator(".cfx-region-details table:visible").CountAsync());
        Assert.Equal("rgb(31, 138, 76)", await session.Page.Locator(".cfx-region-print .cfx-node.tone-good .core ellipse").First.EvaluateAsync<string>("element=>getComputedStyle(element).stroke"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) await session.Page.ScreenshotAsync(new PageScreenshotOptions {
            Path = Path.Combine(capture, name + "-print.png"), FullPage = true, Animations = ScreenshotAnimations.Disabled
        });
        AssertNoConsoleErrors(session);
    }

    [BrowserFact(RequiresGallery = true)]
    public async Task RegionToneUpdatesKeepGeometryAndRespectReducedMotion() {
        var url = new Uri(new Uri(BrowserTestPrerequisites.RequireGalleryUrl()), "service-regions-1440-light.html").AbsoluteUri;
        await using var session = await HtmlBrowser.OpenSessionAsync(url, new HtmlBrowserLaunchOptions { Browser = Engine, Headless = true, ViewportWidth = 1440, ViewportHeight = 900 });
        await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
              const root = document.querySelector('.cfx-region-chart');
              const node = root.querySelector('.cfx-region-desktop [data-node-id="gateway"]');
              const mark = node.querySelector('.core ellipse');
              const before = {x:mark.getAttribute('cx'), y:mark.getAttribute('cy'), color:getComputedStyle(mark).stroke};
              root.dispatchEvent(new CustomEvent('cfx-topology-update-tones',{detail:{nodes:[{id:'gateway',status:'Critical',statusText:'Failing: connection lost'}]}}));
              return { before, after:{x:mark.getAttribute('cx'),y:mark.getAttribute('cy'),color:getComputedStyle(mark).stroke},
                transition:getComputedStyle(mark).transitionDuration, name:node.getAttribute('aria-label') };
            }
            """);
        Assert.Equal(result.GetProperty("before").GetProperty("x").GetString(), result.GetProperty("after").GetProperty("x").GetString());
        Assert.Equal(result.GetProperty("before").GetProperty("y").GetString(), result.GetProperty("after").GetProperty("y").GetString());
        Assert.NotEqual(result.GetProperty("before").GetProperty("color").GetString(), result.GetProperty("after").GetProperty("color").GetString());
        Assert.Equal("0s", result.GetProperty("transition").GetString());
        Assert.Contains("Primary role", result.GetProperty("name").GetString());
        Assert.Contains("Failing: connection lost", result.GetProperty("name").GetString());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory(RequiresGallery = true)]
    [InlineData(1440, false)]
    [InlineData(390, true)]
    public async Task RegionRelationshipsEmitHostSelectionEventsWithModelDetails(int width, bool dark) {
        var url = new Uri(new Uri(BrowserTestPrerequisites.RequireGalleryUrl()), "service-regions-" + width + (dark ? "-dark.html" : "-light.html")).AbsoluteUri;
        await using var session = await HtmlBrowser.OpenSessionAsync(url, new HtmlBrowserLaunchOptions { Browser = Engine, Headless = true, ViewportWidth = width, ViewportHeight = 900 });
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.edgeEvents=[];document.addEventListener('edge:select',e=>edgeEvents.push(e.detail));}");
        var variant = width < 640 ? ".cfx-region-mobile:visible" : ".cfx-region-desktop:visible";
        var edge = page.Locator(variant).Locator("[data-cfx-role=topology-edge][data-edge-id=mirror]").First;
        if (width < 640) await edge.Locator("text").ClickAsync();
        else {
            var point = await edge.Locator("path").First.EvaluateAsync<JsonElement>("path=>{const p=path.getPointAtLength(20);const t=p.matrixTransform(path.getScreenCTM());return {x:t.x,y:t.y};}");
            await page.Mouse.ClickAsync((float)point.GetProperty("x").GetDouble(), (float)point.GetProperty("y").GetDouble());
        }
        var result = await page.EvaluateAsync<JsonElement>("()=>edgeEvents");
        Assert.Equal(1, result.GetArrayLength());
        Assert.Equal("mirror", result[0].GetProperty("id").GetString());
        Assert.Equal("gateway", result[0].GetProperty("sourceNodeId").GetString());
        Assert.Equal("replica", result[0].GetProperty("targetNodeId").GetString());
        Assert.Equal("Every 15 minutes", result[0].GetProperty("metadata").GetProperty("schedule").GetString());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact(RequiresGallery = true)]
    public async Task RegionContainersWrapBeforeThePhoneBreakpointWithoutAnotherLayoutEngine() {
        var url = new Uri(new Uri(BrowserTestPrerequisites.RequireGalleryUrl()), "service-regions-1440-light.html").AbsoluteUri;
        await using var session = await HtmlBrowser.OpenSessionAsync(url, new HtmlBrowserLaunchOptions { Browser = Engine, Headless = true, ViewportWidth = 1440, ViewportHeight = 900 });
        var page = session.Page;
        await page.Locator(".cfx-region-chart").EvaluateAsync("element=>element.style.width='700px'");
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-region-band=\"640\"]').style.display==='block'");
        Assert.Equal(1, await page.Locator(".cfx-region-chart svg:visible").CountAsync());
        var regions = page.Locator(".cfx-region-desktop:visible .cfx-region");
        var first = await regions.Nth(0).Locator("rect").First.GetAttributeAsync("y");
        var second = await regions.Nth(1).Locator("rect").First.GetAttributeAsync("y");
        Assert.True(double.Parse(second!, System.Globalization.CultureInfo.InvariantCulture) > double.Parse(first!, System.Globalization.CultureInfo.InvariantCulture));
        await page.Locator(".cfx-region-chart").EvaluateAsync("element=>element.style.width='390px'");
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-region-mobile').style.display==='block'");
        Assert.Equal(1, await page.Locator(".cfx-region-chart svg:visible").CountAsync());
        Assert.Contains("Links to other regions", await page.Locator(".cfx-region-mobile").InnerTextAsync());
        AssertNoConsoleErrors(session);
    }
}
