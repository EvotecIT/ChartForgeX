using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Run unchanged before and after the opt-in C4 source packet; compare retained artifact bytes externally.</summary>
public sealed class MermaidC4OrdinaryByteControls {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExistingC4AndOrdinaryNativeGroupGridExposeBeforeAfterBytes(bool nested) {
        var source = nested ? MermaidC4NativeSemanticsTests.Source : "C4Context\nPerson(user, \"User\")\nSystem(app, \"App\")\nRel(user,app,\"Uses\")";
        var parsed = new MermaidParser().ParseC4(source); Assert.False(parsed.HasErrors); var c4 = parsed.Document!.ToTopologyChart();
        var grid = TopologyChart.Create().WithTitle("").WithViewport(600,400).WithLayout(TopologyLayoutMode.GroupGrid)
            .AddAutoGroup("scope","Scope").AddAutoNode("one","One",groupId:"scope").AddAutoNode("two","Two",groupId:"scope");
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output) && !Path.IsPathRooted(output)) throw new InvalidOperationException("A configured byte-control capture directory must be absolute.");
        if (!string.IsNullOrWhiteSpace(output)) Directory.CreateDirectory(output);
        foreach (var pair in new[] { ("c4", c4), ("native-grid", grid) }) {
            var prepared = pair.Item2.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
            Assert.Equal("svg", XDocument.Parse(svg).Root!.Name.LocalName);
            var image = RasterImageDecoder.Decode(png); Assert.True(image.Width > 0 && image.Height > 0);
            Assert.Equal(pair.Item1 == "native-grid" || !nested ? 2 : 3, VisualArtifactInterchangeEnvelope.FromJson(json).Nodes.Count);
            if (!string.IsNullOrWhiteSpace(output)) {
                var stem = Path.Combine(output,"c4-default-"+pair.Item1+"-"+nested);
                File.WriteAllText(stem+".svg",svg); File.WriteAllBytes(stem+".png",png); File.WriteAllText(stem+".json",json);
            }
        }
    }
}
