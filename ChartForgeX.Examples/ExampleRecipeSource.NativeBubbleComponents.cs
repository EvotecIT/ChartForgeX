using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits an executable typed native component example with explicit caller font ownership.</summary>
    public static string EmitNativeBubbleComponents(bool initial, bool area, bool reversed, int width, VisualThemeMode mode) {
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeBubbleComponentRecipe), "RecipeNativeBubbleComponentProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{INITIAL}}"] = initial ? "true" : "false", ["{{AREA}}"] = area ? "true" : "false", ["{{REVERSED}}"] = reversed ? "true" : "false",
            ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
