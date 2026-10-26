using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits public native financial Values, InitialValues or changed-point flash without changing the Core recipe boundary.</summary>
    public static string EmitNativeFinancialComponents(bool ohlc, HtmlNativeTransitionEffect effect, int width, VisualThemeMode mode) {
        if (effect is not (HtmlNativeTransitionEffect.Values or HtmlNativeTransitionEffect.InitialValues or HtmlNativeTransitionEffect.ValueFlash)) throw new ArgumentOutOfRangeException(nameof(effect));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width)); if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeFinancialComponentRecipe), "RecipeNativeFinancialComponentProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{OHLC}}"] = ohlc ? "true" : "false", ["{{EFFECT}}"] = effect.ToString(), ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
