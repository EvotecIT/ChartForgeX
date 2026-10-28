using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits an ordinary Core + HTML adapter program using native typed visibility and explicit Reflow.</summary>
    public static string EmitNativeReflow(string kind, int width, VisualThemeMode mode) {
        if (kind is not ("line" or "bar" or "boxes" or "raw")) throw new ArgumentOutOfRangeException(nameof(kind));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeReflowRecipe), "RecipeNativeReflowProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{KIND}}"] = JsonSerializer.Serialize(kind), ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
