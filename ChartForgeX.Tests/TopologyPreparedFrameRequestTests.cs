using System.Text.Json.Nodes;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyPreparedFrameRequestTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void CompleteFiniteCallerFrameAndRoleStylesReplayThroughTheSameNativeFrame(bool transparent) {
        var chart=Source();chart.Legend=TopologyLegend.Default();
        var title=new TextStyle{Font=new FontSpec{Family=BrowserProofFonts.Family,Weight=650,Italic=true,ColorPaletteIndex=1,Variations=FontVariationSettings.Default.WithAxis("wght",550)},
            FontSize=22,Color=ChartColor.FromHex("#123456"),Alignment=TextAlignment.Center,LineHeight=1.1,UnderlineStyle=TextDecorationStyle.Double,TextCase=TextCaseTransform.Uppercase,Hinting=TextHinting.None,OpenTypeLanguageTag="ENG"};
        var subtitle=title.Clone();subtitle.FontSize=13;subtitle.Font.Weight=400;subtitle.Alignment=TextAlignment.Right;subtitle.UnderlineStyle=TextDecorationStyle.None;
        var legend=subtitle.Clone();legend.FontSize=11;legend.Alignment=TextAlignment.Left;legend.StrikethroughStyle=TextDecorationStyle.Single;
        var frame=new VisualFrame(title:"Caller heading",subtitle:"Caller subtitle",showLegend:true,legendPosition:ChartLegendPosition.BottomRight,
            showSurface:true,transparentBackground:transparent,titleStyle:title,subtitleStyle:subtitle,legendStyle:legend,
            legendMaximumRows:3,legendMaximumHeightFraction:.25,showCard:true,legendTitle:"Caller key");
        var layout=new VisualLayoutOptions(new VisualSize(760,520),new ChartPadding(31,27,49,35));
        var before=chart.Prepare(new VisualRenderContext(layout,chart.Theme,chart.ThemeMode,frame,chart.Theme.Typography.ToFontSpec()),new TopologyRenderOptions{FitContentToViewport=false});Capture("finite-"+transparent+"-direct",before);
        var saved=before.ToArtifact().ToInterchangeEnvelope();var json=saved.ToJson();var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var after=restored.Prepare();Capture("finite-"+transparent+"-restored",after.Visual);
        var retained=Assert.IsType<VisualFrame>(saved.Presentation!.Frame);Assert.True(retained.HasExplicitLegend);Assert.True(retained.HasExplicitLegendPosition);
        Assert.True(frame.SameRequest(retained));Assert.True(frame.SameRequest(restored.PreparedFrame!));Assert.Equal(2,saved.Version);
        Assert.Equal("Model heading",saved.Title);Assert.Equal("Model subtitle",saved.Subtitle);Assert.Equal("Model heading",restored.Title);Assert.Equal("Model subtitle",restored.Subtitle);
        var cleared=VisualArtifactInterchangeEnvelope.FromJson(json);cleared.Presentation!.Frame=null;var withoutOverride=cleared.ToTopologyChart().Prepare();Capture("finite-"+transparent+"-cleared-frame",withoutOverride.Visual);
        var sourceText=withoutOverride.Visual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(mark=>mark.Text.Lines).Select(line=>line.Text).ToArray();
        Assert.Contains("Model heading",sourceText);Assert.Contains("Model subtitle",sourceText);Assert.DoesNotContain("CALLER HEADING",sourceText);
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()),TopologyStaticPaintObservation.Paint(after.ToSvg()));Assert.Equal(before.ToPng(),after.ToPng());
        title.Font.Weight=100;title.FontSize=40;var detached=retained.TitleStyle!;detached.Font.Weight=200;detached.FontSize=44;
        Assert.Equal(650,retained.TitleStyle!.Font.Weight);Assert.Equal(22,retained.TitleStyle.FontSize);Assert.Equal(json,saved.ToJson());Assert.Equal(before.ToPng(),after.ToPng());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OriginalNullAndExplicitHeadingsRemainDistinctAfterSourceEditsAndExplicitContextWins(bool explicitHeading) {
        var chart=Source();var frame=new VisualFrame(title:explicitHeading?"Caller heading":null,subtitle:explicitHeading?"":null,showLegend:false,transparentBackground:true);
        var before=chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(760,520),24),chart.Theme,frame:frame),new TopologyRenderOptions{IncludeLegend=false});
        var saved=before.ToArtifact().ToInterchangeEnvelope();Capture("heading-"+explicitHeading+"-direct",before);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();restored.Title="Edited source heading";restored.Subtitle="Edited source subtitle";
        Assert.Equal("Model heading",saved.Title);Assert.Equal("Model subtitle",saved.Subtitle);
        var after=restored.Prepare();Capture("heading-"+explicitHeading+"-edited",after.Visual);
        Assert.Equal(explicitHeading?"Caller heading":null,saved.Presentation!.Frame!.Title);Assert.Equal(explicitHeading?"":null,saved.Presentation.Frame.Subtitle);
        var text=after.Visual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(mark=>mark.Text.Lines).Select(line=>line.Text).ToArray();Assert.Contains(explicitHeading?"Caller heading":"Edited source heading",text);
        if(explicitHeading)Assert.DoesNotContain("Edited source heading",text);
        var opaque=restored.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(760,520),24),restored.Theme,frame:new VisualFrame(showLegend:false)),new TopologyRenderOptions{IncludeLegend=false});Capture("heading-"+explicitHeading+"-explicit-context",opaque);
        Assert.Contains(opaque.Scene.Nodes,mark=>mark.Role=="background");Assert.DoesNotContain(after.Visual.Scene.Nodes,mark=>mark.Role=="background");
    }
    [Fact]
    public void CanonicalDefaultFrameRequestsStayOmittedAndNativeSnapshotsStayDetached() {
        var chart=Source();chart.Theme=VisualTheme.Graphite();chart.Title="";chart.Subtitle="";var options=new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=false};chart.WithRenderOptions(options);
        var before=chart.Prepare();var direct=chart.Prepare(VisualExportRequest.ForTopology(chart,options).Context,options);Capture("default-before",before.Visual);Capture("default-explicit",direct);
        Assert.Null(before.ToInterchangeEnvelope().Presentation!.Frame);Assert.Null(direct.ToArtifact().ToInterchangeEnvelope().Presentation!.Frame);
        Assert.DoesNotContain("\"frame\"",before.ToInterchangeEnvelope().ToJson(),StringComparison.Ordinal);Assert.Equal(1,before.ToInterchangeEnvelope().Version);Assert.Equal(before.ToPng(),direct.ToPng());
        var svg=before.ToSvg();var png=before.ToPng();chart.Title="Changed after capture";chart.Nodes[0].Label="Changed model";Assert.Equal(svg,before.ToSvg());Assert.Equal(png,before.ToPng());
    }
    [Fact]
    public void MalformedFiniteFrameFontsAndFlagsRejectAtTheExistingPortableBoundary() {
        var chart=Source();var frame=new VisualFrame(showLegend:false,transparentBackground:true,titleStyle:new TextStyle{Font=FontSpec.FromFamily(BrowserProofFonts.Family),FontSize=20});
        var before=chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(760,520),24),chart.Theme,frame:frame),new TopologyRenderOptions{IncludeLegend=false});var json=before.ToArtifact().ToInterchangeJson();Capture("malformed-base",before);
        void Reject(Action<JsonNode> change){var raw=JsonNode.Parse(json)!;change(raw["presentation"]!["frame"]!);Assert.ThrowsAny<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(raw.ToJsonString()));}
        Reject(raw=>raw["explicitLegend"]=false);Reject(raw=>raw["legendPosition"]="Undefined");Reject(raw=>raw["legendRows"]=0);Reject(raw=>raw["titleStyle"]!["size"]=-1);
        Reject(raw=>raw["titleStyle"]!["font"]!["policy"]!["fontWeight"]="bold");Reject(raw=>raw["titleStyle"]!["font"]!["file"]="/not/a/portable/font.ttf");
        Assert.Equal(8*1024*1024,VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes);
    }
    [Fact]
    public void FileBoundFrameStylesUseTheOwnedFontSnapshotAfterItsSourceFileIsRemoved() {
        using var resource=typeof(TopologyPreparedFrameRequestTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.fallback-weight-regular.ttf")
            ?? throw new InvalidOperationException("The existing embedded OpenType fixture is required.");
        var path=Path.Combine(Path.GetTempPath(),"cfx-frame-owned-"+Guid.NewGuid().ToString("N")+".ttf");
        using(var file=File.Create(path))resource.CopyTo(file);
        try {
        var chart=Source();
        var frame=new VisualFrame(title:"ChartForgeX",showLegend:false,titleStyle:new TextStyle{Font=FontSpec.FromFile(path),FontSize=20});
        var before=chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(760,520),24),chart.Theme,frame:frame),new TopologyRenderOptions{IncludeLegend=false});var wrapped=before.ToArtifact();var svg=wrapped.ToSvg();var png=wrapped.ToPng();
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"frame-file-native.svg"),svg);File.WriteAllBytes(Path.Combine(output,"frame-file-native.png"),png);}
        var json=wrapped.ToInterchangeJson();File.Delete(path);FontRegistry.Clear();var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();
        Capture("file-owned-restored",restored.Visual);
        Assert.NotEmpty(svg);Assert.NotEmpty(png);Assert.DoesNotContain(path,json,StringComparison.Ordinal);Assert.Equal(png,restored.ToPng());
        Assert.Equal(TopologyStaticPaintObservation.Paint(svg),TopologyStaticPaintObservation.Paint(restored.ToSvg()));Assert.Equal(svg,wrapped.ToSvg());Assert.Equal(png,wrapped.ToPng());
        } finally {File.Delete(path);}
    }
    private static TopologyChart Source() {
        var chart=TopologyChart.Create().WithId("finite-frame-request").WithTitle("Model heading").WithSubtitle("Model subtitle").WithViewport(760,520,24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("from","From",80,140,width:100,height:60).AddNode("to","To",420,210,width:100,height:60);
        chart.Theme=VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);foreach(var node in chart.Nodes){node.Shape=TopologyNodeShape.Rectangle;node.ShowStatusBadge=false;node.PreserveDisplayModeSize=true;}return chart;
    }
    private static void Capture(string name,PreparedVisual value) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var stem=Path.Combine(output,"frame182-"+name);File.WriteAllText(stem+".svg",value.ToSvg());File.WriteAllBytes(stem+".png",value.ToPng());File.WriteAllText(stem+".json",value.ToArtifact().ToInterchangeJson());
    }
}
