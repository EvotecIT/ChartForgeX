using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteSequenceParticipantViewsExamples(string output) {
        var source=MermaidFixtureSources.Read("sequence-participant-views");
        foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var artifact=SequenceParticipantViewsSourceRecipe.Create(source,variant,mode,ExampleTypography.FontFamily);
            var stem=Path.Combine(output,"mermaid-sequence-participant-views-"+variant+"-"+mode.ToString().ToLowerInvariant());
            File.WriteAllText(stem+".mmd",source);File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".json",artifact.ToInterchangeJson());
            File.WriteAllText(stem+".html",artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}).Replace("</head>",ExampleTypography.EmbeddedStyle+"</head>",StringComparison.Ordinal));
            File.WriteAllText(stem+".csharp.txt",ExampleRecipeSource.EmitSequenceParticipantViews(variant,mode));
        }
    }
}
