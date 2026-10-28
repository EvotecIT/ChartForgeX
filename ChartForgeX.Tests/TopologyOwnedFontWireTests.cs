using ChartForgeX.Topology;
using System.Text.Json.Nodes;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyOwnedFontWireTests : IDisposable {
    [Fact]
    public void CanonicalDecodedRootBudgetsAdmitExactBoundariesAndDeduplicateEqualOwnedBytes() {
        var root=Load(OwnedFontResources.MaximumRootBytes,1);var same=Load(OwnedFontResources.MaximumRootBytes,1);var different=Load(OwnedFontResources.MaximumRootBytes,2);
        var font=FontSpec.FromFamily("CFX Boundary").WithOwnedFile(root,true);var style=new TextStyle{Font=FontSpec.FromFamily("CFX Other").WithOwnedFile(different,true)};
        var fact=new PreparedDiagramFont(font,"ordinary");var two=OwnedFontResources.Capture(fact,new VisualFrame(titleStyle:style));Assert.Equal(2,two.Roots.Count);
        var duplicate=OwnedFontResources.Capture(fact,new VisualFrame(titleStyle:new TextStyle{Font=FontSpec.FromFamily("CFX Duplicate").WithOwnedFile(same,true)}));Assert.Single(duplicate.Roots);
        var over=Load(OwnedFontResources.MaximumRootBytes+1,3);Assert.Throws<NotSupportedException>(()=>OwnedFontResources.Capture(new PreparedDiagramFont(FontSpec.FromFamily("CFX Too Large").WithOwnedFile(over,true),"ordinary")));
        var third=Load(64*1024,4);Assert.Throws<NotSupportedException>(()=>OwnedFontResources.Capture(fact,new VisualFrame(titleStyle:style,subtitleStyle:new TextStyle{Font=FontSpec.FromFamily("CFX Third").WithOwnedFile(third,true)})));
        var capture=new VisualSceneTextCapture();for(var index=0;index<=OwnedFontResources.MaximumRoots;index++) {
            var request=FontSpec.FromFamily("CFX Root "+index).WithOwnedFile(Load(32*1024,(byte)index),true);capture.Bind(new TextStyle{Font=request}).Prepare("H",12);
        }
        Assert.Throws<NotSupportedException>(()=>OwnedFontResources.Capture(new PreparedDiagramFont(FontSpec.FromFamily("CFX Root Count"),"ordinary",capture)));
        Assert.Equal(2*1024*1024,OwnedFontResources.MaximumRootBytes);Assert.Equal(4*1024*1024,OwnedFontResources.MaximumDistinctBytes);Assert.Equal(8*1024*1024,VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes);
    }
    [Fact]
    public void MalformedResourcesDescriptorsAndClustersRejectAtTheActualJsonReader() {
        TopologyOwnedFontPortableTests.Register();var chart=TopologyOwnedFontPortableTests.Source("H"+char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter));chart.Theme=chart.Theme.WithFontFamily(TopologyOwnedFontPortableTests.Primary+", "+TopologyOwnedFontPortableTests.Fallback);
        var before=chart.Prepare();TopologyOwnedFontPortableTests.Capture("wire-valid-base",before.Visual);var json=before.ToInterchangeEnvelope().ToJson();
        void Reject(Action<JsonNode> change){var raw=JsonNode.Parse(json)!;change(raw["presentation"]!["contextFont"]!);var bad=raw.ToJsonString();Assert.ThrowsAny<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(bad));}
        Reject(font=>font["fontRoots"]![0]!["data"]="AA AA");Reject(font=>font["fontRoots"]![0]!["data"]=Convert.ToBase64String(new byte[32]));
        Reject(font=>font["fontRoots"]![0]!["data"]=new string('A',((OwnedFontResources.MaximumRootBytes+2)/3+1)*4));
        Reject(font=>font["fontRoots"]![0]!["id"]=new string('0',64));Reject(font=>font["fontRoots"]!.AsArray().Add(font["fontRoots"]![0]!.DeepClone()));
        Reject(font=>{var old=font["fontRoots"]![0]!["id"]!.GetValue<string>();var wrong=new string('0',64);font["fontRoots"]![0]!["id"]=wrong;Rebind(font);
            void Rebind(JsonNode? value){if(value is JsonObject obj)foreach(var pair in obj.ToArray()){if(pair.Key=="root"&&pair.Value?.GetValue<string>()==old)obj[pair.Key]=wrong;else Rebind(pair.Value);}else if(value is JsonArray array)foreach(var child in array)Rebind(child);}});
        Reject(font=>font["fontRequests"]![0]!["face"]!["faceName"]="Contradictory face");Reject(font=>font["fontRequests"]![0]!["face"]!["collection"]=99);
        Reject(font=>font["fontRequests"]![0]!["font"]!["file"]="/not/portable.otf");Reject(font=>font["fontRequests"]![0]!["cases"]!.AsArray().Clear());Reject(font=>{font["fontRequests"]![0]!["sourceCase"]="Uppercase";font["fontRequests"]![0]!["cases"]=new JsonArray("None");});
        var requests=JsonNode.Parse(json)!["presentation"]!["contextFont"]!["fontRequests"]!.AsArray();var selected=Enumerable.Range(0,requests.Count).First(index=>requests[index]!["fontClusters"] is JsonArray rows&&rows.Count>0);
        Reject(font=>font["fontRequests"]![selected]!["fontClusters"]![0]!["inputScalars"]![0]=0xD800);
        Reject(font=>font["fontRequests"]![selected]!["fontClusters"]![0]!["outputScalars"]![0]=0x110000);
        Assert.Equal(json,VisualArtifactInterchangeEnvelope.FromJson(json).ToJson());
    }
    [Fact]
    public void OversizedOwnedFontDoesNotTurnNativePrepareOrStaticWrappingIntoAWireAdmission() {
        var root=Load(OwnedFontResources.MaximumRootBytes+1,5);var chart=TopologyOwnedFontPortableTests.Source();var font=FontSpec.FromFamily("CFX Native Large").WithOwnedFile(root,true);
        var before=chart.Prepare(TopologyOwnedFontPortableTests.Context(chart,font),TopologyOwnedFontPortableTests.Options());var svg=before.ToSvg();var png=before.ToPng();var artifact=before.ToArtifact();
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"owned-font-over-wire-native.svg"),svg);File.WriteAllBytes(Path.Combine(output,"owned-font-over-wire-native.png"),png);}
        Assert.Equal(TopologyStaticPaintObservation.Paint(svg),TopologyStaticPaintObservation.Paint(artifact.ToSvg()));Assert.Equal(png,artifact.ToPng());Assert.Throws<NotSupportedException>(()=>artifact.ToInterchangeJson());Assert.Equal(png,artifact.ToPng());
    }
    private static TrueTypeFont Load(int size,byte identity){var bytes=TopologyOwnedFontPortableTests.Bytes();Array.Resize(ref bytes,size);bytes[bytes.Length-1]=identity;return TrueTypeFont.TryLoad(bytes)!;}
    public void Dispose(){FontRegistry.Clear();_=BrowserProofFonts.Family;}
}
