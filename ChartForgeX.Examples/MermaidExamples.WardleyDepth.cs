using ChartForgeX;
using ChartForgeX.Themes;

internal static partial class MermaidExamples {
    private static void WriteWardleyDepthExamples(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "wide", "compact" }) {
            var block = WardleyDepthRecipe.Create(variant, mode).WithTheme(VisualTheme.Graphite().WithFontFamily(ExampleTypography.FontFamily), mode);
            var id = "native-wardley-depth-" + variant + "-" + mode.ToString().ToLowerInvariant();
            block.SaveSvg(Path.Combine(output, id + ".svg")); block.SavePng(Path.Combine(output, id + ".png"));
            File.WriteAllText(Path.Combine(output, id + ".html"), block.ToHtmlPage().Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
            File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitWardley(variant, mode));
            File.WriteAllText(Path.Combine(output, id + ".gallery.csharp.txt"), ExampleRecipeSource.EmitWardley(variant, mode, galleryTypography: true));
        }
    }
}

internal static class MermaidWardleyDepthExamples {
    internal static string Source(bool dark) {
        var source = MermaidFixtureSources.Read("wardley-native-overlays");
        return !dark ? source : source.Replace("theme: default", "theme: dark", StringComparison.Ordinal)
            .Replace("'#f8fafc'", "'#182235'", StringComparison.Ordinal).Replace("'#ffffff'", "'#243247'", StringComparison.Ordinal)
            .Replace("'#172033'", "'#f1f5f9'", StringComparison.Ordinal).Replace("'#334155'", "'#cbd5e1'", StringComparison.Ordinal)
            .Replace("'#d1fae5'", "'#103b36'", StringComparison.Ordinal).Replace("'#065f46'", "'#a7f3d0'", StringComparison.Ordinal);
    }
}
