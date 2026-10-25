using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits an ordinary Core + HTML adapter program using captured numeric text and explicitly supplied licensed fonts.</summary>
    public static string EmitNativeValueTextRepertoire(bool donut, int width, VisualThemeMode mode) {
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeRadialMotionRecipe), "RecipeNativeValueTextRepertoireProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{DONUT}}"] = donut ? "true" : "false", ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
