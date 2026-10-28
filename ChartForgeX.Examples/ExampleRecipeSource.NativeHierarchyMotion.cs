using System.Text.Json;
using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits a standalone Core + HTML adapter hierarchy program; the caller supplies its licensed font file.</summary>
    public static string EmitNativeHierarchyMotion(string family, string intent, int width, VisualThemeMode mode) {
        if (family is not ("treemap" or "sunburst-leaf" or "sunburst-total")) throw new ArgumentOutOfRangeException(nameof(family));
        if (intent is not ("values" or "initial" or "subtree" or "root" or "ellipsis")) throw new ArgumentOutOfRangeException(nameof(intent));
        if (intent == "ellipsis" && family != "treemap") throw new ArgumentException("Ellipsis is a Treemap caption policy.", nameof(intent));
        if (width is not (390 or 760)) throw new ArgumentOutOfRangeException(nameof(width));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Package(nameof(NativeHierarchyMotionRecipe), "RecipeNativeHierarchyMotionProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FAMILY}}"] = JsonSerializer.Serialize(family), ["{{INTENT}}"] = JsonSerializer.Serialize(intent), ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{MODE}}"] = mode.ToString()
        });
    }
}
