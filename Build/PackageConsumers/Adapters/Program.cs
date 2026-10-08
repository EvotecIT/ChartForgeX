using System;
using ChartForgeX.Markup;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;


internal static class Program {
    private static void Main() {
        PackageAssertions.CoreFixtures();
        var chart = Chart.Create()
            .WithTitle("Package smoke")
            .WithSize(320, 180)
            .AddLine("Values", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });

        if (!chart.ToSvg().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("SVG render failed.");
        if (!chart.ToHtmlFragment().Contains("<svg", StringComparison.Ordinal)) throw new InvalidOperationException("HTML render failed.");
        if (chart.ToPng().Length <= 64) throw new InvalidOperationException("PNG render failed.");
        var html = chart.ToInteractiveHtmlPage(options => options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.Export | ChartInteractionFeatures.SynchronizedCharts));
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

        var mermaid = new MermaidParser().ParseFlowchart("flowchart LR\n  a[Start] --> b[Done]");
        if (mermaid.HasErrors || mermaid.Document is null) throw new InvalidOperationException("Mermaid package parser failed.");
        if (mermaid.Document.Nodes.Count != 2 || mermaid.Document.Edges.Count != 1) throw new InvalidOperationException("Mermaid package parser model missing nodes or edges.");
        var mermaidArtifact = mermaid.Document.ToVisualArtifact(new MermaidFlowchartRenderOptions { Id = "package-mermaid" });
        if (mermaidArtifact.Kind != VisualArtifactKind.Mermaid || !mermaidArtifact.SupportsExport(VisualArtifactExportFormat.Svg)) throw new InvalidOperationException("Mermaid package artifact contract missing.");
        var mermaidClass = new MermaidParser().ParseClass("classDiagram\nclass User\nUser <|-- Admin");
        if (mermaidClass.HasErrors || mermaidClass.Document is null || mermaidClass.Document.Classes.Count != 2) throw new InvalidOperationException("Mermaid package class parser failed.");
        if (mermaidClass.Document.ToVisualArtifact().Model is not ChartForgeX.Topology.TopologyChart) throw new InvalidOperationException("Mermaid package class artifact contract missing.");
        var markupMermaid = new MermaidVisualMarkupParser().Parse("~~~mermaid {#package-flow}\nflowchart LR\n  a --> b\n~~~");
        if (markupMermaid.HasErrors || markupMermaid.Artifacts.Count != 1 || markupMermaid.Artifacts[0].Id != "package-flow") throw new InvalidOperationException("Mermaid markup package parser failed.");
        var useCase = MermaidRenderer.Render("usecase-beta\nactor User\nUser --> Action(Do work)", new MermaidRenderOptions());
        if (useCase.HasErrors || useCase.Artifact is null) throw new InvalidOperationException("Mermaid source package renderer failed.");
        if (!useCase.Artifact.ToSvg().Contains("data-node-shape=\"Actor\"", StringComparison.Ordinal) || useCase.Artifact.ToPng().Length <= 64) throw new InvalidOperationException("Mermaid source package SVG/PNG output failed.");
        var useCaseJson = useCase.Artifact.ToInterchangeJson();
        if (VisualArtifactInterchangeEnvelope.FromJson(useCaseJson).ToJson() != useCaseJson) throw new InvalidOperationException("Mermaid source package interchange failed.");

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
}
