using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void GraphStyleSettingsContracts() {
        foreach(var family in new[]{"flowchart","agentflow"}) {
            var source="---\nconfig:\n  "+family+":\n    diagramPadding: 44\n    nodeSpacing: 75\n    rankSpacing: 140\n    useMaxWidth: false\n---\n"+
                (family=="flowchart"?"flowchart LR\nsubgraph review[Review]":"agentflow-beta LR\nflow review[Review]")+"\na reads@-->|Reads| b\nend\n"+
                "classDef framed fill:#ffeedd,stroke:#345678,color:#234567\nclassDef association stroke:#234567,stroke-width:3px,stroke-dasharray:9 4\nclass review,a framed\nclass reads association\nlinkStyle 0 opacity:0.6";
            var artifact=PackedNativeDiagram(family+"-styles-settings",source);var chart=(TopologyChart)artifact.Model!;
            PackageAssertions.Require(chart.Nodes.Count==2&&chart.Groups.Single().BackgroundColor=="#ffeedd"&&chart.Edges.Single().StrokeWidth==3&&chart.Edges[0].DashPattern.SequenceEqual(new double[]{9,4}),"Packed family class/group/edge paint was lost: "+family);
            PackageAssertions.Require(chart.Viewport.Padding==44&&chart.DefaultRenderOptions!.LayeredNodeSpacing==75&&chart.DefaultRenderOptions.LayeredRankSpacing==140,"Packed graph spacing was lost: "+family);
            var restored=VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();
            PackageAssertions.Require(restored.Groups.Single().BorderColor=="#345678"&&restored.Edges.Single().Opacity==.6,"Packed style portable capture was lost: "+family);
            PackageAssertions.Png(artifact.ToPng());
        }
    }
}
