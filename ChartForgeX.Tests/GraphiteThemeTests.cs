using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphiteThemeTests {
    [Fact]
    public void GeneratedPaletteDocumentLoadsIndependentAnnotationAndGuideRoles() {
        var directory = new DirectoryInfo(TestRepository.Root);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ChartForgeX.sln"))) directory = directory.Parent;
        var path = Path.Combine(directory!.FullName, "ChartForgeX.Tests", "Fixtures", "tokens", "palette-v1-graphite.json");
        var document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        var roles = new Dictionary<string, string> {
            ["muted"]="#626770", ["grid"]="#ECEEF0", ["axis"]="#C4C8CE", ["info"]="#0A728B",
            ["quiet"]="#9FCDB3", ["quietLine"]="#A3A9B1", ["neutral"]="#C9CDD3", ["neutral2"]="#E4E6E9", ["neutral3"]="#F1F2F4"
        };
        var graphite = new System.Text.Json.Nodes.JsonObject();
        foreach (var entry in roles) graphite[entry.Key] = entry.Value;
        document["light"]!["graphite"] = graphite;
        var theme = VisualTheme.FromPaletteJson(document.ToJsonString()).Resolve(VisualThemeMode.Light);
        Assert.Equal(ChartColor.FromHex(roles["quietLine"]), theme.QuietLine);
        Assert.Equal(ChartColor.FromHex(roles["neutral3"]), theme.Neutral3);
        Assert.Equal(ChartColor.FromHex(roles["axis"]), theme.Axis);
        Assert.Equal(ChartColor.FromHex(roles["muted"]), theme.Muted);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GraphiteTokensPreserveRolesThroughThemesAndSvgVariables(bool dark) {
        var tokens = dark ? VisualDesignTokens.GraphiteDark() : VisualDesignTokens.GraphiteLight();
        var canonical = VisualTheme.Graphite().Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var theme = new VisualTheme(VisualDesignTokens.GraphiteLight(), VisualDesignTokens.GraphiteDark())
            .Resolve(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        Assert.Equal(tokens.ElevatedSurface, theme.ElevatedSurface);
        Assert.Equal(tokens.MutedForeground, theme.MutedForeground);
        Assert.Equal(tokens.Muted ?? tokens.MutedForeground, theme.Muted);
        Assert.Equal(tokens.Grid ?? tokens.Border, theme.Grid);
        Assert.Equal(tokens.Axis ?? tokens.Border, theme.Axis);
        Assert.Equal(canonical.Palette, theme.Palette);
        Assert.Equal(canonical.SequentialRamp, theme.SequentialRamp!);
        Assert.Equal(17, VisualTheme.Graphite().Typography.TitleSize);
        Assert.Equal(13.5, VisualTheme.Graphite().Typography.SubtitleSize);
        var copy = tokens.Clone();
        Assert.Equal(tokens.ToSvgColorVariables().Variables.Select(v => (v.Name, v.Role, v.Color)),
            copy.ToSvgColorVariables().Variables.Select(v => (v.Name, v.Role, v.Color)));
        Assert.Contains(copy.ToSvgColorVariables().Variables, v => v.Name == "--cfx-surface-line" && v.Role == SvgColorRole.Surface && v.Color.Equals(canonical.Border));
        Assert.Contains(copy.ToSvgColorVariables().Variables, v => v.Name == "--cfx-outcome-pass-fill" && v.Color.Equals(canonical.Status.Pass.Fill));
        var state = new[] { theme.Positive, theme.Warning, theme.Negative };
        Assert.All(theme.Palette, colour => Assert.DoesNotContain(colour, state));
        var ramp = theme.SequentialRamp;
        Assert.NotEmpty(ramp);
        Assert.All(theme.Palette.Concat(ramp).Concat(state), colour =>
            Assert.True(ChartColorMath.ContrastRatio(ChartColorMath.AccessibleTextOnBackground(colour), colour) >= 4.5));
        // Contrast against black is monotonic in linear luminance, and therefore in CIELAB L*.
        var lightness = ramp.Select(colour => ChartColorMath.ContrastRatio(ChartColor.Black, colour)).ToArray();
        for (var i = 1; i < lightness.Length; i++) Assert.True(dark ? lightness[i] > lightness[i - 1] : lightness[i] < lightness[i - 1]);
    }
}
