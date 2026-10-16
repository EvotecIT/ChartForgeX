using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Uses controlled faces to qualify configured scale fitting without relying on host fallback fonts.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class GaugeConfigurationFontTests {
    [Theory]
    [InlineData("vertical", false)]
    [InlineData("vertical", true)]
    [InlineData("needle", false)]
    [InlineData("needle", true)]
    public void WiderPreparedFacesKeepCompleteAnchoredTicksAndDetachedExports(string layout, bool dark) {
        const string family = "CFX Gauge Controlled Width";
        try {
            FontRegistry.Register(family, OpenTypeTestFonts.NameKeyed(extraGlyphs: Enumerable.Range(32, 95).ToDictionary(value => value, _ => OpenTypeTestFonts.Flex)));
            var chart = GaugeConfigurationRenderingTests.Create(layout, dark, compact: true)
                .WithTheme(VisualTheme.Graphite().WithFontFamily(family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
            var wide = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            var svg = wide.ToSvg(); var png = wide.ToPng();
            Verify(wide);
            FontRegistry.Register(family, Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
            var narrow = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            Verify(narrow);
            var wideCaption = Assert.Single(wide.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "gauge-title");
            var narrowCaption = Assert.Single(narrow.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "gauge-title");
            Assert.True(wideCaption.Text.Metrics.Width > narrowCaption.Text.Metrics.Width);
            Assert.Equal(svg, wide.ToSvg()); Assert.Equal(png, wide.ToPng());
            Capture(wide, "controlled-wide-" + layout + "-" + (dark ? "dark" : "light"));
            Capture(narrow, "controlled-carlito-" + layout + "-" + (dark ? "dark" : "light"));
        } finally { FontRegistry.Clear(); }
    }

    private static void Verify(PreparedVisual prepared) {
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "gauge.insufficient-space" or "radial.text-overflow");
        var texts = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role is "gauge-min-label" or "gauge-max-label"
            || node.Role?.StartsWith("gauge-tick-label-", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(prepared.Scene.Nodes.Count(node => node.Role == "gauge-tick"), texts.Length);
        Assert.All(texts, text => Assert.DoesNotContain("…", string.Join("", text.Text.Lines.Select(line => line.Text)), StringComparison.Ordinal));
        Assert.NotEmpty(prepared.ToPng());
    }

    private static void Capture(PreparedVisual prepared, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_GAUGE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".png"), prepared.ToPng());
    }
}
