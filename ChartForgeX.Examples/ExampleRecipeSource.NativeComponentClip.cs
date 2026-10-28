using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits full raw native component values through the existing managed transition and native clip owner.</summary>
    public static string EmitNativeComponentClip(string family, int width, VisualThemeMode mode) {
        if (family is not ("bubble" or "range" or "error" or "candlestick" or "ohlc")) throw new ArgumentOutOfRangeException(nameof(family));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width)); if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeComponentClipRecipe), "RecipeNativeComponentClipProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FAMILY}}"] = family, ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
