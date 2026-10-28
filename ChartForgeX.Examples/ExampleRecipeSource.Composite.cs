/// <summary>Packages explicit Core source and optional-Visuals composition through the incumbent source owner.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits one complete Core-only source import program.</summary>
    public static string EmitNativeCompositeSource(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        ValidateCompositeSelection(variant, mode);
        return Package(nameof(NativeCompositeSourceExample), "RecipeCompositeSourceProgram",
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString()
            }, "ChartForgeX.Examples.NativeCompositeSourceExample.cs");
    }

    private static void ValidateCompositeSelection(string variant, VisualThemeMode mode) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
    }
}
