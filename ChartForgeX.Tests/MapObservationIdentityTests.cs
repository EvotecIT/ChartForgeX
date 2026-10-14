using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MapObservationIdentityTests {
    [Theory]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    [InlineData(ChartSeriesKind.DottedMap)]
    [InlineData(ChartSeriesKind.RegionMap)]
    [InlineData(ChartSeriesKind.TileMap)]
    public void DirectObservationsCaptureIdsAndPreparedOutputKeepsItsOriginalIdentity(ChartSeriesKind kind) {
        var chart = Sample(kind);
        var series = Assert.Single(chart.Series).WithObservationId(0, new ChartObservationId("first"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg();
        var mark = Assert.Single(Marks(svg, kind), element => (string?)element.Attribute("data-cfx-observation-id") == "first");
        Assert.Equal(series.Id.Value, (string?)mark.Attribute("data-cfx-series-key"));
        Assert.Equal(kind == ChartSeriesKind.DottedMap ? "local" : "shared", (string?)mark.Attribute("data-cfx-series-scope"));
        Assert.Equal("7", (string?)mark.Attribute("data-cfx-value"));
        Assert.Equal(1, series.ObservationCount);

        // Missing calendar days and regions retain their semantic targets without borrowing an observation identity.
        foreach (var missing in Marks(svg, kind).Where(element => (string?)element.Attribute("data-cfx-empty") == "true"))
            Assert.Null(missing.Attribute("data-cfx-observation-id"));
        if (kind != ChartSeriesKind.DottedMap)
            Assert.Contains(Marks(svg, kind), element => (string?)element.Attribute("data-cfx-empty") == "true");

        series.WithObservationId(0, new ChartObservationId("changed"));
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Contains(Marks(chart.ToSvg(), kind), element => (string?)element.Attribute("data-cfx-observation-id") == "changed");
    }

    [Fact]
    public void CalendarWithMultipleCurrentContributorsDoesNotBorrowOneContributorsId() {
        var chart = Sample(ChartSeriesKind.CalendarHeatmap);
        var series = Assert.Single(chart.Series);
        series.Points.Add(new ChartPoint(new DateTime(2026, 1, 5), 9));
        series.WithObservationId(0, new ChartObservationId("original"))
            .WithObservationId(1, new ChartObservationId("later"));

        var mark = Assert.Single(Marks(chart.ToSvg(), ChartSeriesKind.CalendarHeatmap),
            element => (string?)element.Attribute("data-cfx-empty") == "false");
        Assert.Equal("0,1", (string?)mark.Attribute("data-cfx-source-points"));
        Assert.Null(mark.Attribute("data-cfx-observation-id"));
    }

    [Fact]
    public void DottedObservationOutsideTheViewportKeepsItsAuthoredIdentity() {
        var chart = Chart.Create().WithMapViewport(ChartMapViewport.Europe()).AddDottedMap("Locations", new[] {
            new ChartMapPoint("New York", -74, 41, 7)
        });
        chart.Series[0].WithObservationId(0, new ChartObservationId("new-york"));

        var mark = Assert.Single(Marks(chart.ToSvg(), ChartSeriesKind.DottedMap));
        Assert.Equal("false", (string?)mark.Attribute("data-cfx-visible"));
        Assert.Equal("new-york", (string?)mark.Attribute("data-cfx-observation-id"));
    }

    private static Chart Sample(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(640, 360);
        var id = new ChartSeriesId("source");
        var regions = new[] { new ChartRegionMapItem("A", 3), new ChartRegionMapItem("Alpha", 4) };
        return kind switch {
            ChartSeriesKind.CalendarHeatmap => chart.AddCalendarHeatmap("Activity", new[] {
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 3),
                new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 4)
            }, id: id),
            ChartSeriesKind.DottedMap => chart.AddDottedMap("Locations", new[] { new ChartMapPoint("Warsaw", 21, 52, 7) }),
            ChartSeriesKind.RegionMap => chart.AddRegionMap("Values", new ChartMapDefinition("regions", "Regions", 100, 50, new[] {
                new ChartMapRegion("A", "Alpha", "M0 0H50V50H0Z"), new ChartMapRegion("B", "Beta", "M50 0H100V50H50Z")
            }), regions, id: id),
            ChartSeriesKind.TileMap => chart.AddTileMap("Values", new ChartTileMapDefinition("tiles", "Tiles", new[] {
                new ChartTileMapRegion("A", "Alpha", 0, 0), new ChartTileMapRegion("B", "Beta", 1, 0)
            }), regions, id: id),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static XElement[] Marks(string svg, ChartSeriesKind kind) {
        var role = kind switch {
            ChartSeriesKind.CalendarHeatmap => "calendar-cell",
            ChartSeriesKind.DottedMap => "dotted-map-point-source",
            ChartSeriesKind.RegionMap => "region-map-region-source",
            ChartSeriesKind.TileMap => "tile-map-region-source",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    }
}
