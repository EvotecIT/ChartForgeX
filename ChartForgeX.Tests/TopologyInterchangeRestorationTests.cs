using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyInterchangeRestorationTests {
    [Theory]
    [InlineData(false, 390)] [InlineData(true, 1000)]
    public void PublicJsonLoadRestoresEnvelopeIdentitiesAndGraphFactsThenEditsAndRendersDetachedNativeArtifacts(bool dark, int width) {
        var source = Source(dark, width);
        var artifact = source.ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid);
        artifact.Metadata["host.source"] = "Inert <source> & original text";
        var json = artifact.ToInterchangeJson(); var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        var imported = envelope.ToTopologyChart();
        Assert.Equal(TopologyLayoutMode.Manual, imported.LayoutMode);
        Assert.Equal(envelope.Id, imported.Id); Assert.Equal(envelope.Width, imported.Viewport.Width); Assert.Equal(envelope.Height, imported.Viewport.Height);
        Assert.Equal(envelope.Topology!.LayoutDirection, imported.LayoutDirection);
        Assert.Equal(envelope.Nodes.Select(node => node.Id), imported.Nodes.Select(node => node.Id));
        Assert.Equal(envelope.Groups.Select(group => group.Id), imported.Groups.Select(group => group.Id));
        Assert.Equal(envelope.Edges.OrderBy(edge => edge.Order).Select(edge => (edge.Id, edge.SourceId, edge.TargetId)),
            imported.Edges.Select(edge => (edge.Id, edge.SourceNodeId, edge.TargetNodeId)));
        var node = imported.Nodes.Single(item => item.Id == "source"); var portable = envelope.Nodes.Single(item => item.Id == "source");
        Assert.Equal("not-an-identity", node.Metadata["chartforgex.sourceId"]);
        Assert.Equal(portable.Extensions.OrderBy(pair => pair.Key), node.Metadata.OrderBy(pair => pair.Key));
        Assert.Equal((portable.X, portable.Y, portable.Width, portable.Height), ((double?)node.X, (double?)node.Y, (double?)node.Width, (double?)node.Height));
        Assert.True(node.PreserveDisplayModeSize); Assert.Equal(portable.Topology!.PaletteIndex, node.PaletteIndex);
        Assert.Equal(portable.Topology.Artwork!.SvgBody, node.Artwork!.SvgBody);
        Assert.Equal(portable.Ports.Single().Id, node.Ports.Single().Id); Assert.Equal("outbound", node.Ports[0].Metadata["meaning"]);
        Assert.Equal("queued", node.Metrics["phase"]); Assert.Equal("Complete <value>", node.Details[0].Value);
        Assert.Equal("detail", node.Details[0].Metadata["kind"]); Assert.Equal("600", node.Details[0].TextStyle!.FontWeight);
        var edge = imported.Edges.Single(); var portableEdge = envelope.Edges.Single();
        Assert.Equal(portableEdge.Topology!.Waypoints.Select(point => (point.X, point.Y)), edge.Waypoints.Select(point => (point.X, point.Y)));
        Assert.Equal(portableEdge.Topology.DashPattern, edge.DashPattern); Assert.Equal(0, edge.RouteLane);
        Assert.Equal(portableEdge.Topology.LabelAnchor!.X, edge.LabelAnchorX); Assert.Equal("source", edge.LabelAnchorNodeId);
        Assert.Equal("source", edge.SourceNodeId); Assert.Equal("target", edge.TargetNodeId); Assert.Equal("send", edge.SourcePortId);
        Assert.Equal("opaque", edge.Metadata["route.fact"]); Assert.Equal("41", edge.Metrics["latency"]);
        Assert.Equal(portableEdge.Topology.StrokeWidth, edge.StrokeWidth); Assert.Equal(portableEdge.Topology.Opacity, edge.Opacity);
        Assert.Equal(portableEdge.Topology.SourceMarker, edge.SourceMarker); Assert.Equal(portableEdge.Topology.TargetMarker, edge.TargetMarker);
        Assert.Equal("path", imported.Scenarios.Single().Metadata["kind"]); Assert.Equal("edge-step", imported.Scenarios[0].Steps[1].Metadata["kind"]);
        Assert.Equal(envelope.Scenarios[0].Steps.Select(step => (step.Kind, step.TargetId)), imported.Scenarios[0].Steps.Select(step => (step.Kind, step.Id)));
        Assert.Equal(envelope.Annotations.Single().Text, imported.Annotations.Single().Text);
        Assert.Equal("source", imported.Annotations[0].TargetId); Assert.Equal(TopologyAnnotationSide.Left, imported.Annotations[0].Side);
        Assert.Equal(envelope.Presentation!.Legend!.Items[0].Label, imported.Legend!.Items[0].Label);
        var roundTripTheme = imported.ToVisualArtifact().ToInterchangeEnvelope().Presentation!.Theme!;
        Assert.Equal(envelope.Presentation.Theme!.Background, roundTripTheme.Background);
        Assert.Equal(envelope.Presentation.Theme.Card, roundTripTheme.Card); Assert.Equal(envelope.Presentation.Theme.Healthy, roundTripTheme.Healthy);
        Assert.Equal(envelope.Presentation.Theme.FontFamily, imported.Theme.Typography.Family);
        Assert.Equal("Imported request graph", imported.Accessibility.Name); Assert.Equal("en", imported.Accessibility.Language);
        Assert.Equal(VisualArtifactSourceLanguage.Mermaid, envelope.SourceLanguage); Assert.Equal("Inert <source> & original text", envelope.Extensions["host.source"]);
        Assert.Equal(json, envelope.ToJson());

        var before = imported.ToSvg(); var beforePng = imported.ToPng();
        node.Label = "Edited source"; node.Details[0].Value = "Delivered"; node.Metadata["changed"] = "local";
        node.Ports[0].Metadata["meaning"] = "changed"; node.Details[0].TextStyle!.FontWeight = "400";
        node.Artwork!.SvgBody = "<circle cx=\"12\" cy=\"12\" r=\"6\"/>";
        // Artwork mode keeps its caption outside the artwork; explicitly choose a factual card for editable detail rows.
        node.DisplayMode = TopologyNodeDisplayMode.Card;
        imported.Scenarios[0].Steps[1].Metadata["kind"] = "local"; imported.Legend!.Items[0].Label = "Edited legend";
        var svg = imported.ToSvg(); var png = imported.ToPng();
        Assert.NotEqual(before, svg); Assert.NotEqual(beforePng, png); Assert.Contains("Edited source", svg); Assert.Contains("Delivered", svg);
        Assert.Equal(json, envelope.ToJson()); Assert.Equal("Request source", source.Nodes[0].Label);
        var xml = XDocument.Parse(svg); Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject");
        var image = RasterImageDecoder.Decode(png); Assert.True(image.Width >= width); Assert.True(image.Height > 0);
        Save("topology-import-" + width + (dark ? "-dark" : "-light"), svg, png);
    }

    [Fact]
    public void ResolvedSamplesRemainDiagnosticWhileExplicitRouteHintsAndManualBoundsRetainTheirOwnMeaning() {
        var envelope = Source(false, 1000).ToVisualArtifact().ToInterchangeEnvelope();
        var authoredWaypoints = envelope.Edges[0].Topology!.Waypoints.Select(point => (point.X, point.Y)).ToArray();
        Assert.NotEmpty(envelope.Edges[0].ResolvedRoute);
        envelope.Edges[0].ResolvedRoute[0].X = -123456;
        envelope.Edges[0].ResolvedLabelBounds = new ChartRect(-42, -42, 2, 3);
        var chart = envelope.ToTopologyChart();
        Assert.Equal(authoredWaypoints, chart.Edges[0].Waypoints.Select(point => (point.X, point.Y)));
        Assert.DoesNotContain(chart.Edges[0].Waypoints, point => point.X == -123456);
        chart.WithEdgeRouteLane(chart.Edges[0].Id, 17);
        var edited = chart.ToVisualArtifact().ToInterchangeEnvelope();
        Assert.Equal(17, edited.Edges[0].Topology!.RouteLane); Assert.Equal(0, envelope.Edges[0].Topology!.RouteLane);
        Assert.Equal(-123456, envelope.Edges[0].ResolvedRoute[0].X);
        chart.LayoutMode = TopologyLayoutMode.Layered;
        Assert.Equal(TopologyLayoutMode.Layered, chart.ToVisualArtifact().ToInterchangeEnvelope().Topology!.LayoutMode);
    }

    [Fact]
    public void NormalizedEnvelopeIdsAreRestoredExactlyWithoutTreatingOpaqueSourceIdMetadataAsAnIdentityInstruction() {
        var source = Source(false, 1000); source.Groups[0].Id = "source";
        foreach (var node in source.Nodes) node.GroupId = "source";
        var envelope = source.ToVisualArtifact().ToInterchangeEnvelope();
        var restored = envelope.ToTopologyChart();
        Assert.Equal("source", restored.Groups[0].Id); Assert.NotEqual("source", restored.Nodes[0].Id);
        Assert.Equal(envelope.Nodes[0].Id, restored.Nodes[0].Id);
        Assert.Equal(envelope.Nodes[0].Extensions.OrderBy(pair => pair.Key), restored.Nodes[0].Metadata.OrderBy(pair => pair.Key));
        Assert.Equal(restored.Nodes[0].Id, restored.Edges[0].SourceNodeId);
        Assert.Equal(restored.Nodes[0].Id, restored.Scenarios[0].Steps[0].Id);
        Assert.Equal(restored.Nodes[0].Id, restored.Annotations[0].TargetId);
    }

    [Fact]
    public void NestedCompartmentPresentationAndGroupEdgeEndpointsRestoreWithoutFlatteningTheirIdentities() {
        var source = TopologyChart.Create().WithViewport(1000, 500);
        source.Groups.Add(new TopologyGroup { Id = "outer", Label = "Transaction", X = 20, Y = 20, Width = 700, Height = 280 });
        source.Groups.Add(new TopologyGroup { Id = "before", Label = "Before", ParentGroupId = "outer", Presentation = TopologyGroupPresentation.Compartment,
            LayoutDirection = TopologyLayoutDirection.LeftToRight, X = 25, Y = 60, Width = 340, Height = 230, BackgroundColor = "#EEF2FF", BorderColor = "#2563EB" });
        source.Groups.Add(new TopologyGroup { Id = "after", Label = "After", ParentGroupId = "outer", Presentation = TopologyGroupPresentation.Compartment,
            X = 365, Y = 60, Width = 340, Height = 230 });
        source.Groups[1].Metadata["compartment"] = "raw value";
        source.Nodes.Add(new TopologyNode { Id = "work", Label = "Work", GroupId = "before", X = 65, Y = 110, Width = 140, Height = 70 });
        source.Nodes.Add(new TopologyNode { Id = "done", Label = "Done", GroupId = "after", X = 410, Y = 110, Width = 140, Height = 70 });
        source.Edges.Add(new TopologyEdge { Id = "crossing", SourceNodeId = "before", TargetNodeId = "after", Direction = VisualLinkDirection.Forward });
        var envelope = source.ToVisualArtifact().ToInterchangeEnvelope(); var imported = envelope.ToTopologyChart();
        Assert.Equal(3, imported.Groups.Count); Assert.Equal("outer", imported.Groups[1].ParentGroupId);
        Assert.Equal(TopologyGroupPresentation.Compartment, imported.Groups[1].Presentation);
        Assert.Equal(TopologyLayoutDirection.LeftToRight, imported.Groups[1].LayoutDirection);
        Assert.Equal(envelope.Groups[1].BackgroundColor, imported.Groups[1].BackgroundColor);
        Assert.Equal("raw value", imported.Groups[1].Metadata["compartment"]);
        Assert.Equal("before", imported.Edges[0].SourceNodeId); Assert.Equal("after", imported.Edges[0].TargetNodeId);
        imported.Groups[1].Metadata["compartment"] = "edited"; Assert.Equal("raw value", envelope.Groups[1].Extensions["compartment"]);
        Assert.NotEmpty(imported.ToPng()); Assert.Contains("Transaction", imported.ToSvg());
    }

    internal static TopologyChart Source(bool dark, int width) {
        var chart = TopologyChart.Create().WithId("portable-topology").WithTitle("Editable request route")
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithViewport(width, 520).WithRenderOptions(new TopologyRenderOptions { IncludeLegend = true, WrapNodeLabels = true });
        chart.Accessibility.Name = "Imported request graph"; chart.Accessibility.Language = "en";
        chart.Groups.Add(new TopologyGroup { Id = "region", Label = "Service region", X = 24, Y = 30, Width = 640, Height = 240,
            Color = "#2563EB", Status = TopologyHealthStatus.Healthy });
        chart.Groups[0].Metadata["raw.group"] = "<opaque>";
        var node = new TopologyNode { Id = "source", Label = "Request source", Kind = TopologyNodeKind.Server,
            X = 48, Y = 85, Width = 210, Height = 110, GroupId = "region", Status = TopologyHealthStatus.Healthy,
            PaletteIndex = 2, Href = "https://example.invalid/source", ShowStatusBadge = false,
            Artwork = TopologyIconArtwork.InlineSvg("<rect x=\"5\" y=\"5\" width=\"14\" height=\"14\"/>") };
        node.Metadata["chartforgex.sourceId"] = "not-an-identity"; node.Metrics["phase"] = "queued";
        node.Ports.Add(new TopologyNodePort { Id = "send", Side = TopologyEdgePort.Right, Offset = .4, Label = "Send" });
        node.Ports[0].Metadata["meaning"] = "outbound";
        node.Details.Add(new TopologyNodeDetail { Label = "Payload", Value = "Complete <value>", Status = TopologyHealthStatus.Warning,
            TextStyle = new TextStyleOverride { FontWeight = "600" } }); node.Details[0].Metadata["kind"] = "detail";
        chart.Nodes.Add(node); chart.Nodes.Add(new TopologyNode { Id = "target", Label = "Worker", GroupId = "region", X = 410, Y = 85, Width = 190, Height = 110,
            ShowStatusBadge = false, Shape = TopologyNodeShape.Rounded });
        var edge = new TopologyEdge { Id = "request", SourceNodeId = "source", TargetNodeId = "target", SourcePortId = "send",
            Kind = TopologyEdgeKind.Generic, Direction = VisualLinkDirection.Forward, Routing = TopologyEdgeRouting.Orthogonal,
            Label = "Dispatch", StrokeWidth = 2.5, Opacity = .8, SourceMarker = TopologyMarkerKind.Circle, TargetMarker = TopologyMarkerKind.Arrow,
            LabelAnchorNodeId = "source", LabelAnchorX = 290, LabelAnchorY = 150, HasLabelAnchorOverride = true };
        edge.DashPattern.AddRange(new[] { 6d, 2d }); edge.Waypoints.Add(new ChartPoint(310, 125)); edge.Waypoints.Add(new ChartPoint(350, 125));
        edge.Metadata["route.fact"] = "opaque"; edge.Metrics["latency"] = "41"; chart.Edges.Add(edge); chart.WithEdgeRouteLane("request", 0);
        chart.Legend = TopologyLegend.Create("Route key").AddNodeKind("Service", TopologyNodeKind.Server).AddEdgeKind("Request", TopologyEdgeKind.Generic);
        var scenario = new TopologyScenario { Id = "delivery", Label = "Delivery path", LoopPlayback = true, AutoPlay = false, Spotlight = true };
        scenario.Metadata["kind"] = "path"; scenario.Steps.Add(new TopologyScenarioStep { Id = "source", Kind = TopologyScenarioStepKind.Node });
        scenario.Steps.Add(new TopologyScenarioStep { Id = "request", Kind = TopologyScenarioStepKind.Edge, DurationMilliseconds = 1200 });
        scenario.Steps[1].Metadata["kind"] = "edge-step"; chart.Scenarios.Add(scenario);
        chart.Annotations.Add(new TopologyAnnotation("note", "Retained note", "source", TopologyAnnotationSide.Left));
        return chart;
    }

    internal static void Save(string name, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(directory, name + ".png"), png);
    }
}
