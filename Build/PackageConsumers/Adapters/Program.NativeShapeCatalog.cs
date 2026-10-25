using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void NativeShapeCatalogContracts() {
        foreach(var name in Enum.GetNames(typeof(TopologyNodeShape))) {
            var shape=(TopologyNodeShape)Enum.Parse(typeof(TopologyNodeShape),name);if((int)shape<27)continue;
            var chart=TopologyChart.Create().WithTitle("").WithViewport(380,230,12).WithLegend(null);
            chart.Nodes.Add(new TopologyNode {Id="node",Label="Native caption",X=30,Y=25,Width=180,Height=110,Shape=shape,ShowStatusBadge=false,PreserveDisplayModeSize=true});
            var artifact=chart.ToVisualArtifact();var facts=artifact.ToInterchangeEnvelope();
            PackageAssertions.Require(facts.Version==2&&facts.Nodes.Single().Topology!.Shape==shape,"Packed native surface identity/version was lost: "+name);
            PackageAssertions.Require(VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToTopologyChart().Nodes.Single().Shape==shape,"Packed detached native surface was lost: "+name);
            PackageAssertions.Png(artifact.ToPng());
        }
        foreach(var item in new[]{("bang","ScallopedBurst"),("h-cyl","HorizontalCylinder"),("datastore","DataStore"),("person","Person"),
            ("text","TextBlock"),("brace","BraceLeft"),("docs","StackedDocument"),("browser","Browser"),("fork","SynchronizationBar")}) {
            var source="flowchart LR\nnode@{ shape: "+item.Item1+", label: \"Native caption\" }";
            var direct=MermaidRenderer.Render(source);var fenced=new MermaidVisualMarkupParser().Parse("```mermaid\n"+source+"\n```");
            PackageAssertions.Require(!direct.HasErrors&&direct.Diagnostics.Count==0&&direct.Artifact!=null&&!fenced.HasErrors&&fenced.Artifacts.Count==1,"Packed Flowchart surface was rejected: "+item.Item1);
            var chart=(TopologyChart)direct.Artifact!.Model!;
            PackageAssertions.Require(chart.Nodes.Single().Shape.ToString()==item.Item2,"Packed Flowchart surface used a fallback: "+item.Item1);
            PackageAssertions.Require(direct.Artifact.ToSvg()==fenced.Artifacts[0].ToSvg(),"Packed direct/fence surface scenes differ: "+item.Item1);
            PackageAssertions.Require(!XDocument.Parse(direct.Artifact.ToSvg()).Descendants().Any(element=>element.Name.LocalName is "script" or "foreignObject" or "image"),"Packed source surface is not inert.");
            PackageAssertions.Png(direct.Artifact.ToPng());
        }
        const string collapsed="agentflow-beta\nflow review[Review]@{ view: collapsed }\na --> b\nend\nb --> done";
        var summary=(TopologyChart)PackedNativeDiagram("agentflow-native-summary",collapsed).Model!;
        PackageAssertions.Require(summary.Nodes.Single(node=>node.Id=="review").Shape.ToString()=="CollapsedContainer"&&summary.Edges.Single().SourceNodeId=="review","Packed Agentflow summary lost generic native presentation or crossing remapping.");
    }
}
