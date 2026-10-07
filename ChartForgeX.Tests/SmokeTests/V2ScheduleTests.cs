using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2ScheduleTests {
    [Theory]
    [InlineData(ChartSeriesKind.Timeline, "timeline-item")]
    [InlineData(ChartSeriesKind.Gantt, "gantt-task")]
    [InlineData(ChartSeriesKind.StateTimeline, "state-timeline-segment")]
    [InlineData(ChartSeriesKind.GanttLane, "gantt-lane-item")]
    public void SchedulesRetainSourceRegionsAndDetachGeometryTimeAndText(ChartSeriesKind kind, string role) {
        var chart = Fixture(kind); var context = Context(); var prepared = chart.Prepare(context);
        Assert.Contains(prepared.Regions, region => region.Role == role && region.Bounds.Width > 0);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.DoesNotContain("NaN", svg); Assert.DoesNotContain("Infinity", svg);
        chart.Options.GanttToday = 90; chart.Options.Labels.Now = "Changed"; chart.Series.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(640, prepared.Size.Width); Assert.Equal(400, prepared.Size.Height);
    }

    [Fact]
    public void StateLanesCoalesceAdjacentBucketsButPreserveGapProvenanceAndSummaries() {
        var chart = Chart.Create().WithStateCategories(new ChartStateCategory("up", "Available", ChartColor.FromHex("#1d8a52")))
            .AddStateTimelineLane("API", new[] { new ChartStateTimelineSegment(1, 2, "up"), new ChartStateTimelineSegment(2, 3, "up"), new ChartStateTimelineSegment(4, 5, "pending") }, "99%", "Production");
        chart.Options.LaneSummaryHeader = "Availability";
        var prepared = chart.Prepare(Context()); var document = XDocument.Parse(prepared.ToSvg());
        var segments = ByRole(document, "state-timeline-segment"); Assert.Equal(2, segments.Length);
        Assert.Equal("0,1", segments[0].Attribute("data-cfx-source-points")?.Value);
        var regions = prepared.Regions.Where(region => region.Role == "state-timeline-segment").ToArray();
        Assert.True(regions[1].Bounds.Left > regions[0].Bounds.Right);
        Assert.Contains(prepared.Regions, region => region.Label == "Availability"); Assert.Contains(prepared.Regions, region => region.Label == "99%");
        Assert.Contains(prepared.Regions, region => region.Label != null && region.Label.Contains("pending"));
    }

    [Fact]
    public void GanttLanePackingUsesPreparedWidthAndOpenItemsEndAtSnapshottedNow() {
        var chart = Chart.Create().WithSize(4000, 400).WithGanttToday(4d)
            .WithStateCategories(new ChartStateCategory("high", "High", ChartColor.FromHex("#d4302f")))
            .AddGanttLane("API", new[] { new ChartGanttLaneItem(1, 3, "high", "first"), new ChartGanttLaneItem(2, null, "high", "ongoing") }, "Production", "2");
        chart.Options.XAxis.WithBounds(0, 5);
        var prepared = chart.Prepare(Context()); var document = XDocument.Parse(prepared.ToSvg());
        var bars = prepared.Regions.Where(region => region.Role == "gantt-lane-item").ToArray(); Assert.Equal(2, bars.Length);
        Assert.True(bars[1].Bounds.Top >= bars[0].Bounds.Bottom);
        var open = ByRole(document, "gantt-lane-item").Single(element => (string?)element.Attribute("data-cfx-open") == "true");
        Assert.Equal("4", open.Attribute("data-cfx-end")?.Value);
        Assert.NotEmpty(ByRole(document, "gantt-lane-open-end"));
        var now = ByRole(document, "gantt-now-line").Single();
        Assert.Equal(double.Parse(now.Attribute("x1")!.Value, System.Globalization.CultureInfo.InvariantCulture), bars[1].Bounds.Right, 3);
    }

    [Fact]
    public void ClassicGanttPreservesMilestoneDependencyProgressAndExplicitWindow() {
        var chart = Fixture(ChartSeriesKind.Gantt).AddGanttMilestone("Ship", 4, dependsOn: 1).WithGanttToday(2.5);
        chart.Options.XAxis.WithBounds(1.5, 4.5);
        var prepared = chart.Prepare(Context()); var document = XDocument.Parse(prepared.ToSvg());
        Assert.Single(ByRole(document, "gantt-milestone")); Assert.Equal(2, ByRole(document, "gantt-dependency").Length);
        Assert.NotEmpty(ByRole(document, "gantt-progress"));
        var task = prepared.Regions.First(region => region.Role == "gantt-task");
        var progress = ByRole(document, "gantt-progress").First();
        var progressWidth = double.Parse(progress.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(task.Bounds.Width / 3, progressWidth, 3);
        Assert.Equal("1.5", ByRole(document, "gantt-chart").Single().Attribute("data-cfx-min")?.Value);
        Assert.All(prepared.Regions.Where(region => region.Role == "gantt-task"), region => Assert.True(region.Bounds.Width >= 0));
    }

    [Fact]
    public void CompactLanesKeepPlotAndMarksInsideFixedViewport() {
        var chart = Chart.Create().AddGanttLane("Long service name", new[] { new ChartGanttLaneItem(1, 3, "high") }, summary: "Long summary");
        chart.Options.LaneSummaryHeader = "Summary";
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(120, 80), 2),
            frame: new VisualFrame(showLegend: false)));
        Assert.All(prepared.Regions.Where(region => region.Role == "gantt-lane-item"), region => {
            Assert.InRange(region.Bounds.Left, 0, 120); Assert.InRange(region.Bounds.Right, 0, 120);
            Assert.InRange(region.Bounds.Top, 0, 80); Assert.InRange(region.Bounds.Bottom, 0, 80);
        });
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void TimeLanesRetainDisplayTimezoneAndExplicitTickLabels() {
        var start = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/Plus2", TimeSpan.FromHours(2), "Plus2", "Plus2");
        var chart = Chart.Create().WithXAxisTimeScale(zone, true, "TEST")
            .AddStateTimelineLane("API", new[] { new ChartStateTimelineSegment(start, start.AddHours(2), "up", "Full detail") });
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(start.ToOADate(), "Start"));
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(start.AddHours(2).ToOADate(), "Finish"));
        var prepared = chart.Prepare(Context()); var svg = prepared.ToSvg();
        Assert.Contains("Start", svg); Assert.Contains("Finish", svg); Assert.Contains("TEST", svg); Assert.Contains("Full detail", svg);
    }

    private static Chart Fixture(ChartSeriesKind kind) => kind switch {
        ChartSeriesKind.Timeline => Chart.Create().AddTimelineRange("Plan", 1, 3).AddTimelineRange("Build", 2, 4),
        ChartSeriesKind.Gantt => Chart.Create().AddGanttTask("Plan", 1, 3, .5).AddGanttTask("Build", 2, 4, .25, 0),
        ChartSeriesKind.StateTimeline => Chart.Create().AddStateTimelineLane("API", new[] { new ChartStateTimelineSegment(1, 3, "up") }),
        _ => Chart.Create().AddGanttLane("API", new[] { new ChartGanttLaneItem(1, 3, "high") })
    };
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(640, 400)), themeMode: VisualThemeMode.Dark,
        frame: new VisualFrame("Schedule", "Native scene", showLegend: false));
    private static XElement[] ByRole(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
