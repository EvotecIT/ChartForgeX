#region imports
using System;
using System.Linq;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
#endregion

/// <summary>The executable source for the website's mixed-policy organization hierarchy.</summary>
public static class PlatformTeamWebsiteRecipe {
    /// <summary>Creates the maintained team with its standard, compact and vertical branches.</summary>
    public static TopologyChart Create(VisualThemeMode mode = VisualThemeMode.Light) {
#region people
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
#endregion
#region chart
        var chart = TopologyChart.Create()
            .WithThemeMode(mode)
            .WithTitle("Platform team")
            .WithSubtitle("Standard, compact and vertical branches")
            .WithViewport(1180, 620, 28)
            .AddTeam("platform", "Platform team", members,
                new TopologyTeamOptions { MaxLevel = 3 });
#endregion
        return chart;
    }

    /// <summary>Writes the ordinary standalone example outputs.</summary>
    public static void Save(TopologyChart chart) {
#region save
        chart.SaveSvg("platform-team.svg");
        chart.SavePng("platform-team.png");
#endregion
    }

#region helpers
    static TopologyTeamMember Reports(string id, string name, string role, string manager,
        TopologyHealthStatus status = TopologyHealthStatus.Unknown) =>
        new(id, name, role) { ParentId = manager, Status = status };
#endregion
}
