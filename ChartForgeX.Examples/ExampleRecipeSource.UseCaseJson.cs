using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a complete source-backed program using the same canonical source as the displayed example.</summary>
    public static string EmitUseCaseJson(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant));
        return Package(nameof(MermaidUseCaseJsonSourceExample), "RecipeUseCaseJsonProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{SOURCE_TEXT}}"] = JsonSerializer.Serialize(MermaidFixtureSources.Read("usecase-json-table-content")),
            ["{{VARIANT}}"] = JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString()
        }, "ChartForgeX.Examples.MermaidUseCaseJsonSourceExample.cs");
    }
}
