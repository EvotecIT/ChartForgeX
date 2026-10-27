using System.Text.RegularExpressions;
using System.Text.Json;

/// <summary>Packages one compiled native recipe as a standalone ordinary-package C# program.</summary>
public static partial class ExampleRecipeSource {
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
            ["{{FONT_THEME}}"] = galleryTypography ? ".WithFontFamily(\"CFX Proof Carlito\")" : "",
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

    /// <summary>Packages the compiled native Git graph block through its existing static SVG/PNG owner, without a prepared-chart adapter.</summary>
    public static string EmitGitGraph(string variant = "wide-lr", VisualThemeMode mode = VisualThemeMode.Light, bool galleryTypography = false) {
        // Capture the exact public fixture's supported variants before emitting an executable program.
        _ = GitGraphDepthRecipe.Create(variant, mode);
        return Package(nameof(GitGraphDepthRecipe), "RecipeBlockProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FACTORY}}"] = nameof(GitGraphDepthRecipe), ["{{VARIANT}}"] = JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{FONT_SETUP}}"] = galleryTypography
                ? "// Optional exact gallery typography; copy the gallery's fonts directory beside this program.\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Bold.ttf\", 700);\n" : "",
            ["{{FONT_THEME}}"] = galleryTypography ? ".WithFontFamily(\"CFX Proof Carlito\")" : ""
        });
    }

    /// <summary>Packages the compiled native BlockLayout recipe through the existing static block program owner.</summary>
    public static string EmitBlockLayout(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, bool galleryTypography = false) {
        if (variant != "wide" && variant != "compact") throw new ArgumentOutOfRangeException(nameof(variant));
        return Package(nameof(BlockLayoutDepthRecipe), "RecipeBlockProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FACTORY}}"] = nameof(BlockLayoutDepthRecipe), ["{{VARIANT}}"] = JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{FONT_SETUP}}"] = galleryTypography
                ? "// Optional exact gallery typography; copy the gallery's fonts directory beside this program.\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Bold.ttf\", 700);\n" : "",
            ["{{FONT_THEME}}"] = galleryTypography ? ".WithFontFamily(\"CFX Proof Carlito\")" : ""
        });
    }

    /// <summary>Packages the maintained Kanban source as a complete ordinary Mermaid-package program.</summary>
    public static string EmitKanban(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact")) throw new ArgumentOutOfRangeException(nameof(variant), variant, "Use wide or compact.");
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return Template("RecipeKanbanProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{SOURCE_TEXT}}"] = JsonSerializer.Serialize(ReadResource("ChartForgeX.Examples.MermaidFixtures.kanban-source-geometry.mmd")),
            ["{{WIDTH}}"] = variant == "compact" ? "390" : "1100", ["{{MODE}}"] = mode.ToString()
        });
    }

    /// <summary>Packages the complete native cause tree as a Core-only ordinary SVG/PNG/HTML program.</summary>
    public static string EmitIshikawa(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, bool galleryTypography = false) {
        _ = IshikawaDepthRecipe.Create(variant, mode);
        return Package(nameof(IshikawaDepthRecipe), "RecipeFishboneProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{VARIANT}}"] = JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(), ["{{WIDTH}}"] = variant == "compact" ? "390" : "1100",
            ["{{FONT_SETUP}}"] = galleryTypography
                ? "// Optional exact gallery typography; copy the gallery's fonts directory beside this program.\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Bold.ttf\", 700);\n" : "",
            ["{{FONT_THEME}}"] = galleryTypography ? ".WithFontFamily(\"CFX Proof Carlito\")" : ""
        });
    }

    /// <summary>Packages the compiled native Wardley block as an ordinary dependency-free C# program.</summary>
    public static string EmitWardley(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light, bool galleryTypography = false) {
        _ = WardleyDepthRecipe.Create(variant, mode);
        return Package(nameof(WardleyDepthRecipe), "RecipeBlockProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FACTORY}}"] = nameof(WardleyDepthRecipe), ["{{VARIANT}}"] = JsonSerializer.Serialize(variant), ["{{MODE}}"] = mode.ToString(),
            ["{{FONT_SETUP}}"] = galleryTypography
                ? "// Optional exact gallery typography; copy its fonts directory beside this program.\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Regular.ttf\", 400);\nFontRegistry.Register(\"CFX Proof Carlito\", \"fonts/Carlito-Bold.ttf\", 700);\n" : "",
            ["{{FONT_THEME}}"] = galleryTypography ? ".WithFontFamily(\"CFX Proof Carlito\")" : ""
        });
    }

    /// <summary>Packages the compiled native XY counterpart as a complete ordinary Core program.</summary>
    public static string EmitXYChart(string variant = "wide-vertical", VisualThemeMode mode = VisualThemeMode.Light) {
        _ = MermaidXYChartDepthRecipe.Create(variant, mode);
        return Package(nameof(MermaidXYChartDepthRecipe), "RecipeChartProgram", new Dictionary<string, string>(StringComparer.Ordinal) {
            ["{{FACTORY}}"] = nameof(MermaidXYChartDepthRecipe), ["{{VARIANT}}"] = JsonSerializer.Serialize(variant),
            ["{{MODE}}"] = mode.ToString(), ["{{NAME}}"] = JsonSerializer.Serialize("xy-source-depth")
        });
    }

    private static string Package(ExampleRecipe recipe, string template, Dictionary<string, string> tokens) => Package(recipe.Factory, template, tokens);
    private static string Package(string factory, string template, Dictionary<string, string> tokens, string? sourceResource = null) {
        var recipeSource = ReadResource(sourceResource ?? "ChartForgeX.Examples.Recipes." + factory + ".cs").TrimEnd();
        // A compiled recipe may own imports; standalone top-level statements must follow every import.
        var imports = Regex.Match(recipeSource, @"\A(?:using [^\r\n;]+;[ \t]*\r?\n)+");
        tokens["{{SOURCE}}"] = recipeSource.Substring(imports.Length);
        return PrependImports(Template(template, tokens), imports.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Template(string template, Dictionary<string, string> tokens) {
        var source = Regex.Replace(ReadResource("ChartForgeX.Examples." + template + ".cs"), @"\{\{[A-Z_]+\}\}", match => tokens[match.Value]);
        var program = source.Replace("\r\n", "\n").TrimEnd() + "\n";
        // Use the exact import owner that compiles these factories, including Core-only consumers.
        var imports = ReadResource("ChartForgeX.Examples.ExampleRecipeImports.cs").Split('\n')
            .Where(line => line.StartsWith("global using ", StringComparison.Ordinal))
            .Select(line => line.Substring("global ".Length));
        return PrependImports(program, imports);
    }

    private static string PrependImports(string program, IEnumerable<string> imports) {
        var prefix = string.Join("\n", imports.Select(line => line.Trim()).Where(line => line.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Where(line => !Regex.IsMatch(program, "^" + Regex.Escape(line) + @"\r?$", RegexOptions.Multiline)));
        return prefix.Length == 0 ? program : prefix + "\n" + program;
    }

    private static string ReadResource(string name) {
        using var stream = typeof(ExampleRecipeSource).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Compiled recipe source is missing: " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
