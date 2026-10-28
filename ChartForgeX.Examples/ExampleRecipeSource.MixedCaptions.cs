using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Packages the compiled editable native/source caption recipe as a complete ordinary-package program.</summary>
    public static string EmitMixedCaptions(string family = "native", string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (family is not ("native" or "flowchart" or "requirement" or "mindmap")) throw new ArgumentOutOfRangeException(nameof(family));
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FAMILY}}"] = JsonSerializer.Serialize(family), ["{{VARIANT}}"] = JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString()
        };
        return family == "native"
            ? Package(nameof(MixedCaptionRecipe), "RecipeMixedCaptionProgram", tokens)
            : Package(nameof(MermaidMixedCaptionSourceExample), "RecipeMixedCaptionSourceProgram", tokens,
                "ChartForgeX.Examples.MermaidMixedCaptionSourceExample.cs");
    }
}
