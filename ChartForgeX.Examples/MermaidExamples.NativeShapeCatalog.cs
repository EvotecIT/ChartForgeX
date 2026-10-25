using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteNativeShapeCatalogExamples(string output) {
        foreach(var suffix in new[]{"a","b","c","d"}) foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})
            foreach(var variant in new[]{"compact","wide"}) {
                var source=MermaidFixtureSources.Read("flowchart-native-shape-catalog-"+suffix);
                var rendered=MermaidRenderer.Render(source,new MermaidRenderOptions {WarningPolicy=MermaidWarningPolicy.Reject,
                    Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=ExampleTypography.FontFamily,TopologySizing=MermaidTopologySizing.Natural},
                    Flowchart=new MermaidFlowchartRenderOptions{Width=variant=="compact"?390:1100,Height=800}});
                if(rendered.HasErrors||rendered.Artifact==null)throw new InvalidOperationException(string.Join("; ",rendered.Diagnostics.Select(item=>item.Message)));
                var id="mermaid-native-shape-catalog-"+suffix+"-"+variant+"-"+mode.ToString().ToLowerInvariant();var artifact=rendered.Artifact;
                File.WriteAllText(Path.Combine(output,id+".mmd"),source);File.WriteAllText(Path.Combine(output,id+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,id+".png"),artifact.ToPng());
                File.WriteAllText(Path.Combine(output,id+".html"),artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}));
                File.WriteAllText(Path.Combine(output,id+".json"),artifact.ToInterchangeJson());File.WriteAllText(Path.Combine(output,id+".csharp.txt"),ExampleRecipeSource.EmitNativeShapeCatalog(suffix,variant,mode));
            }
    }
}
