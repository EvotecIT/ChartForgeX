using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects independent typed uncertainty through bounds, native painting, captured facts and update identity.</summary>
public sealed class IndependentErrorBarTests {
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void IndependentIntervalsPaintTheirPhysicalAxesAndKeepCompleteFacts(bool horizontal, bool vertical, bool reversed) {
        var source = new ChartErrorBar(5, 5, horizontal ? new ChartErrorBounds(1, 9) : null, vertical ? new ChartErrorBounds(2, 8) : null);
        var chart = ChartFor(source).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        var prepared = Prepare(chart); var x = prepared.XProjection!; var y = prepared.YProjection!;
        var group = Point(prepared); var region = Assert.Single(prepared.Regions, item => item.Id == group.Id && item.Role == "point");
        Assert.Equal("sample", group.Metadata["data-cfx-observation-id"]);
        Assert.Equal("5", group.Metadata["data-cfx-x"]); Assert.Equal("5", group.Metadata["data-cfx-y"]);
        Assert.Equal(horizontal, group.Metadata.ContainsKey("data-cfx-x-lower")); Assert.Equal(vertical, group.Metadata.ContainsKey("data-cfx-y-lower"));
        var lines = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role is "error-range-x" or "error-range" or "error-cap-x" or "error-cap").ToArray();
        Assert.Equal((horizontal ? 3 : 0) + (vertical ? 3 : 0), lines.Length);
        if (horizontal) {
            var line = Assert.Single(lines, item => item.Role == "error-range-x");
            Assert.Equal(x.Project(1), line.Start.X, 8); Assert.Equal(x.Project(9), line.End.X, 8);
            Assert.Equal(y.Project(5), line.Start.Y, 8); Assert.Equal(line.Start.Y, line.End.Y);
        }
        if (vertical) {
            var line = Assert.Single(lines, item => item.Role == "error-range");
            Assert.Equal(y.Project(2), line.Start.Y, 8); Assert.Equal(y.Project(8), line.End.Y, 8);
            Assert.Equal(x.Project(5), line.Start.X, 8); Assert.Equal(line.Start.X, line.End.X);
        }
        foreach (var line in lines) {
            Assert.True(region.Bounds.Left <= Math.Min(line.Start.X, line.End.X) - line.StrokeWidth / 2 + 1e-8);
            Assert.True(region.Bounds.Right >= Math.Max(line.Start.X, line.End.X) + line.StrokeWidth / 2 - 1e-8);
            Assert.True(region.Bounds.Top <= Math.Min(line.Start.Y, line.End.Y) - line.StrokeWidth / 2 + 1e-8);
            Assert.True(region.Bounds.Bottom >= Math.Max(line.Start.Y, line.End.Y) + line.StrokeWidth / 2 - 1e-8);
        }
        Assert.NotEmpty(prepared.ToPng()); Assert.Equal(720, prepared.ToRgba().Width);
    }

    [Fact]
    public void AutomaticBoundsContainBothUncertaintyExtentsAndPerpendicularCaps() {
        var chart = ChartFor(new ChartErrorBar(5, 0, xBounds: new ChartErrorBounds(1, 9)));
        var prepared = Prepare(chart); var plot = prepared.XProjection!.PlotBounds;
        Assert.True(prepared.XProjection.FullMinimum <= 1 && prepared.XProjection.FullMaximum >= 9);
        foreach (var line in prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role is "error-range-x" or "error-cap-x")) {
            Assert.True(Math.Min(line.Start.Y, line.End.Y) - line.StrokeWidth / 2 >= plot.Top - 1e-8);
            Assert.True(Math.Max(line.Start.Y, line.End.Y) + line.StrokeWidth / 2 <= plot.Bottom + 1e-8);
        }
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void NativeWindowKeepsACrossingHorizontalWhiskerWhenItsEstimateIsOutside() {
        var chart = ChartFor(new ChartErrorBar(0, 5, xBounds: new ChartErrorBounds(-1, 9))).WithXAxisBounds(-2, 10).WithYAxisBounds(0, 10);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context, new ChartCartesianViewport(x: new ChartAxisWindow(2, 8)));
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), item => item.Role == "error-range-x");
        var plot = prepared.XProjection!.PlotBounds;
        Assert.True(line.Start.X < plot.Left && line.End.X > plot.Right);
        Assert.False(Point(prepared).Metadata.ContainsKey("data-cfx-visible"));
        Assert.Equal("0", Point(prepared).Metadata["data-cfx-x"]);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void HorizontalDatesAndVerticalUnitsAreCapturedByTheirOwnNativeFormatters() {
        var start = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var source = new ChartErrorBar(start, 5, new ChartErrorBounds(start.AddDays(-2), start.AddDays(3)), new ChartErrorBounds(2, 8));
        var chart = ChartFor(source);
        chart.Options.XAxis.WithTimeScale();
        chart.Options.XAxis.LabelFormatter = value => DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        chart.Options.YAxis.LabelFormatter = value => value.ToString("F2", CultureInfo.InvariantCulture) + " kg";
        var facts = Point(Prepare(chart)).Metadata;
        Assert.Equal("2026-01-08", facts["data-cfx-formatted-x-lower"]); Assert.Equal("2026-01-13", facts["data-cfx-formatted-x-upper"]);
        Assert.Equal("2.00 kg", facts["data-cfx-formatted-y-lower"]); Assert.Equal("8.00 kg", facts["data-cfx-formatted-y-upper"]);
        Assert.Equal(source.XBounds!.Value.Lower.ToString("R", CultureInfo.InvariantCulture), facts["data-cfx-x-lower"]);
        Assert.NotEmpty(Prepare(chart).ToPng());
    }

    [Fact]
    public void ZeroWidthIntervalsAndCapturedSourceRemainValidAcrossTypedUpdates() {
        var inputs = new[] { new ChartErrorBar(5, 5, new ChartErrorBounds(5, 5), new ChartErrorBounds(5, 5)) };
        var chart = ChartFor(inputs); var series = chart.Series[0];
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        inputs[0] = new ChartErrorBar(7, 8, yBounds: new ChartErrorBounds(7, 9));
        Assert.Equal(5, series.ErrorBars[0].XBounds!.Value.Lower);
        series.UpdateErrorBar(0, new ChartErrorBar(5, 6, new ChartErrorBounds(1, 9), new ChartErrorBounds(3, 8)));
        Assert.Equal(new ChartObservationId("sample"), series.ObservationIds[0]);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        var current = Point(Prepare(chart)).Metadata;
        Assert.Equal("1", current["data-cfx-x-lower"]); Assert.Equal("8", current["data-cfx-y-upper"]);
    }

    [Fact]
    public void EachIntervalAndEstimateIsValidatedWithoutPartialReplacement() {
        Assert.Throws<ArgumentException>(() => new ChartErrorBar(1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartErrorBounds(3, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartErrorBounds(double.NaN, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartErrorBounds(1, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartErrorBar(1, 2, new ChartErrorBounds(2, 3)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartErrorBar(1, 2, yBounds: new ChartErrorBounds(3, 4)));
        var series = ChartSeries.CreateErrorBar(new ChartSeriesId("sample"), "Sample", new[] { new ChartErrorBar(1, 2, yBounds: new ChartErrorBounds(1, 3)) });
        Assert.Throws<ArgumentException>(() => series.UpdateErrorBar(0, default));
        Assert.Equal(1, series.ErrorBars[0].YBounds!.Value.Lower);
    }

    [Fact]
    public void SelectedLogarithmicDomainsValidateEveryUncertaintyEndpoint() {
        var chart = ChartFor(new ChartErrorBar(-4, -4, new ChartErrorBounds(-8, -2), new ChartErrorBounds(-8, -1)));
        chart.Options.XAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative);
        chart.Options.YAxis.WithLogarithmicScale(2, ChartLogarithmicDomain.Negative);
        Assert.NotEmpty(Prepare(chart).ToPng());
        chart.Series[0].UpdateErrorBar(0, new ChartErrorBar(-4, -4, new ChartErrorBounds(-8, 0), new ChartErrorBounds(-8, -1)));
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Fact]
    public void SecondaryValueAxisFormatsOnlyTheVerticalInterval() {
        var chart = ChartFor(new ChartErrorBar(5, 100, new ChartErrorBounds(2, 8), new ChartErrorBounds(80, 140)));
        chart.Series[0].UseSecondaryValueAxis();
        chart.Options.XAxis.LabelFormatter = value => "X=" + value.ToString(CultureInfo.InvariantCulture);
        chart.Options.YAxis.LabelFormatter = value => "Primary=" + value.ToString(CultureInfo.InvariantCulture);
        chart.Options.SecondaryYAxis.LabelFormatter = value => "Secondary=" + value.ToString(CultureInfo.InvariantCulture);
        var prepared = Prepare(chart); var facts = Point(prepared).Metadata;
        Assert.Equal("secondary", facts["data-cfx-axis"]);
        Assert.Equal("X=2", facts["data-cfx-formatted-x-lower"]); Assert.Equal("Secondary=80", facts["data-cfx-formatted-y-lower"]);
        Assert.Equal("Secondary=140", facts["data-cfx-formatted-y-upper"]); Assert.NotEmpty(prepared.ToPng());
    }

    private static Chart ChartFor(ChartErrorBar source) => ChartFor(new[] { source });
    private static Chart ChartFor(IEnumerable<ChartErrorBar> sources) => Chart.Create().WithSize(720, 420).WithLegend(false)
        .AddSeries(ChartSeries.CreateErrorBar(new ChartSeriesId("measurements"), "Measurements", sources)
            .WithObservationId(0, new ChartObservationId("sample")));
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneGroup Point(PreparedVisual prepared) => Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point");
}
