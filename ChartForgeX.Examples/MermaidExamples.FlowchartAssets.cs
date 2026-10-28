using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteFlowchartAssetExamples(string output) {
        foreach (var variant in new[] { "compact", "wide" }) foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var artifact = FlowchartAssetRecipe.Create(variant, mode, ExampleTypography.FontFamily);
            var name = "mermaid-assets-" + variant + "-" + mode.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(output,name+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,name+".png"),artifact.ToPng());
            File.WriteAllText(Path.Combine(output,name+".html"),ExampleTypography.EmbedInHtml(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing=VisualArtifactHtmlSizing.PreserveSize })));
            File.WriteAllText(Path.Combine(output,name+".json"),artifact.ToInterchangeJson());File.WriteAllText(Path.Combine(output,name+".csharp.txt"),ExampleRecipeSource.EmitFlowchartAssets(variant,mode));
        }
    }
}
