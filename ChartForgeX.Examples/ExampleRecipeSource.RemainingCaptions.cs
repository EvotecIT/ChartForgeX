using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Packages the same compiled source recipe as a complete ordinary Mermaid-package program.</summary>
    public static string EmitRemainingCaptionRoles(string family, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        _ = MermaidRemainingCaptionRolesExample.Source(family);
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        return Package(nameof(MermaidRemainingCaptionRolesExample), "RecipeRemainingCaptionRolesProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FAMILY}}"] = JsonSerializer.Serialize(family), ["{{VARIANT}}"] = JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString()
        }, "ChartForgeX.Examples.MermaidRemainingCaptionRolesExample.cs");
    }
}
