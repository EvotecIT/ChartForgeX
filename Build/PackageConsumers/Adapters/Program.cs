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


internal static partial class Program {
    private static void Main() {
        PackageAssertions.CoreFixtures();
        SequenceLayoutSourceContracts();
        SequenceParticipantViewSourceContracts();
        SequenceMessagePresentationSourceContracts();
        SequenceSelfRoutesSourceContracts();
        SequenceNavigationSourceContracts();
        SequenceSourceDefaultsSourceContracts();
        SequencePresetAppearanceSourceContracts();
        SequenceSurfaceShadowSourceContracts();
        UseCaseJsonTableContracts();
        NativeViewportSection();
        NativeCartesianAndDrawingSection();
        NativeHierarchySection();
        NativeOrganizationHierarchySection();
        RadarSourcePaint();
        QuadrantSourceDepthContracts();
        MermaidSourceDiagnostics();
        MermaidSequencePolicyContracts();
        MermaidArchitectureContracts();
        NativeDiagramContracts();
        SwimlaneLayoutDepthContracts();
        NativeMermaidBlockContracts();
        GitGraphSourcePolicyContracts();
        WardleyOverlayContracts();
        SankeySourcePaintContracts();
        KanbanSourceGeometryContracts();
        NativeMixedCaptionContracts();
        CynefinPathFontContracts();
        XYSourceCaptionContracts();
        UseCaseSourceDepthContracts();
        AdvancedActorContracts();
        NativeRailroadContracts();
        IshikawaNaturalCauseContracts();
        TypedChordMarkupContracts();
        NativeStagePresentationAdapterContract();
        MultiRingCalloutAdapterContract();
        MixedScheduleAdapterContract();
        AlignedDifferenceAdapterContract();
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
            options.Localization.Language = "pl-PL";
            options.Localization[HtmlChartText.Pinned] = "Przypięte";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts);
            options.Interaction.AddScenario("package-series", "Selected series", scenario => scenario.AddSeriesStep(bubble.Id, "Bubble observation"));
            options.Interaction.WithActiveScenario("package-series");
        });
        PackageAssertions.Require(html.Contains("lang=\"pl-PL\"", StringComparison.Ordinal)
            && html.Contains("data-cfx-pinned-label=\"Przypięte\"", StringComparison.Ordinal),
            "Packed HTML controls lost their language or translated pinned status.");
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
        graph.Options.Enable(GraphSceneFeatures.RuntimePhysics | GraphSceneFeatures.Stabilization | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Manipulation | GraphSceneFeatures.History);
        graph.Options.Manipulation.EnableEditing();
        graph.Options.Physics.Solver = GraphPhysicsSolver.Repulsion;
        var graphHtml = graph.ToGraphExplorerHtmlPage();
        if (!graphHtml.Contains("data-cfx-graph-id=\"package-graph\"", StringComparison.Ordinal)) throw new InvalidOperationException("Graph explorer package surface missing.");
        if (!graphHtml.Contains("data-cfx-graph-physics=\"Repulsion\"", StringComparison.Ordinal)) throw new InvalidOperationException("Graph explorer physics profile missing.");
#if CFX_PACKAGE_BROWSER
        PackageBrowserProbe.RunAsync(html, graphHtml).GetAwaiter().GetResult();
#else
        Console.WriteLine("Static adapter smoke excludes browser execution; use Build/Test-PackageQualification.ps1 with HFX_TEST_ENABLE_PLAYWRIGHT=1 for browser qualification.");
