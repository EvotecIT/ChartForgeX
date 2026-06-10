using System;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesClassDiagramsAndRenders() {
        const string source = @"classDiagram
class User {
  +string Name
  +Login() bool
}
<<service>> User
User <|-- Admin : extends";

        var result = new MermaidParser().ParseClass(source);

        Assert(!result.HasErrors, "Mermaid class parser should parse class members and relationships: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid class parser should produce a document.");
        Assert(document.Classes.Count == 2, "Mermaid class parser should include explicit and relationship-discovered classes.");
        Assert(document.Classes[0].Members.Count == 2 && document.Classes[0].Members[1].IsMethod, "Mermaid class parser should distinguish methods from attributes.");
        Assert(document.Classes[0].Annotations.Count == 1 && document.Classes[0].Annotations[0] == "service", "Mermaid class parser should preserve annotations.");
        Assert(document.Relationships.Count == 1 && document.Relationships[0].Connector == "<|--", "Mermaid class parser should preserve relationship connectors.");
        var artifact = document.ToVisualArtifact(new MermaidTopologyRenderOptions { Id = "class-map" });
        Assert(artifact.Model is TopologyChart && artifact.Metadata["mermaid.classes"] == "2", "Mermaid class artifacts should carry a topology model and counts.");
        Assert(document.ToSvg().Contains("data-node-id=\"User\"", StringComparison.Ordinal), "Mermaid class SVG rendering should include class nodes.");
        Assert(document.ToPng().Length > 64, "Mermaid class PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserParsesStateDiagramsAndRenders() {
        const string source = @"stateDiagram-v2
direction LR
[*] --> Idle
Idle --> Processing : submit
state Processing {
  [*] --> Validating
  Validating --> Done
}
Processing --> [*]";

        var result = new MermaidParser().ParseState(source);

        Assert(!result.HasErrors, "Mermaid state parser should parse states, transitions, and composites: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid state parser should produce a document.");
        Assert(document.Direction == "LR", "Mermaid state parser should preserve direction statements.");
        Assert(document.States.Exists(state => state.Id == "Processing") && document.States.Exists(state => state.ParentId == "Processing"), "Mermaid state parser should preserve composite state membership.");
        Assert(document.Transitions.Count == 5, "Mermaid state parser should parse transition arrows including start and end markers.");
        var topology = document.ToTopologyChart();
        Assert(topology.Groups.Count == 1 && topology.Edges.Count == 5, "Mermaid state conversion should map composites to groups and transitions to edges.");
        Assert(document.ToSvg().Contains("data-node-id=\"Idle\"", StringComparison.Ordinal), "Mermaid state SVG rendering should include state nodes.");
        Assert(document.ToPng().Length > 64, "Mermaid state PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserCreatesDistinctStateStartMarkers() {
        const string source = @"stateDiagram-v2
state First {
  [*] --> A
}
state Second {
  [*] --> B
}";

        var result = new MermaidParser().ParseState(source);

        Assert(!result.HasErrors, "State diagrams with repeated start markers should parse without errors: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid state parser should produce a document.");
        var startCount = 0;
        foreach (var state in document.States) {
            if (state.Kind == "start") startCount++;
        }

        Assert(startCount == 2, "Repeated state start markers should create distinct start nodes.");
        Assert(document.Transitions[0].SourceId != document.Transitions[1].SourceId, "Repeated state start transitions should not reuse the same source id.");
        Assert(document.States.Exists(state => state.Kind == "start" && state.ParentId == "First"), "First composite should keep its own start node.");
        Assert(document.States.Exists(state => state.Kind == "start" && state.ParentId == "Second"), "Second composite should keep its own start node.");
    }

    private static void MermaidParserParsesEntityRelationshipDiagramsAndRenders() {
        const string source = @"erDiagram
CUSTOMER ||--o{ ORDER : places
ORDER {
  string id PK
  string customerId FK
}";

        var result = new MermaidParser().ParseEntityRelationship(source);

        Assert(!result.HasErrors, "Mermaid ER parser should parse entities, attributes, and relationships: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid ER parser should produce a document.");
        Assert(document.Entities.Count == 2, "Mermaid ER parser should include relationship-discovered and block-defined entities.");
        Assert(document.Entities.Find(entity => entity.Id == "ORDER")!.Attributes.Count == 2, "Mermaid ER parser should preserve entity attributes.");
        Assert(document.Relationships.Count == 1 && document.Relationships[0].Connector == "||--o{", "Mermaid ER parser should preserve crow's-foot cardinality.");
        var artifact = document.ToVisualArtifact(new MermaidTopologyRenderOptions { Id = "er-map" });
        Assert(artifact.Model is TopologyChart && artifact.Metadata["mermaid.entities"] == "2", "Mermaid ER artifacts should carry a topology model and counts.");
        Assert(document.ToSvg().Contains("data-node-id=\"CUSTOMER\"", StringComparison.Ordinal), "Mermaid ER SVG rendering should include entity nodes.");
        Assert(document.ToPng().Length > 64, "Mermaid ER PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserParsesMindMapsAndRenders() {
        const string source = @"mindmap
  Root((IX Visuals))
    Mermaid
      classDiagram:::important
      erDiagram
    ChartForgeX
      TableArtifact::icon(fa fa-table)";

        var result = new MermaidParser().ParseMindMap(source);

        Assert(!result.HasErrors, "Mermaid mindmap parser should parse indentation hierarchy, classes, and icons: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid mindmap parser should produce a document.");
        Assert(document.Nodes.Count == 6, "Mermaid mindmap parser should parse outline nodes.");
        Assert(document.Nodes[0].Shape == "circle" && document.Nodes[0].ParentId == null, "Mermaid mindmap parser should preserve root shape and hierarchy.");
        Assert(document.Nodes.Exists(node => node.Classes.Count == 1 && node.Classes[0] == "important"), "Mermaid mindmap parser should preserve class markers.");
        Assert(document.Nodes.Exists(node => node.Icons.Count == 1), "Mermaid mindmap parser should preserve icon markers.");
        var topology = document.ToTopologyChart();
        Assert(topology.LayoutMode == TopologyLayoutMode.MindMap && topology.Edges.Count == 5, "Mermaid mindmap conversion should use the native mind-map topology layout.");
        Assert(document.ToSvg().Contains("data-node-label=\"IX Visuals\"", StringComparison.Ordinal), "Mermaid mindmap SVG rendering should include mindmap labels.");
        Assert(document.ToPng().Length > 64, "Mermaid mindmap PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserParsesKanbanBoardsAndRenders() {
        const string source = @"kanban
todo[Todo]
  docs[Write docs]@{ assigned: ""Ana"", ticket: ""IX-1"", priority: ""High"" }
doing[Doing]
  parser[Parser work]@{ priority: ""Very High"" }
done[Done]";

        var result = new MermaidParser().ParseKanban(source);

        Assert(!result.HasErrors, "Mermaid kanban parser should parse columns, tasks, and metadata: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid kanban parser should produce a document.");
        Assert(document.Columns.Count == 3, "Mermaid kanban parser should parse columns.");
        Assert(document.Columns[0].Tasks.Count == 1 && document.Columns[0].Tasks[0].Metadata["ticket"] == "IX-1", "Mermaid kanban parser should preserve task metadata.");
        var artifact = document.ToVisualArtifact(new MermaidTopologyRenderOptions { Id = "kanban-board" });
        Assert(artifact.Model is TopologyChart && artifact.Metadata["mermaid.tasks"] == "2", "Mermaid kanban artifacts should carry a topology model and task counts.");
        Assert(document.ToSvg().Contains("data-group-id=\"todo\"", StringComparison.Ordinal), "Mermaid kanban SVG rendering should include column groups.");
        Assert(document.ToPng().Length > 64, "Mermaid kanban PNG rendering should emit a valid PNG.");
    }
}
