using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a normal Core + HTML adapter program for one finite native value edit or explicit initial value baseline.</summary>
    public static string EmitNativeCartesianValues(bool bar, int width, VisualThemeMode mode, bool initial = false) {
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        return Package(nameof(NativeCartesianValueRecipe), "RecipeNativeCartesianValueProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{BAR}}"] = bar ? "true" : "false", ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture),
            ["{{MODE}}"] = mode.ToString(), ["{{INITIAL}}"] = initial ? "true" : "false"
        });
    }
}
