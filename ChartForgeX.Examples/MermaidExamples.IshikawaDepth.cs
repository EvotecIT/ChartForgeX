using ChartForgeX;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static class MermaidIshikawaDepthExamples {
    internal static string Source(bool dark) {
        var source = MermaidFixtureSources.Read("ishikawa-native-depth");
        return dark ? source.Replace("theme: default", "theme: dark", StringComparison.Ordinal).Replace("'#e2e8f0'", "'#243247'", StringComparison.Ordinal).Replace("'#182235'", "'#f1f5f9'", StringComparison.Ordinal) : source;
    }
}

internal static partial class MermaidExamples {
    private static void WriteIshikawaDepthExamples(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            foreach (var variant in new[] { "wide", "compact" }) {
                var block = IshikawaDepthRecipe.Create(variant, mode).WithTheme(VisualTheme.Graphite().WithFontFamily(ExampleTypography.FontFamily), mode)
                    .WithNaturalSize(variant == "compact" ? 390 : 1100, 520);
                var name = "native-ishikawa-depth-" + variant + "-" + mode.ToString().ToLowerInvariant();
                block.SaveSvg(Path.Combine(output, name + ".svg")); block.SavePng(Path.Combine(output, name + ".png"));
                var artifact = VisualArtifact.Create(name, VisualArtifactKind.VisualBlock, block); artifact.HtmlSizing = VisualArtifactHtmlSizing.PreserveSize;
                File.WriteAllText(Path.Combine(output, name + ".html"), artifact.ToHtmlPage().Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
                File.WriteAllText(Path.Combine(output, name + ".csharp.txt"), ExampleRecipeSource.EmitIshikawa(variant, mode));
                File.WriteAllText(Path.Combine(output, name + ".gallery.csharp.txt"), ExampleRecipeSource.EmitIshikawa(variant, mode, galleryTypography: true));
            }
    }
}
