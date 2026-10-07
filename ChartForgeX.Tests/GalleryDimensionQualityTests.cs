using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GalleryDimensionQualityTests {
    [Theory]
    [InlineData(120, 63.014, 1, false)]
    [InlineData(120, 63.252, 2, false)]
    [InlineData(120.252, 63.014, 3, false)]
    [InlineData(120.252, 63.014, 2, true)]
    [InlineData(120, 64, 2, false)]
    public void FractionalLogicalViewportMatchesCeilingAfterCommonRasterScale(double width, double height, int scale, bool viewBoxOnly) {
        var scene = Scene(width, height);
        var svg = VisualSceneSvgRenderer.Render(scene);
        if (viewBoxOnly) {
            var root = XElement.Parse(svg);
            root.Attribute("width")!.Remove();
            root.Attribute("height")!.Remove();
            svg = root.ToString(SaveOptions.DisableFormatting);
        }
        var png = PngWriter.WriteRgba(VisualSceneRasterRenderer.Render(scene, scale, supersampling: 1));
        Check(svg, png, pair => {
            Assert.True(pair.GetProperty("dimensionsMatch").GetBoolean());
            Assert.Equal(scale, pair.GetProperty("png").GetProperty("scale").GetInt32());
            Assert.Equal(width, pair.GetProperty("svg").GetProperty("logicalWidth").GetDouble());
            Assert.Equal(height, pair.GetProperty("svg").GetProperty("logicalHeight").GetDouble());
        });
    }

    [Theory]
    [InlineData(120, 63.014, 120, 63)] // Old nearest-integer allocation loses the last logical row.
    [InlineData(120, 63.252, 240, 126)] // Rounding before scaling loses one allocated pixel.
    [InlineData(120, 63.252, 240, 128)] // Ceiling before scaling allocates an extra pixel.
    [InlineData(120, 64, 240, 129)] // Integral viewports do not receive a one-pixel allowance.
    [InlineData(120, 64, 240, 192)] // Independent axis scales are not equivalent output.
    public void IncorrectPixelAllocationRemainsADimensionMismatch(double width, double height, int pngWidth, int pngHeight) {
        var svg = VisualSceneSvgRenderer.Render(Scene(width, height));
        var png = PngWriter.WriteRgba(pngWidth, pngHeight, new byte[pngWidth * pngHeight * 4]);
        Check(svg, png, pair => {
            Assert.False(pair.GetProperty("dimensionsMatch").GetBoolean());
            Assert.Equal(0, pair.GetProperty("png").GetProperty("scale").GetInt32());
            Assert.Contains(pair.GetProperty("warnings").EnumerateArray(), warning => warning.GetString() == "dimension mismatch");
        });
    }

    [Theory]
    [InlineData(1, false, 0, 0, true)] // A legacy integer baseline cannot encode fractional precision.
    [InlineData(2, true, 0, 0, true)]
    [InlineData(3, true, 0, 0, true)]
    [InlineData(2, true, .001, 0, false)] // Same rounded size and pixel allocation, different logical viewport.
    [InlineData(2, true, 0, 1, false)] // No extra-pixel allowance at the baseline boundary.
    public void FractionalBaselineUsesExactLogicalSizeAndScaledCeiling(int scale, bool hasLogicalSize, double widthChange, int extraPixels, bool matches) {
        const double width = 120.252, height = 63.014;
        var baselineChart = new Dictionary<string, object> {
            ["name"] = "allocation", ["width"] = (int)Math.Round(width), ["height"] = (int)Math.Round(height),
            ["svg"] = new { minVisualNodes = 2, maxClippedTextNodes = 0, maxNearEdgeTextNodes = 0 },
            ["png"] = new { outputScale = scale, minVisiblePixels = 64, minTransparentPixels = 0, minDistinctColors = 8, maxEdgeInkPixels = 0 }
        };
        if (hasLogicalSize) { baselineChart["logicalWidth"] = width; baselineChart["logicalHeight"] = height; }
        var baseline = JsonSerializer.Serialize(new { version = 1, charts = new[] { baselineChart } });
        var scene = Scene(width + widthChange, height);
        var image = VisualSceneRasterRenderer.Render(scene, scale, supersampling: 1);
        if (extraPixels > 0) {
            var canvas = new RgbaCanvas(image.Width + extraPixels, image.Height, 1);
            canvas.Clear(ChartColor.White);
            canvas.DrawImage(0, 0, image.Width, image.Height, image.Pixels);
            image = canvas.ToImage();
        }
        Check(VisualSceneSvgRenderer.Render(scene), PngWriter.WriteRgba(image), pair => {
            Assert.Equal(extraPixels == 0, pair.GetProperty("dimensionsMatch").GetBoolean());
            if (extraPixels == 0) {
                Assert.True(pair.GetProperty("png").GetProperty("healthy").GetBoolean(), pair.GetProperty("png").GetRawText());
                Assert.Equal(0, pair.GetProperty("png").GetProperty("edgeInkPixels").GetInt64());
            }
        }, baseline, summary => {
            Assert.True(summary.GetProperty("present").GetBoolean());
            Assert.Equal(matches ? 1 : 0, summary.GetProperty("chartMatches").GetInt32());
            Assert.Equal(matches ? 0 : 1, summary.GetProperty("warnings").GetInt32());
            Assert.Equal(matches, summary.GetProperty("clean").GetBoolean());
        });
    }

    private static VisualScene Scene(double width, double height) {
        var builder = new VisualSceneBuilder(new VisualSize(width, height), FontSpec.SystemSans());
        builder.Rect(new ChartRect(0, 0, width, height), ChartColor.White, role: "background");
        builder.Rect(new ChartRect(10, 10, 40, 30), ChartColor.FromRgb(20, 80, 160));
        for (var index = 0; index < 7; index++)
            builder.Rect(new ChartRect(65, 10 + index * 5, 10, 4), ChartColor.FromRgb((byte)(40 + index * 20), 100, 80));
        return builder.Build();
    }

    private static void Check(string svg, byte[] png, Action<JsonElement> check, string? baseline = null, Action<JsonElement>? checkBaseline = null) {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-gallery-dimensions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            File.WriteAllText(Path.Combine(output, "allocation.svg"), svg);
            File.WriteAllBytes(Path.Combine(output, "allocation.png"), png);
            if (baseline != null) File.WriteAllText(Path.Combine(output, "visual-baseline.json"), baseline);
            GalleryWriter.Write(output);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
            check(manifest.RootElement.GetProperty("charts")[0]);
            checkBaseline?.Invoke(manifest.RootElement.GetProperty("baseline"));
        } finally { Directory.Delete(output, true); }
    }
}
