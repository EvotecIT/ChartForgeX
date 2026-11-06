using System.Text.Json;

public static partial class ExampleRecipeSource {
    /// <summary>Packages the canonical actor source as an ordinary Mermaid-package program; Core native authoring stays separate.</summary>
    public static string EmitAdvancedActorSource(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, bool galleryTypography = false) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        var source = ReadResource("ChartForgeX.Examples.MermaidFixtures.usecase-advanced-actors.mmd");
        if (variant == "compact") source = source.Replace("direction TB", "direction LR", StringComparison.Ordinal);
        return Template("RecipeAdvancedActorSourceProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{SOURCE_TEXT}}"] = JsonSerializer.Serialize(source), ["{{WIDTH}}"] = variant == "compact" ? "390" : "1100",
            ["{{HEIGHT}}"] = variant == "compact" ? "1160" : "680", ["{{MODE}}"] = mode.ToString(),
            ["{{FONT_SETUP}}"] = galleryTypography ? ProductFontSetup : "",
            ["{{FONT_FAMILY}}"] = galleryTypography ? "ChartFontStacks.ProductSans" : "null"
        });
    }
}
