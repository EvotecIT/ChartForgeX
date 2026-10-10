using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Stories;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryDefaultSamplingTests {
    [Theory]
    [InlineData(480)]
    [InlineData(900)]
    public async Task DefaultLongStoryChaptersRemainSeekableInThePlayer(int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var svg = LongStory(240).Prepare().ToAnimatedSvg();
        await using var session = await InteractiveChartBrowser.OpenAsync(new HtmlMotionPlayerRenderer().RenderPage(svg), width, 560);
        var page = session.Page;
        var seek = page.GetByRole(Microsoft.Playwright.AriaRole.Slider, new() { Name = "Story position" });
        await page.GetByRole(Microsoft.Playwright.AriaRole.Button, new() { Name = "Chapter 2", Exact = true }).ClickAsync();
        Assert.Equal("120", await seek.InputValueAsync());
        Assert.Equal("Chapter 2", await page.Locator("nav button[aria-current='true']").InnerTextAsync());
        Assert.Equal(new[] { "scene2" }, await page.EvaluateAsync<string[]>(
            "() => [...document.querySelectorAll('[data-cfx-frame-time]')].filter(f => getComputedStyle(f).display !== 'none' && Number(getComputedStyle(f).opacity) > .5).map(f => f.dataset.cfxScene)"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new() { Path = Path.Combine(capture, "long-story-chapter-" + width + ".png"), FullPage = true });
        }
        await seek.PressAsync("End");
        Assert.Equal("241.5", await seek.InputValueAsync());
        Assert.Equal("Chapter 3", await page.Locator("nav button[aria-current='true']").InnerTextAsync());
        await page.GetByRole(Microsoft.Playwright.AriaRole.Button, new() { Name = "Restart story", Exact = true }).ClickAsync();
        Assert.Equal("0", await seek.InputValueAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(120)]
    [InlineData(240)]
    public void DefaultSvgAndHtmlKeepLongStoriesWithinTheFrameBudget(int seconds) {
        var story = LongStory(seconds);
        var prepared = story.Prepare();
        var svg = prepared.ToAnimatedSvg();
        AssertCovered(prepared, svg, 600);
        Assert.Contains(svg, story.ToHtmlPage());
        Assert.Contains("data-cfx-chapter-id=\"scene1\"", new HtmlMotionPlayerRenderer().RenderPage(svg));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SamplingThatNeedsMoreThanTheDefaultBudgetRequiresExplicitOptions(bool shortChapter) {
        var story = shortChapter ? ShortFinalChapter() : LongStory(600);
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(
            endHold: shortChapter ? TimeSpan.Zero : TimeSpan.FromSeconds(1.5), transition: TimeSpan.Zero));
        var failure = Assert.Throws<InvalidOperationException>(() => prepared.ToAnimatedSvg());
        Assert.Contains("600", failure.Message);
        Assert.Contains("VisualStoryFrameOptions", failure.Message);
        var rate = shortChapter ? 12 : 2;
        var frames = new VisualStoryFrameOptions(rate, maximumFrames: 1800);
        AssertCovered(prepared, prepared.ToAnimatedSvg(frames), frames.MaximumFrames);
        Assert.Throws<InvalidOperationException>(() => prepared.ToAnimatedSvg(new VisualStoryFrameOptions(rate)));
    }

    private static VisualStory LongStory(int seconds) {
        var story = VisualStory.Create("Long story").WithSize(480, 320);
        for (var index = 0; index < seconds / 60; index++)
            story.Scene("scene" + index, "Chapter " + index, 60).Panel("result", new VisualStoryTextSurface("Chapter " + index));
        return story.Outcome("ready", "Ready", "result");
    }

    private static VisualStory ShortFinalChapter() {
        var story = VisualStory.Create("Short completed chapter").WithSize(480, 320);
        story.Scene("first", "First", 59.501).Panel("result", new VisualStoryTextSurface("First"));
        story.Scene("last", "Last", .25).Panel("result", new VisualStoryTextSurface("Last"));
        return story.Outcome("ready", "Ready", "result");
    }

    private static void AssertCovered(PreparedVisualStory prepared, string svg, int maximumFrames) {
        var document = XDocument.Parse(svg);
        var frames = document.Root!.Elements().Where(element => element.Attribute("data-cfx-frame-time") != null).ToArray();
        Assert.InRange(frames.Length, 2, maximumFrames);
        foreach (var chapter in prepared.Chapters)
            Assert.Contains(frames, frame => (string?)frame.Attribute("data-cfx-scene") == chapter.Id);
        Assert.Equal(prepared.Chapters[^1].Id, (string?)frames[^1].Attribute("data-cfx-scene"));
        Assert.Equal(prepared.Duration.TotalSeconds.ToString("0.#########", System.Globalization.CultureInfo.InvariantCulture),
            (string?)document.Root.Attribute("data-cfx-motion-duration"));
    }
}
