using System.Xml.Linq;
using System.Text.Json.Nodes;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyNoteRoutingViewportTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void GroupTargetNotesRetainTheirActualProjectedRoutingDomainAndRecomputeAfterEdits(bool left) {
        var chart=TopologyChart.Create().WithId("group-note-routing").WithTitle("").WithViewport(320,240,24).WithLayout(TopologyLayoutMode.Manual)
            .AddGroup("group","Group",100,100,900,500);
        chart.Theme=VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);
        chart.Groups[0].ShowLabel=false;
        chart.Annotations.Add(new TopologyAnnotation("note","Group note","group",left?TopologyAnnotationSide.Left:TopologyAnnotationSide.Right));
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=true});
        var before=chart.Prepare();var saved=before.ToInterchangeEnvelope();Capture("group-"+left+"-direct",before);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();var after=restored.Prepare();Capture("group-"+left+"-restored",after);
        Assert.InRange(saved.Annotations[0].TopologyDefaultUnits!.Value,.000001,.99);Assert.NotNull(saved.Annotations[0].TopologyRoutingViewport);
        Assert.Equal(AnnotationInk(before.ToSvg()),AnnotationInk(after.ToSvg()));
        var domain=restored.Annotations[0].PreparedRoutingViewport;restored.Groups[0].X+=20;
        var moved=restored.Prepare();Capture("group-"+left+"-moved",moved);
        Assert.Equal(domain,moved.ToInterchangeEnvelope().Annotations[0].TopologyRoutingViewport);
        Assert.NotEqual(AnnotationInk(after.ToSvg()),AnnotationInk(moved.ToSvg()));
        restored.WithViewport(420,320,24);
        // Clearing captured units restores the full native 140-unit panel and 36-unit gutter.
        // Keep that edited request in its admitted positive canvas domain.
        if(left)restored.Groups[0].X+=140+36;
        var resized=restored.Prepare();Capture("group-"+left+"-resized",resized);
        Assert.Null(resized.ToInterchangeEnvelope().Annotations[0].TopologyDefaultUnits);Assert.Null(resized.ToInterchangeEnvelope().Annotations[0].TopologyRoutingViewport);
    }

    [Fact]
    public void MalformedOrDetachedRoutingViewportsFailThroughTheExistingPortableBoundary() {
        var chart=TopologyChart.Create().WithId("invalid-note-domain").WithTitle("").WithViewport(640,400,24)
            .AddNode("node","Node",100,100,width:150,height:80);
        chart.Annotations.Add(new TopologyAnnotation("note","Note","node"));
        var saved=chart.Prepare(new TopologyRenderOptions{IncludeLegend=false,FitContentToViewport=true}).ToInterchangeEnvelope();
        saved.Annotations[0].TopologyDefaultUnits=2;
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ChartRect(double.NaN,0,10,10));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ChartRect(0,0,-1,10));
        saved.Annotations[0].TopologyRoutingViewport=new ChartRect(123456789,0,10,10);
        var admitted=saved.ToJson();
        foreach(var field in new[]{"width","height"}) {
            var raw=JsonNode.Parse(admitted)!;raw["annotations"]![0]!["topologyRoutingViewport"]![field]=-1;
            Assert.ThrowsAny<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(raw.ToJsonString()));
        }
        foreach(var malformed in new JsonNode[]{JsonValue.Create(TopologyPathGeometry.MaximumCoordinate+1)!,JsonValue.Create("not-a-number")!}) {
            var raw=JsonNode.Parse(admitted)!;raw["annotations"]![0]!["topologyRoutingViewport"]!["x"]=malformed;
            Assert.ThrowsAny<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(raw.ToJsonString()));
        }
        var nonfinite=admitted.Replace("\"x\":123456789","\"x\":1e309",StringComparison.Ordinal);
        Assert.NotEqual(admitted,nonfinite);Assert.ThrowsAny<ArgumentException>(()=>VisualArtifactInterchangeEnvelope.FromJson(nonfinite));
        saved.Annotations[0].TopologyRoutingViewport=new ChartRect(7,8,0,0);saved.Validate();
        saved.Annotations[0].TopologyRoutingViewport=new ChartRect(-24,-24,640,400);saved.Validate();
        saved.Annotations[0].TopologyDefaultUnits=null;Assert.Throws<ArgumentException>(()=>saved.Validate());
        saved.Annotations[0].TopologyDefaultUnits=1;Assert.Throws<ArgumentException>(()=>saved.Validate());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ActualSmallTitledAndLegendRoutingKeepsTheLegacyConstantClampDomain(bool legend) {
        var chart=TopologyChart.Create().WithId("constant-router-domain").WithTitle("Actual heading").WithSubtitle("Actual subtitle").WithViewport(90,90,24)
            .AddNode("from","From",25,25,width:10,height:10).AddNode("to","To",45,45,width:10,height:10)
            .AddEdge("edge","from","to",routing:TopologyEdgeRouting.ObstacleAvoidingOrthogonal);
        chart.Legend=legend?TopologyLegend.Default():null;
        var maximum=chart.Viewport.Height-chart.Viewport.Padding-TopologyRenderPrimitives.LegendReservedHeight(chart.Legend,chart.Viewport);
        var domain=TopologyEdgeRouter.RoutingViewport(chart);var edge=chart.Edges[0];edge.SourcePort=TopologyEdgePort.Top;edge.TargetPort=TopologyEdgePort.Top;
        var route=TopologyEdgeRouter.Route(chart,edge,chart.Nodes[0],chart.Nodes[1]);
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output) {
            Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"note-clamp-domain-"+legend+".json"),System.Text.Json.JsonSerializer.Serialize(new{domain,maximum,route.Points,route.Diagnostics}));
        }
        Assert.True(chart.Viewport.Padding+72>maximum);Assert.Equal(maximum,domain.Top);Assert.Equal(0,domain.Height);
        foreach(var value in new[]{-100d,0d,37d,500d})Assert.Equal(Math.Min(maximum,Math.Max(chart.Viewport.Padding+72,value)),Math.Min(domain.Bottom,Math.Max(domain.Top,value)));
        Assert.NotEmpty(route.Points);Assert.All(route.Points,point=>{Assert.True(double.IsFinite(point.X));Assert.True(double.IsFinite(point.Y));});
    }
    private static string AnnotationInk(string svg) => string.Join("\n",XDocument.Parse(svg).Descendants()
        .Where(mark=>((string?)mark.Attribute("data-cfx-role"))?.StartsWith("topology-annotation-",StringComparison.Ordinal)==true)
        .Select(mark=>{var copy=new XElement(mark);copy.Attributes().Where(attribute=>attribute.Name.LocalName=="id"||attribute.Name.LocalName.StartsWith("data-",StringComparison.Ordinal)&&attribute.Name.LocalName!="data-cfx-role").Remove();return copy.ToString(SaveOptions.DisableFormatting);}));
    private static void Capture(string name,PreparedTopology value) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var stem=Path.Combine(output,"note-routing182-"+name);
        File.WriteAllText(stem+".svg",value.ToSvg());File.WriteAllBytes(stem+".png",value.ToPng());File.WriteAllText(stem+".json",value.ToInterchangeEnvelope().ToJson());
    }
}
