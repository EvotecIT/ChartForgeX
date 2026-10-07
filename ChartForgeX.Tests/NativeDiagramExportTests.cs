using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeDiagramExportTests {
    [Fact]
    public void NativeTopologyRetainsHostClassHooksAndDescriptiveMetadataWithoutAttributeInjection() {
        var chart = Sample();
        var options = chart.DefaultRenderOptions!.Clone(); options.CssClassPrefix = "report-network";
        chart.Nodes[0].CssClass = "selected-source";
        chart.Nodes[0].BackgroundColor = "#DCFCE7";
        chart.Edges[0].CssClass = "relationship-source";
        chart.Nodes[0].Metadata["note"] = "\" onload=\"unsafe<&";
        options.IncludeDataAttributes = true;
        var svg = XDocument.Parse(chart.ToSvg(options));
        var node = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-node" && (string?)element.Attribute("data-node-id") == "first");
        Assert.Equal("report-network__node selected-source", (string?)node.Attribute("class"));
        Assert.Equal("#DCFCE7", (string?)node.Attribute("data-node-background-color"));
        Assert.Equal(chart.Nodes[0].Metadata["note"], (string?)node.Attribute("data-cfx-meta-note"));
        var edge = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge");
        Assert.Equal("report-network__edge relationship-source", (string?)edge.Attribute("class"));
        Assert.Equal("first", (string?)edge.Attribute("data-source-node-id"));
        Assert.Equal("second", (string?)edge.Attribute("data-target-node-id"));
        Assert.All(svg.Descendants(), element => Assert.Null(element.Attribute("onload")));
    }

    [Fact]
    public void ForceGraphPresentationUsesCanonicalLowInkAndExplicitOpacityWins() {
        var chart = Sample();
        var options = chart.DefaultRenderOptions!.Clone().WithForceGraphStyle();
        var svg = XDocument.Parse(chart.ToSvg(options));
        static XElement Line(XDocument document) => document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
        Assert.Equal((byte)Math.Round(255 * .26), ChartColor.Parse(Line(svg).Attribute("stroke")!.Value).A);
        Assert.Equal(TopologyRenderPrimitives.ForceGraphNormalEdgeStrokeWidth,
            double.Parse(Line(svg).Attribute("stroke-width")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        chart.Edges[0].Opacity = .8;
        var explicitSvg = XDocument.Parse(chart.ToSvg(options));
        Assert.Equal((byte)Math.Round(255 * .8), ChartColor.Parse(Line(explicitSvg).Attribute("stroke")!.Value).A);
        Assert.NotEmpty(chart.ToPng(options));
    }

    [Fact]
    public void TopologyConvenienceAndMutableArtifactUseSameNativeGeometryAndCurrentHostMetadata() {
        var chart = Sample();
        var native = chart.Prepare();
        Assert.Equal(native.ToSvg(), chart.ToSvg()); Assert.Equal(native.ToPng(), chart.ToPng());
        var artifact = chart.ToVisualArtifact(); artifact.Accessibility.Name = "Host description";
        artifact.Metadata["host"] = "retained";
        var json = artifact.ToInterchangeEnvelope();
        var node = json.Nodes.Single(item => item.Id == "first");
        var region = artifact.Regions.Single(item => item.Id == "first");
        Assert.Equal(node.X, region.Bounds!.Value.X); Assert.Equal(node.Y, region.Bounds.Value.Y);
        Assert.Equal(node.Width, region.Bounds.Value.Width); Assert.Equal(node.Height, region.Bounds.Value.Height);
        Assert.Equal("Host description", json.AccessibleName); Assert.Equal("retained", json.Extensions["host"]);
        Assert.True(Assert.Single(json.Edges).ResolvedRoute.Count >= 2);
        Assert.NotNull(json.Edges[0].ResolvedLabelBounds);
        var svg = XDocument.Parse(artifact.ToSvg());
        Assert.Equal("Host description", (string?)svg.Root!.Attribute("aria-label"));
        Assert.Contains("data-node-id=\"first\"", artifact.ToHtmlPage());
        var original = native.ToSvg(); chart.Nodes.Clear(); chart.Edges.Clear();
        Assert.Equal(original, native.ToSvg());
    }

    [Fact]
    public void FlowConvenienceAndMutableInterchangePreserveDecisionGeometryAndBoundedMetadata() {
        var flow = FlowArtifact.Create("flow").AddLane("team", "Team")
            .AddStep("start", "Start", FlowArtifactStepKind.Start, "team")
            .AddStep("approve", "Approve", FlowArtifactStepKind.Decision, "team").AddConnector("start", "approve", "Review");
        var longKey = new string('k', 600); flow.Metadata[longKey] = "model-value";
        var native = flow.Prepare(VisualExportRequest.ForFlow(flow).Context);
        Assert.Equal(native.ToSvg(), flow.ToSvg()); Assert.Equal(native.ToPng(), flow.ToPng());
        var artifact = flow.ToVisualArtifact(); artifact.Metadata[longKey] = "host-value";
        var envelope = artifact.ToInterchangeEnvelope();
        Assert.Equal(FlowArtifactStepKind.Decision, envelope.Nodes.Single(item => item.Id == "approve").Flow!.Kind);
        Assert.True(Assert.Single(envelope.Edges).ResolvedRoute.Count >= 2);
        Assert.Contains("host-value", envelope.Extensions.Values); Assert.DoesNotContain("model-value", envelope.Extensions.Values);
        Assert.All(envelope.Extensions.Keys, key => Assert.InRange(key.Length, 1, 512));
        Assert.Equal(native.Size.Width, envelope.Width); Assert.Equal(native.Size.Height, envelope.Height);
        Assert.Contains("<svg", flow.ToHtmlPage());
    }

    [Fact]
    public void TopologyMotionSamplesNativeResolvedRouteWithoutChangingBaseSceneOrSourceOptions() {
        var chart = Sample();
        var motion = TopologyMotionOptions.RoutePulseForEdges("link"); motion.Loop = false; motion.Progress = 0;
        var options = new TopologyRenderOptions { IncludeLegend = false, Motion = motion, FitContentToViewport = true };
        var start = chart.ToSvg(options);
        motion.Progress = 1;
        var end = chart.ToSvg(options);
        static XElement Marker(string svg) => XDocument.Parse(svg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-motion-marker");
        Assert.NotEqual((string?)Marker(start).Attribute("cx"), (string?)Marker(end).Attribute("cx"));
        Assert.Equal(1, options.Motion!.Progress);
        var plain = options.Clone(); plain.Motion = null;
        var prepared = chart.Prepare(plain); var original = prepared.ToSvg();
        Assert.DoesNotContain("topology-motion-marker", original); Assert.Equal(original, prepared.ToSvg());
        Assert.NotEmpty(chart.ToPng(options));
    }

    private static TopologyChart Sample() {
        var chart = TopologyChart.Create().WithId("native").WithViewport(560, 310).WithTitle("Network");
        chart.Nodes.Add(new TopologyNode { Id = "first", Label = "First", X = 24, Y = 70, Width = 140, Height = 70, Href = "/first" });
        chart.Nodes.Add(new TopologyNode { Id = "second", Label = "Second", X = 300, Y = 70, Width = 140, Height = 70 });
        chart.Edges.Add(new TopologyEdge { Id = "link", SourceNodeId = "first", TargetNodeId = "second", Label = "Connected" });
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true });
        return chart;
    }
}
