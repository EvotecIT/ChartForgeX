/// <summary>Complete Core and optional Visuals programs use the same source types as the displayed examples.</summary>
public static partial class ExampleRecipeSource {
    public static string EmitNativeAppearance(string effect = "bloom", string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        ValidateAppearanceSelection(effect, variant, mode);
        return Template("RecipeNativeAppearanceProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{EFFECT}}"] = System.Text.Json.JsonSerializer.Serialize(effect),
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.NativeAppearanceExample.cs").TrimEnd()
        });
    }
    private static void ValidateAppearanceSelection(string effect, string variant, VisualThemeMode mode) {
        if (effect != "outline" && effect != "bloom" && effect != "glow" && effect != "inner") throw new ArgumentOutOfRangeException(nameof(effect));
        if (variant != "compact" && variant != "wide") throw new ArgumentOutOfRangeException(nameof(variant));
        if (mode != VisualThemeMode.Light && mode != VisualThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
    }
}
