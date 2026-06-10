using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidVisualMarkupParserMapsMermaidFencesToArtifacts() {
        const string source = @"# Mixed visuals

```mermaid {#flow title=""Native Flow"" width=""640"" height=""360""}
flowchart LR
  user[User] -->|opens| app(Native app)
  app -.-> cfx{ChartForgeX}
```

```chartforgex table
id people
title ""People""
| Name | State |
| ---- | ----- |
| Ada | Enabled |
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse mixed visual fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 2, "Mermaid visual markup parser should emit artifacts for Mermaid and built-in ChartForgeX fences.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Id == "flow", "Mermaid fence id attributes should map to artifact ids.");
        Assert(result.Artifacts[0].Title == "Native Flow", "Mermaid fence title attributes should override generated titles.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid visual artifacts should expose natural size.");
        Assert(naturalSize.Width == 640 && naturalSize.Height == 360, "Mermaid fence size attributes should map to artifact natural size.");
        Assert(result.Artifacts[0].Model is TopologyChart, "Mermaid visual artifacts should carry a renderable topology model.");
        Assert(result.Artifacts[0].Metadata["sourceLine"] == "3", "Mermaid visual artifacts should preserve opening fence source lines.");
        Assert(result.Artifacts[0].Metadata["mermaid.edges"] == "2", "Mermaid visual artifacts should expose Mermaid model counts.");
        Assert(result.Artifacts[1].Kind == VisualArtifactKind.Table, "Built-in ChartForgeX visual fences should still parse through the composed parser.");
    }

    private static void MermaidVisualMarkupParserKeepsUnsupportedFamiliesDiagnosticOnly() {
        const string source = @"# Visual

```mermaid
journey
  title User Journey
  section Start
    Open app: 5: User
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Recognized but unsupported Mermaid families should remain warnings.");
        Assert(result.Artifacts.Count == 0, "Unsupported Mermaid families should not emit misleading visual artifacts.");
        Assert(result.Diagnostics.Count >= 1, "Unsupported Mermaid families should produce diagnostics.");
        Assert(result.Diagnostics[0].Line == 4, "Mermaid diagnostics should map to Markdown payload source lines.");
        Assert(result.Diagnostics[0].Message.Contains("not implemented", StringComparison.Ordinal), "Unsupported Mermaid diagnostics should be explicit.");
    }

    private static void MermaidVisualMarkupParserReportsInvalidFenceSizeAttributes() {
        const string source = @"# Visual

```mermaid {#flow width=""0""}
flowchart LR
  A --> B
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(result.HasErrors, "Invalid Mermaid fence size attributes should produce parse diagnostics.");
        Assert(result.Artifacts.Count == 0, "Invalid Mermaid render attributes should not emit a partial artifact.");
        Assert(result.Diagnostics[0].Line == 3, "Invalid Mermaid fence size diagnostics should point at the opening fence.");
        Assert(result.Diagnostics[0].Message.Contains("width", StringComparison.OrdinalIgnoreCase), "Invalid Mermaid fence size diagnostics should name the invalid option.");
    }

    private static void MermaidVisualMarkupParserMapsSequenceFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#sequence title=""Incident Sequence"" width=""720"" height=""420""}
sequenceDiagram
participant U as User
actor API as Native API
U->>API: Request
API-->>U: Response
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse sequence fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Mermaid sequence fences should emit a visual artifact.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid sequence fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Model is SequenceArtifact, "Mermaid sequence fences should carry a renderable sequence artifact model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(SequenceArtifact), "Mermaid sequence fence artifacts should expose the sequence render model.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid sequence artifacts should expose natural size.");
        Assert(naturalSize.Width == 720 && naturalSize.Height == 420, "Mermaid sequence fence size attributes should map to artifact natural size.");
    }

    private static void MermaidVisualMarkupParserMapsPieFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#pie title=""Result Mix"" width=""640"" height=""360""}
pie showData
""Passed"" : 1260
""Warnings"" : 68
""Failed"" : 10
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse pie fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Mermaid pie fences should emit a visual artifact.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid pie fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Model is Chart, "Mermaid pie fences should carry a renderable chart model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(Chart), "Mermaid pie fence artifacts should expose the chart render model.");
        Assert(result.Artifacts[0].Metadata["mermaid.slices"] == "3", "Mermaid pie fence artifacts should expose slice counts.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid pie artifacts should expose natural size.");
        Assert(naturalSize.Width == 640 && naturalSize.Height == 360, "Mermaid pie fence size attributes should map to artifact natural size.");
    }

    private static void MermaidVisualMarkupParserMapsTimelineFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#timeline title=""Native Visuals"" width=""720"" height=""420""}
timeline
section OfficeIMO
Markdown AST : Visual fence blocks
section ChartForgeX
Artifacts : SVG preview : PNG preview
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse timeline fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Mermaid timeline fences should emit a visual artifact.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid timeline fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Model is Chart, "Mermaid timeline fences should carry a renderable chart model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(Chart), "Mermaid timeline fence artifacts should expose the chart render model.");
        Assert(result.Artifacts[0].Metadata["mermaid.events"] == "3", "Mermaid timeline fence artifacts should expose event counts.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid timeline artifacts should expose natural size.");
        Assert(naturalSize.Width == 720 && naturalSize.Height == 420, "Mermaid timeline fence size attributes should map to artifact natural size.");
    }

    private static void MermaidVisualMarkupParserMapsXYChartFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#tickets title=""Ticket Trend"" width=""720"" height=""420"" dataLabels=""true""}
xychart-beta
x-axis [Jan, Feb, Mar]
y-axis ""Tickets"" 0 --> 100
bar [30, 60, 90]
line [25, 50, 80]
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse XY chart fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Mermaid XY chart fences should emit a visual artifact.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid XY chart fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Model is Chart, "Mermaid XY chart fences should carry a renderable chart model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(Chart), "Mermaid XY chart fence artifacts should expose the chart render model.");
        Assert(result.Artifacts[0].Metadata["mermaid.series"] == "2", "Mermaid XY chart fence artifacts should expose series counts.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid XY chart artifacts should expose natural size.");
        Assert(naturalSize.Width == 720 && naturalSize.Height == 420, "Mermaid XY chart fence size attributes should map to artifact natural size.");
        var chart = result.Artifacts[0].Model as Chart ?? throw new InvalidOperationException("Mermaid XY chart artifact should carry a Chart model.");
        Assert(chart.Options.ShowDataLabels, "Mermaid XY chart fence dataLabels attribute should map to chart rendering options.");
    }

    private static void MermaidVisualMarkupParserMapsSankeyFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#flow title=""Triage Flow"" width=""720"" height=""420""}
sankey-beta
Discovered,Validated,70
Validated,Remediated,44
Validated,Deferred,12
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse Sankey fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Mermaid Sankey fences should emit a visual artifact.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid Sankey fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Model is Chart, "Mermaid Sankey fences should carry a renderable chart model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(Chart), "Mermaid Sankey fence artifacts should expose the chart render model.");
        Assert(result.Artifacts[0].Metadata["mermaid.links"] == "3", "Mermaid Sankey fence artifacts should expose link counts.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid Sankey artifacts should expose natural size.");
        Assert(naturalSize.Width == 720 && naturalSize.Height == 420, "Mermaid Sankey fence size attributes should map to artifact natural size.");
    }

    private static void MermaidVisualMarkupParserMapsRadarFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#capabilities title=""Capability Radar"" width=""720"" height=""520""}
radar-beta
axis ux[""User Experience""], api[""API""], ops[""Operations""]
curve current[""Current""]{70, 65, 82}
curve target[""Target""]{ux: 90, api: 88, ops: 92}
min 0
max 100
ticks 5
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse radar fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Mermaid radar fences should emit a visual artifact.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid radar fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Model is Chart, "Mermaid radar fences should carry a renderable chart model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(Chart), "Mermaid radar fence artifacts should expose the chart render model.");
        Assert(result.Artifacts[0].Metadata["mermaid.axes"] == "3" && result.Artifacts[0].Metadata["mermaid.curves"] == "2", "Mermaid radar fence artifacts should expose axis and curve counts.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid radar artifacts should expose natural size.");
        Assert(naturalSize.Width == 720 && naturalSize.Height == 520, "Mermaid radar fence size attributes should map to artifact natural size.");
    }

    private static void MermaidVisualMarkupParserMapsTreemapFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#infra-map title=""Infrastructure Map"" width=""720"" height=""420""}
treemap-beta
""Infrastructure""
    ""Identity"": 42
    ""Messaging"": 18
    ""Endpoints""
        ""Windows"": 24
        ""macOS"": 9
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse treemap fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Mermaid treemap fences should emit a visual artifact.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid treemap fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Model is Chart, "Mermaid treemap fences should carry a renderable chart model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(Chart), "Mermaid treemap fence artifacts should expose the chart render model.");
        Assert(result.Artifacts[0].Metadata["mermaid.nodes"] == "6" && result.Artifacts[0].Metadata["mermaid.leaves"] == "4", "Mermaid treemap fence artifacts should expose node and leaf counts.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid treemap artifacts should expose natural size.");
        Assert(naturalSize.Width == 720 && naturalSize.Height == 420, "Mermaid treemap fence size attributes should map to artifact natural size.");
    }

    private static void MermaidVisualMarkupParserMapsGanttFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#project-plan title=""Project Plan"" width=""720"" height=""420"" today=""2026-01-08""}
gantt
dateFormat YYYY-MM-DD
axisFormat %m/%d
section Build
Design : active, des, 2026-01-01, 5d
Implement : crit, impl, after des, 7d
Ship : milestone, ship, after impl, 0d
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse Gantt fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Mermaid Gantt fences should emit a visual artifact.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Mermaid Gantt fences should emit Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Model is Chart, "Mermaid Gantt fences should carry a renderable chart model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(Chart), "Mermaid Gantt fence artifacts should expose the chart render model.");
        Assert(result.Artifacts[0].Metadata["mermaid.tasks"] == "3", "Mermaid Gantt fence artifacts should expose task counts.");
        var chart = result.Artifacts[0].Model as Chart ?? throw new InvalidOperationException("Mermaid Gantt artifact should carry a Chart model.");
        Assert(chart.Options.GanttToday.HasValue, "Mermaid Gantt fence today attributes should map to chart rendering options.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid Gantt artifacts should expose natural size.");
        Assert(naturalSize.Width == 720 && naturalSize.Height == 420, "Mermaid Gantt fence size attributes should map to artifact natural size.");
    }

    private static void MermaidVisualMarkupParserMapsTopologyBackedFencesToArtifacts() {
        const string source = @"# Visual

```mermaid {#class-map title=""Class Map"" width=""720"" height=""420""}
classDiagram
class User
User <|-- Admin
```

```mermaid {#state-map title=""State Map""}
stateDiagram-v2
[*] --> Idle
Idle --> [*]
```

```mermaid {#er-map title=""ER Map""}
erDiagram
CUSTOMER ||--o{ ORDER : places
```

```mermaid {#mind-map title=""Mind Map""}
mindmap
  Root
    Branch
```

```mermaid {#kanban title=""Kanban""}
kanban
todo[Todo]
  docs[Write docs]@{ priority: ""High"" }
```";

        var result = new MermaidVisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse topology-backed Mermaid fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 5, "Topology-backed Mermaid fences should emit visual artifacts.");
        Assert(result.Artifacts[0].Id == "class-map" && result.Artifacts[0].Model is TopologyChart && result.Artifacts[0].Metadata["mermaid.classes"] == "2", "Class diagram fences should map to topology artifacts.");
        Assert(result.Artifacts[1].Id == "state-map" && result.Artifacts[1].Model is TopologyChart && result.Artifacts[1].Metadata["mermaid.transitions"] == "2", "State diagram fences should map to topology artifacts.");
        Assert(result.Artifacts[2].Id == "er-map" && result.Artifacts[2].Model is TopologyChart && result.Artifacts[2].Metadata["mermaid.entities"] == "2", "ER diagram fences should map to topology artifacts.");
        Assert(result.Artifacts[3].Id == "mind-map" && result.Artifacts[3].Model is TopologyChart && result.Artifacts[3].Metadata["mermaid.nodes"] == "2", "Mindmap fences should map to topology artifacts.");
        Assert(result.Artifacts[4].Id == "kanban" && result.Artifacts[4].Model is TopologyChart && result.Artifacts[4].Metadata["mermaid.tasks"] == "1", "Kanban fences should map to topology artifacts.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Topology-backed Mermaid artifacts should expose natural size.");
        Assert(naturalSize.Width == 720 && naturalSize.Height == 420, "Topology-backed Mermaid fence size attributes should map to artifact natural size.");
    }

    private static void MermaidVisualMarkupParserUsesRenderOptionsBundle() {
        const string source = @"# Visual

```mermaid {title=""Fence Title""}
gantt
dateFormat YYYY-MM-DD
Task A : a, 2026-01-01, 2d
```";

        var result = new MermaidVisualMarkupParser(new MermaidVisualMarkupRenderOptions {
            Gantt = new MermaidGanttRenderOptions {
                Id = "default-gantt",
                Title = "Default Title",
                Width = 680,
                Height = 390,
                Today = new DateTime(2026, 1, 2)
            }
        }).Parse(source);

        Assert(!result.HasErrors, "Mermaid visual markup parser should accept bundled rendering defaults: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Bundled Mermaid rendering options should still produce one artifact.");
        Assert(result.Artifacts[0].Id == "default-gantt", "Bundled Mermaid Gantt ids should apply when a fence does not override them.");
        Assert(result.Artifacts[0].Title == "Fence Title", "Mermaid fence title attributes should override bundled rendering defaults.");
        var naturalSize = result.Artifacts[0].NaturalSize ?? throw new InvalidOperationException("Mermaid Gantt artifacts should expose natural size.");
        Assert(naturalSize.Width == 680 && naturalSize.Height == 390, "Bundled Mermaid Gantt sizes should apply when a fence does not override them.");
        var chart = result.Artifacts[0].Model as Chart ?? throw new InvalidOperationException("Mermaid Gantt artifact should carry a Chart model.");
        Assert(chart.Options.GanttToday == new DateTime(2026, 1, 2).ToOADate(), "Bundled Mermaid Gantt today values should flow into the render model.");
    }

    private static void MermaidVisualMarkupParserMapsPreScannedBlocksToArtifacts() {
        var blocks = new[] {
            new VisualMarkupBlock(
                VisualMarkupKind.Mermaid,
                "mermaid",
                "mermaid {#flow title=\"Host Flow\" width=640 height=360}",
                "flowchart LR\n  office[OfficeIMO] --> ix[IX]\n  ix --> cfx[ChartForgeX]",
                12,
                13,
                15,
                new Dictionary<string, string> {
                    ["id"] = "flow",
                    ["title"] = "Host Flow",
                    ["width"] = "640",
                    ["height"] = "360"
                }),
            new VisualMarkupBlock(
                VisualMarkupKind.Mermaid,
                "mermaid",
                "mermaid {#pie title=\"Host Pie\"}",
                "pie showData\n\"Passed\" : 10\n\"Failed\" : 1",
                30,
                31,
                33,
                new Dictionary<string, string> {
                    ["id"] = "pie",
                    ["title"] = "Host Pie"
                }),
            new VisualMarkupBlock(
                VisualMarkupKind.Mermaid,
                "mermaid",
                "mermaid {#xy title=\"Host XY\"}",
                "xychart-beta\nx-axis [A, B]\nbar [1, 2]",
                40,
                41,
                43,
                new Dictionary<string, string> {
                    ["id"] = "xy",
                    ["title"] = "Host XY"
                }),
            new VisualMarkupBlock(
                VisualMarkupKind.Mermaid,
                "mermaid",
                "mermaid {#sankey title=\"Host Sankey\"}",
                "sankey-beta\nA,B,10\nB,C,7",
                50,
                51,
                53,
                new Dictionary<string, string> {
                    ["id"] = "sankey",
                    ["title"] = "Host Sankey"
                }),
            new VisualMarkupBlock(
                VisualMarkupKind.Mermaid,
                "mermaid",
                "mermaid {#radar title=\"Host Radar\"}",
                "radar-beta\naxis A, B, C\ncurve current{1, 2, 3}",
                60,
                61,
                63,
                new Dictionary<string, string> {
                    ["id"] = "radar",
                    ["title"] = "Host Radar"
                }),
            new VisualMarkupBlock(
                VisualMarkupKind.Mermaid,
                "mermaid",
                "mermaid {#treemap title=\"Host Treemap\"}",
                "treemap-beta\n\"Root\"\n    \"Leaf A\": 10\n    \"Leaf B\": 7",
                70,
                71,
                74,
                new Dictionary<string, string> {
                    ["id"] = "treemap",
                    ["title"] = "Host Treemap"
                }),
            new VisualMarkupBlock(
                VisualMarkupKind.Mermaid,
                "mermaid",
                "mermaid {#gantt title=\"Host Gantt\"}",
                "gantt\ndateFormat YYYY-MM-DD\nTask A : a, 2026-01-01, 2d\nTask B : b, after a, 2d",
                80,
                81,
                84,
                new Dictionary<string, string> {
                    ["id"] = "gantt",
                    ["title"] = "Host Gantt"
                })
        };

        var result = new MermaidVisualMarkupParser().ParseBlocks(blocks);

        Assert(!result.HasErrors, "Mermaid visual markup parser should parse host-supplied Mermaid blocks without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 7, "Pre-scanned Mermaid blocks should emit visual artifacts.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Mermaid, "Pre-scanned Mermaid blocks should map to Mermaid visual artifacts.");
        Assert(result.Artifacts[0].Id == "flow", "Pre-scanned Mermaid block attributes should map to artifact ids.");
        Assert(result.Artifacts[0].Metadata["sourceLine"] == "12", "Pre-scanned Mermaid blocks should preserve host-provided opening fence lines.");
        Assert(result.Artifacts[0].Metadata["payloadStartLine"] == "13", "Pre-scanned Mermaid blocks should preserve host-provided payload source lines.");
        Assert(result.Artifacts[0].Model is TopologyChart, "Pre-scanned Mermaid flowcharts should carry a renderable topology model.");
        Assert(result.Artifacts[1].Id == "pie" && result.Artifacts[1].Model is Chart, "Pre-scanned Mermaid pie charts should carry a renderable chart model.");
        Assert(result.Artifacts[1].Metadata["sourceLine"] == "30", "Pre-scanned Mermaid pie blocks should preserve host-provided opening fence lines.");
        Assert(result.Artifacts[2].Id == "xy" && result.Artifacts[2].Model is Chart, "Pre-scanned Mermaid XY charts should carry a renderable chart model.");
        Assert(result.Artifacts[2].Metadata["sourceLine"] == "40", "Pre-scanned Mermaid XY chart blocks should preserve host-provided opening fence lines.");
        Assert(result.Artifacts[3].Id == "sankey" && result.Artifacts[3].Model is Chart, "Pre-scanned Mermaid Sankey diagrams should carry a renderable chart model.");
        Assert(result.Artifacts[3].Metadata["sourceLine"] == "50", "Pre-scanned Mermaid Sankey blocks should preserve host-provided opening fence lines.");
        Assert(result.Artifacts[4].Id == "radar" && result.Artifacts[4].Model is Chart, "Pre-scanned Mermaid radar diagrams should carry a renderable chart model.");
        Assert(result.Artifacts[4].Metadata["sourceLine"] == "60", "Pre-scanned Mermaid radar blocks should preserve host-provided opening fence lines.");
        Assert(result.Artifacts[5].Id == "treemap" && result.Artifacts[5].Model is Chart, "Pre-scanned Mermaid treemap diagrams should carry a renderable chart model.");
        Assert(result.Artifacts[5].Metadata["sourceLine"] == "70", "Pre-scanned Mermaid treemap blocks should preserve host-provided opening fence lines.");
        Assert(result.Artifacts[6].Id == "gantt" && result.Artifacts[6].Model is Chart, "Pre-scanned Mermaid Gantt diagrams should carry a renderable chart model.");
        Assert(result.Artifacts[6].Metadata["sourceLine"] == "80", "Pre-scanned Mermaid Gantt blocks should preserve host-provided opening fence lines.");
    }
}
