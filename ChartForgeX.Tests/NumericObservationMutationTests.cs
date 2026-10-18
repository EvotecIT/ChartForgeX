using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects rolling numeric updates from moving source identities or presentation onto different observations.</summary>
public sealed class NumericObservationMutationTests {
    [Fact]
    public void StructuralEditsMoveIdentityAndEveryPointOverrideWithTheOriginalObservation() {
        var series = ChartSeries.CreateScatter(new ChartSeriesId("orders"), "Orders", Points())
            .WithObservationIds(new[] { Id("a"), Id("b"), Id("c") })
            .WithPointColor(1, ChartColor.FromHex("#123456")).WithPointLabel(1, "Second")
            .WithPointFillPattern(1, ChartFillPattern.Crosshatch)
            .ConfigurePointDataLabelStyle(1, style => style.FontSize = 13);
        var chart = Chart.Create().WithDataLabels(false).AddSeries(series);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.Points.Insert(0, new ChartPoint(0, 5));
        Assert.Equal(4, series.SourcePointCount);
        Assert.Equal("b", series.ObservationIds[2].Value); Assert.Equal(2, series.Points[2].X);
        Assert.Null(series.PointLabels[0]); Assert.Equal("Second", series.PointLabels[2]);
        Assert.Equal(ChartColor.FromHex("#123456"), series.PointColors[2]);
        Assert.Equal(ChartFillPattern.Crosshatch, series.PointFillPatterns[2]);
        Assert.Equal(13, series.PointDataLabelStyles[2]!.FontSize);
        Assert.False(series.ObservationIds.ContainsKey(0));
        series.Points.RemoveAt(1);
        Assert.DoesNotContain(series.ObservationIds.Values, id => id.Value == "a");
        Assert.Equal("b", series.ObservationIds[1].Value); Assert.Equal("Second", series.PointLabels[1]);
        Assert.True(series.Points.Remove(new ChartPoint(3, 30)));
        Assert.False(series.Points.Remove(new ChartPoint(9, 90)));
        Assert.DoesNotContain(series.ObservationIds.Values, id => id.Value == "c");
        series.Points[1] = new ChartPoint(2, 25);
        Assert.Equal("b", series.ObservationIds[1].Value); Assert.Equal("Second", series.PointLabels[1]);
        Assert.Equal(2, series.SourcePointCount);
        var current = Prepare(chart).Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Role == "point"
            && node.Metadata.TryGetValue("data-cfx-observation-id", out var id) && id == "b");
        Assert.Equal("2", current.Metadata["data-cfx-x"]); Assert.Equal("25", current.Metadata["data-cfx-y"]);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        series.Points.Add(new ChartPoint(4, 40));
        Assert.Equal(3, series.SourcePointCount);
        series.Points.Clear();
        Assert.Equal(0, series.SourcePointCount); Assert.Equal(0, series.ObservationCount);
        Assert.Empty(series.ObservationIds); Assert.Empty(series.PointColors); Assert.Empty(series.PointLabels);
        Assert.Empty(series.PointFillPatterns); Assert.Empty(series.PointDataLabelStyles); Assert.Empty(series.PointSliceOffsets);
        series.Points.Add(new ChartPoint(8, 80));
        Assert.Equal(1, series.SourcePointCount); Assert.Empty(series.ObservationIds);
    }

    [Fact]
    public void PieOffsetsFollowTheirObservationAndInvalidEditsLeaveMetadataUntouched() {
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", ChartSeriesKind.Pie, Points())
            .WithObservationId(1, Id("second")).WithPointSliceOffset(1, .2);
        series.Points.Insert(0, new ChartPoint(0, 5));
        Assert.Equal(.2, series.PointSliceOffsets[2]); Assert.Equal(0, series.PointSliceOffsets[0]);
        series.Points.RemoveAt(0);
        Assert.Equal(.2, series.PointSliceOffsets[1]); Assert.Equal("second", series.ObservationIds[1].Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Points.Insert(-1, new ChartPoint(0, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Points.RemoveAt(30));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Points[30] = new ChartPoint(0, 1));
        Assert.Equal(3, series.SourcePointCount); Assert.Equal(.2, series.PointSliceOffsets[1]);
        Assert.Equal("second", series.ObservationIds[1].Value);
        Assert.NotEmpty(Prepare(Chart.Create().AddSeries(series)).ToPng());
    }

    [Theory]
    [InlineData("decimated")]
    [InlineData("unreduced")]
    [InlineData("histogram")]
    [InlineData("regression")]
    public void DerivedNumericViewsRejectEveryMutationBeforeChangingSourceProvenance(string family) {
        var source = Enumerable.Range(0, 20).Select(index => new ChartPoint(index, index + 1)).ToArray();
        var series = family switch {
            "decimated" => ChartSeries.CreateDecimated(new ChartSeriesId("derived"), "Derived", ChartSeriesKind.Line, source, 5),
            "unreduced" => ChartSeries.CreateDecimated(new ChartSeriesId("derived"), "Derived", ChartSeriesKind.Line, source, 30),
            "histogram" => ChartSeries.CreateHistogram(new ChartSeriesId("derived"), "Derived", source, ChartHistogramBinLayout.FromCount(0, 20, 4), ChartHistogramAggregation.Sum),
            _ => ChartSeries.CreateTrendLine(new ChartSeriesId("derived"), "Derived", source)
        };
        series.WithObservationId(10, Id("raw-eleven"));
        var before = series.Points.ToArray(); var indices = series.SourcePointIndices.ToArray();
        Assert.True(series.Points.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => series.Points.Add(new ChartPoint(30, 30)));
        Assert.Throws<NotSupportedException>(() => series.Points.Insert(0, new ChartPoint(30, 30)));
        Assert.Throws<NotSupportedException>(() => series.Points[0] = new ChartPoint(30, 30));
        Assert.Throws<NotSupportedException>(() => series.Points.RemoveAt(0));
        Assert.Throws<NotSupportedException>(() => series.Points.Remove(before[0]));
        Assert.Throws<NotSupportedException>(() => series.Points.Clear());
        Assert.Equal(before, series.Points); Assert.Equal(indices, series.SourcePointIndices);
        Assert.Equal(source.Length, series.SourcePointCount); Assert.Equal("raw-eleven", series.ObservationIds[10].Value);
        Assert.NotEmpty(Prepare(Chart.Create().AddSeries(series)).ToPng());
    }

    [Fact]
    public void RetainedNumericViewCannotModifyASeriesAfterItSwitchesToTypedWaterfall() {
        var series = new ChartSeries(new ChartSeriesId("balance"), "Balance", ChartSeriesKind.Waterfall, Points());
        var retained = series.Points;
        series.ReplaceWaterfallItems(new[] { ChartWaterfallItem.Delta(1, 4) });
        Assert.True(retained.IsReadOnly); Assert.Empty(retained);
        Assert.Throws<NotSupportedException>(() => retained.Add(new ChartPoint(2, 9)));
        Assert.Throws<NotSupportedException>(() => retained.Clear());
        Assert.Single(series.WaterfallItems); Assert.Equal(1, series.SourcePointCount);
    }

    private static ChartObservationId Id(string value) => new(value);
    private static ChartPoint[] Points() => new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) };
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
