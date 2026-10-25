using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteMixedCaptionExamples(string output) {
        WriteRemainingCaptionExamples(output);
        foreach (var family in new[] { "native", "flowchart", "requirement", "mindmap" })
            foreach (var variant in new[] { "compact", "wide" }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                var artifact = family == "native"
                    ? MixedCaptionRecipe.Create(variant, mode, ExampleTypography.FontFamily)
                    : MermaidMixedCaptionSourceExample.Create(family, variant, mode, ExampleTypography.FontFamily);
                var name = "mixed-caption-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant();
                File.WriteAllText(Path.Combine(output, name + ".svg"), artifact.ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".png"), artifact.ToPng());
                File.WriteAllText(Path.Combine(output, name + ".html"), artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }));
                File.WriteAllText(Path.Combine(output, name + ".csharp.txt"), ExampleRecipeSource.EmitMixedCaptions(family, variant, mode));
                File.WriteAllText(Path.Combine(output, name + ".json"), artifact.ToInterchangeJson());
            }
    }
}
