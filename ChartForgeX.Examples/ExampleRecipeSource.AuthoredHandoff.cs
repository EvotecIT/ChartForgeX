using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Packages the authored route, badges and label surface as a complete Core-only program.</summary>
    public static string EmitAuthoredHandoff(ThemeMode mode = ThemeMode.Light) {
        if (!Enum.IsDefined(typeof(ThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var source = ReadResource("ChartForgeX.Examples.Recipes.TopologyAuthoredPathBadgeRecipe.cs");
        return Template("RecipeAuthoredHandoffProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = source.Substring(source.IndexOf("/// <summary>", StringComparison.Ordinal)).TrimEnd()
        });
    }
}
