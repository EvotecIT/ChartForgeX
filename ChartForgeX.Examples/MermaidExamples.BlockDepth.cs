using ChartForgeX;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

internal static partial class MermaidExamples {
    private static void WriteNativeBlocks(string output) {
        Directory.CreateDirectory(output);
        foreach (var variant in new[] { "wide", "compact" }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var block = BlockLayoutDepthRecipe.Create(variant, mode).WithTheme(VisualTheme.Graphite().WithFontFamily(ExampleTypography.FontFamily), mode);
            var name = "block-native-" + variant + "-" + mode.ToString().ToLowerInvariant();
            block.SaveSvg(Path.Combine(output, name + ".svg")); block.SavePng(Path.Combine(output, name + ".png"));
            File.WriteAllText(Path.Combine(output, name + ".html"), block.ToHtmlPage().Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
            File.WriteAllText(Path.Combine(output, name + ".csharp.txt"), ExampleRecipeSource.EmitBlockLayout(variant, mode, galleryTypography: true));
        }
    }
}