#endif

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

    private static void MermaidSourceDiagnostics() {
        const string flow = "flowchart LR\nclassDef default fill:#dbeafe\nclassDef color,box color:#7c2d12\nA:::color --> B:::box";
        var rendered = MermaidRenderer.Render(flow);
        PackageAssertions.Require(!rendered.HasErrors && rendered.Diagnostics.Count == 0 && rendered.Artifact != null,
            "Packed flowchart classes no longer reach the native renderer.");
        var artifact = rendered.Artifact ?? throw new InvalidOperationException("Packed flowchart did not produce an artifact.");
        var topology = artifact.Model as TopologyChart
            ?? throw new InvalidOperationException("Packed flowchart did not produce the native topology model.");
        PackageAssertions.Require(topology.Nodes.All(node => node.BackgroundColor == "#dbeafe"),
            "Packed default Mermaid classes did not apply to every node.");
        PackageAssertions.Png(artifact.ToPng());
        var invalid = MermaidRenderer.Render("gitGraph\ncommit type: BAD");
        PackageAssertions.Require(invalid.HasErrors && invalid.Artifact == null
            && invalid.Diagnostics.All(item => item.Code == MermaidDiagnosticCodes.InvalidStatement),
            "Packed GitGraph accepted an invalid commit type without its stable error code.");
        const string journeySource = "journey\nsection Work\nTask: 4: User";
        var journey = MermaidRenderer.Render(journeySource, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        var fencedJourney = new MermaidVisualMarkupParser(new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject })
            .Parse("Intro\n\n```mermaid\n" + journeySource + "\n```");
        PackageAssertions.Require(!journey.HasErrors && journey.Diagnostics.Count == 0 && journey.Artifact?.Model is JourneyDiagramBlock nativeJourney
            && nativeJourney.Tasks.Count == 1 && nativeJourney.Tasks[0].Score == 4 && nativeJourney.Tasks[0].Actors.SequenceEqual(new[] { "User" })
            && nativeJourney.Sections.Single().Label == "Work" && nativeJourney.Tasks[0].SectionId == nativeJourney.Sections[0].Id
            && !fencedJourney.HasErrors && fencedJourney.Diagnostics.Count == 0 && fencedJourney.Artifacts.Count == 1,
            "Packed direct and Markdown Journey lost native score, actor or ordered section identity.");
        var journeyArtifact = journey.Artifact ?? throw new InvalidOperationException("Packed Journey did not produce an artifact.");
        var journeySvg = XDocument.Parse(journeyArtifact.ToSvg());
        PackageAssertions.Require(journeySvg.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "journey-task"
                && (string?)node.Attribute("data-score") == "4")
            && journeySvg.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "journey-actor-membership"
                && (string?)node.Attribute("data-actor") == "User"), "Packed Journey painted no scored task or actor membership.");
        PackageAssertions.Require(journeyArtifact.ToSvg() == fencedJourney.Artifacts[0].ToSvg()
            && journeyArtifact.ToPng().SequenceEqual(fencedJourney.Artifacts[0].ToPng()),
            "Packed Journey direct and Markdown exports disagree.");
        PackageAssertions.Png(journeyArtifact.ToPng());
    }

    private static void NativeViewportSection() {
        var observations = ChartSeries.CreateLine(new ChartSeriesId("native-values"), "Native values",
            new[] { new ChartPoint(0, -100), new ChartPoint(5, 4), new ChartPoint(10, 100) })
            .WithObservationId(1, new ChartObservationId("native-middle"));
        var chart = Chart.Create().WithSize(520, 320).WithLegend(false).WithXAxisBounds(0, 10).AddSeries(observations);
        var renderer = new HtmlInteractiveChartRenderer();
        var full = renderer.PrepareSection(chart, null);
        var projection = full.PreparedVisual.XProjection ?? throw new InvalidOperationException("Packed HTML section omitted its native X projection.");
        var requested = projection.ConstrainViewport(projection.Project(3), projection.Project(7));
        PackageAssertions.Require(requested is not null && Math.Abs(requested.Minimum - 3) < 1e-9 && Math.Abs(requested.Maximum - 7) < 1e-9,
            "Packed logical gesture did not produce the source X window.");
        var narrowed = renderer.PrepareSection(chart, requested == null ? null : new ChartCartesianViewport(x: requested));
        PackageAssertions.Require(narrowed.PreparedVisual.XProjection is ChartAxisProjection window && window.Minimum > 2.99 && window.Maximum < 7.01 &&
            narrowed.Html.Contains("data-cfx-observation-id=\"native-middle\"", StringComparison.Ordinal) &&
            !narrowed.Html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Packed native section lost its window, source identity or asset-free contract.");
        PackageAssertions.Require(!full.PreparedVisual.Regions.Where(region => region.Role == "axis-x-label").Select(region => region.Label)
            .SequenceEqual(narrowed.PreparedVisual.Regions.Where(region => region.Role == "axis-x-label").Select(region => region.Label)),
            "Packed viewport retained full-domain X tick captions.");
        PackageAssertions.Require(full.PreparedVisual.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label)
            .SequenceEqual(narrowed.PreparedVisual.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label)),
            "Packed viewport changed the full-data Y domain.");
        PackageAssertions.Png(narrowed.PreparedVisual.ToPng());
        PackageAssertions.Require(renderer.PrepareSection(chart, null).Html == full.Html && chart.Options.XAxis.Minimum == 0 && chart.Options.XAxis.Maximum == 10,
            "Packed native reset did not recover the full section or mutated authored bounds.");
    }
    private static void NativeCartesianAndDrawingSection() {
        var chart = Chart.Create().WithSize(800, 480).WithTitle("Packed native exploration")
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 100).WithLegend(false)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("measurements"), "Measurements",
                new[] { new ChartPoint(0, 10), new ChartPoint(5, 40), new ChartPoint(10, 90) })
                .WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("middle"), new ChartObservationId("last") }))
            .AddNote(new ChartPoint(5, 40), "Review", id: new ChartAnnotationId("review"));
        void Configure(HtmlChartInteractionOptions options) {
            options.Interaction.ChartId = "packed-depth";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.IncludeContextMenu = true; options.XNavigation.Enabled = true;
            options.XNavigation.RangePresets.Add(new HtmlChartXRangePreset("Latest six", 6));
        }
        var renderer = new HtmlInteractiveChartRenderer();
        var full = renderer.PrepareSection(chart, null, Configure);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 480), chart.Options.Padding),
            chart.Options.Theme, chart.Options.ThemeMode, new VisualFrame(chart.Title, chart.Subtitle, showLegend: false,
                legendPosition: chart.Options.LegendPosition, showSurface: chart.Options.ShowPlotBackground,
                transparentBackground: chart.Options.TransparentBackground, showCard: chart.Options.ShowCard));
        var nativeFull = chart.Prepare(context, null);
        PackageAssertions.Require(full.PreparedVisual.ToSvg() == nativeFull.ToSvg()
            && nativeFull.ToSvg() == ((IVisualRenderable)chart).Prepare(context).ToSvg(),
            "Packed HTML plain-null full preparation disagreed with Core or IVisualRenderable dispatch.");
        foreach (var viewport in new[] { new ChartCartesianViewport(x: new ChartAxisWindow(2, 8)), new ChartCartesianViewport(y: new ChartAxisWindow(20, 80)) }) {
            var section = renderer.PrepareSection(chart, viewport, Configure);
            PackageAssertions.Require(section.PreparedVisual.ToSvg() == chart.Prepare(context, viewport).ToSvg()
                && section.NarrowPreparedVisual!.XProjection!.Minimum == (viewport.X?.Minimum ?? 0)
                && section.NarrowPreparedVisual.XProjection.Maximum == (viewport.X?.Maximum ?? 10)
                && section.NarrowPreparedVisual.YProjection!.Minimum == (viewport.Y?.Minimum ?? 0)
                && section.NarrowPreparedVisual.YProjection.Maximum == (viewport.Y?.Maximum ?? 100),
                "Packed HTML X/Y Cartesian input changed the native scene or an omitted full domain.");
        }
        var requested = new ChartCartesianViewport(new ChartAxisWindow(2, 8), new ChartAxisWindow(20, 80));
        var window = renderer.PrepareSection(chart, requested, Configure);
        PackageAssertions.Require(window.PreparedVisual.YProjection?.Minimum == 20 && window.NarrowPreparedVisual?.YProjection?.Maximum == 80
            && window.PreparedVisual.XProjection?.Minimum == 2 && window.NarrowPreparedVisual?.XProjection?.Maximum == 8,
            "Packed adaptive HTML sections lost an explicit source axis window.");
        PackageAssertions.Require(window.Html.Contains("data-cfx-context-menu=\"true\"", StringComparison.Ordinal)
            && window.Html.Contains("data-cfx-x-navigation=\"true\"", StringComparison.Ordinal)
            && !window.Html.Contains("<script", StringComparison.OrdinalIgnoreCase),
            "Packed optional controls lost their section/asset boundary.");
        PackageAssertions.Require(window.PreparedVisual.ToSvg() == chart.Prepare(context, requested).ToSvg(),
            "Packed HTML XY Cartesian input disagreed with Core native preparation.");
        var windowSvg = window.PreparedVisual.ToSvg();
        var initialHtml = chart.ToInteractiveHtmlPage(Configure);
        var annotations = chart.Annotations;
        var review = annotations.Single();
        var anonymous = new ChartAnnotation(ChartAnnotationKind.HorizontalLine, 50, null, "Anonymous guide", review.Color, 1, showLabel: false);
        var changedAnonymous = new ChartAnnotation(ChartAnnotationKind.HorizontalLine, 55, null, "Changed guide", review.Color, 1, showLabel: false);
        chart.AddAnnotation(anonymous).ReplaceAnnotation(anonymous, changedAnonymous).RemoveAnnotation(changedAnonymous);
        PackageAssertions.Require(ReferenceEquals(annotations, chart.Annotations) && annotations.Count == 1 && ReferenceEquals(annotations[0], review),
            "Packed anonymous annotation operations detached the read-only view or changed a keyed neighbor.");
        var reviewed = ChartAnnotation.Note(new ChartPoint(6, 45), "Reviewed", review.Color, id: new ChartAnnotationId("review"));
        chart.ReplaceAnnotation(reviewed);
        PackageAssertions.Require(ReferenceEquals(annotations[0], reviewed), "Packed keyed replacement failed to update the retained annotation view.");
        var edited = renderer.PrepareSection(chart, requested, Configure);
        var reset = renderer.PrepareSection(chart, null, Configure);
        PackageAssertions.Require(full.PreparedVisual.YProjection?.Minimum == 0 && reset.PreparedVisual.YProjection?.Maximum == 100
            && chart.Options.XAxis.Minimum == 0 && chart.Options.YAxis.Maximum == 100,
            "Packed native exploration mutated authored bounds or lost the full reset domain.");
        chart.RemoveAnnotation(new ChartAnnotationId("review"));
        PackageAssertions.Require(annotations.Count == 0 && window.PreparedVisual.ToSvg() == windowSvg
            && PackageAssertions.Contains(edited.Html, "data-cfx-label=\"Reviewed\""),
            "Packed keyed removal changed an earlier native or HTML snapshot.");
        PackageAssertions.Png(edited.PreparedVisual.ToPng());
        Console.WriteLine("HTML authoring facts: canonical full/X/Y/XY scenes agree with Core; plain-null reset and keyed/anonymous annotation snapshots passed.");
