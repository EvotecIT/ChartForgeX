using ChartForgeX;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteUseCaseSourceDepthExamples(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark })
            foreach (var variant in new[] { "wide", "compact" }) {
                var source = MermaidFixtureSources.Read("usecase-native-source-depth");
                if (variant == "compact") source = source.Replace("direction LR", "direction TB", StringComparison.Ordinal);
                var result = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject,
                    UseCase = new MermaidFlowchartRenderOptions { Width = variant == "compact" ? 390 : 900, Height = 620,
                        Presentation = new MermaidPresentationOptions { ThemeMode = mode, FontFamily = ExampleTypography.FontFamily, TopologySizing = MermaidTopologySizing.FixedViewport } } });
                if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
                var id = "mermaid-usecase-source-depth-" + variant + "-" + mode.ToString().ToLowerInvariant();
                File.WriteAllText(Path.Combine(output, id + ".mmd"), source);
                File.WriteAllText(Path.Combine(output, id + ".svg"), result.Artifact.ToSvg()); File.WriteAllBytes(Path.Combine(output, id + ".png"), result.Artifact.ToPng());
                File.WriteAllText(Path.Combine(output, id + ".html"), result.Artifact.ToHtmlPage().Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
                File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitUseCaseSource(variant, mode, galleryTypography: true));
            }
    }
}
