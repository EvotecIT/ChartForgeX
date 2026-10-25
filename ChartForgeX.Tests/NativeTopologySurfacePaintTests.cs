using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Post-product contracts; the new public fields must exist before this file is installed.</summary>
public sealed class NativeTopologySurfacePaintTests {
    [Theory]
    [InlineData(false)][InlineData(true)]
    public void CapturedContoursKeepTypedWidthDashAndBothOpacityScopes(bool grouped) {
        var chart=Chart();var node=chart.Nodes[0];node.StrokeWidth=3;node.Opacity=.6;node.SurfaceOpacity=.7;node.DashPattern.AddRange(new double[]{9,4,2,9,4,2});
        if(grouped) {
            chart.Groups.Add(new TopologyGroup{Id="scope",Label="Scope",X=12,Y=12,Width=240,Height=190,StrokeWidth=4,Opacity=.5,SurfaceOpacity=.8});
            chart.Groups[0].DashPattern.AddRange(new double[]{7,3});node.GroupId="scope";
        }
        var prepared=Prepare(chart);var artifact=prepared.ToArtifact("surface-paints",VisualArtifactKind.Topology);Capture(artifact,grouped?"native-grouped-paints":"native-node-paints");
        var facts=artifact.ToInterchangeEnvelope();Assert.Equal(2,facts.Version);var native=facts.Nodes[0].Topology!;
        Assert.Equal(.6,native.Opacity);Assert.Equal(.7,native.SurfaceOpacity);Assert.Equal(3,native.StrokeWidth);Assert.Equal(node.DashPattern,native.DashPattern);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToTopologyChart();
        Assert.Equal(native.StrokeWidth,restored.Nodes[0].StrokeWidth);Assert.Equal(native.Opacity,restored.Nodes[0].Opacity);Assert.Equal(native.SurfaceOpacity,restored.Nodes[0].SurfaceOpacity);Assert.Equal(native.DashPattern,restored.Nodes[0].DashPattern);
        restored.Nodes[0].DashPattern[0]=99;Assert.Equal(9,native.DashPattern[0]);Assert.Equal(9,node.DashPattern[0]);
        var svg=XDocument.Parse(prepared.ToSvg());var nodeGroup=svg.Descendants().Single(item=>(string?)item.Attribute("data-cfx-role")=="topology-node");
        Assert.Equal("0.6",(string?)nodeGroup.Attribute("opacity"));Assert.All(nodeGroup.Descendants().Where(item=>(string?)item.Attribute("data-cfx-role")=="topology-node-surface"),item=>Assert.Equal("0.7",(string?)item.Attribute("opacity")));
        Assert.NotEmpty(prepared.ToPng());
        if(grouped) {
            var group=facts.Groups.Single().Topology!;Assert.Equal(4,group.StrokeWidth);Assert.Equal(.5,group.Opacity);Assert.Equal(.8,group.SurfaceOpacity);Assert.Equal(new double[]{7,3},group.DashPattern);
            // Standard group scene scopes own no descendant nodes, so opacity is never inherited from logical containment.
            Assert.DoesNotContain(nodeGroup.Ancestors(),item=>(string?)item.Attribute("data-cfx-role")=="topology-group");
        }
    }

    [Theory]
    [InlineData(false,128)][InlineData(true,192)]
    public void WholeObjectAndSeparateContourElementsCompositeAtTheirActualScopes(bool surface,int expectedAlpha) {
        var chart=Chart();var node=chart.Nodes[0];node.Shape=TopologyNodeShape.DoubleCircle;node.ShowLabel=false;node.Width=node.Height=100;node.StrokeWidth=10;
        if(surface)node.SurfaceOpacity=.5;else node.Opacity=.5;
        var prepared=Prepare(chart);var artifact=prepared.ToArtifact("opacity-scopes",VisualArtifactKind.Topology);Capture(artifact,surface?"separate-contour-opacity":"whole-node-opacity");
        var facts=artifact.ToInterchangeEnvelope();var n=facts.Nodes[0];var image=prepared.ToRgba(new VisualRenderOptions(supersampling:1));
        // The inner circle stroke overlaps the opaque outer filled circle, away from antialiased edges.
        var x=(int)(n.X!.Value+n.Width!.Value/2+42);var y=(int)(n.Y!.Value+n.Height!.Value/2);
        Assert.InRange(image.Pixels[(y*image.Width+x)*4+3],expectedAlpha-1,expectedAlpha+1);
    }

