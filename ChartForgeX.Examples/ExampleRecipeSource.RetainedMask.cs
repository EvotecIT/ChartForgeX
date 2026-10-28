/// <summary>Packages one Core-only retained-mask example without an Examples assembly dependency.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits native/baked SVG, native PNG, typed mask JSON and an ordinary source-edit/replay workflow.</summary>
    public static string EmitRetainedMask(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeRetainedMaskExample), "RecipeRetainedMaskProgram",
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant),
                ["{{MODE}}"] = mode.ToString()
            }, "ChartForgeX.Examples.NativeRetainedMaskExample.cs");
    }
}
