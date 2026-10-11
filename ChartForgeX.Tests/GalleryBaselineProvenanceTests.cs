using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GalleryBaselineProvenanceTests {
    [Theory]
    [InlineData("advanced-topology", "different", false, false, true)]
    [InlineData("advanced-topology", "same", false, false, false)]
    [InlineData("mermaid-er-direction", "different", false, false, true)]
    [InlineData("mermaid-er-direction", "same", false, false, false)]
    [InlineData("mermaid-state-direction", "different", false, false, true)]
    [InlineData("mermaid-state-direction", "same", false, false, false)]
    [InlineData("advanced-topology", "missing", false, false, false)]
    [InlineData("advanced-topology", "request-changed", false, false, false)]
    [InlineData("fixed-canvas", "different", false, false, false)]
    [InlineData("advanced-topology", "different", true, false, false)]
    [InlineData("advanced-topology", "different", false, true, false)]
    public void NaturalHeightRequiresChangedResolvedFrameFontAndExactWidthAndAllocation(string name, string fontState, bool changedWidth, bool extraPixel, bool matches) {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-gallery-font-baseline-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            var builder = new VisualSceneBuilder(new VisualSize(160, 100.014), FontSpec.SystemSans());
            builder.Rect(new ChartRect(0, 0, 160, 100.014), ChartColor.White, role: "background");
            builder.Text("Frame", 15, 30, new TextStyle { Font = FontSpec.SystemSans(), FontSize = 12 }, role: "frame-heading");
            for (var index = 0; index < 10; index++) builder.Rect(new ChartRect(15 + index * 12, 45, 10, 30), ChartColor.FromRgb((byte)(index * 20), 100, 150));
            var scene = builder.Build();
            File.WriteAllText(Path.Combine(output, name + ".svg"), VisualSceneSvgRenderer.Render(scene));
            var image = VisualSceneRasterRenderer.Render(scene, 1, supersampling: 1);
            File.WriteAllBytes(Path.Combine(output, name + ".png"), PngWriter.WriteRgba(image));
            GalleryWriter.Write(output);
            using var generated = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
            var actual = generated.RootElement.GetProperty("charts")[0];
            var layout = actual.GetProperty("layout");
            // A deliberately different resolved face permits only a natural height difference.
            var fingerprint = fontState == "same" ? layout.GetProperty("frameFontFingerprint").GetString() : fontState == "missing" ? "" : "OTHER-FONT-BYTES";
            var request = layout.GetProperty("frameFontRequest").GetString() + (fontState == "request-changed" ? "changed" : "");
            var expected = new {
                name, width = changedWidth ? 159 : 160, height = 99, logicalWidth = changedWidth ? 159d : 160d, logicalHeight = 99.014,
                layout = new { heightMode = "natural", frameFontRequest = request, frameFontFingerprint = fingerprint },
                svg = new { minVisualNodes = 2, maxClippedTextNodes = 0, maxNearEdgeTextNodes = 0 },
                png = new { outputScale = 1, minVisiblePixels = 64, minTransparentPixels = 0, minDistinctColors = 8, maxEdgeInkPixels = 0 }
            };
            File.WriteAllText(Path.Combine(output, "visual-baseline.json"), JsonSerializer.Serialize(new { version = 1, charts = new[] { expected } }));
            if (extraPixel) {
                var expanded = new RgbaCanvas(image.Width, image.Height + 1, 1);
                expanded.Clear(ChartColor.White);
                expanded.DrawImage(0, 0, image.Width, image.Height, image.Pixels);
                File.WriteAllBytes(Path.Combine(output, name + ".png"), PngWriter.WriteRgba(expanded.ToImage()));
            }
            GalleryWriter.Write(output);
            using var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "svg-png-comparison.json")));
            Assert.True(matches == result.RootElement.GetProperty("baseline").GetProperty("clean").GetBoolean(), result.RootElement.GetProperty("charts")[0].GetRawText());
        } finally { Directory.Delete(output, true); }
    }
}
