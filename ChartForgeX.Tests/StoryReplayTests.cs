using System.Xml.Linq;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryReplayTests {
    [Fact]
    public void NativeTerminalViewportUsesExactCompletionAndPartialElementTicks() {
        var terminal = TerminalStory.Create().WithDialect(TerminalDialect.Custom, ">").WithFinalPrompt(false)
            .WithTiming(.1, 42, 0).Command(new string('A', 89), 1);
        var story = VisualStory.Create("Exact terminal typing").WithSize(1200, 400);
        story.Scene("run", "Run", 2).Panel("terminal", new VisualStoryTerminalSurface(terminal, options: new VisualStoryTerminalOptions()));
        story.Outcome("terminal", "Ready", "terminal");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero, 1));
        Assert.Equal(">" + new string('A', 62), Visible(prepared.ToSvg(TimeSpan.FromTicks(8_000_000))));
        Assert.Equal(">" + new string('A', 89), Visible(prepared.ToSvg(TimeSpan.FromTicks(11_000_000))));
        Assert.Equal(prepared.ToPng(TimeSpan.FromSeconds(1.5)), prepared.ToPng(TimeSpan.FromTicks(11_000_000)));
    }

    [Theory]
    [InlineData(1200, true, 2)]
    [InlineData(1200, false, 2)]
    [InlineData(600, true, 4)]
    [InlineData(600, false, 2)]
    public void AuthoredViewportFitsLogicalLinesAgainstItsOwnWidth(int width, bool wrap, int expectedRows) {
        var text = string.Concat(Enumerable.Repeat("0123456789", 7));
        var terminal = TerminalStory.Create().WithWidth(480).WithTypography(24, 36).WithFinalPrompt(false)
            .WithTiming(0, 42, .08).Command(text, 1).Output(text);
        var story = VisualStory.Create("Logical terminal lines").WithSize(width, 500);
        story.Scene("run", "Run", 3).Panel("terminal", new VisualStoryTerminalSurface(terminal, options: new VisualStoryTerminalOptions(wrap: wrap)));
        story.Outcome("terminal", "Ready", "terminal");
        var svg = story.Prepare().ToSvg();
        var rows = XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-viewport-text").ToArray();
        Assert.Equal(expectedRows, rows.Length);
        if (width == 1200) {
            Assert.Contains(text, rows[0].Value, StringComparison.Ordinal);
            Assert.Contains(text, rows[1].Value);
        }
    }

    [Fact]
    public void ReplaySanitizesAllVisibleMetadataAndIdentifiesSelectedTabsInTheTranscript() {
        const string ansiStart = "\u001b[31m";
        const string ansiEnd = "\u001b[0m";
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(4), title: ansiStart + "Main" + ansiEnd)
            .Marker(TimeSpan.Zero, ansiStart + "Build" + ansiEnd)
            .OpenTab(TimeSpan.FromSeconds(1), "second", ansiStart + "Second" + ansiEnd)
            .SelectTab(TimeSpan.FromSeconds(2), "main")
            .Explain(TimeSpan.FromSeconds(3), ansiStart + "Result" + ansiEnd);
        Assert.Equal("Main", replay.Title);
        Assert.Equal("Build", replay.Events[0].Text);
        Assert.Equal("Second", replay.Events[1].TabTitle);
        Assert.Equal("Result", replay.Events[^1].Text);
        var transcript = replay.ToTranscript();
        Assert.DoesNotContain("\u001b", transcript, StringComparison.Ordinal);
        Assert.Contains("OpenTab: Second [second]", transcript);
        Assert.Contains("SelectTab: Main [main]", transcript);
        Assert.Contains("Result", Story(replay).Prepare().ToSvg());
        Assert.Throws<ArgumentException>(() => replay.Marker(TimeSpan.FromSeconds(3), "One\nTwo"));
    }

    [Fact]
    public void AuthoredViewportRetainsCompleteTableCellsAndTheFinalPrompt() {
        var text = string.Concat(Enumerable.Repeat("0123456789", 7));
        var terminal = TerminalStory.Create().WithWidth(480).WithTypography(24, 36).WithDialect(TerminalDialect.Custom, text)
            .Table(TerminalTable.Create().WithColumns("Value").AddRow(text));
        var story = VisualStory.Create("Logical table and prompt").WithSize(1200, 500);
        story.Scene("run", "Run", 3).Panel("terminal", new VisualStoryTerminalSurface(terminal, options: new VisualStoryTerminalOptions(wrap: false)));
        story.Outcome("terminal", "Ready", "terminal");
        var rows = XDocument.Parse(story.Prepare().ToSvg()).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-viewport-text").ToArray();
        Assert.Equal(4, rows.Length);
        Assert.Equal(text, rows[2].Value);
        Assert.Equal(text, rows[3].Value);
    }

    [Fact]
    public void ReplayPreservesTimedTabStateDirectoryClearAndProgressReplacement() {
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(10), workingDirectory: "~/demo")
            .Command(TimeSpan.FromSeconds(1), "Get-Status")
            .Output(TimeSpan.FromSeconds(2), "Starting")
            .ReplaceLine(TimeSpan.FromSeconds(3), "Ready", TerminalTextTone.Success)
            .OpenTab(TimeSpan.FromSeconds(4), "bash", "Build", TerminalDialect.Bash, "~/src")
            .Command(TimeSpan.FromSeconds(5), "make")
            .SelectTab(TimeSpan.FromSeconds(6), "main")
            .ChangeDirectory(TimeSpan.FromSeconds(7), "~/output")
            .Clear(TimeSpan.FromSeconds(8))
            .Command(TimeSpan.FromSeconds(9), "Get-Result");
        var prepared = Story(replay).Prepare();
        Assert.DoesNotContain("✓ Completed session", prepared.ToSvg(TimeSpan.Zero));
        Assert.Contains("✓ Completed session", prepared.ToSvg());
        var html = Story(replay).Prepare(new VisualStoryPlaybackOptions(TimeSpan.FromSeconds(2), TimeSpan.Zero, 1))
            .ToHtmlPage(new VisualStoryFrameOptions(2));
        Assert.Contains("data-cfx-motion-duration=\"12\"", html);
        Assert.Contains("data-cfx-motion-plays=\"1\"", html);
        Assert.DoesNotContain("<script", html);
        Assert.Equal("", Visible(prepared.ToSvg(TimeSpan.FromSeconds(.99))));
        Assert.Contains("Ready", Visible(prepared.ToSvg(TimeSpan.FromSeconds(3))));
        Assert.DoesNotContain("Starting", Visible(prepared.ToSvg(TimeSpan.FromSeconds(3))));
        Assert.Contains("~/src $ make", Visible(prepared.ToSvg(TimeSpan.FromSeconds(5))));
        Assert.Contains("Ready", Visible(prepared.ToSvg(TimeSpan.FromSeconds(6))));
        Assert.DoesNotContain("Ready", Visible(prepared.ToSvg(TimeSpan.FromSeconds(8))));
        Assert.Contains("~/output> Get-Result", Visible(prepared.ToSvg()));
        Assert.Contains("Starting", prepared.ToTranscript());
        replay.Output(TimeSpan.FromSeconds(10), "Later mutation");
        Assert.DoesNotContain("Later mutation", prepared.ToTranscript());
    }
    [Fact]
    public void TrimmingAndCompressionKeepOriginalTimestampsAndInitialScreenContext() {
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(100))
            .Output(TimeSpan.FromSeconds(10), "Context")
            .ChangeDirectory(TimeSpan.FromSeconds(20), "~/demo")
            .Command(TimeSpan.FromSeconds(60), "Get-Result")
            .Output(TimeSpan.FromSeconds(90), "Done");
        var trimmed = replay.Trim(TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(95)).CompressPauses(TimeSpan.FromSeconds(2));
        Assert.Equal(TimeSpan.FromSeconds(100), trimmed.OriginalDuration);
        Assert.Equal(TimeSpan.FromSeconds(6), trimmed.Duration);
        Assert.Equal(TimeSpan.Zero, trimmed.Events[0].Timestamp);
        Assert.Equal(TimeSpan.FromSeconds(60), trimmed.Events[2].OriginalTimestamp);
        Assert.Equal(TimeSpan.FromSeconds(2), trimmed.Events[2].Timestamp);
        var prepared = Story(trimmed).Prepare();
        Assert.Contains("Context", Visible(prepared.ToSvg(TimeSpan.Zero)));
        Assert.Contains("~/demo> Get-Result", Visible(prepared.ToSvg(TimeSpan.FromSeconds(2))));
        Assert.Equal(TimeSpan.FromSeconds(60), replay.Events[2].Timestamp);
    }
    [Fact]
    public void LongReplayScrollsAtFixedFontSizeAndRetainsItsCompleteTranscript() {
        var replay = StoryReplay.Create(TimeSpan.FromMinutes(5));
        for (var i = 1; i <= 250; i++) replay.Output(TimeSpan.FromSeconds(i), "line" + i.ToString("000"));
        var prepared = Story(replay).Prepare();
        var svg = prepared.ToSvg();
        Assert.Contains("line250", Visible(svg));
        Assert.DoesNotContain("line001", Visible(svg));
        Assert.Contains("line001", prepared.ToTranscript());
        Assert.Contains("earlier lines", svg);
        var text = XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-viewport-text").ToArray();
        Assert.InRange(text.Length, 1, 12);
        Assert.All(text, element => Assert.Equal("18", (string?)element.Descendants().Single(node => node.Name.LocalName == "text").Attribute("font-size")));
        Assert.True(prepared.ToPng().Length > 64);
    }
    [Fact]
    public void OutputSanitizesAnsiAndRequiresResolvedCarriageReturnOperations() {
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(2))
            .Output(TimeSpan.Zero, "\u001b[31mReady\u001b[0m\r\nNext");
        Assert.Equal("Ready\nNext", replay.Events[0].Text);
        Assert.Throws<ArgumentException>(() => replay.Output(TimeSpan.FromSeconds(1), "Progress\rDone"));
        Assert.Throws<ArgumentOutOfRangeException>(() => replay.Command(TimeSpan.FromSeconds(-1), "Get-Result"));
        Assert.Throws<ArgumentException>(() => replay.SelectTab(TimeSpan.FromSeconds(1), "missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => replay.Output(TimeSpan.FromSeconds(3), "outside"));
    }
    [Fact]
    public void InsertedExplanationsUseThePresentationClockWithoutInventingRecordingTime() {
        var source = StoryReplay.Create(TimeSpan.FromSeconds(20)).Output(TimeSpan.FromSeconds(10), "Ready");
        var explained = source.CompressPauses(TimeSpan.FromSeconds(2)).Explain(TimeSpan.FromSeconds(1), "Watch the result");
        Assert.Null(explained.Events[0].OriginalTimestamp);
        Assert.Equal(TimeSpan.FromSeconds(10), explained.Events[1].OriginalTimestamp);
        Assert.Contains("Watch the result", Story(explained).Prepare().ToSvg(TimeSpan.FromSeconds(1)));
        Assert.Single(source.Events);
        Assert.Throws<InvalidOperationException>(() => explained.Output(explained.Duration, "New recording"));
    }
    [Fact]
    public void AuthoredTerminalCanUseTheSameFixedNativeViewport() {
        var terminal = TerminalStory.Create().WithFinalPrompt(false).WithTiming(0, 42, .08).Command("Get-Status", 1).Output("Ready");
        var story = VisualStory.Create("Authored viewport").WithSize(600, 400);
        story.Scene("run", "Run", 2).Panel("terminal", new VisualStoryTerminalSurface(terminal, options: new VisualStoryTerminalOptions()));
        story.Outcome("terminal", "Ready", "terminal");
        var prepared = story.Prepare();
        Assert.DoesNotContain("Ready", Visible(prepared.ToSvg(TimeSpan.Zero)));
        Assert.Contains("Ready", Visible(prepared.ToSvg()));
        Assert.NotEqual(prepared.ToPng(TimeSpan.Zero), prepared.ToPng());
    }
    private static VisualStory Story(StoryReplay replay) {
        var story = VisualStory.Create("Recorded session").WithSize(600, 400);
        story.Scene("replay", "Replay", replay.Duration.TotalSeconds).Panel("terminal", new VisualStoryReplaySurface(replay));
        story.Outcome("terminal", "Completed session", "terminal"); return story;
    }
    private static string Visible(string svg) => string.Join("\n", XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-viewport-text").Select(element => element.Value));
}
