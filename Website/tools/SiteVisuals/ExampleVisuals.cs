// Outputs of the curated examples under content/examples. Each builder mirrors the code on its example page; the only
// addition is WithThemeMode, so the site can show the same output in its light and dark themes.
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Data;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace SiteVisuals;

internal static class ExampleVisuals {
    public static Dictionary<string, object> Write(VisualOutput output) {
        var examples = new SortedDictionary<string, object>(StringComparer.Ordinal);

        examples["static-chart"] = Save(output, "static-chart",
            mode => StaticChart(mode).ToSvg("cfx-ex-chart"),
            mode => StaticChart(mode).ToPng(),
            mode => StaticChart(mode).ToHtmlPage());

        examples["report-grid"] = Save(output, "report-grid",
            mode => ReportGrid(mode).ToSvg("cfx-ex-grid"),
            mode => ReportGrid(mode).ToPng(),
            mode => ReportGrid(mode).ToHtmlPage());

        examples["organization-hierarchy"] = Save(output, "organization-hierarchy",
            mode => TeamHierarchy(mode).ToSvg(),
            mode => TeamHierarchy(mode).ToPng(),
            mode => TeamHierarchy(mode).ToHtmlPage());

        // The interactive example's output is the explorer page itself; the static SVG is its card image.
        var estate = output.Pair("examples/interactive-topology", (_, dark) => Estate().ToGraphSvg(configure: options => {
            options.Width = 1180;
            options.Height = 640;
            options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
        }));
        output.Text("examples/interactive-topology.html", Estate().ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = HtmlGraphRenderBackend.Svg;
            options.Theme = HtmlGraphExplorerTheme.System;
            options.IncludeThemeToggle = true;
        }));
        estate["interactive"] = true;
        examples["interactive-topology"] = estate;
        return new Dictionary<string, object>(examples, StringComparer.Ordinal);
    }

    private static Dictionary<string, object> Save(VisualOutput output, string slug,
        Func<VisualThemeMode, string> svg, Func<VisualThemeMode, byte[]> png, Func<VisualThemeMode, string> html) {
        var entry = output.Pair("examples/" + slug, (mode, _) => svg(mode));
        long pngBytes = 0;
        foreach (var (name, mode, _) in output.Modes()) {
            var bytes = png(mode);
            pngBytes = Math.Max(pngBytes, bytes.Length);
            output.Binary($"examples/{slug}-{name}.png", bytes);
            output.Text($"examples/{slug}-{name}.html", html(mode));
        }

        entry["pngKb"] = Math.Round(pngBytes / 1024d);
        entry["interactive"] = false;
        return entry;
    }

    private static IEnumerable<ChartPoint> Points(params double[] values) {
        for (var index = 0; index < values.Length; index++) {
            yield return new ChartPoint(index + 1, values[index]);
        }
    }

    // content/examples/static-chart.md
    public static Chart StaticChart(VisualThemeMode mode) => Chart.Create()
        .WithThemeMode(mode)
        .WithTitle("Deployment health")
        .WithXAxis("Run")
        .WithYAxis("Checks")
        .WithSize(1180, 640)
        .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
        .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120))
        .AddSmoothLine("Failed", Points(22, 30, 28, 21, 18), ChartColor.FromRgb(248, 113, 113));

    // content/examples/report-grid.md
    public static ChartGrid ReportGrid(VisualThemeMode mode) {
        var samples = ChartDataset<CpuSample>.From(new CpuSample[] {
            new("Warsaw", 1, 35), new("Warsaw", 2, 42),
            new("Warsaw", 3, 39), new("Warsaw", 4, 51),
            new("London", 1, 48), new("London", 2, 61),
            new("London", 3, 57), new("London", 4, 66),
            new("Berlin", 1, 22), new("Berlin", 2, 28),
            new("Berlin", 3, 31), new("Berlin", 4, 36),
            new("Madrid", 1, 51), new("Madrid", 2, 47),
            new("Madrid", 3, 44), new("Madrid", 4, 39)
        });

        return ChartGrid.FromFacets(
            samples,
            sample => sample.Site,
            (site, rows) => Chart.Create()
                .WithThemeMode(mode)
                .WithTitle(site)
                .WithYAxis("CPU (%)")
                .AddLine("CPU", rows, sample => sample.Minute, sample => sample.Cpu),
            columns: 2);
    }

    // content/examples/organization-hierarchy.md
    public static TopologyChart TeamHierarchy(VisualThemeMode mode) {
        var members = new[] {
            new TopologyTeamMember("marta", "Marta", "Head of platform") {
                LayoutPolicy = TopologyHierarchyLayoutPolicy.Standard
            },
            new TopologyTeamMember("jonas", "Jonas", "Sales lead") {
                ParentId = "marta",
                LayoutPolicy = TopologyHierarchyLayoutPolicy.Vertical
            },
            new TopologyTeamMember("deniz", "Deniz", "Design lead") {
                ParentId = "marta",
                LayoutPolicy = TopologyHierarchyLayoutPolicy.Compact
            },
            Reports("ana", "Ana", "Product designer", "deniz"),
            Reports("pawel", "Pawel", "Illustrator", "deniz", TopologyHealthStatus.Warning),
            Reports("ruben", "Ruben", "Brand designer", "deniz"),
            Reports("emre", "Emre", "Product designer", "deniz"),
            Reports("vipul", "Vipul", "Account manager", "jonas"),
            Reports("karan", "Karan", "Account manager", "jonas"),
            Reports("lena", "Lena", "Sales engineer", "jonas")
        };

        return TopologyChart.Create()
            .WithThemeMode(mode)
            .WithTitle("Platform team")
            .WithSubtitle("Standard, compact and vertical branches")
            .WithViewport(1180, 620, 28)
            .AddTeam("platform", "Platform team", members,
                new TopologyTeamOptions { MaxLevel = 3 });

        static TopologyTeamMember Reports(string id, string name, string role, string manager,
            TopologyHealthStatus status = TopologyHealthStatus.Unknown) =>
            new(id, name, role) { ParentId = manager, Status = status };
    }
    // content/examples/interactive-topology.md
    public static GraphScene Estate() {
        var graph = GraphScene.Create("estate", "Global estate")
            .AddNode("global", "Global", node => node.BadgeText = "42")
            .AddNode("europe", "Europe", node => {
                node.ParentId = "global";
                node.SecondaryLabel = "4 sites · 18 workloads";
                node.Status = "warning";
            })
            .AddNode("americas", "Americas", node => {
                node.ParentId = "global";
                node.SecondaryLabel = "3 sites · 14 workloads";
                node.Status = "healthy";
            })
            .AddNode("apac", "Asia Pacific", node => {
                node.ParentId = "global";
                node.SecondaryLabel = "2 sites · 10 workloads";
                node.Status = "healthy";
            })
            .AddEdge("global-europe", "global", "europe", configure: edge => edge.Directed = true)
            .AddEdge("global-americas", "global", "americas", configure: edge => edge.Directed = true)
            .AddEdge("global-apac", "global", "apac", configure: edge => edge.Directed = true)
            .AddEdge("europe-apac", "europe", "apac", "replication", edge => edge.Status = "warning");

        foreach (var (id, label, region) in new[] {
            ("warsaw", "Warsaw", "europe"), ("london", "London", "europe"), ("frankfurt", "Frankfurt", "europe"),
            ("new-york", "New York", "americas"), ("toronto", "Toronto", "americas"),
            ("sydney", "Sydney", "apac"), ("singapore", "Singapore", "apac")
        }) {
            graph.AddNode(id, label, node => node.ParentId = region)
                .AddEdge(region + "-" + id, region, id, configure: edge => edge.Directed = true);
        }

        graph.Options.UseSuperTopologyDefaults();
        graph.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        graph.Options.Hierarchy.InitialRootNodeId = "global";
        graph.Options.Hierarchy.InitialDepth = 1;
        return graph;
    }

    public sealed record CpuSample(string Site, double Minute, double Cpu);
}
