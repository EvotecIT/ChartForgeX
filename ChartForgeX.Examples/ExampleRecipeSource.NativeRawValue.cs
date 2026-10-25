using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a standalone Core + HTML adapter raw-value or source-Reflow program.</summary>
    public static string EmitNativeRawValue(bool donut,string kind,int width,VisualThemeMode mode) {
        if(kind is not ("zero" or "empty" or "other" or "initial" or "source-reflow"))throw new ArgumentOutOfRangeException(nameof(kind));
        if(width is not (390 or 760))throw new ArgumentOutOfRangeException(nameof(width));if(!Enum.IsDefined(typeof(VisualThemeMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeRawValueRecipe),"RecipeNativeRawValueProgram",new Dictionary<string,string>(StringComparer.Ordinal){["{{DONUT}}"] =donut?"true":"false",["{{KIND}}"] =JsonSerializer.Serialize(kind),["{{WIDTH}}"] =width.ToString(CultureInfo.InvariantCulture),["{{MODE}}"] =mode.ToString()});
    }
}
