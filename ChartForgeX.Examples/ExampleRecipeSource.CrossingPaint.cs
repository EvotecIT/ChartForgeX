using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits the compiled native recipe as a complete ordinary Core-package program.</summary>
    public static string EmitNativeCrossingPaint(string variant, VisualThemeMode mode) {
        if (variant is not ("compact-arc" or "wide-arc" or "compact-gap" or "wide-gap" or "compact-none" or "wide-none")) throw new ArgumentOutOfRangeException(nameof(variant));
        var recipe = ReadResource("ChartForgeX.Examples.Recipes.TopologyCrossingPaintRecipe.cs");
        var split = recipe.IndexOf("/// <summary>", StringComparison.Ordinal);
        return "using System;\nusing System.IO;\nusing ChartForgeX.Primitives;\nusing ChartForgeX.Themes;\nusing ChartForgeX.Topology;\nusing ChartForgeX.VisualArtifacts;\n"
            + "var artifact=TopologyCrossingPaintRecipe.Create(\"" + variant + "\",VisualThemeMode." + mode + ").ToVisualArtifact();\n"
            + "Directory.CreateDirectory(\"output\");File.WriteAllText(\"output/topology.svg\",artifact.ToSvg());File.WriteAllBytes(\"output/topology.png\",artifact.ToPng());File.WriteAllText(\"output/topology.html\",artifact.ToHtmlPage());File.WriteAllText(\"output/topology.json\",artifact.ToInterchangeJson());\n"
            + recipe.Substring(split);
    }

    /// <summary>Emits the same compiled source recipe as an ordinary Mermaid-package program.</summary>
    public static string EmitSwimlaneCrossingPaint(string form, string variant, VisualThemeMode mode) {
        _ = SwimlaneCrossingPaintRecipe.Source(form);
        if (variant is not ("compact" or "wide")) throw new ArgumentOutOfRangeException(nameof(variant));
        var recipe = ReadResource("ChartForgeX.Examples.MermaidRecipes.SwimlaneCrossingPaintRecipe.cs");
        var split = recipe.IndexOf("/// <summary>", StringComparison.Ordinal);
        return "using System;\nusing System.IO;\nusing ChartForgeX.Mermaid;\nusing ChartForgeX.Themes;\nusing ChartForgeX.VisualArtifacts;\n"
            + "var artifact=SwimlaneCrossingPaintRecipe.Create(\"" + form + "\",\"" + variant + "\",VisualThemeMode." + mode + ");\n"
            + "Directory.CreateDirectory(\"output\");File.WriteAllText(\"output/swimlane.svg\",artifact.ToSvg());File.WriteAllBytes(\"output/swimlane.png\",artifact.ToPng());File.WriteAllText(\"output/swimlane.html\",artifact.ToHtmlPage());File.WriteAllText(\"output/swimlane.json\",artifact.ToInterchangeJson());\n"
            + recipe.Substring(split);
    }
}
