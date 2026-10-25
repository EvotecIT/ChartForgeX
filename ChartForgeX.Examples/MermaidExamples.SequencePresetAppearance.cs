using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
internal static partial class MermaidExamples {
    private static void WriteSequencePresetAppearanceExamples(string output) {
        var source=MermaidFixtureSources.Read("sequence-preset-appearance");foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var artifact=SequencePresetAppearanceSourceRecipe.Create(source,variant,mode,ExampleTypography.FontFamily);var stem=Path.Combine(output,"mermaid-sequence-preset-appearance-"+variant+"-"+mode.ToString().ToLowerInvariant());File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".html",artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}));File.WriteAllText(stem+".json",artifact.ToInterchangeJson());File.WriteAllText(stem+".mmd",source);File.WriteAllText(stem+".csharp.txt",ExampleRecipeSource.EmitSequencePresetAppearance(variant,mode));
        }
    }
}
