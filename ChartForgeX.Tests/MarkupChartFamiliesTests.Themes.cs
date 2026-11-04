using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MarkupChartFamiliesTests {
    private static readonly IReadOnlyDictionary<string, Func<VisualTheme>> ThemePresets = new Dictionary<string, Func<VisualTheme>> {
        ["graphite"] = VisualTheme.Graphite, ["accessible"] = VisualTheme.Accessible,
        ["transparent-overlay"] = VisualTheme.TransparentOverlay
    };

    public static IEnumerable<object[]> ThemePresetCases => ThemePresets.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(ThemePresetCases))]
    public void EveryNativeThemePresetResolvesThroughCommandsOptionsTablesAndFenceAttributes(string preset) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            const string body = "type gauge\nvalues 42";
            var name = preset.ToUpperInvariant();
            foreach (var source in new[] {
                body + "\ntheme " + name + "\nthemeMode " + mode,
                body + "\noption theme " + name + "\noption themeMode " + mode,
                body + "\noptions:\n| key | value |\n| theme | " + name + " |\n| themeMode | " + mode + " |\nend",
                Fence(body, " theme=" + name + " themeMode=" + mode)
            }) {
                var chart = ParseChart(source);
                var expected = ThemePresets[preset]();
                Assert.Equal(expected.ToJson(), chart.Options.Theme.ToJson());
                Assert.Equal(mode, chart.Options.ThemeMode);
                Assert.Equal(expected.Resolve(mode).Background, chart.Options.Theme.Resolve(chart.Options.ThemeMode).Background);
                Assert.Equal(expected.Resolve(mode).Palette, chart.Options.Theme.Resolve(chart.Options.ThemeMode).Palette);
            }
        }
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void ThemeModeShorthandPreservesTheChosenPresetAcrossAllInputForms(VisualThemeMode mode) {
        const string body = "type gauge\nvalues 42\ntheme accessible";
        foreach (var source in new[] {
            body + "\ntheme " + mode,
            body + "\noption theme " + mode,
            Fence("type gauge\nvalues 42\ntheme accessible", " theme=" + mode)
        }) {
            var chart = ParseChart(source);
            Assert.Equal(VisualTheme.Accessible().ToJson(), chart.Options.Theme.ToJson());
            Assert.Equal(mode, chart.Options.ThemeMode);
        }
    }

    [Fact]
    public void RemovedThemePresetNamesAreRejected() {
        var parsed = new MarkupChartParser().Parse("type gauge\nvalues 42\ntheme report");
        var diagnostic = Assert.Single(parsed.Diagnostics);
        Assert.Equal(MarkupDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(3, diagnostic.Line);
        Assert.Equal("Unknown chart theme 'report'.", diagnostic.Message);
    }

    [Theory]
    [InlineData("graphite", 390, VisualThemeMode.Light)]
    [InlineData("graphite", 1000, VisualThemeMode.Dark)]
    [InlineData("accessible", 390, VisualThemeMode.Dark)]
    [InlineData("transparent-overlay", 1000, VisualThemeMode.Light)]
    public void AuthoredThemeAndModeReachConvenienceSvgAndDecodedPng(string preset, int width, VisualThemeMode mode) {
        var actual = ParseChart(Fence("type lineargauge\nseries \"Reading\"\nvalues 42\nsize " + width + "x420",
            " theme=" + preset + " themeMode=" + mode));
        actual.WithFontFamily(BrowserProofFonts.Family);
        var expected = Chart.Create().WithSize(width, 420).WithTheme(ThemePresets[preset](), mode)
            .WithFontFamily(BrowserProofFonts.Family).AddLinearGauge("Reading", 42);
        var svg = actual.ToSvg("authored-theme");
        var png = actual.ToPng();
        Assert.Equal(expected.ToSvg("authored-theme"), svg);
        Assert.Equal(expected.ToPng(), png);
        var image = RasterImageDecoder.Decode(png);
        Assert.Equal(width, image.Width);
        Assert.Equal(420, image.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_MARKUP_PROOF_ROOT");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "theme-" + preset + "-" + width + "-" + mode);
        File.WriteAllText(path + ".svg", svg);
        File.WriteAllBytes(path + ".png", png);
    }
}
