using System;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void SwimlaneLayoutDepthContracts() {
        var chart=TopologyChart.Create().WithTitle("").WithViewport(900,700,24).WithLayout(TopologyLayoutMode.Swimlane)
            .AddAutoGroup("parent","Delivery").AddAutoGroup("local","Local").AddAutoGroup("other","Partner")
            .AddAutoNode("a","Accept",groupId:"local",width:120,height:56).AddAutoNode("b","Build",groupId:"local",width:120,height:56)
            .AddAutoNode("c","Confirm",groupId:"other",width:120,height:56).AddEdge("internal","a","b").AddEdge("handoff","b","c");
        chart.Groups[1].ParentGroupId="parent";chart.Groups[1].LayoutDirection=TopologyLayoutDirection.TopToBottom;
        foreach(var node in chart.Nodes){node.DisplayMode=TopologyNodeDisplayMode.Card;node.PreserveDisplayModeSize=true;node.Metadata["layer"]=node.Id=="b"?"1":"0";}
        var options=new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,LayeredNodeSpacing=70,LayeredRankSpacing=130,SwimlaneIgnoreCrossLaneRankConstraints=true};
        var prepared=chart.Prepare(options);var saved=prepared.ToInterchangeEnvelope();var a=saved.Nodes.Single(node=>node.Id=="a");var b=saved.Nodes.Single(node=>node.Id=="b");
        PackageAssertions.Require(b.Y>a.Y&&saved.Edges.Count==2,"Packed native Swimlane ignored local direction or dropped its real handoff.");
        var svg=prepared.ToSvg();options.SwimlaneIgnoreCrossLaneRankConstraints=false;chart.Groups[1].LayoutDirection=TopologyLayoutDirection.RightToLeft;
        PackageAssertions.Require(svg==prepared.ToSvg(),"Packed prepared Swimlane retained mutable host option/group state.");
        var restored=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();PackageAssertions.Require(restored.LayoutMode==TopologyLayoutMode.Manual&&restored.Groups[1].ParentGroupId=="parent","Packed prepared restoration lost its actual geometry/membership boundary.");
        PackageAssertions.Png(prepared.ToPng());
        var ordered=TopologyChart.Create().WithTitle("").WithViewport(900,700,24).WithLayout(TopologyLayoutMode.Swimlane,TopologyLayoutDirection.LeftToRight);
        foreach(var id in new[]{"A","B","C","D"})ordered.AddAutoGroup(id,id).AddAutoNode(id.ToLowerInvariant(),id,groupId:id);
        for(var index=0;index<3;index++)ordered.AddEdge("ad-"+index,"a","d").AddEdge("bc-"+index,"b","c");
        var native=ordered.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,SwimlaneAutomaticLaneOrdering=true});
        var ids=native.ToInterchangeEnvelope().Groups.OrderBy(group=>group.Y).Select(group=>group.Id).ToArray();
        var cost=3*Math.Abs(Array.IndexOf(ids,"A")-Array.IndexOf(ids,"D"))+3*Math.Abs(Array.IndexOf(ids,"B")-Array.IndexOf(ids,"C"));
        PackageAssertions.Require(cost<12&&ordered.Groups.Select(group=>group.Id).SequenceEqual(new[]{"A","B","C","D"}),"Packed automatic responsibility order did not improve real geometry or changed the source graph.");
        PackageAssertions.Png(native.ToPng());
        var ranked=TopologyChart.Create().WithTitle("").WithViewport(900,700,24).WithLayout(TopologyLayoutMode.Layered,TopologyLayoutDirection.LeftToRight);
        foreach(var id in new[]{"A","B","C","D","E"})ranked.AddAutoNode(id,id,width:120,height:56);
        for(var index=0;index<ranked.Nodes.Count;index++)ranked.Nodes[index].Metadata["layer"]=index<2?"0":index==4?"2":"1";
        ranked.AddEdge("ad","A","D").AddEdge("bc","B","C");
        var optimized=ranked.Prepare(new TopologyRenderOptions {IncludeTitle=false,IncludeLegend=false,LayeredOptimizeRanksByCrossings=true});
        var records=optimized.ToInterchangeEnvelope().Nodes;var c=records.Single(node=>node.Id=="C");var d=records.Single(node=>node.Id=="D");
        PackageAssertions.Require(c.X!=d.X,"Packed rank optimizer did not make the available legal improvement.");PackageAssertions.Png(optimized.ToPng());
    }
}
