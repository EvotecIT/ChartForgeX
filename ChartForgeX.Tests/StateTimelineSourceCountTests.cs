using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StateTimelineSourceCountTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FactoriesCountAuthoredIntervalsAfterSortingAndRetainEmptyCounts(bool standalone) {
        var segments = new[] {
            new ChartStateTimelineSegment(3, 4, "up", "Last"),
            new ChartStateTimelineSegment(1, 2, "up", "First"),
            new ChartStateTimelineSegment(2, 3, "up", "Middle")
        };
        var chart = Chart.Create().WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#168257")));
        if (standalone) chart.AddSeries(ChartSeries.CreateStateTimelineLane(new ChartSeriesId("service"), "Service", segments));
        else chart.AddStateTimelineLane("Service", segments);
        var series = chart.Series[0];
        Assert.Equal(segments.Length, series.SourcePointCount);
        Assert.Equal(segments.Length, series.ObservationCount);
        Assert.Equal(new[] { 1d, 2d, 3d }, series.Points.Select(point => point.X));
        Assert.Equal(new[] { "First", "Middle", "Last" }, series.StateTimelineDetails);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "state-timeline-segment").ToArray();
        Assert.Equal(segments.Length, marks.Length);
        Assert.All(marks, mark => Assert.Equal(series.Id.Value, mark.Metadata["data-cfx-series-key"]));

        var empty = standalone
            ? ChartSeries.CreateStateTimelineLane(new ChartSeriesId("empty"), "Empty", Array.Empty<ChartStateTimelineSegment>())
            : Chart.Create().AddStateTimelineLane("Empty", Array.Empty<ChartStateTimelineSegment>()).Series[0];
        Assert.Equal(0, empty.SourcePointCount); Assert.Equal(0, empty.ObservationCount);
    }
}
