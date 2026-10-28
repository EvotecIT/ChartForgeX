using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a complete public program from the same compiled native recipe.</summary>
    public static string EmitRequirementTheme(string variant, VisualThemeMode mode) {
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var recipe = ReadResource("ChartForgeX.Examples.MermaidRecipes.RequirementThemeRecipe.cs");
        var split = recipe.IndexOf("/// <summary>", StringComparison.Ordinal);
        return "using System;\nusing System.IO;\nusing ChartForgeX.Mermaid;\nusing ChartForgeX.Themes;\nusing ChartForgeX.VisualArtifacts;\n"
            + "var artifact=RequirementThemeRecipe.Create(\"" + variant + "\",VisualThemeMode." + mode + ");\n"
            + "Directory.CreateDirectory(\"output\");File.WriteAllText(\"output/requirement.svg\",artifact.ToSvg());File.WriteAllBytes(\"output/requirement.png\",artifact.ToPng());File.WriteAllText(\"output/requirement.html\",artifact.ToHtmlPage());File.WriteAllText(\"output/requirement.json\",artifact.ToInterchangeJson());\n"
            + recipe.Substring(split);
    }
}
