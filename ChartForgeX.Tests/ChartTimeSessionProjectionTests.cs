using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartTimeSessionProjectionTests {
    private static readonly DateTime Friday = new(2026, 1, 30, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Monday = Friday.AddDays(3);
    private static double Oa(DateTime value) => ChartDateTime.ToOADate(value);
    private static ChartTimeSessionDomain Domain() => new(new[] { new ChartTimeSession(Friday, Friday.AddHours(8)), new ChartTimeSession(Monday, Monday.AddHours(8)) });
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(640, 380)), VisualTheme.Graphite());

    [Fact]
    public void DomainSnapshotsOrderedUtcIntervalsAndKeepsTheRepresentationLimitScoped() {
        var sessions = new[] { new ChartTimeSession(new DateTimeOffset(Friday).ToOffset(TimeSpan.FromHours(2)), new DateTimeOffset(Friday.AddHours(8))), new ChartTimeSession(Monday, Monday.AddHours(8)) };
        var domain = new ChartTimeSessionDomain(sessions);
        sessions[0] = sessions[1];
        Assert.Equal(Oa(Friday), domain.Sessions[0].Minimum);
        Assert.Equal(Oa(Friday.AddHours(8)), domain.Sessions[0].Maximum);
        Assert.Throws<ArgumentException>(() => new ChartTimeSessionDomain(Array.Empty<ChartTimeSession>()));
        Assert.Throws<ArgumentException>(() => new ChartTimeSessionDomain(new[] { new ChartTimeSession(Monday, Monday.AddHours(8)), new ChartTimeSession(Friday, Friday.AddHours(8)) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartTimeSession(-2, -1));
        var continuous = Chart.Create().WithXAxisScale(ChartScaleKind.Time).AddLine("Historic numeric time", new[] { new ChartPoint(-2, 4), new ChartPoint(-1, 8) });
        Assert.NotEmpty(continuous.Prepare(Context()).ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedFinancialLineAndRangeShareNativeIncludedTimeGeometryAndInverse(bool reversed) {
        var chart = Mixed(); chart.Options.XAxis.Reversed = reversed;
        var prepared = chart.Prepare(Context()); var projection = prepared.XProjection!; var plot = projection.PlotBounds;
        var quarter = projection.Project(Oa(Friday.AddHours(4))); var thirdQuarter = projection.Project(Oa(Monday.AddHours(4)));
        Assert.Equal(plot.Left + plot.Width * (reversed ? .75 : .25), quarter, 7);
        Assert.Equal(plot.Left + plot.Width * (reversed ? .25 : .75), thirdQuarter, 7);
        Assert.Equal(projection.Project(Oa(Friday.AddHours(8))), projection.Project(Oa(Monday)), 8);
        Assert.Equal(Oa(Monday), projection.Unproject(projection.Project(Oa(Friday.AddHours(8)))), 8);
        Assert.Equal(Oa(Monday.AddHours(8)), projection.Unproject(projection.Project(Oa(Monday.AddHours(8)))), 8);
        var groups = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(12, groups.Length);
        foreach (var group in groups) {
            var x = double.Parse(group.Attribute("data-cfx-x")!.Value, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(Domain().Includes(x));
            var region = prepared.Regions.Single(region => region.Id == group.Attribute("data-cfx-source-id")!.Value);
            Assert.Equal(projection.Project(x), region.Bounds.Left + region.Bounds.Width / 2, 7);
        }
        var ticks = prepared.Scene.DataProjection!.X.DisplayTicks;
        Assert.NotEmpty(ticks); Assert.All(ticks, tick => Assert.True(Domain().Includes(tick.Value)));
        Assert.Equal(ticks.Count, ticks.Select(tick => projection.Project(tick.Value)).Distinct().Count());
        Assert.NotEmpty(prepared.ToPng());
        var svg = prepared.ToSvg(); chart.Options.XAxis.TimeSessions = null;
        Assert.Equal(svg, prepared.ToSvg()); Assert.NotNull(projection.TimeSessions);
        Assert.Equal(Oa(Monday), projection.Unproject(projection.Project(Oa(Friday.AddHours(8)))), 8);
    }

    [Fact]
    public void RawWeekendWindowBoundsSurviveSourceConfinementWithoutBecomingObservations() {
        var chart = Mixed(); var full = chart.Prepare(Context());
        var minimum = Oa(Monday.AddDays(-1)); var maximum = Oa(Monday.AddHours(6));
        var requested = new ChartXViewport(minimum, maximum);
        var confined = full.XProjection!.ConstrainViewport(requested)!;
        Assert.Equal(requested.Minimum, confined.Minimum); Assert.Equal(requested.Maximum, confined.Maximum);
        var window = chart.Prepare(Context(), confined); var projection = window.XProjection!;
        Assert.Equal(minimum, projection.Minimum); Assert.Equal(maximum, projection.Maximum);
        Assert.Equal(Oa(Monday), projection.Unproject(projection.PlotBounds.Left), 8);
        Assert.Equal(AxisText(full, "axis-y-label"), AxisText(window, "axis-y-label"));
        Assert.Contains(window.Scene.DataProjection!.X.DisplayBounds, caption => caption.Value == minimum);
        var before = XDocument.Parse(window.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-x") == Oa(Friday.AddHours(8)).ToString("G17", System.Globalization.CultureInfo.InvariantCulture)
            && (string?)node.Attribute("data-cfx-role") == "point");
        Assert.All(before, point => Assert.Equal("false", (string?)point.Attribute("data-cfx-visible")));
        Assert.Null(full.XProjection.ConstrainViewport(new ChartXViewport(full.XProjection.FullMinimum, full.XProjection.FullMaximum)));
        var zero = new ChartXViewport(Oa(Friday.AddDays(1)), Oa(Friday.AddDays(2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => full.XProjection.ConstrainViewport(zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Prepare(Context(), zero));
        Assert.NotEmpty(window.ToPng());
    }

    [Fact]
    public void ClosedGapObservationsAndPointAnchorsRejectWhileSourceRangeBoundsRemainTruthful() {
        var chart = Mixed(); var closed = Oa(Friday.AddDays(1));
        chart.AddLine("Closed observation", new[] { new ChartPoint(closed, 40) });
        Assert.Contains("Closed observation", Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context())).Message);
        var regression = Mixed().AddSeries(ChartSeries.CreateTrendLine(new ChartSeriesId("regression"), "Closed regression source",
            new[] { new ChartPoint(Oa(Friday), 20), new ChartPoint(closed, 25), new ChartPoint(Oa(Monday), 30) }));
        Assert.Contains("Closed regression source", Assert.Throws<InvalidOperationException>(() => regression.Prepare(Context())).Message);
        var anchored = Mixed().AddNote(new ChartPoint(closed, 40), "Closed anchor");
        Assert.Contains("Source annotation", Assert.Throws<InvalidOperationException>(() => anchored.Prepare(Context())).Message);
        var range = Mixed();
        range.Annotations.Add(new ChartAnnotation(ChartAnnotationKind.VerticalBand, closed, Oa(Monday.AddHours(4)), "Window", new ChartColor(40, 90, 180), .15));
        Assert.NotEmpty(range.Prepare(Context()).ToPng());
        var incompatible = Mixed(); incompatible.Options.YAxis.TimeSessions = Domain();
        Assert.Throws<NotSupportedException>(() => incompatible.Prepare(Context()));
    }

    [Fact]
    public void CompactWeekendWindowKeepsDateContextWhenCalendarStrideFitsCaptions() {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 440)), VisualTheme.Graphite());
        var window = Mixed().Prepare(context, new ChartXViewport(Oa(Monday.AddDays(-1)), Oa(Monday.AddHours(6))));
        var ticks = window.Scene.DataProjection!.X.DisplayTicks;
        Assert.NotEmpty(ticks);
        Assert.All(ticks, tick => Assert.Contains("02-02", tick.Text));
    }

    private static Chart Mixed() {
        var x = new[] { Friday, Friday.AddHours(8), Monday, Monday.AddHours(8) };
        var chart = Chart.Create().WithLegend(false).WithXAxisBounds(Oa(Friday), Oa(Monday.AddHours(8))).WithYAxisBounds(0, 100)
            .AddSeries(ChartSeries.CreateCandlestick(new ChartSeriesId("prices"), "Prices", x.Select((time, index) => new ChartCandlestick(time, 30 + index, 50 + index, 20 + index, 40 + index))))
            .AddSeries(new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.Line, x.Select((time, index) => new ChartPoint(Oa(time), 60 + index))))
            .AddSeries(ChartSeries.CreateRangeArea(new ChartSeriesId("range"), "Range", x.Select((time, index) => new ChartRangeBand(time, 65 + index, 75 + index))));
        chart.ConfigureXAxis(axis => axis.WithTimeSessions(Domain()));
        return chart;
    }
    private static string?[] AxisText(PreparedVisual prepared, string role) => prepared.Regions.Where(region => region.Role == role).Select(region => region.Label).ToArray();
}
