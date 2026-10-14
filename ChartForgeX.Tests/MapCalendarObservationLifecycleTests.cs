using System.Collections.Generic;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects complete map/calendar observations, normalization and retained identity through public exports.</summary>
public sealed class MapCalendarObservationLifecycleTests {
    private static readonly DateTime First = new(2026, 1, 5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefinitionsRejectUninitializedRegionSlotsAtConstruction(bool tileMap) {
        var error = Assert.Throws<ArgumentException>(() => {
            if (tileMap) _ = new ChartTileMapDefinition("custom", "Custom", new[] {
                new ChartTileMapRegion("A", "Alpha", 1, 0), default(ChartTileMapRegion)
            });
            else _ = new ChartMapDefinition("custom", "Custom", 100, 50, new[] {
                new ChartMapRegion("A", "Alpha", "M0 0H100V50H0Z"), default(ChartMapRegion)
            });
        });
        Assert.Equal("regions", error.ParamName);
    }

    [Theory]
    [InlineData(ChartSeriesKind.DottedMap)]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    [InlineData(ChartSeriesKind.RegionMap)]
    [InlineData(ChartSeriesKind.TileMap)]
    public void CompleteUpdatesKeepIdentityAndPreparedExportsRemainDetached(ChartSeriesKind kind) {
        var series = Create(kind).WithObservationId(0, new ChartObservationId("retained"));
        var chart = Chart.Create().WithSize(640, 360).AddSeries(series);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var before = prepared.ToSvg();
        Update(series);
        var mark = Assert.Single(Marks(chart), mark => (string?)mark.Attribute("data-cfx-observation-id") == "retained");
        Assert.Equal("owned", (string?)mark.Attribute("data-cfx-series-key"));
        Assert.Equal("9", (string?)mark.Attribute("data-cfx-value"));
        Assert.Equal(before, prepared.ToSvg());
        Assert.NotEqual(before, chart.ToSvg());
        Assert.Equal(2, series.ObservationCount);
        Assert.Equal(2, series.SourcePointCount);
        Assert.Empty(series.Points);
        Assert.Throws<NotSupportedException>(() => series.Points.Add(new ChartPoint(1, 2)));
        Assert.True(chart.ToPng().Length > 200);
        Assert.Contains("data-cfx-observation-id=\"retained\"", chart.ToHtmlPage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChartSeriesKind.DottedMap)]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    [InlineData(ChartSeriesKind.RegionMap)]
    [InlineData(ChartSeriesKind.TileMap)]
    public void ReplacementClearsObservationMetadataAndSupportsEmptyRefill(ChartSeriesKind kind) {
        var series = Create(kind).WithObservationId(0, new ChartObservationId("old"))
            .WithPointColor(0, "#123456").WithPointLabel(0, "Old label").WithState(ChartSeriesState.Warning);
        var chart = Chart.Create().WithSize(480, 280).AddSeries(series);
        ReplaceEmpty(series);
        Assert.Equal(0, series.SourcePointCount);
        Assert.Equal(0, series.ObservationCount);
        Assert.Empty(series.ObservationIds); Assert.Empty(series.PointColors); Assert.Empty(series.PointLabels);
        Assert.DoesNotContain(Marks(chart), mark => mark.Attribute("data-cfx-observation-id") != null);
        Assert.True(chart.ToPng().Length > 200);
        Refill(series);
        Assert.Equal(1, series.ObservationCount); Assert.Equal(1, series.SourcePointCount);
        Assert.Equal("owned", series.Id.Value); Assert.Equal(ChartSeriesState.Warning, series.StateRole);
        Assert.Contains(Marks(chart), mark => (string?)mark.Attribute("data-cfx-value") == "4");
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void DottedPointsKeepCoordinatesLabelsValuesAndColorsTogetherIncludingRepeatedCoordinates() {
        var input = new[] { new ChartMapPoint("Warsaw", 21, 52, 5), new ChartMapPoint("Paris", 2, 48, 9, ChartColor.FromHex("#123456")), new ChartMapPoint("Paris peer", 2, 48, 1) };
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("sites"), "Sites", input);
        var chart = Chart.Create().AddSeries(series).WithXLabels("Unrelated axis label");
        input[1] = new ChartMapPoint("Changed input", 1, 1);
        Assert.Equal(3, series.ObservationCount); Assert.Equal("Paris", series.MapPoints[1].Label);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartMapPoint>)series.MapPoints)[1] = input[1]);
        series.ReplaceMapPoints(series.MapPoints.Skip(1));
        var points = Marks(chart).Where(mark => (string?)mark.Attribute("data-cfx-role") == "dotted-map-point-source").ToArray();
        Assert.Equal(new[] { "Paris", "Paris peer" }, points.Select(mark => (string?)mark.Attribute("data-cfx-label")));
        Assert.Equal("9", (string?)points[0].Attribute("data-cfx-value"));
        Assert.Equal("2", (string?)points[0].Attribute("data-cfx-longitude"));
        var glyph = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneEllipse>().First(mark => mark.Role == "dotted-map-point");
        Assert.Equal(ChartColor.FromHex("#123456"), glyph.Fill);
        chart.AddMapRouteBetweenPoints("Route snapshot", "Paris", "Paris peer");
        series.UpdateMapPoint(0, new ChartMapPoint("Paris moved", 5, 50, 7));
        Assert.Equal(2, chart.Options.MapConnectors[0].FromLongitude);
        Assert.Equal(48, chart.Options.MapConnectors[0].FromLatitude);
    }

    [Fact]
    public void CalendarNormalizationAndUpdatesKeepDateOwnershipUnambiguous() {
        var red = ChartColor.FromHex("#AB1234");
        var series = ChartSeries.CreateCalendarHeatmap(new ChartSeriesId("days"), "Days", new[] {
            new ChartCalendarHeatmapItem(First.AddDays(2), 2), new ChartCalendarHeatmapItem(First.AddHours(4), 3, red),
            new ChartCalendarHeatmapItem(First, 4), new ChartCalendarHeatmapItem(First.AddDays(1), 1)
        }).WithObservationId(0, new ChartObservationId("original-day"));
        Assert.Equal(new[] { First, First.AddDays(1), First.AddDays(2) }, series.CalendarItems.Select(item => item.Date));
        Assert.Equal(7, series.CalendarItems[0].Value); Assert.Equal(red, series.CalendarItems[0].Color);
        var chart = Chart.Create().AddSeries(series);
        var before = chart.ToSvg();
        Assert.Throws<ArgumentException>(() => series.UpdateCalendarItem(0, new ChartCalendarHeatmapItem(First.AddDays(1), 10)));
        Assert.Equal(before, chart.ToSvg());
        series.UpdateCalendarItem(0, new ChartCalendarHeatmapItem(First.AddDays(5), 11));
        Assert.Equal(First.AddDays(5), series.CalendarItems[0].Date);
        var updated = Assert.Single(Marks(chart), mark => (string?)mark.Attribute("data-cfx-observation-id") == "original-day");
        Assert.Equal("2026-01-10", (string?)updated.Attribute("data-cfx-date"));
        Assert.Equal("11", (string?)updated.Attribute("data-cfx-value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegionNormalizationResolvesAliasesAndGeometryTravelsWithTheSeries(bool tile) {
        var series = tile ? ChartSeries.CreateTileMap(new ChartSeriesId("regions"), "Regions", Tiles(), Values())
            : ChartSeries.CreateRegionMap(new ChartSeriesId("regions"), "Regions", Regions(), Values());
        Assert.Equal(new[] { "a-Mixed", "b-Mixed" }, series.RegionValues.Select(item => item.Region));
        Assert.Equal(5, series.RegionValues[0].Value);
        series.WithObservationId(0, new ChartObservationId("a"));
        var first = Chart.Create().WithXLabels("Unrelated").AddSeries(series);
        var before = first.ToSvg();
        Assert.Throws<ArgumentException>(() => series.UpdateRegionValue(0, new ChartRegionMapItem("Beta", 20)));
        Assert.Equal(before, first.ToSvg());
        first.RemoveSeries(series);
        var second = Chart.Create().AddSeries(series);
        Assert.Contains("data-cfx-map-id=\"owned-map\"", second.ToSvg(), StringComparison.Ordinal);
        Assert.Contains("data-cfx-region=\"a-Mixed\"", second.ToSvg(), StringComparison.Ordinal);
        Assert.NotNull(tile ? (object?)series.TileMapDefinition : series.RegionMapDefinition);
        var aliases = tile ? series.TileMapDefinition!.Regions[0].Aliases : series.RegionMapDefinition!.Regions[0].Aliases;
        Assert.Throws<NotSupportedException>(() => ((IList<string>)aliases)[0] = "Changed alias");
        Assert.Equal("Alpha alias", aliases[0]);
        Assert.True(second.ToPng().Length > 200);
    }

    [Theory]
    [InlineData(ChartSeriesKind.DottedMap)]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    [InlineData(ChartSeriesKind.RegionMap)]
    [InlineData(ChartSeriesKind.TileMap)]
    public void FailedReplacementKeepsInstalledDataMetadataAndGeometry(ChartSeriesKind kind) {
        var series = Create(kind).WithObservationId(0, new ChartObservationId("first")).WithPointColor(0, "#123456");
        var chart = Chart.Create().AddSeries(series);
        var before = chart.ToSvg();
        Assert.ThrowsAny<ArgumentException>(() => {
            switch (kind) {
                case ChartSeriesKind.DottedMap: series.ReplaceMapPoints(new[] { new ChartMapPoint("Valid", 1, 2), default }); break;
                case ChartSeriesKind.CalendarHeatmap: series.ReplaceCalendarItems(new[] { new ChartCalendarHeatmapItem(First, double.MaxValue), new ChartCalendarHeatmapItem(First, double.MaxValue) }); break;
                default: series.ReplaceRegionValues(new[] { new ChartRegionMapItem("Alpha", double.MaxValue), new ChartRegionMapItem("a-Mixed", double.MaxValue) }); break;
            }
        });
        Assert.Equal(before, chart.ToSvg()); Assert.Equal(2, series.ObservationCount);
        Assert.Equal("first", series.ObservationIds[0].Value); Assert.Single(series.PointColors);
    }

    [Fact]
    public void FailedConvenienceAddDoesNotChangePaintGeometryOrAxisLabels() {
        var chart = Chart.Create().WithXLabels("Host label");
        var scale = ChartColorScale.Sequential(ChartColor.FromHex("#123456"), ChartColor.FromHex("#ABCDEF"));
        Assert.Throws<ArgumentException>(() => chart.AddRegionHeatmap("Bad", Regions(), new[] { new ChartRegionMapItem("Missing", 1) }, scale));
        Assert.Empty(chart.Series); Assert.Null(chart.Options.MapColorScale);
        Assert.Equal("Host label", Assert.Single(chart.Options.XAxisLabels).Text);
        Assert.Throws<ArgumentException>(() => chart.AddTileHeatmap("Bad", Tiles(), new[] { default(ChartRegionMapItem) }, scale));
        Assert.Empty(chart.Series); Assert.Null(chart.Options.MapColorScale);
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartSeries.CreateCalendarHeatmap(new ChartSeriesId("bad"), "Bad", new[] { default(ChartCalendarHeatmapItem) }));
    }

    private static ChartSeries Create(ChartSeriesKind kind) => kind switch {
        ChartSeriesKind.DottedMap => ChartSeries.CreateDottedMap(new ChartSeriesId("owned"), "Items", new[] { new ChartMapPoint("First", 1, 2, 3), new ChartMapPoint("Second", 3, 4, 5) }),
        ChartSeriesKind.CalendarHeatmap => ChartSeries.CreateCalendarHeatmap(new ChartSeriesId("owned"), "Items", new[] { new ChartCalendarHeatmapItem(First, 3), new ChartCalendarHeatmapItem(First.AddDays(1), 5) }),
        ChartSeriesKind.RegionMap => ChartSeries.CreateRegionMap(new ChartSeriesId("owned"), "Items", Regions(), new[] { new ChartRegionMapItem("Alpha", 3), new ChartRegionMapItem("Beta", 5) }),
        _ => ChartSeries.CreateTileMap(new ChartSeriesId("owned"), "Items", Tiles(), new[] { new ChartRegionMapItem("Alpha", 3), new ChartRegionMapItem("Beta", 5) })
    };
    private static void Update(ChartSeries series) {
        if (series.Kind == ChartSeriesKind.DottedMap) series.UpdateMapPoint(0, new ChartMapPoint("Updated", 5, 6, 9));
        else if (series.Kind == ChartSeriesKind.CalendarHeatmap) series.UpdateCalendarItem(0, new ChartCalendarHeatmapItem(First.AddDays(3), 9));
        else series.UpdateRegionValue(0, new ChartRegionMapItem("Alpha", 9));
    }
    private static void ReplaceEmpty(ChartSeries series) {
        if (series.Kind == ChartSeriesKind.DottedMap) series.ReplaceMapPoints(Array.Empty<ChartMapPoint>());
        else if (series.Kind == ChartSeriesKind.CalendarHeatmap) series.ReplaceCalendarItems(Array.Empty<ChartCalendarHeatmapItem>());
        else series.ReplaceRegionValues(Array.Empty<ChartRegionMapItem>());
    }
    private static void Refill(ChartSeries series) {
        if (series.Kind == ChartSeriesKind.DottedMap) series.ReplaceMapPoints(new[] { new ChartMapPoint("Refilled", 2, 3, 4) });
        else if (series.Kind == ChartSeriesKind.CalendarHeatmap) series.ReplaceCalendarItems(new[] { new ChartCalendarHeatmapItem(First, 4) });
        else series.ReplaceRegionValues(new[] { new ChartRegionMapItem("Alpha", 4) });
    }
    private static IEnumerable<ChartRegionMapItem> Values() => new[] { new ChartRegionMapItem("Beta", 1), new ChartRegionMapItem("Alpha alias", 2), new ChartRegionMapItem("A-mIXED", 3) };
    private static ChartMapDefinition Regions() => new("owned-map", "Owned map", 100, 50, new[] {
        new ChartMapRegion("a-Mixed", "Alpha", "M0 0H50V50H0Z", aliases: new[] { "Alpha alias" }), new ChartMapRegion("b-Mixed", "Beta", "M50 0H100V50H50Z") });
    private static ChartTileMapDefinition Tiles() => new("owned-map", "Owned map", new[] {
        new ChartTileMapRegion("a-Mixed", "Alpha", 0, 0, new[] { "Alpha alias" }), new ChartTileMapRegion("b-Mixed", "Beta", 1, 0) });
    private static XElement[] Marks(Chart chart) => XDocument.Parse(chart.ToSvg()).Descendants().Where(node => node.Attribute("data-cfx-role") != null).ToArray();
}
