/// <summary>Optional Visuals composition is emitted outside the Core-only recipe owner.</summary>
public static partial class ExampleRecipeSource {
    public static string EmitNativeGlassComposition(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        ValidateAppearanceSelection("outline", variant, mode);
        return Template("RecipeNativeGlassCompositionProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.NativeAppearanceExample.cs").TrimEnd() + "\n\n"
                + ReadResource("ChartForgeX.Examples.NativeGlassCompositionExample.cs").TrimEnd()
        });
    }
}
