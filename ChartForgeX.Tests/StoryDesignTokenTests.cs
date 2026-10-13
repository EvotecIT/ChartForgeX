using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;
using ChartForgeX.Terminal;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StoryDesignTokenTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void TranslucentSyntaxColorsUseReadableForegroundInsteadOfOpaqueOnlyContrast(VisualThemeMode mode) {
        var invisible = mode == VisualThemeMode.Dark ? ChartColor.FromHex("#FFFFFF20") : ChartColor.FromHex("#00000020");
        var theme = VisualTheme.Graphite().Configure(builder => {
            builder.Colors(mode).Palette = new[] { invisible };
            builder.Colors(mode).Accent = invisible.WithAlpha(0);
        });
        var story = VisualStory.Create("Readable syntax").WithTheme(theme, mode);
        foreach (var kind in new[] { StorySyntaxKind.Keyword, StorySyntaxKind.Type, StorySyntaxKind.Command,
            StorySyntaxKind.Parameter, StorySyntaxKind.Variable, StorySyntaxKind.Property, StorySyntaxKind.String, StorySyntaxKind.Number })
            Assert.Equal(theme.Resolve(mode).Foreground, story.ResolvedStyle.Syntax.Resolve(kind));
        Assert.Equal(invisible, theme.Resolve(mode).Palette[0]);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void SharedThemeSelectsStoryAndTerminalSurfacesFontsAndSemanticInk(VisualThemeMode mode) {
        var theme = VisualTheme.Graphite();
        var colors = theme.Resolve(mode);
        var story = VisualStory.Create("Shared appearance").WithTheme(theme, mode);
        var terminal = TerminalStory.Create().WithTheme(theme, mode);
        Assert.Same(theme, story.Theme);
        Assert.Same(theme, terminal.Theme);
        Assert.Equal(colors.Background, story.ResolvedStyle.Background);
        Assert.Equal(colors.ElevatedSurface, story.ResolvedStyle.Panel);
        Assert.Equal(colors.ElevatedSurface, terminal.ResolvedStyle.Background);
        Assert.Equal(colors.Status.Pass.Ink, terminal.ResolvedStyle.Success);
        Assert.Equal(colors.Status.Medium.Ink, terminal.ResolvedStyle.Warning);
        Assert.Equal(colors.Status.Critical.Ink, terminal.ResolvedStyle.Error);
        Assert.Equal(theme.Typography.Family, story.ResolvedStyle.FontFamily);
        Assert.Equal(theme.Typography.MonospaceFamily, terminal.ResolvedStyle.FontFamily);
        foreach (var kind in Enum.GetValues<StorySyntaxKind>())
            Assert.True(ChartColorMath.ContrastRatio(story.ResolvedStyle.Syntax.Resolve(kind), colors.ElevatedSurface) >= 4.5);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void TerminalSvgUsesSharedSemanticTonesAndRetainsAuthoredOverrides(VisualThemeMode mode) {
        var error = ChartColor.FromHex("#A52A6D");
        var theme = VisualTheme.Graphite().Configure(builder =>
            builder.Colors(mode).Status.Critical = new VisualTokenColor(ChartColors.Red, error));
        var story = TerminalStory.Create().WithTheme(theme, mode)
            .Output("normal").Output("success", TerminalTextTone.Success)
            .Output("warning", TerminalTextTone.Warning).Output("error", TerminalTextTone.Error);
        var output = XDocument.Parse(story.ToSvg()).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-output")
            .ToDictionary(element => element.Value, element => (string?)element.Attribute("fill"));
        var colors = theme.Resolve(mode);
        Assert.Equal(colors.Foreground.ToCss(), output["normal"]);
        Assert.Equal(colors.Status.Pass.Ink.ToCss(), output["success"]);
        Assert.Equal(colors.Status.Medium.Ink.ToCss(), output["warning"]);
        Assert.Equal(error.ToCss(), output["error"]);
        Assert.NotEmpty(story.ToPng());
    }

    [Fact]
    public void PreparedStoriesCaptureLocalSyntaxAndWindowOptionsWithoutMutatingSharedTheme() {
        var shared = VisualTheme.Graphite();
        var style = new VisualStoryStyleOptions { Syntax = new StorySyntaxPalette { Plain = ChartColors.Red } };
        var story = VisualStory.Create("Detached style").WithTheme(shared, VisualThemeMode.Dark).WithStyle(style).WithSize(480, 320);
        story.Scene("source", "Source", 1).Panel("source", new VisualStorySourceSurface(StorySourceText.Create("Ready")));
        story.Outcome("ready", "Ready", "source");
        var prepared = story.Prepare();
        var before = prepared.ToSvg();
        style.Syntax.Plain = ChartColors.Blue;
        style.WindowStyle = TerminalWindowStyle.MacOS;
        Assert.Equal(before, prepared.ToSvg());
        Assert.Same(shared, story.Theme);
        Assert.Contains(ChartColors.Red.ToCss(), before);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void StoryHtmlKeepsExplicitModeWhenItsAuthoredBackgroundHasDifferentLuminance(VisualThemeMode mode) {
        var theme = VisualTheme.Graphite().Configure(builder =>
            builder.Colors(mode).Background = mode == VisualThemeMode.Light ? ChartColor.Black : ChartColor.White);
        var story = VisualStory.Create("Explicit appearance").WithTheme(theme, mode).WithSize(480, 320);
        story.Scene("source", "Source", 1).Panel("source", new VisualStorySourceSurface(StorySourceText.Create("Ready")));
        story.Outcome("ready", "Ready", "source");
        Assert.Contains("color-scheme:" + (mode == VisualThemeMode.Light ? "light" : "dark"), story.ToHtmlPage());
    }
}
