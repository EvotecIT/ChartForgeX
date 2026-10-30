using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ServerRenderedPageBrowserTests {
    [BrowserTheory(RequiresGallery = true)]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData("light", 1200)]
    [InlineData("dark", 1200)]
    [InlineData("light", 390)]
    [InlineData("dark", 390)]
    public async Task EmbeddingPageKeepsReadableTracksAndNativeNavigationAtHostWidths(string theme, int width) {
        var gallery = BrowserTestPrerequisites.RequireGalleryUrl();
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body></body></html>", width, 900);
        var response = await session.Page.GotoAsync(new Uri(new Uri(gallery), "embedding/" + theme + ".html").AbsoluteUri);
        Assert.Equal(200, response!.Status);
        await session.Page.EvaluateAsync("async () => { await document.fonts.ready; }");
        var measured = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const tracks = [...document.querySelectorAll('.strip-row')].map(row => {
                    const label = row.querySelector('.row-label').getBoundingClientRect();
                    const track = row.querySelector('.track').getBoundingClientRect();
                    return { label:label.toJSON(), track:track.toJSON(), height:row.querySelector('svg').getBoundingClientRect().height };
                });
                const mark = document.querySelector('.activity-mark');
                const text = document.querySelector('svg text');
                const fill = [...mark.querySelectorAll('[fill]')].find(node => node.getAttribute('fill').includes('var(--updates'));
                return { scripts:document.querySelectorAll('script').length, overflow:document.documentElement.scrollWidth > innerWidth,
                    tracks, links:document.querySelectorAll('.activity-mark,.publisher-mark').length,
                    font:getComputedStyle(text).fontSize, color:getComputedStyle(fill).fill,
                    fades:[...document.querySelectorAll('linearGradient')].map(gradient=>({
                        first:getComputedStyle(gradient.firstElementChild).stopColor,
                        last:getComputedStyle(gradient.lastElementChild).stopColor,
                        opacity:getComputedStyle(gradient.lastElementChild).stopOpacity })),
                    current:document.querySelector('.support-row[aria-current="true"]')?.getAttribute('data-release'),
                    css:getComputedStyle(document.documentElement).getPropertyValue('--updates').trim() };
            }
            """);
        Assert.Equal(0, measured.GetProperty("scripts").GetInt32());
        Assert.False(measured.GetProperty("overflow").GetBoolean());
        Assert.Equal(10, measured.GetProperty("links").GetInt32());
        Assert.Equal("14px", measured.GetProperty("font").GetString());
        Assert.All(measured.GetProperty("fades").EnumerateArray(), stop => {
            Assert.Equal("0", stop.GetProperty("opacity").GetString());
            Assert.Equal(stop.GetProperty("first").GetString(), stop.GetProperty("last").GetString());
        });
        Assert.Equal("release-2026", measured.GetProperty("current").GetString());
        Assert.Equal(theme == "dark" ? "rgb(110, 174, 255)" : "rgb(42, 120, 214)", measured.GetProperty("color").GetString());
        foreach (var row in measured.GetProperty("tracks").EnumerateArray()) {
            var label = row.GetProperty("label"); var track = row.GetProperty("track");
            Assert.Equal(48, row.GetProperty("height").GetDouble());
            if (width == 390) Assert.True(label.GetProperty("bottom").GetDouble() <= track.GetProperty("top").GetDouble());
            else Assert.True(label.GetProperty("right").GetDouble() <= track.GetProperty("left").GetDouble());
        }
        var publishers = session.Page.Locator(".publisher-mark");
        Assert.Equal("_blank", await publishers.First.GetAttributeAsync("target"));
        Assert.Equal("noopener noreferrer", await publishers.First.GetAttributeAsync("rel"));
        var activity = session.Page.Locator(".activity-mark").First;
        Assert.Equal("0", await activity.GetAttributeAsync("tabindex"));
        await activity.FocusAsync();
        await session.Page.Keyboard.PressAsync("Tab");
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.activeElement?.matches('a[data-day]') === true"));
        await activity.ClickAsync();
        Assert.EndsWith("#day-2026-07-13", session.Page.Url, StringComparison.Ordinal);
        var inherited = await session.Page.EvaluateAsync<string>("""
            () => { document.documentElement.style.setProperty('--chart-font-size','17px');
                const size=getComputedStyle(document.querySelector('svg text')).fontSize;
                document.documentElement.style.removeProperty('--chart-font-size'); return size; }
            """);
        Assert.Equal("17px", inherited);
        await session.Page.EvaluateAsync("() => scrollTo(0,0)");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions {
                Path = Path.Combine(captures, "embedding-" + theme + "-" + width + ".png"), FullPage = true
            });
        }
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(ChartDataTableMode.Details)]
    [InlineData(ChartDataTableMode.VisuallyHidden)]
    public async Task NativeDataFallbackRemainsAvailableOnScreenAndInPrint(ChartDataTableMode mode) {
        var chart = Chart.Create().WithTitle("Requests").WithSize(500, 220)
            .AddBar("API", new[] { new ChartPoint(1, 24), new ChartPoint(2, 18) });
        var fragment = new HtmlChartRenderer().RenderFragment(chart, new VisualSvgOptions("requests"), mode);
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body>" + fragment + "</body></html>", 600, 500);
        if (mode == ChartDataTableMode.Details) {
            Assert.False(await session.Page.Locator("table").IsVisibleAsync());
            await session.Page.Locator("summary").ClickAsync();
            Assert.True(await session.Page.Locator("table").IsVisibleAsync());
            await session.Page.Locator("summary").ClickAsync();
        } else {
            var box = await session.Page.Locator(".chartforgex-data-table-hidden").BoundingBoxAsync();
            Assert.Equal(1, box!.Width);
            Assert.Null(await session.Page.Locator(".chartforgex-data-table-hidden").GetAttributeAsync("aria-hidden"));
        }
        await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
        Assert.True(await session.Page.Locator("table").IsVisibleAsync());
        var printed = await session.Page.Locator("table").BoundingBoxAsync();
        Assert.True(printed!.Width > 100);
        Assert.True(printed.Height > 30);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
