using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidC4PresentationDepthTests {
    [Theory]
    [InlineData("person", TopologyNodeShape.Actor)] [InlineData("box", TopologyNodeShape.Rounded)]
    [InlineData("rounded", TopologyNodeShape.Rounded)] [InlineData("cylinder", TopologyNodeShape.Cylinder)]
    [InlineData("database", TopologyNodeShape.Cylinder)] [InlineData("db", TopologyNodeShape.Cylinder)]
    [InlineData("queue", TopologyNodeShape.HorizontalCylinder)] [InlineData("pipe", TopologyNodeShape.HorizontalCylinder)]
    [InlineData("component", TopologyNodeShape.Subroutine)]
    public void FiniteSourceShapeKeywordsUseNativeSurfaceAndPortableIdentity(string shape, TopologyNodeShape expected) {
        var document = Parse("C4Context\nSystem(item, \"Item\")\nUpdateElementStyle(item, $shape=\"" + shape + "\")");
        var chart = document.ToTopologyChart(); Assert.Equal(expected, Assert.Single(chart.Nodes).Shape);
        var prepared = chart.Prepare(); var facts = prepared.ToInterchangeEnvelope();
        Assert.Equal(expected, Assert.Single(facts.Nodes).Topology!.Shape);
        Assert.Equal(facts.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
        Assert.Contains("data-node-shape=\"" + expected + "\"", prepared.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void ExplicitShapeThenSpriteThenCommaTagsFollowPinnedPrecedenceAndCanBeCleared() {
        const string input = "C4Context\nSystem(item, \"Item\", $sprite=\"database\", $tags=\"person,component\")";
        var document = Parse(input); var element = Assert.Single(document.Elements);
        Assert.Equal(TopologyNodeShape.Cylinder, Assert.Single(document.ToTopologyChart().Nodes).Shape);
        element.Shape = "component"; Assert.Equal(TopologyNodeShape.Subroutine, Assert.Single(document.ToTopologyChart().Nodes).Shape);
        element.Shape = ""; element.Sprite = ""; Assert.Equal(TopologyNodeShape.Actor, Assert.Single(document.ToTopologyChart().Nodes).Shape);
        var cleared = Parse(input + "\nUpdateElementStyle(item,$sprite=\"\",$shape=\"\")");
        Assert.Equal("", Assert.Single(cleared.Elements).Sprite); Assert.Equal(TopologyNodeShape.Actor, Assert.Single(cleared.ToTopologyChart().Nodes).Shape);
    }

    [Fact]
    public void PaintTechnologyAndExplicitNativeLegendProjectionReachSharedOwners() {
        var document = Parse(Fixture("c4-style-shapes-native")); var chart = document.ToTopologyChart();
        var app = chart.Nodes.Single(node => node.Id == "app"); Assert.Equal("#123456", app.BackgroundColor); Assert.Equal("#ef6543", app.Color);
        Assert.Equal(TopologyNodeShape.Subroutine, app.Shape); Assert.Contains("Native", app.Subtitle);
        Assert.Equal(new[] { "Application tier", "Data tier" }, chart.Legend!.Items.Select(item => item.Label));
        var parsed = Parse("C4Container\nContainer(app, \"App\", \"Before\")\nUpdateElementStyle(app,$techn=\"After\")");
        Assert.Equal("After", Assert.Single(parsed.Elements).Technology); Assert.Contains("After", Assert.Single(parsed.ToTopologyChart().Nodes).Subtitle);
        var facts = chart.Prepare().ToInterchangeEnvelope(); Assert.Equal("component", facts.Nodes.Single(node => node.Id == "app").Extensions["mermaid.shape"]);
    }

    [Theory]
    [InlineData("Rel(user,scope,\"Uses\")")] [InlineData("Rel(scope,user,\"Serves\")")]
    [InlineData("RelIndex(7,user,scope,\"Uses\")")]
    public void DeclaredBoundaryEndpointsRemainRealGroupReferences(string relation) {
        var document = Parse("C4Dynamic\nSystem_Boundary(scope, \"Scope\") {\nSystem(app, \"App\")\n}\nPerson(user, \"User\")\n" + relation);
        var chart = document.ToTopologyChart(); Assert.Equal(2, chart.Nodes.Count); Assert.Single(chart.Groups);
        var edge = Assert.Single(chart.Edges); Assert.True(edge.SourceNodeId == "scope" || edge.TargetNodeId == "scope");
        var facts = chart.Prepare().ToInterchangeEnvelope(); Assert.Single(facts.Groups); Assert.Equal(2, facts.Nodes.Count);
        Assert.Single(facts.Edges); Assert.NotEmpty(chart.Prepare().ToPng());
    }

    [Fact]
    public void TwoRowLimitsUseOneNativeGridRetainSourceOrderAndNestGroups() {
        var document = Parse(Fixture("c4-layout-config-native")); Assert.Equal(2, document.MaximumShapesPerRow); Assert.Equal(1, document.MaximumBoundariesPerRow);
        var chart = document.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.GroupGrid, chart.LayoutMode); Assert.Equal(2, chart.GridLayout!.MaximumNodesPerRow);
        var copy = TopologyLayoutEngine.Clone(chart); Assert.Same(chart.GridLayout, copy.GridLayout);
        var facts = chart.Prepare().ToInterchangeEnvelope(); var a = facts.Nodes.Single(node => node.Id == "a"); var b = facts.Nodes.Single(node => node.Id == "b"); var c = facts.Nodes.Single(node => node.Id == "c");
        Assert.Equal(a.Y, b.Y); Assert.True(a.X + a.Width < b.X); Assert.True(c.Y >= a.Y + a.Height);
        var west = facts.Groups.Single(group => group.Id == "west"); var east = facts.Groups.Single(group => group.Id == "east"); var cloud = facts.Groups.Single(group => group.Id == "cloud");
        Assert.True(west.Y + west.Height < east.Y); Assert.True(cloud.X <= west.X && cloud.Y <= west.Y && cloud.X + cloud.Width >= west.X + west.Width && cloud.Y + cloud.Height >= east.Y + east.Height);
        Assert.Equal("cloud", west.Topology!.ParentGroupId); Assert.Equal("cloud", east.Topology!.ParentGroupId);
        Assert.Equal(facts.ToJson(), VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToJson());
    }

    [Theory]
    [InlineData("0")] [InlineData("-1")] [InlineData("NaN")]
    public void IgnoredRowValuesRetainPreviousLimitsWithLocatedWarnings(string value) {
        var parsed = new MermaidParser().ParseC4("C4Context\nPerson(user, \"User\")\nUpdateLayoutConfig(3,2)\nUpdateLayoutConfig(" + value + ",1)");
        Assert.False(parsed.HasErrors); Assert.Equal(3, parsed.Document!.MaximumShapesPerRow); Assert.Equal(1, parsed.Document.MaximumBoundariesPerRow);
        Assert.Contains(parsed.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Warning && item.Span.Line == 4);
    }

    [Theory]
    [InlineData("2.5",2)] [InlineData("2e3",2)] [InlineData("0x10",16)] [InlineData("2147483648",int.MaxValue)]
    public void SourceRowCountsFollowThePinnedIntegerPrefixMeaning(string value, int expected) {
        var document = Parse("C4Context\nPerson(user, \"User\")\nUpdateLayoutConfig(" + value + ",1)");
        Assert.Equal(expected, document.MaximumShapesPerRow);
    }

    [Fact]
    public void LayoutNamedValueObjectsRetainThePinnedTwoSignaturePositions() {
        var document = Parse("C4Context\nPerson(user, \"User\")\nUpdateLayoutConfig($c4BoundaryInRow=\"1\",$c4ShapeInRow=\"2\")");
        Assert.Equal(1, document.MaximumShapesPerRow); Assert.Equal(2, document.MaximumBoundariesPerRow);
    }

    [Fact]
    public void UnsupportedSpriteShadowAndUnrecognizedCommandsStayLocatedAndNeverFetchArtwork() {
        var parsed = new MermaidParser().ParseC4("C4Context\nSystem(app, \"App\", $sprite=\"https://example.test/remote.svg\")\nUpdateElementStyle(app,$shadowing=\"true\",$shape=\"unknown\",$legendSprite=\"external:logo\")\nShowLegend()\nAddElementTag(critical, \"red\")");
        Assert.False(parsed.HasErrors); Assert.Contains(parsed.Diagnostics, item => item.Severity == MermaidDiagnosticSeverity.Warning && item.Span.Line == 2);
        Assert.Contains(parsed.Document!.RetainedStatements, item => item.Text.StartsWith("ShowLegend", StringComparison.Ordinal));
        var chart = parsed.Document.ToTopologyChart(); Assert.Null(Assert.Single(chart.Nodes).Shape); Assert.Equal("https://example.test/remote.svg", parsed.Document.Elements[0].Sprite);
        Assert.DoesNotContain(XDocument.Parse(chart.Prepare().ToSvg()).Descendants(), node => node.Name.LocalName is "image" or "script" or "foreignObject");
    }

    [Theory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public void ConfiguredDirectFenceAndPortableFactsRetainPaintLabelsAndNativeFrames(bool dark, int width) {
        foreach (var name in new[] { "c4-style-shapes-native", "c4-layout-config-native" }) {
            var source = Fixture(name); var options = Options(dark,width); var direct = MermaidRenderer.Render(source,options); Assert.False(direct.HasErrors);
            var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors); var artifact = direct.Artifact!;
            Assert.Equal(artifact.ToSvg(), Assert.Single(fence.Artifacts).ToSvg()); Assert.Equal(artifact.ToPng(), Assert.Single(fence.Artifacts).ToPng());
            var facts = artifact.ToInterchangeEnvelope(); Assert.Equal(facts.ToJson(), VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
            var image = RasterImageDecoder.Decode(artifact.ToPng()); var size = artifact.MeasureRasterSize(); Assert.Equal((int)size.Width,image.Width); Assert.Equal((int)size.Height,image.Height);
            Capture(artifact,name+"-"+width+(dark?"-dark":"-light"));
        }
    }
    internal static MermaidC4Document Parse(string source) { var parsed = new MermaidParser().ParseC4(source); Assert.False(parsed.HasErrors,string.Join("; ",parsed.Diagnostics.Select(item=>item.Message))); return parsed.Document!; }
    internal static string Fixture(string name) => File.ReadAllText(Path.Combine(TestRepository.Root,"tests","mermaid-conformance","fixtures",name+".mmd"));
    internal static MermaidRenderOptions Options(bool dark,int width) => new() { C4 = new MermaidTopologyRenderOptions { Width=width,Height=800 }, Presentation=new MermaidPresentationOptions { ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light,FontFamily=BrowserProofFonts.Family } };
    internal static void Capture(VisualArtifact artifact,string name) { if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length:>0 } output)return;Directory.CreateDirectory(output);var stem=Path.Combine(output,name);File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".json",artifact.ToInterchangeJson()); }
}
