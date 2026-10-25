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
    [InlineData("service-regions", 390, true)]
    [InlineData("timing-hierarchy", 1440, false)]
    [InlineData("timing-hierarchy", 390, true)]
    [InlineData("directory-topology", 1440, false)]
    [InlineData("directory-topology", 390, true)]
    public async Task GalleryKeepsReadableNamesAndLinkedViewEvents(string name, int width, bool dark) {
        var stem = name + (dark ? "-390-dark" : "-1440-light");
        var url = new Uri(new Uri(BrowserTestPrerequisites.RequireGalleryUrl()), stem + ".html").AbsoluteUri;
        await using var session = await HtmlBrowser.OpenSessionAsync(url, new HtmlBrowserLaunchOptions {
            Browser = Engine, Headless = true, ViewportWidth = width, ViewportHeight = 900, Timeout = 15000
        });
        var page = session.Page;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var root = page.Locator(".cfx-topology-wrapper");
        Assert.Equal("true", await root.GetAttributeAsync("data-cfx-runtime-bound"));
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
        var node = page.Locator("[data-cfx-role=topology-node]").First;
        var id = await node.GetAttributeAsync("data-node-id");
        var label = await node.GetAttributeAsync("data-node-label");
        Assert.Contains(label!, await node.GetAttributeAsync("aria-label"));
        Assert.Equal("0", await node.GetAttributeAsync("tabindex"));
        await node.PressAsync("Enter");
        Assert.Equal("true", await node.GetAttributeAsync("aria-selected"));
        await MoveToAsync(page, "[data-cfx-role=topology-node]");
        var events = await page.EvaluateAsync<JsonElement>("()=>topologyEvents");
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("name").GetString() == "cfx-topology-select" && item.GetProperty("id").GetString() == id);
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("name").GetString() == "cfx-topology-hover" && item.GetProperty("id").GetString() == id);
        if (name != "timing-hierarchy") {
            Assert.Equal(4, await page.Locator("[data-cfx-role=topology-node-health-ring]").CountAsync());
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
}
