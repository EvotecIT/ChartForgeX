/// <summary>Packages the adapter-owned participant-view program separately from native Core recipes.</summary>
public static partial class ExampleRecipeSource {
    /// <summary>Emits a complete ordinary Core/Mermaid save, restore and edit program.</summary>
    public static string EmitSequenceParticipantViews(string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        var source=ReadResource("ChartForgeX.Examples.MermaidRecipes.SequenceParticipantViewsSourceRecipe.cs");
        return Template("RecipeSequenceParticipantViewsProgram",new Dictionary<string,string>(StringComparer.Ordinal){
            ["{{SOURCE_TEXT}}"] = System.Text.Json.JsonSerializer.Serialize(MermaidFixtureSources.Read("sequence-participant-views")),
            ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant),["{{MODE}}"] = mode.ToString(),["{{SOURCE}}"] = source.Substring(source.IndexOf("/// <summary>",StringComparison.Ordinal))});
    }
}
