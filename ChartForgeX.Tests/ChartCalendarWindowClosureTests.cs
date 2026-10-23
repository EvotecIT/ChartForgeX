using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects calendar-band replay through the same public windows a native host transports.</summary>
public sealed class ChartCalendarWindowClosureTests {
    private static readonly DateTime January = Utc(2024, 1, 1);
    private static double Oa(DateTime instant) => ChartDateTime.ToOADate(instant);
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(800, 440)));

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CalendarGestureSurvivesTransportAndRepeatedWindowPreparationWithoutAddingBands(bool horizontal, bool reversed) {
        var chart = MonthlyChart(horizontal, reversed);
        var full = chart.Prepare(Context());
        var fullAxis = Axis(full, horizontal);
        var february = Oa(January.AddMonths(1).AddDays(9));
        var center = fullAxis.Project(february);
        var selected = Assert.IsType<ChartAxisWindow>(fullAxis.ConstrainViewport(center - 1, center + 1));
        Assert.Equal(Oa(January.AddMonths(1)), selected.Minimum);
        Assert.Equal(Oa(January.AddMonths(2)), selected.Maximum);
        var originalTransport = JsonSerializer.Serialize(selected);

        for (var round = 0; round < 6; round++) {
            // A durable host/bookmark round trip must retain every public boundary property.
            var replay = JsonSerializer.Deserialize<ChartAxisWindow>(JsonSerializer.Serialize(selected))!;
            Assert.Equal(originalTransport, JsonSerializer.Serialize(replay));
            var prepared = chart.Prepare(Context(), horizontal ? new ChartCartesianViewport(y: replay) : new ChartCartesianViewport(x: replay));
            var axis = Axis(prepared, horizontal);
            var lower = horizontal ? axis.PlotBounds.Top : axis.PlotBounds.Left;
            var upper = horizontal ? axis.PlotBounds.Bottom : axis.PlotBounds.Right;
            var extent = upper - lower;
            Assert.Equal((lower + upper) / 2, axis.Project(february), 8);
            Assert.Equal(extent, Math.Abs(axis.Project(Oa(January.AddMonths(2).AddDays(9))) - axis.Project(february)), 8);
            Assert.Equal(originalTransport, JsonSerializer.Serialize(axis.ConstrainViewport(replay)));
            selected = Assert.IsType<ChartAxisWindow>(axis.ConstrainViewport(lower, upper));
            Assert.Equal(originalTransport, JsonSerializer.Serialize(selected));
        }
        Assert.Equal(3, chart.Series[0].Points.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AuthoredInclusiveMaximumAtCalendarBoundaryStillIncludesThatMonthsObservation(bool horizontal) {
        var chart = MonthlyChart(horizontal, false);
        var maximum = Oa(January.AddMonths(1));
        var authored = new ChartAxisWindow(Oa(January.AddDays(4)), maximum);
        var full = chart.Prepare(Context());
        var confined = Assert.IsType<ChartAxisWindow>(Axis(full, horizontal).ConstrainViewport(authored));
        Assert.Equal(authored.Minimum, confined.Minimum);
        Assert.Equal(maximum, confined.Maximum);
        var prepared = chart.Prepare(Context(), horizontal ? new ChartCartesianViewport(y: confined) : new ChartCartesianViewport(x: confined));
        var axis = Axis(prepared, horizontal);
        var lower = horizontal ? axis.PlotBounds.Top : axis.PlotBounds.Left;
        var upper = horizontal ? axis.PlotBounds.Bottom : axis.PlotBounds.Right;
        Assert.InRange(axis.Project(maximum), lower, upper);
        Assert.Equal((upper - lower) / 2, Math.Abs(axis.Project(maximum) - axis.Project(Oa(January.AddDays(2)))), 8);
        Assert.Equal(maximum, axis.Maximum);
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, true)]
    public void RepeatedHourHalfHourBandsRemainDistinctWhenTheDomainEndsInsideEitherOccurrence(int utcHour, bool reversed) {
        var first = Utc(2024, 11, 3, utcHour, 15);
        var last = Utc(2024, 11, 3, utcHour, 45);
        var chart = DstChart(first, last);
        chart.Options.XAxis.Reversed = reversed;
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart);
        var axis = section.PreparedVisual.XProjection!;
        Assert.Equal(Oa(Utc(2024, 11, 3, utcHour)), axis.Unproject(axis.Project(Oa(first))), 8);
        Assert.Equal(Oa(Utc(2024, 11, 3, utcHour, 30)), axis.Unproject(axis.Project(Oa(last))), 8);
        Assert.NotEqual(axis.Project(Oa(first)), axis.Project(Oa(last)));
        Assert.Equal(new[] { Oa(Utc(2024, 11, 3, utcHour)), Oa(Utc(2024, 11, 3, utcHour, 30)), Oa(Utc(2024, 11, 3, utcHour + 1)) }, Boundaries(section.Html));
        Assert.Equal(new[] { Oa(first), Oa(last) }, chart.Series[0].Points.Select(point => point.X));
    }

    [Fact]
    public void BothRepeatedWallClockOccurrencesKeepUtcOrderingAndEqualCalendarBandPitch() {
        var instants = new[] { Utc(2024, 11, 3, 5, 15), Utc(2024, 11, 3, 5, 45), Utc(2024, 11, 3, 6, 15), Utc(2024, 11, 3, 6, 45) };
        var chart = DstChart(instants);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart);
        var axis = section.PreparedVisual.XProjection!;
        var positions = instants.Select(instant => axis.Project(Oa(instant))).ToArray();
        for (var index = 1; index < positions.Length; index++) Assert.Equal(positions[1] - positions[0], positions[index] - positions[index - 1], 8);
        Assert.Equal(Enumerable.Range(0, 5).Select(index => Oa(Utc(2024, 11, 3, 5).AddMinutes(30 * index))), Boundaries(section.Html));
        Assert.Equal(instants.Select(Oa), chart.Series[0].Points.Select(point => point.X));
    }

    [Fact]
    public void FallbackWindowStartingLaterInTheEarlyOccurrenceStillFindsTheNextRepeatedBoundary() {
        var first = Utc(2024, 11, 3, 5, 45);
        var last = Utc(2024, 11, 3, 6, 15);
        var chart = DstChart(first, last);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart);
        var axis = section.PreparedVisual.XProjection!;
        Assert.Equal(new[] { Oa(Utc(2024, 11, 3, 5, 30)), Oa(Utc(2024, 11, 3, 6)), Oa(Utc(2024, 11, 3, 6, 30)) }, Boundaries(section.Html));
        Assert.Equal(axis.PlotBounds.Width / 2, axis.Project(Oa(last)) - axis.Project(Oa(first)), 8);
        Assert.Equal(Oa(Utc(2024, 11, 3, 6)), axis.Unproject(axis.Project(Oa(last))), 8);
    }

    [Fact]
    public void SpringMissingLocalHalfHoursDoNotCreateEmptyBandsOrLoseEnclosingBoundaries() {
        var first = Utc(2024, 3, 10, 6, 45);
        var last = Utc(2024, 3, 10, 7, 15);
        var chart = DstChart(first, last);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart);
        var axis = section.PreparedVisual.XProjection!;
        Assert.Equal(new[] { Oa(Utc(2024, 3, 10, 6, 30)), Oa(Utc(2024, 3, 10, 7)), Oa(Utc(2024, 3, 10, 7, 30)) }, Boundaries(section.Html));
        Assert.Equal(Oa(Utc(2024, 3, 10, 6, 30)), axis.Unproject(axis.Project(Oa(first))), 8);
        Assert.Equal(Oa(Utc(2024, 3, 10, 7)), axis.Unproject(axis.Project(Oa(last))), 8);
        Assert.Equal(axis.PlotBounds.Width / 2, axis.Project(Oa(last)) - axis.Project(Oa(first)), 8);
    }

    [Fact]
    public void CalendarBoundaryEnumerationStillRejectsOverBudgetRepeatedHourSchedules() {
        var chart = DstChart(Utc(2024, 11, 2, 5), Utc(2024, 11, 4, 5));
        chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Second)));
        Assert.Contains("100,000", Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context())).Message);
        Assert.Equal(2, chart.Series[0].Points.Count);
    }

    private static Chart MonthlyChart(bool horizontal, bool reversed) {
        var points = new[] { new ChartPoint(Oa(January.AddDays(2)), 2), new ChartPoint(Oa(January.AddMonths(1).AddDays(9)), 4), new ChartPoint(Oa(January.AddMonths(2).AddDays(9)), 6) };
        var chart = Chart.Create().WithLegend(false);
        if (horizontal) chart.AddHorizontalBar("Monthly", points); else chart.AddLine("Monthly", points);
        if (!horizontal) chart.Series[0].Markers.Enabled = false;
        var axis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        axis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month))).Reversed = reversed;
        return chart;
    }

    private static ChartAxisProjection Axis(PreparedVisual prepared, bool horizontal) => horizontal ? prepared.YProjection! : prepared.XProjection!;
    private static Chart DstChart(params DateTime[] instants) {
        var chart = Chart.Create().WithLegend(false).AddLine("Observed instants", instants.Select((instant, index) => new ChartPoint(Oa(instant), index + 1)));
        chart.Series[0].Markers.Enabled = false;
        chart.Options.XAxis.WithTimeScale(DstZone()).WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Minute, 30)));
        return chart;
    }

    private static TimeZoneInfo DstZone() {
        var spring = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday);
        var autumn = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), TimeSpan.FromHours(1), spring, autumn);
        return TimeZoneInfo.CreateCustomTimeZone("CFX/CalendarClosure", TimeSpan.FromHours(-5), "Calendar closure", "Standard", "Daylight", new[] { rule });
    }

    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0) => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);
    private static double[] Boundaries(string html) {
        var encoded = System.Text.RegularExpressions.Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]*)\"").Groups[1].Value;
        using var metadata = JsonDocument.Parse(System.Net.WebUtility.HtmlDecode(encoded));
        return metadata.RootElement.GetProperty("xProjection").GetProperty("timeSpacing").GetProperty("instants")
            .EnumerateArray().Select(value => value.GetDouble()).ToArray();
    }
}
