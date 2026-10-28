using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Lane text keeps a readable size, and group headers and summaries are never dropped in dense charts.</summary>
public sealed class StateTimelineLabelPolicyTests {
    private static readonly DateTime Day = new(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);

    internal static Chart DenseLanes(int width = 1100, int height = 450, int lanes = 12) {
        var chart = Chart.Create().WithTheme(VisualTheme.Report()).WithSize(width, height)
            .WithTitle("Domain controllers, last 24 hours").WithSubtitle("LDAP, Kerberos and DNS checks every 5 minutes, grouped by site")
            .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#1d8a52")), new ChartStateCategory("down", "Down", ChartColor.FromHex("#d4302f")))
            .WithXAxisTimeScale(TimeZoneInfo.Utc, showTimeZone: true);
        chart.Options.LaneSummaryHeader = "Available";
        var sites = new[] { "Warsaw", "Krakow", "Frankfurt" };
        for (var lane = 0; lane < lanes; lane++) {
            var name = lane % 4 == 3 ? $"synthetic-dc-{lane + 1:00}.branch-office.example.test" : $"DC{lane + 1}.ad.evotec.pl";
            chart.AddStateTimelineLane(name, new[] {
                new ChartStateTimelineSegment(Day, Day.AddHours(10 + lane % 5), "up"),
                new ChartStateTimelineSegment(Day.AddHours(10 + lane % 5), Day.AddHours(24), lane % 3 == 0 ? "down" : "up")
            }, summary: (0.9 + lane / 200d).ToString("P1", CultureInfo.InvariantCulture), group: sites[lane * sites.Length / lanes], id: new ChartSeriesId("dc/" + name));
        }
        return chart;
    }

    private static XElement[] Texts(XDocument svg, string role) => svg.Descendants()
        .Where(element => (string?)element.Attribute(ChartDomRoles.RoleAttribute) == role)
        .SelectMany(element => element.Name.LocalName == "text" ? new[] { element } : element.Descendants().Where(child => child.Name.LocalName == "text"))
        .ToArray();

    [Fact]
    public void ToSvg_DenseLanes_KeepsLaneTextAtOrAboveTheReadableFloor() {
        var svg = XDocument.Parse(DenseLanes().ToSvg());
        var labels = Texts(svg, ChartDomRoles.ScheduleRowLabel).Concat(Texts(svg, ChartDomRoles.LaneSummary)).Concat(Texts(svg, ChartDomRoles.StateTimelineGroup)).ToArray();
        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.True(double.Parse((string)label.Attribute("font-size")!, CultureInfo.InvariantCulture) >= 10,
            "Lane text must not shrink below 10 px: " + label.Value));
    }

    [Fact]
    public void ToSvg_DenseLanes_DrawsEveryGroupHeaderAndSummary() {
        var svg = XDocument.Parse(DenseLanes().ToSvg());
        Assert.Equal(new[] { "Warsaw", "Krakow", "Frankfurt" }, Texts(svg, ChartDomRoles.StateTimelineGroup).Select(text => text.Value).ToArray());
        Assert.Equal(12, Texts(svg, ChartDomRoles.LaneSummary).Length);
        Assert.Equal(12, Texts(svg, ChartDomRoles.ScheduleRowLabel).Length);
    }

    [Fact]
    public void ToSvg_OverfullLanes_HideLabelsThatWouldOverlapTheirNeighbours() {
        var svg = XDocument.Parse(DenseLanes(1100, 400, 36).ToSvg());
        var labels = Texts(svg, ChartDomRoles.ScheduleRowLabel)
            .Select(label => (Y: double.Parse((string)label.Attribute("y")!, CultureInfo.InvariantCulture), Size: double.Parse((string)label.Attribute("font-size")!, CultureInfo.InvariantCulture)))
            .OrderBy(label => label.Y).ToArray();
        Assert.All(labels, label => Assert.True(label.Size >= 10));
        for (var index = 1; index < labels.Length; index++)
            Assert.True(labels[index].Y - labels[index - 1].Y >= labels[index].Size, "Lane labels overlap at y=" + labels[index].Y.ToString(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("Arial")]
    [InlineData("ChartForgeX Missing Test Face")]
    public void ToSvg_OversizedTickFont_StillDrawsTheLaneLabelWithInstalledOrFallbackMetrics(string font) {
        var chart = Chart.Create().WithSize(360, 360).WithPngFont(font).WithLegend(false)
            .AddStateTimelineLane("API", new[] { new ChartStateTimelineSegment(1, 3, "up") }, "99%", "Services");
        chart.Options.TickLabelStyle.FontSize = 256;
        var labels = Texts(XDocument.Parse(chart.ToSvg()), ChartDomRoles.ScheduleRowLabel);
        Assert.Equal("API", Assert.Single(labels).Value);
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void ToSvg_OverfullGanttLanesWithMultilineNames_DoNotOverlapNeighbours(string lineBreak) {
        var chart = Chart.Create().WithSize(720, 300).WithLegend(false)
            .WithStateCategories(new ChartStateCategory("planned", "Planned", ChartColor.FromHex("#2563eb")));
        for (var lane = 0; lane < 18; lane++)
            chart.AddGanttLane("Lane " + lane + lineBreak + "second line", new[] { new ChartGanttLaneItem(Day, Day.AddHours(2 + lane % 4), "planned") }, id: new ChartSeriesId("lane-" + lane));
        var labels = Texts(XDocument.Parse(chart.ToSvg()), ChartDomRoles.ScheduleRowLabel)
            .Select(label => (Y: double.Parse((string)label.Attribute("y")!, CultureInfo.InvariantCulture), Size: double.Parse((string)label.Attribute("font-size")!, CultureInfo.InvariantCulture)))
            .OrderBy(label => label.Y).ToArray();
        for (var index = 1; index < labels.Length; index++)
            Assert.True(labels[index].Y - labels[index - 1].Y >= labels[index].Size * 2, "Two-line lane labels overlap at y=" + labels[index].Y.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToSvg_NarrowStaticLanes_TruncatesLongNamesInsteadOfShrinkingThem() {
        var svg = XDocument.Parse(DenseLanes(420, 450).ToSvg());
        var labels = Texts(svg, ChartDomRoles.ScheduleRowLabel);
        Assert.Contains(labels, label => label.Value.EndsWith("…", StringComparison.Ordinal));
        Assert.All(labels, label => Assert.True(double.Parse((string)label.Attribute("font-size")!, CultureInfo.InvariantCulture) >= 10));
    }
}
