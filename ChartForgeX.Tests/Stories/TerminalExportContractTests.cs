using ChartForgeX.Primitives;
using ChartForgeX.Terminal;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TerminalExportContractTests {
    [Theory]
    [InlineData(TerminalWindowStyle.MacOS, VisualThemeMode.Dark)]
    [InlineData(TerminalWindowStyle.MacOS, VisualThemeMode.Light)]
    [InlineData(TerminalWindowStyle.WindowsTerminal, VisualThemeMode.Dark)]
    [InlineData(TerminalWindowStyle.WindowsTerminal, VisualThemeMode.Light)]
    [InlineData(TerminalWindowStyle.Linux, VisualThemeMode.Dark)]
    [InlineData(TerminalWindowStyle.Linux, VisualThemeMode.Light)]
    public void SvgChromeTitlesUseTheReviewedRegularPngTypography(TerminalWindowStyle style, VisualThemeMode mode) {
        var story = TerminalStory.Create().WithWindowStyle(style).WithThemeMode(mode)
            .WithTitle("Terminal export").WithFinalPrompt(false).Output("ready");
        var document = System.Xml.Linq.XDocument.Parse(story.ToSvg());
        var title = Assert.Single(document.Descendants(), element =>
            (string?)element.Attribute("data-cfx-role") is "terminal-title" or "terminal-tab-title");
        Assert.Equal("13", (string?)title.Attribute("font-size"));
        Assert.Equal("400", (string?)title.Attribute("font-weight"));
        Assert.Equal((style == TerminalWindowStyle.WindowsTerminal ? story.ResolvedStyle.Text : story.ResolvedStyle.Muted).ToCss(),
            (string?)title.Attribute("fill"));
    }

    [Theory]
    [InlineData(TerminalStoryPlaybackSpeed.Slow, 28, .6, .14, 2)]
    [InlineData(TerminalStoryPlaybackSpeed.Normal, 42, .35, .08, 1.2)]
    [InlineData(TerminalStoryPlaybackSpeed.Fast, 72, .15, .04, .55)]
    public void PlaybackCalculationUsesVisibleElementsAndPresetClock(TerminalStoryPlaybackSpeed speed,
        double charactersPerSecond, double initial, double lineDelay, double hold) {
        var story = TerminalStory.Create().WithPlaybackSpeed(speed);
        Assert.Equal(initial, story.InitialDelaySeconds);
        Assert.Equal(lineDelay, story.LineDelaySeconds);
        Assert.Equal(hold, story.TabHoldSeconds);
        Assert.Equal(42 / charactersPerSecond, story.GetCommandDurationSeconds(new string('a', 42)));
        Assert.Equal(.35, story.GetCommandDurationSeconds("e\u0301"));
        Assert.Equal(4.5, story.GetCommandDurationSeconds(new string('a', 1000)));
        Assert.Throws<ArgumentNullException>(() => story.GetCommandDurationSeconds(null!));
    }

    [Theory]
    [InlineData(TerminalWindowStyle.MacOS, 28, 10)]
    [InlineData(TerminalWindowStyle.WindowsTerminal, 32, 8)]
    [InlineData(TerminalWindowStyle.Linux, 36, 12)]
    public void TerminalWindowUsesExportCellAndChromeGeometry(TerminalWindowStyle style, double height, double radius) {
        var story = TerminalStory.Create().WithWindowStyle(style).WithFinalPrompt(false).Output("ready");
        var layout = TerminalStoryLayout.Build(story);
        Assert.Equal(height, layout.HeaderHeightValue);
        Assert.Equal(radius, TerminalWindowChrome.FrameRadius(style));
        Assert.Equal(20, layout.ContentX); // 8px page inset + 12px window padding.
        Assert.Equal(height + 20, layout.ContentTop);
        Assert.Equal(16.8, story.LineHeight);
        Assert.Equal(ChartColor.FromHex("#0C0C0C"), story.ResolvedStyle.Background);
        Assert.Equal(ChartColor.FromHex("#1F2A3C"), story.ResolvedStyle.HeaderBackground);
        Assert.Equal(ChartColor.FromHex("#CCCCCC"), story.ResolvedStyle.Text);
        Assert.Equal(ChartColor.FromHex("#FFFFFF"), TerminalStory.Create().WithThemeMode(VisualThemeMode.Light).ResolvedStyle.Background);
    }
}
