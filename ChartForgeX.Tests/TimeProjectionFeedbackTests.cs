using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TimeProjectionFeedbackTests {
    public static IEnumerable<object[]> DiscontinuousBuckets() {
        yield return Case("epoch-hour", new DateTime(1899, 12, 29, 23, 30, 0), ChartTimeTickUnit.Hour);
        yield return Case("earlier-hour", new DateTime(1899, 12, 28, 23, 30, 0), ChartTimeTickUnit.Hour);
        yield return Case("millisecond-rollover", new DateTime(1899, 12, 28, 23, 59, 59, 999), ChartTimeTickUnit.Millisecond);
        yield return Case("second-rollover", new DateTime(1899, 12, 28, 23, 59, 59), ChartTimeTickUnit.Second);
        yield return Case("minute-rollover", new DateTime(1899, 12, 28, 23, 59, 0), ChartTimeTickUnit.Minute);
        yield return Case("midnight-day", new DateTime(1899, 12, 27), ChartTimeTickUnit.Day);
        yield return Case("fractional-day", new DateTime(1899, 12, 27, 12, 0, 0), ChartTimeTickUnit.Day);
        yield return Case("week", new DateTime(1899, 12, 20, 12, 0, 0), ChartTimeTickUnit.Week);
        yield return Case("month", new DateTime(1899, 11, 28, 12, 0, 0), ChartTimeTickUnit.Month);
        yield return Case("positive-offset-day", new DateTime(1899, 12, 28, 12, 0, 0), ChartTimeTickUnit.Day, 14);
        yield return Case("negative-offset-day", new DateTime(1899, 12, 29, 6, 0, 0), ChartTimeTickUnit.Day, -12);
    }

    [Theory]
    [MemberData(nameof(DiscontinuousBuckets))]
    public void CalendarBucketsRejectDiscontinuousNegativeOleIntervals(string caseName, double start, ChartTimeTickUnit unit, int offsetHours) {
        var zone = TimeZoneInfo.CreateCustomTimeZone(caseName, TimeSpan.FromHours(offsetHours), caseName, caseName);
        var chart = BucketChart(start, new ChartTimeTickInterval(unit)).WithXAxisTimeScale(zone);
        var error = Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Contains("negative OLE date must remain within one UTC day", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-.5)]
    [InlineData(-.000001)]
    public void CalendarBucketsRejectNegativeEpochDayAliases(double start) {
        var error = Assert.Throws<InvalidOperationException>(() => BucketChart(start, new ChartTimeTickInterval(ChartTimeTickUnit.Hour)).ToSvg());
        Assert.Contains("epoch-day alias", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ChartTimeTickUnit.Millisecond, 1)]
    [InlineData(ChartTimeTickUnit.Second, 1)]
    [InlineData(ChartTimeTickUnit.Minute, 1)]
    [InlineData(ChartTimeTickUnit.Hour, 1)]
    public void NegativeSameDayBucketsKeepTheirExactDescendingBoundary(ChartTimeTickUnit unit, int count) {
        var start = new DateTime(1899, 12, 29, 6, 0, 0, DateTimeKind.Utc);
        var end = unit switch {
            ChartTimeTickUnit.Millisecond => start.AddMilliseconds(count),
            ChartTimeTickUnit.Second => start.AddSeconds(count),
            ChartTimeTickUnit.Minute => start.AddMinutes(count),
            _ => start.AddHours(count)
        };
        var rawStart = start.ToOADate();
        var boundary = end.ToOADate();
        Assert.Equal(boundary, ChartTimeScale.BucketEnd(new ChartAxis { Scale = ChartScaleKind.Time }, rawStart, new ChartTimeTickInterval(unit, count)));
        Assert.True(boundary < rawStart);
        var mark = XDocument.Parse(BucketChart(rawStart, new ChartTimeTickInterval(unit, count)).ToSvg()).Descendants()
            .Single(node => (string?)node.Attribute("data-cfx-observation-id") == "first");
        Assert.Equal(rawStart, Number(mark, "data-cfx-range-start"));
        Assert.Equal(Math.BitIncrement(boundary), Number(mark, "data-cfx-range-end"));
        Assert.Null(mark.Attribute("data-cfx-range-end-exclusive"));
        Assert.Null(mark.Attribute("data-cfx-range-start-instant"));
    }

    [Fact]
    public void PositiveCalendarBucketsKeepLocalDayAndMonthArithmetic() {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var axis = new ChartAxis { Scale = ChartScaleKind.Time, TimeZone = zone };
        var start = new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        var nextDay = ChartTimeScale.BucketEnd(axis, start.ToOADate(), new ChartTimeTickInterval(ChartTimeTickUnit.Day));
        Assert.Equal(start.AddHours(23).ToOADate(), nextDay);
        var nextMonth = ChartTimeScale.BucketEnd(axis, start.ToOADate(), new ChartTimeTickInterval(ChartTimeTickUnit.Month));
        Assert.Equal(new DateTime(2026, 4, 28, 11, 0, 0, DateTimeKind.Utc).ToOADate(), nextMonth);
    }

    [Theory]
    [InlineData(ChartScaleKind.Linear, false)]
    [InlineData(ChartScaleKind.Linear, true)]
    [InlineData(ChartScaleKind.Time, true)]
    [InlineData(ChartScaleKind.Logarithmic, true)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, true)]
    public void LinearOnlyProjectionPreservesTheNativeInverseDirection(ChartScaleKind scale, bool reversed) {
        var plot = new ChartRect(20, 30, 400, 200);
        var axis = new ChartAxis { Scale = scale, Reversed = reversed };
        var captured = Assert.IsType<ChartDataProjection>(ChartDataProjection.TryCreate(plot, 1, 11, axis, linearOnly: true));
        var inverse = new ChartAxisProjection(1, 11, captured.X.Minimum, captured.X.Maximum, captured.Plot, new ChartAxis { Reversed = captured.X.Reversed }, ChartAxisDimension.X);
        var nativeAxis = new ChartAxis { Reversed = reversed };
        foreach (var value in new[] { 2d, 7d, 10d }) {
            var pixel = plot.Left + ChartScaleTransform.Normalize(value, 1, 11, nativeAxis) * plot.Width;
            Assert.Equal(value, inverse.Unproject(pixel), 10);
        }
        Assert.Equal(reversed ? 9 : 3, inverse.Unproject(plot.Left + plot.Width * .2), 10);
        Assert.Equal(scale == ChartScaleKind.Time ? ChartScaleKind.Time : ChartScaleKind.Linear, captured.X.Scale);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HorizontalProjectionInvertsTheActualCategoryMarkCenters(bool reversed) {
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(0, 10).WithYAxisBounds(0, 4)
            .AddHorizontalBar("Readings", new[] { new ChartPoint(1, 3), new ChartPoint(2, 7), new ChartPoint(3, 5) });
        chart.Options.YAxis.Reversed = reversed;
        var prepared = chart.Prepare(new VisualRenderContext());
        var captured = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        var category = Assert.IsType<ChartDataAxisProjection>(captured.Y);
        foreach (var region in prepared.Regions.Where(region => region.Role == "point")) {
            var index = int.Parse(region.Id.Substring(region.Id.LastIndexOf('-') + 1), CultureInfo.InvariantCulture);
            var ratio = (captured.Plot.Bottom - (region.Bounds.Top + region.Bounds.Height / 2)) / captured.Plot.Height;
            var value = category.Minimum + (category.Reversed ? 1 - ratio : ratio) * (category.Maximum - category.Minimum);
            Assert.Equal(chart.Series[0].Points[index].X, value, 8);
        }
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "point"));
    }

    private static Chart BucketChart(double start, ChartTimeTickInterval interval) => Chart.Create().WithSize(640, 360).WithXAxisTimeScale()
        .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("bucket"), "Bucket", new[] { new ChartPoint(start, 5) })
            .WithTimeBuckets(interval).WithObservationIds(new[] { new ChartObservationId("first") }));
    private static double Number(XElement node, string name) => double.Parse(node.Attribute(name)!.Value, CultureInfo.InvariantCulture);
    private static object[] Case(string name, DateTime start, ChartTimeTickUnit unit, int offsetHours = 0) => new object[] { name, start.ToOADate(), unit, offsetHours };
}
