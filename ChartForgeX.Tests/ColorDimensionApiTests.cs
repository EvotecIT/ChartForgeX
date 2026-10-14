using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.ScatterBubbleColorRenderingTests;

namespace ChartForgeX.Tests;

/// <summary>Color presentation has owned callback options and clear numeric-family applicability.</summary>
public sealed class ColorDimensionApiTests {
    [Fact]
    public void LegendControlsHaveIndependentDefaultsAndRejectUnsupportedStylesWithoutChangingThePolicy() {
        var options = new ChartColorDimensionOptions();
        Assert.True(options.ShowLegend); Assert.Equal(ChartColorLegendStyle.Auto, options.LegendStyle);
        options.LegendStyle = ChartColorLegendStyle.Gradient;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.LegendStyle = (ChartColorLegendStyle)999);
        Assert.Equal(ChartColorLegendStyle.Gradient, options.LegendStyle);
        var chart = Chart.Create().AddHeatmapRow("Values", new[] { 1d, 2 });
        chart.Options.HeatmapColorDimension.LegendStyle = ChartColorLegendStyle.Swatches;
        Assert.Contains("resolved ChartColorScale", Assert.Throws<InvalidOperationException>(() => Prepare(chart)).Message);
        chart.Options.HeatmapColorDimension.LegendStyle = ChartColorLegendStyle.Auto;
        Assert.NotNull(Prepare(chart));
    }

    [Fact]
    public void ConfigurationOwnsDetachedSnapshotsAndResetsEachPolicyIndependently() {
        var chart = Chart.Create().AddScatter("Samples", new[] { new ChartPoint(1, 2) });
        var series = chart.Series[0];
        var options = new[] { series.ColorDimension, chart.Options.MapColorDimension, chart.Options.HeatmapColorDimension,
            chart.Options.Treemap.ColorDimension, chart.Options.Sunburst.ColorDimension };
        Assert.Equal(options.Length, options.Distinct().Count());
        var format = ChartValueFormat.Number("0.0");
        Assert.Same(series, series.ConfigureColorDimension(value => { value.Name = "Temperature"; value.ValueFormat = format; }));
        Assert.Same(chart, chart.ConfigureMapColorDimension(value => value.Name = "Rainfall"));
        Assert.Same(chart, chart.ConfigureHeatmapColorDimension(value => value.ValueFormat = format));
        Assert.Same(chart, chart.ConfigureTreemap(value => value.ColorDimension.Name = "Margin"));
        Assert.Same(chart, chart.ConfigureSunburst(value => value.ColorDimension.Name = "Risk"));
        Assert.All(options, original => { Assert.Null(original.Name); Assert.Null(original.ValueFormat); });
        Assert.Equal("Rainfall", chart.Options.MapColorDimension.Name);
        Assert.Same(format, chart.Options.HeatmapColorDimension.ValueFormat);
        Assert.Equal("Margin", chart.Options.Treemap.ColorDimension.Name);
        Assert.Equal("Risk", chart.Options.Sunburst.ColorDimension.Name);
        series.ConfigureColorDimension(value => value.Name = null);
        Assert.Null(series.ColorDimension.Name); Assert.Same(format, series.ColorDimension.ValueFormat);
        series.ConfigureColorDimension(value => { value.Name = "Temperature"; value.ValueFormat = null; });
        Assert.Equal("Temperature", series.ColorDimension.Name); Assert.Null(series.ColorDimension.ValueFormat);
        Assert.Throws<ArgumentNullException>(() => series.ConfigureColorDimension(null!));
        Assert.Throws<ArgumentNullException>(() => chart.ConfigureMapColorDimension(null!));
        Assert.Throws<ArgumentNullException>(() => chart.ConfigureHeatmapColorDimension(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    public void NonnullNamesMustBeReadable(string name) {
        var options = new ChartColorDimensionOptions { Name = "Temperature" };
        Assert.Throws<ArgumentException>(() => options.Name = name);
        Assert.Equal("Temperature", options.Name);
        options.Name = null; Assert.Null(options.Name);
    }

    [Theory]
    [InlineData("series")]
    [InlineData("map")]
    [InlineData("heatmap")]
    [InlineData("treemap")]
    [InlineData("sunburst")]
    public void ExplicitPresentationRejectsUnsupportedFamiliesAtPreparation(string owner) {
        var chart = Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });
        if (owner == "series") {
            var invoked = false;
            Assert.Throws<InvalidOperationException>(() => chart.Series[0].ConfigureColorDimension(_ => invoked = true));
            Assert.False(invoked);
        }
        var options = owner switch {
            "series" => chart.Series[0].ColorDimension, "map" => chart.Options.MapColorDimension,
            "heatmap" => chart.Options.HeatmapColorDimension, "treemap" => chart.Options.Treemap.ColorDimension,
            _ => chart.Options.Sunburst.ColorDimension
        };
        options.Name = "Measurement";
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        options.Name = null;
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.NotNull(Prepare(Chart.Create().AddLine("Unconfigured", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) })));
    }

    [Fact]
    public void CategoricalHeatmapCannotAcquireNumericColorPresentation() {
        var chart = Chart.Create().AddHeatmapCategoryRow("State", new ChartHeatmapCell("pass"));
        chart.Options.StateCategories.Add(new ChartStateCategory("pass", "Passed", ChartColor.Black));
        chart.ConfigureHeatmapColorDimension(options => options.ValueFormat = ChartValueFormat.Number());
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        chart.Options.HeatmapColorDimension.ValueFormat = null;
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }
}
