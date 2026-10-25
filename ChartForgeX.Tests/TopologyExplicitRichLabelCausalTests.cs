using System.Text;
using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Explicit native edge anchors include the same captured sibling rows as automatic caption painting.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExplicitRichLabelCausalTests : IDisposable {
    private const string Family="CFX Explicit Rich Label Unequal Metrics";
    public TopologyExplicitRichLabelCausalTests() {
        // These tests own their metric fixture: fill incidental ASCII gaps without replacing H or its italic ascent.
        var incidental = Enumerable.Range(32, 95).Where(value => !"HOxeFChartForgeX 0123456789".Contains((char)value))
            .ToDictionary(value => value, _ => OpenTypeTestFonts.Box);
        FontRegistry.Register(Family,OpenTypeTestFonts.NameKeyed(extraGlyphs:incidental));
        FontRegistry.Register(Family,WithAscent(OpenTypeTestFonts.NameKeyed(extraGlyphs:incidental,italic:true),1100),italic:true);
        var style=new TextStyle{Font=FontSpec.FromFamily(Family),FontSize=18};
        Assert.True(TextLayoutEngine.Measure(Content(),style).LineHeight>TextLayoutEngine.Measure("HH",style).LineHeight+4,
            "The same retained synthetic sibling fixture must exceed the old regular-row allocation.");
    }
    private static TextContent Content()=>TextContent.FromRuns(new[]{new TextContentRun("H"),new TextContentRun("H",TextInlineEmphasis.Italic)});
    private static TopologyChart Source(TopologyLabelVerticalAnchor anchor,bool structured,double themeSize) {
        var chart=TopologyExplicitLabelMarkerTests.Source(anchor).WithTheme(VisualTheme.Report().WithTypography(new VisualTypography(Family,dataLabelSize:themeSize)));
        chart.Edges[0].TextStyle=new TextStyleOverride{FontSize=18,FontWeight="400"};
        if(structured)chart.Edges[0].LabelContent=Content();else chart.Edges[0].Label="HH";
        return chart;
    }
    private static PreparedVisual Prepare(TopologyChart chart,bool fit) {
        var options=new TopologyRenderOptions{FitContentToViewport=fit,IncludeLegend=false,IncludeEdgeLabelBackplates=false};
        return fit?chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400,280),20),chart.Theme),options):chart.Prepare(options).Visual;
    }
    [Theory] [InlineData(TopologyLabelVerticalAnchor.Center)] [InlineData(TopologyLabelVerticalAnchor.TextBaseline)]
    public void BothExplicitAnchorsAllocateEveryCapturedSiblingBeforeTheirEarlyPlacementReturn(TopologyLabelVerticalAnchor anchor) {
        var chart=Source(anchor,true,11);var prepared=Prepare(chart,false);Capture(prepared,"explicit-rich-allocation-"+anchor);
        AssertFullCaption(prepared,anchor);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ActualFirstSiblingBaselineStaysPinnedWhenTheOldAllocationAlreadyFits(bool fit) {
        // A large theme plus a smaller explicit edge size is a valid public composition.
        // Its old 66px allocation already fits the tall row, isolating the plain-face baseline defect.
        var chart=Source(TopologyLabelVerticalAnchor.TextBaseline,true,33);var prepared=Prepare(chart,fit);
        Capture(prepared,"explicit-rich-baseline-"+fit);AssertFullCaption(prepared,TopologyLabelVerticalAnchor.TextBaseline);
    }
    [Theory]
    [InlineData(TopologyLabelVerticalAnchor.Center,false)] [InlineData(TopologyLabelVerticalAnchor.TextBaseline,false)]
    [InlineData(TopologyLabelVerticalAnchor.Center,true)] [InlineData(TopologyLabelVerticalAnchor.TextBaseline,true)]
    public void OrdinaryExplicitAnchorControlsKeepTheirOriginalSvgPngAndPortableBytes(TopologyLabelVerticalAnchor anchor,bool fit) {
        var prepared=Prepare(Source(anchor,false,11),fit);Capture(prepared,"explicit-rich-ordinary-"+anchor+"-"+fit);
        var svg=prepared.ToSvg();var png=prepared.ToPng();var json=prepared.ToArtifact().ToInterchangeEnvelope().ToJson();
        Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());Assert.Equal(json,prepared.ToArtifact().ToInterchangeEnvelope().ToJson());
        Assert.DoesNotContain("data-cfx-caption=\"HH\"",svg,StringComparison.Ordinal);
    }
    private static void AssertFullCaption(PreparedVisual prepared,TopologyLabelVerticalAnchor anchor) {
        var runs=prepared.Scene.Nodes.OfType<VisualSceneText>().Where(item=>item.Role=="inline-text-run").ToArray();
        Assert.Equal(2,runs.Length);Assert.Equal(new[]{false,true},runs.Select(item=>item.Text.Style.Font.Italic));
        Assert.All(runs,item=>Assert.True(item.Text.NativeGlyphInk));Assert.Equal(runs[0].Baseline,runs[1].Baseline);
        var saved=prepared.ToArtifact().ToInterchangeEnvelope();var point=saved.Edges[0].Topology!.LabelPosition!.Point;
        var region=Assert.Single(prepared.Regions,item=>item.Role=="topology-edge-label");
        Assert.True(region.Bounds.Height>=runs.Max(item=>item.Text.Metrics.LineHeight));
        if(anchor==TopologyLabelVerticalAnchor.TextBaseline)Assert.Equal(point.Y,runs[0].Baseline,6);
        else Assert.Equal(point.Y,region.Bounds.Top+region.Bounds.Height/2,6);
        var svg=prepared.ToSvg();var png=prepared.ToPng();var json=saved.ToJson();
        Assert.Equal(2,XDocument.Parse(svg).Descendants().Count(item=>item.Attribute("data-cfx-native-text-ink")!=null));NativeInlineContentTests.AssertResources(svg);
        FontRegistry.Clear();Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());Assert.Equal(json,prepared.ToArtifact().ToInterchangeEnvelope().ToJson());
    }
    private static void Capture(PreparedVisual prepared,string name) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var path=Path.Combine(output,name);File.WriteAllText(path+".svg",prepared.ToSvg());
        File.WriteAllBytes(path+".png",prepared.ToPng());
        TopologyPortableFontObservation.Capture(name, prepared);
        File.WriteAllText(path+".json",prepared.ToArtifact().ToInterchangeEnvelope().ToJson());
    }
    private static byte[] WithAscent(byte[] source,short ascent) {
        var bytes=(byte[])source.Clone();var count=(bytes[4]<<8)|bytes[5];
        for(var table=0;table<count;table++) {
            var at=12+table*16;if(Encoding.ASCII.GetString(bytes,at,4)!="hhea")continue;
            var offset=(bytes[at+8]<<24)|(bytes[at+9]<<16)|(bytes[at+10]<<8)|bytes[at+11];
            bytes[offset+4]=(byte)(ascent>>8);bytes[offset+5]=(byte)ascent;return bytes;
        }
        throw new InvalidOperationException("The same retained synthetic fixture must have its hhea table.");
    }
    public void Dispose()=>FontRegistry.Clear();
}
