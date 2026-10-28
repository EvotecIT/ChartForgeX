using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
internal static partial class MermaidExamples {
    private static void WriteSequenceNavigationExamples(string output) {
        var source=MermaidFixtureSources.Read("sequence-navigation");foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var result=SequenceNavigationSourceRecipe.Create(source,variant,mode,ExampleTypography.FontFamily);var stem=Path.Combine(output,"mermaid-sequence-navigation-"+variant+"-"+mode.ToString().ToLowerInvariant());File.WriteAllText(stem+".mmd",source);File.WriteAllText(stem+".svg",result.Artifact.ToSvg());File.WriteAllBytes(stem+".png",result.Artifact.ToPng());File.WriteAllText(stem+".json",result.Artifact.ToInterchangeJson());File.WriteAllText(stem+".html",result.Html.Replace("</head>",ExampleTypography.EmbeddedStyle+"</head>",StringComparison.Ordinal));File.WriteAllText(stem+".csharp.txt",ExampleRecipeSource.EmitSequenceNavigation(variant,mode));
        }
    }
}
