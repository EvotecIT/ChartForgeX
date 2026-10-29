using System.Reflection;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Guards the documented host-facing DOM names in docs/dom-contract.md.</summary>
public sealed class ChartDomContractTests {
    private static readonly DateTime Day = new(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);

    private static Chart StateTimeline() => Chart.Create().WithSize(720, 260)
        .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#1d8a52")), new ChartStateCategory("down", "Down", ChartColor.FromHex("#d4302f")))
        .AddStateTimelineLane("DC1.ad.evotec.pl", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(10), "up"), new ChartStateTimelineSegment(Day.AddHours(10), Day.AddHours(12), "down") },
            summary: "83%", group: "Warsaw", id: new ChartSeriesId("dc/DC1.ad.evotec.pl"))
        .AddStateTimelineLane("DC2.ad.evotec.pl", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(12), "up") },
            summary: "100%", group: "Warsaw", id: new ChartSeriesId("dc/DC2.ad.evotec.pl"))
        .AddStateTimelineLane("1.1.1.1", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(12), "up") }, summary: "100%", id: new ChartSeriesId("host/1.1.1.1"));

    private static Chart GanttLanes() => Chart.Create().WithSize(720, 260)
        .WithStateCategories(new ChartStateCategory("planned", "Planned", ChartColor.FromHex("#2563eb")))
        .AddGanttLane("Patching", new[] { new ChartGanttLaneItem(Day, Day.AddHours(4), "planned", "Window") }, group: "Operations", id: new ChartSeriesId("lane-patching"));

    private static Chart MixedSchedule() => Chart.Create().WithSize(720, 260)
        .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#1d8a52")))
        .AddStateTimelineLane("DC1", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(12), "up") }, group: "Warsaw", id: new ChartSeriesId("dc/DC1"))
        .AddGanttLane("Patching", new[] { new ChartGanttLaneItem(Day, Day.AddHours(4), "up", "Window") }, group: "Operations", id: new ChartSeriesId("lane-patching"));

    private static XElement[] ByRole(XDocument svg, string role) =>
        svg.Descendants().Where(element => (string?)element.Attribute(ChartDomRoles.RoleAttribute) == role).ToArray();

    [Fact]
    public void StateTimeline_Rows_CarryLaneIdGroupAndContractRoles() {
        var svg = XDocument.Parse(StateTimeline().ToSvg());

        var rows = ByRole(svg, ChartDomRoles.ScheduleRow);
        Assert.Equal(new[] { "dc/DC1.ad.evotec.pl", "dc/DC2.ad.evotec.pl", "host/1.1.1.1" }, rows.Select(row => (string?)row.Attribute(ChartDomRoles.LaneIdAttribute)).ToArray());
        Assert.Equal(new[] { "Warsaw", "Warsaw", null }, rows.Select(row => (string?)row.Attribute(ChartDomRoles.LaneGroupAttribute)).ToArray());
        Assert.All(rows, row => Assert.Single(row.Descendants(), element => (string?)element.Attribute(ChartDomRoles.RoleAttribute) == ChartDomRoles.ScheduleRowLabel));
        Assert.All(rows, row => Assert.Single(row.Descendants(), element => (string?)element.Attribute(ChartDomRoles.RoleAttribute) == ChartDomRoles.LaneSummary));
        // Rows identify lanes for hosts but must not become interaction targets.
        Assert.All(rows, row => Assert.Null(row.Attribute("data-cfx-series")));

        var segments = ByRole(svg, ChartDomRoles.StateTimelineSegment);
        Assert.Equal(new[] { "dc/DC1.ad.evotec.pl", "dc/DC1.ad.evotec.pl", "dc/DC2.ad.evotec.pl", "host/1.1.1.1" },
            segments.Select(segment => (string?)segment.Attribute(ChartDomRoles.LaneIdAttribute)).ToArray());

        var group = Assert.Single(ByRole(svg, ChartDomRoles.StateTimelineGroup));
        Assert.Equal("Warsaw", group.Value);
        Assert.Contains(group.Ancestors(), element => (string?)element.Attribute(ChartDomRoles.LaneGroupAttribute) == "Warsaw");
    }

    [Fact]
    public void GanttLanes_Rows_CarryLaneIdAndGroup() {
        var svg = XDocument.Parse(GanttLanes().ToSvg());
        var row = Assert.Single(ByRole(svg, ChartDomRoles.ScheduleRow));
        Assert.Equal("lane-patching", (string?)row.Attribute(ChartDomRoles.LaneIdAttribute));
        Assert.Equal("Operations", (string?)row.Attribute(ChartDomRoles.LaneGroupAttribute));
        Assert.Equal("lane-patching", (string?)Assert.Single(ByRole(svg, ChartDomRoles.GanttLaneItem)).Attribute(ChartDomRoles.LaneIdAttribute));
        var header = Assert.Single(ByRole(svg, ChartDomRoles.GanttLaneGroup));
        Assert.Equal("Operations", header.Value);
        Assert.Contains(header.Ancestors(), element => (string?)element.Attribute(ChartDomRoles.LaneGroupAttribute) == "Operations");
    }

    [Fact]
    public void MixedSchedule_RowsAndGroupHeaders_CarryLaneIdAndGroup() {
        var svg = XDocument.Parse(MixedSchedule().ToSvg());
        var rows = ByRole(svg, ChartDomRoles.ScheduleRow);
        Assert.Equal(new[] { "dc/DC1", "lane-patching" }, rows.Select(row => (string?)row.Attribute(ChartDomRoles.LaneIdAttribute)).ToArray());
        Assert.Equal(new[] { "Warsaw", "Operations" }, rows.Select(row => (string?)row.Attribute(ChartDomRoles.LaneGroupAttribute)).ToArray());
        var headers = ByRole(svg, ChartDomRoles.ScheduleGroup);
        Assert.Equal(new[] { "Warsaw", "Operations" }, headers.Select(header => header.Value).ToArray());
        Assert.All(headers, header => Assert.Contains(header.Ancestors(), element => (string?)element.Attribute(ChartDomRoles.LaneGroupAttribute) == header.Value));
    }

    [Fact]
    public void ContractRoles_AreAllEmittedByRepresentativeCharts() {
        var line = Chart.Create().WithSize(640, 320).WithLegend(true)
            .AddLine("Latency", new[] { new ChartPoint(1, 20), new ChartPoint(2, 35), new ChartPoint(3, 28) })
            .AddLine("Baseline", new[] { new ChartPoint(1, 18), new ChartPoint(2, 22), new ChartPoint(3, 24) });
        var topology = TopologyChart.Create().WithViewport(640, 360).AddAutoNode("A", "Receive").AddAutoNode("B", "Store").AddEdge("link", "A", "B");
        var emitted = new[] { line.ToSvg(), StateTimeline().ToSvg(), GanttLanes().ToSvg(), MixedSchedule().ToSvg(), topology.ToSvg() }
            .SelectMany(svg => XDocument.Parse(svg).Descendants())
            .Select(element => (string?)element.Attribute(ChartDomRoles.RoleAttribute))
            .Where(role => role != null).ToHashSet(StringComparer.Ordinal);

        var roles = typeof(ChartDomRoles).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.Name.EndsWith("Attribute", StringComparison.Ordinal))
            .Select(field => (string)field.GetRawConstantValue()!).ToArray();
        Assert.NotEmpty(roles);
        Assert.All(roles, role => Assert.Contains(role, emitted));
    }

    [Fact]
    public void ContractDocument_ListsEveryRoleAndAttribute() {
        var document = File.ReadAllText(Path.Combine(TestRepository.Root, "docs", "dom-contract.md"));
        foreach (var field in typeof(ChartDomRoles).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral))
            Assert.Contains("`" + (string)field.GetRawConstantValue()! + "`", document, StringComparison.Ordinal);
    }
}
