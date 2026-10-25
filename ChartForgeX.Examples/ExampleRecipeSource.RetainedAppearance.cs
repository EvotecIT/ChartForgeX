/// <summary>Emits the same complete Core-only retained/terminal source workflow used by native examples.</summary>
public static partial class ExampleRecipeSource {
    public static string EmitNativeRetainedAppearance(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "compact" && variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        if (mode != VisualThemeMode.Light && mode != VisualThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
        return Template("RecipeNativeRetainedAppearanceProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.NativeRetainedAppearanceExample.cs").TrimEnd()
        });
    }
}
