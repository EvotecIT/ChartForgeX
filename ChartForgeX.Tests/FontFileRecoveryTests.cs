using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class FontFileRecoveryTests {
    [Fact]
    public void MissingAndInvalidFontFilesCanBeRegisteredAfterTheyBecomeValid() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-font-recovery-" + Guid.NewGuid().ToString("N") + ".otf");
        try {
            Assert.Null(TrueTypeFont.TryLoadFromPath(path));
            File.WriteAllBytes(path, new byte[] { 0, 1, 2 });
            Assert.Null(TrueTypeFont.TryLoadFromPath(path));
            File.WriteAllBytes(path, OpenTypeTestFonts.NameKeyed());
            FontRegistry.Register("Recovery", path);
            Assert.NotNull(TypographyFontResolver.ResolveThemeFont("Recovery"));
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }

    [Fact]
    public void ReplacementAndExplicitClearReloadFontFileContents() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-font-replacement-" + Guid.NewGuid().ToString("N") + ".otf");
        try {
            File.WriteAllBytes(path, OpenTypeTestFonts.NameKeyed(weight: 400));
            var regular = TrueTypeFont.TryLoadFromPath(path)!;
            Assert.Equal(400, regular.Weight);
            File.WriteAllBytes(path, OpenTypeTestFonts.NameKeyed(weight: 700));
            File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddSeconds(2));
            Assert.Equal(700, TrueTypeFont.TryLoadFromPath(path)!.Weight);
            var timestamp = File.GetLastWriteTimeUtc(path);
            File.WriteAllBytes(path, OpenTypeTestFonts.NameKeyed(weight: 400));
            File.SetLastWriteTimeUtc(path, timestamp);
            FontRegistry.Clear();
            Assert.Equal(400, TrueTypeFont.TryLoadFromPath(path)!.Weight);
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }
}
