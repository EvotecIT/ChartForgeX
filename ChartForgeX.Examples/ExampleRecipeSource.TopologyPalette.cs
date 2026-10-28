using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a normal source consumer through the same compiled palette recipe.</summary>
    public static string EmitTopologySourcePalette(string family, string variant, VisualThemeMode mode) {
        if (family is not ("flowchart" or "swimlane" or "agentflow") || variant is not ("compact" or "wide")) throw new ArgumentException("Unknown palette recipe.");
        var recipe = ReadResource("ChartForgeX.Examples.MermaidRecipes.TopologySourcePaletteRecipe.cs");
        var split = recipe.IndexOf("/// <summary>", StringComparison.Ordinal);
        return "using System;\nusing System.IO;\nusing ChartForgeX.Mermaid;\nusing ChartForgeX.Themes;\nusing ChartForgeX.VisualArtifacts;\n"
            + "var artifact=TopologySourcePaletteRecipe.Create(\"" + family + "\",\"" + variant + "\",VisualThemeMode." + mode + ");\n"
            + "Directory.CreateDirectory(\"output\");File.WriteAllText(\"output/workflow.svg\",artifact.ToSvg());File.WriteAllBytes(\"output/workflow.png\",artifact.ToPng());File.WriteAllText(\"output/workflow.html\",artifact.ToHtmlPage());File.WriteAllText(\"output/workflow.json\",artifact.ToInterchangeJson());\n"
            + recipe.Substring(split);
    }
}
