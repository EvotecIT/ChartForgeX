using ChartForgeX.Themes;
using ChartForgeX.Typography;
using System.Xml.Linq;

/// <summary>Shares the licensed gallery faces between native metrics and example HTML.</summary>
internal static class ExampleTypography {
    internal const string FontFamily = ChartFontStacks.ProductSans;

    internal static string EmbeddedStyle => "<style data-cfx-product-fonts=\"true\">" + ChartFontAssets.StyleSheet + "</style>";

    internal static string EmbedInHtml(string html) => html.Contains(ChartFontAssets.StyleSheet, StringComparison.Ordinal)
        ? html : html.Replace("</head>", EmbeddedStyle + "</head>", StringComparison.Ordinal);

    internal static string EmbedInSvg(string svg) => svg.Contains("font-family:'Archivo'", StringComparison.Ordinal)
        ? svg : svg.Insert(svg.IndexOf('>', svg.IndexOf("<svg", StringComparison.Ordinal)) + 1,
            new XElement("style", ChartFontAssets.StyleSheet).ToString(SaveOptions.DisableFormatting));

    // Existing gallery entry points initialize the embedded assets here; native faces resolve in core.
    internal static void Register() => _ = ChartFontAssets.StyleSheet;

    internal static void WriteAssets(string output) {
        Register();
        File.WriteAllText(Path.Combine(output, "cfx-native-text.js"), ChartForgeX.Interactivity.Html.HtmlInteractiveAssetFiles.NativeTextScript.Content);
        var destination = Path.Combine(output, "fonts");
        Directory.CreateDirectory(destination);
        foreach (var file in new[] { "Carlito-Regular.ttf", "Carlito-Bold.ttf", "OFL.txt" })
            File.Copy(Path.Combine(FontDirectory, file), Path.Combine(destination, file), true);
    }

    private static string FontDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
}
