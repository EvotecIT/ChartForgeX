using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CategoricalObservationLifecycleTests {
    private static Chart ChartWithStates() => Chart.Create().WithSize(640, 320).WithStateCategories(
        new ChartStateCategory("up", "Available", ChartColor.FromHex("#168257")),
        new ChartStateCategory("down", "Unavailable", ChartColor.FromHex("#BE3455")));

    [Fact]
    public void TimelineUpdateRetainsIdentityWhenTimeOrderChangesAndPreparedOutputStaysDetached() {
        var series = ChartSeries.CreateStateTimelineLane(new ChartSeriesId("service"), "Service", new[] {
            new ChartStateTimelineSegment(1, 2, "up", "First"), new ChartStateTimelineSegment(3, 4, "up", "Second")
        }).WithObservationId(0, new ChartObservationId("first"));
        var chart = ChartWithStates().AddSeries(series);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var before = prepared.ToSvg();
        series.UpdateStateTimelineSegment(0, new ChartStateTimelineSegment(5, 6, "down", "Current detail"));
        var marks = Marks(chart, "state-timeline-segment");
        Assert.Equal(new[] { "1", "0" }, marks.Select(mark => (string?)mark.Attribute("data-cfx-point")));
        var updated = marks.Single(mark => (string?)mark.Attribute("data-cfx-observation-id") == "first");
        Assert.Equal("5", (string?)updated.Attribute("data-cfx-start"));
        Assert.Equal("Current detail", (string?)updated.Attribute("data-cfx-meta-detail"));
        Assert.Equal("down", (string?)updated.Attribute("data-cfx-status"));
        Assert.Equal(2, series.SourcePointCount); Assert.Equal(2, series.ObservationCount);
        Assert.Empty(series.Points);
        Assert.Equal(before, prepared.ToSvg());
        Assert.NotEqual(before, chart.ToSvg());
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void TimelineRunsRetainOnlyTheirActualContributors() {
        var chart = ChartWithStates().AddStateTimelineLane("Service", new[] {
            new ChartStateTimelineSegment(1, 2, "up", "Separate detail"),
            new ChartStateTimelineSegment(1, 2, "up", "Shared detail"),
            new ChartStateTimelineSegment(2, 3, "up", "Shared detail")
        });
        chart.Series[0].WithObservationId(0, new ChartObservationId("separate"))
            .WithObservationId(1, new ChartObservationId("aggregate-contributor"));
        var marks = Marks(chart, "state-timeline-segment");
        Assert.Equal(2, marks.Length);
        var separate = marks.Single(mark => (string?)mark.Attribute("data-cfx-source-points") == "0");
        Assert.Equal("separate", (string?)separate.Attribute("data-cfx-observation-id"));
        var aggregate = marks.Single(mark => (string?)mark.Attribute("data-cfx-source-points") == "1,2");
        Assert.Null(aggregate.Attribute("data-cfx-observation-id"));
    }

    [Fact]
    public void LaneUpdateReordersPackingWithoutChangingObservationIdentity() {
        var series = ChartSeries.CreateGanttLane(new ChartSeriesId("incidents"), "Incidents", new[] {
            new ChartGanttLaneItem(1, 2, "down", "First"), new ChartGanttLaneItem(3, 4, "up", "Second")
        }).WithObservationId(0, new ChartObservationId("first"));
        var chart = ChartWithStates().AddSeries(series);
        series.UpdateGanttLaneItem(0, new ChartGanttLaneItem(5, 6, "up", "Updated", "Reordered detail"));
        var row = Assert.Single(ChartGanttLaneModel.Build(chart).Rows);
        Assert.Equal(1, row.SubRows);
        Assert.Equal(new[] { 1, 0 }, row.Items.Select(item => item.PointIndex));
        var updated = Marks(chart, "gantt-lane-item").Single(mark => (string?)mark.Attribute("data-cfx-observation-id") == "first");
        Assert.Equal("5", (string?)updated.Attribute("data-cfx-start"));
        Assert.Contains("Reordered detail", updated.ToString(), StringComparison.Ordinal);
        Assert.Empty(series.Points);
        Assert.Equal(2, series.SourcePointCount);
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void MaskedHeatmapColumnsKeepTheirObservationIdsAndCompleteCellFacts() {
        var cells = new ChartHeatmapCell?[] { new("up", "A"), null, new("down", "C", "Current detail", "#details") };
        var series = ChartSeries.CreateHeatmapCategoryRow(new ChartSeriesId("checks"), "Checks", cells)
            .WithObservationId(1, new ChartObservationId("middle")).WithObservationId(2, new ChartObservationId("last"));
        var chart = ChartWithStates().AddSeries(series);
        cells[2] = null;
        series.UpdateHeatmapCell(0, null);
        var last = Assert.Single(Marks(chart, "heatmap-cell"));
        Assert.Equal("last", (string?)last.Attribute("data-cfx-observation-id"));
        Assert.Equal("2", (string?)last.Attribute("data-cfx-point"));
        Assert.Contains("Current detail", last.ToString(), StringComparison.Ordinal);
        series.UpdateHeatmapCell(1, new ChartHeatmapCell("up", "B"));
        var middle = Marks(chart, "heatmap-cell").Single(mark => (string?)mark.Attribute("data-cfx-observation-id") == "middle");
        Assert.Equal("1", (string?)middle.Attribute("data-cfx-source-point"));
        Assert.Equal(3, series.ObservationCount); Assert.Equal(3, series.SourcePointCount);
        Assert.Empty(series.Points);
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void FailedReplacementKeepsLaneDataAndKeysWhileSuccessfulReplacementClearsOverrides() {
        var series = ChartSeries.CreateStateTimelineLane(new ChartSeriesId("service"), "Service",
            new[] { new ChartStateTimelineSegment(1, 2, "up") })
            .WithObservationId(0, new ChartObservationId("first")).WithPointColor(0, "#123456");
        var chart = ChartWithStates().AddSeries(series);
        var before = chart.ToSvg();
        Assert.Throws<ArgumentException>(() => series.ReplaceStateTimelineSegments(new[] { new ChartStateTimelineSegment(3, 4, "down"), default }));
        Assert.Equal(before, chart.ToSvg());
        series.ReplaceStateTimelineSegments(new[] { new ChartStateTimelineSegment(5, 6, "down") });
        Assert.Empty(series.ObservationIds); Assert.Empty(series.PointColors);
        Assert.Equal("service", series.Id.Value);
        series.ReplaceStateTimelineSegments(Array.Empty<ChartStateTimelineSegment>());
        Assert.Equal(0, series.SourcePointCount); Assert.Empty(Marks(chart, "state-timeline-segment"));
        Assert.True(chart.ToPng().Length > 200);
    }

    private static XElement[] Marks(Chart chart, string role) => XDocument.Parse(chart.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
