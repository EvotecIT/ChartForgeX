using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a standalone native flash program requiring an explicitly supplied font file.</summary>
    public static string EmitNativeOwnedValueFlash(string kind,int width,VisualThemeMode mode) {
        if(kind is not ("line" or "bar-gradient" or "bar-capsule" or "pie" or "donut" or "raw-pie" or "raw-donut" or "treemap" or "sunburst"))throw new ArgumentOutOfRangeException(nameof(kind));
        if(width is not (390 or 760))throw new ArgumentOutOfRangeException(nameof(width));if(!Enum.IsDefined(typeof(VisualThemeMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeOwnedValueFlashRecipe),"RecipeNativeOwnedValueFlashProgram",new Dictionary<string,string>(StringComparer.Ordinal){["{{KIND}}"] =JsonSerializer.Serialize(kind),["{{WIDTH}}"] =width.ToString(CultureInfo.InvariantCulture),["{{MODE}}"] =mode.ToString()});
    }
}
