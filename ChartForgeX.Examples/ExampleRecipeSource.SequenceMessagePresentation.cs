/// <summary>Packages a complete adapter-owned sequence message source program without expanding Core-only snippets.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits ordinary save/load/edit code sharing the canonical source and native preparation owner.</summary>
    public static string EmitSequenceMessagePresentation(string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        var source=ReadResource("ChartForgeX.Examples.MermaidRecipes.SequenceMessagePresentationSourceRecipe.cs");
        return Template("RecipeSequenceMessagePresentationProgram",new Dictionary<string,string>(StringComparer.Ordinal){
            ["{{SOURCE_TEXT}}"] = System.Text.Json.JsonSerializer.Serialize(MermaidFixtureSources.Read("sequence-message-presentation")),
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant),["{{MODE}}"] = mode.ToString(),["{{SOURCE}}"] = source.Substring(source.IndexOf("/// <summary>",StringComparison.Ordinal))});
    }
}
