using System.Xml.Linq;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryReplayViewportReviewTests {
    [Theory]
    [InlineData(TerminalDialect.Custom)]
    [InlineData(TerminalDialect.PowerShell)]
    public void ReplayTranscriptBoundsExpandedCustomPromptsAndDirectories(TerminalDialect dialect) {
        var metadata = new string('>', 32768);
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(2), dialect, workingDirectory: metadata, customPrompt: metadata);
        for (var index = 0; index < 600; index++) replay.Command(TimeSpan.FromTicks(index), "A");
        var error = Assert.Throws<InvalidOperationException>(() => replay.ToTranscript());
        Assert.Contains("transcript", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() => new VisualStoryReplaySurface(replay));
    }

    [Fact]
    public void AuthoredViewportPreservesPaletteAlphaOutsideATabTransition() {
        var theme = TerminalTheme.GraphiteDark();
        theme.Background = theme.Background.WithAlpha(64);
        theme.Border = theme.Border.WithAlpha(128);
        var terminal = TerminalStory.Create().WithTheme(theme).WithFinalPrompt(false).Output("Ready");
        var viewport = XDocument.Parse(Story(terminal, 600, 400).Prepare().ToSvg()).Descendants()
            .Single(element => (string?)element.Attribute("data-cfx-role") == "terminal-viewport");
        Assert.Equal(theme.Background.ToCss(), (string?)viewport.Attribute("fill"));
        Assert.Equal(theme.Border.ToCss(), (string?)viewport.Attribute("stroke"));
    }

    [Fact]
    public void ExplanationsEndRecordingWithoutChangingTheOriginalReplay() {
        var recording = StoryReplay.Create(TimeSpan.FromSeconds(3)).OpenTab(TimeSpan.Zero, "other", "Other")
            .Output(TimeSpan.FromSeconds(1), "Ready");
        var explained = recording.Explain(TimeSpan.FromSeconds(.5), "Watch the result");
        var at = TimeSpan.FromSeconds(2);
        foreach (var observation in new Action<StoryReplay>[] {
            replay => replay.Command(at, "run"), replay => replay.Output(at, "Later"), replay => replay.ReplaceLine(at, "Done"),
            replay => replay.Clear(at), replay => replay.Marker(at, "Recorded marker"), replay => replay.ChangeDirectory(at, "~/next"),
            replay => replay.OpenTab(at, "third", "Third"), replay => replay.SelectTab(at, "main")
        }) Assert.Throws<InvalidOperationException>(() => observation(explained));
        Assert.Equal(4, explained.Explain(at, "Another explanation").Events.Count);
        recording.Output(at, "New observation");
        Assert.DoesNotContain(explained.Events, item => item.Text == "New observation");
    }

    [Fact]
    public void AuthoredHistoryLimitAppliesBeforePixelViewportClipping() {
        var terminal = TerminalStory.Create().WithFinalPrompt(false).WithTiming(0, 42, .01)
            .Output(string.Join("\n", Enumerable.Range(1, 30).Select(index => "line" + index.ToString("000"))));
        var prepared = Story(terminal, 600, 1000, new VisualStoryTerminalOptions(fontSize: 14, historyLines: 20)).Prepare();
        var svg = prepared.ToSvg();
        var rows = Rows(svg);
        Assert.Equal(20, rows.Length);
        Assert.Equal("line011", rows[0].Value);
        Assert.Equal("line030", rows[^1].Value);
        Assert.Contains("↑ 10 earlier lines", svg);
        Assert.Contains("line001", prepared.ToTranscript());
        Assert.Equal(2, Rows(prepared.ToSvg(TimeSpan.FromSeconds(.015))).Length);
    }

    [Theory]
    [InlineData(.25)]
    [InlineData(.75)]
    public void AuthoredTabTransitionRetainsBothVisibleSessions(double progress) {
        var terminal = TransitionTerminal();
        var start = TerminalStoryLayout.BuildLogical(terminal).Transitions.Last().StartSeconds;
        var prepared = Story(terminal, 600, 400).Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero, 1));
        var timestamp = TimeSpan.FromSeconds(start + progress);
        var rows = Rows(prepared.ToSvg(timestamp));
        Assert.Contains(rows, row => row.Value == "FIRST");
        Assert.Contains(rows, row => row.Value == "SECOND");
        // A later tab's background must never cover an earlier tab's foreground.
        var viewport = XDocument.Parse(prepared.ToSvg(timestamp)).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-viewport").ToArray();
        Assert.Single(viewport);
        Assert.StartsWith("#", (string?)viewport[0].Attribute("fill"));
        var image = prepared.ToPng(timestamp);
        Assert.NotEqual(prepared.ToPng(TimeSpan.FromSeconds(start)), image);
        Assert.NotEqual(prepared.ToPng(TimeSpan.FromSeconds(start + 1)), image);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void FrameProducerAccountsForGeneratedReplayTranscripts(int scenes) {
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(2), TerminalDialect.Custom, customPrompt: new string('>', 2048));
        for (var index = 0; index < 1000; index++) replay.Command(TimeSpan.FromTicks(index), "A");
        var surface = new VisualStoryReplaySurface(replay);
        var story = VisualStory.Create("Transcript working memory").WithSize(480, 320);
        for (var index = 0; index < scenes; index++) story.Scene("run" + index, "Run", 2).Panel("terminal", surface);
        story.Outcome("ready", "Ready", "terminal");
        var prepared = story.Prepare(new VisualStoryPlaybackOptions(TimeSpan.Zero, TimeSpan.Zero, 1));
        var source = prepared.FrameSource(new VisualStoryFrameOptions(2));
        // Both the captured surface and prepared story retain UTF-16 transcript text.
        Assert.True(source.AdditionalWorkingBytes >= (surface.AccessibleText.Length + prepared.ToTranscript().Length) * 2L);
    }

    private static TerminalStory TransitionTerminal() => TerminalStory.Create().WithFinalPrompt(false).WithTiming(0, 42, 0).WithTabHold(0)
        .Output("FIRST").OpenTab("other", "Other", TerminalDialect.Bash, "~/src", TerminalTheme.GraphiteDark(), transitionSeconds: 0)
        .Output("SECOND").SelectTab("main", 1);

    private static VisualStory Story(TerminalStory terminal, int width, int height, VisualStoryTerminalOptions? options = null) {
        var story = VisualStory.Create("Authored terminal viewport").WithSize(width, height);
        story.Scene("run", "Run", 3).Panel("terminal", new VisualStoryTerminalSurface(terminal, options: options ?? new VisualStoryTerminalOptions()));
        story.Outcome("ready", "Ready", "terminal"); return story;
    }
    private static XElement[] Rows(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-viewport-text").ToArray();
}
