using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeValueRepertoireProgramSourceTests {
    public static IEnumerable<object[]> Cases() {
        foreach (var donut in new[] { false, true }) foreach (var width in new[] { 390, 760 }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            yield return new object[] { donut, width, mode };
    }
    [Theory, MemberData(nameof(Cases))]
    public void NormalCoreAndAdapterProgramUsesTheActualCompactFormatterAndExplicitHostFonts(bool donut, int width, VisualThemeMode mode) {
        var program = ExampleRecipeSource.EmitNativeValueTextRepertoire(donut, width, mode);
        NativeRadialMotionBeforeControls.Save("value-text-consumer-" + donut + "-" + width + "-" + mode + ".program.cs", program);
        Assert.DoesNotContain("{{", program, StringComparison.Ordinal); Assert.Contains("class NativeRadialMotionRecipe", program, StringComparison.Ordinal);
        Assert.Contains("ChartValueFormat.Compact()", program, StringComparison.Ordinal); Assert.Contains("FontRegistry.Register", program, StringComparison.Ordinal);
        Assert.Contains("Model(900)", program, StringComparison.Ordinal); Assert.Contains("Model(1100)", program, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenTypeTestFonts", program, StringComparison.Ordinal); Assert.DoesNotContain("Microsoft.Playwright", program, StringComparison.Ordinal);
    }
    [Fact]
    public void RetainDeterministicPrimaryAndActualNumericFallbackFontInputsForTheOrdinaryConsumerPrograms() {
        var primary = NativeFallbackClusterBeforeControls.PrimaryBytes(); var fallback = NativeValueRepertoireMotionTests.FallbackBytes();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            var directory = Path.Combine(output, "value-text-consumer-fonts"); Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "primary.otf"), primary); File.WriteAllBytes(Path.Combine(directory, "fallback.otf"), fallback);
            NativeRadialMotionBeforeControls.Save("value-text-consumer-fonts.facts.json", System.Text.Json.JsonSerializer.Serialize(new {
                owner = "existing test-only OpenTypeTestFonts", primary = "ordinary digits/Mg", fallback = "actual numeric punctuation/suffix Box glyphs", meaning = "native fallback/replay fixture, no glyph artwork reference" }));
        }
        Assert.NotEmpty(primary); Assert.NotEmpty(fallback);
    }
}
