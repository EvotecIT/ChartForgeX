/// <summary>Packages optional-Visuals composition without entering the Core-only recipe compile boundary.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits one ordinary Core + Visuals program with both complete shared consumer types.</summary>
    public static string EmitNativeMaskComposition(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Template("RecipeMaskCompositionProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.NativeMaskExample.cs").TrimEnd() + "\n\n"
                + ReadResource("ChartForgeX.Examples.NativeMaskBlendCompositionExample.cs").TrimEnd()
        });
    }
}
