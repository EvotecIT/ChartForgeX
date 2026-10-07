using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyDuplicateEdgeTests {
    [Fact]
    public void AuthoredEdgeIdMatchesEveryInstanceWhilePortableIdsAndResolvedGeometryStayDistinct() {
        var chart = Diagram();
        var prepared = chart.Prepare();
        var svg = XDocument.Parse(prepared.ToSvg());
        var edges = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-edge").ToArray();
        Assert.Equal(2, edges.Length);
        Assert.All(edges, edge => Assert.Equal("dup", (string?)edge.Attribute("data-edge-id")));
        Assert.Equal(new[] { "First leg", "Second leg" }, edges.Select(edge => (string?)edge.Attribute("data-edge-label")));
        var ids = svg.Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToInterchangeEnvelope().ToJson());
        Assert.Equal(2, envelope.Edges.Select(edge => edge.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(new[] { "a", "b" }, envelope.Edges.Select(edge => edge.SourceId));
        Assert.Equal(new[] { "b", "c" }, envelope.Edges.Select(edge => edge.TargetId));
        Assert.NotEqual(envelope.Edges[0].ResolvedRoute[0].X, envelope.Edges[1].ResolvedRoute[0].X);
        Assert.All(envelope.Edges, edge => Assert.NotNull(edge.ResolvedLabelBounds));
        var scenario = Assert.Single(envelope.Scenarios);
        Assert.Equal(envelope.Edges.Select(edge => edge.Id), scenario.Steps.Select(step => step.TargetId));
        Assert.NotEmpty(prepared.ToPng());
        Assert.All(chart.Edges, edge => Assert.Equal("dup", edge.Id));
    }

    [Fact]
    public void AuthoredIdMotionTraversesEveryMatchingNativeRouteInSourceOrder() {
        var chart = Diagram();
        var sourceOptions = new TopologyRenderOptions { IncludeLegend = false };
        var compiler = new VisualTopologyCompiler(chart, VisualExportRequest.ForTopology(chart, sourceOptions).Context, sourceOptions, naturalSize: true);
        var prepared = compiler.Compile();
        var options = new TopologyRenderOptions { IncludeLegend = false, Motion = TopologyMotionOptions.RoutePulseForEdges("dup") };
        var plan = compiler.MotionPlan(options)!;
        Assert.Equal(2, plan.Entries.Count);
        var envelope = prepared.SemanticInterchange!;
        for (var index = 0; index < plan.Entries.Count; index++) {
            Assert.Equal(envelope.Edges[index].ResolvedRoute[0].X, plan.Entries[index].Points[0].X);
            Assert.Equal(envelope.Edges[index].ResolvedRoute.Last().X, plan.Entries[index].Points.Last().X);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnimatedSvgUsesDetachedCompiledRoutesAndScopedReferences(bool loop) {
        var chart = Diagram();
        var motion = TopologyMotionOptions.RoutePulseForEdges("dup"); motion.Loop = loop;
        var options = new TopologyRenderOptions { IncludeLegend = false, IdScope = "animation", Motion = motion };
        var prepared = chart.Prepare(options);
        var svg = prepared.ToSvg(); var xml = XDocument.Parse(svg);
        var routes = xml.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-motion-route").ToArray();
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(envelope.Edges.Count, routes.Length);
        for (var i = 0; i < routes.Length; i++) {
            var points = Assert.Single(ChartMapPathParser.ParseSubpaths((string)routes[i].Attribute("d")!, 1)).Points;
            Assert.Equal(envelope.Edges[i].ResolvedRoute.Count, points.Count);
            for (var j = 0; j < points.Count; j++) {
                Assert.Equal(envelope.Edges[i].ResolvedRoute[j].X, points[j].X, 3);
                Assert.Equal(envelope.Edges[i].ResolvedRoute[j].Y, points[j].Y, 3);
            }
        }
        var tour = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "topology-motion-tour-path");
        var reference = Assert.Single(xml.Descendants(), element => element.Name.LocalName == "mpath");
        Assert.Equal("#" + (string?)tour.Attribute("id"), (string?)reference.Attribute("href"));
        Assert.StartsWith("animation-", (string?)tour.Attribute("id"), StringComparison.Ordinal);
        Assert.All(xml.Descendants().Where(element => element.Name.LocalName is "animate" or "animateMotion"), element => {
            Assert.Equal(loop ? "indefinite" : "1", (string?)element.Attribute("repeatCount"));
            Assert.Equal(loop ? "remove" : "freeze", (string?)element.Attribute("fill"));
        });
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName == "script");
        motion.Loop = !loop; motion.EdgeIds.Clear(); chart.Nodes.Clear(); chart.Edges.Clear();
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Fact]
    public void AuthoredAccessibilityRemainsSubjectToPortableValidationWithoutRestrictingStaticOutput() {
        var chart = Diagram().WithAccessibility(accessibility => accessibility.Description = new string('x', VisualArtifactInterchangeValidation.MaximumTextCharacters + 1));
        var prepared = chart.Prepare();
        Assert.Contains(new string('x', 128), prepared.ToSvg(), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => prepared.ToInterchangeEnvelope());
    }

    private static TopologyChart Diagram() => TopologyChart.Create().WithId("duplicate-native").WithViewport(700, 300, 20).WithLegend(null)
        .AddNode("a", "A", 40, 80, width: 80, height: 48)
        .AddNode("b", "B", 280, 80, width: 80, height: 48)
        .AddNode("c", "C", 520, 80, width: 80, height: 48)
        .AddEdge("dup", "a", "b", "First leg", routing: TopologyEdgeRouting.Straight)
        .AddEdge("dup", "b", "c", "Second leg", routing: TopologyEdgeRouting.Straight)
        .AddScenario("both", "Both legs", scenario => scenario.AddEdgeStep("dup"));
}
