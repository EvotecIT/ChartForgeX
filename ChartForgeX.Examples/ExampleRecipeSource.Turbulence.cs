public static partial class ExampleRecipeSource {
    /// <summary>Emits the same complete Core-only procedural source used by the native example.</summary>
    public static string EmitNativeTurbulence(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant != "wide" && variant != "compact") throw new ArgumentOutOfRangeException(nameof(variant));
        if (mode != VisualThemeMode.Light && mode != VisualThemeMode.Dark) throw new ArgumentOutOfRangeException(nameof(mode));
        return Template("RecipeNativeTurbulenceProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.NativeTurbulenceExample.cs").TrimEnd()
        });
    }
}
