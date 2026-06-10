using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MarkupTopologyParsesFencedCommandDiagram() {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "ChartForgeX.Tests", "Fixtures", "markup", "topology-service-map.md"));
        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Command topology markup should parse without errors: " + Diagnostics(result));
        Assert(result.Document != null, "Topology markup should produce a document.");
        Assert(result.Document!.Groups.Count == 2, "Topology markup should parse groups.");
        Assert(result.Document.Nodes.Count == 3, "Topology markup should parse nodes.");
        Assert(result.Document.Edges.Count == 2, "Topology markup should parse edges.");

        var chart = result.Document.ToTopologyChart();
        Assert(chart.LayoutMode == TopologyLayoutMode.Layered && chart.LayoutDirection == TopologyLayoutDirection.LeftToRight, "Topology markup should map compact layout aliases.");
        Assert(chart.ToSvg().Contains("data-cfx-role=\"topology\"", System.StringComparison.Ordinal), "Topology markup should render through the ChartForgeX SVG renderer.");
    }

    private static void MarkupTopologyParsesTableDiagramAndEmitsCSharp() {
        const string source = @"```chartforgex topology
title: ""Regional Directory Topology""
layout: densegrouped tb
groups:
| id | label | status | icon |
| -- | ----- | ------ | ---- |
| emea | EMEA | warning | microsoft-ad:site |
| amer | AMER | healthy | microsoft-ad:site |
nodes:
| id | label | group | kind | status | badge |
| -- | ----- | ----- | ---- | ------ | ----- |
| dc-emea | EMEA DC01 | emea | server | warning | GC |
| dc-amer | AMER DC01 | amer | server | healthy | GC |
edges:
| from | to | label | status | direction |
| ---- | -- | ----- | ------ | --------- |
| dc-emea | dc-amer | 92 ms | warning | bidirectional |
```";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Table topology markup should parse without errors: " + Diagnostics(result));
        var code = MarkupTopologyCSharpEmitter.Emit(result.Document!);
        Assert(code.Contains("TopologyChart.Create()", System.StringComparison.Ordinal), "C# emitter should create a topology chart.");
        Assert(code.Contains(".AddGroup(\"emea\", \"EMEA\", 0, 0, 260, 160, TopologyHealthStatus.Warning", System.StringComparison.Ordinal), "C# emitter should include parsed groups.");
        Assert(code.Contains(".WithNodeBadge(\"dc-emea\", \"GC\")", System.StringComparison.Ordinal), "C# emitter should include node badge helpers.");
    }

    private static void MarkupTopologyReportsMissingNodes() {
        var result = new MarkupTopologyParser().Parse("title \"Empty\"");

        Assert(result.HasErrors, "Topology markup without nodes should report a parser error.");
        Assert(Diagnostics(result).Contains("at least one node", System.StringComparison.Ordinal), "Missing-node diagnostic should be actionable.");
    }

    private static void MarkupTopologyExtractsTildeFenceWithMetadata() {
        const string source = @"# Diagram

~~~chartforgex topology {#service-map}
title ""Tilde Fence""
node api ""API"" kind:service status:healthy
~~~
";

        var blocks = ChartForgeXMarkdown.ExtractTopologyPayloads(source);
        Assert(blocks.Count == 1, "Markdown extraction should support standard three-tilde fences with trailing metadata.");
        var result = new MarkupTopologyParser().Parse(source);
        Assert(!result.HasErrors, "Tilde-fenced topology markup should parse without errors: " + Diagnostics(result));
        Assert(result.Document != null && result.Document.Title == "Tilde Fence", "Tilde-fenced topology markup should produce a document.");
        Assert(result.Document!.Nodes.Count == 1, "Tilde-fenced topology markup should parse nodes.");
    }

    private static void MarkupTopologyDiagnosticsUseMarkdownSourceLines() {
        const string source = @"# Diagram

```chartforgex topology
title ""Source Line Check""
unknownThing yes
node api ""API"" kind:service status:healthy
```";

        var result = new MarkupTopologyParser().Parse(source);
        Assert(!result.HasErrors, "Unknown commands should remain warnings.");
        var warning = result.Diagnostics.Find(diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Warning);
        Assert(warning != null, "Unknown commands should produce a warning.");
        Assert(warning!.Line == 5, "Markdown parser diagnostics should use the original source line.");
    }

    private static void MarkupTopologyPreservesMarkdownTableAuthoringDetails() {
        const string source = @"```chartforgex topology
nodes:
id | label | kind | status | display | width | height | subtitle
:-- | :---- | :--- | ---: | :------ | ----: | -----: | :-------
api | API \| Gateway | service | healthy | tile | 180 | 80 | https://api.example.com
db | Database | database | warning | card | 150 | 70 | DOMAIN\user
edges:
from | to | label | status
:--- | --: | :---- | :-----
api | db | https://api.example.com | warning
```";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Markdown table topology should parse without errors: " + Diagnostics(result));
        Assert(result.Document!.Nodes[0].Label == "API | Gateway", "Escaped table pipes should stay inside the cell.");
        Assert(result.Document.Nodes[0].Subtitle == "https://api.example.com", "Table values should preserve URL-style text.");
        Assert(result.Document.Nodes[0].Display == TopologyNodeDisplayMode.Tile, "Node table rows should map display.");
        Assert(result.Document.Nodes[0].Width == 180 && result.Document.Nodes[0].Height == 80, "Node table rows should map explicit dimensions.");
        Assert(result.Document.Nodes[1].Subtitle == @"DOMAIN\user", "Table cells should preserve literal backslashes.");
        Assert(result.Document.Edges[0].Label == "https://api.example.com", "Edge table labels should preserve colon-containing values.");
    }

    private static void MarkupTopologyPreservesCommandAuthoringDetails() {
        const string source = @"title ""Command Details""
node api ""API"" kind:service status:healthy subtitle:https://api.example.com width:180 height:80 display:tile
node db ""Database"" kind:database status:warning
edge api -> db ""status:warning"" status:healthy
";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(!result.HasErrors, "Command topology should parse without errors: " + Diagnostics(result));
        Assert(result.Document!.Nodes[0].Subtitle == "https://api.example.com", "Command attributes should preserve URL-style text.");
        Assert(result.Document.Nodes[0].Width == 180 && result.Document.Nodes[0].Height == 80, "Command nodes should map explicit dimensions.");
        Assert(result.Document.Nodes[0].Display == TopologyNodeDisplayMode.Tile, "Command nodes should map display.");
        Assert(result.Document.Edges[0].Label == "status:warning", "Quoted command edge labels that look like attributes should stay labels.");
        Assert(result.Document.Edges[0].Status == TopologyHealthStatus.Healthy, "Attributes after quoted edge labels should still be parsed.");
        Assert(result.Document.Edges[0].Direction == TopologyDirection.Forward, "Arrow commands should default -> to forward direction.");

        const string sectionSource = @"nodes:
node api ""API | Gateway"" kind:service status:healthy
";
        var sectionResult = new MarkupTopologyParser().Parse(sectionSource);
        Assert(!sectionResult.HasErrors, "Command-style section rows with pipe text should not be misread as Markdown tables: " + Diagnostics(sectionResult));
        Assert(sectionResult.Document!.Nodes[0].Label == "API | Gateway", "Command-style section rows should preserve pipe labels.");
    }

    private static void MarkupTopologyRejectsMismatchedSectionCommands() {
        const string source = @"nodes:
edge api -> db
title ""Wrong Place""
layout layered lr
";

        var result = new MarkupTopologyParser().Parse(source);

        Assert(result.HasErrors, "Mismatched section commands should produce parser errors.");
        var diagnostics = Diagnostics(result);
        Assert(diagnostics.Contains("edge' cannot appear inside nodes section", StringComparison.Ordinal), "Mismatched entry commands should identify the bad section.");
        Assert(diagnostics.Contains("title' cannot appear inside nodes section", StringComparison.Ordinal), "Document-level title commands inside typed sections should be rejected instead of coerced.");
        Assert(diagnostics.Contains("layout' cannot appear inside nodes section", StringComparison.Ordinal), "Document-level layout commands inside typed sections should be rejected instead of coerced.");
    }

    private static void MarkupTopologyHandlesEditingFriendlyMarkdownFences() {
        const string unterminated = @"# Draft

```chartforgex topology
node api ""API"" kind:service status:healthy";

        var blocks = ChartForgeXMarkdown.ExtractTopologyBlocks(unterminated);
        Assert(blocks.Count == 1, "Unterminated topology fences should remain open through EOF.");
        Assert(blocks[0].StartLine == 4, "Unterminated fence payload should preserve source line.");

        const string indented = @"    ```chartforgex topology
    node api ""API""
    ```
node raw ""Raw"" kind:service status:healthy";

        var result = new MarkupTopologyParser().Parse(indented);
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("Unknown topology command", StringComparison.Ordinal)), "Four-space indented fences should stay as code block text, not live topology fences.");

        var tabIndented = "\t```chartforgex topology\n\tnode api \"API\"\n\t```\nnode raw \"Raw\" kind:service status:healthy";
        var tabResult = new MarkupTopologyParser().Parse(tabIndented);
        Assert(tabResult.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("Unknown topology command", StringComparison.Ordinal)), "Tab-indented fences should stay as code block text, not live topology fences.");

        const string closingSuffix = @"```chartforgex topology
