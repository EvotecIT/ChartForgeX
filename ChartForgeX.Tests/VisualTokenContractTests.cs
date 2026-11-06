using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class VisualTokenContractTests {
    [Fact]
    public void BundledStackFallbackUsesArchivoForACharacterMissingFromJetBrainsMono() {
        var face = TypographyFontResolver.ResolveFace("\"JetBrains Mono\", Archivo", 650, false);
        var glyph = Assert.Single(TextShaper.Shape(face.Font!, "Ĳ"));
        Assert.Equal("Archivo", glyph.Face.SelectedFamily);
        Assert.NotEqual(0, glyph.Glyph);
        Assert.Equal(650, glyph.Face.Variations["wght"]);
    }

    [Fact]
    public void UnsupportedVariationDoesNotDiscardTheRequestedBundledWeight() {
        var request = new FontSpec { Family = ChartFontStacks.ProductSans, Weight = 650 };
        var expected = TextLayoutEngine.Measure("Hamburgefonts", new TextStyle { Font = request, FontSize = 32 });
        request.WithVariation("opsz", 16);
        var resolved = TypographyFontResolver.ResolveFace(request);
        Assert.Equal(650, resolved.Font!.Variations["wght"]);
        Assert.Equal(expected.Width, TextLayoutEngine.Measure("Hamburgefonts", new TextStyle { Font = request, FontSize = 32 }).Width, 6);
        request.WithVariation("wght", 400);
        Assert.Equal(400, TypographyFontResolver.ResolveFace(request).Font!.Variations["wght"]);
    }

    [Fact]
    public void RegisteredVariableFaceRetainsTheRequestedWeightAndSharedTabularNumericPolicy() {
        using var font = typeof(ChartFontAssets).Assembly.GetManifestResourceStream("ChartForgeX.Typography.Fonts.Archivo.ttf")!;
        try {
            FontRegistry.Register("CFX Token Override", font);
            var registered = TypographyFontResolver.ResolveFace(new FontSpec { Family = "CFX Token Override", Weight = 650 });
            Assert.Equal(registered.Font!.Measure("11111", 16), registered.Font.Measure("88888", 16), 6);
            Assert.True(registered.Font.TabularNumerals);
            Assert.Equal(650, registered.Font.Variations["wght"]);
            Assert.False(registered.SynthesizeBold);
        } finally { FontRegistry.Clear(); }
    }

    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    [InlineData(600)]
    [InlineData(650)]
    [InlineData(700)]
    public void ProductFaceUsesBundledWeightAndTabularDigitsWithoutInstalledFonts(int weight) {
        var request = new FontSpec { Family = ChartFontStacks.ProductSans, Weight = weight };
        var resolved = TypographyFontResolver.ResolveFace(request);
        Assert.NotNull(resolved.Font);
        Assert.Null(resolved.Path);
        Assert.False(resolved.SynthesizeBold);
        Assert.Equal("Archivo", resolved.Font!.SelectedFamily);
        Assert.True(resolved.Font.HasSelectedAxis("wght"));
        var widths = "0123456789".Select(digit => resolved.Font.Measure(digit.ToString(), 16)).ToArray();
        Assert.All(widths, width => Assert.Equal(widths[0], width, 6));
        Assert.True(widths[0] > 0);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void GraphiteChromeSurvivesIndependentThemeEditsAndJsonRoundTrip(VisualThemeMode mode) {
        var graphite = VisualTheme.Graphite();
        var changed = graphite.Configure(builder => builder.Spacing = 19).WithFontFamily("Example host font");
        var imported = VisualTheme.FromJson(changed.ToJson());
        var chrome = imported.Resolve(mode).Chrome;
        Assert.Equal(10, graphite.CardRadius);
        Assert.Equal(6, graphite.PlotRadius);
        Assert.Equal(10, chrome.TooltipRadius);
        Assert.Equal(6, chrome.ControlRadius);
        Assert.Equal(ChartColor.FromHex(mode == VisualThemeMode.Light ? "#cdd0d5" : "#363a42"), chrome.Line);
        Assert.Equal(ChartColor.FromHex(mode == VisualThemeMode.Light ? "#02726a" : "#37c9b8"), chrome.FocusRing);
        Assert.Equal(graphite.Resolve(mode).Chrome, chrome);
        Assert.Equal("Example host font", imported.Typography.Family);
        Assert.Equal(19, imported.Spacing);
        Assert.Equal(changed.ToJson(), imported.ToJson());
    }

    [Fact]
    public void InteractiveOutputRetainsExplicitChartOverridesAndUsesLiveHostRolesBeforeLiteralDefaults() {
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite()).WithSize(400, 260)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("requests"), "Requests", new[] { new ChartPoint(0, 2), new ChartPoint(1, 5), new ChartPoint(2, 3) }));
        var html = new HtmlInteractiveChartRenderer().RenderPage(chart);
        Assert.Contains("--cfx-tooltip-surface:var(--cfx-surface-card,var(--hfx-color-surface,", html);
        Assert.Contains("--cfx-tooltip-shadow:var(--cfx-shadow-tooltip,var(--hfx-elevation-2,", html);
        Assert.Contains("--cfx-tooltip-radius:var(--hfx-radius-overlay,10px)", html);
        Assert.Contains("--cfx-focus-ring:var(--cfx-host-accent,var(--hfx-color-focus-ring,", html);
    }
}
