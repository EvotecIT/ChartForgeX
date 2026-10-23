using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TrendLineModelsTests {
    [Fact]
    public void FiniteNativeLinearIntervalsDoNotSerializeAnOverflowingSourceUnitSlope() {
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("scaled"), "Small predictor units",
            new[] { new ChartPoint(0, 1e9), new ChartPoint(1e-300, 2e9), new ChartPoint(2e-300, 4e9) }, null,
            options => options.Interval = ChartTrendLineIntervalKind.Confidence);
        var chart = Chart.Create().WithAxes(false).WithGrid(false).WithLegend(false).AddSeries(series);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var regression = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Role == "regression");
        Assert.Equal("not-representable", regression.Metadata["data-cfx-source-coefficients"]);
        Assert.False(regression.Metadata.ContainsKey("data-cfx-slope")); Assert.False(regression.Metadata.ContainsKey("data-cfx-intercept"));
        Assert.All(series.TrendLineSamples, sample => { Assert.True(double.IsFinite(sample.Value)); Assert.True(double.IsFinite(sample.Lower!.Value)); Assert.True(double.IsFinite(sample.Upper!.Value)); });
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
    }

    [Fact]
    public void ConfiguredCreationDoesNotRequireAnIntermediateLinearFitAndDefaultErrorBoundariesRemain() {
        var source = Enumerable.Range(0, 100).Select(index => index < 50 ? new ChartPoint(0, 1) : new ChartPoint(1, 1e308)).ToArray();
        // The legacy covariance sum exceeds double range despite finite endpoints. Keep that existing default
        // boundary, but do not run it before the requested log-response model's independently scaled fit.
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartSeries.CreateTrendLine(new ChartSeriesId("default"), "Default linear", source));
        var exponential = ChartSeries.CreateTrendLine(new ChartSeriesId("direct"), "Direct exponential", source, null,
            options => options.Model = ChartTrendLineModel.Exponential);
        Assert.Equal(source, exponential.TrendLineSourcePoints); Assert.Equal(2, exponential.Points.Count);
        Assert.All(exponential.TrendLineSamples, sample => Assert.InRange(Math.Abs(sample.Value / Math.Exp(Math.Log(1e308) * sample.X) - 1), 0, 1e-10));
        var chart = Chart.Create().AddTrendLine("Configured chart", source, null, options => options.Model = ChartTrendLineModel.Exponential);
        Assert.Equal(ChartTrendLineModel.Exponential, Assert.Single(chart.Series).TrendLineConfiguration!.Model);
        var empty = Assert.Throws<ArgumentException>(() => ChartSeries.CreateTrendLine(new ChartSeriesId("empty"), "Empty", Array.Empty<ChartPoint>()));
        Assert.Equal("observations", empty.ParamName);
        var span = Assert.Throws<ArgumentOutOfRangeException>(() => ChartSeries.CreateTrendLine(new ChartSeriesId("span"), "Infinite span",
            new[] { new ChartPoint(-double.MaxValue, 0), new ChartPoint(double.MaxValue, 1) }));
        Assert.Equal("observations", span.ParamName);
    }

    [Theory]
    [InlineData(ChartTrendLineModel.Linear, 2)]
    [InlineData(ChartTrendLineModel.Polynomial, 2)]
    [InlineData(ChartTrendLineModel.Polynomial, 3)]
    [InlineData(ChartTrendLineModel.Logarithmic, 2)]
    [InlineData(ChartTrendLineModel.Exponential, 2)]
    [InlineData(ChartTrendLineModel.Power, 2)]
    public void ModelsRecoverKnownCurvesWithoutChangingSourceObservationsOrTwoEndpointHandles(ChartTrendLineModel model, int degree) {
        double Expected(double x) => model switch {
            ChartTrendLineModel.Polynomial when degree == 3 => -2 + x - 2 * x * x + .5 * x * x * x,
            ChartTrendLineModel.Polynomial => 1 + 2 * x + 3 * x * x,
            ChartTrendLineModel.Logarithmic => 2 + 3 * Math.Log(x),
            ChartTrendLineModel.Exponential => 2 * Math.Exp(.3 * x),
            ChartTrendLineModel.Power => 3 * Math.Pow(x, 1.5),
            _ => 2 + 3 * x
        };
        var source = Enumerable.Range(1, 7).Select(x => new ChartPoint(x, Expected(x)).WithColorValue(-x)).ToArray();
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("known-fit"), "Known model", source)
            .ConfigureTrendLine(options => { options.Model = model; options.PolynomialDegree = degree; });
        Assert.Equal(source, series.TrendLineSourcePoints); Assert.Equal(source.Length, series.SourcePointCount); Assert.Equal(2, series.Points.Count);
        Assert.All(series.TrendLineSamples, sample => Assert.InRange(Math.Abs(sample.Value - Expected(sample.X)), 0, Math.Max(1e-11, Math.Abs(Expected(sample.X)) * 1e-11)));
        Assert.Equal(model, series.TrendLineConfiguration!.Model);
        var detachedOptions = series.TrendLineConfiguration!; detachedOptions.Model = ChartTrendLineModel.Power;
        Assert.Equal(model, series.TrendLineConfiguration!.Model);
        var chart = Chart.Create().WithLegend(false).AddSeries(series); var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point"), node => {
            Assert.Equal("regression-endpoint", node.Metadata["data-cfx-derived"]); Assert.False(node.Metadata.ContainsKey("data-cfx-source-point"));
        });
        Assert.NotEmpty(prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void DefaultLinearRefittingPreservesExistingEndpointArithmeticAndCompleteSvg() {
        var source = new[] { new ChartPoint(1e12, 2), new ChartPoint(1e12 + .125, 5), new ChartPoint(1e12 + .25, 7) };
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("legacy"), "Linear", source);
        var chart = Chart.Create().AddSeries(series); var before = chart.ToSvg();
        series.ConfigureTrendLine(options => { options.Model = ChartTrendLineModel.Linear; options.SampleCount = 17; });
        Assert.Equal(13d / 6, series.Points[0].Y, 10); Assert.Equal(43d / 6, series.Points[1].Y, 10);
        Assert.Equal(2, series.TrendLineSamples.Count); Assert.Equal(before, chart.ToSvg());
    }

    [Theory]
    [InlineData(0d, 1d)]
    [InlineData(1e12, .125)]
    public void PolynomialCenteringRetainsInteriorExtremaInPrimarySecondaryAndVisibleDomains(double origin, double unit) {
        var source = Enumerable.Range(-2, 5).Select(index => new ChartPoint(origin + index * unit, 4 - index * index)).ToArray();
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("hill"), "Interior maximum", source)
            .ConfigureTrendLine(options => { options.Model = ChartTrendLineModel.Polynomial; options.SampleCount = 18; });
        var maximum = series.TrendLineSamples.Max(sample => sample.Value);
        Assert.Equal(4, maximum, 10); Assert.Equal(2, series.Points.Count);
        Assert.Contains(series.TrendLineSamples, sample => Math.Abs(sample.X - origin) < unit * 1e-6 && Math.Abs(sample.Value - 4) < 1e-10);
        var chart = Chart.Create().WithLegend(false).AddSeries(series); var context = VisualExportRequest.ForChart(chart).Context;
        var full = chart.Prepare(context); Assert.True(full.YProjection!.Maximum >= maximum);
        var visible = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(origin - unit, origin + unit), valueDomain: ChartValueDomain.VisibleCategories));
        Assert.True(visible.YProjection!.Maximum >= maximum);
        series.UseSecondaryValueAxis(); var secondary = chart.Prepare(context);
        Assert.True(secondary.SecondaryYProjection!.Maximum >= maximum);
        Assert.NotEmpty(visible.ToPng());
    }

    [Fact]
    public void InvalidModelDomainsAndInsufficientRankLeaveConfigurationSourceAndSceneUnchanged() {
        var source = new[] { new ChartPoint(0, -2), new ChartPoint(1, 1), new ChartPoint(2, 4) };
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("valid"), "Existing", source);
        var chart = Chart.Create().AddSeries(series); var before = chart.ToSvg();
        foreach (var model in new[] { ChartTrendLineModel.Logarithmic, ChartTrendLineModel.Exponential, ChartTrendLineModel.Power }) {
            Assert.Throws<ArgumentException>(() => series.ConfigureTrendLine(options => options.Model = model));
            Assert.Equal(before, chart.ToSvg()); Assert.Equal(source, series.TrendLineSourcePoints);
            Assert.Equal(ChartTrendLineModel.Linear, series.TrendLineConfiguration!.Model);
        }
        var repeated = ChartSeries.CreateTrendLine(new ChartSeriesId("rank"), "Two distinct predictors",
            new[] { new ChartPoint(0, 1), new ChartPoint(0, 2), new ChartPoint(1, 4) });
        Assert.Throws<ArgumentException>(() => repeated.ConfigureTrendLine(options => options.Model = ChartTrendLineModel.Polynomial));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ConfigureTrendLine(options => options.PolynomialDegree = 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ConfigureTrendLine(options => options.SampleCount = 100000));
        Assert.Equal(before, chart.ToSvg());
    }
}
