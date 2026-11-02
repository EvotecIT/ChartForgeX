using ChartForgeX.Themes;
using ChartForgeX.Typography;

/// <summary>Shares the licensed gallery faces between native metrics and example HTML.</summary>
internal static class ExampleTypography {
    internal const string FontFamily = ChartFontStacks.ProductSans;

    internal static string EmbeddedStyle => "<style data-cfx-product-fonts=\"true\">" + ChartFontAssets.StyleSheet + "</style>";

    internal static string EmbedInHtml(string html) => html.Contains(ChartFontAssets.StyleSheet, StringComparison.Ordinal)
        ? html : html.Replace("</head>", EmbeddedStyle + "</head>", StringComparison.Ordinal);

    /// <summary>Applies shared typography to ordinary example hosts without changing authored font demonstrations.</summary>
    internal static string EmbedInHost(string html) => html
        .Replace(EmbeddedStyle, string.Empty, StringComparison.Ordinal)
        .Replace(ChartFontAssets.StyleSheet, string.Empty, StringComparison.Ordinal)
        .Replace("</head>", EmbeddedStyle + "<style>body{font-family:" + FontFamily
            + ";font-variant-numeric:tabular-nums}code,pre,kbd,samp{font-family:"
            + ChartFontStacks.ProductMono + "}</style></head>", StringComparison.Ordinal);

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
