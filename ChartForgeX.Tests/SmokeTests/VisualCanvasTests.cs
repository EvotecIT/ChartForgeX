using System;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ResponsiveVisualCanvasPreservesDesignLayoutAcrossOutputs() {
        var canvas = VisualCanvas.Create(960, 540)
            .WithResponsiveLayout(1920, 1080, VisualCanvasImageFit.Contain)
            .WithTitle("Responsive wallpaper")
            .AddText(120, 120, 900, "One design, multiple outputs", 48, ChartColor.White)
            .AddInfoTile(120, 240, 520, 160, "CPU", "Processor", "42%");

        var svg = canvas.ToSvg();
        var png = RasterImageDecoder.Decode(canvas.ToPng());
        Assert(svg.Contains("width=\"960\"", StringComparison.Ordinal) && svg.Contains("viewBox=\"0 0 1920 1080\"", StringComparison.Ordinal), "Responsive SVG should expose output dimensions while preserving design coordinates.");
        Assert(svg.Contains("preserveAspectRatio=\"xMidYMid meet\"", StringComparison.Ordinal), "Responsive contain mode should preserve the design aspect ratio.");
        Assert(png.Width == 960 && png.Height == 540, "Responsive PNG should render the requested output dimensions.");
        Assert(canvas.AnalyzeLayout().Diagnostics.Count == 0, "Responsive layout analysis should validate layers against design coordinates.");

        var letterboxed = VisualCanvas.Create(320, 320)
            .WithResponsiveLayout(320, 160, VisualCanvasImageFit.Contain)
            .WithBackground(ChartColor.FromHex("#123456"), ChartColor.FromHex("#214365"));
        var letterboxedSvg = letterboxed.ToSvg();
        var letterboxedPng = RasterImageDecoder.Decode(letterboxed.ToPng());
        Assert(letterboxedSvg.Contains("background-color:#214365", StringComparison.Ordinal), "Opaque responsive SVG contain output should fill its viewport letterbox with the PNG background color.");
        Assert(letterboxedPng.Pixels[0] == 0x21 && letterboxedPng.Pixels[1] == 0x43 && letterboxedPng.Pixels[2] == 0x65 && letterboxedPng.Pixels[3] == 255, "Opaque responsive PNG contain output should preserve the shared letterbox color.");

        var transparent = VisualCanvas.Create(320, 320)
            .WithResponsiveLayout(320, 160, VisualCanvasImageFit.Contain)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent);
        var transparentSvg = transparent.ToSvg();
        var transparentPng = RasterImageDecoder.Decode(transparent.ToPng());
        Assert(!transparentSvg.Contains("background-color:", StringComparison.Ordinal), "Transparent responsive SVG letterboxes should remain transparent.");
        Assert(transparentPng.Pixels[3] == 0, "Transparent responsive PNG letterboxes should remain transparent.");
    }

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
            .AddText(240, 402, 720, "Desktop background insights for Windows and PowerShell", 24, ChartColor.FromHex("#C6D3EA"), TextAlignment.Center)
            .AddFeatureStrip(290, 522, 620, 62, new[] {
                new VisualCanvasFeatureItem("PS", "LIGHTWEIGHT"),
                new VisualCanvasFeatureItem("OK", "SECURE"),
                new VisualCanvasFeatureItem("UI", "CUSTOMIZABLE"),
                new VisualCanvasFeatureItem("OS", "OPEN SOURCE")
            });

        var svg = canvas.ToSvg("visual-canvas-smoke");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-background\"", StringComparison.Ordinal), "VisualCanvas should render a background role in SVG.");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-tech-backdrop\"", StringComparison.Ordinal) && svg.Contains("data-cfx-role=\"visual-canvas-horizon\"", StringComparison.Ordinal), "TechHorizon should retain its quiet star, contour, and horizon backdrop.");
        Assert(!svg.Contains("visual-canvas-road-glow", StringComparison.Ordinal), "TechHorizon should keep its lower foreground clear instead of drawing a decorative road streak across content.");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-info-tile\"", StringComparison.Ordinal), "VisualCanvas should render info tile layers in SVG.");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-info-tile-mini-chart\"", StringComparison.Ordinal), "VisualCanvas should render info tile mini chart layers in SVG.");
        Assert(svg.Contains("data-cfx-role=\"visual-canvas-hero-title\"", StringComparison.Ordinal), "VisualCanvas should render multi-run hero titles in SVG.");
        Assert(svg.Contains("Power", StringComparison.Ordinal) && svg.Contains("BGInfo", StringComparison.Ordinal), "VisualCanvas hero title should preserve colored title runs.");
        Assert(canvas.ToPng().Length > 64, "VisualCanvas should render PNG output.");

        var badgeLogoPixels = new byte[] {
            255, 32, 32, 255, 255, 32, 32, 255,
            255, 32, 32, 255, 255, 32, 32, 255
        };
        var brandedBadgeCanvas = VisualCanvas.Create(180, 120)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddHeroBadge(40, 20, 100, 70, "LG", ChartColor.FromHex("#22A7FF"), "data:image/png;base64,AA==", badgeLogoPixels, 2, 2, VisualCanvasImageFit.Contain, imagePadding: 12);
        var brandedBadgeSvg = brandedBadgeCanvas.ToSvg("visual-canvas-branded-badge");
        Assert(brandedBadgeSvg.Contains("data:image/png;base64", StringComparison.Ordinal), "VisualCanvas hero badges should embed configured logo images in SVG output.");
        Assert(brandedBadgeSvg.Contains("preserveAspectRatio=\"xMidYMid meet\"", StringComparison.Ordinal), "VisualCanvas hero badge logos should default to contain fit for SVG output.");
        var brandedBadgePng = RasterImageDecoder.Decode(brandedBadgeCanvas.ToPng());
        Assert(CountPixels(brandedBadgePng, (r, g, b, a) => a > 240 && r > 220 && g < 80 && b < 80) > 500, "VisualCanvas hero badges should render configured logo pixels in PNG output.");
        var rgbaOnlyBadgeSvg = VisualCanvas.Create(180, 120)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddHeroBadge(20, 20, 60, 50, string.Empty, null, null, imageRgba: badgeLogoPixels, imageSourceWidth: 2, imageSourceHeight: 2, imageFit: VisualCanvasImageFit.Tile)
            .AddHeroBadge(100, 20, 60, 50, string.Empty, null, null, imageRgba: badgeLogoPixels, imageSourceWidth: 2, imageSourceHeight: 2, imageFit: VisualCanvasImageFit.Center)
            .ToSvg("visual-canvas-rgba-only-badges");
        Assert(rgbaOnlyBadgeSvg.Contains("data:image/png;base64", StringComparison.Ordinal), "VisualCanvas hero badge SVG output should synthesize image hrefs for RGBA-only logo pixels.");
        Assert(rgbaOnlyBadgeSvg.Contains("-hero-badge-image-0", StringComparison.Ordinal) && rgbaOnlyBadgeSvg.Contains("-hero-badge-image-1", StringComparison.Ordinal), "Multiple hero badge images should use unique SVG image IDs.");

        var resizedTileCanvas = VisualCanvas.Create(620, 220)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddInfoTile(24, 24, 520, 150, "OS", "OPERATING SYSTEM", "Windows 11 Enterprise Intune compliance", "cross-site policy drift", surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised, iconKind: VisualCanvasInfoTileIconKind.OperatingSystem);
        var resizedTileSvg = resizedTileCanvas.ToSvg("visual-canvas-resized-tile-text");
        Assert(resizedTileSvg.Contains("compliance", StringComparison.Ordinal), "Resized VisualCanvas info tiles should preserve long value text across wrapped lines.");
        Assert(resizedTileSvg.Contains("policy drift", StringComparison.Ordinal), "Resized VisualCanvas info tiles should preserve detail text when height is available.");
        Assert(resizedTileCanvas.AnalyzeLayout().IsClean, "Resized VisualCanvas info tiles should not report layout diagnostics when text fits.");
        Assert(resizedTileCanvas.ToPng().Length > 64, "Resized VisualCanvas info tiles should render wrapped text to PNG output.");

        var constrainedTileCanvas = VisualCanvas.Create(230, 120)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddInfoTile(12, 12, 160, 72, "OS", "OPERATING SYSTEM", "Windows 11 Enterprise Intune compliance", textFitPolicy: VisualCanvasTextFitPolicy.SingleLineEllipsis);
        var constrainedTileSvg = constrainedTileCanvas.ToSvg("visual-canvas-constrained-tile-text");
        Assert(constrainedTileSvg.Contains("...", StringComparison.Ordinal), "Single-line VisualCanvas info tile policy should trim overflowing values with an ellipsis.");
        Assert(constrainedTileCanvas.AnalyzeLayout().HasWarnings, "Constrained VisualCanvas info tiles should report layout diagnostics when text is trimmed.");

        var spacedTileSvg = VisualCanvas.Create(360, 112)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddInfoTile(12, 12, 300, 82, "PC", "HOST", "DEV  WKS  01")
            .ToSvg("visual-canvas-spaced-tile-text");
        Assert(spacedTileSvg.Contains("DEV  WKS  01", StringComparison.Ordinal), "VisualCanvas info tile fitting should preserve intentional spacing when text fits without wrapping.");

        var progressDetailCanvas = VisualCanvas.Create(360, 128)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddInfoTile(12, 12, 300, 96, "CPU", "CPU", "Intel Core i7", "Package temperature nominal", progress: 0.23);
        var progressDetailReport = progressDetailCanvas.AnalyzeLayout();
        var progressDetailTrimmed = false;
        var progressDetailTooTall = false;
        foreach (var diagnostic in progressDetailReport.Diagnostics) {
            progressDetailTrimmed |= diagnostic.Code == "InfoTileTextTrimmed";
            progressDetailTooTall |= diagnostic.Code == "InfoTileTextTooTall";
        }

        Assert(progressDetailTrimmed && !progressDetailTooTall, "Progress info tiles should report omitted detail as fitted text instead of letting text collide with the progress rail.");

        var outsideCanvas = VisualCanvas.Create(100, 80)
            .AddInfoTile(80, 20, 60, 44, "I", "LABEL", "VALUE", textFitPolicy: VisualCanvasTextFitPolicy.Wrap);
        Assert(outsideCanvas.AnalyzeLayout().HasWarnings, "VisualCanvas layout diagnostics should detect layers outside the canvas bounds.");

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

        var sourceImage = VisualCanvas.Create(18, 12)
            .WithBackground(ChartColor.FromHex("#123456"), ChartColor.FromHex("#ABCDEF"))
            .AddText(2, 2, 14, "PX", 8, ChartColor.White, emphasized: true);
        var sourcePng = sourceImage.ToPng();
        var decodedPng = RasterImageDecoder.Decode(sourcePng);
        Assert(decodedPng.Width == 18 && decodedPng.Height == 12 && decodedPng.Pixels.Length == 18 * 12 * 4, "RasterImageDecoder should decode dependency-free PNG images to RGBA pixels.");
        var decodedTransparentPng = RasterImageDecoder.Decode(PngWithTransparentColor());
        Assert(decodedTransparentPng.Pixels[3] == 0, "RasterImageDecoder should apply PNG truecolor transparency metadata.");
        var corruptPng = PngWithTransparentColor();
        corruptPng[corruptPng.Length - 5] ^= 0x7F;
        Assert(!RasterImageDecoder.TryDecode(corruptPng, out _), "RasterImageDecoder should reject PNG inputs with invalid chunk CRC data.");
        using (var sourceStream = new System.IO.MemoryStream(sourcePng)) {
            Assert(RasterImageDecoder.Read(sourceStream).Width == 18, "RasterImageDecoder should decode image streams.");
        }

        var sampleJpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAIAAgDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDlPgv+yH/qP9C9P4aKKKrCYmr7JanVwHxrnX9i0/3v9fef/9k=");
        Assert(RasterImageDecoder.TryDecode(sampleJpeg, out var tryDecodedJpeg) && tryDecodedJpeg.Width == 8, "RasterImageDecoder should expose a non-throwing byte decode helper.");
        Assert(!RasterImageDecoder.TryDecode(new byte[] { 1, 2, 3, 4 }, out _), "RasterImageDecoder TryDecode should return false for unsupported data.");
        var decodedJpeg = RasterImageDecoder.Decode(sampleJpeg);
        Assert(decodedJpeg.Width == 8 && decodedJpeg.Height == 8 && decodedJpeg.Pixels.Length == 8 * 8 * 4, "RasterImageDecoder should decode baseline JPEG images to RGBA pixels.");
        var nonSquareJpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAGAAoDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDlfhX+yTon7nmHt/Cf8K+hIP2SdE8mPmH7o/hPp9KKK+nyLEVfq/xHPwDxJm/9nL/aJdD/2Q==");
        var decodedNonSquareJpeg = RasterImageDecoder.Decode(nonSquareJpeg);
        Assert(decodedNonSquareJpeg.Width == 10 && decodedNonSquareJpeg.Height == 6, "RasterImageDecoder should preserve JPEG dimensions before orientation metadata is applied.");
        var orientedJpeg = RasterImageDecoder.Decode(WithExifOrientation(nonSquareJpeg, 6));
        Assert(orientedJpeg.Width == 6 && orientedJpeg.Height == 10, "RasterImageDecoder should apply EXIF orientation for JPEG input.");
        Assert(RasterImageDecoder.Decode(sourceImage.ToBmp()).Width == 18, "RasterImageDecoder should decode dependency-free BMP images.");
        var decodedIndexedBmp = RasterImageDecoder.Decode(IndexedBmp());
        Assert(decodedIndexedBmp.Width == 2 && decodedIndexedBmp.Height == 1 && decodedIndexedBmp.Pixels[0] == 255 && decodedIndexedBmp.Pixels[4 + 2] == 255, "RasterImageDecoder should decode indexed BMP palettes.");
        var decodedTransparentBmp = RasterImageDecoder.Decode(Transparent32BitBmp());
        Assert(decodedTransparentBmp.Pixels[3] == 0, "RasterImageDecoder should preserve explicit BMP alpha channels even when all pixels are transparent.");
        Assert(RasterImageDecoder.Decode(sourceImage.ToPpm()).Height == 12, "RasterImageDecoder should decode dependency-free PPM images.");
        var decodedCrLfPpm = RasterImageDecoder.Decode(BinaryPpmWithCrLfHeader());
        Assert(decodedCrLfPpm.Pixels[0] == 255 && decodedCrLfPpm.Pixels[1] == 0 && decodedCrLfPpm.Pixels[2] == 0, "RasterImageDecoder should consume CRLF PPM header separators before binary pixels.");
        Assert(RasterImageDecoder.Decode(sourceImage.ToTiff()).Width == 18, "RasterImageDecoder should decode dependency-free uncompressed TIFF images.");
        var whiteIsZeroTiff = RasterImageDecoder.Decode(GrayscaleTiff(0, new byte[] { 0, 255 }));
        Assert(whiteIsZeroTiff.Pixels[0] == 255 && whiteIsZeroTiff.Pixels[4] == 0, "RasterImageDecoder should decode WhiteIsZero grayscale TIFF images.");
        Assert(!RasterImageDecoder.TryDecode(PlanarRgbTiff(), out _), "RasterImageDecoder should reject unsupported planar TIFF images without misreading channel planes.");
        Assert(!RasterImageDecoder.TryDecode(OversizedTiff(), out _), "RasterImageDecoder TryDecode should return false for malformed raster dimensions that overflow.");
        var imageFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chartforgex-raster-input-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            System.IO.File.WriteAllBytes(imageFilePath, sourcePng);
            Assert(RasterImageDecoder.TryRead(imageFilePath, out var tryReadPng) && tryReadPng.Height == 12, "RasterImageDecoder should expose a non-throwing file read helper.");
            var imageLayerCanvas = VisualCanvas.Create(48, 32)
                .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
                .AddImageFile(0, 0, 48, 32, imageFilePath, VisualCanvasImageFit.Cover)
                .AddImageBytes(VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 4, 4), 12, 8, sourcePng, VisualCanvasImageFit.Contain)
                .AddImageBytes(VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 4, 4), 12, 8, sampleJpeg, VisualCanvasImageFit.Contain)
                .AddText(4, 4, 40, "overlay", 9, ChartColor.White);
            var imageLayerSvg = imageLayerCanvas.ToSvg("visual-canvas-raster-input");
            Assert(imageLayerSvg.Contains("data:image/png;base64", StringComparison.Ordinal), "VisualCanvas image file layers should embed decoded source bytes for SVG output.");
            Assert(imageLayerSvg.Contains("data:image/jpeg;base64", StringComparison.Ordinal), "VisualCanvas image byte layers should preserve JPEG source bytes for SVG output.");
            Assert(imageLayerCanvas.ToPng().Length > 64, "VisualCanvas image file layers should render to PNG output.");
            Assert(decodedPng.ToVisualCanvas().ToPng().Length > 64, "Decoded RGBA images should be reusable as visual canvases.");
            var nonWebRasterSvg = VisualCanvas.Create(60, 20)
                .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
                .AddImageBytes(0, 0, 20, 20, sourceImage.ToBmp())
                .AddImageBytes(20, 0, 20, 20, sourceImage.ToPpm())
                .AddImageBytes(40, 0, 20, 20, sourceImage.ToTiff())
                .ToSvg("visual-canvas-non-web-raster-input");
            Assert(nonWebRasterSvg.Contains("data:image/png;base64", StringComparison.Ordinal), "VisualCanvas should re-encode non-web raster image inputs as PNG for SVG output.");
            Assert(!nonWebRasterSvg.Contains("data:image/bmp", StringComparison.Ordinal) && !nonWebRasterSvg.Contains("image/tiff", StringComparison.Ordinal) && !nonWebRasterSvg.Contains("image/x-portable-pixmap", StringComparison.Ordinal), "VisualCanvas SVG output should not embed non-web raster MIME types.");
        } finally {
            if (System.IO.File.Exists(imageFilePath)) System.IO.File.Delete(imageFilePath);
        }

        var anchoredCanvas = VisualCanvas.Create(400, 300)
            .AddInfoTile(VisualCanvasPlacement.At(VisualCanvasAnchor.TopLeft, 20, 20), 120, 60, "PC", "HOST", "WK01")
            .AddInfoTile(VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 20, 20), 120, 60, "IP", "IP", "10.0.0.42")
            .AddChart(VisualCanvasPlacement.At(VisualCanvasAnchor.TopRight, 20, 90), 140, 72, cpuChart, VisualCanvasImageFit.Contain)
            .AddTopology(VisualCanvasPlacement.At(VisualCanvasAnchor.BottomLeft, 20, 20), 140, 72, topology, fit: VisualCanvasImageFit.Cover);
        var topLeftTile = anchoredCanvas.Layers[0];
        var bottomRightTile = anchoredCanvas.Layers[1];
        var topRightChart = anchoredCanvas.Layers[2];
        var bottomLeftTopology = anchoredCanvas.Layers[3];
        Assert(IsClose(topLeftTile.X, 20) && IsClose(topLeftTile.Y, 20), "VisualCanvas TopLeft placement should use offsets from the top-left edge.");
        Assert(IsClose(bottomRightTile.X, 260) && IsClose(bottomRightTile.Y, 220), "VisualCanvas BottomRight placement should use offsets as insets from the bottom-right edge.");
        Assert(IsClose(topRightChart.X, 240) && IsClose(topRightChart.Y, 90), "VisualCanvas chart placement should support top-right anchors.");
        Assert(IsClose(bottomLeftTopology.X, 20) && IsClose(bottomLeftTopology.Y, 208), "VisualCanvas topology placement should support bottom-left anchors.");
        var relativeBounds = VisualCanvasPlacement.At(VisualCanvasAnchor.Center, 10, -8).Resolve(topLeftTile.Bounds, 20, 10);
        Assert(IsClose(relativeBounds.X, 80) && IsClose(relativeBounds.Y, 37), "VisualCanvas placements should resolve relative to another layer's bounds.");
        AssertThrows<ArgumentOutOfRangeException>(() => VisualCanvasPlacement.At((VisualCanvasAnchor)999, 0, 0), "VisualCanvas placement should reject unknown anchors.");

        var keyValueCanvas = VisualCanvas.Create(420, 260)
            .WithTitle("Key/value block")
            .WithBackground(ChartColor.FromHex("#07111F"), ChartColor.FromHex("#102A43"))
            .AddKeyValueBlock(
                VisualCanvasPlacement.At(VisualCanvasAnchor.BottomRight, 24, 24),
                300,
                new[] {
                    VisualCanvasKeyValueItem.LabelRow("System"),
                    VisualCanvasKeyValueItem.Pair("Host", "DEV-WKS-01"),
                    VisualCanvasKeyValueItem.Pair("IPv4", "10.0.0.42 192.168.1.42"),
                    VisualCanvasKeyValueItem.Pair("Notes", "This value should wrap into multiple deterministic lines for cross-platform BGInfo-style text.")
                },
                labelFontSize: 15,
                valueFontSize: 14,
                columnGap: 18,
                rowGap: 5,
                valueWrapWidth: 160,
                labelColor: ChartColor.FromHex("#FDE68A"),
                valueColor: ChartColor.FromHex("#E0F2FE"));
        var keyValue = (VisualCanvasKeyValueBlockLayer)keyValueCanvas.Layers[0];
        Assert(keyValue.Height > 70, "VisualCanvas key/value blocks should measure wrapped row height.");
        Assert(IsClose(keyValue.X, 96) && keyValue.Y > 24, "VisualCanvas key/value blocks should support bottom-right anchor placement.");
        var keyValueSvg = keyValueCanvas.ToSvg("visual-canvas-key-value");
        Assert(keyValueSvg.Contains("data-cfx-role=\"visual-canvas-key-value-block\"", StringComparison.Ordinal), "VisualCanvas should render key/value block layers in SVG.");
        Assert(keyValueSvg.Contains("DEV-WKS-01", StringComparison.Ordinal) && keyValueSvg.Contains("cross-platform", StringComparison.Ordinal), "VisualCanvas key/value blocks should preserve wrapped value text.");
        Assert(keyValueCanvas.ToPng().Length > 64, "VisualCanvas key/value blocks should render PNG output.");

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
        AssertThrows<ArgumentOutOfRangeException>(() => new VisualCanvasInfoTileLayer(0, 0, 100, 80, "I", "L", "V").WithTextFitPolicy((VisualCanvasTextFitPolicy)999), "VisualCanvas info tile text fit policy should reject unknown values.");
        AssertThrows<ArgumentException>(() => new VisualCanvasKeyValueBlockLayer(0, 0, 100, 1, Array.Empty<VisualCanvasKeyValueItem>()), "VisualCanvas key/value blocks should require rows.");
        AssertThrows<ArgumentOutOfRangeException>(() => new VisualCanvasKeyValueBlockLayer(0, 0, 100, 1, new[] { VisualCanvasKeyValueItem.Pair("A", "B") }) { LabelFontSize = 0 }, "VisualCanvas key/value blocks should reject invalid font sizes.");
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

    private static bool IsClose(double actual, double expected) => Math.Abs(actual - expected) < 0.001;

    private static int CountPixels(RgbaImage image, Func<byte, byte, byte, byte, bool> predicate) {
        var count = 0;
        for (var i = 0; i < image.Pixels.Length; i += 4) {
            if (predicate(image.Pixels[i], image.Pixels[i + 1], image.Pixels[i + 2], image.Pixels[i + 3])) count++;
        }

        return count;
    }

    private static byte[] WithExifOrientation(byte[] jpeg, ushort orientation) {
        var segment = new byte[] {
            0xFF, 0xE1, 0x00, 0x22,
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
            (byte)'M', (byte)'M', 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x01,
            0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };
        segment[28] = (byte)(orientation >> 8);
        segment[29] = (byte)(orientation & 255);
        var output = new byte[jpeg.Length + segment.Length];
        Buffer.BlockCopy(jpeg, 0, output, 0, 2);
        Buffer.BlockCopy(segment, 0, output, 2, segment.Length);
        Buffer.BlockCopy(jpeg, 2, output, 2 + segment.Length, jpeg.Length - 2);
        return output;
    }

    private static byte[] PngWithTransparentColor() {
        using var stream = new System.IO.MemoryStream();
        stream.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
        WritePngChunk(stream, "IHDR", new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 8, 2, 0, 0, 0 });
        WritePngChunk(stream, "tRNS", new byte[] { 0, 0x12, 0, 0x34, 0, 0x56 });
        WritePngChunk(stream, "IDAT", ZlibDeflate(new byte[] { 0, 0x12, 0x34, 0x56 }));
        WritePngChunk(stream, "IEND", Array.Empty<byte>());
        return stream.ToArray();
    }

    private static void WritePngChunk(System.IO.Stream stream, string type, byte[] data) {
        WriteBigEndian(stream, data.Length);
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        using var crcInput = new System.IO.MemoryStream();
        crcInput.Write(typeBytes, 0, typeBytes.Length);
        crcInput.Write(data, 0, data.Length);
        stream.Write(typeBytes, 0, typeBytes.Length);
        stream.Write(data, 0, data.Length);
        WriteBigEndian(stream, unchecked((int)Crc32(crcInput.ToArray())));
    }

    private static byte[] ZlibDeflate(byte[] data) {
        using var stream = new System.IO.MemoryStream();
        stream.WriteByte(0x78);
        stream.WriteByte(0x9C);
        using (var deflate = new System.IO.Compression.DeflateStream(stream, System.IO.Compression.CompressionLevel.Optimal, true)) {
            deflate.Write(data, 0, data.Length);
        }

        WriteBigEndian(stream, unchecked((int)Adler32(data)));
        return stream.ToArray();
    }

    private static uint Adler32(byte[] data) {
        const uint mod = 65521;
        uint a = 1;
        uint b = 0;
        foreach (var value in data) {
            a = (a + value) % mod;
            b = (b + a) % mod;
        }

        return (b << 16) | a;
    }

    private static uint Crc32(byte[] data) {
        uint crc = 0xffffffff;
        foreach (var value in data) {
            crc ^= value;
            for (var i = 0; i < 8; i++) crc = (crc & 1) == 1 ? 0xedb88320 ^ (crc >> 1) : crc >> 1;
        }

        return ~crc;
    }

    private static void WriteBigEndian(System.IO.Stream stream, int value) {
        stream.WriteByte((byte)((value >> 24) & 255));
        stream.WriteByte((byte)((value >> 16) & 255));
        stream.WriteByte((byte)((value >> 8) & 255));
        stream.WriteByte((byte)(value & 255));
    }

    private static byte[] IndexedBmp() {
        const int width = 2;
        const int height = 1;
        const int fileHeader = 14;
        const int dibHeader = 40;
        const int paletteBytes = 8;
        const int stride = 4;
        var bytes = new byte[fileHeader + dibHeader + paletteBytes + stride];
        bytes[0] = (byte)'B';
        bytes[1] = (byte)'M';
        WriteLittleEndian(bytes, 2, bytes.Length);
        WriteLittleEndian(bytes, 10, fileHeader + dibHeader + paletteBytes);
        WriteLittleEndian(bytes, 14, dibHeader);
        WriteLittleEndian(bytes, 18, width);
        WriteLittleEndian(bytes, 22, height);
        bytes[26] = 1;
        bytes[28] = 8;
        WriteLittleEndian(bytes, 34, stride);
        WriteLittleEndian(bytes, 46, 2);
        var palette = fileHeader + dibHeader;
        bytes[palette + 2] = 255;
        bytes[palette + 4] = 255;
        var pixels = fileHeader + dibHeader + paletteBytes;
        bytes[pixels] = 0;
        bytes[pixels + 1] = 1;
        return bytes;
    }

    private static byte[] Transparent32BitBmp() {
        const int width = 1;
        const int height = 1;
        const int fileHeader = 14;
        const int dibHeader = 40;
        const int pixelOffset = fileHeader + dibHeader;
        var bytes = new byte[pixelOffset + 4];
        bytes[0] = (byte)'B';
        bytes[1] = (byte)'M';
        WriteLittleEndian(bytes, 2, bytes.Length);
        WriteLittleEndian(bytes, 10, pixelOffset);
        WriteLittleEndian(bytes, 14, dibHeader);
        WriteLittleEndian(bytes, 18, width);
        WriteLittleEndian(bytes, 22, height);
        bytes[26] = 1;
        bytes[28] = 32;
        WriteLittleEndian(bytes, 34, 4);
        bytes[pixelOffset] = 0x30;
        bytes[pixelOffset + 1] = 0x20;
        bytes[pixelOffset + 2] = 0x10;
        bytes[pixelOffset + 3] = 0x00;
        return bytes;
    }

    private static byte[] BinaryPpmWithCrLfHeader() {
        var header = System.Text.Encoding.ASCII.GetBytes("P6\n1 1\n255\r\n");
        var bytes = new byte[header.Length + 3];
        Buffer.BlockCopy(header, 0, bytes, 0, header.Length);
        bytes[header.Length] = 255;
        return bytes;
    }

    private static byte[] GrayscaleTiff(ushort photometric, byte[] pixels) => SimpleTiff(2, 1, photometric, 1, 1, pixels);

    private static byte[] PlanarRgbTiff() => SimpleTiff(1, 1, 2, 3, 2, new byte[] { 255, 0, 0 });

    private static byte[] OversizedTiff() => SimpleTiff(0x80000000, 1, 1, 1, 1, Array.Empty<byte>());

    private static byte[] SimpleTiff(uint width, uint height, ushort photometric, ushort samplesPerPixel, ushort planarConfiguration, byte[] pixels) {
        const int entryCount = 10;
        const int ifdOffset = 8;
        const int ifdBytes = 2 + entryCount * 12 + 4;
        const int pixelOffset = ifdOffset + ifdBytes;
        var bytes = new byte[pixelOffset + pixels.Length];
        bytes[0] = (byte)'I';
        bytes[1] = (byte)'I';
        WriteLittleEndianShort(bytes, 2, 42);
        WriteLittleEndian(bytes, 4, ifdOffset);
        WriteLittleEndianShort(bytes, ifdOffset, entryCount);
        var entry = ifdOffset + 2;
        WriteTiffEntry(bytes, ref entry, 256, 4, 1, width);
        WriteTiffEntry(bytes, ref entry, 257, 4, 1, height);
        WriteTiffEntry(bytes, ref entry, 258, 3, 1, 8);
        WriteTiffEntry(bytes, ref entry, 259, 3, 1, 1);
        WriteTiffEntry(bytes, ref entry, 262, 3, 1, photometric);
        WriteTiffEntry(bytes, ref entry, 273, 4, 1, pixelOffset);
        WriteTiffEntry(bytes, ref entry, 277, 3, 1, samplesPerPixel);
        WriteTiffEntry(bytes, ref entry, 278, 4, 1, height);
        WriteTiffEntry(bytes, ref entry, 279, 4, 1, (uint)pixels.Length);
        WriteTiffEntry(bytes, ref entry, 284, 3, 1, planarConfiguration);
        Buffer.BlockCopy(pixels, 0, bytes, pixelOffset, pixels.Length);
        return bytes;
    }

    private static void WriteTiffEntry(byte[] data, ref int offset, ushort tag, ushort type, uint count, uint value) {
        WriteLittleEndianShort(data, offset, tag);
        WriteLittleEndianShort(data, offset + 2, type);
        WriteLittleEndian(data, offset + 4, unchecked((int)count));
        WriteLittleEndian(data, offset + 8, unchecked((int)value));
        offset += 12;
    }

    private static void WriteLittleEndian(byte[] data, int offset, int value) {
        data[offset] = (byte)(value & 255);
        data[offset + 1] = (byte)((value >> 8) & 255);
        data[offset + 2] = (byte)((value >> 16) & 255);
        data[offset + 3] = (byte)((value >> 24) & 255);
    }

    private static void WriteLittleEndianShort(byte[] data, int offset, int value) {
        data[offset] = (byte)(value & 255);
        data[offset + 1] = (byte)((value >> 8) & 255);
    }
}
