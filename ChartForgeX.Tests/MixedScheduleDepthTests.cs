using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MixedScheduleDepthTests {
    [Theory]
    [InlineData(390, false, false)]
    [InlineData(390, true, true)]
    [InlineData(900, false, true)]
    [InlineData(900, true, false)]
    public void FourTypedSourcesShareOneWindowAxisAndSourceOrderedPackedRows(int width, bool dark, bool reversed) {
        var chart = Fixture(width, dark, reversed); var prepared = ScheduleReversalDepthTests.Prepare(chart);
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Single(xml.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "schedule-composition");
        Assert.Equal(new[] { "range", "task", "states", "incidents", "next", "marker" }, chart.Series.Select(series => series.Id.Value));
        foreach (var role in new[] { "timeline-item", "gantt-task", "state-timeline-segment", "gantt-lane-item", "gantt-vertical-marker" })
            Assert.Contains(xml.Descendants(), node => (string?)node.Attribute("data-cfx-role") == role);
        var projection = prepared.Scene.DataProjection!; Assert.Equal(reversed, projection.X.Reversed); Assert.Null(projection.Y);
        Assert.Equal(0, projection.X.Minimum); Assert.Equal(10, projection.X.Maximum);
        Assert.Equal(5, prepared.Scene.Regions.Count(region => region.Role == "schedule-lane"));
        var lanes = prepared.Scene.Regions.Where(region => region.Role == "schedule-lane").ToArray();
        Assert.Equal(5, lanes.Select(lane => lane.Id).Distinct().Count());
        var state = xml.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "state-timeline-segment");
        Assert.Equal("0,1", state.Attribute("data-cfx-source-points")!.Value); Assert.Equal("states", state.Attribute("data-cfx-series-key")!.Value);
        var incidentRows = xml.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "gantt-lane-item").ToArray();
        Assert.Equal(2, incidentRows.Length); Assert.Equal(2, incidentRows.Select(node => node.Attribute("data-cfx-sub-row")!.Value).Distinct().Count());
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        Assert.NotEmpty(prepared.ToPng()); NumericRadialEncodedGeometryTests.Capture(chart, "schedule-mixed-" + width + (dark ? "-dark" : "-light") + (reversed ? "-reversed" : ""));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Timeline, ChartSeriesKind.Gantt)]
    [InlineData(ChartSeriesKind.Timeline, ChartSeriesKind.StateTimeline)]
    [InlineData(ChartSeriesKind.Timeline, ChartSeriesKind.GanttLane)]
    [InlineData(ChartSeriesKind.Gantt, ChartSeriesKind.StateTimeline)]
    [InlineData(ChartSeriesKind.Gantt, ChartSeriesKind.GanttLane)]
    [InlineData(ChartSeriesKind.StateTimeline, ChartSeriesKind.GanttLane)]
    public void EveryScheduleKindPairUsesTheSameRealRowOwner(ChartSeriesKind first, ChartSeriesKind second) {
        var chart = Chart.Create().WithSize(700, 460).WithHeader(false).WithLegend(false).WithXAxisBounds(0, 10)
            .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#218A63")));
        chart.AddSeries(Source(first, "first")).AddSeries(Source(second, "second"));
        var prepared = ScheduleReversalDepthTests.Prepare(chart); Assert.Equal(2, prepared.Scene.Regions.Count(region => region.Role == "schedule-lane"));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "schedule-composition");
        Assert.Equal(2, chart.Series.Count); Assert.All(chart.Series, series => Assert.Equal(1, series.ObservationCount)); Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic)]
    public void MixedNonlinearAxesRejectBeforeAnyPreparedDrawingAndRawInputsStayIntact(ChartScaleKind scale) {
        var chart = Fixture(700, false, false); var raw = chart.Series[0].Points.ToArray(); chart.WithXAxisBounds(1, 10); chart.Options.XAxis.Scale = scale;
        Assert.Contains("linear or time", Assert.Throws<InvalidOperationException>(() => ScheduleReversalDepthTests.Prepare(chart)).Message);
        Assert.Equal(raw, chart.Series[0].Points); Assert.Equal(6, chart.Series.Count);
    }

    [Fact]
    public void SecondaryAxesAndAmbiguousMultiObservationTimelineSourcesRejectClearly() {
        var chart = Fixture(700, false, false); chart.Series[0].UseSecondaryValueAxis();
        Assert.Contains("secondary", Assert.Throws<InvalidOperationException>(() => ScheduleReversalDepthTests.Prepare(chart)).Message);
        chart.Series[0].UsePrimaryValueAxis(); chart.Series[0].Points.Add(new ChartPoint(2, 5));
        Assert.Contains("one start/end", Assert.Throws<InvalidOperationException>(() => ScheduleReversalDepthTests.Prepare(chart)).Message);
        Assert.Equal(2, chart.Series[0].ObservationCount);
    }

    [Fact]
    public void EmptyRefillReorderAndRemovalKeepRealSourcesAndRelationshipsWithoutChangingCapturedOutput() {
        var chart = Fixture(800, false, false); var before = ScheduleReversalDepthTests.Prepare(chart); var svg = before.ToSvg(); var png = before.ToPng();
        var task = chart.Series.Single(series => series.Id.Value == "task"); var saved = task.GanttItems.ToArray();
        task.ReplaceGanttItems(Array.Empty<ChartGanttItem>()); var empty = ScheduleReversalDepthTests.Prepare(chart);
        Assert.DoesNotContain(empty.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        Assert.Equal(5, empty.Scene.Regions.Count(region => region.Role == "schedule-lane")); Assert.Single(chart.GanttDependencies);
        task.ReplaceGanttItems(saved); chart.MoveSeries(task, 4); var moved = ScheduleReversalDepthTests.Prepare(chart);
        Assert.Contains(moved.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "gantt-dependency-arrow");
        Assert.Equal("task", chart.GanttDependencies[0].PredecessorId.Value); Assert.Equal(svg, before.ToSvg()); Assert.Equal(png, before.ToPng());
        chart.RemoveSeries(task); Assert.Empty(chart.GanttDependencies); Assert.NotEmpty(ScheduleReversalDepthTests.Prepare(chart).ToPng());
    }

    [Fact]
    public void RepeatedLocalHourAndRawExclusiveStateEndRemainDatesRatherThanFictionalLaneCoordinates() {
        var start = new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc); var end = start.AddHours(1);
        var chart = Chart.Create().WithSize(800, 480).WithXAxisTimeScale(TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw"))
            .WithXAxisBounds(start.ToOADate(), end.AddHours(1).ToOADate())
            .AddTimelineItem("Window", start, end.AddHours(1)).AddStateTimelineLane("Service", new[] { new ChartStateTimelineSegment(start, end, "up") });
        chart.Options.XAxis.Reversed = chart.Options.YAxis.Reversed = true;
        var prepared = ScheduleReversalDepthTests.Prepare(chart); var xml = XDocument.Parse(prepared.ToSvg());
        var source = xml.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "state-timeline-segment");
        Assert.Contains("+02:00", source.Attribute("data-cfx-meta-start")!.Value); Assert.Contains("+01:00", source.Attribute("data-cfx-meta-end")!.Value);
        Assert.Equal("true", source.Attribute("data-cfx-range-end-exclusive")!.Value); Assert.Null(prepared.Scene.DataProjection!.Y);
        Assert.Equal(start.ToOADate(), double.Parse(source.Attribute("data-cfx-start")!.Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CompressedSessionsRetainTheirExistingClearUnsupportedScheduleBoundary() {
        var day = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc).ToOADate();
        var chart = Chart.Create().WithXAxisBounds(day, day + 4).AddTimelineRange("First session", day, day + 1)
            .AddStateTimelineLane("Last session", new[] { new ChartStateTimelineSegment(day + 3, day + 4, "up") });
        chart.Options.XAxis.WithTimeSessions(new ChartTimeSessionDomain(new[] { new ChartTimeSession(day, day + 1), new ChartTimeSession(day + 3, day + 4) }));
        Assert.Contains("Time session compression", Assert.Throws<NotSupportedException>(() => ScheduleReversalDepthTests.Prepare(chart)).Message);
        Assert.Equal(day, chart.Series[0].Points[0].X); Assert.Equal(day + 4, chart.Series[1].StateTimelineSegments[0].End);
    }

    internal static Chart Fixture(int width, bool dark, bool reversed) {
        var chart = Chart.Create().WithSize(width, 640).WithHeader(false).WithLegend(false).WithDataLabels(false).WithXAxisBounds(0, 10)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#218A63")));
        chart.AddSeries(new ChartSeries(new ChartSeriesId("range"), "Preparation window", ChartSeriesKind.Timeline, new[] { new ChartPoint(1, 9) }));
        var first = ChartSeries.CreateGanttTask(new ChartSeriesId("task"), "Implement", 2, 4, .5); chart.AddSeries(first);
        chart.AddSeries(ChartSeries.CreateStateTimelineLane(new ChartSeriesId("states"), "Service", new[] {
            new ChartStateTimelineSegment(1, 3, "up"), new ChartStateTimelineSegment(3, 5, "up") }, summary: "Ready", group: "Resources"));
        chart.AddSeries(ChartSeries.CreateGanttLane(new ChartSeriesId("incidents"), "Incidents", new[] {
            new ChartGanttLaneItem(2, 6, "up", "Review"), new ChartGanttLaneItem(3, null, "up", "Ongoing") }, group: "Resources", summary: "2 items"));
        var next = ChartSeries.CreateGanttTask(new ChartSeriesId("next"), "Release", 7, 9); chart.AddSeries(next).AddGanttDependency(first, next);
        chart.AddSeries(ChartSeries.CreateGanttMarker(new ChartSeriesId("marker"), "Checkpoint", 8)); chart.WithGanttToday(9);
        chart.Options.XAxis.Reversed = chart.Options.YAxis.Reversed = reversed;
        foreach (var source in chart.Series) if (source.ObservationCount > 0) source.WithObservationId(0, new ChartObservationId(source.Id.Value + "-first"));
        return chart;
    }

    private static ChartSeries Source(ChartSeriesKind kind, string id) => kind == ChartSeriesKind.Timeline
        ? new ChartSeries(new ChartSeriesId(id), id, kind, new[] { new ChartPoint(1, 4) })
        : kind == ChartSeriesKind.Gantt ? ChartSeries.CreateGanttTask(new ChartSeriesId(id), id, 1, 4, .5)
        : kind == ChartSeriesKind.StateTimeline ? ChartSeries.CreateStateTimelineLane(new ChartSeriesId(id), id, new[] { new ChartStateTimelineSegment(1, 4, "up") })
        : ChartSeries.CreateGanttLane(new ChartSeriesId(id), id, new[] { new ChartGanttLaneItem(1, 4, "up") });
}