node api ""API"" kind:service status:healthy
```not-a-close
node db ""Database"" kind:database status:warning
```";
        var closingResult = new MarkupTopologyParser().Parse(closingSuffix);
        Assert(!closingResult.HasErrors, "Closing fences with trailing text should stay inside the payload until a valid close.");
        Assert(closingResult.Document!.Nodes.Count == 2, "Invalid closing-fence suffixes should not truncate topology payloads.");
    }

    private static void VisualMarkupScannerExtractsSupportedVisualFences() {
        const string source = @"# Visuals

```mermaid
flowchart LR
  a --> b
```

~~~chartforgex table {#users .compact mode=""native""}
| Name | State |
| ---- | ----- |
| Ada | Enabled |
~~~

```cfx flow
layout LR
node a ""A""
```

```chartforgex chart
type bar
```

```cfx timeline
event started
```";

        var result = VisualMarkupScanner.Scan(source);

        Assert(result.Diagnostics.Count == 0, "Supported visual fences should not produce scanner diagnostics.");
        Assert(result.Blocks.Count == 5, "Visual markup scanner should extract all supported visual fence families.");
        Assert(result.Blocks[0].Kind == VisualMarkupKind.Mermaid && result.Blocks[0].StartLine == 4, "Mermaid fences should preserve kind and payload source line.");
        Assert(result.Blocks[1].Kind == VisualMarkupKind.Table, "ChartForgeX table fences should be recognized.");
        Assert(result.Blocks[1].Attributes["id"] == "users", "Visual fence attributes should parse id shorthand.");
        Assert(result.Blocks[1].Attributes["class"] == "compact", "Visual fence attributes should parse class shorthand.");
        Assert(result.Blocks[1].Attributes["mode"] == "native", "Visual fence attributes should parse key/value attributes.");
        Assert(result.Blocks[2].Kind == VisualMarkupKind.Flow && result.Blocks[2].FenceName == "cfx flow", "CFX flow fences should preserve normalized fence names.");
        Assert(result.Blocks[3].Kind == VisualMarkupKind.Chart, "ChartForgeX chart fences should be recognized.");
        Assert(result.Blocks[4].Kind == VisualMarkupKind.Timeline, "CFX timeline fences should be recognized.");
    }

    private static void VisualMarkupScannerReportsUnsupportedChartForgeXFences() {
        const string source = @"# Visuals

```chartforgex swimlane
lane support
```

```powershell
Get-Process
```";

        var result = VisualMarkupScanner.Scan(source);

        Assert(result.Blocks.Count == 0, "Unsupported ChartForgeX visual fences should not become parseable blocks.");
        Assert(result.Diagnostics.Count == 1, "Unsupported ChartForgeX visual fences should produce one line-aware diagnostic.");
        Assert(result.Diagnostics[0].Line == 3, "Unsupported fence diagnostics should point at the opening fence line.");
        Assert(result.Diagnostics[0].Message.Contains("chartforgex swimlane", StringComparison.Ordinal), "Unsupported fence diagnostics should name the unsupported fence.");
    }

    private static void VisualMarkupScannerKeepsTopologyCompatibility() {
        const string source = @"# Mixed

```chartforgex table
| A |
```

```chartforgex topology
node api ""API"" kind:service status:healthy
```

```mermaid
flowchart LR
  a --> b
```";

        var visualBlocks = VisualMarkupScanner.ExtractBlocks(source);
        var topologyBlocks = ChartForgeXMarkdown.ExtractTopologyBlocks(source);

        Assert(visualBlocks.Count == 3, "Generic scanner should retain all supported visual blocks.");
        Assert(topologyBlocks.Count == 1, "Existing topology extraction should remain topology-only.");
        Assert(topologyBlocks[0].StartLine == 8, "Existing topology extraction should preserve payload source line.");
        Assert(topologyBlocks[0].Payload.Contains("node api", StringComparison.Ordinal), "Existing topology extraction should preserve payload text.");
    }

    private static void VisualMarkupScannerKeepsIndentedFencesLiteral() {
        const string source = @"    ```chartforgex table
    | A |
    ```

```chartforgex table
| B |
```";

        var result = VisualMarkupScanner.Scan(source);

        Assert(result.Blocks.Count == 1, "Four-space indented fences should stay literal code and not become visual markup.");
        Assert(result.Blocks[0].StartLine == 6, "Non-indented visual fences should still preserve payload source line.");
        Assert(result.Blocks[0].Payload.Contains("| B |", StringComparison.Ordinal), "Scanner should extract the non-indented visual fence payload.");
    }

    private static void VisualMarkupParserMapsTopologyFencesToArtifacts() {
        const string source = @"# Visual

```chartforgex topology {#services}
id service-map
title ""Service Map""
subtitle ""Generated from Markdown""
node api ""API"" kind:service status:healthy
```";

        var result = new VisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Visual markup parser should parse supported topology fences without errors.");
        Assert(result.Artifacts.Count == 1, "Visual markup parser should emit one artifact for one topology fence.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Topology, "Topology fences should map to topology visual artifacts.");
        Assert(result.Artifacts[0].Id == "service-map", "Topology visual artifact ids should come from topology markup when declared.");
        Assert(result.Artifacts[0].Title == "Service Map", "Topology visual artifact titles should come from topology markup.");
        Assert(result.Artifacts[0].SupportsExport(VisualArtifactExportFormat.Svg), "Topology visual artifacts should declare SVG export support.");
        Assert(result.Artifacts[0].Model is TopologyChart, "Topology visual artifacts should keep a typed TopologyChart model.");
        Assert(result.Artifacts[0].Metadata["sourceLine"] == "3", "Topology visual artifacts should preserve source fence metadata.");
    }

    private static void VisualMarkupParserMapsPreScannedBlocksToArtifacts() {
        var blocks = new[] {
            new VisualMarkupBlock(
                VisualMarkupKind.Topology,
                "chartforgex topology",
                "chartforgex topology {#services}",
                "id service-map\nnode api \"API\" kind:service status:healthy",
                25,
                26,
                27,
                new Dictionary<string, string> { ["id"] = "services" }),
            new VisualMarkupBlock(
                VisualMarkupKind.Table,
                "chartforgex table",
                "chartforgex table",
                "id people\n| Name | State |\n| ---- | ----- |\n| Ada | Enabled |",
                40,
                41,
                44,
                new Dictionary<string, string>()),
            new VisualMarkupBlock(
                VisualMarkupKind.Chart,
                "chartforgex chart",
                "chartforgex chart",
                "id trend\ntype line\nlabels Jan Feb Mar\nvalues 12 18 16",
                55,
                56,
                59,
                new Dictionary<string, string>())
        };

        var result = new VisualMarkupParser().ParseBlocks(blocks);

        Assert(!result.HasErrors, "Visual markup parser should parse host-supplied visual blocks without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 3, "Pre-scanned visual blocks should emit the same artifacts as Markdown-scanned blocks.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Topology, "Pre-scanned topology blocks should map to topology visual artifacts.");
        Assert(result.Artifacts[0].Metadata["sourceLine"] == "25", "Pre-scanned topology blocks should preserve host-provided opening fence lines.");
        Assert(result.Artifacts[1].Kind == VisualArtifactKind.Table, "Pre-scanned table blocks should map to table visual artifacts.");
        Assert(result.Artifacts[1].Metadata["sourceLine"] == "40", "Pre-scanned table blocks should preserve host-provided opening fence lines.");
        Assert(result.Artifacts[2].Kind == VisualArtifactKind.Chart, "Pre-scanned chart blocks should map to chart visual artifacts.");
        Assert(result.Artifacts[2].Metadata["sourceLine"] == "55", "Pre-scanned chart blocks should preserve host-provided opening fence lines.");
    }

    private static void MarkupTableParserParsesSimpleMarkdownTable() {
        const string source = @"```chartforgex table
title ""People""
capabilities search sort filter copy export
| Name | State |
| ---- | ----- |
| Ada Lovelace | Enabled |
| Grace Hopper | Disabled |
```";

        var result = new MarkupTableParser().Parse(source);

        Assert(!result.HasErrors, "Simple table markup should parse without errors: " + Diagnostics(result));
        Assert(result.Document != null, "Simple table markup should produce a table artifact.");
        Assert(result.Document!.Title == "People", "Simple table markup should map title.");
        Assert(result.Document.Supports(TableArtifactCapabilities.Search), "Simple table markup should map search capability.");
        Assert(result.Document.Supports(TableArtifactCapabilities.Export), "Simple table markup should map export capability.");
        Assert(result.Document.Columns.Count == 2 && result.Document.Columns[0].Id == "name", "Simple table markup should build columns from Markdown headers.");
        Assert(result.Document.Rows.Count == 2 && result.Document.Rows[1].Key == "row-2", "Simple table markup should build stable fallback row keys.");
        Assert(result.Document.Rows[1].Cells[1].DisplayText == "Disabled", "Simple table markup should preserve cell text.");
        Assert(result.Document.ToSvg().Contains("data-cfx-role=\"table-header\"", StringComparison.Ordinal), "Simple table artifacts should render static SVG previews.");
    }

    private static void MarkupTableParserParsesTypedColumnsAndRows() {
        const string source = @"```chartforgex table
id services
title ""Services""
subtitle ""Typed native table""
capabilities search sort filter multiselect copy export virtualization
totalRows 125

columns:
| id | label | type | alignment | width | searchable | sortable | filterable |
| -- | ----- | ---- | --------- | ----- | ---------- | -------- | ---------- |
| name | Name | text | left | 180 | true | true | true |
| status | Status | status | left | 110 | true | true | true |
| latency | Latency | number | right | 90 | false | true | false |

rows:
| key | name | status | latency |
| --- | ---- | ------ | ------- |
| api | API | Healthy | 24 |
| worker | Worker | Warning | 91 |
```";

        var result = new MarkupTableParser().Parse(source);

        Assert(!result.HasErrors, "Typed table markup should parse without errors: " + Diagnostics(result));
        var table = result.Document!;
        Assert(table.Id == "services", "Typed table markup should map id.");
        Assert(table.Subtitle == "Typed native table", "Typed table markup should map subtitle.");
        Assert(table.Supports(TableArtifactCapabilities.Virtualization), "Typed table markup should map virtualization capability.");
        Assert(table.TotalRowCount == 125, "Typed table markup should map total row count.");
        Assert(table.Columns[2].Type == TableArtifactColumnType.Number && table.Columns[2].Alignment == VisualTextAlignment.Right, "Typed table columns should map type and alignment.");
        Assert(!table.Columns[2].Searchable && table.Columns[2].Sortable && !table.Columns[2].Filterable, "Typed table columns should map host behavior flags.");
        Assert(table.Rows[0].Key == "api" && table.Rows[0].Cells[0].DisplayText == "API", "Typed table rows should use key without rendering it as the first cell.");
        Assert(table.Rows[1].Cells[1].Status == VisualStatus.Warning, "Status columns should map recognized status text to cell status.");
    }

    private static void VisualMarkupParserMapsTableFencesToArtifacts() {
        const string source = @"# Visual

```chartforgex table
id people
title ""People""
| Name | State |
| ---- | ----- |
| Ada | Enabled |
```";

        var result = new VisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Visual markup parser should parse supported table fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Visual markup parser should emit one artifact for one table fence.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Table, "Table fences should map to table visual artifacts.");
        Assert(result.Artifacts[0].Id == "people", "Table visual artifact ids should come from table markup when declared.");
        Assert(result.Artifacts[0].Model is TableArtifact, "Table visual artifacts should keep a typed TableArtifact model.");
        Assert(result.Artifacts[0].SupportsExport(VisualArtifactExportFormat.Png), "Table visual artifacts should declare PNG preview export support.");
        Assert(result.Artifacts[0].Metadata["sourceLine"] == "3", "Table visual artifacts should preserve source fence metadata.");
    }

    private static void MarkupChartParserParsesSimpleChart() {
        const string source = @"```chartforgex chart
id result-mix
title ""Result Mix""
type pie
series ""Checks""
| Label | Value |
| ----- | ----- |
| Passed | 1260 |
| Warnings | 68 |
| Failed | 10 |
```";

        var result = new MarkupChartParser().Parse(source);

        Assert(!result.HasErrors, "Simple chart markup should parse without errors: " + Diagnostics(result));
        Assert(result.Document != null, "Simple chart markup should produce a chart document.");
        Assert(result.Document!.Id == "result-mix", "Simple chart markup should map id.");
        Assert(result.Document.Chart.Title == "Result Mix", "Simple chart markup should map chart title.");
        Assert(result.Document.Chart.Series.Count == 1 && result.Document.Chart.Series[0].Kind == ChartSeriesKind.Pie, "Simple chart markup should build the requested chart kind.");
        Assert(result.Document.Chart.Options.XAxisLabels.Count == 3, "Simple chart markup should preserve labels.");
        Assert(result.Document.Chart.ToSvg().Contains("data-cfx-role=\"pie-slice\"", StringComparison.Ordinal), "Simple chart markup should render through ChartForgeX chart SVG.");
    }

    private static void VisualMarkupParserMapsChartFencesToArtifacts() {
        const string source = @"# Visual

```chartforgex chart
id trend
title ""Trend""
type line
labels Jan Feb Mar
values 12 18 16
```";

        var result = new VisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Visual markup parser should parse supported chart fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Visual markup parser should emit one artifact for one chart fence.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Chart, "Chart fences should map to chart visual artifacts.");
        Assert(result.Artifacts[0].Id == "trend", "Chart visual artifact ids should come from chart markup.");
        Assert(result.Artifacts[0].Model is Chart, "Chart visual artifacts should keep a typed Chart model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(Chart), "Chart visual artifacts should expose the chart render model.");
        Assert(result.Artifacts[0].Metadata["sourceLine"] == "3", "Chart visual artifacts should preserve source fence metadata.");
    }

    private static void VisualMarkupParserReportsUnimplementedVisualDispatch() {
        const string source = @"# Visual

```mermaid
flowchart LR
  a --> b
```

```chartforgex timeline
event started
```";

        var result = new VisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Unimplemented visual dispatch should be a warning, not a parser error.");
        Assert(result.Artifacts.Count == 0, "Unimplemented visual dispatch should not emit partial artifacts.");
        Assert(result.Diagnostics.Count == 2, "Parser should report each recognized but unimplemented visual fence.");
        Assert(result.Diagnostics[0].Line == 3 && result.Diagnostics[0].Message.Contains("mermaid", StringComparison.Ordinal), "Parser should report Mermaid dispatch with the opening fence line.");
        Assert(result.Diagnostics[1].Line == 8 && result.Diagnostics[1].Message.Contains("chartforgex timeline", StringComparison.Ordinal), "Parser should report timeline dispatch with the opening fence line.");
    }

    private static void MarkupTopologyCliKeepsWarningsOffGeneratedStreams() {
        var fixture = Path.Combine(Path.GetTempPath(), "chartforgex-markup-warning-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(fixture, "title \"Warning Stream Check\"\nunknownThing yes\nnode api \"API\" kind:service status:healthy\n");
        try {
            var preview = RunMarkupCli("preview", fixture);
            Assert(preview.ExitCode == 0, "CLI preview should succeed for warning-only markup: " + preview.StandardError);
            Assert(preview.StandardOutput.TrimStart().StartsWith("<!doctype html>", StringComparison.Ordinal), "CLI preview stdout should start with HTML.");
            Assert(!preview.StandardOutput.Contains("warning(", StringComparison.OrdinalIgnoreCase), "CLI preview stdout should not be contaminated by diagnostics.");
            Assert(preview.StandardError.Contains("warning(2): Unknown topology command 'unknownThing'.", StringComparison.Ordinal), "CLI preview should write parser warnings to stderr.");

            var emit = RunMarkupCli("emit", fixture, "--target", "csharp");
            Assert(emit.ExitCode == 0, "CLI emit should succeed for warning-only markup: " + emit.StandardError);
            Assert(emit.StandardOutput.TrimStart().StartsWith("using ChartForgeX.Topology;", StringComparison.Ordinal), "CLI emit stdout should start with generated C#.");
            Assert(!emit.StandardOutput.Contains("warning(", StringComparison.OrdinalIgnoreCase), "CLI emit stdout should not be contaminated by diagnostics.");
            Assert(emit.StandardError.Contains("warning(2): Unknown topology command 'unknownThing'.", StringComparison.Ordinal), "CLI emit should write parser warnings to stderr.");
        } finally {
            try {
                File.Delete(fixture);
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }
        }
    }

    private static void MarkupTopologyCliRejectsMalformedAutomationInputs() {
        var shortInvocation = RunMarkupCliRaw("validate");
        Assert(shortInvocation.ExitCode == 1, "CLI should fail when an input file is missing.");
        Assert(shortInvocation.StandardError.Contains("Missing input file", StringComparison.Ordinal), "Missing input file should be reported on stderr.");

        var fixture = Path.Combine(Path.GetTempPath(), "chartforgex-markup-options-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(fixture, "not even valid topology\n");
        try {
            var unknownCommand = RunMarkupCliRaw("valdiate", fixture);
            Assert(unknownCommand.ExitCode == 1, "CLI should reject unknown commands before parsing input.");
            Assert(unknownCommand.StandardError.Contains("Unknown command", StringComparison.Ordinal), "Unknown commands should be reported on stderr.");

            File.WriteAllText(fixture, "node api \"API\" kind:service status:healthy\n");
            var missingValue = RunMarkupCli("emit", fixture, "--target");
            Assert(missingValue.ExitCode == 1, "CLI should fail when an option value is missing.");
            Assert(missingValue.StandardError.Contains("requires a value", StringComparison.Ordinal), "Missing option value should be reported on stderr.");
        } finally {
            try {
                File.Delete(fixture);
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }
        }
    }

    private static void MarkupTopologyCliReportsRenderValidationErrors() {
        var fixture = Path.Combine(Path.GetTempPath(), "chartforgex-markup-invalid-" + Guid.NewGuid().ToString("N") + ".md");
        var output = Path.Combine(Path.GetTempPath(), "chartforgex-markup-invalid-" + Guid.NewGuid().ToString("N") + ".svg");
        File.WriteAllText(fixture, "node api \"API\" kind:service status:healthy\nedge api -> missing \"broken\"\n");
        try {
            var preview = RunMarkupCli("preview", fixture);
            Assert(preview.ExitCode == 2, "CLI preview should return a stable validation error exit code.");
            Assert(preview.StandardError.Contains("error ", StringComparison.Ordinal), "CLI preview should write topology validation errors to stderr.");

            var export = RunMarkupCli("export", fixture, "--output", output);
            Assert(export.ExitCode == 2, "CLI export should return a stable validation error exit code.");
            Assert(export.StandardError.Contains("error ", StringComparison.Ordinal), "CLI export should write topology validation errors to stderr.");
        } finally {
            try {
                File.Delete(fixture);
                File.Delete(output);
            } catch (IOException) {
            } catch (UnauthorizedAccessException) {
            }
        }
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunMarkupCli(string command, string input, params string[] extraArguments) {
        var arguments = new string[2 + extraArguments.Length];
        arguments[0] = command;
        arguments[1] = input;
        Array.Copy(extraArguments, 0, arguments, 2, extraArguments.Length);
        return RunMarkupCliRaw(arguments);
    }

    private static (int ExitCode, string StandardOutput, string StandardError) RunMarkupCliRaw(params string[] arguments) {
        var cli = FindMarkupCliDll();
        var startInfo = new ProcessStartInfo("dotnet") {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(cli);
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start ChartForgeX.Markup.Cli.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) {
            process.Kill(true);
            throw new TimeoutException("ChartForgeX.Markup.Cli timed out.");
        }

        return (process.ExitCode, standardOutput.GetAwaiter().GetResult(), standardError.GetAwaiter().GetResult());
    }

    private static string FindMarkupCliDll() {
        var root = FindRepositoryRoot();
        foreach (var configuration in new[] { "Release", "Debug" }) {
            var candidate = Path.Combine(root, "ChartForgeX.Markup.Cli", "bin", configuration, "net8.0", "ChartForgeX.Markup.Cli.dll");
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException("Build ChartForgeX.Markup.Cli before running CLI stream smoke tests.");
    }

    private static string Diagnostics<TDocument>(MarkupParseResult<TDocument> result) where TDocument : class =>
        string.Join("; ", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Severity + ":" + diagnostic.Message));

    private static string Diagnostics(VisualMarkupParseResult result) =>
        string.Join("; ", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Severity + ":" + diagnostic.Message));
}
