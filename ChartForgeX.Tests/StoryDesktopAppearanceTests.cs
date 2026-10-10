using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryDesktopAppearanceTests {
    [Fact]
    public void ResolvedTypingRevealsSyntaxBeforeCompletionAndCapturesItsInput() {
        var source = StorySourceText.Create("$name = 'ok'", "powershell")
            .AddSpan(0, 5, StorySyntaxKind.Variable).AddSpan(8, 4, StorySyntaxKind.String);
        var editor = StorySourceTimeline.Create(StorySourceText.Create("", "powershell")).Type(source, TimeSpan.FromSeconds(2));
        var theme = VisualStoryTheme.MacOS();
        var story = VisualStory.Create("Type with color").WithSize(600, 400).WithTheme(theme);
        story.Scene("write", "Write", 2).Panel("code", new VisualStorySourceSurface(editor));
        story.Outcome("ready", "Source ready", "code");
        var prepared = story.Prepare();
        var partial = XDocument.Parse(prepared.ToSvg(TimeSpan.FromSeconds(1)));
        var runs = Roles(partial, "source-text");
        Assert.Equal("$name ", string.Concat(runs.Select(item => item.Value)));
        Assert.Contains(runs.Single(item => item.Value == "$name").DescendantsAndSelf(), item => (string?)item.Attribute("fill") == theme.Syntax.Variable.ToCss());
        var snapshot = prepared.ToSvg();
        theme.Syntax.Variable = ChartColor.Black;
        editor.Type(" later", TimeSpan.Zero);
        Assert.Equal(snapshot, prepared.ToSvg());
        Assert.Equal("$name = 'ok'", string.Concat(Roles(XDocument.Parse(snapshot), "source-text").Select(item => item.Value)));
        Assert.Throws<ArgumentNullException>(() => editor.Type((StorySourceText)null!, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(TerminalWindowStyle.MacOS, false)]
    [InlineData(TerminalWindowStyle.MacOS, true)]
    [InlineData(TerminalWindowStyle.WindowsTerminal, false)]
    [InlineData(TerminalWindowStyle.WindowsTerminal, true)]
    [InlineData(TerminalWindowStyle.Linux, false)]
    [InlineData(TerminalWindowStyle.Linux, true)]
    public void DesktopPresetsRenderMatchingEditorAndReplayChromeWithReadableInk(TerminalWindowStyle style, bool light) {
        var theme = Theme(style, light);
        foreach (var kind in Enum.GetValues<StorySyntaxKind>())
            Assert.True(ChartColorMath.ContrastRatio(theme.Syntax.Resolve(kind), theme.Panel) >= 4.5, $"{style} {light} {kind}");
        Assert.True(ChartColorMath.ContrastRatio(theme.Text, theme.WindowHeader) >= 4.5);
        foreach (var background in new[] { theme.Background, theme.BackgroundEnd!.Value }) {
            Assert.True(ChartColorMath.ContrastRatio(theme.Text, background) >= 4.5);
            Assert.True(ChartColorMath.ContrastRatio(theme.Muted, background) >= 4.5);
        }
        var story = VisualStory.Create("Desktop demo").WithFormat(VisualStoryFormat.Portrait, 480).WithTheme(theme);
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(1)).Output(TimeSpan.Zero, "Ready", TerminalTextTone.Success);
        story.Scene("demo", "Source and output", 1, VisualStorySceneLayout.Stacked)
            .Panel("code", new VisualStorySourceSurface(StorySourceText.Create("$x = 42"), options: new VisualStorySourceOptions("demo.ps1", 16)))
            .Panel("output", new VisualStoryReplaySurface(replay, options: new VisualStoryTerminalOptions(16)));
        story.Outcome("ready", "Ready", "output");
        var prepared = story.Prepare();
        var svg = prepared.ToSvg();
        var document = XDocument.Parse(svg);
        var chrome = Roles(document, "story-window-chrome");
        Assert.Equal(2, chrome.Length);
        Assert.All(chrome, element => Assert.Equal(style.ToString(), (string?)element.Attribute("data-cfx-window-style")));
        Assert.Single(Roles(document, "story-desktop-background"));
        Assert.Equal(theme.Panel.ToCss(), (string?)Roles(document, "terminal-viewport").Single().Attribute("fill"));
        Assert.Contains(Roles(document, "terminal-viewport-text").Single().DescendantsAndSelf(), item => (string?)item.Attribute("fill") == theme.Success.ToCss());
        var image = PngReader.Decode(prepared.ToPng());
        Assert.Equal(480, image.Width);
        Assert.Equal(853, image.Height);
        Assert.True(CountColor(image, theme.WindowHeader) > 1000);
        Assert.True(CountColor(image, theme.Success) > 5);
        theme.WindowStyle = TerminalWindowStyle.None;
        theme.BackgroundEnd = ChartColor.Black;
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Fact]
    public void CallerReplayPaletteIsPreservedAndNoChromeCanBeSelected() {
        var custom = TerminalTheme.GraphiteDark(); custom.Background = ChartColor.FromHex("#192D19");
        var theme = VisualStoryTheme.Linux(); theme.WindowStyle = TerminalWindowStyle.None;
        var story = ReplayStory(theme, custom);
        var document = XDocument.Parse(story.Prepare().ToSvg());
        Assert.Empty(Roles(document, "story-window-chrome"));
        Assert.Equal(custom.Background.ToCss(), (string?)Roles(document, "terminal-viewport").Single().Attribute("fill"));
        Assert.Single(Roles(document, "terminal-viewport-text"));
    }

    [Fact]
    public void NarrowDesktopPanelsHaveActionableChromeGuidance() {
        var story = VisualStory.Create("Narrow windows").WithSize(540, 400).WithTheme(VisualStoryTheme.Windows());
        story.Scene("code", "Code", 1, VisualStorySceneLayout.Split)
            .Panel("code", new VisualStorySourceSurface(StorySourceText.Create("42")))
            .Panel("other", new VisualStoryTextSurface("Ready"));
        story.Outcome("ready", "Ready", "code");
        var error = Assert.Throws<InvalidOperationException>(() => story.Prepare().ToPng());
        Assert.Contains("title bar cannot fit its controls", error.Message);
    }

    [Fact]
    public void WindowStyleCanBeChosenWithoutChangingALightPalette() {
        var theme = VisualStoryTheme.GraphiteLight();
        theme.WindowStyle = TerminalWindowStyle.MacOS;
        Assert.Equal(theme.Panel, theme.WindowHeader);
        Assert.True(ChartColorMath.ContrastRatio(theme.Text, theme.WindowHeader) >= 4.5);
        var document = XDocument.Parse(ReplayStory(theme).Prepare().ToSvg());
        Assert.Equal("MacOS", (string?)Roles(document, "story-window-chrome").Single().Attribute("data-cfx-window-style"));
    }

    [Fact]
    public void ReplayCapturesChangesToAnExplicitPalette() {
        var palette = TerminalTheme.GraphiteDark();
        var surface = new VisualStoryReplaySurface(StoryReplay.Create(TimeSpan.FromSeconds(1)).Output(TimeSpan.Zero, "Ready"), theme: palette);
        var background = ChartColor.FromHex("#193A27");
        palette.Background = background;
        var story = VisualStory.Create("Replay").WithSize(600, 400);
        story.Scene("replay", "Output", 1).Panel("terminal", surface);
        story.Outcome("ready", "Ready", "terminal");
        var prepared = story.Prepare();
        palette.Background = ChartColor.Black;
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(background.ToCss(), (string?)Roles(document, "terminal-viewport").Single().Attribute("fill"));
    }

    [Theory]
    [InlineData(TerminalWindowStyle.Minimal)]
    [InlineData(TerminalWindowStyle.None)]
    [InlineData(TerminalWindowStyle.MacOS)]
    [InlineData(TerminalWindowStyle.WindowsTerminal)]
    [InlineData(TerminalWindowStyle.Linux)]
    public void OmittedReplayPaletteFollowsLightStoryRegardlessOfChrome(TerminalWindowStyle style) {
        var theme = VisualStoryTheme.GraphiteLight(); theme.WindowStyle = style;
        var prepared = ReplayStory(theme).Prepare();
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(theme.Panel.ToCss(), (string?)Roles(document, "terminal-viewport").Single().Attribute("fill"));
        Assert.True(CountColor(PngReader.Decode(prepared.ToPng()), theme.Panel) > 1000);
        var captured = prepared.ToSvg();
        theme.Panel = ChartColor.Black;
        Assert.Equal(captured, prepared.ToSvg());
    }

    [Fact]
    public void StandaloneLinuxTerminalExportsBothNativeBackends() {
        var terminal = TerminalStory.Create().WithWidth(480).WithWindowStyle(TerminalWindowStyle.Linux)
            .WithPngOutputScale(1).WithFinalPrompt(false).Output("Linux output");
        var document = XDocument.Parse(terminal.ToSvg());
        Assert.Equal("Linux", (string?)Roles(document, "terminal-window-chrome").Single().Attribute("data-cfx-window-style"));
        Assert.Single(Roles(document, "terminal-window-close"));
        Assert.Contains("Linux output", document.Root!.Value);
        Assert.Equal(480, PngReader.Decode(terminal.ToPng()).Width);
    }

    private static VisualStory ReplayStory(VisualStoryTheme theme, TerminalTheme? palette = null) {
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(1)).Output(TimeSpan.Zero, "Ready");
        var story = VisualStory.Create("Replay").WithSize(600, 400).WithTheme(theme);
        story.Scene("replay", "Output", 1).Panel("terminal", new VisualStoryReplaySurface(replay, theme: palette));
        story.Outcome("ready", "Ready", "terminal"); return story;
    }
    private static VisualStoryTheme Theme(TerminalWindowStyle style, bool light) => style switch {
        TerminalWindowStyle.MacOS => VisualStoryTheme.MacOS(light),
        TerminalWindowStyle.WindowsTerminal => VisualStoryTheme.Windows(light),
        _ => VisualStoryTheme.Linux(light)
    };
    private static XElement[] Roles(XDocument document, string role) => document.Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    private static int CountColor(RgbaImage image, ChartColor color) {
        var count = 0;
        for (var i = 0; i < image.Pixels.Length; i += 4)
            if (image.Pixels[i] == color.R && image.Pixels[i + 1] == color.G && image.Pixels[i + 2] == color.B) count++;
        return count;
    }
}
