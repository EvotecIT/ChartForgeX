/// <summary>Emits the same complete Core-only retained/terminal source workflow used by native examples.</summary>
public static partial class ExampleRecipeSource {
    public static string EmitNativeRetainedAppearance(string variant = "wide", ThemeMode mode = ThemeMode.Light) {
        if (variant != "compact" && variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        if (mode != ThemeMode.Light && mode != ThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
        return Template("RecipeNativeRetainedAppearanceProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.NativeRetainedAppearanceExample.cs").TrimEnd()
        });
    }
}
