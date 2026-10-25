using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a standalone consumer using the exact compiled finite asset recipe.</summary>
    public static string EmitFlowchartAssets(string variant,VisualThemeMode mode) {
        if(variant is not ("compact" or "wide"))throw new ArgumentException("Unknown asset recipe size.",nameof(variant));
        var recipe=ReadResource("ChartForgeX.Examples.MermaidRecipes.FlowchartAssetRecipe.cs");var split=recipe.IndexOf("/// <summary>",StringComparison.Ordinal);
        return "using System;\nusing System.IO;\nusing System.Collections.Generic;\nusing ChartForgeX.Mermaid;\nusing ChartForgeX.Raster;\nusing ChartForgeX.Themes;\nusing ChartForgeX.VisualArtifacts;\n"
            +"var artifact=FlowchartAssetRecipe.Create(\""+variant+"\",VisualThemeMode."+mode+");\nDirectory.CreateDirectory(\"output\");File.WriteAllText(\"output/workflow.svg\",artifact.ToSvg());File.WriteAllBytes(\"output/workflow.png\",artifact.ToPng());File.WriteAllText(\"output/workflow.html\",artifact.ToHtmlPage());File.WriteAllText(\"output/workflow.json\",artifact.ToInterchangeJson());\n"+recipe.Substring(split);
    }
}
