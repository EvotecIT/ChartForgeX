using ChartForgeX;
using ChartForgeX.Themes;

internal static partial class MermaidExamples {
    private static void WriteGitGraphDepthExamples(string output) {
        foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
            foreach (var variant in new[] { "wide-lr", "compact-lr", "wide-tb", "compact-tb", "wide-bt", "compact-bt" }) {
                var block = GitGraphDepthRecipe.Create(variant, mode).Theme(VisualTheme.Graphite().WithFontFamily(ExampleTypography.FontFamily)).ThemeMode(mode);
                var id = "native-gitgraph-depth-" + variant + "-" + mode.ToString().ToLowerInvariant();
                block.SaveSvg(Path.Combine(output, id + ".svg")); block.SavePng(Path.Combine(output, id + ".png"));
                File.WriteAllText(Path.Combine(output, id + ".html"), block.ToHtmlPage().Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
                File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitGitGraph(variant, mode));
                File.WriteAllText(Path.Combine(output, id + ".gallery.csharp.txt"), ExampleRecipeSource.EmitGitGraph(variant, mode, galleryTypography: true));
            }
    }
}
