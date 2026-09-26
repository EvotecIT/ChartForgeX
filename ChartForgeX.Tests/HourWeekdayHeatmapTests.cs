using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HourWeekdayHeatmapTests {
    // 2026-09-21 is a Monday.
    private static readonly DateTime Monday = new(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AddHourWeekdayHeatmap_Count_BucketsByWeekdayAndHourWithZeroForEmpty() {
        var chart = Chart.Create().AddHourWeekdayHeatmap(new[] {
            new ChartTimedValue(Monday.AddHours(9).AddMinutes(5)),
            new ChartTimedValue(Monday.AddHours(9).AddMinutes(55)),
            new ChartTimedValue(Monday.AddDays(7).AddHours(9)),
            new ChartTimedValue(Monday.AddDays(6).AddHours(23).AddMinutes(59))
        });
        Assert.Equal(new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" }, chart.Series.Select(series => series.Name).ToArray());
        Assert.All(chart.Series, series => Assert.Equal(24, series.Points.Count));
        Assert.Equal(3, Cell(chart, "Mon", 9));
        Assert.Equal(0, Cell(chart, "Mon", 10));
        Assert.Equal(1, Cell(chart, "Sun", 23));
        Assert.Equal("00", chart.Options.XAxisLabels[0].Text);
        Assert.Equal("23", chart.Options.XAxisLabels[23].Text);
        Assert.Equal("Hour of day (UTC)", chart.XAxisTitle);
        Assert.True(chart.Options.HeatmapRelativeScale);
        Assert.All(chart.Series, series => Assert.False(series.ShowInLegend));
    }

    [Fact]
    public void AddHourWeekdayHeatmap_MeanAndMaximum_MaskEmptyBucketsAndKeepEmptyRows() {
        var values = new[] { new ChartTimedValue(Monday.AddHours(2), 10), new ChartTimedValue(Monday.AddHours(2).AddMinutes(30), 30) };
        var mean = Chart.Create().WithSize(720, 320).AddHourWeekdayHeatmap(values, ChartTimeAggregation.Mean);
        Assert.Equal(20, Cell(mean, "Mon", 2));
        Assert.Single(mean.Series[0].Points);
        Assert.Empty(mean.Series[1].Points);
        var svg = XDocument.Parse(mean.ToSvg());
        Assert.Single(ByRole(svg, "heatmap-cell"));
        Assert.Equal(7, ByRole(svg, "heatmap-row-label").Length);
        Assert.True(mean.ToPng().Length > 200);

        var maximum = Chart.Create().AddHourWeekdayHeatmap(values, ChartTimeAggregation.Maximum);
        Assert.Equal(30, Cell(maximum, "Mon", 2));
        var sum = Chart.Create().AddHourWeekdayHeatmap(values, ChartTimeAggregation.Sum);
        Assert.Equal(40, Cell(sum, "Mon", 2));
    }

    [Fact]
    public void AddHourWeekdayHeatmap_TimeZoneAndFirstDay_ShiftBuckets() {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/Plus2", TimeSpan.FromHours(2), "Test +02", "Test +02");
        var sundayLate = new ChartTimedValue(Monday.AddHours(-1).AddMinutes(-30));
        var chart = Chart.Create().AddHourWeekdayHeatmap(new[] { sundayLate }, timeZone: zone, firstDayOfWeek: DayOfWeek.Sunday);
        Assert.Equal("Sun", chart.Series[0].Name);
        Assert.Equal(1, Cell(chart, "Mon", 0));
        Assert.Equal(0, Cell(chart, "Sun", 22));
        Assert.Equal("Hour of day (Test/Plus2)", chart.XAxisTitle);

        var titled = Chart.Create().WithXAxis("Local hour").AddHourWeekdayHeatmap(new[] { sundayLate });
        Assert.Equal("Local hour", titled.XAxisTitle);

        var localized = Chart.Create().WithLabels(labels => labels.HourOfDay = "Godzina").AddHourWeekdayHeatmap(new[] { sundayLate });
        Assert.Equal("Godzina (UTC)", localized.XAxisTitle);
    }

    [Fact]
    public void Render_RelativeScale_ColoursCountsByObservedRangeInSvgAndPng() {
        var values = Enumerable.Range(0, 12).Select(index => new ChartTimedValue(Monday.AddHours(10))).Append(new ChartTimedValue(Monday.AddHours(11)));
        var chart = Chart.Create().WithSize(900, 360).AddHourWeekdayHeatmap(values);
        var svg = XDocument.Parse(chart.ToSvg());
        var cells = ByRole(svg, "heatmap-cell");
        var busiest = cells.Single(cell => ((string)cell.Attribute("aria-label")!).StartsWith("Mon, 10:", StringComparison.Ordinal));
        var quiet = cells.Single(cell => ((string)cell.Attribute("aria-label")!).StartsWith("Mon, 11:", StringComparison.Ordinal));
        Assert.Equal("positive", (string)busiest.Attribute("data-cfx-status")!);
        Assert.NotEqual((string)busiest.Attribute("fill")!, (string)quiet.Attribute("fill")!);

        var percent = Chart.Create().WithSize(900, 360).AddHourWeekdayHeatmap(values);
        percent.Options.HeatmapRelativeScale = false;
        var percentBusiest = ByRole(XDocument.Parse(percent.ToSvg()), "heatmap-cell").Single(cell => ((string)cell.Attribute("aria-label")!).StartsWith("Mon, 10:", StringComparison.Ordinal));
        Assert.Equal("negative", (string)percentBusiest.Attribute("data-cfx-status")!);

        Assert.Equal(chart.ToPng(), Chart.Create().WithSize(900, 360).AddHourWeekdayHeatmap(values).ToPng());
        Assert.NotEqual(chart.ToPng(), percent.ToPng());
        Assert.True(PngReader.Decode(chart.ToPng()).Width > 0);
    }

    [Fact]
    public void Validation_RejectsEmptyInputAndNonFiniteValues() {
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHourWeekdayHeatmap(Array.Empty<ChartTimedValue>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartTimedValue(Monday, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create().AddHourWeekdayHeatmap(new[] { new ChartTimedValue(Monday) }, (ChartTimeAggregation)9));
        var local = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Local);
        Assert.Equal(local.ToUniversalTime(), new ChartTimedValue(local).Timestamp);
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHeatmapRow("Only", new double?[] { null, null }));
    }

    [Fact]
    public void AddHourWeekdayHeatmap_DaylightSavingZone_BucketsByWallClock() {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2000, 1, 1), new DateTime(2099, 12, 31), TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday));
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/Central", TimeSpan.FromHours(1), "Test Central", "TST", "TDT", new[] { rule });
        var fallBack = new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc);
        var springForward = new DateTime(2026, 3, 29, 0, 30, 0, DateTimeKind.Utc);
        var chart = Chart.Create().AddHourWeekdayHeatmap(new[] {
            new ChartTimedValue(fallBack), new ChartTimedValue(fallBack.AddHours(1)),
            new ChartTimedValue(springForward), new ChartTimedValue(springForward.AddHours(1))
        }, timeZone: zone, timeZoneLabel: "CET");
        Assert.Equal(2, Cell(chart, "Sun", 2));
        Assert.Equal(1, Cell(chart, "Sun", 1));
        Assert.Equal(1, Cell(chart, "Sun", 3));
        Assert.Equal("Hour of day (CET)", chart.XAxisTitle);
    }

    [Fact]
    public void AddHourWeekdayHeatmap_OwnsTheChartAndAcceptsLocalizedDayNames() {
        var values = new[] { new ChartTimedValue(new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Unspecified)) };
        var names = new[] { "Nd", "Pn", "Wt", "Śr", "Cz", "Pt", "So" };
        var chart = Chart.Create().AddHourWeekdayHeatmap(values, dayNames: names);
        Assert.Equal(new[] { "Pn", "Wt", "Śr", "Cz", "Pt", "So", "Nd" }, chart.Series.Select(series => series.Name).ToArray());
        Assert.Equal(1, Cell(chart, "Pn", 9));
        Assert.Throws<InvalidOperationException>(() => chart.AddHourWeekdayHeatmap(values));
        Assert.Throws<InvalidOperationException>(() => Chart.Create().AddHeatmapRow("Row", new[] { 1d }).AddHourWeekdayHeatmap(values));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHourWeekdayHeatmap(values, dayNames: new[] { "a" }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create().AddHourWeekdayHeatmap(values, firstDayOfWeek: (DayOfWeek)9));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHourWeekdayHeatmap(new[] { new ChartTimedValue(Monday, double.MaxValue), new ChartTimedValue(Monday, double.MaxValue) }, ChartTimeAggregation.Sum));
    }

    private static double Cell(Chart chart, string day, int hour) {
        var series = chart.Series.Single(item => item.Name == day);
        return series.Points.Single(point => Math.Abs(point.X - (hour + 1)) < 0.5).Y;
    }

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
}
