using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteGraphStyleSettingsExamples(string output) {
        foreach(var family in new[]{"flowchart","agentflow"})foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var source=MermaidFixtureSources.Read(family+"-style-settings");var width=variant=="compact"?390:1100;
            var rendered=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,
                Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=ExampleTypography.FontFamily,TopologySizing=MermaidTopologySizing.Natural},
                Flowchart=new MermaidFlowchartRenderOptions{Width=width,Height=700},Agentflow=new MermaidTopologyRenderOptions{Width=width,Height=700}});
            if(rendered.HasErrors||rendered.Artifact==null)throw new InvalidOperationException(string.Join("; ",rendered.Diagnostics.Select(item=>item.Message)));
            var artifact=rendered.Artifact;var id="mermaid-"+family+"-style-settings-"+variant+"-"+mode.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(output,id+".mmd"),source);File.WriteAllText(Path.Combine(output,id+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,id+".png"),artifact.ToPng());
            File.WriteAllText(Path.Combine(output,id+".html"),ExampleTypography.EmbedInHtml(artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize})));
            File.WriteAllText(Path.Combine(output,id+".json"),artifact.ToInterchangeJson());File.WriteAllText(Path.Combine(output,id+".csharp.txt"),ExampleRecipeSource.EmitGraphStyleSettings(family,variant,mode));
        }
    }
}
