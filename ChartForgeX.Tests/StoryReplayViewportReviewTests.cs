using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
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
        var before = prepared.ToPng(TimeSpan.FromSeconds(start));
        var after = prepared.ToPng(TimeSpan.FromSeconds(start + 1));
        Assert.NotEqual(before, image);
        Assert.NotEqual(after, image);
        var pixels = PngReader.Decode(image);
        var first = rows.Single(row => row.Value == "FIRST");
        var second = rows.Single(row => row.Value == "SECOND");
        // Separate rows and saturated ink on black isolate each session's fade,
        // including attenuation by a mistakenly repainted tab background.
        Assert.True(InkCount(pixels, first, ChartColor.FromHex("#00FF00"), progress) >= 8, "The incoming session must retain its faded green glyphs.");
        Assert.True(InkCount(pixels, second, ChartColor.FromHex("#FF0000"), 1 - progress) >= 8, "The outgoing session must retain its faded red glyphs.");
        Assert.Equal(0, InkCount(PngReader.Decode(before), first, ChartColor.FromHex("#00FF00"), progress));
        Assert.Equal(0, InkCount(PngReader.Decode(after), second, ChartColor.FromHex("#FF0000"), 1 - progress));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            File.WriteAllBytes(Path.Combine(capture, "terminal-fade-" + progress.ToString("0.00", CultureInfo.InvariantCulture) + ".png"), image);
        }
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

    private static TerminalStory TransitionTerminal() {
        var theme = TerminalTheme.GraphiteDark();
        theme.Background = ChartColor.FromHex("#000000");
        theme.Success = ChartColor.FromHex("#00FF00");
        theme.Error = ChartColor.FromHex("#FF0000");
        return TerminalStory.Create().WithTheme(theme).WithFinalPrompt(false).WithTiming(0, 42, 0).WithTabHold(0)
            .Output("FIRST", TerminalTextTone.Success).OpenTab("other", "Other", TerminalDialect.Bash, "~/src", theme, transitionSeconds: 0)
            .Blank().Output("SECOND", TerminalTextTone.Error).SelectTab("main", 1);
    }

    private static int InkCount(RgbaImage image, XElement row, ChartColor color, double opacity) {
        var text = row.Descendants().Single(element => element.Name.LocalName == "text");
        double Number(string attribute) => double.Parse((string)text.Attribute(attribute)!, CultureInfo.InvariantCulture);
        var x = Number("x"); var y = Number("y"); var size = Number("font-size");
        var expected = new[] { color.R * opacity, color.G * opacity, color.B * opacity };
        var count = 0;
        for (var py = Math.Max(0, (int)Math.Floor(y - size - 2)); py < Math.Min(image.Height, Math.Ceiling(y + 2)); py++) {
            for (var px = Math.Max(0, (int)Math.Floor(x)); px < Math.Min(image.Width, Math.Ceiling(x + row.Value.Length * size)); px++) {
                var offset = (py * image.Width + px) * 4;
                if (Math.Abs(image.Pixels[offset] - expected[0]) <= 4 && Math.Abs(image.Pixels[offset + 1] - expected[1]) <= 4 &&
                    Math.Abs(image.Pixels[offset + 2] - expected[2]) <= 4) count++;
            }
        }
        return count;
    }

    private static VisualStory Story(TerminalStory terminal, int width, int height, VisualStoryTerminalOptions? options = null) {
        var story = VisualStory.Create("Authored terminal viewport").WithSize(width, height);
        story.Scene("run", "Run", 3).Panel("terminal", new VisualStoryTerminalSurface(terminal, options: options ?? new VisualStoryTerminalOptions()));
        story.Outcome("ready", "Ready", "terminal"); return story;
    }
    private static XElement[] Rows(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-viewport-text").ToArray();
}
