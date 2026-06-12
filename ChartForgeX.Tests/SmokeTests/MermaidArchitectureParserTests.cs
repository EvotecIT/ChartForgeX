using System;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesArchitectureGroupsServicesJunctionsAndEdges() {
        const string source = @"architecture-beta
group api(cloud)[API]
group private(server)[Private API] in api
service gateway(internet)[Gateway] in api
service db(database)[Database] in private
junction split in api
gateway:R --> L:split
split:R --> L:db{group}";

        var result = new MermaidParser().ParseArchitecture(source);

        Assert(!result.HasErrors, "Mermaid architecture parser should parse groups, services, junctions, and edges: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid architecture parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Architecture, "Mermaid architecture parser should produce an architecture document.");
        Assert(document.Groups.Count == 2 && document.Groups[1].ParentId == "api", "Mermaid architecture parser should parse nested group declarations.");
        Assert(document.Services.Count == 2 && document.Services[1].Icon == "database" && document.Services[1].GroupId == "private", "Mermaid architecture parser should parse service icons and group membership.");
        Assert(document.Junctions.Count == 1 && document.Junctions[0].GroupId == "api", "Mermaid architecture parser should parse junction declarations.");
        Assert(document.Edges.Count == 2, "Mermaid architecture parser should parse architecture edges.");
        Assert(document.Edges[0].Left.Id == "gateway" && document.Edges[0].Left.Side == "R", "Mermaid architecture parser should preserve left endpoint side metadata.");
        Assert(document.Edges[1].Right.Id == "db" && document.Edges[1].Right.GroupBoundary, "Mermaid architecture parser should preserve group-boundary edge modifiers.");
    }

    private static void MermaidArchitectureConvertsToTopologyArtifactAndRenders() {
        const string source = @"architecture-beta
group public_api(cloud)[Public API]
service server(server)[Server] in public_api
service database(database)[Database] in public_api
server:R --> L:database";

        var result = new MermaidParser().ParseArchitecture(source);
        Assert(!result.HasErrors, "Mermaid architecture parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid architecture parser should produce a document.");
        var topology = document.ToTopologyChart(new MermaidTopologyRenderOptions { Id = "api-architecture" });
        Assert(topology.Id == "api-architecture", "Mermaid architecture conversion should preserve caller-provided ids.");
        Assert(topology.Groups.Count == 1 && topology.Groups[0].Metadata["mermaid.icon"] == "cloud", "Mermaid architecture conversion should map groups and icon metadata.");
        Assert(topology.Nodes.Count == 2 && topology.Nodes[1].Kind == TopologyNodeKind.Database, "Mermaid architecture conversion should map services into topology nodes with icon-informed node kinds.");
        Assert(topology.Edges.Count == 1 && topology.Edges[0].Metadata["mermaid.source.side"] == "R" && topology.Edges[0].Metadata["mermaid.target.side"] == "L", "Mermaid architecture conversion should preserve edge side metadata.");

        var artifact = document.ToVisualArtifact(new MermaidTopologyRenderOptions { Id = "api-architecture" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid architecture visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is TopologyChart, "Mermaid architecture visual artifact should carry the topology model.");
        Assert(artifact.Metadata["mermaid.groups"] == "1" && artifact.Metadata["mermaid.services"] == "2" && artifact.Metadata["mermaid.edges"] == "1", "Mermaid architecture artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(TopologyChart), "Mermaid architecture artifacts should expose the topology render model.");

        var svg = document.ToSvg(new MermaidTopologyRenderOptions { Id = "api-architecture" });
        var png = document.ToPng(new MermaidTopologyRenderOptions { Id = "api-architecture" });
        Assert(svg.Contains("data-node-id=\"server\"", StringComparison.Ordinal), "Mermaid architecture SVG rendering should include service nodes.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid architecture PNG rendering should emit a valid PNG.");
    }

    private static void MermaidArchitecturePreservesTitlesContainingIn() {
        const string source = @"architecture-beta
group memory(server)[Memory]
service cache(database)[Data in memory] in memory";

        var result = new MermaidParser().ParseArchitecture(source);

        Assert(!result.HasErrors, "Mermaid architecture parser should preserve titles containing 'in': " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid architecture parser should produce a document.");
        Assert(document.Services.Count == 1 && document.Services[0].Title == "Data in memory", "Mermaid architecture parser should not treat title text as a parent clause.");
        Assert(document.Services[0].GroupId == "memory", "Mermaid architecture parser should still parse the trailing parent group.");
    }

    private static void MermaidArchitectureRejectsServiceJunctionIdCollisions() {
        const string source = @"architecture-beta
service api(server)[API]
junction api";

        var result = new MermaidParser().ParseArchitecture(source);

        Assert(result.HasErrors, "Mermaid architecture parser should reject service and junction id collisions before topology conversion.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("junction id 'api' is already defined", StringComparison.Ordinal)), "Architecture collision diagnostics should identify the duplicate junction id.");
    }

    private static void MermaidArchitectureHonorsGroupEdgeAnchors() {
        const string source = @"architecture-beta
group subnet(server)[Subnet]
service server(server)[Server] in subnet
service api(server)[API]
server{group}:B --> T:api";

        var result = new MermaidParser().ParseArchitecture(source);

        Assert(!result.HasErrors, "Mermaid architecture parser should parse group-boundary edges: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid architecture parser should produce a document.");
        var topology = document.ToTopologyChart();
        Assert(topology.Nodes.Exists(node => node.Metadata.TryGetValue("mermaid.kind", out var kind) && kind == "group-boundary-anchor"), "Mermaid architecture conversion should create hidden topology anchors for group-boundary endpoints.");
        Assert(topology.Edges.Count == 1 && topology.Edges[0].SourceNodeId.StartsWith("mermaid-architecture-group-anchor-subnet", StringComparison.Ordinal), "Mermaid architecture conversion should route group-boundary endpoints through the group anchor node.");
    }
}
