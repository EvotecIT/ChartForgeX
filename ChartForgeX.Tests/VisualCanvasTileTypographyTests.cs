using System.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualCanvasTileTypographyTests {
    [Fact]
    public void DefaultTileOutputMatchesExplicitCurrentTypography() {
        var baseline = VisualCanvas.Create(420, 150)
            .AddInfoTile(12, 12, 396, 126, "PC", "HOSTNAME", "DEV-WKS-01", "Windows workstation");
        var explicitDefaults = VisualCanvas.Create(420, 150)
            .AddInfoTile(12, 12, 396, 126, "PC", "HOSTNAME", "DEV-WKS-01", "Windows workstation",
                textScale: 1, labelFontSize: 14, valueFontSize: 22, detailFontSize: 13);

        Assert.Equal(baseline.ToSvg(), explicitDefaults.ToSvg());
        Assert.Equal(baseline.ToPng(), explicitDefaults.ToPng());
    }

    [Theory]
    [InlineData(0.8)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    public void TileScaleChangesAllTextAndSupportingGeometry(double scale) {
        var canvas = VisualCanvas.Create(760, 220)
            .ConfigureStyle(style => style.TileTextScale = scale)
            .AddInfoTile(12, 12, 736, 196, "CPU", "PROCESSOR", "Intel Xeon", "48 cores",
                miniChartKind: VisualCanvasInfoTileMiniChartKind.Sparkline,
                miniChartValues: new[] { 20d, 40d, 30d, 55d });
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var layout = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height,
            metrics.TextX, metrics.TextMax, style);

        Assert.Equal(14 * scale, layout.Lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Label).FontSize, 6);
        Assert.Equal(22 * scale, layout.Lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Value).FontSize, 6);
        Assert.Equal(13 * scale, layout.Lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Detail).FontSize, 6);
        Assert.Equal(54 * scale, metrics.IconBox, 6);
        Assert.True(metrics.ChartHeight >= 28 * scale);
    }

    [Fact]
    public void PerRoleSizeOverridesThemeAndTileScale() {
        var theme = VisualTheme.Graphite().WithTypography(new VisualTypography(
            tileLabelSize: 16, tileValueSize: 26, tileDetailSize: 15));
        var canvas = VisualCanvas.Create(440, 160).WithTheme(theme)
            .AddInfoTile(12, 12, 416, 136, "OS", "SYSTEM", "Windows 11", "Enterprise",
                textScale: 0.8, labelFontSize: 18);
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var style = canvas.ResolveStyle();
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile, style);
        var lines = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height,
            metrics.TextX, metrics.TextMax, style).Lines;

        Assert.Equal(14.4, lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Label).FontSize, 6);
        Assert.Equal(20.8, lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Value).FontSize, 6);
        Assert.Equal(12, lines.Single(line => line.Role == VisualCanvasInfoTileTextRole.Detail).FontSize, 6);
    }

    [Fact]
    public void AScaleThatDoesNotFitIsReducedAndReported() {
        var canvas = VisualCanvas.Create(300, 100).AddInfoTile(8, 8, 284, 58,
            "PC", "VERY LONG HOST LABEL", "A workstation value that needs room", "extra detail",
            textScale: 1.5);

        Assert.Contains(canvas.AnalyzeLayout().Diagnostics,
            diagnostic => diagnostic.Code == "InfoTileTextScaleReduced");
    }
}
