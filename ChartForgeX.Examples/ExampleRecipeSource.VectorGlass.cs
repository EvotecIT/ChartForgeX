/// <summary>Complete optional Visuals program for a filtered background with retained vector captions.</summary>
public static partial class ExampleRecipeSource {
    public static string EmitNativeVectorGlass(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        ValidateAppearanceSelection("outline", variant, mode);
        return Template("RecipeNativeVectorGlassProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.NativeVectorGlassExample.cs").TrimEnd()
        });
    }
}
