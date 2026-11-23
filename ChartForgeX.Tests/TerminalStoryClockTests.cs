using ChartForgeX.Themes;
using ChartForgeX.Terminal;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TerminalStoryClockTests {
    [Fact]
    public void CommandIsCompleteAtTheExactAccumulatedEndTick() {
        var story = Terminal().InitialDelaySeconds(.1).CharactersPerSecond(42).LineDelaySeconds(0).Command("AB", .2);
        var completed = Pixels(story, null);
        Assert.Equal(completed, Pixels(story, .3));
        Assert.NotEqual(completed, Pixels(story, .3 - .0000001));
    }

    [Fact]
    public void PartialCommandRevealsTheExactNumberOfWholeElements() {
        var story = Terminal().Command(new string('A', 89), 1); // The prompt is the ninetieth element.
        var expected = Terminal().Command(new string('A', 62), 1);
        Assert.Equal(Pixels(expected, null), Pixels(story, .7));
    }

    [Fact]
    public void TabsActivateAtTheExactAccumulatedStartTick() {
        var story = Terminal().InitialDelaySeconds(.1).CharactersPerSecond(42).LineDelaySeconds(0).Pause(.2)
            .OpenTab("other", "Other", TerminalDialect.Bash, "/", TerminalThemes.Default(), transitionSeconds: 0);
        var layout = TerminalStoryLayout.Build(story);
        Assert.True(layout.TabVisible("other", .3));
        Assert.Equal(1, layout.TabOpacity("other", .3));
        Assert.False(layout.TabVisible("other", .3 - .0000001));
        Assert.Equal(0, layout.TabOpacity("other", .3 - .0000001));
    }

    private static TerminalStory Terminal() => TerminalStory.Create().Width(1200)
        .Dialect(TerminalDialect.Custom, ">").InitialDelaySeconds(0).CharactersPerSecond(42).LineDelaySeconds(0).ShowFinalPrompt(false);

    private static byte[] Pixels(TerminalStory story, double? elapsed) => PngTerminalStoryRenderer.RenderImage(
        story, TerminalStoryLayout.Build(story), TerminalTabRasterFonts.WithOutline(story, null), 1, elapsed).Pixels;
}
