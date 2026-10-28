using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a normal Core + HTML adapter program with explicit host fonts and captured native numeric/case policy.</summary>
    public static string EmitNativeNumberCultureCase(string kind, string policy, int width, VisualThemeMode mode) {
        if (kind is not ("pie" or "donut" or "treemap")) throw new ArgumentOutOfRangeException(nameof(kind));
        _ = NativeNumberCultureCaseRecipe.Policy(policy);
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeNumberCultureCaseRecipe), "RecipeNativeNumberCultureCaseProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{KIND}}"] = JsonSerializer.Serialize(kind), ["{{POLICY}}"] = JsonSerializer.Serialize(policy), ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
