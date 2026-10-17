using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartLinkedReadableValueContractsTests {
    [Fact]
    public void AutomaticCoordinateHeadingsDoNotRoundObservationBodyValues() {
        const double x = .025, y = .12345678901234566;
        var chart = Chart.Create().WithXAxisBounds(0, 1).WithYAxisBounds(0, 1)
            .AddScatter("Samples", new[] { new ChartPoint(x, y) });
        var point = Mark(chart.Prepare(new VisualRenderContext()).ToSvg());
        Assert.Equal("0.03", point.Attribute("data-cfx-display-label")!.Value);
        Assert.Equal("0.025", point.Attribute("data-cfx-formatted-x")!.Value);
        Assert.Equal("0.12345678901234566", point.Attribute("data-cfx-formatted-value")!.Value);
        Assert.Equal(x, double.Parse(point.Attribute("data-cfx-x")!.Value, CultureInfo.InvariantCulture));
        Assert.Equal(y, double.Parse(point.Attribute("data-cfx-y")!.Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void PreparedReadoutsCaptureAxisCultureAndKeepSourceCoordinatesExact() {
        const double x = 4.242394913100822, y = 8.88888888888889;
        var culture = CultureInfo.GetCultureInfo("de-DE");
        var chart = Chart.Create().AddScatter("Samples", new[] { new ChartPoint(x, y) });
        chart.Options.XAxis.ValueFormat = ChartValueFormat.Number("0.0", culture);
        chart.Options.YAxis.ValueFormat = ChartValueFormat.Number("0.0", culture);
        var prepared = chart.Prepare(new VisualRenderContext());
        var point = Mark(prepared.ToSvg());
        Assert.Equal("4,2", point.Attribute("data-cfx-formatted-x")!.Value);
        Assert.Equal("4,2", point.Attribute("data-cfx-display-label")!.Value);
        Assert.Equal("8,9", point.Attribute("data-cfx-formatted-value")!.Value);
        Assert.Equal(x, double.Parse(point.Attribute("data-cfx-x")!.Value, CultureInfo.InvariantCulture));
        Assert.Equal(y, double.Parse(point.Attribute("data-cfx-y")!.Value, CultureInfo.InvariantCulture));
        chart.Options.XAxis.ValueFormat = ChartValueFormat.Number("0.000");
        Assert.Equal(point.Attribute("data-cfx-formatted-x")!.Value, Mark(prepared.ToSvg()).Attribute("data-cfx-formatted-x")!.Value);
    }

    [Fact]
    public void HistogramReadoutUsesRealBinEdgesWithoutChangingClosedFilterMembership() {
        var chart = Chart.Create().AddSeries(ChartSeries.CreateHistogram(new ChartSeriesId("duration"), "Duration", new[] { 30d, 45 },
            ChartHistogramBinLayout.FromBoundaries(new[] { 20d, 40, 60 })));
        var point = XDocument.Parse(chart.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-bin-index") == "0");
        Assert.Equal("20 – 40", point.Attribute("data-cfx-range-label")!.Value);
        Assert.Equal("20 – 40", point.Attribute("data-cfx-display-label")!.Value);
        Assert.Equal(Math.BitDecrement(40), double.Parse(point.Attribute("data-cfx-range-end")!.Value, CultureInfo.InvariantCulture));
        Assert.Equal("40", point.Attribute("data-cfx-bin-upper")!.Value);
    }

    [Fact]
    public void TimeReadoutsAndBrushTicksUseTheSameAuthoredAxisFormatterOncePerValue() {
        var first = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var counts = new Dictionary<double, int>();
        var chart = Chart.Create().WithSize(800, 400).WithXAxisTimeScale().AddLine("Requests",
            new[] { new ChartPoint(first, 7), new ChartPoint(first.AddDays(2), 12) });
        chart.Options.XAxis.LabelFormatter = number => {
            counts[number] = counts.TryGetValue(number, out var count) ? count + 1 : 1;
            return DateTime.FromOADate(number).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        };
        var prepared = chart.Prepare(new VisualRenderContext());
        var projection = Assert.IsType<ChartDataProjection>(prepared.Scene.DataProjection);
        Assert.NotEmpty(projection.X.DisplayTicks);
        Assert.All(projection.X.DisplayTicks, tick => Assert.Equal(DateTime.FromOADate(tick.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), tick.Text));
        var marks = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(new[] { "2026-09-01", "2026-09-03" }, marks.Select(point => point.Attribute("data-cfx-display-label")!.Value));
        Assert.All(counts.Values, count => Assert.Equal(1, count));
        var snapshot = counts.ToDictionary(pair => pair.Key, pair => pair.Value);
        prepared.ToSvg();
        Assert.Equal(snapshot, counts);
    }

    [Fact]
    public void ProjectionMetadataPreservesExplicitEmptyCaptionsForBrushDisplay() {
        var chart = Chart.Create().WithXAxisBounds(0, 10).AddLine("Empty captions", new[] { new ChartPoint(0, 1), new ChartPoint(10, 2) });
        chart.Options.XAxis.LabelFormatter = _ => string.Empty;
        var html = chart.ToInteractiveHtmlFragmentWithoutAssets();
        var match = Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"");
        using var metadata = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));
        var ticks = metadata.RootElement.GetProperty("dataProjection").GetProperty("x").GetProperty("displayTicks");
        Assert.NotEmpty(ticks.EnumerateArray());
        Assert.All(ticks.EnumerateArray(), tick => Assert.Equal(string.Empty, tick.GetProperty("text").GetString()));
    }

    [Fact]
    public void StateTimelineReadoutsCaptureFormattedBoundsWithoutChangingRawInstants() {
        var first = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var chart = Chart.Create().WithXAxisTimeScale().WithStateCategories(new ChartStateCategory("up", "Available", ChartColor.FromRgb(13, 148, 136)))
            .AddStateTimelineLane("Service", new[] { new ChartStateTimelineSegment(first, first.AddHours(6), "up") });
        chart.Options.XAxis.LabelFormatter = number => DateTime.FromOADate(number).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var point = Mark(chart.ToSvg());
        Assert.Equal("2026-09-01 00:00 – 2026-09-01 06:00", point.Attribute("data-cfx-range-label")!.Value);
        Assert.Equal("2026-09-01 06:00", point.Attribute("data-cfx-formatted-end")!.Value);
        Assert.Equal(first.AddHours(6).ToOADate(), double.Parse(point.Attribute("data-cfx-end")!.Value, CultureInfo.InvariantCulture));
    }

    private static XElement Mark(string svg) => XDocument.Parse(svg).Descendants().Single(node => node.Attribute("data-cfx-point") != null);
}
