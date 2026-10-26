using ChartForgeX.Themes;

public static partial class ExampleRecipeSource {
    /// <summary>Emits an ordinary Core + HTML adapter program with a selectable text presentation.</summary>
    public static string EmitHtmlOwnedFonts(string variant, VisualThemeMode mode, bool galleryTypography = false) {
        if (variant is not ("wide" or "compact")) throw new ArgumentException("Unknown font report size.", nameof(variant));
        if (!Enum.IsDefined(typeof(VisualThemeMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var recipe = ReadResource("ChartForgeX.Examples.Recipes.HtmlOwnedFontRecipe.cs");
        var split = recipe.IndexOf("/// <summary>", StringComparison.Ordinal);
        var setup = galleryTypography ? "FontRegistry.Register(\"Report text\",\"fonts/Carlito-Regular.ttf\",400);\nFontRegistry.Register(\"Report text\",\"fonts/Carlito-Bold.ttf\",700);\n" : "";
        return "using System;\nusing System.IO;\nusing ChartForgeX;\nusing ChartForgeX.Core;\nusing ChartForgeX.Primitives;\nusing ChartForgeX.Themes;\nusing ChartForgeX.Typography;\nusing ChartForgeX.Interactivity.Html;\n"
            + setup + "var chart=HtmlOwnedFontRecipe.Create(\"" + variant + "\",VisualThemeMode." + mode + (galleryTypography ? ",\"Report text\"" : "") + ");\n"
            + "Directory.CreateDirectory(\"output\");File.WriteAllText(\"output/report.svg\",chart.ToSvg());File.WriteAllBytes(\"output/report.png\",chart.ToPng());\n"
            + "File.WriteAllText(\"output/report.html\",chart.ToInteractiveHtmlPage(options=>options.TextPresentation=HtmlChartTextPresentation.Automatic));\n" + recipe.Substring(split);
    }
}
