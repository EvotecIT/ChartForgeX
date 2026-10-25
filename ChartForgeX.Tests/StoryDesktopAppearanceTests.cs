using ChartForgeX.Themes;
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
        var theme = Theme(TerminalWindowStyle.MacOS, false);
        var story = VisualStory.Create("Type with color").WithSize(600, 400).WithTheme(theme.Palette, theme.Mode).WithStyle(theme.Style);
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
        var story = VisualStory.Create("Desktop demo").WithFormat(VisualStoryFormat.Portrait, 480).WithTheme(theme.Palette, theme.Mode).WithStyle(theme.Style);
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
        var custom = VisualTheme.Graphite(); custom = custom.Configure(builder => builder.Dark.ElevatedSurface = ChartColor.FromHex("#192D19"));
        var theme = Theme(TerminalWindowStyle.Linux, false); theme.WindowStyle = TerminalWindowStyle.None;
        var story = ReplayStory(theme, custom);
        var document = XDocument.Parse(story.Prepare().ToSvg());
        Assert.Empty(Roles(document, "story-window-chrome"));
        Assert.Equal(custom.Resolve(VisualThemeMode.Dark).ElevatedSurface.ToCss(), (string?)Roles(document, "terminal-viewport").Single().Attribute("fill"));
        Assert.Single(Roles(document, "terminal-viewport-text"));
    }

    [Fact]
    public void NarrowDesktopPanelsHaveActionableChromeGuidance() {
        var story = VisualStory.Create("Narrow windows").WithSize(540, 400).WithTheme(VisualStoryThemes.Windows(), VisualThemeMode.Dark).WithStyle(VisualStoryStyleOptions.Windows());
        story.Scene("code", "Code", 1, VisualStorySceneLayout.Split)
            .Panel("code", new VisualStorySourceSurface(StorySourceText.Create("42")))
            .Panel("other", new VisualStoryTextSurface("Ready"));
        story.Outcome("ready", "Ready", "code");
        var error = Assert.Throws<InvalidOperationException>(() => story.Prepare().ToPng());
        Assert.Contains("title bar cannot fit its controls", error.Message);
    }

    [Fact]
    public void WindowStyleCanBeChosenWithoutChangingALightPalette() {
        var theme = new StoryAppearance(VisualTheme.Graphite(), VisualThemeMode.Light, new VisualStoryStyleOptions());
        theme.WindowStyle = TerminalWindowStyle.MacOS;
        Assert.Equal(theme.Panel, theme.WindowHeader);
        Assert.True(ChartColorMath.ContrastRatio(theme.Text, theme.WindowHeader) >= 4.5);
        var document = XDocument.Parse(ReplayStory(theme).Prepare().ToSvg());
        Assert.Equal("MacOS", (string?)Roles(document, "story-window-chrome").Single().Attribute("data-cfx-window-style"));
    }

    [Fact]
    public void ReplayRetainsItsExplicitImmutablePalette() {
        var palette = VisualTheme.Graphite();
        var background = ChartColor.FromHex("#193A27");
        palette = palette.Configure(builder => builder.Dark.ElevatedSurface = background);
        var surface = new VisualStoryReplaySurface(StoryReplay.Create(TimeSpan.FromSeconds(1)).Output(TimeSpan.Zero, "Ready"), theme: palette);
        var story = VisualStory.Create("Replay").WithSize(600, 400);
        story.Scene("replay", "Output", 1).Panel("terminal", surface);
        story.Outcome("ready", "Ready", "terminal");
        var prepared = story.Prepare();
        palette = palette.Configure(builder => builder.Dark.ElevatedSurface = ChartColor.Black);
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
        var theme = new StoryAppearance(VisualTheme.Graphite(), VisualThemeMode.Light, new VisualStoryStyleOptions()); theme.WindowStyle = style;
        var prepared = ReplayStory(theme).Prepare();
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(theme.Panel.ToCss(), (string?)Roles(document, "terminal-viewport").Single().Attribute("fill"));
        Assert.True(CountColor(PngReader.Decode(prepared.ToPng()), theme.Panel) > 1000);
        var captured = prepared.ToSvg();
        theme.Palette = theme.Palette.Configure(builder => builder.Light.ElevatedSurface = ChartColor.Black);
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
        var image = PngReader.Decode(terminal.ToPng());
        Assert.Equal(480, image.Width);
        // Use the SVG control bounds to locate independent PNG title-bar ink.
        var closeRing = Roles(document, "terminal-window-close-ring").Single();
        var left = (int)Math.Floor((double)closeRing.Attribute("cx")! - (double)closeRing.Attribute("r")!) - 1;
        var top = (int)Math.Floor((double)closeRing.Attribute("cy")! - (double)closeRing.Attribute("r")!) - 1;
        var right = left + (int)Math.Ceiling((double)closeRing.Attribute("r")! * 2) + 2;
        var bottom = top + (int)Math.Ceiling((double)closeRing.Attribute("r")! * 2) + 2;
        var ink = terminal.Theme.Resolve(terminal.ThemeMode).MutedForeground;
        var controlPixels = 0;
        for (var y = top; y <= bottom; y++)
            for (var x = left; x <= right; x++) {
                var offset = (y * image.Width + x) * 4;
                // Thin strokes share output pixels with the dark title-bar background.
                if (Math.Abs(image.Pixels[offset] - ink.R) <= 90 &&
                    Math.Abs(image.Pixels[offset + 1] - ink.G) <= 90 &&
                    Math.Abs(image.Pixels[offset + 2] - ink.B) <= 90) controlPixels++;
            }
        Assert.True(controlPixels >= 12, "Linux PNG chrome must include visible title-bar control strokes.");
    }

    [Theory]
    [InlineData(TerminalWindowStyle.MacOS)]
    [InlineData(TerminalWindowStyle.WindowsTerminal)]
    [InlineData(TerminalWindowStyle.Linux)]
    public void DesktopControlsStaySolidWhileAuthoredTabTitlesCrossFade(TerminalWindowStyle style) {
        var palette = VisualTheme.Graphite();
        var terminal = TerminalStory.Create().WithInitialTab("first", "First", TerminalDialect.PowerShell, "~", palette)
            .WithTiming(0, 200, 0).WithTabHold(0).WithFinalPrompt(false).Output("First output")
            .OpenTab("second", "Second", TerminalDialect.Bash, "~", palette, transitionSeconds: 1)
            .Output("Second output").Pause(1);
        var layout = TerminalStoryLayout.Build(terminal);
        var transition = layout.Transitions.Single();
        var theme = Theme(TerminalWindowStyle.MacOS, false); theme.WindowStyle = style;
        var story = VisualStory.Create("Switch tabs").WithSize(600, 400).WithTheme(theme.Palette, theme.Mode).WithStyle(theme.Style);
        story.Scene("tabs", "Authored tabs", layout.DurationSeconds)
            .Panel("terminal", new VisualStoryTerminalSurface(terminal, options: new VisualStoryTerminalOptions(18)));
        story.Outcome("ready", "Ready", "terminal");
        var prepared = story.Prepare();
        var beforeTime = TimeSpan.FromSeconds(transition.StartSeconds - 0.01);
        var midpointTime = TimeSpan.FromSeconds(transition.StartSeconds + transition.DurationSeconds / 2);
        var before = XDocument.Parse(prepared.ToSvg(beforeTime));
        var midpoint = XDocument.Parse(prepared.ToSvg(midpointTime));
        var role = style == TerminalWindowStyle.WindowsTerminal ? "story-window-maximize" : "story-window-control";
        var expectedCount = style == TerminalWindowStyle.MacOS ? 3 : 1;
        Assert.Equal(expectedCount, Roles(midpoint, role).Length);
        var control = Roles(before, role).First();
        var x = style == TerminalWindowStyle.WindowsTerminal ? (double)control.Attribute("x")! + 5 : (double)control.Attribute("cx")!;
        var y = style == TerminalWindowStyle.WindowsTerminal ? (double)control.Attribute("y")! : (double)control.Attribute("cy")!;
        var beforeImage = PngReader.Decode(prepared.ToPng(beforeTime));
        var midpointImage = PngReader.Decode(prepared.ToPng(midpointTime));
        for (var row = (int)y - 2; row <= (int)y + 2; row++)
            for (var column = (int)x - 2; column <= (int)x + 2; column++) {
                var pixel = (row * beforeImage.Width + column) * 4;
                Assert.Equal(beforeImage.Pixels.AsSpan(pixel, 4).ToArray(), midpointImage.Pixels.AsSpan(pixel, 4).ToArray());
            }
    }

    private static VisualStory ReplayStory(StoryAppearance theme, VisualTheme? palette = null) {
        var replay = StoryReplay.Create(TimeSpan.FromSeconds(1)).Output(TimeSpan.Zero, "Ready");
        var story = VisualStory.Create("Replay").WithSize(600, 400).WithTheme(theme.Palette, theme.Mode).WithStyle(theme.Style);
        story.Scene("replay", "Output", 1).Panel("terminal", new VisualStoryReplaySurface(replay, theme: palette));
        story.Outcome("ready", "Ready", "terminal"); return story;
    }
    private static StoryAppearance Theme(TerminalWindowStyle style, bool light) => new(style switch {
        TerminalWindowStyle.MacOS => VisualStoryThemes.MacOS(),
        TerminalWindowStyle.WindowsTerminal => VisualStoryThemes.Windows(),
        _ => VisualStoryThemes.Linux()
    }, light ? VisualThemeMode.Light : VisualThemeMode.Dark, style switch {
        TerminalWindowStyle.MacOS => VisualStoryStyleOptions.MacOS(light),
        TerminalWindowStyle.WindowsTerminal => VisualStoryStyleOptions.Windows(light),
        _ => VisualStoryStyleOptions.Linux(light)
    });
    private sealed class StoryAppearance {
        internal StoryAppearance(VisualTheme palette, VisualThemeMode mode, VisualStoryStyleOptions style) { Palette = palette; Mode = mode; Style = style; }
        internal VisualTheme Palette { get; set; }
        internal VisualThemeMode Mode { get; }
        internal VisualStoryStyleOptions Style { get; }
        internal ChartColor Background => Palette.Resolve(Mode).Background;
        internal ChartColor Panel => Palette.Resolve(Mode).ElevatedSurface;
        internal ChartColor Text => Palette.Resolve(Mode).Foreground;
        internal ChartColor Muted => Palette.Resolve(Mode).MutedForeground;
        internal ChartColor Success => Palette.Resolve(Mode).Status.Pass.Ink;
        internal StorySyntaxPalette Syntax => Style.Syntax!;
        internal TerminalWindowStyle WindowStyle { get => Style.WindowStyle; set => Style.WindowStyle = value; }
        internal ChartColor WindowHeader => Style.WindowHeader ?? Panel;
        internal ChartColor? BackgroundEnd { get => Style.BackgroundEnd; set => Style.BackgroundEnd = value; }
    }
    private static XElement[] Roles(XDocument document, string role) => document.Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    private static int CountColor(RgbaImage image, ChartColor color) {
        var count = 0;
        for (var i = 0; i < image.Pixels.Length; i += 4)
            if (image.Pixels[i] == color.R && image.Pixels[i + 1] == color.G && image.Pixels[i + 2] == color.B) count++;
        return count;
    }
}