    [Fact]
    public void OneSurfaceElementIsolatesItsOverlappingFillAndStrokeOnce() {
        var chart=Chart();var node=chart.Nodes[0];node.ShowLabel=false;node.StrokeWidth=20;node.SurfaceOpacity=.5;
        var prepared=Prepare(chart);var artifact=prepared.ToArtifact("surface-element",VisualArtifactKind.Topology);Capture(artifact,"single-element-opacity");
        var n=artifact.ToInterchangeEnvelope().Nodes[0];var image=prepared.ToRgba(new VisualRenderOptions(supersampling:1));
        var x=(int)n.X!.Value+2;var y=(int)(n.Y!.Value+n.Height!.Value/2);
        Assert.InRange(image.Pixels[(y*image.Width+x)*4+3],127,129);
    }

    [Fact]
    public void ContainerOpacityDoesNotDimTheContainedNode() {
        var chart=Chart();chart.Groups.Add(new TopologyGroup{Id="scope",Label="Scope",X=12,Y=12,Width=240,Height=190,Opacity=.5,BackgroundColor="#ff0000",ShowLabel=false});
        var node=chart.Nodes[0];node.GroupId="scope";node.BackgroundColor=node.Color="#0000ff";node.ShowLabel=false;
        var prepared=Prepare(chart);var artifact=prepared.ToArtifact("container-opacity",VisualArtifactKind.Topology);Capture(artifact,"container-opacity-isolates-children");
        var n=artifact.ToInterchangeEnvelope().Nodes[0];var image=prepared.ToRgba(new VisualRenderOptions(supersampling:1));var x=(int)(n.X!.Value+n.Width!.Value/2);var y=(int)(n.Y!.Value+n.Height!.Value/2);var p=(y*image.Width+x)*4;
        Assert.Equal(255,image.Pixels[p+2]);Assert.Equal(255,image.Pixels[p+3]);
    }

    [Theory]
    [InlineData(TopologyLayoutMode.Geographic,TopologyGroupPresentation.Container)]
    [InlineData(TopologyLayoutMode.Manual,TopologyGroupPresentation.Compartment)]
    public void UnsupportedGroupPaintPresentationsFailBeforePrepareAndInPortableData(TopologyLayoutMode mode,TopologyGroupPresentation presentation) {
        var chart=Chart();chart.Groups.Add(new TopologyGroup{Id="scope",Label="Scope",X=12,Y=12,Width=240,Height=190});
        var control=Prepare(chart).ToArtifact("group-paint-boundary",VisualArtifactKind.Topology);Capture(control,"ordinary-group-paint-boundary-"+mode+"-"+presentation);var facts=control.ToInterchangeEnvelope();
        chart.LayoutMode=mode;chart.Groups[0].Presentation=presentation;chart.Groups[0].Opacity=.5;
        var issue=Assert.Single(new TopologyChartValidator().ValidateSurfacePaintInputs(chart).Errors);Assert.Equal("group-surface-paint-presentation",issue.Code);
        Assert.Throws<TopologyValidationException>(()=>Prepare(chart));
        facts.Topology!.LayoutMode=mode;facts.Groups[0].Topology!.Presentation=presentation;facts.Groups[0].Topology!.Opacity=.5;
        Assert.Throws<NotSupportedException>(()=>facts.Validate());
    }

    [Fact]
    public void MutablePatternsAreBoundedByTheSameNativeAndPortableRule() {
        var chart=Chart();var node=chart.Nodes[0];Capture(Prepare(chart).ToArtifact("paint-before-invalid",VisualArtifactKind.Topology),"ordinary-paint-before-invalid");node.DashPattern.AddRange(new double[]{1,2,3});
        Assert.False(new TopologyChartValidator().ValidateSurfacePaintInputs(chart).IsValid);Assert.Throws<TopologyValidationException>(()=>Prepare(chart));
        node.DashPattern.Clear();var facts=Prepare(chart).ToArtifact("paint-invalid",VisualArtifactKind.Topology).ToInterchangeEnvelope();facts.Nodes[0].Topology!.DashPattern.AddRange(Enumerable.Repeat(1d,66));
        Assert.Throws<ArgumentException>(()=>facts.Validate());Assert.Throws<ArgumentOutOfRangeException>(()=>node.StrokeWidth=0);Assert.Throws<ArgumentOutOfRangeException>(()=>node.Opacity=double.NaN);Assert.Throws<ArgumentOutOfRangeException>(()=>node.SurfaceOpacity=1.1);
    }

