using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryTranscriptBoundaryTests {
    [Fact]
    public void AuthoredLanguageIsNotGuessedForStoriesOrTheirPlayer() {
        var story = VisualStory.Create("Napraw skrypt").WithSize(480, 320);
        story.Scene("result", "Wynik", 1).Panel("result", new VisualStoryTextSurface("Witaj świecie"));
        story.Outcome("ready", "Gotowe", "result");
        var prepared = story.Prepare();
        foreach (var timestamp in new[] { TimeSpan.Zero, prepared.ContentDuration }) {
            var frame = prepared.PrepareFrame(timestamp);
            Assert.Null(frame.Accessibility.Language);
            var svg = XDocument.Parse(frame.ToSvg()).Root!;
            Assert.Null(svg.Attribute("lang"));
            Assert.Null(svg.Attribute(XNamespace.Xml + "lang"));
            Assert.Contains("Witaj świecie", frame.Accessibility.Description);
        }
        Assert.DoesNotContain("<html lang=\"en\">", story.ToHtmlPage());
        Assert.DoesNotContain("<html lang=\"en\">", new HtmlMotionPlayerRenderer().RenderPage(prepared.ToAnimatedSvg()));
        Assert.DoesNotContain("<html lang=\"en\">", TerminalStory.Create().Output("Gotowe").ToHtmlPage());
    }

    [Fact]
    public void TerminalTranscriptRetainsTabsPromptsBlankLinesAndCompleteTableCells() {
        var terminal = TerminalStory.Create().WithTitle("Main").WithDialect(TerminalDialect.Custom, "> ")
            .DeclareTab("build", "Build", TerminalDialect.Custom, ".", TerminalTheme.GraphiteDark(), customPrompt: "$ ")
            .Command("echo hello", .05).Output("hello\r\n").Blank().Pause(.05)
            .SelectTab("build", 0).Table(TerminalTable.Create().WithColumns("Name", "Value").AddRow("long complete value", "42"))
            .OpenTab("result", "Result", TerminalDialect.Custom, ".", TerminalTheme.GraphiteDark(), customPrompt: "# ", transitionSeconds: 0)
            .Output("ready");
        var expected = string.Join(Environment.NewLine, new[] {
            "[Tab added: Build]", "[Main] > echo hello", "[Main] hello", "[Main] ", "[Main]", "[Tab: Build]",
            "[Build] Name | Value", "[Build] long complete value | 42", "[Tab: Result]", "[Result] ready", "[Result] # "
        });
        Assert.Equal(expected, terminal.ToTranscript());
        Assert.Equal(expected, string.Join(Environment.NewLine, TerminalStoryLayout.Build(terminal).TranscriptLines));
        Assert.Equal(expected, new VisualStoryTerminalSurface(terminal).AccessibleText);
    }

    [Fact]
    public void AuthoredTerminalTranscriptBoundsRepeatedPromptsBeforeJoiningLines() {
        var terminal = TerminalStory.Create().WithFinalPrompt(false).WithTiming(0, 42, 0)
            .WithDialect(TerminalDialect.Custom, new string('>', 170000));
        for (var index = 0; index < 100; index++) terminal.Command("A", .05);
        var error = Assert.Throws<InvalidOperationException>(() => terminal.ToTranscript());
        Assert.Contains("transcript", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Throws<InvalidOperationException>(() => new VisualStoryTerminalSurface(terminal));
    }
}
