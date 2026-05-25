using System;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualCanvasComposesWallpaperStyleArtboards() {
        var canvas = VisualCanvas.CreateSocialPreview()
            .WithTitle("PowerBGInfo social preview")
            .WithBackground(ChartColor.FromHex("#020713"), ChartColor.FromHex("#071A35"))
            .WithBackdrop(VisualCanvasBackdropStyle.TechHorizon)
            .AddInfoTile(48, 92, 300, 82, "PC", "HOSTNAME", "DEV-WKS-01", accent: ChartColor.FromHex("#2F80FF"))
            .AddInfoTile(48, 190, 300, 82, "IP", "IP ADDRESS", "10.0.0.42", "192.168.1.42", ChartColor.FromHex("#2F80FF"))
            .AddInfoTile(852, 70, 300, 96, "CPU", "CPU", "Intel Core i7", accent: ChartColor.FromHex("#60A5FA"), progress: 0.23, surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised, miniChartKind: VisualCanvasInfoTileMiniChartKind.Area, miniChartValues: new[] { 18d, 26d, 22d, 37d, 48d, 43d })
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

        var cpuChart = Chart.Create()
            .WithSize(220, 120)
            .WithTransparentBackground()
            .WithHeader(false)
            .WithCard(false)
            .AddLine("CPU", new[] {
                new ChartPoint(1, 18),
                new ChartPoint(2, 31),
                new ChartPoint(3, 24),
                new ChartPoint(4, 45),
                new ChartPoint(5, 39)
            }, ChartColor.FromHex("#60A5FA"));
        var memoryCard = MetricCard.Create()
            .WithSize(180, 104)
            .WithTransparentBackground()
            .WithCard(false)
            .WithMetric("RAM", "41%")
            .WithMiniSparkline(new[] { 32d, 36d, 41d, 38d, 43d }, color: ChartColor.FromHex("#22C55E"));
        var topology = TopologyChart.Create()
            .WithViewport(260, 142, 14)
            .WithLayout(TopologyLayoutMode.Manual)
            .AddNode("host", "Host", 20, 45, TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy, symbol: "PC")
            .AddNode("dc", "DC", 160, 45, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, symbol: "AD")
            .AddEdge("host-dc", "host", "dc", "LDAP", status: TopologyHealthStatus.Healthy);
        var nativeCanvas = VisualCanvas.Create(760, 260)
            .WithTitle("Native renderables")
            .WithBackground(ChartColor.FromHex("#09111F"), ChartColor.FromHex("#102A43"))
            .AddImage(24, 24, 120, 78, "data:image/svg+xml;charset=utf-8,%3Csvg%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%20viewBox%3D%220%200%2020%2010%22%3E%3Crect%20width%3D%2220%22%20height%3D%2210%22%20fill%3D%22%23ff0000%22%2F%3E%3C%2Fsvg%3E", new byte[] {
                255, 0, 0, 255, 0, 255, 0, 255,
                0, 0, 255, 255, 255, 255, 0, 255
            }, 2, 2, fit: VisualCanvasImageFit.Cover)
            .AddChart(160, 24, 220, 120, cpuChart, VisualCanvasImageFit.Contain)
            .AddVisualBlock(400, 24, 180, 104, memoryCard, VisualCanvasImageFit.Center)
            .AddTopology(160, 150, 260, 86, topology, fit: VisualCanvasImageFit.Cover)
            .AddText(446, 160, 260, "ChartForgeX renderables can be composed as canvas layers.", 20, ChartColor.FromHex("#DBEAFE"));
        var nativeSvg = nativeCanvas.ToSvg("visual-canvas-native-renderables");
        Assert(nativeSvg.Contains("data:image/svg+xml", StringComparison.Ordinal), "VisualCanvas should embed SVG renderables for SVG/HTML output.");
        Assert(nativeSvg.Contains("preserveAspectRatio=\"xMidYMid slice\"", StringComparison.Ordinal), "VisualCanvas Cover image fit should map to SVG slice behavior.");
        Assert(nativeSvg.Contains("preserveAspectRatio=\"xMidYMid meet\"", StringComparison.Ordinal), "VisualCanvas Contain image fit should map to SVG meet behavior.");
        Assert(nativeCanvas.ToPng().Length > 64, "VisualCanvas should render embedded ChartForgeX renderables to PNG output.");
        Assert(nativeCanvas.ToBmp().Length > 64, "VisualCanvas should render embedded ChartForgeX renderables to BMP output.");

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
        AssertThrows<ArgumentOutOfRangeException>(() => VisualCanvas.Create(24, 24).AddImage(0, 0, 12, 12, rgba: new byte[] { 0, 0, 0, 255 }, sourceWidth: 1, sourceHeight: 1, fit: (VisualCanvasImageFit)999), "VisualCanvas image fit should reject unknown values.");
        var htmlPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chartforgex-visual-canvas-" + Guid.NewGuid().ToString("N") + ".html");
        var ppmPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chartforgex-visual-canvas-" + Guid.NewGuid().ToString("N") + ".ppm");
        try {
            canvas.Save(htmlPath);
            var html = System.IO.File.ReadAllText(htmlPath, System.Text.Encoding.UTF8);
            Assert(html.Contains("<!DOCTYPE html>", StringComparison.OrdinalIgnoreCase) && html.Contains("data-cfx-role=\"visual-canvas-info-tile\"", StringComparison.Ordinal), "VisualCanvas extension-inferred save should support HTML output.");
            nativeCanvas.Save(ppmPath);
            var ppm = System.IO.File.ReadAllBytes(ppmPath);
            Assert(ppm.Length > 64 && ppm[0] == (byte)'P' && ppm[1] == (byte)'6', "VisualCanvas extension-inferred save should support opaque raster output.");
        } finally {
            if (System.IO.File.Exists(htmlPath)) System.IO.File.Delete(htmlPath);
            if (System.IO.File.Exists(ppmPath)) System.IO.File.Delete(ppmPath);
        }
    }
}
