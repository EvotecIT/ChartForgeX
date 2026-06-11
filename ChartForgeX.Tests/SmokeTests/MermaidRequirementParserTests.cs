using System;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesRequirementsElementsAndRelationships() {
        const string source = @"requirementDiagram
direction LR
requirement test_req:::critical {
  id: 1
  text: the test text.
  risk: High
  verifymethod: Test
}
element test_entity {
  type: simulation
  docref: Test entity
}
test_entity - satisfies -> test_req
classDef critical fill:#fee,stroke:#c00
class test_req audited";

        var result = new MermaidParser().ParseRequirement(source);

        Assert(!result.HasErrors, "Mermaid requirement parser should parse requirements, elements, and relationships: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid requirement parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Requirement, "Mermaid requirement parser should produce a requirement document.");
        Assert(document.Direction == "LR", "Mermaid requirement parser should parse direction statements.");
        Assert(document.Requirements.Count == 1, "Mermaid requirement parser should parse requirement blocks.");
        Assert(document.Requirements[0].Name == "test_req" && document.Requirements[0].RequirementType == "requirement", "Mermaid requirement parser should preserve requirement names and types.");
        Assert(document.Requirements[0].RequirementId == "1" && document.Requirements[0].Risk == "High" && document.Requirements[0].VerifyMethod == "Test", "Mermaid requirement parser should parse requirement fields.");
        Assert(document.Requirements[0].Classes.Count == 2 && document.Requirements[0].Classes.Contains("critical") && document.Requirements[0].Classes.Contains("audited"), "Mermaid requirement parser should preserve inline and class-statement class assignments.");
        Assert(document.Elements.Count == 1 && document.Elements[0].ElementType == "simulation", "Mermaid requirement parser should parse element blocks.");
        Assert(document.Relationships.Count == 1 && document.Relationships[0].SourceName == "test_entity" && document.Relationships[0].TargetName == "test_req", "Mermaid requirement parser should parse forward relationships.");
        Assert(document.StyleStatements.Count == 2, "Mermaid requirement parser should retain style/class statements for hosts.");
    }

    private static void MermaidRequirementConvertsToTopologyArtifactAndRenders() {
        const string source = @"requirementDiagram
direction LR
functionalRequirement auth_req {
  id: ""AUTH-1""
  text: Users must authenticate.
  risk: Medium
  verifymethod: Inspection
}
element auth_service {
  type: service
  docref: Auth service
}
auth_service - satisfies -> auth_req";

        var result = new MermaidParser().ParseRequirement(source);
        Assert(!result.HasErrors, "Mermaid requirement parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid requirement parser should produce a document.");
        var topology = document.ToTopologyChart(new MermaidTopologyRenderOptions { Id = "auth-requirements" });
        Assert(topology.Id == "auth-requirements", "Mermaid requirement conversion should preserve caller-provided ids.");
        Assert(topology.LayoutDirection == TopologyLayoutDirection.LeftToRight, "Mermaid requirement conversion should preserve LR direction.");
        Assert(topology.Nodes.Count == 2 && topology.Edges.Count == 1, "Mermaid requirement conversion should map requirements, elements, and relationships.");
        Assert(topology.Nodes[0].Metadata["mermaid.requirementType"] == "functionalRequirement", "Mermaid requirement conversion should preserve requirement type metadata.");
        Assert(topology.Nodes[0].Status == TopologyHealthStatus.Warning, "Mermaid requirement conversion should map risk into topology status.");
        Assert(topology.Edges[0].Metadata["mermaid.relationship"] == "satisfies", "Mermaid requirement conversion should preserve relationship metadata.");

        var artifact = document.ToVisualArtifact(new MermaidTopologyRenderOptions { Id = "auth-requirements" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid requirement visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is TopologyChart, "Mermaid requirement visual artifact should carry the topology model.");
        Assert(artifact.Metadata["mermaid.requirements"] == "1" && artifact.Metadata["mermaid.elements"] == "1" && artifact.Metadata["mermaid.relationships"] == "1", "Mermaid requirement artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(TopologyChart), "Mermaid requirement artifacts should expose the topology render model.");

        var svg = document.ToSvg(new MermaidTopologyRenderOptions { Id = "auth-requirements" });
        var png = document.ToPng(new MermaidTopologyRenderOptions { Id = "auth-requirements" });
        Assert(svg.Contains("data-node-id=\"auth_req\"", StringComparison.Ordinal), "Mermaid requirement SVG rendering should include requirement nodes.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid requirement PNG rendering should emit a valid PNG.");
    }

    private static void MermaidRequirementRejectsUnknownRelationshipEndpoints() {
        const string source = @"requirementDiagram
requirement req {
  id: 1
}
missing - satisfies -> req
req - verifies -> absent";

        var result = new MermaidParser().ParseRequirement(source);

        Assert(result.HasErrors, "Mermaid requirement parser should reject relationship endpoints that were not declared.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("source 'missing'", StringComparison.Ordinal)), "Requirement relationship diagnostics should identify missing source endpoints.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("target 'absent'", StringComparison.Ordinal)), "Requirement relationship diagnostics should identify missing target endpoints.");
    }

    private static void MermaidRequirementRejectsRequirementElementNameCollisions() {
        const string source = @"requirementDiagram
requirement shared {
  id: 1
}
element shared {
  type: service
}";

        var result = new MermaidParser().ParseRequirement(source);

        Assert(result.HasErrors, "Mermaid requirement parser should reject requirement and element name collisions.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("already defined as a requirement", StringComparison.Ordinal)), "Requirement collision diagnostics should identify the shared namespace.");
    }
}
