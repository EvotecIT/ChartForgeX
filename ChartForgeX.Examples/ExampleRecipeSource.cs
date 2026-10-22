using System.Text.RegularExpressions;
using System.Text.Json;

/// <summary>Packages one compiled native recipe as a standalone ordinary-package C# program.</summary>
public static class ExampleRecipeSource {
    /// <summary>Creates a standalone program with the selected viewport, frame, palette mode and optional gallery typography.</summary>
    public static string Emit(ExampleRecipe recipe, string variant, string title, string subtitle, VisualThemeMode mode,
        int width, int height, bool? legend = null, string artifactKind = "Chart", bool surface = false,
        bool viewport = false, bool galleryTypography = false) {
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FONT_SETUP}}"] = galleryTypography
                ? "// Optional exact gallery typography; copy the gallery's fonts directory beside this program.\n" +
                    "FontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Regular.ttf\", 400);\n" +
                    "FontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Bold.ttf\", 700);\n" : "",
            ["{{FONT}}"] = galleryTypography ? ", FontSpec.FromFamily(\"CFX Proof Carlito\")" : "",
            ["{{MODE}}"] = mode.ToString(),
            ["{{FACTORY}}"] = recipe.Factory,
            ["{{VARIANT}}"] = JsonSerializer.Serialize(variant),
            ["{{WIDTH}}"] = width.ToString(CultureInfo.InvariantCulture),
            ["{{HEIGHT}}"] = height.ToString(CultureInfo.InvariantCulture),
            ["{{TITLE}}"] = JsonSerializer.Serialize(title),
            ["{{SUBTITLE}}"] = JsonSerializer.Serialize(subtitle),
            ["{{LEGEND}}"] = legend.HasValue ? legend.Value ? "true" : "false" : "null",
            ["{{SURFACE}}"] = surface ? "true" : "false",
            ["{{VIEWPORT}}"] = viewport ? ", new ChartCartesianViewport(x: new ChartAxisWindow(2, 8))" : "",
            ["{{ARTIFACT_KIND}}"] = artifactKind
        };
        return Package(recipe, "RecipeProgram", tokens);
    }

    /// <summary>Creates a standalone chart program preserving the factory's own frame, theme, size and output names.</summary>
    public static string EmitChart(ExampleRecipe recipe, string name, string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        return Package(recipe, "RecipeChartProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FACTORY}}"] = recipe.Factory,
            ["{{VARIANT}}"] = JsonSerializer.Serialize(variant),
            ["{{MODE}}"] = mode.ToString(),
            ["{{NAME}}"] = JsonSerializer.Serialize(name)
        });
    }

    private static string Package(ExampleRecipe recipe, string template, Dictionary<string, string> tokens) {
        tokens["{{SOURCE}}"] = ReadResource("ChartForgeX.Examples.Recipes." + recipe.Factory + ".cs").TrimEnd();
        var source = Regex.Replace(ReadResource("ChartForgeX.Examples." + template + ".cs"), @"\{\{[A-Z_]+\}\}", match => tokens[match.Value]);
        return source.Replace("\r\n", "\n").TrimEnd() + "\n";
    }

    private static string ReadResource(string name) {
        using var stream = typeof(ExampleRecipeSource).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Compiled recipe source is missing: " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
