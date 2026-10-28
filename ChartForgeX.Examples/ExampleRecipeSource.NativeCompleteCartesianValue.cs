using System.Text.Json;
using ChartForgeX.Themes;
public static partial class ExampleRecipeSource {
    /// <summary>Emits actual native complete Cartesian values with an explicit caller-owned font.</summary>
    public static string EmitNativeCompleteCartesianValue(string variant,int width,VisualThemeMode mode) {
        if(!NativeCompleteCartesianValueRecipe.Variants.Contains(variant,StringComparer.Ordinal))throw new ArgumentOutOfRangeException(nameof(variant));
        if(width is not (390 or 760))throw new ArgumentOutOfRangeException(nameof(width));if(!Enum.IsDefined(typeof(VisualThemeMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeCompleteCartesianValueRecipe),"RecipeNativeCompleteCartesianValueProgram",new Dictionary<string,string>(StringComparer.Ordinal){["{{KIND}}"] =JsonSerializer.Serialize(variant),["{{WIDTH}}"] =width.ToString(CultureInfo.InvariantCulture),["{{MODE}}"] =mode.ToString()});
    }
}
