using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Exports an ordinary source styling and graph settings program.</summary>
    public static string EmitGraphStyleSettings(string family,string variant="wide",VisualThemeMode mode=VisualThemeMode.Light) {
        if(family is not ("flowchart" or "agentflow"))throw new ArgumentOutOfRangeException(nameof(family));
        if(variant is not ("compact" or "wide"))throw new ArgumentOutOfRangeException(nameof(variant));
        return Template("RecipeGraphStyleSettingsProgram",new Dictionary<string,string>(StringComparer.Ordinal){
            ["{{SOURCE_TEXT}}"] = JsonSerializer.Serialize(ReadResource("ChartForgeX.Examples.MermaidFixtures."+family+"-style-settings.mmd")),
            ["{{WIDTH}}"] = variant=="compact"?"390":"1100",["{{MODE}}"] = mode.ToString()
        });
    }
}
