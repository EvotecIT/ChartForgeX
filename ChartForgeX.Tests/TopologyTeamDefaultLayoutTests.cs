using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyTeamDefaultLayoutTests {
    [Fact]
    public void DefaultOrganizationKeepsManagersInOneBandAndReportsUnderTheirManager() {
        var chart = TopologyChart.Create().WithViewport(960, 520).WithLegend(null).AddTeam("team", "Team", new[] {
            new TopologyTeamMember("director", "Director"),
            new TopologyTeamMember("a", "Engineering") { ParentId = "director" },
            new TopologyTeamMember("b", "Operations") { ParentId = "director" },
            new TopologyTeamMember("c", "Success") { ParentId = "director" },
            new TopologyTeamMember("a1", "Platform") { ParentId = "a" },
            new TopologyTeamMember("b1", "Service") { ParentId = "b" },
            new TopologyTeamMember("b2", "Support") { ParentId = "b" },
            new TopologyTeamMember("c1", "Onboarding") { ParentId = "c" },
            new TopologyTeamMember("c2", "Training") { ParentId = "c" }
        }, new TopologyTeamOptions { IncludeTeamNode = false });
        var layout = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = true });
        var managers = layout.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) && parent == "director").OrderBy(node => node.X).ToArray();
        Assert.Equal(3, managers.Length);
        Assert.Single(managers.Select(node => node.Y).Distinct());
        Assert.Single(layout.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent)
            && managers.Any(manager => manager.Id == parent)).Select(node => node.Y).Distinct());
        for (var index = 0; index < managers.Length; index++) {
            var manager = managers[index]; var center = manager.X + manager.Width / 2;
            var children = layout.Nodes.Where(node => node.Metadata.TryGetValue("hierarchy.parentId", out var parent) && parent == manager.Id).ToArray();
            Assert.NotEmpty(children);
            Assert.All(children, child => {
                Assert.True(child.Y > manager.Y);
                if (index > 0) Assert.True(child.X + child.Width / 2 > (center + managers[index - 1].X + managers[index - 1].Width / 2) / 2);
                if (index < managers.Length - 1) Assert.True(child.X + child.Width / 2 < (center + managers[index + 1].X + managers[index + 1].Width / 2) / 2);
            });
        }
    }
}
