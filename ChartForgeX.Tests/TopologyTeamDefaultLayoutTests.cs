using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyTeamDefaultLayoutTests {
    [Theory]
    [InlineData(TopologyCaptionPlacement.Top)]
    [InlineData(TopologyCaptionPlacement.Bottom)]
    public void HorizontalTeamReservesUnequalExternalCaptionFootprints(TopologyCaptionPlacement placement) {
        var chart = TopologyChart.Create().WithViewport(960, 800).WithLegend(null).AddTeam("team", "Team", new[] {
            new TopologyTeamMember("manager", "Manager"),
            new TopologyTeamMember("first", placement == TopologyCaptionPlacement.Top ? "Local" : "Regional\nsupport\nreporting") { ParentId = "manager" },
            new TopologyTeamMember("second", placement == TopologyCaptionPlacement.Top ? "Regional\nsupport\nreporting" : "Local") { ParentId = "manager" }
        }, new TopologyTeamOptions { IncludeTeamNode = false, NodeDisplayMode = TopologyNodeDisplayMode.Icon, LayoutDirection = TopologyLayoutDirection.LeftToRight });
        foreach (var node in chart.Nodes.Where(node => node.Id != "manager")) {
            node.Width = 160; node.Height = 64; node.MaximumTextWidth = 200;
            node.CaptionPlacement = placement; node.CaptionGap = 8; node.CaptionSurface = TopologyCaptionSurface.Plain;
            node.CaptionTextStyle = new TextStyleOverride { FontSize = node.Label.Contains('\n') ? 24 : 14 };
        }
        var layout = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions {
            IncludeLegend = false, FitContentToViewport = false, IncludeIconLabels = true, MaxNodeLabelLines = 3
        });
        var reports = layout.Nodes.Where(node => node.Id != "manager").OrderBy(node => node.Y).ToArray();
        var footprints = reports.Select(node => TopologyNodeFootprint.RenderedBounds(node, layout.RenderOptions!)!.Value).ToArray();
        Assert.True(footprints.Max(box => box.Bottom - box.Y) - footprints.Min(box => box.Bottom - box.Y) > 56);
        Assert.True(footprints[0].Bottom <= footprints[1].Y, "Horizontal sibling captions must remain inside disjoint reserved footprints.");
        var manager = layout.Nodes.Single(node => node.Id == "manager");
        Assert.Equal((footprints[0].Y + footprints[1].Bottom) / 2, manager.Y + manager.Height / 2, 6);
    }

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
