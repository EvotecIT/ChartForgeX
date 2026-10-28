public static partial class ExampleRecipeSource {
    /// <summary>Emits the complete source-aware navigation consumer, outside the Core-only recipe boundary.</summary>
    public static string EmitSequenceNavigation(string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));var source=ReadResource("ChartForgeX.Examples.MermaidRecipes.SequenceNavigationSourceRecipe.cs");
        return Template("RecipeSequenceNavigationProgram",new Dictionary<string,string>(StringComparer.Ordinal){["{{SOURCE_TEXT}}"] = System.Text.Json.JsonSerializer.Serialize(MermaidFixtureSources.Read("sequence-navigation")),["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant),["{{MODE}}"] = mode.ToString(),["{{SOURCE}}"] = source.Substring(source.IndexOf("/// <summary>",StringComparison.Ordinal))});
    }
}
