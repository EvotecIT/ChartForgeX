using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits public native uncertainty Values, InitialValues or changed-point flash without changing the Core recipe boundary.</summary>
    public static string EmitNativeErrorComponents(string form, HtmlNativeTransitionEffect effect, int width, VisualThemeMode mode) {
        if (form is not ("x" or "y" or "xy")) throw new ArgumentOutOfRangeException(nameof(form));
        if (effect is not (HtmlNativeTransitionEffect.Values or HtmlNativeTransitionEffect.InitialValues or HtmlNativeTransitionEffect.ValueFlash)) throw new ArgumentOutOfRangeException(nameof(effect));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width)); if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeErrorComponentRecipe), "RecipeNativeErrorComponentProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FORM}}"] = form, ["{{EFFECT}}"] = effect.ToString(), ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