#if CFX_PACKAGE_BROWSER
        PackageBrowserProbe.RunNativeChartAsync(initialHtml, window.Html, edited.Html, reset.Html).GetAwaiter().GetResult();
#endif
    }

    private static void RadarSourcePaint() {
        const string source = "%%{init: {'themeVariables': {'cScale0': '#2563EB', 'radar': {'curveStrokeWidth': 0, 'curveOpacity': 0, 'axisStrokeWidth': 3}}}}%%\nradar-beta\naxis A,B,C\ncurve c{20,40,30}\nmax 100";
        var parsed = new MermaidParser().ParseRadar(source);
        PackageAssertions.Require(!parsed.HasErrors && parsed.Diagnostics.Count == 0, "Packed radar style parser failed.");
        var chart = parsed.Document!.ToChart();
        PackageAssertions.Require(chart.Series[0].Color?.Equals(ChartColor.FromHex("#2563EB")) == true
            && chart.Series[0].Radar.OutlineStroke!.Width == 0 && chart.Options.PolarAxisStroke!.Width == 3,
            "Packed radar theme settings did not reach native owners.");
        var markup = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!markup.HasErrors && markup.Artifacts.Count == 1
            && markup.Artifacts[0].ToSvg() == parsed.Document!.ToVisualArtifact().ToSvg(), "Packed radar fence changed source paint.");
        PackageAssertions.Png(markup.Artifacts[0].ToPng());
    }
}
