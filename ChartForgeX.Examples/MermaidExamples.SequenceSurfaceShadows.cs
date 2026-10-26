internal static partial class MermaidExamples {
    private static void WriteSequenceSurfaceShadowExamples(string output) {
        var source = MermaidFixtureSources.Read("sequence-retained-surface-shadows");
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            foreach (var variant in new[] { "compact", "wide" }) {
                var result = SequenceSurfaceShadowSourceRecipe.Parse(source, variant, mode, ExampleTypography.FontFamily);
                var artifact = result.Artifact!;
                var stem = Path.Combine(output, "mermaid-sequence-surface-shadows-" + variant + "-" + mode.ToString().ToLowerInvariant());
                File.WriteAllText(stem + ".svg", artifact.ToSvg());
                File.WriteAllBytes(stem + ".png", artifact.ToPng());
                File.WriteAllText(stem + ".html", ExampleTypography.EmbedInHtml(artifact.ToHtmlPage()));
                File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
                File.WriteAllText(stem + ".mmd", source);
                File.WriteAllText(stem + ".diagnostics.txt", string.Join("\n", result.Diagnostics.Select(item => item.Code + ": " + item.Message)));
                File.WriteAllText(stem + ".csharp.txt", ExampleRecipeSource.EmitSequenceSurfaceShadows(variant, mode));
            }
        }
    }
}
