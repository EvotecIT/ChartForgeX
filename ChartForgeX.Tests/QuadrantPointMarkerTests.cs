using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Scatter observations keep their own detached marker paint, geometry and source identity.</summary>
public sealed class QuadrantPointMarkerTests {
    [Fact]
    public void ConfigurationAndReadbackDoNotExposeInstalledOverridesAndThrowingCallbacksAreAtomic() {
        var series = Series();
        ChartMarkerOptions? captured = null;
        Assert.Same(series, series.ConfigurePointMarkers(1, marker => {
            captured = marker; marker.Radius = 11; marker.Fill = ChartColor.White;
        }));
        captured!.Radius = 99; captured.Fill = ChartColor.Black;
        var readback = series.GetPointMarkers(1)!;
        Assert.Equal(11, readback.Radius); Assert.Equal(ChartColor.White, readback.Fill);
        readback.Radius = 77; readback.StrokeWidth = 6;
        Assert.Equal(11, series.GetPointMarkers(1)!.Radius);
        Assert.Null(series.GetPointMarkers(1)!.StrokeWidth);
        Assert.Throws<InvalidOperationException>(() => series.ConfigurePointMarkers(1, marker => {
            marker.Radius = 40; marker.Fill = ChartColor.Black; throw new InvalidOperationException("Rejected authoring.");
        }));
        Assert.Equal(11, series.GetPointMarkers(1)!.Radius);
        Assert.Equal(ChartColor.White, series.GetPointMarkers(1)!.Fill);
        Assert.Null(series.GetPointMarkers(0));
        Assert.Same(series, series.UseSeriesMarkers(1)); Assert.Null(series.GetPointMarkers(1));
    }

