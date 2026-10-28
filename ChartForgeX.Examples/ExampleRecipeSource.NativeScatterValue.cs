using ChartForgeX.Themes;
public static partial class ExampleRecipeSource {
    /// <summary>Emits native Scatter values or an explicit initial native baseline through the existing source recipe.</summary>
    public static string EmitNativeScatterValue(bool initial,int width,VisualThemeMode mode){if(width is not (390 or 760))throw new ArgumentOutOfRangeException(nameof(width));if(!Enum.IsDefined(typeof(VisualThemeMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));return Package(nameof(NativeOwnedValueFlashRecipe),"RecipeNativeScatterValueProgram",new Dictionary<string,string>(StringComparer.Ordinal){["{{INITIAL}}"] =initial?"true":"false",["{{WIDTH}}"] =width.ToString(CultureInfo.InvariantCulture),["{{MODE}}"] =mode.ToString()});}
}
