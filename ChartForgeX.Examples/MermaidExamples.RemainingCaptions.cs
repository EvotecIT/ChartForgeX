using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteRemainingCaptionExamples(string output) {
        foreach (var family in new[] { "class", "er", "flowchart", "requirement" })
            foreach (var variant in new[] { "compact", "wide" }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
                var artifact = MermaidRemainingCaptionRolesExample.Create(family, variant, mode, ExampleTypography.FontFamily);
                var stem = Path.Combine(output, "remaining-caption-" + family + "-" + variant + "-" + mode.ToString().ToLowerInvariant());
                File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".png", artifact.ToPng());
                File.WriteAllText(stem + ".html", ExampleTypography.EmbedInHtml(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize })));
                File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
                File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitRemainingCaptionRoles(family, variant, mode));
                File.WriteAllText(stem + ".mmd", MermaidRemainingCaptionRolesExample.Source(family));
            }
    }
}
