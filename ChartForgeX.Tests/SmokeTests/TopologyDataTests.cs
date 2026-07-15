using System;
using ChartForgeX.Raster;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TypedTopologyDataMapsProductRecordsDeterministically() {
        var nodes = new[] {
            new TopologySourceNode("client", "Client", TopologyNodeKind.Endpoint, TopologyHealthStatus.Healthy),
            new TopologySourceNode("api", "API", TopologyNodeKind.Service, TopologyHealthStatus.Warning),
            new TopologySourceNode("db", "Database", TopologyNodeKind.Database, TopologyHealthStatus.Healthy)
        };
        var edges = new[] {
            new TopologySourceEdge("request", "client", "api", "HTTPS"),
            new TopologySourceEdge("query", "api", "db", "SQL")
        };

        var topology = TopologyChart.FromData(
            nodes,
            edges,
            node => node.Id,
            node => node.Label,
            edge => edge.Id,
            edge => edge.Source,
            edge => edge.Target,
            (node, source) => { node.Kind = source.Kind; node.Status = source.Status; },
            (edge, source) => { edge.Label = source.Label; edge.Direction = VisualLinkDirection.Forward; });

        var svg = topology.ToSvg();
        var png = RasterImageDecoder.Decode(topology.ToPng());
        Assert(topology.LayoutMode == TopologyLayoutMode.Layered && topology.Nodes.Count == 3 && topology.Edges.Count == 2, "Typed topology mapping should use deterministic automatic layout and preserve input counts.");
        Assert(topology.Nodes[1].Id == "api" && topology.Nodes[1].Kind == TopologyNodeKind.Service && topology.Edges[1].Label == "SQL", "Typed topology mapping should preserve order and configured product-neutral properties.");
        Assert(svg.Contains(">Client</text>", StringComparison.Ordinal) && svg.Contains(">HTTPS</text>", StringComparison.Ordinal), "Mapped topology data should render to SVG.");
        Assert(png.Width > 0 && png.Height > 0, "Mapped topology data should render with SVG and PNG parity.");
        AssertThrows<ArgumentException>(() => TopologyChart.FromData(nodes, new[] { new TopologySourceEdge("bad", "missing", "api", "Bad") }, node => node.Id, node => node.Label, edge => edge.Id, edge => edge.Source, edge => edge.Target), "Typed topology mapping should reject dangling endpoints before rendering.");
    }

    private sealed class TopologySourceNode {
        public TopologySourceNode(string id, string label, TopologyNodeKind kind, TopologyHealthStatus status) { Id = id; Label = label; Kind = kind; Status = status; }
        public string Id { get; }
        public string Label { get; }
        public TopologyNodeKind Kind { get; }
        public TopologyHealthStatus Status { get; }
    }

    private sealed class TopologySourceEdge {
        public TopologySourceEdge(string id, string source, string target, string label) { Id = id; Source = source; Target = target; Label = label; }
        public string Id { get; }
        public string Source { get; }
        public string Target { get; }
        public string Label { get; }
    }
}
