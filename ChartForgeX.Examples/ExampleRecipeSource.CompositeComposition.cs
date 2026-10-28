/// <summary>Packages the optional Visuals facade without expanding Core-only Snippets references.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits a complete Core+Visuals program; this source stays outside Core-only recipe globs.</summary>
    public static string EmitNativeCompositeComposition(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        ValidateCompositeSelection(variant, mode);
        return Template("RecipeCompositeCompositionProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.NativeRetainedMaskExample.cs").TrimEnd() + "\n\n"
                + ReadResource("ChartForgeX.Examples.NativeCompositeCompositionExample.cs").TrimEnd()
        });
    }

}
