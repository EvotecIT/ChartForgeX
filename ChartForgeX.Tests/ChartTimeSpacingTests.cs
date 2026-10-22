using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartTimeSpacingTests {
    private static readonly DateTime January = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static double Oa(DateTime value) => ChartDateTime.ToOADate(value);
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(800, 440)));
    private static Chart ChartOf(params DateTime[] instants) {
        var chart = Chart.Create().WithXAxisScale(ChartScaleKind.Time).AddLine("Observations", instants.Select((instant, index) => new ChartPoint(Oa(instant), index + 1)));
        chart.Series[0].Markers.Enabled = false;
        return chart;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinalUsesSuppliedInstantsRatherThanElapsedGapsAndKeepsDetachedRawFacts(bool reversed) {
        var instants = new[] { January, January.AddDays(1), January.AddDays(11) };
        var chart = ChartOf(instants); chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal).Reversed = reversed;
        var prepared = chart.Prepare(Context()); var axis = prepared.XProjection!;
        var positions = instants.Select(instant => axis.Project(Oa(instant))).ToArray();
        Assert.Equal(Math.Abs(positions[1] - positions[0]), Math.Abs(positions[2] - positions[1]), 8);
        foreach (var instant in instants) Assert.Equal(Oa(instant), axis.Unproject(axis.Project(Oa(instant))), 8);
        Assert.Equal(Oa(January.AddDays(6)), axis.Unproject((positions[1] + positions[2]) / 2), 8);
        var groups = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(3, groups.Length);
        Assert.Equal(instants.Select(instant => Oa(instant).ToString("G17", CultureInfo.InvariantCulture)), groups.Select(node => node.Attribute("data-cfx-x")!.Value));
        Assert.All(groups, node => Assert.Contains("2024-", node.Attribute("data-cfx-x-instant")!.Value));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.XAxis.TimeSpacing = null; chart.Options.XAxis.Reversed = !reversed;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.Same(ChartTimeSpacing.Ordinal, axis.TimeSpacing);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CalendarMonthsKeepMissingBandsCenterRawInstantsAndBoundBarFootprints(bool horizontal) {
        var chart = Chart.Create();
        var points = new[] { new ChartPoint(Oa(January.AddDays(9)), 3), new ChartPoint(Oa(January.AddMonths(2).AddDays(29)), 7) };
        if (horizontal) chart.AddHorizontalBar("Monthly", points); else chart.AddBar("Monthly", points);
        var authored = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        authored.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
        var prepared = chart.Prepare(Context()); var axis = horizontal ? prepared.YProjection! : prepared.XProjection!;
        var first = axis.Project(points[0].X); var middle = axis.Project(Oa(January.AddMonths(1).AddDays(14))); var last = axis.Project(points[1].X);
        Assert.Equal(Math.Abs(middle - first), Math.Abs(last - middle), 8);
        Assert.Equal(Oa(January), axis.Unproject(first), 8); // Deliberate discrete canonical inverse, not the January 10 source instant.
        Assert.Equal(Oa(January.AddMonths(2)), axis.Unproject(last), 8);
        var bands = prepared.Regions.Where(region => region.Role == "point").ToArray();
        Assert.Equal(2, bands.Length);
        Assert.All(bands, region => Assert.InRange(horizontal ? region.Bounds.Height : region.Bounds.Width, .1, Math.Abs(middle - first)));
        Assert.Equal(points.Select(point => point.X), chart.Series[0].Points.Select(point => point.X));
        Assert.NotEmpty(prepared.ToPng());
        var ticks = horizontal ? prepared.Scene.DataProjection!.Y!.DisplayTicks : prepared.Scene.DataProjection!.X.DisplayTicks;
        var missingCaption = Assert.Single(ticks, tick => tick.Value == Oa(January.AddMonths(1)));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == (horizontal ? "axis-y-label" : "axis-x-label")
            && string.Join("\n", text.Text.Lines.Select(line => line.Text)) == missingCaption.Text);
        Assert.DoesNotContain(XDocument.Parse(prepared.ToSvg()).Descendants(), node => (string?)node.Attribute("data-cfx-x") == Oa(January.AddMonths(1)).ToString("G17", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(ChartSeriesKind.RangeBar, false)]
    [InlineData(ChartSeriesKind.RangeBar, true)]
    [InlineData(ChartSeriesKind.BoxPlot, false)]
    [InlineData(ChartSeriesKind.BoxPlot, true)]
    [InlineData(ChartSeriesKind.Waterfall, false)]
    [InlineData(ChartSeriesKind.Waterfall, true)]
    public void IntervalCategoryCaptionsCannotEraseMissingCalendarBandsOrOverrideAuthoredTicks(ChartSeriesKind kind, bool horizontal) {
        var chart = Chart.Create().WithLegend(false); var first = January.AddDays(9); var last = January.AddMonths(2).AddDays(29);
        var orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        if (kind == ChartSeriesKind.RangeBar) chart.ConfigureRangeBar(options => options.Orientation = orientation)
            .AddRangeBar("Ranges", new[] { new ChartInterval(first, 2, 4), new ChartInterval(last, 5, 8) });
        else if (kind == ChartSeriesKind.BoxPlot) chart.ConfigureBoxPlot(options => options.Orientation = orientation)
            .AddBoxPlot("Boxes", new[] { new ChartBoxPlot(first, 1, 2, 3, 4, 5), new ChartBoxPlot(last, 4, 5, 6, 7, 8) });
        else chart.ConfigureWaterfall(options => options.Orientation = orientation)
            .AddWaterfall("Changes", new[] { ChartWaterfallItem.OpeningBalance(first, 3), ChartWaterfallItem.Delta(last, 4) });
        var axis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        axis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
        var prepared = chart.Prepare(Context());
        var projected = horizontal ? prepared.Scene.DataProjection!.Y! : prepared.Scene.DataProjection!.X;
        var missingCaption = Assert.Single(projected.DisplayTicks, tick => tick.Value == Oa(January.AddMonths(1)));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == (horizontal ? "axis-y-label" : "axis-x-label")
            && string.Join("\n", text.Text.Lines.Select(line => line.Text)) == missingCaption.Text);
        Assert.Equal(2, chart.Series[0].ObservationCount); Assert.NotEmpty(prepared.ToPng());
        axis.Labels.Add(new ChartAxisLabel(Oa(January.AddMonths(1)), "Authored February"));
        var authored = chart.Prepare(Context());
        var ticks = horizontal ? authored.Scene.DataProjection!.Y!.DisplayTicks : authored.Scene.DataProjection!.X.DisplayTicks;
        Assert.Equal("Authored February", Assert.Single(ticks).Text); Assert.Equal(2, chart.Series[0].ObservationCount);
    }

    [Fact]
    public void UnitRejectsDuplicateSeriesBandsWithoutMergingSourcesButAllowsSharedBandsAcrossSeries() {
        var chart = ChartOf(January, January.AddDays(2));
        chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
        var saved = chart.Series[0].Points.ToArray();
        var error = Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
        Assert.Contains("more than one observation", error.Message); Assert.Equal(saved, chart.Series[0].Points);
        var shared = ChartOf(January, January.AddMonths(1));
        shared.AddLine("Same bands, different source instants", new[] { new ChartPoint(Oa(January.AddDays(12)), 8), new ChartPoint(Oa(January.AddMonths(1).AddDays(8)), 9) });
        shared.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month)));
        var prepared = shared.Prepare(Context());
        Assert.Equal(4, XDocument.Parse(prepared.ToSvg()).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "point"));
        Assert.Equal(prepared.XProjection!.Project(Oa(January)), prepared.XProjection.Project(Oa(January.AddDays(12))), 8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnitSourceWindowsKeepExactRequestsWhileGesturesSelectWholeCalendarBands(bool reversed) {
        var chart = ChartOf(January.AddDays(2), January.AddMonths(1).AddDays(9), January.AddMonths(2).AddDays(9));
        chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month))).Reversed = reversed;
        var full = chart.Prepare(Context()); var requested = new ChartAxisWindow(Oa(January.AddDays(5)), Oa(January.AddDays(20)));
        var contained = full.XProjection!.ConstrainViewport(requested)!;
        Assert.Equal(requested.Minimum, contained.Minimum); Assert.Equal(requested.Maximum, contained.Maximum);
        var prepared = chart.Prepare(Context(), new ChartCartesianViewport(x: requested)); var axis = prepared.XProjection!;
        Assert.Equal(requested.Minimum, axis.Minimum); Assert.Equal(requested.Maximum, axis.Maximum);
        Assert.Equal(axis.PlotBounds.Left + axis.PlotBounds.Width / 2, axis.Project(Oa(January.AddDays(2))), 8);
        var firstFact = XDocument.Parse(prepared.ToSvg()).Descendants().First(node => (string?)node.Attribute("data-cfx-role") == "point");
        Assert.NotEqual("false", (string?)firstFact.Attribute("data-cfx-visible"));
        var projected = full.XProjection; var left = projected.Project(Oa(January.AddMonths(1).AddDays(5)));
        var confined = projected.ConstrainViewport(left - 1, left + 1)!;
        Assert.Equal(Oa(January.AddMonths(1)), confined.Minimum); Assert.Equal(Oa(January.AddMonths(2)), confined.Maximum);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void CalendarDayBandsRetainEqualWidthsAcrossSpringDstAndYearUnitsUseExistingCalendarOwner() {
        var start = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday);
        var end = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday);
        var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), TimeSpan.FromHours(1), start, end);
        var zone = TimeZoneInfo.CreateCustomTimeZone("cfx-spacing-test", TimeSpan.FromHours(-5), "Local", "Standard", "Daylight", new[] { rule });
        var dates = new[] { new DateTime(2024, 3, 9, 12, 0, 0, DateTimeKind.Utc), new DateTime(2024, 3, 10, 12, 0, 0, DateTimeKind.Utc), new DateTime(2024, 3, 11, 12, 0, 0, DateTimeKind.Utc) };
        var chart = ChartOf(dates); chart.Options.XAxis.WithTimeScale(zone).WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Day)));
        var axis = chart.Prepare(Context()).XProjection!;
        Assert.Equal(Math.Abs(axis.Project(Oa(dates[1])) - axis.Project(Oa(dates[0]))), Math.Abs(axis.Project(Oa(dates[2])) - axis.Project(Oa(dates[1]))), 8);
        Assert.Equal(Oa(new DateTime(2024, 3, 10, 5, 0, 0, DateTimeKind.Utc)), axis.Unproject(axis.Project(Oa(dates[1]))), 8);
        var years = ChartOf(January.AddDays(59), January.AddYears(2).AddDays(20));
        years.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Year)));
        var yearAxis = years.Prepare(Context()).XProjection!;
        Assert.Equal(Oa(January), yearAxis.Unproject(yearAxis.Project(Oa(January.AddDays(59)))), 8);
        Assert.Equal(Math.Abs(yearAxis.Project(Oa(January.AddYears(1))) - yearAxis.Project(Oa(January))), Math.Abs(yearAxis.Project(Oa(January.AddYears(2))) - yearAxis.Project(Oa(January.AddYears(1)))), 8);
    }

    [Fact]
    public void CapturedPoliciesCoverSecondaryValueAxesAndHtmlKeyedMetadataWithoutChangingElapsedDefaults() {
        var chart = ChartOf(January, January.AddDays(1), January.AddDays(11));
        var elapsed = chart.Prepare(Context()); chart.Options.XAxis.TimeSpacing = null;
        Assert.Equal(elapsed.ToSvg(), chart.Prepare(Context()).ToSvg()); Assert.Equal(elapsed.ToPng(), chart.Prepare(Context()).ToPng());
        chart.AddLine("Secondary dates", new[] { new ChartPoint(Oa(January), Oa(January)), new ChartPoint(Oa(January.AddDays(1)), Oa(January.AddDays(1))), new ChartPoint(Oa(January.AddDays(11)), Oa(January.AddDays(11))) });
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
        chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
        var section = new HtmlInteractiveChartRenderer().PrepareSection(chart);
        var projection = section.PreparedVisual.SecondaryYProjection!;
        Assert.Same(ChartTimeSpacing.Ordinal, projection.TimeSpacing);
        Assert.Equal(Oa(January.AddDays(1)), projection.Unproject(projection.Project(Oa(January.AddDays(1)))), 8);
        using var metadata = Metadata(section.Html);
        foreach (var key in new[] { "xProjection", "secondaryYProjection" }) {
            var spacing = metadata.RootElement.GetProperty(key).GetProperty("timeSpacing");
            Assert.Equal("ordinal", spacing.GetProperty("kind").GetString()); Assert.Equal(3, spacing.GetProperty("instants").GetArrayLength());
        }
        Assert.False(metadata.RootElement.GetProperty("yProjection").TryGetProperty("timeSpacing", out _));
        Assert.Null(chart.Options.XAxis.PreparedTimeDomain); Assert.Null(chart.Options.SecondaryYAxis.PreparedTimeDomain);
    }

    [Fact]
    public void ContradictorySettingsAndUnboundedCalendarDomainsRejectBeforePaintWithoutMutatingPolicies() {
        var axis = new ChartAxis().WithTimeSpacing(ChartTimeSpacing.Ordinal);
        var sessions = new ChartTimeSessionDomain(new[] { new ChartTimeSession(January, January.AddDays(1)) });
        Assert.Throws<InvalidOperationException>(() => axis.WithTimeSessions(sessions)); Assert.Null(axis.TimeSessions); Assert.Same(ChartTimeSpacing.Ordinal, axis.TimeSpacing);
        Assert.Throws<InvalidOperationException>(() => axis.Scale = ChartScaleKind.Linear); Assert.Equal(ChartScaleKind.Time, axis.Scale);
        Assert.Throws<InvalidOperationException>(() => axis.WithLogarithmicScale()); Assert.Equal(ChartScaleKind.Time, axis.Scale);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.Minimum = -1); Assert.Null(axis.Minimum);
        var prior = new ChartAxis().WithBounds(-2, -1); Assert.Throws<ArgumentOutOfRangeException>(() => prior.WithTimeSpacing(ChartTimeSpacing.Ordinal)); Assert.Equal(ChartScaleKind.Linear, prior.Scale);
        var tooMany = ChartOf(January, January.AddDays(1));
        tooMany.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Millisecond)));
        Assert.Contains("100,000", Assert.Throws<InvalidOperationException>(() => tooMany.Prepare(Context())).Message);
        var invalid = Chart.Create().WithXAxisScale(ChartScaleKind.Time).AddLine("Unordered OLE", new[] { new ChartPoint(-2, 2), new ChartPoint(-1, 3) });
        invalid.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
        Assert.Throws<InvalidOperationException>(() => invalid.Prepare(Context()));
    }

    [Fact]
    public void EmptyAndSingletonTimePoliciesRemainFiniteWithoutAddingSourceObservations() {
        foreach (var spacing in new[] { ChartTimeSpacing.Ordinal, ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Day)) }) {
            var empty = ChartOf(); empty.Options.XAxis.WithTimeSpacing(spacing); Assert.NotEmpty(empty.Prepare(Context()).ToSvg()); Assert.Empty(empty.Series[0].Points);
            var single = ChartOf(January.AddHours(12)); single.Options.XAxis.WithTimeSpacing(spacing);
            var prepared = single.Prepare(Context()); Assert.True(double.IsFinite(prepared.XProjection!.Project(Oa(January.AddHours(12)))));
            Assert.Single(single.Series[0].Points); Assert.NotEmpty(prepared.ToPng());
        }
    }

    private static JsonDocument Metadata(string html) {
        var value = System.Text.RegularExpressions.Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]*)\"").Groups[1].Value;
        return JsonDocument.Parse(System.Net.WebUtility.HtmlDecode(value));
    }
}
