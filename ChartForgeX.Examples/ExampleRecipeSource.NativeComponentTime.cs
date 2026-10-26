using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits fixed native time-X component programs through the existing managed transition entry.</summary>
    public static string EmitNativeComponentTime(string family, string policy, bool initial, int width, VisualThemeMode mode) {
        if (family is not ("candlestick" or "ohlc" or "bubble" or "range" or "error")) throw new ArgumentOutOfRangeException(nameof(family));
        if (policy is not ("elapsed" or "sessions" or "ordinal" or "calendar")) throw new ArgumentOutOfRangeException(nameof(policy));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width)); if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeComponentTimeRecipe), "RecipeNativeComponentTimeProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FAMILY}}"] = family, ["{{POLICY}}"] = policy, ["{{EFFECT}}"] = initial ? "InitialValues" : "Values", ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
