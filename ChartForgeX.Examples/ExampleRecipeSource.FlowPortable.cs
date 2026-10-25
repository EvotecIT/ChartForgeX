using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits the compiled Core-only flow recipe and its editable portable round trip.</summary>
    public static string EmitFlowPortablePresentation(string variant,VisualThemeMode mode) {
        if(variant is not ("compact" or "wide"))throw new ArgumentException("Unknown flow recipe size.",nameof(variant));
        var recipe=ReadResource("ChartForgeX.Examples.Recipes.FlowPortablePresentationRecipe.cs");var split=recipe.IndexOf("/// <summary>",StringComparison.Ordinal);
        return "using System;\nusing System.IO;\nusing System.Linq;\nusing ChartForgeX.Primitives;\nusing ChartForgeX.Rendering;\nusing ChartForgeX.Themes;\nusing ChartForgeX.Typography;\nusing ChartForgeX.VisualArtifacts;\nusing ChartForgeX.VisualBlocks;\n"
            +"var artifact=FlowPortablePresentationRecipe.Create(\""+variant+"\",VisualThemeMode."+mode+");\nvar json=artifact.ToInterchangeJson();\nvar restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToFlowArtifact();\n"
            +"if(!artifact.ToPng().SequenceEqual(restored.ToPng()))throw new InvalidOperationException(\"The native portable flow pixels must match.\");\nDirectory.CreateDirectory(\"output\");File.WriteAllText(\"output/flow.svg\",artifact.ToSvg());File.WriteAllBytes(\"output/flow.png\",artifact.ToPng());File.WriteAllText(\"output/flow.html\",artifact.ToHtmlPage());File.WriteAllText(\"output/flow.json\",json);\n"
            +recipe.Substring(split);
    }
}
