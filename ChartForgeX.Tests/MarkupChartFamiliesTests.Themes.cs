using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MarkupChartFamiliesTests {
    private static readonly IReadOnlyDictionary<string, Func<VisualTheme>> ThemePresets = new Dictionary<string, Func<VisualTheme>> {
        ["graphite"] = VisualTheme.Graphite, ["report"] = VisualTheme.Report,
        ["executive"] = VisualTheme.Executive, ["product"] = VisualTheme.Product,
        ["accessible"] = VisualTheme.Accessible, ["colorblind"] = VisualTheme.Colorblind,
        ["aurora"] = VisualTheme.Aurora, ["editorial"] = VisualTheme.Editorial,
        ["candy"] = VisualTheme.Candy, ["people-infographic"] = VisualTheme.PeopleInfographic,
        ["terminal"] = VisualTheme.Terminal, ["transparent-overlay"] = VisualTheme.TransparentOverlay,
        ["minimal"] = VisualTheme.Minimal, ["dashboard"] = VisualTheme.Dashboard,
        ["saas-dashboard"] = VisualTheme.SaasDashboard, ["restaurant-dashboard"] = VisualTheme.RestaurantDashboard
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
        const string body = "type gauge\nvalues 42\ntheme report";
        foreach (var source in new[] {
            body + "\ntheme " + mode,
            body + "\noption theme " + mode,
            Fence("type gauge\nvalues 42\ntheme report", " theme=" + mode)
        }) {
            var chart = ParseChart(source);
            Assert.Equal(VisualTheme.Report().ToJson(), chart.Options.Theme.ToJson());
            Assert.Equal(mode, chart.Options.ThemeMode);
        }
    }

    [Theory]
    [InlineData("report", 390, VisualThemeMode.Light)]
    [InlineData("report", 1000, VisualThemeMode.Dark)]
    [InlineData("aurora", 390, VisualThemeMode.Dark)]
    [InlineData("dashboard", 1000, VisualThemeMode.Light)]
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
