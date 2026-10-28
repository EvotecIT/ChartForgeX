using System.Text;
using System.Text.Json.Nodes;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Post-product budget, recognition and native restoration contracts.</summary>
public sealed class NativeArtworkPortableSizeTests {
    [Fact]
    public void LargeNativePortraitRetainsEncodedPixelsAndUntouchedNativePngAfterPortableRestoration() {
        var pixels=new byte[192*192*4];new Random(193).NextBytes(pixels);var image=new RgbaImage(192,192,pixels);var artwork=TopologyIconArtwork.Portrait(image);
        var chart=Chart(artwork);var artifact=chart.ToVisualArtifact();var native=chart.ToPng();var json=artifact.ToInterchangeJson();Capture("large-artwork-native.png",native);Capture("large-artwork-portable.json",json);
        Assert.True(artwork.ImageHref!.Length>65536);var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var href=restored.Nodes[0].Artwork!.ImageHref!;var decoded=RasterImageDecoder.Decode(Convert.FromBase64String(href.Substring(href.IndexOf(',')+1)));
        Assert.Equal(image.Width,decoded.Width);Assert.Equal(image.Height,decoded.Height);Assert.Equal(pixels,decoded.Pixels);Assert.Equal(native,restored.ToPng());
    }

    [Fact]
    public void RecognizedRasterUriHasAnExactIndependentStringBoundary() {
        var maximum=VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters;
        var at=Data(maximum);var accepted=Facts(at);var json=accepted.ToJson();Capture("artwork-uri-exact-boundary.json",json);Assert.Equal(maximum,at.Length);
        Assert.Equal(at,VisualArtifactInterchangeEnvelope.FromJson(json).Nodes[0].Topology!.Artwork!.ImageHref);
        var beyond=Facts(Data(maximum+4));Assert.Throws<ArgumentException>(()=>beyond.ToJson());
        var wire=JsonNode.Parse(json)!;wire["nodes"]![0]!["topology"]!["artwork"]!["imageHref"]=Data(maximum+4);var rejected=wire.ToJsonString();Capture("artwork-uri-beyond-boundary.json",rejected);
        Assert.Throws<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(rejected));
    }

    [Theory]
    [InlineData("bad-alphabet")][InlineData("bad-padding")][InlineData("noncanonical-padding")][InlineData("bad-signature")][InlineData("wrong-mime")][InlineData("svg")][InlineData("url")]
    public void LargeAllowanceDoesNotAdmitMalformedOrUnrelatedStrings(string mode) {
        var valid=Data(70000);string value;
        if(mode=="bad-alphabet")value=valid.Substring(0,60)+"!"+valid.Substring(61);
        else if(mode=="bad-padding")value=valid.Substring(0,valid.Length-3)+"=AA";
        else if(mode=="noncanonical-padding")value=valid.Substring(0,valid.Length-4)+"AB==";
        else if(mode=="bad-signature")value="data:image/x-portable-pixmap;base64,"+new string('A',valid.Length-36);
        else if(mode=="wrong-mime")value=valid.Replace("image/x-portable-pixmap","image/png",StringComparison.Ordinal);
        else if(mode=="svg")value="data:image/svg+xml;base64,"+new string('A',70000);
        else value="https://unbound.invalid/"+new string('a',70000);
        var facts=Facts(value);Capture("artwork-rejected-"+mode+".input.txt",value);Assert.Throws<ArgumentException>(()=>facts.ToJson());
    }

    [Fact]
    public void WholeEnvelopeAndGeneralTextLimitsRemainIndependent() {
        var large=Data(VisualArtifactInterchangeEnvelope.MaximumArtworkImageDataUriCharacters);var facts=Facts(large);var first=facts.Nodes[0];
        for(var i=1;i<4;i++)facts.Nodes.Add(new VisualArtifactInterchangeNode{Id="copy-"+i,Role=first.Role,Kind=first.Kind,Label=first.Label,Status=first.Status,
            X=first.X,Y=first.Y,Width=first.Width,Height=first.Height,Topology=first.Topology});
        Assert.Throws<InvalidOperationException>(()=>facts.ToJson());
        var aggregate=JsonNode.Parse(Facts(Data(70000)).ToJson())!;aggregate["nodes"]![0]!["topology"]!["artwork"]!["imageHref"]=large;
        var nodes=aggregate["nodes"]!.AsArray();for(var i=1;i<4;i++){var copy=nodes[0]!.DeepClone();copy["id"]="copy-"+i;nodes.Add(copy);}
        var aggregateJson=aggregate.ToJsonString();Capture("artwork-envelope-beyond-boundary.json",aggregateJson);Assert.True(aggregateJson.Length>VisualArtifactInterchangeEnvelope.MaximumJsonCharacters);
        Assert.Throws<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(aggregateJson));
        var json=JsonNode.Parse(Facts(Data(70000)).ToJson())!;json["title"]=new string('x',65537);var oversizedCaption=json.ToJsonString();Capture("large-artwork-oversized-label.json",oversizedCaption);
        Assert.Throws<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(oversizedCaption));
        Assert.Throws<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(new string(' ',VisualArtifactInterchangeEnvelope.MaximumJsonCharacters+1)));
    }

    // Existing PPM magic and canonical Base64 let the URI land exactly at its character bound without adding a codec.
    // Only string/signature admission is tested here; full native pixel fidelity uses the valid Portrait PNG above.
    private static string Data(int characters) {
        const string prefix="data:image/x-portable-pixmap;base64,";if((characters-prefix.Length)%4!=0)throw new ArgumentException("Boundary must align with Base64 groups.");
        var bytes=new byte[(characters-prefix.Length)/4*3];Encoding.ASCII.GetBytes("P6\n1 1\n255\n").CopyTo(bytes,0);return prefix+Convert.ToBase64String(bytes);
    }
    private static VisualArtifactInterchangeEnvelope Facts(string href) {
        var tiny=TopologyIconArtwork.Portrait(new RgbaImage(1,1,new byte[]{255,0,0,255}));var facts=Chart(tiny).ToVisualArtifact().ToInterchangeEnvelope();facts.Nodes[0].Topology!.Artwork!.ImageHref=href;return facts;
    }
    private static TopologyChart Chart(TopologyIconArtwork artwork) {
        var chart=TopologyChart.Create().WithTitle("").WithViewport(320,240,12).WithLegend(null);chart.Nodes.Add(new TopologyNode{Id="image",Label="Image",X=70,Y=40,Width=160,Height=160,
            DisplayMode=TopologyNodeDisplayMode.Artwork,Artwork=artwork,ShowLabel=false,ShowStatusBadge=false,PreserveDisplayModeSize=true});return chart;
    }
    private static void Capture(string name,string value){var output=Output();if(output!=null)File.WriteAllText(Path.Combine(output,name),value);}
    private static void Capture(string name,byte[] value){var output=Output();if(output!=null)File.WriteAllBytes(Path.Combine(output,name),value);}
    private static string? Output(){var output=Environment.GetEnvironmentVariable("CFX_ARTWORK_SIZE_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return null;Directory.CreateDirectory(output);return output;}
}
