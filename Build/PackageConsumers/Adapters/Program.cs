using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Markup;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;


internal static class Program {
    private static void Main() {
        PackageAssertions.CoreFixtures();
        NativeViewportSection();
        var values = ChartSeries.CreateLine(new ChartSeriesId("package-values"), "Values",
            new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });
        var bubble = ChartSeries.CreateBubble(new ChartSeriesId("0"), "Values", new[] { new ChartBubble(1.5, 2.5, 7) })
            .WithObservationId(0, new ChartObservationId("package-bubble"));
        var chart = Chart.Create().WithTitle("Package smoke").WithSize(320, 180)
            .AddSeries(values).AddSeries(bubble);
        PackageAssertions.Require(ReferenceEquals(chart.Series[0], values) && ReferenceEquals(chart.Series[1], bubble),
            "Packed chart attachment did not retain the authored series handles.");
        PackageAssertions.Require(bubble.Id.Value == "0" && bubble.Bubbles.Count == 1 && bubble.SourcePointCount == 1 && bubble.Points.Count == 0,
            "Packed Bubble construction did not preserve its stable identity and single typed observation.");
        var artifact = chart.ToVisualArtifact("package-chart");
        PackageAssertions.Require(artifact.Metadata["chart.series.0.id"] == values.Id.Value && artifact.Metadata["chart.series.1.id"] == "0" &&
            artifact.Metadata["chart.series.1.scope"] == "shared", "Packed artifact metadata confused a numeric-looking stable ID with a series ordinal.");
        var chartSvg = chart.ToSvg();
        if (!chartSvg.Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("SVG render failed.");
        var bubblePoints = XDocument.Parse(chartSvg).Descendants().Where(element =>
            (string?)element.Attribute("data-cfx-role") == "point" && (string?)element.Attribute("data-cfx-series-key") == "0").ToArray();
        PackageAssertions.Require(bubblePoints.Length == 1 && (string?)bubblePoints[0].Attribute("data-cfx-series") == "1" &&
            (string?)bubblePoints[0].Attribute("data-cfx-source-point") == "0" && (string?)bubblePoints[0].Attribute("data-cfx-source-count") == "1" &&
            (string?)bubblePoints[0].Attribute("data-cfx-size") == "7" && (string?)bubblePoints[0].Attribute("data-cfx-observation-id") == "package-bubble",
            "Packed Bubble SVG lost typed observation facts or mapped the stable series ID to its numeric ordinal.");
        if (!chart.ToHtmlFragment().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("HTML render failed.");
        if (chart.ToPng().Length <= 64) throw new InvalidOperationException("PNG render failed.");
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
            options.Interaction.AddScenario("package-series", "Selected series", scenario => scenario.AddSeriesStep(bubble.Id, "Bubble observation"));
            options.Interaction.WithActiveScenario("package-series");
        });
        PackageAssertions.Require(html.Contains("data-cfx-scenario-target-id=\"0\"", StringComparison.Ordinal) &&
            html.Contains("data-cfx-scenario-target-kind=\"series\"", StringComparison.Ordinal),
            "Packed HTML adapter did not accept the typed stable series ID in its scenario step.");
        if (!html.Contains("data-cfx-zoom=\"in\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML zoom controls missing.");
        if (!html.Contains("data-cfx-mode-button=\"brush\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML brush controls missing.");
        if (!html.Contains("data-cfx-export=\"svg\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML export controls missing.");
        if (!html.Contains("data-cfx-export=\"png\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML PNG export controls missing.");
        if (!html.Contains("new CustomEvent('cfxsync'", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive HTML sync events missing.");
        var dashboard = new[] { chart, chart }.ToInteractiveHtmlDashboardPage(options => {
            options.IdScope = "package-dashboard";
            options.Interaction.GroupName = "package-group";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.SynchronizedCharts);
        });
        if (!dashboard.Contains("class=\"cfx-dashboard\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive dashboard surface missing.");
        if (!dashboard.Contains("data-cfx-chart-id=\"package-dashboard-2\"", StringComparison.Ordinal)) throw new InvalidOperationException("Interactive dashboard child chart IDs missing.");

        var graph = GraphScene.Create("package-graph", "Package graph")
            .AddNode("api", "API", node => {
                node.Kind = "service";
                node.Status = "healthy";
            })
            .AddNode("db", "Database", node => {
                node.Kind = "database";
                node.Status = "warning";
            })
            .AddEdge("api-db", "api", "db", "queries", edge => edge.Kind = "dependency");
        graph.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization);
        graph.Options.Physics.Solver = GraphPhysicsSolver.Repulsion;
        var graphHtml = graph.ToGraphExplorerHtmlPage();
        if (!graphHtml.Contains("data-cfx-graph-id=\"package-graph\"", StringComparison.Ordinal)) throw new InvalidOperationException("Graph explorer package surface missing.");
        if (!graphHtml.Contains("data-cfx-graph-physics=\"Repulsion\"", StringComparison.Ordinal)) throw new InvalidOperationException("Graph explorer physics profile missing.");

        var mermaid = new MermaidParser().ParseFlowchart("---\nconfig:\n  theme: dark\n---\nflowchart LR\n  a[Start] --> b[Done]");
        if (mermaid.HasErrors || mermaid.Document is null) throw new InvalidOperationException("Mermaid package parser failed.");
        if (mermaid.Document.Nodes.Count != 2 || mermaid.Document.Edges.Count != 1) throw new InvalidOperationException("Mermaid package parser model missing nodes or edges.");
        var mermaidArtifact = mermaid.Document.ToVisualArtifact(new MermaidFlowchartRenderOptions { Id = "package-mermaid" });
        if (mermaidArtifact.Kind != VisualArtifactKind.Mermaid || !mermaidArtifact.SupportsExport(VisualArtifactExportFormat.Svg)) throw new InvalidOperationException("Mermaid package artifact contract missing.");
        PackageAssertions.Require(mermaidArtifact.Model is TopologyChart mermaidTopology && mermaidTopology.ThemeMode == VisualThemeMode.Dark &&
            mermaidTopology.Theme.ToJson() == VisualTheme.Graphite().ToJson(), "Packed Mermaid conversion did not preserve the canonical paired theme and explicit dark mode.");
        var mermaidClass = new MermaidParser().ParseClass("classDiagram\nclass User\nUser <|-- Admin");
        if (mermaidClass.HasErrors || mermaidClass.Document is null || mermaidClass.Document.Classes.Count != 2) throw new InvalidOperationException("Mermaid package class parser failed.");
        if (mermaidClass.Document.ToVisualArtifact().Model is not ChartForgeX.Topology.TopologyChart) throw new InvalidOperationException("Mermaid package class artifact contract missing.");
        var markupMermaid = new MermaidVisualMarkupParser().Parse("~~~mermaid {#package-flow}\nflowchart LR\n  a --> b\n~~~");
        if (markupMermaid.HasErrors || markupMermaid.Artifacts.Count != 1 || markupMermaid.Artifacts[0].Id != "package-flow") throw new InvalidOperationException("Mermaid markup package parser failed.");
        var useCase = MermaidRenderer.Render("usecase-beta\nactor User\nUser --> Action(Do work)", new MermaidRenderOptions());
        if (useCase.HasErrors || useCase.Artifact is null) throw new InvalidOperationException("Mermaid source package renderer failed.");
        if (!useCase.Artifact.ToSvg().Contains("data-node-shape=\"Actor\"", StringComparison.Ordinal) || useCase.Artifact.ToPng().Length <= 64) throw new InvalidOperationException("Mermaid source package SVG/PNG output failed.");
        var useCaseJson = useCase.Artifact.ToInterchangeJson();
        var treemap = new MermaidParser().ParseTreemap("treemap-beta\n\"Storage\"\n  \"Documents\": 1,234.5\n  \"Media\": 765.5\n");
        if (treemap.HasErrors || treemap.Document is null) throw new InvalidOperationException("Packed treemap parser rejected grouped magnitudes.");
        var treemapChart = treemap.Document.ToChart();
        if (treemapChart.Series[0].HierarchyItems[1].Value != 1234.5 || !treemapChart.ToSvg().Contains("data-cfx-role=\"treemap-label\"", StringComparison.Ordinal)) throw new InvalidOperationException("Packed treemap values or native captions were lost.");
        PackageAssertions.Png(treemapChart.ToPng());
        var hiddenTreemap = MermaidRenderer.Render("---\nconfig:\n  treemap:\n    showValues: false\n---\ntreemap-beta\n\"Storage\"\n  \"Documents\": 7\n  \"Media\": 3\n");
        if (hiddenTreemap.HasErrors || hiddenTreemap.Artifact?.Model is not ChartForgeX.Core.Chart hiddenChart || hiddenChart.Options.Treemap.ShowValues) throw new InvalidOperationException("Packed treemap source visibility was lost.");
        var hostTreemap = new MermaidParser().ParseTreemap(hiddenTreemap.Document!.SourceText).Document!.ToChart(new MermaidTreemapRenderOptions { ShowValues = true });
        if (!hostTreemap.Options.Treemap.ShowValues || hostTreemap.Series[0].HierarchyItems[1].Value != 7) throw new InvalidOperationException("Packed treemap host visibility or semantic values were lost.");
        PackageAssertions.Png(hiddenTreemap.Artifact.ToPng());
        if (VisualArtifactInterchangeEnvelope.FromJson(useCaseJson).ToJson() != useCaseJson) throw new InvalidOperationException("Mermaid source package interchange failed.");

        const string packetSource = "---\nconfig:\n  packet:\n    bitsPerRow: 16\n    showBits: false\n    bitOrder: descending\n    bitWidth: 24\n    rowHeight: 40\n    paddingX: 3\n    paddingY: 7\n---\npacket-beta\n0-3: \"Flags\"\n+14: \"Payload\"";
        var packet = new MermaidParser().ParsePacket(packetSource);
        if (packet.HasErrors || packet.Document is null) throw new InvalidOperationException("Packet package parser failed.");
        var packetBlock = packet.Document.ToPacketLayoutBlock();
        PackageAssertions.Require(packetBlock.BitsPerRow == 16 && !packetBlock.ShowBitNumbers && packetBlock.BitOrder == PacketBitOrder.Descending,
            "Packet source configuration did not reach the native package model.");
        PackageAssertions.Png(packetBlock.ToPng());
        PackageAssertions.Require(packetBlock.BitWidth == 24 && packetBlock.RowHeight == 40 && packetBlock.FieldGap == 3 && packetBlock.RowGap == 7,
            "Packet geometry settings did not reach the native package model.");
        var packetMarkup = new MermaidVisualMarkupParser(new MermaidRenderOptions {
            Packet = new MermaidPacketRenderOptions { ShowBitNumbers = true, RowHeight = 28, FieldGap = 0, RowGap = 0 }
        }).Parse("```mermaid\n" + packetSource + "\n```");
        PackageAssertions.Require(!packetMarkup.HasErrors && packetMarkup.Artifacts.Count == 1 &&
            packetMarkup.Artifacts[0].Model is PacketLayoutBlock fencedPacket && fencedPacket.BitsPerRow == 16 &&
            fencedPacket.ShowBitNumbers && fencedPacket.BitOrder == PacketBitOrder.Descending && fencedPacket.BitWidth == 24 &&
            fencedPacket.RowHeight == 28 && fencedPacket.FieldGap == 0 && fencedPacket.RowGap == 0,
            "Packet package Markdown host overrides did not preserve independent source settings.");
        var packetJson = packetMarkup.Artifacts[0].ToInterchangeJson();
        PackageAssertions.Require(VisualArtifactInterchangeEnvelope.FromJson(packetJson).ToJson() == packetJson, "Packet package interchange failed.");

        var tableMarkup = new VisualMarkupParser().Parse("~~~chartforgex table v1 {#package-table}\n| State |\n| --- |\n| Ready |\n~~~");
        if (tableMarkup.HasErrors || tableMarkup.Artifacts.Count != 1)
            throw new InvalidOperationException("Markup table package parser failed.");
        var tableArtifact = tableMarkup.Artifacts[0];
        if (tableArtifact.Model is not TableArtifact || !tableArtifact.ToSvg().Contains("Ready", StringComparison.Ordinal))
            throw new InvalidOperationException("Markup table producer failed.");
        PackageAssertions.Png(tableArtifact.ToPng());
        PackageAssertions.References(typeof(VisualMarkupParser).Assembly, "ChartForgeX", "ChartForgeX.Visuals");
        PackageAssertions.Payload("ChartForgeX", "ChartForgeX.Visuals", "ChartForgeX.Interactivity",
            "ChartForgeX.Interactivity.Html", "ChartForgeX.Markup", "ChartForgeX.Mermaid", "ChartForgeX.Markup.Mermaid");
        Console.WriteLine("Adapter package consumer and Markup table producer passed.");
    }

    private static void NativeViewportSection() {
        var observations = ChartSeries.CreateLine(new ChartSeriesId("native-values"), "Native values",
            new[] { new ChartPoint(0, -100), new ChartPoint(5, 4), new ChartPoint(10, 100) })
            .WithObservationId(1, new ChartObservationId("native-middle"));
        var chart = Chart.Create().WithSize(520, 320).WithLegend(false).WithXAxisBounds(0, 10).AddSeries(observations);
        var renderer = new HtmlInteractiveChartRenderer();
        var full = renderer.PrepareSection(chart);
        var projection = full.PreparedVisual.XProjection ?? throw new InvalidOperationException("Packed HTML section omitted its native X projection.");
        var requested = projection.ConstrainViewport(projection.Project(3), projection.Project(7));
        PackageAssertions.Require(requested is not null && Math.Abs(requested.Minimum - 3) < 1e-9 && Math.Abs(requested.Maximum - 7) < 1e-9,
            "Packed logical gesture did not produce the source X window.");
        var narrowed = renderer.PrepareSection(chart, requested);
        PackageAssertions.Require(narrowed.PreparedVisual.XProjection is ChartXProjection window && window.Minimum > 2.99 && window.Maximum < 7.01 &&
            narrowed.Html.Contains("data-cfx-observation-id=\"native-middle\"", StringComparison.Ordinal) &&
            !narrowed.Html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Packed native section lost its window, source identity or asset-free contract.");
        PackageAssertions.Require(!full.PreparedVisual.Regions.Where(region => region.Role == "axis-x-label").Select(region => region.Label)
            .SequenceEqual(narrowed.PreparedVisual.Regions.Where(region => region.Role == "axis-x-label").Select(region => region.Label)),
            "Packed viewport retained full-domain X tick captions.");
        PackageAssertions.Require(full.PreparedVisual.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label)
            .SequenceEqual(narrowed.PreparedVisual.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label)),
            "Packed viewport changed the full-data Y domain.");
        PackageAssertions.Png(narrowed.PreparedVisual.ToPng());
        PackageAssertions.Require(renderer.PrepareSection(chart).Html == full.Html && chart.Options.XAxis.Minimum == 0 && chart.Options.XAxis.Maximum == 10,
            "Packed native reset did not recover the full section or mutated authored bounds.");
    }
}
