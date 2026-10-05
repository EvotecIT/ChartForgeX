using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteThemeTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GraphiteTokensPreserveRolesThroughThemesAndSvgVariables(bool dark) {
        var tokens = dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        var theme = tokens.ApplyTo(new ChartTheme()).Clone();
        Assert.True(theme.UseGraphiteLayout && theme.FlatMarks);
        Assert.Equal(tokens.ElevatedSurface, theme.CardBackground);
        Assert.Equal(tokens.MutedForeground, theme.Text2);
        Assert.Equal(tokens.Muted, theme.MutedText);
        Assert.Equal(tokens.Grid, theme.Grid);
        Assert.Equal(tokens.Axis, theme.Axis);
        Assert.Equal(tokens.QuietLine, theme.QuietLine);
        Assert.Equal(8, theme.Palette.Length);
        Assert.Equal(6, theme.SequentialRamp!.Length);
        Assert.Equal(17, theme.TitleFontSize);
        Assert.Equal(13.5, theme.SubtitleFontSize);
        var copy = tokens.Clone();
        Assert.Equal(tokens.ToSvgColorVariables().Variables.Select(v => (v.Name, v.Role, v.Color)),
            copy.ToSvgColorVariables().Variables.Select(v => (v.Name, v.Role, v.Color)));
        Assert.Contains(copy.ToSvgColorVariables().Variables, v => v.Name == "--cfx-guide-grid" && v.Role == SvgColorRole.Grid);
        Assert.Contains(copy.ToSvgColorVariables().Variables, v => v.Name == "--cfx-status-quiet-line");
        var state = new[] { theme.Positive, theme.Warning, theme.Negative, theme.Info, theme.Quiet, theme.QuietLine, theme.Neutral };
        Assert.All(theme.Palette, colour => Assert.DoesNotContain(colour, state));
        Assert.All(theme.Palette.Concat(theme.SequentialRamp).Concat(state), colour =>
            Assert.True(ChartColorMath.ContrastRatio(ChartColorMath.AccessibleTextOnBackground(colour), colour) >= 4.5));
        var lightness = theme.SequentialRamp.Select(ChartColorMath.RelativeLuminance).ToArray();
        for (var i = 1; i < lightness.Length; i++) Assert.True(dark ? lightness[i] > lightness[i - 1] : lightness[i] < lightness[i - 1]);
    }
}
