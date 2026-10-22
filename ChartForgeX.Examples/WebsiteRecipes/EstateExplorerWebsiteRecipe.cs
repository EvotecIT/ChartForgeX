#region imports
using System.IO;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
#endregion

/// <summary>The executable source for the website's interactive estate explorer.</summary>
public static class EstateExplorerWebsiteRecipe {
    /// <summary>Creates the estate and its initial hierarchical navigation state.</summary>
    public static GraphScene Create() {
#region estate
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
            .AddEdge("global-europe", "global", "europe",
                configure: edge => edge.Directed = true)
            .AddEdge("global-americas", "global", "americas",
                configure: edge => edge.Directed = true)
            .AddEdge("global-apac", "global", "apac",
                configure: edge => edge.Directed = true)
            .AddEdge("europe-apac", "europe", "apac", "replication",
                edge => edge.Status = "warning");
#endregion
#region sites
        foreach (var (id, label, region) in new[] {
            ("warsaw", "Warsaw", "europe"), ("london", "London", "europe"),
            ("frankfurt", "Frankfurt", "europe"), ("new-york", "New York", "americas"),
            ("toronto", "Toronto", "americas"), ("sydney", "Sydney", "apac"),
            ("singapore", "Singapore", "apac")
        }) {
            graph.AddNode(id, label, node => node.ParentId = region)
                .AddEdge(region + "-" + id, region, id,
                    configure: edge => edge.Directed = true);
        }
#endregion
#region layout
        graph.Options.UseSuperTopologyDefaults();
        graph.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        graph.Options.Hierarchy.InitialRootNodeId = "global";
        graph.Options.Hierarchy.InitialDepth = 1;
#endregion
        return graph;
    }

    /// <summary>Writes the ordinary self-contained explorer page.</summary>
    public static void Save(GraphScene graph) {
#region save
        var html = graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = HtmlGraphRenderBackend.Svg;
            options.Theme = HtmlGraphExplorerTheme.System;
            options.IncludeThemeToggle = true;
        });

        File.WriteAllText("estate.html", html);
#endregion
    }
}
