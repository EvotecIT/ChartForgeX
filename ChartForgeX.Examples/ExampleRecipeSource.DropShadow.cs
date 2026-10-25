/// <summary>Packages the shared complete native-shadow consumer through the ordinary source emitter.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Creates a complete Core-only program; filtered SVG pixels remain a separately labeled raw RGBA result.</summary>
    public static string EmitNativeDropShadow(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeDropShadowExample), "RecipeDropShadowProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString()
        }, "ChartForgeX.Examples.NativeDropShadowExample.cs");
    }
}
