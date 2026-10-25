/// <summary>Packages the ordinary adapter-owned source consumer separately from native Core recipes.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits complete parse, native/baked export, portable save/load and edit code.</summary>
    public static string EmitSequenceSurfaceShadows(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var source = ReadResource("ChartForgeX.Examples.MermaidRecipes.SequenceSurfaceShadowSourceRecipe.cs");
        return Template("RecipeSequenceSurfaceShadowProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{SOURCE_TEXT}}"] = System.Text.Json.JsonSerializer.Serialize(MermaidFixtureSources.Read("sequence-retained-surface-shadows")),
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant),
            ["{{MODE}}"] = mode.ToString(),
            ["{{SOURCE}}"] = source.Substring(source.IndexOf("/// <summary>", StringComparison.Ordinal))
        });
    }
}
