using System.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void NativeSurfacePaintContracts() {
        var chart=TopologyChart.Create().WithTitle("").WithViewport(400,280,12).WithLegend(null);
        chart.Groups.Add(new TopologyGroup{Id="scope",Label="Scope",X=12,Y=12,Width=360,Height=240,Opacity=.5,SurfaceOpacity=.8,StrokeWidth=4});chart.Groups[0].DashPattern.AddRange(new double[]{7,3});
        chart.Nodes.Add(new TopologyNode{Id="node",Label="Node",X=60,Y=70,Width=180,Height=100,Shape=TopologyNodeShape.Rectangle,GroupId="scope",Opacity=.6,SurfaceOpacity=.7,StrokeWidth=3});chart.Nodes[0].DashPattern.AddRange(new double[]{9,4});
        var artifact=chart.ToVisualArtifact();var facts=artifact.ToInterchangeEnvelope();var restored=VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToTopologyChart();
        PackageAssertions.Require(facts.Version==2&&restored.Groups.Single().Opacity==.5&&restored.Nodes.Single().SurfaceOpacity==.7&&restored.Nodes[0].DashPattern.SequenceEqual(new double[]{9,4}),"Packed typed native contour paints were lost.");PackageAssertions.Png(artifact.ToPng());
        foreach(var family in new[]{"flowchart","agentflow"}) {
            var source=(family=="flowchart"?"flowchart LR\nsubgraph scope[Scope]":"agentflow-beta LR\nflow scope[Scope]")+"\na[Work] --> b[Done]\nend\nclassDef contour stroke-width:3px,stroke-dasharray:9 4\nclass a,scope contour";
            var native=(TopologyChart)PackedNativeDiagram(family+"-surface-paints",source).Model!;
            PackageAssertions.Require(native.Nodes[0].StrokeWidth==3&&native.Groups.Single().StrokeWidth==3&&native.Nodes[0].DashPattern.SequenceEqual(new double[]{9,4}),"Packed source contour paints were lost: "+family);
        }
    }
}
