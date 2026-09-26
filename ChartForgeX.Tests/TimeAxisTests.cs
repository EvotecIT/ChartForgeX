using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TimeAxisTests {
    private static readonly DateTime Day = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GenerateInside_UtcDay_UsesSixHourTicksWithDateAtMidnight() {
        var axis = new ChartAxis().WithTimeScale();
        var labels = Labels(axis, ChartTicks.GenerateInside(axis, Day.ToOADate(), Day.AddDays(1).ToOADate()));
        Assert.Equal(new[] { "2026-03-01", "06:00", "12:00", "18:00", "2026-03-02" }, labels);
    }

    [Fact]
    public void GenerateInside_OneHour_UsesMinuteTicks() {
        var axis = new ChartAxis().WithTimeScale();
        var start = Day.AddHours(9);
        var labels = Labels(axis, ChartTicks.GenerateInside(axis, start.AddMinutes(3).ToOADate(), start.AddHours(1).ToOADate()));
        Assert.Equal(new[] { "09:10", "09:20", "09:30", "09:40", "09:50", "10:00" }, labels);
    }

    [Fact]
    public void GenerateInside_TwoWeeks_UsesDayTicksRestartingEachMonth() {
        var axis = new ChartAxis().WithTimeScale();
        var labels = Labels(axis, ChartTicks.GenerateInside(axis, new DateTime(2026, 7, 25).ToOADate(), new DateTime(2026, 8, 6).ToOADate()));
        Assert.Equal(new[] { "2026-07-25", "2026-07-27", "2026-07-29", "2026-07-31", "2026-08-01", "2026-08-03", "2026-08-05" }, labels);
    }

    [Fact]
    public void GenerateInside_ThreeYears_UsesHalfYearTicks() {
        var axis = new ChartAxis().WithTimeScale();
        var labels = Labels(axis, ChartTicks.GenerateInside(axis, new DateTime(2025, 11, 20).ToOADate(), new DateTime(2028, 11, 20).ToOADate()));
        Assert.Equal(new[] { "2026-01-01", "2026-07-01", "2027-01-01", "2027-07-01", "2028-01-01", "2028-07-01" }, labels);
    }

    [Fact]
    public void Generate_PartialHours_ExpandsToAlignedBounds() {
        var axis = new ChartAxis().WithTimeScale();
        var min = Day.AddHours(1).AddMinutes(20).ToOADate();
        var max = Day.AddHours(23).AddMinutes(40).ToOADate();
        var ticks = ChartTicks.Generate(axis, min, max);
        Assert.True(ticks[0] <= min && ticks[ticks.Count - 1] >= max);
        Assert.Equal(new[] { "2026-03-01", "06:00", "12:00", "18:00", "2026-03-02" }, Labels(axis, ticks));
    }

    [Fact]
    public void GenerateInside_FixedOffsetZone_AlignsTicksToLocalWallClock() {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/Plus0530", TimeSpan.FromMinutes(330), "Test +05:30", "Test +05:30");
        var axis = new ChartAxis().WithTimeScale(zone);
        var ticks = ChartTicks.GenerateInside(axis, Day.ToOADate(), Day.AddDays(1).ToOADate());
        Assert.Equal(new[] { "06:00", "12:00", "18:00", "2026-03-02" }, Labels(axis, ticks));
        Assert.Equal(Day.AddMinutes(30).ToOADate(), ticks[0], 9);
    }

    [Fact]
    public void GenerateInside_DaylightSavingStart_SkipsMissingHourAndKeepsLocalAlignment() {
        var axis = new ChartAxis().WithTimeScale(CentralTestZone());
        var start = new DateTime(2026, 3, 28, 18, 0, 0);
        var ticks = ChartTicks.GenerateInside(axis, start.ToOADate(), start.AddDays(1).ToOADate());
        Assert.Equal(new[] { "2026-03-29", "06:00", "12:00", "18:00" }, Labels(axis, ticks));
        Assert.Equal(5.0 / 24.0, ticks[1] - ticks[0], 9);

        axis.TickCount = 25;
        var hourlyTicks = ChartTicks.GenerateInside(axis, start.ToOADate(), start.AddDays(1).ToOADate());
        var hourly = Labels(axis, hourlyTicks);
        Assert.DoesNotContain("02:00", hourly);
        Assert.Contains("03:00", hourly);
        Assert.Equal(1.0 / 24.0, hourlyTicks[Array.IndexOf(hourly, "03:00")] - hourlyTicks[Array.IndexOf(hourly, "01:00")], 9);
    }

    [Fact]
    public void GenerateInside_DaylightSavingEnd_TicksRepeatedHourAtEvenSpacing() {
        var axis = new ChartAxis().WithTimeScale(CentralTestZone());
        axis.TickCount = 13;
        var start = new DateTime(2026, 10, 24, 22, 0, 0);
        var ticks = ChartTicks.GenerateInside(axis, start.ToOADate(), start.AddHours(6).ToOADate());
        Assert.Equal(13, ticks.Count);
        for (var i = 1; i < ticks.Count; i++) Assert.Equal(1.0 / 48.0, ticks[i] - ticks[i - 1], 9);
        Assert.Equal(2, Labels(axis, ticks).Count(label => label == "02:30"));
    }

    [Fact]
    public void Generate_BoundInsideRepeatedHour_KeepsEvenQuarterHourSpacing() {
        var axis = new ChartAxis().WithTimeScale(CentralTestZone());
        axis.TickCount = 11;
        var inside = ChartTicks.GenerateInside(axis, new DateTime(2026, 10, 25, 0, 35, 0).ToOADate(), new DateTime(2026, 10, 25, 3, 0, 0).ToOADate());
        Assert.Equal(new DateTime(2026, 10, 25, 0, 45, 0).ToOADate(), inside[0], 9);
        Assert.Equal(10, inside.Count);
        for (var i = 1; i < inside.Count; i++) Assert.Equal(1.0 / 96.0, inside[i] - inside[i - 1], 9);

        var outer = ChartTicks.Generate(axis, new DateTime(2026, 10, 24, 22, 0, 0).ToOADate(), new DateTime(2026, 10, 25, 0, 35, 0).ToOADate());
        Assert.Equal(new DateTime(2026, 10, 25, 0, 45, 0).ToOADate(), outer[outer.Count - 1], 9);
        for (var i = 1; i < outer.Count; i++) Assert.Equal(1.0 / 96.0, outer[i] - outer[i - 1], 9);
    }

    [Fact]
    public void Generate_FloorInSkippedHour_StillCoversMinimum() {
        var axis = new ChartAxis().WithTimeScale(CentralTestZone());
        var min = new DateTime(2026, 3, 29, 1, 10, 0).ToOADate();
        var max = new DateTime(2026, 3, 29, 11, 10, 0).ToOADate();
        var ticks = ChartTicks.Generate(axis, min, max);
        Assert.True(ticks[0] <= min && ticks[ticks.Count - 1] >= max);
        Assert.DoesNotContain("02:00", Labels(axis, ticks));
    }

    [Theory]
    [InlineData(-657434, -657400, 6)]
    [InlineData(-500000, 500000, 3)]
    [InlineData(2958000, 2958465, 6)]
    public void Generate_NearDateLimits_DoesNotThrowAndStaysOrdered(double min, double max, int tickCount) {
        var axis = new ChartAxis().WithTimeScale(TimeZoneInfo.CreateCustomTimeZone("Test/Minus5", TimeSpan.FromHours(-5), "Test -05", "Test -05"));
        axis.TickCount = tickCount;
        foreach (var ticks in new[] { ChartTicks.Generate(axis, min, max), ChartTicks.GenerateInside(axis, min, max) }) {
            Assert.True(ticks.Count >= 2);
            for (var i = 1; i < ticks.Count; i++) Assert.True(ticks[i] > ticks[i - 1]);
        }

        var outer = ChartTicks.Generate(axis, min, max);
        Assert.True(outer[0] <= min && outer[outer.Count - 1] >= max);
    }

    [Fact]
    public void Format_UsesConfiguredTimeZoneAndSeconds() {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/Minus3", TimeSpan.FromHours(-3), "Test -03", "Test -03");
        var value = Day.AddHours(14).AddSeconds(30).ToOADate();
        Assert.Equal("14:00:30", ChartTimeScale.Format(new ChartAxis().WithTimeScale(), value));
        Assert.Equal("11:00:30", ChartTimeScale.Format(new ChartAxis().WithTimeScale(zone), value));
    }

    [Fact]
    public void Render_ShowTimeZone_DecoratesAxisTitleInSvgAndPng() {
        Chart Create(bool show, string? label = null) => Chart.Create().WithSize(640, 300).WithLegend(false)
            .WithXAxis("Observed")
            .WithXAxisTimeScale(null, show, label)
            .AddLine("Latency", HourlyPoints(24));

        Assert.Contains(">Observed (UTC)</text>", Create(true).ToSvg(), StringComparison.Ordinal);
        Assert.Contains(">Observed (Europe/Warsaw)</text>", Create(true, "Europe/Warsaw").ToSvg(), StringComparison.Ordinal);
        Assert.DoesNotContain("(UTC)", Create(false).ToSvg(), StringComparison.Ordinal);
        Assert.False(Create(true).ToPng().SequenceEqual(Create(false).ToPng()), "The PNG axis title should include the zone designator.");

        var untitled = Chart.Create().WithSize(640, 300).WithLegend(false).WithXAxisTimeScale(showTimeZone: true).AddLine("Latency", HourlyPoints(24));
        Assert.Contains(">UTC</text>", untitled.ToSvg(), StringComparison.Ordinal);

        var timeline = Chart.Create().WithSize(640, 300).WithXAxis("Window").WithXAxisTimeScale(showTimeZone: true)
            .AddTimelineItem("Maintenance", Day.AddHours(2), Day.AddHours(5));
        Assert.Contains(">Window (UTC)</text>", timeline.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void Render_TimeAxisWithBreakBefore_KeepsGapAndHourLabelsDeterministic() {
        var points = HourlyPoints(24).Select((point, index) => index == 12 ? new ChartPoint(point.X, point.Y, true) : point).ToArray();
        Chart Create() => Chart.Create().WithSize(720, 320).WithLegend(false).WithXAxisTimeScale().AddLine("Availability", points);

        var svgText = Create().ToSvg();
        var svg = XDocument.Parse(svgText);
        var lines = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "line").ToArray();
        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.Equal(2, ((string)line.Attribute("d")!).Count(character => character == 'M')));
        var labels = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "x-axis-label").Select(element => element.Value).ToArray();
        Assert.Contains("06:00", labels);
        Assert.All(labels, label => Assert.True(label.Length == 10 || label.EndsWith(":00", StringComparison.Ordinal), label));
        Assert.Equal(svgText, Create().ToSvg());
        var png = Create().ToPng();
        Assert.Equal(png, Create().ToPng());
        Assert.True(PngReader.Decode(png).Width > 0);
    }

    [Fact]
    public void Render_UnrepresentableTimeRange_FallsBackToNumericTicks() {
        var chart = Chart.Create().WithSize(480, 240).WithXAxisTimeScale()
            .AddLine("Out of range", new[] { new ChartPoint(3_000_000, 1), new ChartPoint(3_000_100, 2) });
        Assert.Contains("data-cfx-role=\"x-axis-label\"", chart.ToSvg(), StringComparison.Ordinal);
        Assert.True(chart.ToPng().Length > 200);
    }

    [Fact]
    public void Render_LabelFormatter_OverridesTimeDefaults() {
        var chart = Chart.Create().WithSize(640, 300).WithXAxisTimeScale()
            .AddLine("Latency", HourlyPoints(24));
        chart.Options.XAxis.LabelFormatter = value => "T" + DateTime.FromOADate(value).Hour.ToString(CultureInfo.InvariantCulture);
        var svg = chart.ToSvg();
        Assert.Contains(">T6</text>", svg, StringComparison.Ordinal);
        Assert.DoesNotContain(">06:00</text>", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void DateTimeInputs_LocalAndUnspecified_NormalizeToUtcInstants() {
        var utc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var local = utc.ToLocalTime();
        var unspecified = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
        Assert.Equal(utc.ToOADate(), new ChartPoint(local, 1).X, 9);
        Assert.Equal(utc.ToOADate(), new ChartPoint(unspecified, 1).X, 9);
        Assert.Equal(utc.ToOADate(), new ChartAxisLabel(local, "noon").Value, 9);
        Assert.Equal(utc.ToOADate(), new ChartRangeBand(local, 1, 2).X, 9);
    }

    [Fact]
    public void DateTimeInputs_DateBasedCharts_KeepWallClockDates() {
        var localDay = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Local);
        var wallClock = DateTime.SpecifyKind(localDay, DateTimeKind.Unspecified).ToOADate();
        var calendar = Chart.Create().AddCalendarHeatmap("Days", new[] { new ChartCalendarHeatmapItem(localDay, 3) });
        Assert.Equal(wallClock, calendar.Series[0].Points[0].X, 9);
        Assert.Equal(DateTimeKind.Unspecified, new ChartCalendarHeatmapItem(localDay, 1).Date.Kind);
        Assert.Equal(wallClock, Chart.Create().WithGanttToday(localDay).Options.GanttToday!.Value, 9);
        Assert.Equal(wallClock, Chart.Create().AddGanttTask("Task", localDay, localDay.AddDays(1)).Series[0].Points[0].X, 9);
    }

    [Fact]
    public void Render_MixedLocalAndUtcSeriesWithTimeZone_LineUpWithoutDoubleShift() {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test/Plus0530", TimeSpan.FromMinutes(330), "Test +05:30", "Test +05:30");
        var utcPoints = HourlyPoints(6);
        var localPoints = Enumerable.Range(0, 7).Select(hour => new ChartPoint(Day.AddHours(hour).ToLocalTime(), 10 + hour)).ToArray();
        Assert.Equal(utcPoints.Select(point => point.X), localPoints.Select(point => point.X));
        var chart = Chart.Create().WithSize(640, 300).WithXAxisTimeScale(zone).AddLine("UTC", utcPoints).AddLine("Local", localPoints);
        var labels = XDocument.Parse(chart.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "x-axis-label").Select(element => element.Value).ToArray();
        Assert.Contains("06:00", labels);
        Assert.DoesNotContain(labels, label => label.EndsWith(":30", StringComparison.Ordinal));
    }

    private static ChartPoint[] HourlyPoints(int count) =>
        Enumerable.Range(0, count + 1).Select(hour => new ChartPoint(Day.AddHours(hour), 40 + (hour * 7 % 11))).ToArray();

    private static string[] Labels(ChartAxis axis, IReadOnlyList<double> ticks) => ticks.Select(tick => ChartTimeScale.Format(axis, tick)).ToArray();

    private static TimeZoneInfo CentralTestZone() {
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2000, 1, 1),
            new DateTime(2099, 12, 31),
            TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday));
        return TimeZoneInfo.CreateCustomTimeZone("Test/Central", TimeSpan.FromHours(1), "Test Central", "TST", "TDT", new[] { rule });
    }
}
