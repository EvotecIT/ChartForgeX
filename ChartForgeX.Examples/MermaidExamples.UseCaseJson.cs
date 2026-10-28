using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteUseCaseJsonExamples(string output) {
        var source = MermaidFixtureSources.Read("usecase-json-table-content");
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) foreach (var variant in new[] { "wide", "compact" }) {
            var artifact = MermaidUseCaseJsonSourceExample.Create(source, variant, mode, ExampleTypography.FontFamily);
            var name = "mermaid-usecase-json-" + variant + "-" + mode.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(output, name + ".mmd"), variant == "compact" ? source.Replace("direction LR", "direction TB", StringComparison.Ordinal) : source);
            File.WriteAllText(Path.Combine(output, name + ".svg"), artifact.ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".png"), artifact.ToPng());
            File.WriteAllText(Path.Combine(output, name + ".html"), ExampleTypography.EmbedInHtml(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize })));
            File.WriteAllText(Path.Combine(output, name + ".csharp.txt"), ExampleRecipeSource.EmitUseCaseJson(variant, mode));
            File.WriteAllText(Path.Combine(output, name + ".json"), artifact.ToInterchangeJson());
        }
    }
}
