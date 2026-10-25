/// <summary>Packages the shared adapter-owned sequence source program, independent of Core-only recipes.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits a complete ordinary Mermaid/Core program with one embedded canonical source literal.</summary>
    public static string EmitSequenceLayoutSource(string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        var resource=ReadResource("ChartForgeX.Examples.MermaidRecipes.SequenceLayoutTypographySourceRecipe.cs");
        var beginning=resource.IndexOf("/// <summary>",StringComparison.Ordinal);
        return Template("RecipeSequenceLayoutSourceProgram",new Dictionary<string,string>(StringComparer.Ordinal){
            ["{{SOURCE_TEXT}}"] = System.Text.Json.JsonSerializer.Serialize(MermaidFixtureSources.Read("sequence-layout-typography")),
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant),["{{MODE}}"] = mode.ToString(),["{{SOURCE}}"] = resource.Substring(beginning)
        });
    }
}
