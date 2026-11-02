using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Packages the compiled support-row factory with an actual plot-strip and matching-axis export program.</summary>
    public static string EmitServerRenderedStrip(string variant, VisualThemeMode mode, int width, int height, bool galleryTypography = false) {
        if (variant is not ("shared-strip" or "compact-shared-strip")) throw new ArgumentException("Unknown shared-strip variant.", nameof(variant));
        return Package(nameof(ServerRenderedRecipe), "ServerRenderedStripProgram", new Dictionary<string, string> {
            ["{{MODE}}"] = mode.ToString(), ["{{VARIANT}}"] = System.Text.Json.JsonSerializer.Serialize(variant),
            ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture), ["{{HEIGHT}}"] = height.ToString(CultureInfo.InvariantCulture),
            ["{{FONT_SETUP}}"] = galleryTypography
                ? "FontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Bold.ttf\", 700);\n" : "",
            ["{{FONT}}"] = galleryTypography ? "CFX Proof Carlito" : "system-ui, sans-serif"
        });
    }
}
