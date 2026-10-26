using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteZenUmlPresentationExamples(string output) {
        foreach(var family in new[]{"presentation","order"})foreach(var variant in new[]{"compact","wide"})foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark}) {
            var artifact=ZenUmlPresentationRecipe.Create(family,variant,mode,ExampleTypography.FontFamily);
            var name="zenuml-"+family+"-"+variant+"-"+mode.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(output,name+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,name+".png"),artifact.ToPng());
            File.WriteAllText(Path.Combine(output,name+".html"),ExampleTypography.EmbedInHtml(artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize})));
            File.WriteAllText(Path.Combine(output,name+".json"),artifact.ToInterchangeJson());
            File.WriteAllText(Path.Combine(output,name+".csharp.txt"),ExampleRecipeSource.EmitZenUmlPresentation(family,variant,mode));
        }
    }
}
