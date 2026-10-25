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
        foreach (var (width, dark) in new[] { (1440, false), (1440, true), (390, true) }) {
            WriteChart(output, "service-regions", CreateTopology(width, dark), dark);
            WriteChart(output, "directory-topology", CreateDirectoryExample(width, dark), dark);
            if (width == 390 || !dark) WriteChart(output, "timing-hierarchy", CreateHierarchy(width, dark), dark);
        }
    }

    /// <summary>Creates a neutral service topology with automatically packed regions and typed relationships.</summary>
    public static TopologyChart CreateTopology(int width, bool dark) {
        TopologyRegionTypography.Register();
        var chart = Frame("service-regions", "Service topology", "Two regions · five services · six links", width, dark)
            .WithTheme(RegionTheme(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithViewport(width - 48, 500, 20).WithLayout(TopologyLayoutMode.Regions)
            .AddAutoGroup("north", "North region", TopologyHealthStatus.Healthy, "Primary processing · zone A")
            .AddAutoGroup("south", "South region", TopologyHealthStatus.Warning, "Recovery processing · zone B")
            .AddAutoNode("gateway", "GATE", status: TopologyHealthStatus.Healthy, groupId: "north", subtitle: "Primary gateway")
            .AddAutoNode("worker", "WORK", status: TopologyHealthStatus.Warning, groupId: "north", subtitle: "Queue growing")
            .AddAutoNode("replica", "REPL", status: TopologyHealthStatus.Healthy, groupId: "south", subtitle: "Recovery leader")
            .AddAutoNode("archive", "ARCH", status: TopologyHealthStatus.Critical, groupId: "south", subtitle: "Archive unavailable")
            .AddAutoNode("cache", "MEM", status: TopologyHealthStatus.Healthy, groupId: "south", subtitle: "Read cache")
            .AddEdge("process", "gateway", "worker", status: TopologyHealthStatus.Healthy)
            .AddEdge("mirror", "gateway", "replica", "Scheduled · 47 min", status: TopologyHealthStatus.Warning)
            .AddEdge("recovery", "worker", "replica", "4 min", status: TopologyHealthStatus.Healthy)
            .AddEdge("checkpoint", "replica", "archive", "Failing 6 h", status: TopologyHealthStatus.Critical)
            .AddEdge("cache-feed", "replica", "cache", status: TopologyHealthStatus.Healthy)
            .AddEdge("overlay", "gateway", "replica", "Shared control plane", direction: VisualLinkDirection.Bidirectional)
            .WithEdgeLineStyle("mirror", TopologyEdgeLineStyle.Dashed)
            .WithLegend(TopologyLegend.Create().AddStatus("Current < 15 min", TopologyHealthStatus.Healthy)
                .AddStatus("Scheduled, 15–60 min", TopologyHealthStatus.Warning).AddStatus("Failing", TopologyHealthStatus.Critical));
        chart.Nodes.Single(node => node.Id == "gateway").Weight = TopologyNodeWeight.Primary;
        chart.Groups[1].StatusText = "Attention";
        chart.Nodes.Single(node => node.Id == "replica").Weight = TopologyNodeWeight.Primary;
        chart.Nodes.Single(node => node.Id == "gateway").AddBadge(new TopologyNodeBadge("crown", "Primary role"));
        chart.Nodes.Single(node => node.Id == "cache").Weight = TopologyNodeWeight.Minor;
        chart.Edges.Single(edge => edge.Id == "overlay").IsOverlay = true;
        chart.Edges.Single(edge => edge.Id == "mirror").Metadata["schedule"] = "Every 15 minutes";
        foreach (var node in chart.Nodes) node.ShowHealthRing = true;
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
        chart.Groups[1].Subtitle = "10.30.0.0/24 · three controllers";
        chart.Groups[1].StatusText = null;
        var labels = new[] { "AD0", "AD1", "AD2", "RODC", "PL1" };
        for (var index = 0; index < chart.Nodes.Count; index++) {
            chart.Nodes[index].Label = labels[index];
            chart.Nodes[index].Subtitle = index == 0 ? "5 roles" : index == 1 ? "site SRV missing" : index == 2 ? "bridgehead" : index == 3 ? "read only" : "global catalog";
            chart.Nodes[index].Symbol = "DC";
        }
        chart.Edges[1].Label = "Inter-site · 47 min";
        chart.Edges[5].Label = "example.net ⇄ example.org · parent–child";
        return chart;
    }

    private static void WriteChart(string output, string name, TopologyChart chart, bool dark) {
        var options = new TopologyRenderOptions {
            IncludeTileSubtitles = true, IncludeIconLabels = true, WrapNodeLabels = true,
            IncludeEdgeLabelLeaders = true,
            ReadableDenseLayout = true, NodeSurfaceStyle = TopologyNodeSurfaceStyle.Card,
            EnableHtmlViewportControls = true, EnableHtmlExportControls = true,
            IncludeRegionDetails = true, HtmlNarrowMinimumHeight = 0
        };
        var stem = name + "-" + (int)(chart.Viewport.Width + 48) + (dark ? "-dark" : "-light");
        var prepared = chart.Prepare(options);
        ExampleArtifactWriter.WriteText(Path.Combine(output, stem + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, stem + ".png"), prepared.ToPng());
        var html = new HtmlInteractiveTopologyRenderer().RenderPage(chart, options);
        var typography = chart.LayoutMode == TopologyLayoutMode.Regions ? TopologyRegionTypography.EmbeddedStyle : ExampleTypography.EmbeddedStyle;
        ExampleArtifactWriter.WriteText(Path.Combine(output, stem + ".html"), html.Replace("</head>", typography + "</head>", StringComparison.Ordinal));
        if (chart.LayoutMode == TopologyLayoutMode.Regions && !dark) {
            options.RegionPrint = true;
            ExampleArtifactWriter.WriteText(Path.Combine(output, name + "-print.svg"), chart.ToSvg(options));
            File.WriteAllBytes(Path.Combine(output, name + "-print.png"), chart.ToPng(options));
            var printPage = new TopologyHtmlRenderer().RenderPage(chart, options);
            ExampleArtifactWriter.WriteText(Path.Combine(output, name + "-print.html"), printPage.Replace("</head>", typography + "</head>", StringComparison.Ordinal));
        }
    }

    private static VisualTheme RegionTheme() => VisualTheme.Graphite().WithFontFamily(TopologyRegionTypography.Family).Configure(builder => {
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var c = builder.Colors(mode); var dark = mode == VisualThemeMode.Dark;
            ChartColor Color(string light, string night) => ChartColor.FromHex(dark ? night : light);
            c.Background = Color("#f6f8f9", "#0f161b"); c.Surface = Color("#eef2f4", "#1b2730"); c.ElevatedSurface = Color("#ffffff", "#151f26");
            c.Border = Color("#d6dde1", "#2a3943"); c.Foreground = Color("#14202a", "#e3eaef"); c.MutedForeground = Color("#5b6b76", "#93a4af");
            c.Accent = Color("#0f7c76", "#4fc7bd"); c.Info = Color("#2f6fb5", "#6aa8ef");
            c.Status.Pass = new VisualTokenColor(Color("#1f8a4c", "#4cc38a"), Color("#1f8a4c", "#4cc38a"));
            c.Status.Medium = new VisualTokenColor(Color("#b7791f", "#e2b34c"), Color("#b7791f", "#e2b34c"));
            c.Status.Critical = new VisualTokenColor(Color("#c2413a", "#f0716a"), Color("#c2413a", "#f0716a"));
            c.Warning = c.Status.Medium.Fill;
        }
    });
}
