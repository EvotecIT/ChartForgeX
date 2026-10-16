using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartLinkedSourceContractsTests {
    [Fact]
    public void BulkObservationIdsAreAtomicAndUseOriginalHistogramSourceOrder() {
        var series = ChartSeries.CreateHistogram(new ChartSeriesId("readings"), "Readings", new[] { .2, .4, 1.5 },
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1, 2 }));
        var ids = new[] { new ChartObservationId("north"), new ChartObservationId("south"), new ChartObservationId("west") };
        Assert.Same(series, series.WithObservationIds(ids));
        ids[0] = new ChartObservationId("changed-input");
        Assert.Equal(new[] { "north", "south", "west" }, series.ObservationIds.OrderBy(item => item.Key).Select(item => item.Value.Value));
        Assert.Throws<ArgumentException>(() => series.WithObservationIds(new[] { new ChartObservationId("one") }));
        Assert.Throws<ArgumentException>(() => series.WithObservationIds(new[] { new ChartObservationId("one"), new ChartObservationId("one"), new ChartObservationId("three") }));
        Assert.Throws<InvalidOperationException>(() => series.WithObservationIds(FailingIds()));
        Assert.Equal("north", series.ObservationIds[0].Value);
        // Index two is a valid original sample even though only two bins are drawn.
        series.WithObservationId(2, new ChartObservationId("east"));
        Assert.Equal("east", series.ObservationIds[2].Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.WithObservationId(3, new ChartObservationId("outside")));
        static IEnumerable<ChartObservationId> FailingIds() {
            yield return new ChartObservationId("partial");
            throw new InvalidOperationException("Source enumeration failed.");
        }
    }

    [Fact]
    public void TimeBucketsCaptureCalendarRolloverAndRemainDetachedFromLaterSeriesChanges() {
        var start = new DateTime(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);
        var series = ChartSeries.CreateBar(new ChartSeriesId("months"), "Months", new[] { new ChartPoint(start.ToOADate(), 5) })
            .WithTimeBuckets(new ChartTimeTickInterval(ChartTimeTickUnit.Month));
        var chart = Chart.Create().AddSeries(series);
        chart.Options.XAxis.Scale = ChartScaleKind.Time;
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = prepared.ToSvg();
        var mark = XDocument.Parse(svg).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point");
        Assert.Equal(start.ToOADate(), double.Parse(mark.Attribute("data-cfx-range-start")!.Value, CultureInfo.InvariantCulture));
        Assert.Equal(start.AddMonths(1).ToOADate(), double.Parse(mark.Attribute("data-cfx-range-end")!.Value, CultureInfo.InvariantCulture));
        Assert.Equal("time", mark.Attribute("data-cfx-range-scale")!.Value);
        Assert.Equal("true", mark.Attribute("data-cfx-range-end-exclusive")!.Value);
        series.WithTimeBuckets(new ChartTimeTickInterval(ChartTimeTickUnit.Week, 2));
        Assert.Equal(svg, prepared.ToSvg());
        Assert.NotEqual(svg, chart.Prepare(new VisualRenderContext()).ToSvg());
        series.WithTimeBuckets(null);
        Assert.DoesNotContain("data-cfx-range-start", chart.Prepare(new VisualRenderContext()).ToSvg());
    }

    [Fact]
    public void CalendarDayBucketsFollowTheAxisZoneAcrossDaylightSaving() {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var start = new DateTime(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc);
        var series = ChartSeries.CreateBar(new ChartSeriesId("days"), "Days", new[] { new ChartPoint(start.ToOADate(), 5) })
            .WithTimeBuckets(new ChartTimeTickInterval(ChartTimeTickUnit.Day));
        var chart = Chart.Create().AddSeries(series);
        chart.Options.XAxis.Scale = ChartScaleKind.Time; chart.Options.XAxis.TimeZone = zone;
        var mark = XDocument.Parse(chart.Prepare(new VisualRenderContext()).ToSvg()).Descendants()
            .Single(node => (string?)node.Attribute("data-cfx-role") == "point");
        var end = double.Parse(mark.Attribute("data-cfx-range-end")!.Value, CultureInfo.InvariantCulture);
        Assert.Equal(start.AddHours(23).ToOADate(), end);
    }

    [Fact]
    public void TimeBucketsPreserveAuthoredSubmillisecondInstantsWhenReplacingValues() {
        var start = new DateTime(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc).AddTicks(1_000);
        var series = ChartSeries.CreateBar(new ChartSeriesId("precise"), "Precise", new[] { new ChartPoint(start, 5) });
        var chart = Chart.Create().WithXAxisTimeScale().AddSeries(series);
        foreach (var unit in new[] { ChartTimeTickUnit.Hour, ChartTimeTickUnit.Month }) {
            var interval = new ChartTimeTickInterval(unit);
            series.WithTimeBuckets(interval);
            series.Points[0] = new ChartPoint(start, 7);
            Assert.Same(interval, series.TimeBucketInterval);
            var mark = XDocument.Parse(chart.Prepare(new VisualRenderContext()).ToSvg()).Descendants()
                .Single(node => (string?)node.Attribute("data-cfx-role") == "point");
            var expected = unit == ChartTimeTickUnit.Hour ? start.AddHours(1) : start.AddMonths(1);
            Assert.Equal(start.ToString("O", CultureInfo.InvariantCulture), mark.Attribute("data-cfx-range-start-instant")!.Value);
            Assert.Equal(expected.ToString("O", CultureInfo.InvariantCulture), mark.Attribute("data-cfx-range-end-instant")!.Value);
        }
    }

    [Fact]
    public void TimeBucketDeclarationsRejectUnsupportedSeriesAndNumericAxes() {
        Assert.Throws<InvalidOperationException>(() => ChartSeries.CreateLine(new ChartSeriesId("line"), "Line", new[] { new ChartPoint(1, 2) })
            .WithTimeBuckets(new ChartTimeTickInterval(ChartTimeTickUnit.Day)));
        var series = ChartSeries.CreateBar(new ChartSeriesId("bars"), "Bars", new[] { new ChartPoint(1, 2) })
            .WithTimeBuckets(new ChartTimeTickInterval(ChartTimeTickUnit.Day));
        Assert.Throws<InvalidOperationException>(() => Chart.Create().AddSeries(series).Prepare(new VisualRenderContext()));
    }

    [Fact]
    public void BrushProjectionCapturesFinalScaleWithoutExpandingNativeViewportSupport() {
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(1, 100).WithYAxisBounds(-10, 10)
            .AddBar("Readings", new[] { new ChartPoint(1, -5), new ChartPoint(100, 5) });
        chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        chart.Options.YAxis.Scale = ChartScaleKind.SymmetricLogarithmic;
        chart.Options.YAxis.Reversed = true;
        var prepared = chart.Prepare(new VisualRenderContext());
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        Assert.Null(prepared.XProjection);
        Assert.Equal(Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds, projection.Plot);
        Assert.Equal(0, projection.X.TransformedMinimum);
        Assert.Equal(2, projection.X.TransformedMaximum);
        var y = Assert.IsType<ChartDataAxisProjection>(projection.Y);
        Assert.True(y.Reversed);
        chart.Options.YAxis.Reversed = false; chart.Options.XAxis.Maximum = 1000;
        Assert.Equal(100, projection.X.Maximum);
        Assert.True(y.Reversed);
    }
}
