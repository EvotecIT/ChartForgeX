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
            ["{{FONT_SETUP}}"] = galleryTypography ? "// Optional exact gallery typography; copy the gallery fonts directory beside this program.\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Bold.ttf\", 700);\n" : "",
            ["{{FONT_FAMILY}}"] = galleryTypography ? JsonSerializer.Serialize("CFX Proof Carlito") : "null"
        });
    }
}
