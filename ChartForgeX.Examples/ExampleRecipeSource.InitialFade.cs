using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits an ordinary HTML-adapter consumer of the same compiled native chart recipe and initial presentation owner.</summary>
    public static string EmitInitialFade(ChartSeriesKind kind, int width, VisualThemeMode mode) {
        if (kind is not (ChartSeriesKind.Line or ChartSeriesKind.Pie or ChartSeriesKind.Gauge or ChartSeriesKind.Sunburst)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        return Package(ExampleRecipes.ForKind(kind), "RecipeInitialFadeProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FACTORY}}"] = ExampleRecipes.ForKind(kind).Factory, ["{{VARIANT}}"] = width == 390 ? "\"compact\"" : "\"wide\"",
            ["{{MODE}}"] = mode.ToString(), ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture)
        });
    }
}
