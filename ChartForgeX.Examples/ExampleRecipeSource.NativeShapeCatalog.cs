using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Exports one maintained source catalog as a complete ordinary Mermaid-package program.</summary>
    public static string EmitNativeShapeCatalog(string suffix,string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(suffix is not ("a" or "b" or "c" or "d"))throw new ArgumentOutOfRangeException(nameof(suffix));
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        return Template("RecipeNativeShapeCatalogProgram",new Dictionary<string,string>(StringComparer.Ordinal) {
            ["{{SOURCE_TEXT}}"] = JsonSerializer.Serialize(ReadResource("ChartForgeX.Examples.MermaidFixtures.flowchart-native-shape-catalog-"+suffix+".mmd")),
            ["{{WIDTH}}"] = variant=="compact"?"390":"1100", ["{{MODE}}"] = mode.ToString()
        });
    }
}
