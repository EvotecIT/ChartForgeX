using System;
using System.IO;
using ChartForgeX.Mermaid;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesImplementedConformanceFixtures() {
        var fixturesRoot = Path.Combine(FindRepositoryRoot(), "tests", "mermaid-conformance", "fixtures");
        var fixtures = new[] {
            ("flowchart-basic.mmd", typeof(MermaidFlowchartDocument)),
            ("flowchart-advanced.mmd", typeof(MermaidFlowchartDocument)),
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
            ("mindmap-basic.mmd", typeof(MermaidMindMapDocument)),
            ("kanban-basic.mmd", typeof(MermaidKanbanDocument)),
            ("pie-basic.mmd", typeof(MermaidPieDocument)),
            ("journey-basic.mmd", typeof(MermaidJourneyDocument)),
            ("timeline-basic.mmd", typeof(MermaidTimelineDocument)),
            ("quadrant-basic.mmd", typeof(MermaidQuadrantDocument)),
            ("gantt-basic.mmd", typeof(MermaidGanttDocument)),
            ("gitgraph-basic.mmd", typeof(MermaidGitGraphDocument)),
            ("block-basic.mmd", typeof(MermaidBlockDocument)),
            ("packet-basic.mmd", typeof(MermaidPacketDocument)),
            ("venn-basic.mmd", typeof(MermaidVennDocument)),
            ("ishikawa-basic.mmd", typeof(MermaidIshikawaDocument)),
            ("wardley-basic.mmd", typeof(MermaidWardleyDocument)),
            ("treeview-basic.mmd", typeof(MermaidTreeViewDocument)),
            ("eventmodeling-basic.mmd", typeof(MermaidEventModelingDocument)),
            ("xychart-basic.mmd", typeof(MermaidXYChartDocument)),
            ("sankey-basic.mmd", typeof(MermaidSankeyDocument)),
            ("radar-basic.mmd", typeof(MermaidRadarDocument)),
            ("treemap-basic.mmd", typeof(MermaidTreemapDocument))
        };

        foreach (var fixture in fixtures) {
            var path = Path.Combine(fixturesRoot, fixture.Item1);
            Assert(File.Exists(path), "Mermaid conformance fixture should exist: " + fixture.Item1);
            var result = new MermaidParser().Parse(File.ReadAllText(path));
            Assert(!result.HasErrors, "Implemented Mermaid conformance fixture should parse without errors: " + fixture.Item1 + " " + MermaidDiagnostics(result));
            Assert(result.Document != null && result.Document.GetType() == fixture.Item2, "Implemented Mermaid conformance fixture should produce expected document type: " + fixture.Item1);
        }
    }
}
