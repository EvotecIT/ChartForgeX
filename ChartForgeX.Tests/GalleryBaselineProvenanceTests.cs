using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class GalleryBaselineProvenanceTests {
    [Theory]
    [InlineData("Arial")]
    [InlineData("Missing CFX Provenance Font")]
    [InlineData("CFX Provenance Carlito")]
    public void HeadingProvenanceRetainsAuthoredIdentityAcrossFittingAndLegacyMetadata(string family) {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-gallery-fitted-font-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        if (family == "CFX Provenance Carlito")
            FontRegistry.Register(family, Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Bold.ttf"), 700);
        try {
            var smaller = Render(120, Path.Combine(output, "smaller"));
            var larger = Render(240, Path.Combine(output, "larger"));
            Assert.NotEqual(smaller.PaintedSize, larger.PaintedSize);
            Assert.Equal(family + "|700|normal|256|", smaller.Request);
            Assert.Equal(smaller.Request, larger.Request);
            Assert.NotEmpty(smaller.Fingerprint);
            Assert.Equal(smaller.Fingerprint, larger.Fingerprint);
            var ordinary = Render(120, Path.Combine(output, "ordinary"), 12);
            var legacy = Render(120, Path.Combine(output, "legacy"), 12, stripAuthoredRequest: true);
            Assert.Equal(12, ordinary.PaintedSize);
            Assert.Equal(family + ", sans-serif|700|normal|12|", legacy.Request);
            Assert.NotEqual(ordinary.Fingerprint, legacy.Fingerprint);
        } finally { Directory.Delete(output, true); }

        (double PaintedSize, string Request, string Fingerprint) Render(int height, string folder, double fontSize = 256, bool stripAuthoredRequest = false) {
            Directory.CreateDirectory(folder);
            var font = FontSpec.FromFamily(family); font.Weight = 700;
            var size = new VisualSize(240, height);
            var context = new VisualRenderContext(new VisualLayoutOptions(size, 16),
                frame: new VisualFrame("Fitted request", showLegend: false, titleStyle: new TextStyle { Font = font, FontSize = fontSize }), font: font);
            var builder = new VisualSceneBuilder(size, font);
            VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
            var scene = builder.Build();
            const string name = "mermaid-source-font-size-group-header-fit";
            var svg = VisualSceneSvgRenderer.Render(scene);
            if (stripAuthoredRequest) {
                var document = XDocument.Parse(svg);
                foreach (var attribute in document.Descendants().Attributes("data-cfx-authored-font-request").ToArray()) attribute.Remove();
                svg = document.ToString(SaveOptions.DisableFormatting);
            }
            File.WriteAllText(Path.Combine(folder, name + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), PngWriter.WriteRgba(VisualSceneRasterRenderer.Render(scene, 1, supersampling: 1)));
            GalleryWriter.Write(folder);
            using var comparison = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "svg-png-comparison.json")));
            var layout = comparison.RootElement.GetProperty("charts")[0].GetProperty("layout");
            var painted = Assert.Single(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName == "text");
            return ((double)painted.Attribute("font-size")!, layout.GetProperty("frameFontRequest").GetString()!, layout.GetProperty("frameFontFingerprint").GetString()!);
        }
    }

    [Theory]
    [InlineData("advanced-topology", "different", false, false, true)]
    [InlineData("advanced-topology", "same", false, false, false)]
    [InlineData("mermaid-er-direction", "different", false, false, true)]
    [InlineData("mermaid-er-direction", "same", false, false, false)]
    [InlineData("mermaid-state-direction", "different", false, false, true)]
    [InlineData("mermaid-state-direction", "same", false, false, false)]
    [InlineData("mermaid-source-font-size-group-header-fit", "different", false, false, true)]
    [InlineData("mermaid-source-font-size-group-header-fit", "same", false, false, false)]
    [InlineData("mermaid-source-font-size-group-header-fit", "missing", false, false, false)]
    [InlineData("mermaid-source-font-size-group-header-fit", "request-changed", false, false, false)]
    [InlineData("mermaid-source-font-size-group-header-fit", "different", true, false, false)]
    [InlineData("mermaid-source-font-size-group-header-fit", "different", false, true, false)]
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