    [Fact]
    public void NullSurfaceFieldsKeepAnExplicitOrdinaryControlCapture() {
        var artifact=Prepare(Chart()).ToArtifact("surface-default",VisualArtifactKind.Topology);Capture(artifact,"ordinary-native-surface-paints");
        var facts=artifact.ToInterchangeEnvelope();var n=facts.Nodes[0].Topology!;Assert.Null(n.StrokeWidth);Assert.Null(n.Opacity);Assert.Null(n.SurfaceOpacity);Assert.Empty(n.DashPattern);
        Assert.DoesNotContain("surfaceOpacity",facts.ToJson());
    }

    [Fact]
    public void ExplicitWideStrokesParticipateInFittingAndScaleExactlyOnce() {
        var chart=Chart();chart.Nodes[0].StrokeWidth=180;chart.Nodes[0].ShowLabel=false;
        var prepared=chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(300,250),0),frame:new VisualFrame(showLegend:false,transparentBackground:true)),
            new TopologyRenderOptions{FitContentToViewport=true,CanvasSurfaceStyle=TopologyCanvasSurfaceStyle.Plain});
        var artifact=prepared.ToArtifact("wide-surface",VisualArtifactKind.Topology);Capture(artifact,"wide-stroke-fit");var facts=artifact.ToInterchangeEnvelope();var n=facts.Nodes[0];var half=n.Topology!.StrokeWidth!.Value/2;
        Assert.True(n.X!.Value-half>=-.01);Assert.True(n.Y!.Value-half>=-.01);Assert.True(n.X.Value+n.Width!.Value+half<=facts.Width!.Value+.01);Assert.True(n.Y.Value+n.Height!.Value+half<=facts.Height!.Value+.01);
    }

    [Fact]
    public void LiveForceGraphCannotSilentlyDropNativeSurfacePaints() {
        var chart=Chart();chart.Nodes[0].StrokeWidth=3;Capture(chart.ToVisualArtifact(),"native-surface-before-live-host-rejection");
        Assert.Throws<NotSupportedException>(()=>chart.ToSvg(new TopologyRenderOptions{EnableHtmlInteractions=true,EnableHtmlForceGraphControls=true}));
    }

    [Theory]
    [InlineData(TopologyNodeShape.Actor, TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeShape.DoubleCircle, TopologyNodeDisplayMode.Icon)]
    public void CapturedFigureSurfaceWidthsAndDashesRetainOneOriginalUnitProjection(TopologyNodeShape shape, TopologyNodeDisplayMode mode) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(360, 260, 16).WithLegend(null);
        foreach (var x in new[] { 100d, 800d }) {
            var node = new TopologyNode { Id = "node-" + x, X = x, Y = 80, Width = 140, Height = 150,
                Shape = shape, DisplayMode = mode, PreserveDisplayModeSize = true, ShowLabel = false,
                ShowStatusBadge = false, StrokeWidth = 4, Color = "#173B59", BackgroundColor = "#88CCEE" };
            node.DashPattern.AddRange(new[] { 9d, 3d }); chart.Nodes.Add(node);
        }
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 260), 16),
            frame: new VisualFrame(showLegend: false, transparentBackground: true));
        var ordinaryContext = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(1200, 400), 16),
            frame: new VisualFrame(showLegend: false, transparentBackground: true));
        var ordinary = chart.Prepare(ordinaryContext, new TopologyRenderOptions { FitContentToViewport = false, CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Plain })
            .ToArtifact("unit-control", VisualArtifactKind.Topology);
        var fitted = chart.Prepare(context, new TopologyRenderOptions { FitContentToViewport = true, CanvasSurfaceStyle = TopologyCanvasSurfaceStyle.Plain })
            .ToArtifact("unit-fitted", VisualArtifactKind.Topology);
        var facts = fitted.ToInterchangeEnvelope();
        var restoredChart = VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToTopologyChart();
        var restored = restoredChart.Prepare().Visual.ToArtifact(fitted.Id, fitted.Kind);
        Capture(ordinary, "unit-before-fit-" + mode); Capture(fitted, "unit-fitted-" + mode); Capture(restored, "unit-restored-" + mode);
        var factor = facts.Nodes[0].Width!.Value / ordinary.ToInterchangeEnvelope().Nodes[0].Width!.Value;
        Assert.InRange(factor, 0.0001, .99);
        static XElement Surface(string svg) => XDocument.Parse(svg).Descendants()
            .First(element => (string?)element.Attribute("data-cfx-role") == "topology-node-surface" && element.Attribute("stroke-width") != null);
        static double Number(string value) => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        static string Format(double value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        var raw = Surface(ordinary.ToSvg()); var projected = Surface(fitted.ToSvg());
        Assert.Equal(Format(Number(raw.Attribute("stroke-width")!.Value) * factor), projected.Attribute("stroke-width")!.Value);
        var rawDash = raw.Attribute("stroke-dasharray")!.Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var projectedDash = projected.Attribute("stroke-dasharray")!.Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(rawDash.Length, projectedDash.Length);
        for (var index = 0; index < rawDash.Length; index++)
            Assert.Equal(Number(rawDash[index]) * factor, Number(projectedDash[index]), 12);
        // Envelope identity is authoritative, and prepared scene restoration uses Manual/fit:false metadata.
        Assert.Equal(facts.Id, restoredChart.Id); Assert.Equal("unit-fitted", restoredChart.Id);
        Assert.Equal(TopologyLayoutMode.Manual, restoredChart.LayoutMode);
        // Resolved prepared-coordinate fitting is asserted on the emitted metadata below;
        // an absent optional DefaultRenderOptions is a valid restored chart policy.
        var directRoot = Assert.Single(XDocument.Parse(fitted.ToSvg()).Descendants(),
            element => (string?)element.Attribute("data-cfx-role") == "topology");
        var restoredRoot = Assert.Single(XDocument.Parse(restored.ToSvg()).Descendants(),
            element => (string?)element.Attribute("data-cfx-role") == "topology");
        Assert.Equal("topology", (string?)directRoot.Attribute("data-chart-id"));
        Assert.Equal(facts.Id, (string?)restoredRoot.Attribute("data-chart-id"));
        Assert.Equal("true", (string?)directRoot.Attribute("data-fit-content-to-viewport"));
        Assert.Equal("false", (string?)restoredRoot.Attribute("data-fit-content-to-viewport"));
        Assert.Equal(TopologyStaticPaintObservation.Paint(fitted.ToSvg()), TopologyStaticPaintObservation.Paint(restored.ToSvg()));
        var directPng = fitted.ToPng(); var restoredPng = restored.ToPng();
        Assert.Equal(directPng, restoredPng);
        var directPixels = PngReader.Decode(directPng); var restoredPixels = PngReader.Decode(restoredPng);
        Assert.Equal(directPixels.Width, restoredPixels.Width); Assert.Equal(directPixels.Height, restoredPixels.Height);
        Assert.Equal(directPixels.Pixels, restoredPixels.Pixels);
        Assert.All(chart.Nodes, node => { Assert.Equal(4, node.StrokeWidth); Assert.Equal(new[] { 9d, 3d }, node.DashPattern); });
    }

    private static TopologyChart Chart() {
        var chart=TopologyChart.Create().WithTitle("").WithViewport(300,250,0).WithLegend(null);
        chart.Nodes.Add(new TopologyNode{Id="node",Label="Node",X=60,Y=60,Width=100,Height=80,Shape=TopologyNodeShape.Rectangle,BackgroundColor="#ff0000",Color="#ff0000",ShowStatusBadge=false,PreserveDisplayModeSize=true});return chart;
    }
    private static PreparedVisual Prepare(TopologyChart chart)=>chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(300,250),0),
        frame:new VisualFrame(showLegend:false,transparentBackground:true)),new TopologyRenderOptions{FitContentToViewport=false,CanvasSurfaceStyle=TopologyCanvasSurfaceStyle.Plain});
    private static void Capture(VisualArtifact artifact,string name) {
        var output=Environment.GetEnvironmentVariable("CFX_SURFACE_PAINT_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(output))return;Directory.CreateDirectory(output);var stem=Path.Combine(output,name);
        File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".json",artifact.ToInterchangeJson());
    }
}
