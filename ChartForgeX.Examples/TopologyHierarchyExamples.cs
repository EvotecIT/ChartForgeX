using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

/// <summary>Regions, semantic health rings and labelled trees, using the same native scene in static and interactive output.</summary>
public static class TopologyHierarchyExamples {
    /// <summary>Writes neutral examples and one illustrative directory-shaped mapping at desktop and compact sizes.</summary>
    public static void Write(string output) {
        Directory.CreateDirectory(output);
        ExampleTypography.Register();
        foreach (var dark in new[] { false, true }) {
            var width = dark ? 390 : 1440;
            WriteChart(output, "service-regions", CreateTopology(width, dark), dark);
            WriteChart(output, "timing-hierarchy", CreateHierarchy(width, dark), dark);
            WriteChart(output, "directory-topology", CreateDirectoryExample(width, dark), dark);
        }
    }

    /// <summary>Creates a neutral service topology with automatically packed regions and typed relationships.</summary>
    public static TopologyChart CreateTopology(int width, bool dark) {
        var compact = width < 640;
        var chart = Frame("service-regions", "Service topology", "Four services · two regions · one relationship needs attention", width, dark)
            .WithViewport(width - 48, compact ? 820 : 560, 24)
            .WithLayout(compact ? TopologyLayoutMode.GroupGrid : TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("north", "North region", TopologyHealthStatus.Healthy, "Primary processing · two services")
            .AddAutoGroup("south", "South region", TopologyHealthStatus.Warning, "Recovery processing · two services")
            .AddAutoNode("gateway", "Gateway", status: TopologyHealthStatus.Healthy, groupId: "north", subtitle: "Primary", symbol: "G")
            .AddAutoNode("worker", "Worker", status: TopologyHealthStatus.Healthy, groupId: "north", subtitle: "Consumer", symbol: "W")
            .AddAutoNode("replica", "Replica", status: TopologyHealthStatus.Warning, groupId: "south", subtitle: "Lag 42 s", symbol: "R")
            .AddAutoNode("archive", "Archive", status: TopologyHealthStatus.Healthy, groupId: "south", subtitle: "Daily", symbol: "A")
            .WithNodeBadge("gateway", "LEAD")
            .AddEdge("process", "gateway", "worker", "Requests", status: TopologyHealthStatus.Healthy, direction: VisualLinkDirection.Forward)
            .AddEdge("mirror", "worker", "replica", "42 s lag", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, VisualLinkDirection.Bidirectional)
            .AddEdge("checkpoint", "replica", "archive", "Checkpoint", TopologyEdgeKind.DataFlow, TopologyHealthStatus.Healthy, VisualLinkDirection.Forward)
            .WithEdgeLineStyle("mirror", TopologyEdgeLineStyle.Solid)
            .WithEdgeLineStyle("checkpoint", TopologyEdgeLineStyle.Dashed);
        chart.Edges.Single(edge => edge.Id == "mirror").StrokeWidth = 2.5;
        if (compact) {
            chart.WithEdgePorts("process", TopologyEdgePort.Right, TopologyEdgePort.Left)
                .WithEdgePorts("checkpoint", TopologyEdgePort.Right, TopologyEdgePort.Left);
            chart.Edges.Single(edge => edge.Id == "process").Label = "Flow";
            chart.Edges.Single(edge => edge.Id == "checkpoint").Label = "Save";
        }
        foreach (var node in chart.Nodes) {
            node.DisplayMode = TopologyNodeDisplayMode.Tile;
            node.Shape = TopologyNodeShape.Circle;
            node.Width = node.Height = 64;
            node.PreserveDisplayModeSize = true;
            node.ShowStatusBadge = false;
            node.ShowHealthRing = true;
            node.Color = chart.Theme.Resolve(chart.ThemeMode).MutedForeground.ToCss();
        }
        return chart;
    }

    /// <summary>Creates a timing tree whose incoming edge labels are ordinary caller-supplied strings.</summary>
    public static TopologyChart CreateHierarchy(int width, bool dark) =>
        Frame("timing-hierarchy", "Timing hierarchy", "Reference → coordinator → regional clocks · illustrative offsets", width, dark)
            .WithLegend(TopologyLegend.Create("Health")
                .AddStatus("Healthy", TopologyHealthStatus.Healthy)
                .AddStatus("Needs attention", TopologyHealthStatus.Warning))
            .WithViewport(width - 48, width < 640 ? 820 : 580, 24)
            .AddHierarchy(new[] {
                new TopologyHierarchyItem("reference", "Reference clock") { Subtitle = "Independent source", Badge = "REF", Status = TopologyHealthStatus.Healthy },
                new TopologyHierarchyItem("coordinator", "Coordinator", "reference") { Subtitle = "Level 2 · last sync 15 s", ParentEdgeLabel = "+0.2 ms", Status = TopologyHealthStatus.Healthy },
                new TopologyHierarchyItem("north-clock", "North clock", "coordinator") { Subtitle = "Level 3 · last sync 21 s", ParentEdgeLabel = "+0.7 ms", Status = TopologyHealthStatus.Healthy },
                new TopologyHierarchyItem("south-clock", "South clock", "coordinator") { Subtitle = "Level 3 · last sync 8 min", ParentEdgeLabel = "+84 ms", ParentEdgeStatus = TopologyHealthStatus.Warning, Status = TopologyHealthStatus.Warning }
            }, new TopologyHierarchyOptions { NodeWidth = width < 640 ? 132 : 200, NodeHeight = 80,
                EdgeStatus = TopologyHealthStatus.Healthy, EdgeKind = TopologyEdgeKind.Dependency });

    private static TopologyChart Frame(string id, string title, string subtitle, int width, bool dark) =>
        TopologyChart.Create().WithId(id).WithTitle(title).WithSubtitle(subtitle)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(ExampleTypography.FontFamily), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithLegend(TopologyLegend.Create("Health and relationships")
                .AddStatus("Healthy", TopologyHealthStatus.Healthy)
                .AddStatus("Needs attention", TopologyHealthStatus.Warning)
                .AddEdgeKind("Scheduled", TopologyEdgeKind.DataFlow, lineStyle: TopologyEdgeLineStyle.Dashed));

    // All directory vocabulary and mapping stays in the example, outside the rendering library.
    private static TopologyChart CreateDirectoryExample(int width, bool dark) {
        var chart = CreateTopology(width, dark).WithId("directory-topology")
            .WithTitle("Directory topology — illustrative")
            .WithSubtitle("Example mapping only · synthetic sites and controllers · no directory access");
        chart.Groups[0].Label = "HQ site";
        chart.Groups[0].Subtitle = "10.20.0.0/24 · two controllers";
        chart.Groups[1].Label = "Branch site";
        chart.Groups[1].Subtitle = "10.30.0.0/24 · two controllers";
        var labels = new[] { "DC-01", "DC-02", "DC-03", "DC-04" };
        for (var index = 0; index < chart.Nodes.Count; index++) {
            chart.Nodes[index].Label = labels[index];
            chart.Nodes[index].Subtitle = index == 0 ? "PDC · GC" : "GC";
            chart.Nodes[index].Symbol = "DC";
        }
        chart.Edges[0].Label = width < 640 ? "Sync" : "Replication";
        chart.Edges[1].Label = "Last success 42 s";
        chart.Nodes[0].Badge = "PDC";
        return chart;
    }

    private static void WriteChart(string output, string name, TopologyChart chart, bool dark) {
        var options = new TopologyRenderOptions {
            IncludeTileSubtitles = true, IncludeIconLabels = true, WrapNodeLabels = true,
            IncludeEdgeLabelLeaders = true,
            ReadableDenseLayout = true, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card,
            EnableHtmlViewportControls = true, EnableHtmlExportControls = true,
            HtmlNarrowMinimumHeight = dark ? 620 : 0
        };
        var stem = name + (dark ? "-390-dark" : "-1440-light");
        var prepared = chart.Prepare(options);
        ExampleArtifactWriter.WriteText(Path.Combine(output, stem + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, stem + ".png"), prepared.ToPng());
        var html = new HtmlInteractiveTopologyRenderer().RenderPage(chart, options);
        ExampleArtifactWriter.WriteText(Path.Combine(output, stem + ".html"), html.Replace("</head>", ExampleTypography.EmbeddedStyle + "</head>", StringComparison.Ordinal));
    }
}
