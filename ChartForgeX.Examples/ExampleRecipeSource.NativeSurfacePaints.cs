using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Exports native contour paints or one finite source contour-style program.</summary>
    public static string EmitNativeSurfacePaints(string family,string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(family is not ("flowchart" or "agentflow" or "native"))throw new ArgumentOutOfRangeException(nameof(family));
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        return Template("RecipeNativeSurfacePaintProgram",new Dictionary<string,string>(StringComparer.Ordinal){
            ["{{SOURCE_TEXT}}"] = family=="native"?"null":JsonSerializer.Serialize(ReadResource("ChartForgeX.Examples.MermaidFixtures."+family+"-native-surface-paints.mmd")),
            ["{{WIDTH}}"] = variant=="compact"?"390":"1100",["{{MODE}}"] = mode.ToString()
        });
    }
}
