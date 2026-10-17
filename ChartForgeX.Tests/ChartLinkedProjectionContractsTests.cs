using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartLinkedProjectionContractsTests {
    [Theory]
    [InlineData("primary")]
    [InlineData("secondary")]
    [InlineData("mixed")]
    [InlineData("empty-primary")]
    [InlineData("empty-secondary")]
    [InlineData("secondary-primary-y-line")]
    [InlineData("secondary-primary-y-region")]
    [InlineData("secondary-x-only")]
    public void PreparedBrushProjectionUsesOnlyObservedValueDomains(string domains) {
        var chart = ValueDomainsChart(domains);
        var prepared = chart.Prepare(new VisualRenderContext());
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        Assert.Equal(0, projection.X.Minimum); Assert.Equal(3, projection.X.Maximum);
        if (domains == "mixed" || domains.StartsWith("secondary-primary-y", StringComparison.Ordinal)) Assert.Null(projection.Y);
        else {
            var y = Assert.IsType<ChartDataAxisProjection>(projection.Y);
            var secondary = domains.StartsWith("secondary", StringComparison.Ordinal) || domains == "empty-primary";
            Assert.Equal(secondary ? 1000 : 0, y.Minimum);
            Assert.Equal(secondary ? 2000 : 10, y.Maximum);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FiniteAuthoredCoordinatesCannotPublishInvalidProjectionJson(bool schedule) {
        var chart = OverflowChart(schedule);
        var html = chart.ToInteractiveHtmlFragmentWithoutAssets();
        var match = Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"");
        Assert.True(match.Success);
        using var metadata = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
        Assert.False(metadata.RootElement.TryGetProperty("dataProjection", out _));
        Assert.NotEmpty(metadata.RootElement.GetProperty("regions").EnumerateArray());
    }

    [Fact]
    public void NativeNegativeOleCoordinatesKeepNumericIdentityMetadata() {
        var instant = new DateTime(1899, 12, 29, 12, 0, 0, DateTimeKind.Utc);
        var point = Chart.Create().WithXAxisTimeScale().AddLine("Earlier", new[] { new ChartPoint(instant, 5) });
        var mark = XDocument.Parse(point.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "point");
        Assert.Equal("-1.5", mark.Attribute("data-cfx-x")!.Value);
        Assert.Null(mark.Attribute("data-cfx-x-instant"));
        var range = Chart.Create().WithXAxisTimeScale().AddTimelineRange("Crossing", -1, 1);
        var interval = XDocument.Parse(range.ToSvg()).Descendants().Single(node => node.Attribute("data-cfx-range-start") != null);
        Assert.Equal("-1", interval.Attribute("data-cfx-range-start")!.Value);
        Assert.Equal("1", interval.Attribute("data-cfx-range-end")!.Value);
        Assert.Null(interval.Attribute("data-cfx-range-start-instant"));
    }

    [Fact]
    public void APreparedLegacyTimeDomainKeepsItsPositiveSourceFactsNumericToo() {
        var chart = Chart.Create().WithXAxisTimeScale().WithXAxisBounds(-2, 2)
            .AddLine("Crossing", new[] { new ChartPoint(-1, 5), new ChartPoint(1, 8) });
        var marks = XDocument.Parse(chart.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(new[] { "-1", "1" }, marks.Select(mark => mark.Attribute("data-cfx-x")!.Value));
        Assert.All(marks, mark => Assert.Null(mark.Attribute("data-cfx-x-instant")));
    }

    [Theory]
    [InlineData("cadence", -1.25, -1.2916666666666667)]
    [InlineData("state", -3, -2)]
    [InlineData("state", -1, 1)]
    public void HalfOpenLegacyTimeMetadataCapturesAClosedNumericEndpoint(string family, double start, double boundary) {
        var chart = HalfOpenLegacyTimeChart(family, start, boundary);
        var first = XDocument.Parse(chart.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-observation-id") == "first");
        var inclusiveEnd = boundary < start ? Math.BitIncrement(boundary) : Math.BitDecrement(boundary);
        Assert.Equal(inclusiveEnd, double.Parse(first.Attribute("data-cfx-range-end")!.Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Null(first.Attribute("data-cfx-range-end-exclusive"));
        Assert.Null(first.Attribute("data-cfx-range-start-instant"));
        Assert.Null(first.Attribute("data-cfx-range-end-instant"));
    }

    [Theory]
    [InlineData("cadence", 0)]
    [InlineData("cadence", 1_000)]
    [InlineData("cadence", 9_000)]
    [InlineData("state", 0)]
    [InlineData("state", 1_000)]
    [InlineData("state", 9_000)]
    public void HalfOpenTimeMetadataPreservesItsPreciseExclusiveAndInclusiveEndpoints(string family, int phaseTicks) {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(phaseTicks);
        var first = XDocument.Parse(PreciseTimeIntervalsChart(family, start).ToSvg()).Descendants()
            .Single(node => (string?)node.Attribute("data-cfx-observation-id") == "first");
        Assert.Equal(start.ToString("O"), first.Attribute("data-cfx-range-start-instant")!.Value);
        Assert.Equal(start.AddDays(1).ToString("O"), first.Attribute("data-cfx-range-end-instant")!.Value);
        Assert.Equal(start.AddDays(1).AddTicks(-1).ToString("O"), first.Attribute("data-cfx-range-inclusive-end-instant")!.Value);
    }

    internal static Chart ValueDomainsChart(string domains) {
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(0, 3).WithYAxisBounds(0, 10).WithSecondaryYAxisBounds(1000, 2000);
        if (!domains.StartsWith("secondary", StringComparison.Ordinal)) chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("primary"), "Primary",
            domains == "empty-primary" ? Array.Empty<ChartPoint>() : new[] { new ChartPoint(1, 2), new ChartPoint(2, 8) }));
        if (domains != "primary") chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("secondary"), "Secondary",
            domains == "empty-secondary" ? Array.Empty<ChartPoint>() : new[] { new ChartPoint(1, 1200), new ChartPoint(2, 1800) }).UseSecondaryYAxis());
        if (domains == "secondary-primary-y-line") chart.AddHorizontalLine(5, "Primary threshold");
        if (domains == "secondary-primary-y-region") chart.AddPlotRegion(new ChartPlotRegion(0, yMinimum: 2, yMaximum: 8));
        if (domains == "secondary-x-only") chart.AddVerticalLine(1.5, "Shared X").AddPlotRegion(new ChartPlotRegion(0, xMinimum: 1, xMaximum: 2));
        return chart;
    }

    internal static Chart OverflowChart(bool schedule) {
        if (!schedule) return Chart.Create().WithSize(640, 360).AddBar("Large finite X",
            new[] { new ChartPoint(double.MaxValue * .5, 1), new ChartPoint(double.MaxValue, 2) });
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(1, 2)
            .WithXLabels(new[] { new ChartAxisLabel(1, "Start"), new ChartAxisLabel(2, "End") }).AddGanttTask("Task", 1, 2, .5);
        chart.Options.XAxis.Scale = ChartScaleKind.SymmetricLogarithmic;
        chart.Options.XAxis.SymmetricLogarithmThreshold = double.Epsilon;
        return chart;
    }

    internal static Chart HalfOpenLegacyTimeChart(string family, double start, double boundary) {
        var last = boundary + (boundary - start);
        var chart = Chart.Create().WithSize(640, 360).WithXAxisTimeScale().WithXAxisBounds(Math.Min(start, last), Math.Max(start, last));
        if (family == "cadence") chart.AddSeries(ChartSeries.CreateBar(new ChartSeriesId("legacy-hours"), "Hours",
            new[] { new ChartPoint(start, 5), new ChartPoint(boundary, 8) }).WithTimeBuckets(new ChartTimeTickInterval(ChartTimeTickUnit.Hour)));
        else chart.WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromRgb(13, 148, 136)),
                new ChartStateCategory("down", "Down", ChartColor.FromRgb(220, 38, 38)))
            .AddStateTimelineLane("States", new[] { new ChartStateTimelineSegment(start, boundary, "up"),
                new ChartStateTimelineSegment(boundary, boundary + (boundary - start), "down") });
        chart.Series[0].WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second") });
        return chart;
    }

    internal static Chart PreciseTimeIntervalsChart(string family, DateTime start) {
        var boundary = start.AddDays(1); var end = start.AddDays(2);
        var chart = Chart.Create().WithSize(640, 360).WithXAxisTimeScale();
        if (family == "cadence") chart.AddSeries(ChartSeries.CreateBar(new ChartSeriesId("precise-days"), "Days",
            new[] { new ChartPoint(start, 5), new ChartPoint(boundary, 8) }).WithTimeBuckets(new ChartTimeTickInterval(ChartTimeTickUnit.Day)));
        else chart.WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromRgb(13, 148, 136)),
                new ChartStateCategory("down", "Down", ChartColor.FromRgb(220, 38, 38)))
            .AddStateTimelineLane("States", new[] { new ChartStateTimelineSegment(start, boundary, "up"), new ChartStateTimelineSegment(boundary, end, "down") });
        chart.Series[0].WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second") });
        return chart;
    }
}
