using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteFlowchartCurveExamples(string output) {
        foreach (var family in new[] { "flowchart", "swimlane" }) foreach (var variant in new[] { "compact", "wide" })
            foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                var artifact = FlowchartCurveRecipe.Create(family, variant, mode, ExampleTypography.FontFamily);
                var name = "mermaid-curves-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                File.WriteAllText(Path.Combine(output, name + ".svg"), artifact.ToSvg());
                File.WriteAllBytes(Path.Combine(output, name + ".png"), artifact.ToPng());
                File.WriteAllText(Path.Combine(output, name + ".html"), ExampleTypography.EmbedInHtml(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize })));
                File.WriteAllText(Path.Combine(output, name + ".json"), artifact.ToInterchangeJson());
                File.WriteAllText(Path.Combine(output, name + ".csharp.txt"), ExampleRecipeSource.EmitFlowchartCurves(family, variant, mode));
            }
    }
}
