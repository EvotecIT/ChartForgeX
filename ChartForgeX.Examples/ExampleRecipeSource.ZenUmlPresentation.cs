using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a complete public consumer program using the same compiled native recipe as the gallery.</summary>
    public static string EmitZenUmlPresentation(string family,string variant,VisualThemeMode mode) {
        if(family is not ("presentation" or "order")||variant is not ("compact" or "wide"))throw new ArgumentException("Unknown finite Zen recipe.");
        var recipe=ReadResource("ChartForgeX.Examples.MermaidRecipes.ZenUmlPresentationRecipe.cs");
        var split=recipe.IndexOf("/// <summary>",StringComparison.Ordinal);
        return "using System;\nusing System.IO;\nusing ChartForgeX.Mermaid;\nusing ChartForgeX.Themes;\nusing ChartForgeX.VisualArtifacts;\n"
            +"var artifact=ZenUmlPresentationRecipe.Create(\""+family+"\",\""+variant+"\",VisualThemeMode."+mode+");\n"
            +"Directory.CreateDirectory(\"output\");File.WriteAllText(\"output/sequence.svg\",artifact.ToSvg());File.WriteAllBytes(\"output/sequence.png\",artifact.ToPng());File.WriteAllText(\"output/sequence.html\",artifact.ToHtmlPage());File.WriteAllText(\"output/sequence.json\",artifact.ToInterchangeJson());\n"
            +recipe.Substring(split);
    }
}
