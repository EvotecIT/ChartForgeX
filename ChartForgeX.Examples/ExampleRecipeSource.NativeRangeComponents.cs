using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a complete public typed interval program with captured native Values or an explicit InitialValues baseline.</summary>
    public static string EmitNativeRangeComponents(bool initial, bool area, int width, VisualThemeMode mode) {
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeRangeComponentRecipe), "RecipeNativeRangeComponentProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{INITIAL}}"] = initial ? "true" : "false", ["{{AREA}}"] = area ? "true" : "false", ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
