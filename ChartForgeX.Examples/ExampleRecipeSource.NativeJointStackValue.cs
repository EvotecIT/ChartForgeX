using System.Text.Json;
using ChartForgeX.Themes;
public static partial class ExampleRecipeSource {
    /// <summary>Emits a native full-source stack joint presentation; shared totals remain global Preserve context.</summary>
    public static string EmitNativeJointStackValue(string variant,int width,VisualThemeMode mode) {
        if(variant is not ("stacked-bar" or "default-stacked" or "normalized-bar"))throw new ArgumentOutOfRangeException(nameof(variant));if(width is not (390 or 760))throw new ArgumentOutOfRangeException(nameof(width));if(!Enum.IsDefined(typeof(VisualThemeMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeCompleteCartesianValueRecipe),"RecipeNativeJointStackValueProgram",new Dictionary<string,string>(StringComparer.Ordinal){["{{KIND}}"] =JsonSerializer.Serialize(variant),["{{WIDTH}}"] =width.ToString(CultureInfo.InvariantCulture),["{{MODE}}"] =mode.ToString()});
    }
}
