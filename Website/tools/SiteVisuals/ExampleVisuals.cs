// Outputs of the curated examples under content/examples. Each builder mirrors the code on its example page; the only
// addition is WithDesignTokens, so the site can show the same output in its light and dark themes.
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
            tokens => StaticChart(tokens).ToSvg("cfx-ex-chart"),
            tokens => StaticChart(tokens).ToPng(),
            tokens => StaticChart(tokens).ToHtmlPage());

        examples["report-grid"] = Save(output, "report-grid",
            tokens => ReportGrid(tokens).ToSvg("cfx-ex-grid"),
            tokens => ReportGrid(tokens).ToPng(),
            tokens => ReportGrid(tokens).ToHtmlPage());

        examples["organization-hierarchy"] = Save(output, "organization-hierarchy",
            tokens => TeamHierarchy(tokens).ToSvg(),
            tokens => TeamHierarchy(tokens).ToPng(),
            tokens => TeamHierarchy(tokens).ToHtmlPage());

        // The interactive example's output is the explorer page itself; the static SVG is its card image.
        var estate = output.Pair("examples/interactive-topology", (_, _) => Estate().ToGraphSvg(configure: options => {
            options.Width = 1180;
            options.Height = 640;
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
        Func<VisualDesignTokens, string> svg, Func<VisualDesignTokens, byte[]> png, Func<VisualDesignTokens, string> html) {
        var entry = output.Pair("examples/" + slug, (tokens, _) => svg(tokens));
        long pngBytes = 0;
        foreach (var (mode, tokens, _) in output.Modes()) {
            var bytes = png(tokens);
            pngBytes = Math.Max(pngBytes, bytes.Length);
            output.Binary($"examples/{slug}-{mode}.png", bytes);
            output.Text($"examples/{slug}-{mode}.html", html(tokens));
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
    public static Chart StaticChart(VisualDesignTokens tokens) => Chart.Create()
        .WithDesignTokens(tokens)
        .WithTitle("Deployment health")
        .WithXAxis("Run")
        .WithYAxis("Checks")
        .WithSize(1180, 640)
        .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
        .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120))
        .AddSmoothLine("Failed", Points(22, 30, 28, 21, 18), ChartColor.FromRgb(248, 113, 113));

    // content/examples/report-grid.md
    public static ChartGrid ReportGrid(VisualDesignTokens tokens) {
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
                .WithDesignTokens(tokens)
                .WithTitle(site)
                .WithYAxis("CPU (%)")
                .AddLine("CPU", rows, sample => sample.Minute, sample => sample.Cpu),
            columns: 2);
    }

    // content/examples/organization-hierarchy.md
    public static TopologyChart TeamHierarchy(VisualDesignTokens tokens) => TopologyChart.Create()
        .WithDesignTokens(tokens)
        .WithTitle("Platform team")
        .WithSubtitle("Standard, compact and vertical branches in one hierarchy")
        .WithViewport(1180, 620, 28)
        .AddTeam("platform", "Platform team", new[] {
            new TopologyTeamMember("marta", "Marta", "Head of platform") { LayoutPolicy = TopologyHierarchyLayoutPolicy.Standard },
            new TopologyTeamMember("jonas", "Jonas", "Sales lead") { ParentId = "marta", LayoutPolicy = TopologyHierarchyLayoutPolicy.Vertical },
            new TopologyTeamMember("deniz", "Deniz", "Design lead") { ParentId = "marta", LayoutPolicy = TopologyHierarchyLayoutPolicy.Compact },
            new TopologyTeamMember("ana", "Ana", "Product designer") { ParentId = "deniz" },
            new TopologyTeamMember("pawel", "Pawel", "Illustrator") { ParentId = "deniz", Status = TopologyHealthStatus.Warning },
            new TopologyTeamMember("ruben", "Ruben", "Brand designer") { ParentId = "deniz" },
            new TopologyTeamMember("emre", "Emre", "Product designer") { ParentId = "deniz" },
            new TopologyTeamMember("vipul", "Vipul", "Account manager") { ParentId = "jonas" },
            new TopologyTeamMember("karan", "Karan", "Account manager") { ParentId = "jonas" },
            new TopologyTeamMember("lena", "Lena", "Sales engineer") { ParentId = "jonas" }
        }, new TopologyTeamOptions { MaxLevel = 3 });

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
            .AddEdge("europe-americas", "europe", "americas", "replication", edge => edge.Status = "warning");

        foreach (var (id, label, region) in new[] {
            ("warsaw", "Warsaw", "europe"), ("london", "London", "europe"), ("frankfurt", "Frankfurt", "europe"),
            ("new-york", "New York", "americas"), ("toronto", "Toronto", "americas"),
            ("sydney", "Sydney", "apac"), ("singapore", "Singapore", "apac")
        }) {
            graph.AddNode(id, label, node => node.ParentId = region)
                .AddEdge(region + "-" + id, region, id);
        }

        graph.Options.UseSuperTopologyDefaults();
        graph.Options.Hierarchy.InitialRootNodeId = "global";
        graph.Options.Hierarchy.InitialDepth = 1;
        return graph;
    }

    public sealed record CpuSample(string Site, double Minute, double Cpu);
}