    [Fact]
    public void PointColorPrecedesPointMarkerFillAndPointLegendsUseTheActualOutline() {
        var chart = Chart.Create().WithSize(640, 400).WithDataLabels(false).WithPointLegend().WithLegend()
            .WithXAxisBounds(0, 4).WithYAxisBounds(0, 40).AddSeries(Series());
        var series = chart.Series[0];
        series.ConfigureMarkers(marker => { marker.Radius = 4; marker.Fill = ChartColor.White; marker.Stroke = ChartColor.Black; marker.StrokeWidth = 1; });
        var red = ChartColor.FromHex("#B91C1C"); var blue = ChartColor.FromHex("#1D4ED8"); var green = ChartColor.FromHex("#047857");
        series.ConfigurePointMarkers(1, marker => { marker.Radius = 13; marker.Fill = red; marker.Stroke = blue; marker.StrokeWidth = 3; });
        series.WithPointColor(1, green);
        var prepared = Prepare(chart); var xml = XDocument.Parse(prepared.ToSvg());
        var point = Source(xml, "point", "series-0-point-1");
        var mark = Assert.Single(point.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "marker");
        Assert.Equal(13, double.Parse(mark.Attribute("rx")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(green, mark.RenderedColor("fill")); Assert.Equal(blue, mark.RenderedColor("stroke"));
        Assert.Equal("3", (string?)mark.Attribute("stroke-width"));
        var legend = Source(xml, "legend-entry", "legend-series-0-point-1");
        var swatch = Assert.Single(legend.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "legend-swatch");
        Assert.Equal(green, swatch.RenderedColor("fill")); Assert.Equal(blue, swatch.RenderedColor("stroke"));
        Assert.Equal("3", (string?)swatch.Attribute("stroke-width"));
        series.UseSeriesColor(1);
        var reset = XDocument.Parse(Prepare(chart).ToSvg());
        Assert.Equal(red, Assert.Single(Source(reset, "point", "series-0-point-1").Descendants(),
            node => (string?)node.Attribute("data-cfx-role") == "marker").RenderedColor("fill"));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(320, 240)]
    [InlineData(800, 460)]
    public void EnabledPointOverridesDriveAutomaticBoundsAndSemanticFootprints(int width, int height) {
        var chart = Chart.Create().WithSize(width, height).WithHeader(false).WithLegend(false).WithDataLabels(false).AddSeries(Series());
        var series = chart.Series[0];
        series.ConfigureMarkers(marker => { marker.Enabled = false; marker.Radius = 3; });
        series.ConfigurePointMarkers(0, marker => { marker.Enabled = true; marker.Radius = 7; marker.Stroke = ChartColor.Black; marker.StrokeWidth = 2; });
        series.ConfigurePointMarkers(2, marker => { marker.Enabled = true; marker.Radius = 28; marker.Stroke = ChartColor.Black; marker.StrokeWidth = 6; });
        var prepared = Prepare(chart);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "marker").ToArray();
        Assert.Equal(new[] { 7d, 28d }, marks.Select(mark => mark.Rx));
        var horizontal = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-x");
        var vertical = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-y");
        foreach (var mark in marks) {
            var extent = mark.Rx + mark.StrokeWidth / 2;
            Assert.True(mark.Cx - extent >= horizontal.Start.X - 1e-7);
            Assert.True(mark.Cx + extent <= horizontal.End.X + 1e-7);
            Assert.True(mark.Cy - extent >= vertical.Start.Y - 1e-7);
            Assert.True(mark.Cy + extent <= vertical.End.Y + 1e-7);
        }
        Assert.Equal(16, prepared.Regions.Single(region => region.Id == "series-0-point-0").Bounds.Width, 8);
        Assert.Equal(62, prepared.Regions.Single(region => region.Id == "series-0-point-2").Bounds.Width, 8);
        Assert.Contains(prepared.Regions, region => region.Id == "series-0-point-1");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void InsertRemoveItemReplacementAndClearKeepOverridesWithTheirObservation() {
        var series = Series().ConfigurePointMarkers(1, marker => { marker.Radius = 12; marker.Fill = ChartColor.Black; });
        series.Points.Insert(0, new ChartPoint(0, 0));
        Assert.Null(series.GetPointMarkers(0)); Assert.Equal("b", series.ObservationIds[2].Value);
        Assert.Equal(12, series.GetPointMarkers(2)!.Radius); Assert.Null(series.GetPointMarkers(1));
        series.Points.RemoveAt(1);
        Assert.Equal("b", series.ObservationIds[1].Value); Assert.Equal(12, series.GetPointMarkers(1)!.Radius);
        series.Points[1] = new ChartPoint(2, 25);
        Assert.Equal("b", series.ObservationIds[1].Value); Assert.Equal(12, series.GetPointMarkers(1)!.Radius);
        series.Points.RemoveAt(1);
        Assert.Null(series.GetPointMarkers(1)); Assert.DoesNotContain(series.ObservationIds.Values, id => id.Value == "b");
        series.ConfigurePointMarkers(1, marker => marker.Radius = 20);
        series.Points.Clear(); series.Points.Add(new ChartPoint(8, 80));
        Assert.Null(series.GetPointMarkers(0)); Assert.Empty(series.ObservationIds);
    }

    [Fact]
    public void KeyedReplacementAndRollingEvictionMoveOnlyRetainedAuthoredOverrides() {
        var series = Series().ConfigurePointMarkers(1, marker => { marker.Radius = 12; marker.StrokeWidth = 4; })
            .ConfigurePointMarkers(2, marker => marker.Radius = 18);
        series.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 25)),
            ChartObservationChange<ChartPoint>.Append(Id("d"), new ChartPoint(4, 40))
        }, maximumObservations: 3);
        Assert.Equal(new[] { "b", "c", "d" }, Keys(series));
        Assert.Equal(12, series.GetPointMarkers(0)!.Radius); Assert.Equal(4, series.GetPointMarkers(0)!.StrokeWidth);
        Assert.Equal(18, series.GetPointMarkers(1)!.Radius); Assert.Null(series.GetPointMarkers(2));
        Assert.Throws<ArgumentException>(() => series.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Remove(Id("b")), ChartObservationChange<ChartPoint>.Remove(Id("b"))
        }));
        Assert.Equal(new[] { "b", "c", "d" }, Keys(series)); Assert.Equal(12, series.GetPointMarkers(0)!.Radius);
        series.ApplyPointChanges(new[] { ChartObservationChange<ChartPoint>.Remove(Id("b")) });
        Assert.Equal(new[] { "c", "d" }, Keys(series)); Assert.Equal(18, series.GetPointMarkers(0)!.Radius);
        Assert.Null(series.GetPointMarkers(1));
    }

    [Fact]
    public void PreparedStaticOutputDoesNotObserveLaterPointOrSeriesMarkerEdits() {
        var chart = Chart.Create().WithSize(640, 400).WithDataLabels(false).AddSeries(Series());
        var series = chart.Series[0]; series.ConfigurePointMarkers(1, marker => { marker.Radius = 13; marker.Fill = ChartColor.Black; });
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.ConfigurePointMarkers(1, marker => { marker.Radius = 30; marker.Fill = ChartColor.White; });
        series.ConfigureMarkers(marker => marker.Radius = 9); series.Points[1] = new ChartPoint(2, -30);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.Bubble)]
    public void OtherFamiliesRejectPointMarkerOverridesBeforeInvokingTheCallback(ChartSeriesKind kind) {
        var series = kind == ChartSeriesKind.Bubble
            ? ChartSeries.CreateBubble(new ChartSeriesId("other"), "Other", new[] { new ChartBubble(1, 2, 3) })
            : new ChartSeries(new ChartSeriesId("other"), "Other", kind, new[] { new ChartPoint(1, 2) });
        var invoked = false;
        Assert.Throws<NotSupportedException>(() => series.ConfigurePointMarkers(0, marker => invoked = true));
        Assert.False(invoked); Assert.Throws<NotSupportedException>(() => series.GetPointMarkers(0));
        Assert.Throws<NotSupportedException>(() => series.UseSeriesMarkers(0));
    }

    [Fact]
    public void InvalidOrdinalsAndNullCallbacksLeaveExistingAuthoredMarkersUntouched() {
        var series = Series().ConfigurePointMarkers(1, marker => marker.Radius = 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ConfigurePointMarkers(-1, marker => marker.Radius = 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.GetPointMarkers(series.Points.Count));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.UseSeriesMarkers(series.Points.Count));
        Assert.Throws<ArgumentNullException>(() => series.ConfigurePointMarkers(1, null!));
        Assert.Equal(12, series.GetPointMarkers(1)!.Radius);
    }

    private static ChartSeries Series() => ChartSeries.CreateScatter(new ChartSeriesId("observations"), "Observations",
        new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) })
        .WithObservationIds(new[] { Id("a"), Id("b"), Id("c") });
    private static ChartObservationId Id(string value) => new(value);
    private static string[] Keys(ChartSeries series) => series.ObservationIds.OrderBy(entry => entry.Key).Select(entry => entry.Value.Value).ToArray();
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static XElement Source(XDocument xml, string role, string source) => Assert.Single(xml.Descendants(),
        node => (string?)node.Attribute("data-cfx-role") == role && (string?)node.Attribute("data-cfx-source-id") == source);
}
