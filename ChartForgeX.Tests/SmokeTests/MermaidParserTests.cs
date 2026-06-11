using System;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserDetectsFlowchartHeaderAndSourceSpans() {
        const string source = @"flowchart LR
  user[User] --> app[Native app]
  app --> cfx[ChartForgeX]";

        var result = new MermaidParser().ParseFlowchart(source);

        Assert(!result.HasErrors, "Mermaid flowchart parser should parse a flowchart header without errors: " + MermaidDiagnostics(result));
        Assert(result.Document != null, "Mermaid flowchart parser should produce a document.");
        Assert(result.Document!.Kind == MermaidDiagramKind.Flowchart, "Mermaid flowchart document should report the flowchart diagram kind.");
        Assert(result.Document.Header == "flowchart LR", "Mermaid flowchart document should preserve the header line.");
        Assert(result.Document.Direction == MermaidFlowchartDirection.LeftToRight, "Mermaid flowchart document should parse LR direction.");
        Assert(result.Document.HeaderSpan.Line == 1 && result.Document.HeaderSpan.Column == 1, "Mermaid flowchart header should preserve source span.");
        Assert(result.Document.Statements.Count == 2, "Mermaid flowchart parser should preserve body statements for later semantic parsing.");
        Assert(result.Document.Statements[0].Span.Line == 2 && result.Document.Statements[0].Span.Column == 3, "Mermaid flowchart statements should preserve source spans.");
        Assert(result.Document.Statements[0].Text.Contains("-->", StringComparison.Ordinal), "Mermaid flowchart statements should preserve raw syntax.");
        Assert(result.Document.Nodes.Count == 3, "Mermaid flowchart parser should extract node references from edge statements.");
        Assert(result.Document.Edges.Count == 2, "Mermaid flowchart parser should extract edge statements.");
        Assert(result.Document.Nodes[0].Id == "user" && result.Document.Nodes[0].Text == "User", "Mermaid flowchart parser should parse bracket node labels.");
        Assert(result.Document.Edges[0].SourceId == "user" && result.Document.Edges[0].TargetId == "app", "Mermaid flowchart parser should preserve edge endpoints.");
    }

    private static void MermaidParserParsesFlowchartNodesEdgesAndLabels() {
        const string source = @"flowchart LR
  user[User] -->|opens| app(Native app)
  app -- renders --> cfx{ChartForgeX}
  cfx -.-> report((Report))";

        var result = new MermaidParser().ParseFlowchart(source);

        Assert(!result.HasErrors, "Mermaid flowchart parser should parse common nodes, edges, and labels: " + MermaidDiagnostics(result));
        Assert(result.Document != null, "Mermaid flowchart parser should produce a document.");
        Assert(result.Document!.Nodes.Count == 4, "Mermaid flowchart parser should de-duplicate nodes by id.");
        Assert(result.Document.Edges.Count == 3, "Mermaid flowchart parser should parse each edge.");
        Assert(result.Document.Nodes[0].Shape == MermaidFlowchartNodeShape.Rectangle, "Mermaid flowchart parser should parse rectangle nodes.");
        Assert(result.Document.Nodes[1].Shape == MermaidFlowchartNodeShape.Rounded, "Mermaid flowchart parser should parse rounded nodes.");
        Assert(result.Document.Nodes[2].Shape == MermaidFlowchartNodeShape.Rhombus, "Mermaid flowchart parser should parse rhombus nodes.");
        Assert(result.Document.Nodes[3].Shape == MermaidFlowchartNodeShape.Circle, "Mermaid flowchart parser should parse circle nodes.");
        Assert(result.Document.Edges[0].Label == "opens", "Mermaid flowchart parser should parse pipe edge labels.");
        Assert(result.Document.Edges[1].Label == "renders", "Mermaid flowchart parser should parse inline edge labels.");
        Assert(result.Document.Edges[2].Operator == "-.->", "Mermaid flowchart parser should preserve dotted edge operators.");
    }

    private static void MermaidParserParsesFlowchartChainedEdgesAndStandaloneNodes() {
        const string source = @"graph TD
  A[/Input/] --> B[(Store)] --> C{{Decision}}
  C --> D[\Output\]";

        var result = new MermaidParser().ParseFlowchart(source);

        Assert(!result.HasErrors, "Mermaid flowchart parser should parse chained edges: " + MermaidDiagnostics(result));
        Assert(result.Document != null, "Mermaid flowchart parser should produce a document.");
        Assert(result.Document!.Nodes.Count == 4, "Mermaid flowchart parser should parse all nodes in a chained edge.");
        Assert(result.Document.Edges.Count == 3, "Mermaid flowchart parser should split chained edge statements into individual edges.");
        Assert(result.Document.Edges[0].SourceId == "A" && result.Document.Edges[0].TargetId == "B", "Mermaid flowchart parser should parse the first chained edge.");
        Assert(result.Document.Edges[1].SourceId == "B" && result.Document.Edges[1].TargetId == "C", "Mermaid flowchart parser should parse the second chained edge.");
        Assert(result.Document.Nodes[0].Shape == MermaidFlowchartNodeShape.Parallelogram, "Mermaid flowchart parser should parse slash parallelogram nodes.");
        Assert(result.Document.Nodes[1].Shape == MermaidFlowchartNodeShape.Cylinder, "Mermaid flowchart parser should parse cylinder nodes.");
        Assert(result.Document.Nodes[2].Shape == MermaidFlowchartNodeShape.Hexagon, "Mermaid flowchart parser should parse hexagon nodes.");
        Assert(result.Document.Nodes[3].Shape == MermaidFlowchartNodeShape.ParallelogramAlt, "Mermaid flowchart parser should parse backslash parallelogram nodes.");
    }

    private static void MermaidParserParsesFlowchartSubgraphsClassesStylesAndClicks() {
        const string source = @"flowchart LR
classDef alert fill:#fee,stroke:#c00,color:#111
subgraph cluster[Cluster]
  api[API]:::alert --> db[(DB)]
end
style db fill:#eef,stroke:#00c
click api href ""https://example.com/api"" ""Open API""
linkStyle 0 stroke:#f00,stroke-dasharray: 5 5";

        var result = new MermaidParser().ParseFlowchart(source);

        Assert(!result.HasErrors, "Mermaid flowchart parser should parse subgraphs, classes, styles, clicks, and link styles: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid flowchart parser should produce a document.");
        Assert(document.Subgraphs.Count == 1 && document.Subgraphs[0].Id == "cluster", "Mermaid flowchart parser should parse subgraph ids.");
        Assert(document.Subgraphs[0].NodeIds.Count == 2, "Mermaid flowchart parser should associate nodes with their containing subgraph.");
        Assert(document.ClassDefinitions.Count == 1 && document.ClassDefinitions[0].Styles["fill"] == "#fee", "Mermaid flowchart parser should parse classDef styles.");
        Assert(document.Nodes[0].SubgraphId == "cluster", "Mermaid flowchart nodes should retain subgraph membership.");
        Assert(document.Nodes[0].Classes.Count == 1 && document.Nodes[0].Classes[0] == "alert", "Mermaid flowchart parser should parse inline node classes.");
        Assert(document.Nodes[0].Styles["stroke"] == "#c00", "Mermaid flowchart parser should apply class definition styles to nodes.");
        Assert(document.Nodes[0].Href == "https://example.com/api" && document.Nodes[0].Tooltip == "Open API", "Mermaid flowchart parser should parse click href and tooltip metadata.");
        Assert(document.Nodes[1].Styles["fill"] == "#eef" && document.Nodes[1].Styles["stroke"] == "#00c", "Mermaid flowchart parser should parse direct node styles.");
        Assert(document.LinkStyles.Count == 1, "Mermaid flowchart parser should retain linkStyle declarations.");
        Assert(document.Edges[0].Styles["stroke"] == "#f00" && document.Edges[0].Styles["stroke-dasharray"] == "5 5", "Mermaid flowchart parser should apply linkStyle declarations to selected edges.");
    }

    private static void MermaidParserPreservesStyleAndClickBeforeNodeDefinition() {
        const string source = @"flowchart LR
style api fill:#eef,stroke:#00c
click api href ""https://example.com/api"" ""Open API""
api[API] --> worker[Worker]";

        var result = new MermaidParser().ParseFlowchart(source);

        Assert(!result.HasErrors, "Mermaid flowchart parser should parse out-of-order style and click metadata: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid flowchart parser should produce a document.");
        var api = document.Nodes.Find(node => node.Id == "api");
        Assert(api != null, "Mermaid flowchart parser should retain nodes created by style and click declarations.");
        Assert(api!.Text == "API", "Mermaid flowchart parser should update placeholder nodes when the real node definition appears.");
        Assert(api.Styles["fill"] == "#eef" && api.Styles["stroke"] == "#00c", "Mermaid flowchart parser should keep styles declared before the node.");
        Assert(api.Href == "https://example.com/api" && api.Tooltip == "Open API", "Mermaid flowchart parser should keep click metadata declared before the node.");
    }

    private static void MermaidParserPreservesFrontMatterAndDirectives() {
        const string source = @"---
title: Native Visual
---
%%{ init: { 'theme': 'base' } }%%
%% plain comment

graph TD
  a --> b";

        var result = new MermaidParser().Parse(source);

        Assert(!result.HasErrors, "Mermaid parser should parse source with frontmatter and directives: " + MermaidDiagnostics(result));
        Assert(result.Document is MermaidFlowchartDocument, "Mermaid parser should return a flowchart document for graph syntax.");
        Assert(result.Document!.FrontMatter != null && result.Document.FrontMatter.Contains("title: Native Visual", StringComparison.Ordinal), "Mermaid parser should preserve frontmatter.");
        Assert(result.Document.Directives.Count == 1, "Mermaid parser should preserve directive comments separately from plain comments.");
        Assert(result.Document.Directives[0].Span.Line == 4, "Mermaid directives should preserve source line.");
        Assert(((MermaidFlowchartDocument)result.Document).Direction == MermaidFlowchartDirection.TopDown, "Mermaid graph TD should map top-down direction.");
    }

    private static void MermaidFlowchartConvertsToTopologyArtifact() {
        const string source = @"---
title: Native Visual
---
flowchart LR
  user[User] -->|opens| app(Native app)
  app -.-> cfx{ChartForgeX}";

        var result = new MermaidParser().ParseFlowchart(source);
        Assert(!result.HasErrors, "Mermaid flowchart parser should parse artifact source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid flowchart parser should produce a document.");

        var topology = document.ToTopologyChart(new MermaidFlowchartRenderOptions { Id = "native-visual" });
        Assert(topology.Id == "native-visual", "Mermaid flowchart topology conversion should preserve caller-provided ids.");
        Assert(topology.Title == "Native Visual", "Mermaid flowchart topology conversion should use frontmatter title by default.");
        Assert(topology.LayoutMode == TopologyLayoutMode.Layered, "Mermaid flowchart topology conversion should use deterministic layered layout.");
        Assert(topology.LayoutDirection == TopologyLayoutDirection.LeftToRight, "Mermaid flowchart topology conversion should preserve Mermaid LR direction.");
        Assert(topology.Nodes.Count == 3 && topology.Edges.Count == 2, "Mermaid flowchart topology conversion should map parsed nodes and edges.");
        Assert(topology.Nodes[2].Metadata["mermaid.shape"] == MermaidFlowchartNodeShape.Rhombus.ToString(), "Mermaid flowchart topology conversion should preserve node shape metadata.");
        Assert(topology.Edges[0].Label == "opens", "Mermaid flowchart topology conversion should preserve edge labels.");
        Assert(topology.Edges[1].LineStyle == TopologyEdgeLineStyle.Dotted, "Mermaid flowchart topology conversion should map dotted Mermaid edges.");
        Assert(topology.Edges[1].Metadata["mermaid.operator"] == "-.->", "Mermaid flowchart topology conversion should preserve raw edge operators.");

        var artifact = document.ToVisualArtifact(new MermaidFlowchartRenderOptions { Id = "native-visual" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid flowchart visual artifact should report Mermaid artifact kind.");
        Assert(artifact.SourceLanguage == VisualArtifactSourceLanguage.Mermaid, "Mermaid flowchart visual artifact should preserve source language.");
        Assert(artifact.Model is TopologyChart, "Mermaid flowchart visual artifact should carry the renderable topology model.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Svg) && artifact.SupportsExport(VisualArtifactExportFormat.Png), "Mermaid flowchart visual artifact should declare static SVG and PNG exports.");
        Assert(artifact.Metadata["mermaid.nodes"] == "3" && artifact.Metadata["mermaid.edges"] == "2", "Mermaid flowchart visual artifact should expose model counts.");
    }

    private static void MermaidFlowchartConversionPreservesSubgraphsAndStyles() {
        const string source = @"flowchart LR
classDef alert fill:#fee,stroke:#c00,color:#111
subgraph cluster[Cluster]
  api[API]:::alert --> db[(DB)]
end
style db fill:#eef,stroke:#00c
click api href ""https://example.com/api"" ""Open API""
linkStyle 0 stroke:#f00,stroke-dasharray: 5 5";

        var result = new MermaidParser().ParseFlowchart(source);
        Assert(!result.HasErrors, "Mermaid flowchart parser should parse styled source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid flowchart parser should produce a document.");

        var topology = document.ToTopologyChart(new MermaidFlowchartRenderOptions { Id = "styled-flow" });
        Assert(topology.Groups.Count == 1 && topology.Groups[0].Id == "cluster", "Mermaid flowchart conversion should map subgraphs to topology groups.");
        Assert(topology.Nodes[0].GroupId == "cluster", "Mermaid flowchart conversion should preserve subgraph membership on nodes.");
        Assert(topology.Nodes[0].Href == "https://example.com/api" && topology.Nodes[0].Tooltip == "Open API", "Mermaid flowchart conversion should map click links to topology node links.");
        Assert(topology.Nodes[0].BackgroundColor == "#fee" && topology.Nodes[0].Color == "#c00", "Mermaid flowchart conversion should map class styles to node colors.");
        Assert(topology.Nodes[0].Metadata["mermaid.classes"] == "alert", "Mermaid flowchart conversion should preserve class metadata.");
        Assert(topology.Nodes[1].BackgroundColor == "#eef" && topology.Nodes[1].Color == "#00c", "Mermaid flowchart conversion should map direct node styles.");
        Assert(topology.Edges[0].Color == "#f00" && topology.Edges[0].LineStyle == TopologyEdgeLineStyle.Dashed, "Mermaid flowchart conversion should map linkStyle to edge styling.");

        var artifact = document.ToVisualArtifact(new MermaidFlowchartRenderOptions { Id = "styled-flow" });
        Assert(artifact.Metadata["mermaid.subgraphs"] == "1", "Mermaid flowchart artifacts should expose subgraph counts.");
        Assert(artifact.Metadata["mermaid.classDefinitions"] == "1", "Mermaid flowchart artifacts should expose class definition counts.");
        Assert(artifact.Metadata["mermaid.linkStyles"] == "1", "Mermaid flowchart artifacts should expose link style counts.");
    }

    private static void MermaidFlowchartRendersStaticSvgAndPng() {
        const string source = @"flowchart TD
  a[Start] --> b[(Store)]
  b --> c((Done))";

        var result = new MermaidParser().ParseFlowchart(source);
        Assert(!result.HasErrors, "Mermaid flowchart parser should parse render source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid flowchart parser should produce a document.");

        var options = new MermaidFlowchartRenderOptions { Id = "render-proof", Title = "Render proof", Width = 640, Height = 360 };
        var svg = document.ToSvg(options);
        var png = document.ToPng(options);

        Assert(svg.Contains("<svg", StringComparison.Ordinal) && svg.Contains("Render proof", StringComparison.Ordinal), "Mermaid flowchart SVG rendering should emit a topology SVG.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid flowchart PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserParsesPieTitleShowDataAndSlices() {
        const string source = @"pie showData
title ""Result Mix""
""Passed"" : 1260
""Warnings"" : 68.5
""Failed"" : 10";

        var result = new MermaidParser().ParsePie(source);

        Assert(!result.HasErrors, "Mermaid pie parser should parse title, showData, and slices: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid pie parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Pie, "Mermaid pie parser should produce a pie document.");
        Assert(document.ShowData, "Mermaid pie parser should parse showData from the header.");
        Assert(document.Title == "Result Mix", "Mermaid pie parser should parse quoted title text.");
        Assert(document.Slices.Count == 3, "Mermaid pie parser should parse slices.");
        Assert(document.Slices[1].Label == "Warnings" && Math.Abs(document.Slices[1].Value - 68.5) < 0.001, "Mermaid pie parser should parse slice labels and numeric values.");
        Assert(document.Slices[0].Span.Line == 3, "Mermaid pie slices should preserve source spans.");
    }

    private static void MermaidPieConvertsToChartArtifactAndRenders() {
        const string source = @"pie
title ""Domain Checks""
""Passed"" : 1260
""Warnings"" : 68
""Failed"" : 10";

        var result = new MermaidParser().ParsePie(source);
        Assert(!result.HasErrors, "Mermaid pie parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid pie parser should produce a document.");

        var chart = document.ToChart(new MermaidPieRenderOptions { Id = "domain-checks", Width = 640, Height = 360 });
        Assert(chart.Series.Count == 1 && chart.Series[0].Kind == ChartSeriesKind.Pie, "Mermaid pie conversion should produce a ChartForgeX pie chart.");
        Assert(chart.Title == "Domain Checks", "Mermaid pie conversion should use Mermaid title by default.");
        Assert(chart.Options.XAxisLabels.Count == 3, "Mermaid pie conversion should preserve labels as chart axis labels.");

        var artifact = document.ToVisualArtifact(new MermaidPieRenderOptions { Id = "domain-checks" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid pie visual artifact should report Mermaid artifact kind.");
        Assert(artifact.SourceLanguage == VisualArtifactSourceLanguage.Mermaid, "Mermaid pie visual artifact should preserve source language.");
        Assert(artifact.Model is Chart, "Mermaid pie visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.slices"] == "3", "Mermaid pie artifacts should expose slice counts.");
        Assert(artifact.Metadata["render.model"] == nameof(Chart), "Mermaid pie artifacts should expose the chart render model.");

        var svg = document.ToSvg(new MermaidPieRenderOptions { Id = "domain-checks" });
        var png = document.ToPng(new MermaidPieRenderOptions { Id = "domain-checks" });
        Assert(svg.Contains("data-cfx-role=\"pie-slice\"", StringComparison.Ordinal), "Mermaid pie SVG rendering should emit ChartForgeX pie slices.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid pie PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserRejectsInvalidPieValues() {
        const string source = @"pie
""Valid"" : 10
""Invalid"" : -1";

        var result = new MermaidParser().ParsePie(source);

        Assert(result.HasErrors, "Mermaid pie parser should reject non-positive values.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("positive numbers", StringComparison.Ordinal)), "Invalid pie value diagnostics should explain the current contract.");
    }

    private static void MermaidParserParsesTimelineSectionsPeriodsAndEvents() {
        const string source = @"timeline TD
title Native Projection
section OfficeIMO
2026 Q1 : Parse Markdown
        : Preserve visual fences
section ChartForgeX
2026 Q2 : Render visual artifacts : Export SVG and PNG";

        var result = new MermaidParser().ParseTimeline(source);

        Assert(!result.HasErrors, "Mermaid timeline parser should parse sections, periods, and continuation events: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid timeline parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Timeline, "Mermaid timeline parser should produce a timeline document.");
        Assert(document.Direction == MermaidTimelineDirection.TopDown, "Mermaid timeline parser should parse TD direction.");
        Assert(document.Title == "Native Projection", "Mermaid timeline parser should parse title.");
        Assert(document.Sections.Count == 2, "Mermaid timeline parser should parse sections.");
        Assert(document.Periods.Count == 2, "Mermaid timeline parser should parse periods.");
        Assert(document.Periods[0].Section == "OfficeIMO", "Mermaid timeline periods should retain section membership.");
        Assert(document.Periods[0].Events.Count == 2, "Mermaid timeline parser should parse continuation events.");
        Assert(document.Periods[1].Events.Count == 2, "Mermaid timeline parser should parse multiple inline events.");
    }

    private static void MermaidTimelineConvertsToChartArtifactAndRenders() {
        const string source = @"timeline
title Native Visuals
section OfficeIMO
Markdown AST : Visual fence blocks
section ChartForgeX
Artifacts : SVG preview : PNG preview";

        var result = new MermaidParser().ParseTimeline(source);
        Assert(!result.HasErrors, "Mermaid timeline parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid timeline parser should produce a document.");

        var chart = document.ToChart(new MermaidTimelineRenderOptions { Id = "native-visuals", Width = 720, Height = 420 });
        Assert(chart.Series.Count == 3, "Mermaid timeline conversion should produce one timeline item per event.");
        Assert(chart.Series[0].Kind == ChartSeriesKind.Timeline, "Mermaid timeline conversion should produce ChartForgeX timeline series.");
        Assert(chart.Title == "Native Visuals", "Mermaid timeline conversion should use Mermaid title by default.");

        var artifact = document.ToVisualArtifact(new MermaidTimelineRenderOptions { Id = "native-visuals" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid timeline visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is Chart, "Mermaid timeline visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.sections"] == "2", "Mermaid timeline artifacts should expose section counts.");
        Assert(artifact.Metadata["mermaid.events"] == "3", "Mermaid timeline artifacts should expose event counts.");

        var svg = document.ToSvg(new MermaidTimelineRenderOptions { Id = "native-visuals" });
        var png = document.ToPng(new MermaidTimelineRenderOptions { Id = "native-visuals" });
        Assert(svg.Contains("data-cfx-role=\"timeline\"", StringComparison.Ordinal), "Mermaid timeline SVG rendering should emit ChartForgeX timeline output.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid timeline PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserParsesXYChartAxesOrientationAndSeries() {
        const string source = @"xychart-beta horizontal
title ""Adoption Trend""
x-axis ""Quarter"" [Q1, ""Q2 launch"", Q3]
y-axis Score 0 --> 100
bar [42, 58, 73]
line [40, 61, 80]";

        var result = new MermaidParser().ParseXYChart(source);

        Assert(!result.HasErrors, "Mermaid XY chart parser should parse title, axes, orientation, and series: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid XY chart parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.XYChart, "Mermaid XY chart parser should produce an XY chart document.");
        Assert(document.Orientation == MermaidXYChartOrientation.Horizontal, "Mermaid XY chart parser should parse horizontal orientation.");
        Assert(document.Title == "Adoption Trend", "Mermaid XY chart parser should parse quoted titles.");
        Assert(document.XAxis.Title == "Quarter" && document.XAxis.Labels.Count == 3, "Mermaid XY chart parser should parse x-axis labels and title.");
        Assert(document.XAxis.Labels[1] == "Q2 launch", "Mermaid XY chart parser should parse quoted category labels.");
        Assert(document.YAxis.Title == "Score" && document.YAxis.Minimum == 0 && document.YAxis.Maximum == 100, "Mermaid XY chart parser should parse y-axis ranges.");
        Assert(document.Series.Count == 2, "Mermaid XY chart parser should parse bar and line series.");
        Assert(document.Series[0].Kind == MermaidXYChartSeriesKind.Bar && document.Series[1].Kind == MermaidXYChartSeriesKind.Line, "Mermaid XY chart parser should preserve series kinds.");
        Assert(Math.Abs(document.Series[1].Values[2] - 80) < 0.001, "Mermaid XY chart parser should parse numeric series values.");
    }

    private static void MermaidXYChartConvertsToChartArtifactAndRenders() {
        const string source = @"xychart-beta
title Tickets
x-axis [Jan, Feb, Mar]
y-axis ""Ticket count"" 0 --> 100
bar [30, 60, 90]
line [25, 50, 80]";

        var result = new MermaidParser().ParseXYChart(source);
        Assert(!result.HasErrors, "Mermaid XY chart parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid XY chart parser should produce a document.");

        var chart = document.ToChart(new MermaidXYChartRenderOptions { Id = "ticket-trend", Width = 720, Height = 420 });
        Assert(chart.Series.Count == 2, "Mermaid XY chart conversion should produce one ChartForgeX series per Mermaid series.");
        Assert(chart.Series[0].Kind == ChartSeriesKind.Bar && chart.Series[1].Kind == ChartSeriesKind.Line, "Mermaid XY chart conversion should map bar and line series.");
        Assert(chart.Title == "Tickets", "Mermaid XY chart conversion should use Mermaid title by default.");
        Assert(chart.Options.XAxisLabels.Count == 3, "Mermaid XY chart conversion should preserve x-axis labels.");

        var artifact = document.ToVisualArtifact(new MermaidXYChartRenderOptions { Id = "ticket-trend" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid XY chart visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is Chart, "Mermaid XY chart visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.series"] == "2", "Mermaid XY chart artifacts should expose series counts.");
        Assert(artifact.Metadata["mermaid.barSeries"] == "1" && artifact.Metadata["mermaid.lineSeries"] == "1", "Mermaid XY chart artifacts should expose series family counts.");

        var svg = document.ToSvg(new MermaidXYChartRenderOptions { Id = "ticket-trend" });
        var png = document.ToPng(new MermaidXYChartRenderOptions { Id = "ticket-trend" });
        Assert(svg.Contains("data-cfx-role=\"bar\"", StringComparison.Ordinal) && svg.Contains("data-cfx-role=\"line-marker\"", StringComparison.Ordinal), "Mermaid XY chart SVG rendering should emit ChartForgeX bar and line marks.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid XY chart PNG rendering should emit a valid PNG.");
    }

    private static void MermaidXYChartHorizontalBarRendersHorizontalBars() {
        const string source = @"xychart-beta horizontal
x-axis [A, B]
bar [10, 20]";

        var result = new MermaidParser().ParseXYChart(source);
        Assert(!result.HasErrors, "Mermaid XY chart parser should parse horizontal bar source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid XY chart parser should produce a document.");

        var chart = document.ToChart(new MermaidXYChartRenderOptions { Id = "horizontal-bars" });
        Assert(chart.Series[0].Kind == ChartSeriesKind.HorizontalBar, "Mermaid horizontal XY bar charts should map to ChartForgeX horizontal bar series.");
        Assert(document.ToSvg(new MermaidXYChartRenderOptions { Id = "horizontal-bars" }).Contains("data-cfx-role=\"horizontal-bar\"", StringComparison.Ordinal), "Mermaid horizontal XY bar charts should render horizontal bar marks.");
    }

    private static void MermaidParserParsesSankeyCsvLinks() {
        const string source = @"sankey-beta
Discovered,Validated,70
""Validated, high confidence"",Remediated,44
Remediated,""Closed """"done"""" "",31";

        var result = new MermaidParser().ParseSankey(source);

        Assert(!result.HasErrors, "Mermaid Sankey parser should parse CSV rows with quoted commas and doubled quotes: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Sankey parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Sankey, "Mermaid Sankey parser should produce a Sankey document.");
        Assert(document.Links.Count == 3, "Mermaid Sankey parser should parse one link per CSV row.");
        Assert(document.Links[1].Source == "Validated, high confidence", "Mermaid Sankey parser should preserve quoted commas in labels.");
        Assert(document.Links[2].Target == "Closed \"done\"", "Mermaid Sankey parser should unescape doubled quotes in quoted fields.");
        Assert(Math.Abs(document.Links[2].Value - 31) < 0.001, "Mermaid Sankey parser should parse link values.");
        Assert(document.Links[0].Span.Line == 2, "Mermaid Sankey links should preserve source spans.");
    }

    private static void MermaidSankeyConvertsToChartArtifactAndRenders() {
        const string source = @"sankey
Discovered,Validated,70
Validated,Remediated,44
Validated,Deferred,12";

        var result = new MermaidParser().ParseSankey(source);
        Assert(!result.HasErrors, "Mermaid Sankey parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Sankey parser should produce a document.");

        var chart = document.ToChart(new MermaidSankeyRenderOptions { Id = "triage-flow", Title = "Triage Flow", Width = 720, Height = 420 });
        Assert(chart.Series.Count == 1 && chart.Series[0].Kind == ChartSeriesKind.Sankey, "Mermaid Sankey conversion should produce a ChartForgeX Sankey chart.");
        Assert(chart.Title == "Triage Flow", "Mermaid Sankey conversion should use caller-provided titles.");

        var artifact = document.ToVisualArtifact(new MermaidSankeyRenderOptions { Id = "triage-flow" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid Sankey visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is Chart, "Mermaid Sankey visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.links"] == "3", "Mermaid Sankey artifacts should expose link counts.");
        Assert(artifact.Metadata["render.model"] == nameof(Chart), "Mermaid Sankey artifacts should expose the chart render model.");

        var svg = document.ToSvg(new MermaidSankeyRenderOptions { Id = "triage-flow" });
        var png = document.ToPng(new MermaidSankeyRenderOptions { Id = "triage-flow" });
        Assert(svg.Contains("data-cfx-role=\"sankey-link\"", StringComparison.Ordinal), "Mermaid Sankey SVG rendering should emit ChartForgeX Sankey links.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid Sankey PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserRejectsInvalidSankeyRows() {
        const string source = @"sankey-beta
Discovered,Validated,0
Too,Few";

        var result = new MermaidParser().ParseSankey(source);

        Assert(result.HasErrors, "Mermaid Sankey parser should reject invalid CSV rows.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("positive numbers", StringComparison.Ordinal)), "Invalid Sankey values should explain the positive-number contract.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("exactly three columns", StringComparison.Ordinal)), "Invalid Sankey rows should explain the three-column contract.");
    }

    private static void MermaidParserParsesRadarAxesOptionsAndCurves() {
        const string source = @"radar-beta
title Capability Radar
axis ux[""User Experience""], api[""API""], ops[""Operations""]
curve current[""Current""]{70, 65, 82}
curve target[""Target""]{ux: 90, api: 88, ops: 92}
showLegend true
min 0
max 100
ticks 5
graticule polygon";

        var result = new MermaidParser().ParseRadar(source);

        Assert(!result.HasErrors, "Mermaid radar parser should parse axes, curves, and scale options: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid radar parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Radar, "Mermaid radar parser should produce a radar document.");
        Assert(document.Title == "Capability Radar", "Mermaid radar parser should preserve title statements.");
        Assert(document.Axes.Count == 3 && document.Axes[0].Id == "ux" && document.Axes[0].Label == "User Experience", "Mermaid radar parser should parse axis ids and labels.");
        Assert(document.Curves.Count == 2 && document.Curves[1].ValuesByAxisId["ops"] == 92, "Mermaid radar parser should parse ordered and keyed curve values.");
        Assert(document.ShowLegend && document.Minimum == 0 && document.Maximum == 100 && document.Ticks == 5 && document.Graticule == "polygon", "Mermaid radar parser should parse rendering scale and graticule options.");
    }

    private static void MermaidRadarConvertsToChartArtifactAndRenders() {
        const string source = @"radar-beta
title Capability Radar
axis ux[""User Experience""], api[""API""], ops[""Operations""]
curve current[""Current""]{70, 65, 82}
curve target[""Target""]{ux: 90, api: 88, ops: 92}
showLegend true
min 0
max 100
ticks 5";

        var result = new MermaidParser().ParseRadar(source);
        Assert(!result.HasErrors, "Mermaid radar parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid radar parser should produce a document.");

        var chart = document.ToChart(new MermaidRadarRenderOptions { Id = "capability-radar", Width = 720, Height = 520 });
        Assert(chart.Series.Count == 2 && chart.Series[0].Kind == ChartSeriesKind.Radar, "Mermaid radar conversion should produce ChartForgeX radar series.");
        Assert(chart.Title == "Capability Radar", "Mermaid radar conversion should use Mermaid titles by default.");
        Assert(chart.Options.XAxisLabels.Count == 3, "Mermaid radar conversion should preserve axis labels.");
        Assert(chart.Options.YAxisMinimum == 0 && chart.Options.YAxisMaximum == 100 && chart.Options.TickCount == 5, "Mermaid radar conversion should preserve explicit scale options.");

        var artifact = document.ToVisualArtifact(new MermaidRadarRenderOptions { Id = "capability-radar" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid radar visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is Chart, "Mermaid radar visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.axes"] == "3" && artifact.Metadata["mermaid.curves"] == "2", "Mermaid radar artifacts should expose axis and curve counts.");
        Assert(artifact.Metadata["render.model"] == nameof(Chart), "Mermaid radar artifacts should expose the chart render model.");

        var svg = document.ToSvg(new MermaidRadarRenderOptions { Id = "capability-radar" });
        var png = document.ToPng(new MermaidRadarRenderOptions { Id = "capability-radar" });
        Assert(svg.Contains("data-cfx-role=\"radar-chart\"", StringComparison.Ordinal), "Mermaid radar SVG rendering should emit ChartForgeX radar marks.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid radar PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserRejectsInvalidRadarCurves() {
        const string source = @"radar-beta
axis ux, api, ops
curve short{1, 2}
curve unknown{ux: 1, missing: 2}
max 0";

        var result = new MermaidParser().ParseRadar(source);

        Assert(result.HasErrors, "Mermaid radar parser should reject invalid curve values.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("one ordered value per axis", StringComparison.Ordinal)), "Invalid ordered radar values should explain the one-value-per-axis contract.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("unknown axis", StringComparison.Ordinal)), "Invalid keyed radar values should explain unknown axis references.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("max must be greater than min", StringComparison.Ordinal)), "Invalid radar scale should explain the min/max contract.");
    }

    private static void MermaidParserParsesTreemapHierarchyClassesAndValues() {
        const string source = @"treemap-beta
""Infrastructure"":::platform
    ""Identity"": 42
    ""Messaging"": 18 :::risk
    ""Endpoints""
        ""Windows"": 24
        ""macOS"": 9";

        var result = new MermaidParser().ParseTreemap(source);

        Assert(!result.HasErrors, "Mermaid treemap parser should parse quoted hierarchy and class suffixes: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid treemap parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Treemap, "Mermaid treemap parser should produce a treemap document.");
        Assert(document.Roots.Count == 1 && document.Roots[0].Label == "Infrastructure", "Mermaid treemap parser should preserve root section nodes.");
        Assert(document.Nodes.Count == 6, "Mermaid treemap parser should retain every node in source order.");
        Assert(document.Roots[0].ClassName == "platform", "Mermaid treemap parser should preserve section class suffixes.");
        Assert(document.Roots[0].Children[1].ClassName == "risk", "Mermaid treemap parser should preserve leaf class suffixes.");
        Assert(document.Roots[0].Children[2].Children[0].Path == "Infrastructure / Endpoints / Windows", "Mermaid treemap parser should preserve indentation hierarchy.");
        Assert(document.Roots[0].Children[2].Children[1].Value == 9, "Mermaid tremap parser should parse finite numeric leaf values.");
    }

    private static void MermaidTreemapConvertsToChartArtifactAndRenders() {
        const string source = @"treemap-beta
""Infrastructure""
    ""Identity"": 42
    ""Messaging"": 18
    ""Endpoints""
        ""Windows"": 24
        ""macOS"": 9";

        var result = new MermaidParser().ParseTreemap(source);
        Assert(!result.HasErrors, "Mermaid treemap parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid treemap parser should produce a document.");

        var chart = document.ToChart(new MermaidTreemapRenderOptions { Id = "infra-map", Title = "Infrastructure Map", Width = 720, Height = 420 });
        Assert(chart.Series.Count == 1 && chart.Series[0].Kind == ChartSeriesKind.Treemap, "Mermaid treemap conversion should produce a ChartForgeX treemap chart.");
        Assert(chart.Title == "Infrastructure Map", "Mermaid treemap conversion should use caller-provided titles.");
        Assert(chart.Options.XAxisLabels.Count == 4, "Mermaid treemap conversion should render one flat preview item per valued leaf.");
        Assert(chart.Options.XAxisLabels[3].Text == "Infrastructure / Endpoints / macOS", "Mermaid treemap conversion should use hierarchy paths as preview labels.");

        var artifact = document.ToVisualArtifact(new MermaidTreemapRenderOptions { Id = "infra-map" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid treemap visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is Chart, "Mermaid treemap visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.nodes"] == "6" && artifact.Metadata["mermaid.leaves"] == "4", "Mermaid treemap artifacts should expose node and leaf counts.");
        Assert(artifact.Metadata["render.model"] == nameof(Chart), "Mermaid treemap artifacts should expose the chart render model.");

        var svg = document.ToSvg(new MermaidTreemapRenderOptions { Id = "infra-map" });
        var png = document.ToPng(new MermaidTreemapRenderOptions { Id = "infra-map" });
        Assert(svg.Contains("data-cfx-role=\"treemap-tile\"", StringComparison.Ordinal), "Mermaid treemap SVG rendering should emit ChartForgeX treemap tiles.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid treemap PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserRejectsInvalidTreemapNodes() {
        const string source = @"treemap-beta
Unquoted
""Negative"": -1";

        var result = new MermaidParser().ParseTreemap(source);

        Assert(result.HasErrors, "Mermaid treemap parser should reject malformed nodes.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("quoted label", StringComparison.Ordinal)), "Invalid treemap labels should explain the quoted-label contract.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("non-negative", StringComparison.Ordinal)), "Invalid treemap values should explain the non-negative numeric contract.");
    }

    private static void MermaidParserParsesGanttTasksDependenciesAndMilestones() {
        const string source = @"gantt
title Project Plan
dateFormat YYYY-MM-DD
axisFormat %m/%d
section Build
Design : active, des, 2026-01-01, 5d
Implement : crit, impl, after des, 7d
Ship : milestone, ship, after impl, 0d";

        var result = new MermaidParser().ParseGantt(source);

        Assert(!result.HasErrors, "Mermaid Gantt parser should parse tasks, dependencies, and milestones: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Gantt parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.Gantt, "Mermaid Gantt parser should produce a Gantt document.");
        Assert(document.Title == "Project Plan", "Mermaid Gantt parser should preserve title statements.");
        Assert(document.DateFormat == "YYYY-MM-DD" && document.AxisFormat == "%m/%d", "Mermaid Gantt parser should preserve schedule format directives.");
        Assert(document.Sections.Count == 1 && document.Sections[0].Name == "Build", "Mermaid Gantt parser should preserve sections.");
        Assert(document.Tasks.Count == 3, "Mermaid Gantt parser should parse task lines.");
        Assert(document.Tasks[0].Id == "des" && Math.Abs(document.Tasks[0].Progress - 0.5) < 0.001, "Mermaid Gantt parser should parse ids and active progress.");
        Assert(document.Tasks[1].DependencyIds.Count == 1 && document.Tasks[1].DependencyIds[0] == "des" && document.Tasks[1].DependencyIndex == 0, "Mermaid Gantt parser should resolve after dependencies.");
        Assert(document.Tasks[2].IsMilestone && document.Tasks[2].DependencyIndex == 1, "Mermaid Gantt parser should parse milestones with dependencies.");
        Assert(document.Tasks[0].End == new DateTime(2026, 1, 6), "Mermaid Gantt parser should resolve day durations from start dates.");
    }

    private static void MermaidGanttConvertsToChartArtifactAndRenders() {
        const string source = @"gantt
title Project Plan
dateFormat YYYY-MM-DD
axisFormat %m/%d
section Build
Design : active, des, 2026-01-01, 5d
Implement : crit, impl, after des, 7d
Ship : milestone, ship, after impl, 0d";

        var result = new MermaidParser().ParseGantt(source);
        Assert(!result.HasErrors, "Mermaid Gantt parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid Gantt parser should produce a document.");

        var chart = document.ToChart(new MermaidGanttRenderOptions { Id = "project-plan", Width = 720, Height = 420, Today = new DateTime(2026, 1, 8) });
        Assert(chart.Series.Count == 3 && chart.Series[0].Kind == ChartSeriesKind.Gantt, "Mermaid Gantt conversion should produce one ChartForgeX Gantt series per task.");
        Assert(chart.Title == "Project Plan", "Mermaid Gantt conversion should use Mermaid titles by default.");
        Assert(chart.Options.GanttToday.HasValue, "Mermaid Gantt conversion should accept caller-provided today markers.");

        var artifact = document.ToVisualArtifact(new MermaidGanttRenderOptions { Id = "project-plan" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid Gantt visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is Chart, "Mermaid Gantt visual artifact should carry a renderable chart model.");
        Assert(artifact.Metadata["mermaid.tasks"] == "3" && artifact.Metadata["mermaid.milestones"] == "1", "Mermaid Gantt artifacts should expose task and milestone counts.");
        Assert(artifact.Metadata["mermaid.dependencies"] == "2", "Mermaid Gantt artifacts should expose dependency counts.");
        Assert(artifact.Metadata["render.model"] == nameof(Chart), "Mermaid Gantt artifacts should expose the chart render model.");

        var svg = document.ToSvg(new MermaidGanttRenderOptions { Id = "project-plan", Today = new DateTime(2026, 1, 8) });
        var png = document.ToPng(new MermaidGanttRenderOptions { Id = "project-plan", Today = new DateTime(2026, 1, 8) });
        Assert(svg.Contains("data-cfx-role=\"gantt-chart\"", StringComparison.Ordinal), "Mermaid Gantt SVG rendering should emit a ChartForgeX Gantt chart.");
        Assert(svg.Contains("data-cfx-role=\"gantt-milestone\"", StringComparison.Ordinal), "Mermaid Gantt SVG rendering should emit milestones.");
        Assert(svg.Contains("data-cfx-role=\"gantt-dependency\"", StringComparison.Ordinal), "Mermaid Gantt SVG rendering should emit dependency connectors.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid Gantt PNG rendering should emit a valid PNG.");
    }

    private static void MermaidParserRejectsInvalidGanttTasks() {
        const string source = @"gantt
dateFormat YYYY-MM-DD
First : after missing, 2d
Second : bad, 2026-01-01, nope";

        var result = new MermaidParser().ParseGantt(source);

        Assert(result.HasErrors, "Mermaid Gantt parser should reject invalid task metadata.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("after clauses", StringComparison.Ordinal)), "Invalid Gantt dependencies should explain earlier task id requirements.");
        Assert(result.Diagnostics.Exists(diagnostic => diagnostic.Message.Contains("dates or durations", StringComparison.Ordinal)), "Invalid Gantt end values should explain the date-or-duration contract.");
    }

    private static void MermaidParserReportsRecognizedButUnimplementedFamilies() {
        const string source = @"zenuml
  title Order
  A.method()";

        var result = new MermaidParser().Parse(source);

        Assert(!result.HasErrors, "Recognized Mermaid families should remain inspectable while semantic support is pending.");
        Assert(result.Document != null && result.Document.Kind == MermaidDiagramKind.ZenUml, "Mermaid parser should identify recognized diagram families.");
        Assert(result.Diagnostics.Count == 1, "Recognized but unimplemented Mermaid families should produce one diagnostic.");
        Assert(result.Diagnostics[0].Severity == MermaidDiagnosticSeverity.Warning, "Recognized but unimplemented Mermaid families should be warnings.");
        Assert(result.Diagnostics[0].Span.Line == 1, "Mermaid family diagnostics should point at the header line.");
        Assert(result.Diagnostics[0].Message.Contains("not implemented", StringComparison.Ordinal), "Mermaid family diagnostics should be explicit.");
        Assert(result.Document!.RawStatements.Count == 2, "Recognized but unimplemented Mermaid families should retain raw body statements.");
        Assert(result.Document.RawStatements[0].Text == "title Order", "Raw statements should preserve trimmed Mermaid body text.");
        Assert(result.Document.RawStatements[0].Span.Line == 2 && result.Document.RawStatements[0].Span.Column == 3, "Raw statements should preserve one-based source spans.");

        var families = new[] {
            ("zenuml\n  title Order\n  A.method()", MermaidDiagramKind.ZenUml)
        };

        foreach (var family in families) {
            var familyResult = new MermaidParser().Parse(family.Item1);
            Assert(!familyResult.HasErrors, "Recognized Mermaid family should remain inspectable while semantic support is pending: " + family.Item2);
            Assert(familyResult.Document != null && familyResult.Document.Kind == family.Item2, "Mermaid parser should identify recognized family: " + family.Item2);
            Assert(familyResult.Diagnostics.Count == 1 && familyResult.Diagnostics[0].Severity == MermaidDiagnosticSeverity.Warning, "Unimplemented recognized families should produce one warning diagnostic: " + family.Item2);
            Assert(familyResult.Document!.RawStatements.Count > 0, "Unimplemented recognized families should retain raw body statements: " + family.Item2);
        }
    }

    private static void MermaidParserRejectsUnknownDiagramFamily() {
        const string source = @"madeUpDiagram
  a --> b";

        var result = new MermaidParser().Parse(source);

        Assert(result.HasErrors, "Unknown Mermaid diagram families should produce parser errors.");
        Assert(result.Document == null, "Unknown Mermaid diagram families should not produce documents.");
        Assert(result.Diagnostics.Count == 1, "Unknown Mermaid diagram families should produce one diagnostic.");
        Assert(result.Diagnostics[0].Span.Line == 1, "Unknown Mermaid family diagnostics should point at the header line.");
    }

    private static string MermaidDiagnostics<TDocument>(MermaidParseResult<TDocument> result) where TDocument : MermaidDocument =>
        string.Join("; ", result.Diagnostics.ConvertAll(diagnostic => diagnostic.Severity + ":" + diagnostic.Message));
}
