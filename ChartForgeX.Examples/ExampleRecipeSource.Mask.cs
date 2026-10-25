/// <summary>Packages the same complete native-mask program used by compiled Core-only snippets.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits an ordinary Core-only program, plus a separately labeled opt-in source filter import.</summary>
    public static string EmitNativeMask(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeMaskExample), "RecipeMaskProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString()
        }, "ChartForgeX.Examples.NativeMaskExample.cs");
    }
}
