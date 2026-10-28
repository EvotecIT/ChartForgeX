using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void FlowchartAssetContracts() {
        const string source="flowchart LR\nimage@{img:'owned',label:'Review group',pos:'t',h:24,constraint:'on'} --> server@{icon:'common:server',label:'Publish result',form:'rounded'}";
        var pixels=new byte[]{255,0,0,255,0,255,0,128};var options=new MermaidRenderOptions {WarningPolicy=MermaidWarningPolicy.Reject,
            Flowchart=new MermaidFlowchartRenderOptions{Width=640,Height=480,ImageBindings=new Dictionary<string,RgbaImage>{{"owned",new RgbaImage(2,1,pixels)}}}};
        var result=RenderFixture(source,options);PackageAssertions.Require(!result.HasErrors&&result.Diagnostics.Count==0&&result.Artifact!=null,"Packed finite assets were rejected.");
        var artifact=result.Artifact!;var png=artifact.ToPng();var fence=CreateFixtureParser(options).Parse("```mermaid\n"+source+"\n```").Artifacts.Single();
        PackageAssertions.Require(png.SequenceEqual(fence.ToPng())&&artifact.ToSvg()==fence.ToSvg(),"Packed direct and fence assets diverged.");
        var chart=(TopologyChart)artifact.Model!;PackageAssertions.Require(chart.Nodes[0].CaptionPlacement==TopologyCaptionPlacement.Top&&chart.Nodes[0].CaptionGap==8&&chart.Nodes[0].CaptionTextStyle?.FontSize==16&&chart.Nodes[1].GlyphScale.HasValue,"Packed asset presentation facts were lost.");
        var restored=VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();PackageAssertions.Require(png.SequenceEqual(restored.ToPng()),"Packed finite assets changed on native restoration.");
        pixels[0]=0;options.Flowchart!.ImageBindings!.Clear();PackageAssertions.Require(png.SequenceEqual(artifact.ToPng()),"Packed caller image pixels remained mutable aliases.");PackageAssertions.Png(png);
        PackageAssertions.Require(!artifact.ToHtmlPage().Contains("<script",StringComparison.OrdinalIgnoreCase),"Packed static assets enabled script.");
    }
}
