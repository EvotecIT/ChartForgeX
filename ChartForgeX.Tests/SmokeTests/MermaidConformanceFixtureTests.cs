using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using ChartForgeX.Mermaid;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesImplementedConformanceFixtures() {
        var fixturesRoot = Path.Combine(FindRepositoryRoot(), "tests", "mermaid-conformance", "fixtures");
        var fixtures = new[] {
            ("railroad-ebnf-native.mmd", typeof(MermaidRailroadDocument)),
            ("railroad-abnf-native.mmd", typeof(MermaidRailroadDocument)),
            ("railroad-peg-native.mmd", typeof(MermaidRailroadDocument)),
            ("railroad-ir-native.mmd", typeof(MermaidRailroadDocument)),
            ("flowchart-basic.mmd", typeof(MermaidFlowchartDocument)),
            ("flowchart-native-shape-aliases.mmd", typeof(MermaidFlowchartDocument)),
            ("flowchart-advanced.mmd", typeof(MermaidFlowchartDocument)),
            ("flowchart-compact.mmd", typeof(MermaidFlowchartDocument)),
            ("flowchart-endpoints.mmd", typeof(MermaidFlowchartDocument)),
            ("flowchart-inline-endpoints.mmd", typeof(MermaidFlowchartDocument)),
            ("flowchart-quoted-shapes.mmd", typeof(MermaidFlowchartDocument)),
            ("sequence-basic.mmd", typeof(MermaidSequenceDocument)),
            ("sequence-rich.mmd", typeof(MermaidSequenceDocument)),
            ("class-basic.mmd", typeof(MermaidClassDocument)),
            ("state-basic.mmd", typeof(MermaidStateDocument)),
            ("er-basic.mmd", typeof(MermaidEntityRelationshipDocument)),
            ("requirement-basic.mmd", typeof(MermaidRequirementDocument)),
            ("requirement-rich.mmd", typeof(MermaidRequirementDocument)),
            ("architecture-basic.mmd", typeof(MermaidArchitectureDocument)),
            ("architecture-nested.mmd", typeof(MermaidArchitectureDocument)),
            ("c4-basic.mmd", typeof(MermaidC4Document)),
            ("c4-nested-native.mmd", typeof(MermaidC4Document)),
            ("agentflow-native-workflow.mmd", typeof(MermaidAgentflowDocument)),
            ("mindmap-basic.mmd", typeof(MermaidMindMapDocument)),
            ("kanban-basic.mmd", typeof(MermaidKanbanDocument)),
            ("pie-basic.mmd", typeof(MermaidPieDocument)),
            ("journey-basic.mmd", typeof(MermaidJourneyDocument)),
            ("journey-actors-sections.mmd", typeof(MermaidJourneyDocument)),
            ("timeline-basic.mmd", typeof(MermaidTimelineDocument)),
            ("timeline-period-sections.mmd", typeof(MermaidTimelineDocument)),
            ("quadrant-basic.mmd", typeof(MermaidQuadrantDocument)),
            ("gantt-basic.mmd", typeof(MermaidGanttDocument)),
            ("gitgraph-basic.mmd", typeof(MermaidGitGraphDocument)),
            ("block-basic.mmd", typeof(MermaidBlockDocument)),
            ("packet-basic.mmd", typeof(MermaidPacketDocument)),
            ("venn-basic.mmd", typeof(MermaidVennDocument)),
            ("venn-proportional-overlap.mmd", typeof(MermaidVennDocument)),
            ("ishikawa-basic.mmd", typeof(MermaidIshikawaDocument)),
            ("wardley-basic.mmd", typeof(MermaidWardleyDocument)),
            ("treeview-basic.mmd", typeof(MermaidTreeViewDocument)),
            ("eventmodeling-basic.mmd", typeof(MermaidEventModelingDocument)),
            ("xychart-basic.mmd", typeof(MermaidXYChartDocument)),
            ("xychart-horizontal.mmd", typeof(MermaidXYChartDocument)),
            ("sankey-basic.mmd", typeof(MermaidSankeyDocument)),
            ("radar-basic.mmd", typeof(MermaidRadarDocument)),
            ("treemap-basic.mmd", typeof(MermaidTreemapDocument))
        };

        foreach (var fixture in fixtures) ValidateConformanceFixture(Path.Combine(fixturesRoot, fixture.Item1), fixture.Item2);
        var pluginRoot = Path.Combine(FindRepositoryRoot(), "tests", "mermaid-conformance", "external-plugins", "zenuml");
        foreach (var file in new[] { "zenuml-native-occurrences.mmd", "zenuml-native-local-context.mmd" })
            ValidateConformanceFixture(Path.Combine(pluginRoot, file), typeof(MermaidZenUmlDocument));
    }

    private static void ValidateConformanceFixture(string path, Type documentType) {
        var name = Path.GetFileName(path);
        Assert(File.Exists(path), "Mermaid conformance fixture should exist: " + name);
        var result = new MermaidParser().Parse(File.ReadAllText(path));
        Assert(!result.HasErrors, "Implemented Mermaid conformance fixture should parse without errors: " + name + " " + MermaidDiagnostics(result));
        Assert(result.Document != null && result.Document.GetType() == documentType, "Implemented Mermaid conformance fixture should produce expected document type: " + name);
        var expectedPath = Path.ChangeExtension(path, ".expected.json");
        if (result.Document is MermaidFlowchartDocument flowchart && File.Exists(expectedPath)) {
            using var expected = JsonDocument.Parse(File.ReadAllText(expectedPath));
            Assert(flowchart.Nodes.Select(node => node.Id).SequenceEqual(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(node => node.GetString())), "Flowchart semantic fixture must preserve every node.");
            Assert(flowchart.Edges.Select(edge => (edge.SourceId, edge.TargetId)).SequenceEqual(expected.RootElement.GetProperty("edges").EnumerateArray().Select(edge => (edge[0].GetString() ?? throw new InvalidDataException("Missing source node id."), edge[1].GetString() ?? throw new InvalidDataException("Missing target node id.")))), "Flowchart semantic fixture must preserve every relationship.");
        }
    }
}
