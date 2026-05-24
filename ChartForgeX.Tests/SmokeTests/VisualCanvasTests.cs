using System;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualCanvasComposesWallpaperStyleArtboards() {
        var canvas = VisualCanvas.CreateSocialPreview()
            .WithTitle("PowerBGInfo social preview")
            .WithBackground(ChartColor.FromHex("#020713"), ChartColor.FromHex("#071A35"))
            .WithBackdrop(VisualCanvasBackdropStyle.TechHorizon)
            .AddInfoTile(48, 92, 300, 82, "PC", "HOSTNAME", "DEV-WKS-01", accent: ChartColor.FromHex("#2F80FF"))
            .AddInfoTile(48, 190, 300, 82, "IP", "IP ADDRESS", "10.0.0.42", "192.168.1.42", ChartColor.FromHex("#2F80FF"))
            .AddInfoTile(852, 70, 300, 96, "CPU", "CPU", "Intel Core i7", accent: ChartColor.FromHex("#60A5FA"), progress: 0.23, miniChartKind: VisualCanvasInfoTileMiniChartKind.Area, miniChartValues: new[] { 18d, 26d, 22d, 37d, 48d, 43d })
            .AddInfoTile(852, 184, 300, 96, "RAM", "RAM", "32.0 GB", accent: ChartColor.FromHex("#60A5FA"), progress: 0.41, miniChartKind: VisualCanvasInfoTileMiniChartKind.Bars, miniChartValues: new[] { 42d, 48d, 51d, 55d, 52d })
            .AddHeroBadge(538, 157, 124, 88, ">_", ChartColor.FromHex("#22A7FF"))
            .AddHeroTitle(312, 296, 576, 82, new[] {
                new VisualCanvasTextRun("Power", ChartColor.FromHex("#F8FAFC")),
                new VisualCanvasTextRun("BGInfo", ChartColor.FromHex("#2F80FF"))
            })
            .AddText(240, 402, 720, "Desktop background insights for Windows and PowerShell", 24, ChartColor.FromHex("#C6D3EA"), VisualCanvasTextAlignment.Center)
            .AddFeatureStrip(290, 522, 620, 62, new[] {
                new VisualCanvasFeatureItem("PS", "LIGHTWEIGHT"),
                new VisualCanvasFeatureItem("OK", "SECURE"),
                new VisualCanvasFeatureItem("UI", "CUSTOMIZABLE"),
                new VisualCanvasFeatureItem("OS", "OPEN SOURCE")
            });

        var svg = canvas.ToSvg("visual-canvas-smoke");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-background\"", StringComparison.Ordinal), "VisualCanvas should render a background role in SVG.");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-info-tile\"", StringComparison.Ordinal), "VisualCanvas should render info tile layers in SVG.");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-info-tile-mini-chart\"", StringComparison.Ordinal), "VisualCanvas should render info tile mini chart layers in SVG.");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-hero-title\"", StringComparison.Ordinal), "VisualCanvas should render multi-run hero titles in SVG.");
        Assert(svg.Contains("Power", StringComparison.Ordinal) && svg.Contains("BGInfo", StringComparison.Ordinal), "VisualCanvas hero title should preserve colored title runs.");
        Assert(canvas.ToPng().Length > 64, "VisualCanvas should render PNG output.");

        var overlay = VisualCanvas.CreateDesktopWallpaper()
            .WithTitle("Transparent overlay")
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddInfoTile(80, 120, 360, 92, "PC", "HOSTNAME", "DEV-WKS-01", surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Outline, iconKind: VisualCanvasInfoTileIconKind.Computer);
        var overlaySvg = overlay.ToSvg("visual-canvas-overlay");
        Assert(!overlaySvg.Contains("data-cfx-role=\"visual-canvas-background\"", StringComparison.Ordinal), "Transparent VisualCanvas overlays should not render a full background rect.");
        Assert(overlay.ToPng().Length > 64, "Transparent VisualCanvas overlays should render PNG output.");

        AssertThrows<ArgumentOutOfRangeException>(() => VisualCanvas.Create(0, 630), "VisualCanvas should reject invalid widths.");
        AssertThrows<ArgumentOutOfRangeException>(() => canvas.PngOutputScale = 5, "VisualCanvas should reject unsupported PNG output scales.");
        AssertThrows<ArgumentOutOfRangeException>(() => new VisualCanvasInfoTileLayer(0, 0, 100, 80, "I", "L", "V").Progress = 1.2, "VisualCanvas info tile progress should stay normalized.");
        AssertThrows<ArgumentOutOfRangeException>(() => new VisualCanvasInfoTileLayer(0, 0, 100, 80, "I", "L", "V").WithMiniChart(VisualCanvasInfoTileMiniChartKind.Sparkline, new[] { double.NaN }), "VisualCanvas info tile mini charts should reject invalid values.");
        AssertThrows<NotSupportedException>(() => canvas.Save("canvas.html"), "VisualCanvas extension-inferred save should fail clearly for unsupported formats.");
    }
}
