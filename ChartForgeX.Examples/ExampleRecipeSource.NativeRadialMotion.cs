using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a standalone Core + HTML adapter program using captured native radial frames.</summary>
    public static string EmitNativeRadialMotion(bool donut, string kind, int width, VisualThemeMode mode) {
        if (kind is not ("values" or "initial" or "other")) throw new ArgumentOutOfRangeException(nameof(kind));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeRadialMotionRecipe), "RecipeNativeRadialMotionProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{DONUT}}"] = donut ? "true" : "false", ["{{KIND}}"] = JsonSerializer.Serialize(kind), ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
