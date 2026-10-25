using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteAdvancedActorExamples(string output) {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "wide", "compact" }) {
            var source = MermaidFixtureSources.Read("usecase-advanced-actors");
            if (variant == "compact") source = source.Replace("direction TB", "direction LR", StringComparison.Ordinal);
            var result = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject,
                UseCase = new MermaidFlowchartRenderOptions { Width = variant == "compact" ? 390 : 1100, Height = variant == "compact" ? 1160 : 680,
                    Presentation = new MermaidPresentationOptions { ThemeMode = mode, FontFamily = ExampleTypography.FontFamily, TopologySizing = MermaidTopologySizing.Natural } } });
            if (result.HasErrors || result.Artifact == null) throw new InvalidOperationException(string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
            var id = "mermaid-usecase-advanced-actors-" + variant + "-" + mode.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(output, id + ".mmd"), source); File.WriteAllText(Path.Combine(output, id + ".svg"), result.Artifact.ToSvg());
            File.WriteAllBytes(Path.Combine(output, id + ".png"), result.Artifact.ToPng());
            File.WriteAllText(Path.Combine(output, id + ".html"), result.Artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }).Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
            File.WriteAllText(Path.Combine(output, id + ".csharp.txt"), ExampleRecipeSource.EmitAdvancedActorSource(variant, mode, galleryTypography: true));
        }
    }
}
