using System.Text.Json;

public static partial class ExampleRecipeSource {
    /// <summary>Packages the canonical UseCase source as a complete ordinary Mermaid-package program.</summary>
    public static string EmitUseCaseSource(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, bool galleryTypography = false) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var source = ReadResource("ChartForgeX.Examples.MermaidFixtures.usecase-native-source-depth.mmd");
        if (variant == "compact") source = source.Replace("direction LR", "direction TB", StringComparison.Ordinal);
        return Template("RecipeUseCaseSourceProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{SOURCE_TEXT}}"] = JsonSerializer.Serialize(source), ["{{WIDTH}}"] = variant == "compact" ? "390" : "900", ["{{MODE}}"] = mode.ToString(),
            ["{{FONT_SETUP}}"] = galleryTypography ? ProductFontSetup : "",
            ["{{FONT_FAMILY}}"] = galleryTypography ? "ChartFontStacks.ProductSans" : "null"
        });
    }
}
