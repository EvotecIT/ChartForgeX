using ChartForgeX.Interactivity.Html;
using ChartForgeX.Stories;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class StoryChapterPlayerBrowserTests {
    [Fact]
    public async Task AutomaticChaptersUseAuthoredBoundariesWhileButtonsSeekReadableSamples() {
        if (!Enabled) return;
        var story = VisualStory.Create("Chapter clocks").WithSize(480, 320);
        story.Scene("first", "First", .6).Panel("first", new VisualStoryTextSurface("First"));
        story.Scene("middle", "Middle", .35).Panel("middle", new VisualStoryTextSurface("Middle"));
        story.Scene("last", "Last", .5).Panel("last", new VisualStoryTextSurface("Last"));
        story.Outcome("last", "Last", "last");
        var svg = story.Prepare(new VisualStoryPlaybackOptions(transition: TimeSpan.FromSeconds(.4)))
            .ToAnimatedSvg(new VisualStoryFrameOptions(2));
        await using var session = await OpenAsync(new HtmlMotionPlayerRenderer().RenderPage(svg));
        var page = session.Page;
        var seek = page.GetByRole(Microsoft.Playwright.AriaRole.Slider, new() { Name = "Story position" });
        var active = page.Locator("nav button[aria-current='true']");
        await seek.PressAsync("Home");
        for (var index = 0; index < 50; index++) await seek.PressAsync("ArrowRight");
        Assert.Equal("0.5", await seek.InputValueAsync());
        Assert.Equal("First", await active.InnerTextAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new() { Path = Path.Combine(capture, "story-authored-chapter-boundary.png"), FullPage = true });
        }
        await page.GetByRole(Microsoft.Playwright.AriaRole.Button, new() { Name = "Middle", Exact = true }).ClickAsync();
        Assert.Equal("0.5", await seek.InputValueAsync());
        Assert.Equal("Middle", await active.InnerTextAsync());
        await seek.PressAsync("ArrowRight");
        Assert.Equal("First", await active.InnerTextAsync());
        for (var index = 0; index < 9; index++) await seek.PressAsync("ArrowRight");
        Assert.Equal("0.6", await seek.InputValueAsync());
        Assert.Equal("Middle", await active.InnerTextAsync());
        AssertNoConsoleErrors(session);
    }
}
