using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeFallbackConsumerFontInputsTests : IDisposable {
    [Fact]
    public void RetainExistingLatinAndSyntheticCjkFallbackInputsForOrdinaryConsumerPrograms() {
        var regular = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        var fallback = OpenTypeTestFonts.NameKeyed(extraGlyphs: new Dictionary<int, int> { [0x4E16] = OpenTypeTestFonts.Box, [0x754C] = OpenTypeTestFonts.Box });
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            var fonts = Path.Combine(output, "fallback-consumer-fonts"); Directory.CreateDirectory(fonts); File.Copy(regular, Path.Combine(fonts, "primary.ttf"), true); File.WriteAllBytes(Path.Combine(fonts, "fallback.otf"), fallback);
            NativeRadialMotionBeforeControls.Save("fallback-consumer-fonts.facts.json", System.Text.Json.JsonSerializer.Serialize(new { primary = "existing test-only Carlito-Regular.ttf", fallback = "synthetic OpenTypeTestFonts.NameKeyed, U+4E16/U+754C box glyphs", runtime = "not a Chinese glyph appearance reference" }));
        }
        FontRegistry.Register("CFX Consumer Cjk Fixture", fallback); Assert.NotEmpty(fallback); Assert.True(File.Exists(regular));
    }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
