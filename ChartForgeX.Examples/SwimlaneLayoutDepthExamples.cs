using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static class SwimlaneLayoutDepthExamples {
    public static void Write(string output) {
        Directory.CreateDirectory(output);var assembly=typeof(SwimlaneLayoutDepthExamples).Assembly;
        using var stream=assembly.GetManifestResourceStream("ChartForgeX.Examples.MermaidFixtures.swimlane-native-layout-depth.mmd")??throw new InvalidOperationException("Swimlane source missing.");
        using var reader=new StreamReader(stream);var source=reader.ReadToEnd();File.WriteAllText(Path.Combine(output,"swimlane-native-layout-depth.mmd"),source);
        using var code=assembly.GetManifestResourceStream("ChartForgeX.Examples.SwimlaneLayoutDepthProgram.cs")??throw new InvalidOperationException("Complete Swimlane program missing.");
        using var codeReader=new StreamReader(code);File.WriteAllText(Path.Combine(output,"SwimlaneLayoutDepthProgram.cs"),codeReader.ReadToEnd());
        foreach(var dark in new[]{false,true})foreach(var width in new[]{390,1100}){
            var options=new MermaidRenderOptions {WarningPolicy=MermaidWarningPolicy.Reject,Presentation=new MermaidPresentationOptions {ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light},Swimlane=new MermaidFlowchartRenderOptions {Width=width,Height=850}};
            var direct=MermaidRenderer.Render(source,options);if(direct.HasErrors||direct.Artifact==null)throw new InvalidOperationException("Swimlane source failed.");
            var fence=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");if(fence.HasErrors||fence.Artifacts.Count!=1)throw new InvalidOperationException("Swimlane fence failed.");
            foreach(var pair in new[]{("direct",direct.Artifact!),("fenced",fence.Artifacts[0])}){
                var path=Path.Combine(output,"swimlane-"+width+(dark?"-dark-":"-light-")+pair.Item1);var artifact=pair.Item2;
                File.WriteAllText(path+".svg",artifact.ToSvg());File.WriteAllBytes(path+".png",artifact.ToPng());
                File.WriteAllText(path+".html",artifact.ToHtmlPage(new VisualArtifactRenderOptions {HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}));File.WriteAllText(path+".json",artifact.ToInterchangeJson());
            }
        }
    }
}
