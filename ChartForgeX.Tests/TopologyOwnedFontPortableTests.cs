using System.Text.Json.Nodes;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Post-product contracts for actual owned primary/fallback faces; no host-font coverage claim.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyOwnedFontPortableTests : IDisposable {
    internal const string Primary = "CFX Owned Diagram Primary";
    internal const string Fallback = "CFX Owned Diagram Fallback";
    [Theory] [InlineData(false)] [InlineData(true)]
    public void CapturedFileAndCollectionMemberRestoreAfterTheOwnedSourceFileIsRemoved(bool collection) {
        var bytes = collection ? OpenTypeTestFonts.Collection(Bytes(), Bytes(weight:700)) : Bytes();
        var path = Path.Combine(Path.GetTempPath(), "cfx-owned-diagram-" + Guid.NewGuid().ToString("N") + (collection ? ".ttc" : ".otf"));
        File.WriteAllBytes(path, bytes);
        try {
            var font = FontSpec.FromFile(path); font.Family=Primary; if(collection){font.CollectionIndex=1;font.Weight=700;}
            var chart=Source();var before=chart.Prepare(Context(chart,font),Options());Capture("file-"+collection+"-direct",before);
            var saved=before.ToArtifact().ToInterchangeEnvelope();var json=saved.ToJson();var oldPng=before.ToPng();File.Delete(path);FontRegistry.Clear();
            var loaded=VisualArtifactInterchangeEnvelope.FromJson(json);var restored=loaded.ToTopologyChart();var after=restored.Prepare();Capture("file-"+collection+"-restored",after.Visual);
            Assert.False(File.Exists(path));Assert.DoesNotContain(path,json,StringComparison.Ordinal);Assert.DoesNotContain("filePath",json,StringComparison.Ordinal);
            Assert.NotNull(saved.Presentation!.ContextFont);Assert.Equal(2,saved.Version);Assert.Single(JsonNode.Parse(json)!["presentation"]!["contextFont"]!["fontRoots"]!.AsArray());
            SamePaint(before,after.Visual);Assert.Equal(Faces(before),Faces(after.Visual));Assert.Equal(oldPng,before.ToPng());
            var clone=TopologyLayoutEngine.Clone(restored);clone.Nodes[0].Label="OH";var edited=clone.Prepare();Capture("file-"+collection+"-edited-clone",edited.Visual);
            Assert.Equal("HO",restored.Nodes[0].Label);Assert.Equal(oldPng,before.ToPng());Assert.Equal(json,saved.ToJson());
            var detached=loaded.Presentation!.ContextFont!.Font;detached.Weight=100;detached.Family="Changed";Assert.Equal(json,loaded.ToJson());
        } finally {File.Delete(path);}
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ThemeAndExplicitContextCaptureActualFallbackRootsAndContinueKnownClustersWithoutTheRegistry(bool explicitContext) {
        Register();var chart=Source("H"+char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter));var stack=Primary+", "+Fallback;
        if(!explicitContext)chart.Theme=chart.Theme.WithFontFamily(stack);
        var before=explicitContext?chart.Prepare(Context(chart,FontSpec.FromFamily(stack)),Options()):chart.Prepare().Visual;Capture("fallback-"+explicitContext+"-direct",before);
        var json=before.ToArtifact().ToInterchangeJson();FontRegistry.Clear();var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var after=restored.Prepare();Capture("fallback-"+explicitContext+"-restored",after.Visual);
        Assert.Equal(2,JsonNode.Parse(json)!["presentation"]!["contextFont"]!["fontRoots"]!.AsArray().Count);SamePaint(before,after.Visual);Assert.Equal(Faces(before),Faces(after.Visual));
        var original=before.ToPng();restored.Nodes[0].Label="OH"+char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter);var edited=restored.Prepare();Capture("fallback-"+explicitContext+"-known-edit",edited.Visual);
        Assert.Equal(Faces(before),Faces(edited.Visual));Assert.Equal(original,before.ToPng());
        Register();var family=Primary+" New";FontRegistry.Register(family,Bytes(weight:700));restored.Theme=restored.Theme.WithFontFamily(family);var rebound=restored.Prepare();Capture("fallback-"+explicitContext+"-theme-edit",rebound.Visual);
        Assert.Equal(family,rebound.ToInterchangeEnvelope().Presentation!.ContextFont!.Font.Family);Assert.Equal(original,before.ToPng());
    }
    [Fact]
    public void EditingANewFallbackClusterContinuesOnlyItsDetachedCaptureAndKeepsEarlierPreparedOutputImmutable() {
        Register();var chart=Source("H"+char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter));chart.Theme=chart.Theme.WithFontFamily(Primary+", "+Fallback);
        var before=chart.Prepare();Capture("continuation-original",before.Visual);var json=before.ToInterchangeEnvelope().ToJson();var png=before.ToPng();FontRegistry.Clear();
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var known=restored.Prepare();Capture("continuation-known",known.Visual);SamePaint(before.Visual,known.Visual);
        var newPoint=OpenTypeTestFonts.PrivateUseCharacter+1;var extra=Enumerable.Range(32,95).ToDictionary(value=>value,_=>OpenTypeTestFonts.Box);extra[newPoint]=OpenTypeTestFonts.Box;
        FontRegistry.Register("CFX New Detached Fallback",OpenTypeTestFonts.NameKeyed(extraGlyphs:extra));
        restored.Nodes[0].Label+=char.ConvertFromUtf32(newPoint);var edited=restored.Prepare();Capture("continuation-new-cluster",edited.Visual);var editedJson=edited.ToInterchangeEnvelope().ToJson();
        Assert.Equal(3,JsonNode.Parse(editedJson)!["presentation"]!["contextFont"]!["fontRoots"]!.AsArray().Count);
        Assert.Equal(json,before.ToInterchangeEnvelope().ToJson());Assert.Equal(png,before.ToPng());Assert.Equal(png,known.ToPng());
        FontRegistry.Clear();var replay=VisualArtifactInterchangeEnvelope.FromJson(editedJson).ToTopologyChart().Prepare();Capture("continuation-new-replayed",replay.Visual);SamePaint(edited.Visual,replay.Visual);
    }
    [Fact]
    public void NodeRoleFamilyUsesTheSameCapturedRequestOwnerAndExplicitNewContextRemainsAuthoritative() {
        Register();var chart=Source();chart.Nodes[0].TextStyle=new TextStyleOverride{FontFamily=Primary,FontWeight="700",OpenTypeLanguageTag="ENG"};
        var before=chart.Prepare();Capture("role-direct",before.Visual);var json=before.ToInterchangeEnvelope().ToJson();FontRegistry.Clear();
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var after=restored.Prepare();Capture("role-restored",after.Visual);SamePaint(before.Visual,after.Visual);
        Assert.Contains(after.Visual.Scene.Nodes.OfType<VisualSceneText>(),text=>text.Role=="topology-node-label"&&text.Text.Face.Font!.SelectedFamily==Primary);
        Register();var newContext=Context(restored,FontSpec.FromFamily(Fallback));restored.Nodes[0].TextStyle=null;var overridden=restored.Prepare(newContext,Options());Capture("role-explicit-context",overridden);
        Assert.Equal(Fallback,overridden.ToArtifact().ToInterchangeEnvelope().Presentation!.ContextFont!.Font.Family);
    }
    [Fact]
    public void OrdinaryGraphiteAndSizeOnlyRoleKeepTheIncumbentNullCaptureAndVersionOne() {
        var chart=Source();var before=chart.Prepare();Capture("ordinary-null",before.Visual);var saved=before.ToInterchangeEnvelope();
        Assert.Null(saved.Presentation!.ContextFont);Assert.Equal(1,saved.Version);Assert.DoesNotContain("contextFont",saved.ToJson(),StringComparison.Ordinal);
        chart.Nodes[0].TextStyle=new TextStyleOverride{FontSize=12};var explicitSize=chart.Prepare();Capture("ordinary-size-only",explicitSize.Visual);Assert.Null(explicitSize.ToInterchangeEnvelope().Presentation!.ContextFont);
    }
    [Fact]
    public void PublicFileCollectionAndFaceEditsInvalidateTheOwnedViewWhileKnownRequestsRemainFrozen() {
        var a=TrueTypeFont.TryLoad(OpenTypeTestFonts.Collection(Bytes(),Bytes(weight:700)),0)!;
        var font=FontSpec.FromFamily(Primary).WithOwnedFile(a,true);var capture=new VisualSceneTextCapture();var owned=font.WithCapturedText(capture);
        var first=TypographyFontResolver.ResolveFace(owned).Font!;owned.CollectionIndex=1;var changed=owned.WithCapturedText(new VisualSceneTextCapture());var second=TypographyFontResolver.ResolveFace(changed).Font!;
        Assert.Equal(0,first.CollectionIndex);Assert.Equal(1,second.CollectionIndex);Assert.True(changed.CapturedExplicitFile);
        changed.FaceName="No such face";Assert.Null(changed.OwnedFileFont);Assert.Null(changed.NativeTextCapture);
        changed.FilePath="/absent/cfx-font.otf";Assert.Null(changed.OwnedFileFont);Assert.Null(changed.NativeTextCapture);Assert.Equal(0,first.CollectionIndex);
    }
    internal static TopologyChart Source(string label="HO") {
        var chart=TopologyChart.Create().WithId("owned-font-diagram").WithTitle("").WithViewport(760,520,24).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("node",label,180,160,width:180,height:90);
        var node=chart.Nodes[0];node.Shape=TopologyNodeShape.Rectangle;node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;chart.WithRenderOptions(Options());return chart;
    }
    internal static TopologyRenderOptions Options()=>new(){IncludeTitle=false,IncludeLegend=false,FitContentToViewport=false};
    internal static VisualRenderContext Context(TopologyChart chart,FontSpec font)=>new(new VisualLayoutOptions(new VisualSize(760,520),24),chart.Theme,chart.ThemeMode,new VisualFrame(showLegend:false),font);
    internal static byte[] Bytes(int weight=400,bool privateUse=true)=>OpenTypeTestFonts.NameKeyed(includePrivateUse:privateUse,weight:weight,extraGlyphs:Enumerable.Range(32,95).ToDictionary(value=>value,_=>OpenTypeTestFonts.Box));
    internal static void Register(){FontRegistry.Register(Primary,Bytes(privateUse:false));FontRegistry.Register(Fallback,Bytes());}
    internal static string[] Faces(PreparedVisual value)=>value.Scene.Nodes.OfType<VisualSceneText>().SelectMany(mark=>mark.Text.Lines).SelectMany(line=>line.Glyphs).Select(glyph=>glyph.Face.CapturedByteIdentity).Distinct().OrderBy(value=>value,StringComparer.Ordinal).ToArray();
    internal static void SamePaint(PreparedVisual expected,PreparedVisual actual){Assert.Equal(TopologyStaticPaintObservation.Paint(expected.ToSvg()),TopologyStaticPaintObservation.Paint(actual.ToSvg()));Assert.Equal(expected.ToPng(),actual.ToPng());}
    internal static void Capture(string name,PreparedVisual value){
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not{Length:>0} output)return;Directory.CreateDirectory(output);var stem=Path.Combine(output,"owned-font-"+name);
        File.WriteAllText(stem+".svg",value.ToSvg());File.WriteAllBytes(stem+".png",value.ToPng());File.WriteAllText(stem+".json",value.ToArtifact().ToInterchangeJson());
    }
    public void Dispose(){FontRegistry.Clear();_=BrowserProofFonts.Family;}
}
