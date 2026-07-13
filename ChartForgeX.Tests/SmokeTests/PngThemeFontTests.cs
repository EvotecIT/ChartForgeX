using System;
using System.IO;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PngFontDiagnosticsDescribeFallbackDecisions() {
        var collectionPath = "/System/Library/Fonts/HelveticaNeue.ttc";
        var automatic = Chart.Create().GetPngFontInfo();
        Assert(automatic.Source == PngFontSource.Automatic || automatic.Source == PngFontSource.BuiltIn, "PNG font diagnostics should report automatic or built-in fallback for default charts.");
        Assert(automatic.UsesOutlineFont == (automatic.Source != PngFontSource.BuiltIn), "PNG font diagnostics should describe outline font usage consistently.");

        var missingFont = Path.Combine(Path.GetTempPath(), "ChartForgeX-missing-font-" + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture) + ".ttf");
        var missing = Chart.Create().WithPngFont(missingFont).GetPngFontInfo();
        Assert(missing.RequestedPath == Path.GetFullPath(missingFont), "PNG font diagnostics should include the requested font path.");
        Assert(missing.Source != PngFontSource.Requested, "PNG font diagnostics should not report requested source when the configured font cannot be loaded.");

        if (File.Exists(collectionPath)) {
            var requested = Chart.Create().WithPngFont(collectionPath, faceName: "Helvetica Neue").GetPngFontInfo();
            Assert(requested.Source == PngFontSource.Requested, "PNG font diagnostics should report requested source when a configured font loads.");
            Assert(string.Equals(requested.ResolvedPath, Path.GetFullPath(collectionPath), StringComparison.OrdinalIgnoreCase), "PNG font diagnostics should include the resolved requested path.");
            Assert(requested.ResolvedCollectionIndex.HasValue, "PNG font diagnostics should include the resolved collection index for TrueType collections.");
            Assert(!string.IsNullOrWhiteSpace(requested.ResolvedFaceName), "PNG font diagnostics should include a resolved face name when available.");
            var indexed = Chart.Create().WithPngFont(collectionPath, 0).GetPngFontInfo();
            Assert(indexed.ResolvedCollectionIndex == 0, "PNG font diagnostics should preserve explicitly selected collection indexes.");
        }
    }

    private static void PngAutomaticFontsHonorThemeFamilies() {
        var serifTheme = ChartTheme.Light();
        serifTheme.FontFamily = ChartFontStacks.Serif;
        var monoTheme = ChartTheme.Light();
        monoTheme.FontFamily = ChartFontStacks.Mono;
        var serifChart = Chart.Create().WithTheme(serifTheme).WithSize(420, 240).WithTitle("Typography 012345").AddLine("Values", Points(10, 20, 30));
        var monoChart = Chart.Create().WithTheme(monoTheme).WithSize(420, 240).WithTitle("Typography 012345").AddLine("Values", Points(10, 20, 30));
        var serif = serifChart.GetPngFontInfo();
        var mono = monoChart.GetPngFontInfo();

        Assert(serif.ThemeFontFamily == ChartFontStacks.Serif && mono.ThemeFontFamily == ChartFontStacks.Mono, "PNG font diagnostics should preserve the theme family used for automatic selection.");
        if (serif.Source == PngFontSource.Automatic && mono.Source == PngFontSource.Automatic) {
            Assert(!string.Equals(serif.ResolvedPath, mono.ResolvedPath, StringComparison.OrdinalIgnoreCase), "Automatic PNG font resolution should select distinct serif and monospace faces when platform fonts are available.");
            Assert(!serifChart.ToPng().SequenceEqual(monoChart.ToPng()), "Theme font-family changes should affect PNG pixels when distinct platform faces are available.");
        }

        TrueTypeFont.TryLoadForFamily("Georgia", out var explicitSerifPath);
        TrueTypeFont.TryLoadForFamily(ChartFontStacks.Serif, out var genericSerifPath);
        TrueTypeFont.TryLoadForFamily("Courier New", out var explicitMonoPath);
        TrueTypeFont.TryLoadForFamily(ChartFontStacks.Mono, out var genericMonoPath);
        if (explicitSerifPath != null && genericSerifPath != null) Assert(string.Equals(explicitSerifPath, genericSerifPath, StringComparison.OrdinalIgnoreCase), "Explicit serif family names should select the serif PNG font category.");
        if (explicitMonoPath != null && genericMonoPath != null) Assert(string.Equals(explicitMonoPath, genericMonoPath, StringComparison.OrdinalIgnoreCase), "Explicit monospace family names should select the monospace PNG font category.");
    }

    private static void PngCanvasMeasuresWithItsSelectedDrawingFont() {
        var font = TrueTypeFont.TryLoadForFamily(ChartFontStacks.Mono, out _);
        if (font == null) return;

        var canvas = new RgbaCanvas(320, 120, 2, font);
        const string sample = "Metric WWW 012345";
        const double fontSize = 19;
        Assert(Math.Abs(canvas.MeasureTextWidth(sample, fontSize) - RgbaCanvas.MeasureTextWidth(sample, fontSize, font)) < 0.000001, "Canvas text fitting should use the same selected font as PNG drawing.");
        Assert(Math.Abs(canvas.MeasureTextEmphasizedWidth(sample, fontSize) - RgbaCanvas.MeasureTextEmphasizedWidth(sample, fontSize, font)) < 0.000001, "Emphasized canvas text fitting should use the selected drawing font.");
        Assert(Math.Abs(canvas.MeasureTextHeight(fontSize) - RgbaCanvas.MeasureTextHeight(fontSize, font)) < 0.000001, "Canvas vertical alignment should use the selected drawing font line height.");
    }
}
