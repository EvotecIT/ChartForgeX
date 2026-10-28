using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a standalone Core and HTML adapter program for captured native radial bands.</summary>
    public static string EmitNativeRadialBands(string family, bool automatic, string intent, int width, VisualThemeMode mode) {
        if (family is not ("pie" or "donut" or "mixed")) throw new ArgumentOutOfRangeException(nameof(family));
        if (intent is not ("values" or "initial" or "other")) throw new ArgumentOutOfRangeException(nameof(intent));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(typeof(VisualThemeMode),mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeRadialBandMotionRecipe),"RecipeNativeRadialBandsProgram",new Dictionary<string,string>(StringComparer.Ordinal) {
            ["{{FAMILY}}"] = JsonSerializer.Serialize(family), ["{{AUTOMATIC}}"] = automatic ? "true" : "false", ["{{INTENT}}"] = JsonSerializer.Serialize(intent),
            ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
