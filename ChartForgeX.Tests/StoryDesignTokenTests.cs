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
    [InlineData(false)]
    [InlineData(true)]
    public void GraphiteFactoriesShareSurfaceTypographyAndSemanticInkRoles(bool dark) {
        var tokens = dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        var story = dark ? VisualStoryTheme.GraphiteDark() : VisualStoryTheme.GraphiteLight();
        var terminal = dark ? TerminalTheme.GraphiteDark() : TerminalTheme.GraphiteLight();

        Assert.Equal(tokens.Background, story.Background);
        Assert.Equal(tokens.ElevatedSurface, story.Panel);
        Assert.Equal(tokens.Border, story.Border);
        Assert.Equal(tokens.Foreground, story.Text);
        Assert.Equal(tokens.MutedForeground, story.Muted);
        Assert.Equal(tokens.Accent, story.Accent);
        Assert.Equal(tokens.Status.Pass.Ink, story.Success);
        Assert.Equal(tokens.FontFamily, story.FontFamily);
        Assert.Equal(tokens.MonospaceFontFamily, story.MonospaceFontFamily);
        Assert.Equal(tokens.Background, terminal.PageBackground);
        Assert.Equal(tokens.ElevatedSurface, terminal.Background);
        Assert.Equal(tokens.Surface, terminal.HeaderBackground);
        Assert.Equal(tokens.Border, terminal.Border);
        Assert.Equal(tokens.Foreground, terminal.Text);
        Assert.Equal(tokens.MutedForeground, terminal.Muted);
        Assert.Equal(tokens.Accent, terminal.Accent);
        Assert.Equal(tokens.Status.Pass.Ink, terminal.Success);
        Assert.Equal(tokens.Status.Medium.Ink, terminal.Warning);
        Assert.Equal(tokens.Status.Critical.Ink, terminal.Error);
        Assert.Equal(tokens.Foreground, terminal.Cursor);
        Assert.Equal(tokens.MonospaceFontFamily, terminal.FontFamily);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GraphiteSyntaxAndTerminalTonesAreReadableOnTheirContentSurfaces(bool dark) {
        var story = dark ? VisualStoryTheme.GraphiteDark() : VisualStoryTheme.GraphiteLight();
        var terminal = dark ? TerminalTheme.GraphiteDark() : TerminalTheme.GraphiteLight();
        foreach (var kind in Enum.GetValues<StorySyntaxKind>()) {
            Assert.True(ChartColorMath.ContrastRatio(story.Syntax.Resolve(kind), story.Panel) >= 4.5,
                $"{kind} must stay legible on the {(dark ? "dark" : "light")} source panel.");
        }
        var tones = new[] { terminal.Text, terminal.Muted, terminal.Accent, terminal.Success, terminal.Warning, terminal.Error, terminal.Cursor };
        Assert.All(tones, color => Assert.True(ChartColorMath.ContrastRatio(color, terminal.Background) >= 4.5));
        Assert.True(ChartColorMath.ContrastRatio(terminal.Text, terminal.HeaderBackground) >= 4.5);
        Assert.True(ChartColorMath.ContrastRatio(story.Text, story.Background) >= 4.5);
        Assert.True(ChartColorMath.ContrastRatio(story.Muted, story.Background) >= 4.5);
    }

    [Fact]
    public void CustomTokensMapInkRatherThanMarkFillAndDoNotAliasSyntaxState() {
        var tokens = VisualDesignTokens.GraphiteLight();
        tokens.FontFamily = "Aptos, sans-serif";
        tokens.MonospaceFontFamily = "Consolas, monospace";
        tokens.Accent = ChartColor.FromHex("#563D7C");
        tokens.Status.Pass = new VisualTokenColor(ChartColor.FromHex("#43D69A"), ChartColor.FromHex("#125F39"));
        tokens.Status.Medium = new VisualTokenColor(ChartColor.FromHex("#FFDD66"), ChartColor.FromHex("#765200"));
        tokens.Status.Critical = new VisualTokenColor(ChartColor.FromHex("#FF8888"), ChartColor.FromHex("#8D1616"));
        tokens.Palette = new[] { ChartColor.FromHex("#FEFEFE") };
        var story = new VisualStoryTheme();
        var terminal = new TerminalTheme();

        Assert.Same(story, tokens.ApplyTo(story));
        Assert.Same(terminal, tokens.ApplyTo(terminal));
        Assert.Equal(tokens.Status.Pass.Ink, story.Success);
        Assert.Equal(tokens.Status.Pass.Ink, terminal.Success);
        Assert.Equal(tokens.Status.Medium.Ink, terminal.Warning);
        Assert.Equal(tokens.Status.Critical.Ink, terminal.Error);
        Assert.Equal(tokens.Accent, story.Accent);
        Assert.Equal(tokens.Accent, terminal.Accent);
        Assert.Equal(tokens.FontFamily, story.FontFamily);
        Assert.Equal(tokens.MonospaceFontFamily, story.MonospaceFontFamily);
        Assert.Equal(tokens.MonospaceFontFamily, terminal.FontFamily);
        Assert.Equal(tokens.Foreground, story.Syntax.Keyword);
        Assert.Equal(tokens.Foreground, story.Syntax.Number);

        var independent = tokens.ApplyTo(new VisualStoryTheme());
        story.Syntax.Keyword = ChartColor.FromHex("#1B5A93");
        Assert.Equal(tokens.Foreground, independent.Syntax.Keyword);
        Assert.Equal("#FEFEFE", tokens.Palette[0].ToHex());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TerminalSvgUsesMappedSemanticTonesAndRespectsLaterCallerOverrides(bool dark) {
        var theme = dark ? TerminalTheme.GraphiteDark() : TerminalTheme.GraphiteLight();
        var errorOverride = ChartColor.FromHex("#A52A6D");
        theme.Error = errorOverride;
        var story = TerminalStory.Create().WithTheme(theme)
            .Output("normal", TerminalTextTone.Default)
            .Output("success", TerminalTextTone.Success)
            .Output("warning", TerminalTextTone.Warning)
            .Output("error", TerminalTextTone.Error);
        var output = XDocument.Parse(story.ToSvg()).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "terminal-output")
            .ToDictionary(element => element.Value, element => (string?)element.Attribute("fill"));

        Assert.Equal(theme.Text.ToCss(), output["normal"]);
        Assert.Equal(theme.Success.ToCss(), output["success"]);
        Assert.Equal(theme.Warning.ToCss(), output["warning"]);
        Assert.Equal(errorOverride.ToCss(), output["error"]);
        Assert.Equal(errorOverride, story.Theme.Error);
    }
}
