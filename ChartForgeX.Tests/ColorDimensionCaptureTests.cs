using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.ScatterBubbleColorRenderingTests;

namespace ChartForgeX.Tests;

/// <summary>Preparation captures explicit policies once and retains authored empty, missing and reset semantics.</summary>
public sealed class ColorDimensionCaptureTests {
    [Fact]
    public void FormatterMutationCannotChangeCapturedLegendVisibilityOrStyle() {
        var chart = Create(false, 7.125, 0, null).WithLegend();
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High).WithValueRange(0, 20));
        chart.Series[0].ShowInLegend = false;
        var calls = new Dictionary<double, int>();
        chart.Series[0].ConfigureColorDimension(options => {
            options.LegendStyle = ChartColorLegendStyle.Gradient;
            options.ValueFormat = ChartValueFormat.Custom(value => {
                calls[value] = calls.GetValueOrDefault(value) + 1;
                options.ShowLegend = false; options.LegendStyle = ChartColorLegendStyle.Swatches;
                return value.ToString(CultureInfo.InvariantCulture);
            });
        });
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Contains("data-cfx-role=\"color-scale-0-gradient\"", svg);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.DoesNotContain("data-cfx-role=\"color-scale-0\"", Prepare(chart).ToSvg());
    }

    [Fact]
    public void ReusedExplicitPolicyCapturesNamesAndFormatsEachDistinctValueOncePerPreparation() {
        var chart = Create(false, 7.125, 0, 7.125, null).WithLegend(false);
        chart.AddBubble("Other", new[] { new ChartBubble(1, 12, 9).WithColorValue(7.125), new ChartBubble(2, 18, 36).WithColorValue(0), new ChartBubble(3, 24, 81) });
        var calls = new Dictionary<double, int>();
        var replacement = ChartValueFormat.Custom(_ => throw new InvalidOperationException("Late policy must not be observed."));
        var policy = ChartValueFormat.Custom(value => {
            calls[value] = calls.GetValueOrDefault(value) + 1;
            foreach (var series in chart.Series) { series.ColorDimension.Name = "Changed"; series.ColorDimension.ValueFormat = replacement; }
            chart.Options.Labels.NoData = "Changed missing";
            return value.ToString("0.000", CultureInfo.InvariantCulture) + " °C";
        });
        foreach (var series in chart.Series) {
            series.WithColorScale(ChartColorScale.Sequential(Low, High));
            series.ConfigureColorDimension(options => { options.Name = "Temperature"; options.ValueFormat = policy; });
        }
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal(new[] { 0d, 7.125 }, calls.Keys.OrderBy(value => value));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        var points = Role(XDocument.Parse(svg), "point");
        Assert.All(points, point => Assert.Equal("Temperature", (string?)point.Attribute("data-cfx-label-color")));
        Assert.All(points.Where(point => point.Attribute("data-cfx-color-missing") != null), point =>
            Assert.Equal("No data", (string?)point.Attribute("data-cfx-label-no-data")));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        foreach (var series in chart.Series) series.ColorDimension.ValueFormat = policy;
        _ = Prepare(chart);
        Assert.All(calls.Values, count => Assert.Equal(2, count));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CustomEmptyRemainsARecordedColorCaptionAndMissingNeverCallsTheFormatter(bool bubble) {
        var chart = Create(bubble, 7.125, 0, null);
        var calls = new Dictionary<double, int>();
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Low, High).WithValueRange(0, 20))
            .ConfigureColorDimension(options => { options.Name = "Temperature"; options.ValueFormat = ChartValueFormat.Custom(value => {
                calls[value] = calls.GetValueOrDefault(value) + 1; return string.Empty;
            }); });
        var prepared = Prepare(chart); var svg = XDocument.Parse(prepared.ToSvg()); var points = Role(svg, "point");
        Assert.NotNull(points[0].Attribute("data-cfx-formatted-color-value"));
        Assert.Equal(string.Empty, (string?)points[0].Attribute("data-cfx-formatted-color-value"));
        Assert.EndsWith("Temperature: ", prepared.Regions.Single(region => region.Id == "series-0-point-0").Label);
        Assert.Null(points[2].Attribute("data-cfx-formatted-color-value"));
        Assert.Null(points[2].Attribute("data-cfx-color-value"));
        Assert.EndsWith("Temperature: No data", prepared.Regions.Single(region => region.Id == "series-0-point-2").Label);
        Assert.All(Role(svg, "color-scale-0-step"), sample => Assert.Equal(string.Empty, (string?)sample.Attribute("data-cfx-formatted-value")));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        chart.Series[0].Points.Clear();
        if (bubble) chart.AddBubble("Missing", new[] { new ChartBubble(1, 10, 9) });
        else chart.AddScatter("Missing", new[] { new ChartPoint(1, 10) });
        var missingSeries = chart.Series[1];
        missingSeries.WithColorScale(ChartColorScale.Sequential(Low, High))
            .ConfigureColorDimension(options => options.ValueFormat = ChartValueFormat.Custom(_ => throw new InvalidOperationException("Missing has no numeric caption.")));
        Assert.NotNull(Prepare(chart));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullResetRestoresExactOrdinaryPaintAndCaptionsWhileKeepingRawColorFacts(bool bubble) {
        var chart = Create(bubble, -10, 0, null).WithValueFormat(ChartValueFormat.Number("0.0 'units'"));
        var baseline = Prepare(chart); var svg = baseline.ToSvg(); var png = baseline.ToPng();
        var series = chart.Series[0];
        series.ConfigureColorDimension(options => { options.Name = "Temperature"; options.ValueFormat = ChartValueFormat.Number("0.000 '°C'"); });
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
        series.ConfigureColorDimension(options => { options.Name = null; options.ValueFormat = null; });
        var restored = Prepare(chart);
        Assert.Equal(svg, restored.ToSvg()); Assert.Equal(png, restored.ToPng());
        var points = Role(XDocument.Parse(restored.ToSvg()), "point");
        Assert.Equal("-10", (string?)points[0].Attribute("data-cfx-color-value"));
        Assert.Null(points[2].Attribute("data-cfx-color-missing"));
    }

    [Fact]
    public void AuthoredFormatExceptionsPropagateFromPreparation() {
        var chart = Create(false, 7.125).WithLegend(false);
        var failure = new InvalidOperationException("Cannot format color.");
        chart.Series[0].ColorDimension.ValueFormat = ChartValueFormat.Custom(_ => throw failure);
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Prepare(chart)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HierarchyColorAndKeyShareOneExplicitPolicyPerDistinctValue(bool sunburst) {
        var chart = Chart.Create().WithSize(800, 500).WithDataLabels(false);
        var items = new[] { new ChartHierarchyItem("root", "Root"), new ChartHierarchyItem("a", "A", "root", 9, 7.125),
            new ChartHierarchyItem("b", "B", "root", 4, 7.125), new ChartHierarchyItem("missing", "Missing", "root", 2) };
        if (sunburst) chart.AddSunburst("Samples", items); else chart.AddTreemap("Samples", items);
        var calls = new Dictionary<double, int>();
        var options = sunburst ? chart.Options.Sunburst.ColorDimension : chart.Options.Treemap.ColorDimension;
        options.Name = "Temperature";
        options.ValueFormat = ChartValueFormat.Custom(value => { calls[value] = calls.GetValueOrDefault(value) + 1; return "T=" + value.ToString("R", CultureInfo.InvariantCulture); });
        var scale = ChartColorScale.Sequential(new[] { new ChartColorStop(0, Low), new ChartColorStop(8, Empty), new ChartColorStop(20, High) });
        if (sunburst) chart.Options.Sunburst.ColorScale = scale; else chart.Options.Treemap.ColorScale = scale;
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Contains(7.125, calls.Keys); Assert.Contains(8, calls.Keys);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        options.Name = "Changed"; options.ValueFormat = ChartValueFormat.Custom(_ => throw new InvalidOperationException());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }
}
