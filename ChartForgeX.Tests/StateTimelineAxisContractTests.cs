using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StateTimelineAxisContractTests {
    private static readonly DateTime Day = new(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc);
    private static Chart Create() => Chart.Create().WithSize(720, 300)
        .WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromHex("#1d8a52")))
        .AddStateTimelineLane("Service", new[] { new ChartStateTimelineSegment(Day, Day.AddHours(6), "up") });

    [Fact]
    public void ExplicitOffGridLabelDefinesTickAndHighlight() {
        var value = Day.AddHours(3).AddMinutes(15).ToOADate();
        var chart = Create().WithXLabels(new[] { new ChartAxisLabel(value, "Maintenance") })
            .WithHighlightedXAxisLabel(value, ChartColor.FromHex("#e01234"));
        var model = ChartStateTimelineModel.Build(chart);
        Assert.Equal(new[] { value }, model.Ticks);
        var svg = XDocument.Parse(chart.ToSvg());
        var label = svg.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "state-timeline-tick-label");
        Assert.Equal("Maintenance", label.Value);
        Assert.Equal("#E01234", (string?)label.Attribute("fill"));
        Assert.NotEmpty(chart.ToPng());
    }

    [Fact]
    public void LegendBudgetIgnoresConfiguredPosition() {
        var chart = Create();
        var categories = Enumerable.Range(0, 12).Select(i => new ChartStateCategory("s" + i, "Long category " + i, ChartColor.FromHex("#1d8a52"))).ToArray();
        chart.WithStateCategories(categories).WithLegendBudget(0.35);
        chart.WithLegendPosition(ChartLegendPosition.Bottom);
        var expected = ChartStateTimelineModel.Build(chart).LayoutLegend(text => text.Length * 8, 0, 200, 115).Select(x => (x.Row, x.Omitted)).ToArray();
        chart.WithLegendPosition(ChartLegendPosition.Left);
        Assert.Equal(expected, ChartStateTimelineModel.Build(chart).LayoutLegend(text => text.Length * 8, 0, 200, 115).Select(x => (x.Row, x.Omitted)).ToArray());
    }

    [Theory]
    [InlineData(6, 7)]
    [InlineData(23, 25)]
    public void DateTimeBeforeOaEpochIsRejectedClearly(int startHour, int endHour) {
        var epoch = new DateTime(1899, 12, 29, 0, 0, 0, DateTimeKind.Utc);
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new ChartStateTimelineSegment(epoch.AddHours(startHour), epoch.AddHours(endHour), "up"));
        Assert.Contains("1899-12-30", error.Message);
    }

    [Fact]
    public void RepeatedLocalHourMetadataIncludesDistinctOffsets() {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var start = new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithSize(720, 300).WithXAxisTimeScale(zone)
            .AddStateTimelineLane("Service", new[] { new ChartStateTimelineSegment(start, start.AddHours(1), "up") });
        var segment = XDocument.Parse(chart.ToSvg()).Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "state-segment");
        Assert.Contains("+02:00", (string?)segment.Attribute("data-cfx-start"));
        Assert.Contains("+01:00", (string?)segment.Attribute("data-cfx-end"));
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic)]
    public void NonlinearTimeAxisIsRejected(ChartScaleKind scale) {
        var chart = Create().ConfigureXAxis(axis => axis.Scale = scale);
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Throws<InvalidOperationException>(() => chart.ToPng());
    }

    [Theory]
    [InlineData(-657434, -657433)]
    [InlineData(3000000, 3000000.01)]
    public void NumericFallbackMetadataPreservesEndpoints(double start, double end) {
        var chart = Chart.Create().WithSize(720, 300).AddStateTimelineLane("Service", new[] { new ChartStateTimelineSegment(start, end, "up") });
        var segment = XDocument.Parse(chart.ToSvg()).Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "state-segment");
        Assert.Equal(start.ToString("G17", System.Globalization.CultureInfo.InvariantCulture), (string?)segment.Attribute("data-cfx-start"));
        Assert.Equal(end.ToString("G17", System.Globalization.CultureInfo.InvariantCulture), (string?)segment.Attribute("data-cfx-end"));
        Assert.NotEmpty(chart.ToPng());
    }
}


