using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class MermaidExamples {
    private static void WriteNativeSurfacePaintExamples(string output) {
        foreach(var family in new[]{"flowchart","agentflow","native"})foreach(var mode in new[]{VisualThemeMode.Light,VisualThemeMode.Dark})foreach(var variant in new[]{"compact","wide"}) {
            var width=variant=="compact"?390:1100;string source="";VisualArtifact artifact;
            if(family=="native") {
                var chart=TopologyChart.Create().WithTitle("Native contour paints").WithViewport(width,360,24).WithLegend(null);chart.ThemeMode=mode;
                chart.Groups.Add(new TopologyGroup{Id="scope",Label="Own container ink",X=16,Y=16,Width=310,Height=240,StrokeWidth=4,Opacity=.5,SurfaceOpacity=.8,BackgroundColor="#ffd8b8"});chart.Groups[0].DashPattern.AddRange(new double[]{7,3});
                chart.Nodes.Add(new TopologyNode{Id="node",Label="Independent node ink",X=50,Y=85,Width=220,Height=100,Shape=TopologyNodeShape.Rectangle,GroupId="scope",StrokeWidth=3,Opacity=.6,SurfaceOpacity=.7,BackgroundColor="#b8d8ff",ShowStatusBadge=false});chart.Nodes[0].DashPattern.AddRange(new double[]{9,4});artifact=chart.ToVisualArtifact();
            } else {
                source=MermaidFixtureSources.Read(family+"-native-surface-paints");var rendered=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,
                    Presentation=new MermaidPresentationOptions{ThemeMode=mode,FontFamily=ExampleTypography.FontFamily,TopologySizing=MermaidTopologySizing.Natural},Flowchart=new MermaidFlowchartRenderOptions{Width=width,Height=700},Agentflow=new MermaidTopologyRenderOptions{Width=width,Height=700}});
                if(rendered.HasErrors||rendered.Artifact==null)throw new InvalidOperationException(string.Join("; ",rendered.Diagnostics.Select(item=>item.Message)));artifact=rendered.Artifact;
            }
            var id="mermaid-"+family+"-native-surface-paints-"+variant+"-"+mode.ToString().ToLowerInvariant();
            File.WriteAllText(Path.Combine(output,id+".source.txt"),source);File.WriteAllText(Path.Combine(output,id+".svg"),artifact.ToSvg());File.WriteAllBytes(Path.Combine(output,id+".png"),artifact.ToPng());
            File.WriteAllText(Path.Combine(output,id+".html"),artifact.ToHtmlPage(new VisualArtifactRenderOptions{HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}));File.WriteAllText(Path.Combine(output,id+".json"),artifact.ToInterchangeJson());
            File.WriteAllText(Path.Combine(output,id+".csharp.txt"),ExampleRecipeSource.EmitNativeSurfacePaints(family,variant,mode));
        }
    }
}
