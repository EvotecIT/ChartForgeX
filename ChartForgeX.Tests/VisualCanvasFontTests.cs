using ChartForgeX.Composition;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// VisualCanvas PNG output draws with the theme font family at the weights its SVG output writes,
/// and fits, wraps, and measures with the face it draws. Cases that need a particular installed
/// family return early on hosts without it.
/// </summary>
public sealed class VisualCanvasFontTests {
    [Fact]
    public void PngTextUsesTheThemeFamilyAndRealWeights() {
        if (!HasFace("Georgia", 400) || !HasFace("Georgia", 700) || !HasFace("Segoe UI", 400)) return;
        var georgia = Png(Canvas("Georgia, serif").AddText(10, 10, 580, "Hamburgefonstiv 0123", 28, ChartColors.White));
        var segoe = Png(Canvas("Segoe UI, sans-serif").AddText(10, 10, 580, "Hamburgefonstiv 0123", 28, ChartColors.White));
        var georgiaBold = Png(Canvas("Georgia, serif").AddText(10, 10, 580, "Hamburgefonstiv 0123", 28, ChartColors.White, emphasized: true));
        Assert.NotEqual(georgia.Pixels, segoe.Pixels);
        Assert.True(Ink(georgiaBold) > Ink(georgia) * 1.15, "Emphasized canvas text should draw Georgia Bold, not Georgia.");
        Assert.False(TypographyFontResolver.ResolveFace("Georgia", VisualCanvasFontWeights.Emphasized, false).SynthesizeBold);
    }

    [Fact]
    public void PngAndSvgTextShareTheirFaceAndPlacement() {
        var family = HasFace("Segoe UI", 400) ? "Segoe UI, sans-serif" : "sans-serif";
        var canvas = Canvas(family)
            .AddText(20, 12, 560, "Plain canvas text", 24, ChartColors.White)
            .AddText(20, 60, 560, "Emphasized canvas text", 24, ChartColors.White, emphasized: true);
        var png = Png(canvas);
        var svg = SvgRasterizer.ToImage(canvas.ToSvg());
        foreach (var band in new[] { (Top: 0, Bottom: 50), (Top: 50, Bottom: 110) }) {
            var fromPng = InkBounds(png, band.Top, band.Bottom);
            var fromSvg = InkBounds(svg, band.Top, band.Bottom);
            Assert.InRange(fromPng.Right - fromSvg.Right, -2, 2);
            Assert.InRange(fromPng.Top - fromSvg.Top, -2, 2);
            Assert.InRange(fromPng.Bottom - fromSvg.Bottom, -2, 2);
        }
    }

    [Fact]
    public void FittedTextStaysInsideItsBoxInBothOutputs() {
        // A wide face: measuring with a narrower one and drawing with this one would overflow.
        var family = HasFace("Verdana", 700) ? "Verdana" : HasFace("Georgia", 700) ? "Georgia" : "sans-serif";
        const double left = 20, width = 220;
        var canvas = Canvas(family)
            .AddText(left, 10, width, "Emphasized text that is far too long for its box", 22, ChartColors.White, emphasized: true)
            .AddKeyValueBlock(left, 60, width + 40, new[] { VisualCanvasKeyValueItem.Pair("Operating system", "Windows Server 2025 Datacenter with a long edition name") }, labelFontSize: 16, valueFontSize: 16, columnGap: 12);
        foreach (var image in new[] { Png(canvas), SvgRasterizer.ToImage(canvas.ToSvg()) }) {
            Assert.True(InkBounds(image, 0, 50).Right <= left + width + 1, "Fitted text must end inside its layer.");
            Assert.True(InkBounds(image, 55, image.Height).Right <= left + width + 40 + 1, "Wrapped key/value text must end inside its block.");
        }

        var block = (VisualCanvasKeyValueBlockLayer)canvas.Layers[1];
        Assert.Equal(block.MeasureHeight(canvas.Theme), block.Height);
    }

    [Fact]
    public void InfoTileTextFitsTheTileWithTheDrawnFace() {
        var family = HasFace("Verdana", 700) ? "Verdana" : "sans-serif";
        var canvas = Canvas(family).AddInfoTile(10, 10, 360, 100, "CPU", "Processor", "Intel Xeon Platinum 8380 @ 2.30GHz", "40 cores, 80 threads", textFitPolicy: VisualCanvasTextFitPolicy.SingleLineEllipsis);
        var tile = (VisualCanvasInfoTileLayer)canvas.Layers[0];
        var metrics = VisualCanvasInfoTileTextLayout.CalculateMetrics(tile);
        var layout = VisualCanvasInfoTileTextLayout.BuildResult(tile, metrics.Y, metrics.Height, metrics.TextX, metrics.TextMax, family);
        foreach (var line in layout.Lines) {
            var width = VisualCanvasTextFace.Resolve(family, line.Weight).Measure(line.Text, line.FontSize);
            Assert.True(width <= metrics.TextMax + 0.01, "Tile line '" + line.Text + "' is wider than the tile text area.");
        }

        Assert.True(canvas.AnalyzeLayout().HasWarnings, "A value trimmed to fit should still be reported.");
    }

    private static VisualCanvas Canvas(string family) =>
        VisualCanvas.Create(600, 130).WithBackdrop(VisualCanvasBackdropStyle.Transparent).WithTheme(new VisualCanvasTheme { FontFamily = family });

    private static RgbaImage Png(VisualCanvas canvas) => new PngVisualCanvasRenderer().RenderImage(canvas);

    private static bool HasFace(string family, int weight) => InstalledFontCatalog.Find(family, weight, false)?.Weight == weight;

    private static long Ink(RgbaImage image) {
        long ink = 0;
        for (var index = 3; index < image.Pixels.Length; index += 4) ink += image.Pixels[index];
        return ink;
    }

    private static (int Top, int Bottom, int Right) InkBounds(RgbaImage image, int fromRow, int toRow) {
        int top = int.MaxValue, bottom = -1, right = -1;
        for (var y = Math.Max(0, fromRow); y < Math.Min(image.Height, toRow); y++) {
            for (var x = 0; x < image.Width; x++) {
                if (image.Pixels[(y * image.Width + x) * 4 + 3] < 96) continue;
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
                right = Math.Max(right, x);
            }
        }

        return (top, bottom, right);
    }
}
