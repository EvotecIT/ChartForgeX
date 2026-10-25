using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a standalone consumer program using the same compiled source recipe as the gallery.</summary>
    public static string EmitFlowchartCurves(string family, string variant, VisualThemeMode mode) {
        if (family is not ("flowchart" or "swimlane") || variant is not ("compact" or "wide")) throw new ArgumentException("Unknown curve recipe.");
        var recipe = ReadResource("ChartForgeX.Examples.MermaidRecipes.FlowchartCurveRecipe.cs");
        var split = recipe.IndexOf("/// <summary>", StringComparison.Ordinal);
        return "using System;\nusing System.IO;\nusing ChartForgeX.Mermaid;\nusing ChartForgeX.Themes;\nusing ChartForgeX.VisualArtifacts;\n"
            + "var artifact=FlowchartCurveRecipe.Create(\"" + family + "\",\"" + variant + "\",VisualThemeMode." + mode + ");\n"
            + "Directory.CreateDirectory(\"output\");File.WriteAllText(\"output/workflow.svg\",artifact.ToSvg());File.WriteAllBytes(\"output/workflow.png\",artifact.ToPng());File.WriteAllText(\"output/workflow.html\",artifact.ToHtmlPage());File.WriteAllText(\"output/workflow.json\",artifact.ToInterchangeJson());\n"
            + recipe.Substring(split);
    }
}
