/// <summary>Packages the adapter-owned source program independently of Core-only native recipes.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits a complete public parse, export, save/load and edit program.</summary>
    public static string EmitSequencePresetAppearance(string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));var source=ReadResource("ChartForgeX.Examples.MermaidRecipes.SequencePresetAppearanceSourceRecipe.cs");
        return Template("RecipeSequencePresetAppearanceProgram",new Dictionary<string,string>(StringComparer.Ordinal){["{{SOURCE_TEXT}}"] = System.Text.Json.JsonSerializer.Serialize(MermaidFixtureSources.Read("sequence-preset-appearance")),["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant),["{{MODE}}"] = mode.ToString(),["{{SOURCE}}"] = source.Substring(source.IndexOf("/// <summary>",StringComparison.Ordinal))});
    }
}
