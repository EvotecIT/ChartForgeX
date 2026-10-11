using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;
using static ChartForgeX.Tests.ScatterBubbleColorRenderingTests;

namespace ChartForgeX.Tests;

/// <summary>Color presentation has owned callback options and clear numeric-family applicability.</summary>
public sealed class ColorDimensionApiTests {
    [Fact]
    public void ConfigurationEditsExistingOwnedOptionsAndResetsEachPolicyIndependently() {
        var chart = Chart.Create().AddScatter("Samples", new[] { new ChartPoint(1, 2) });
        var series = chart.Series[0];
        var options = new[] { series.ColorDimension, chart.Options.MapColorDimension, chart.Options.HeatmapColorDimension,
            chart.Options.Treemap.ColorDimension, chart.Options.Sunburst.ColorDimension };
        Assert.Equal(options.Length, options.Distinct().Count());
        var format = ChartValueFormat.Number("0.0");
        Assert.Same(series, series.ConfigureColorDimension(value => { Assert.Same(options[0], value); value.Name = "Temperature"; value.ValueFormat = format; }));
        Assert.Same(chart, chart.ConfigureMapColorDimension(value => { Assert.Same(options[1], value); value.Name = "Rainfall"; }));
        Assert.Same(chart, chart.ConfigureHeatmapColorDimension(value => { Assert.Same(options[2], value); value.ValueFormat = format; }));
        Assert.Same(chart, chart.ConfigureTreemap(value => { Assert.Same(options[3], value.ColorDimension); value.ColorDimension.Name = "Margin"; }));
        Assert.Same(chart, chart.ConfigureSunburst(value => { Assert.Same(options[4], value.ColorDimension); value.ColorDimension.Name = "Risk"; }));
        series.ConfigureColorDimension(value => value.Name = null);
        Assert.Null(options[0].Name); Assert.Same(format, options[0].ValueFormat);
        series.ConfigureColorDimension(value => { value.Name = "Temperature"; value.ValueFormat = null; });
        Assert.Equal("Temperature", options[0].Name); Assert.Null(options[0].ValueFormat);
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
        Assert.NotNull(Prepare(chart));
    }

    [Fact]
    public void CategoricalHeatmapCannotAcquireNumericColorPresentation() {
        var chart = Chart.Create().AddHeatmapCategoryRow("State", new ChartHeatmapCell("pass"));
        chart.Options.StateCategories.Add(new ChartStateCategory("pass", "Passed", ChartColor.Black));
        chart.ConfigureHeatmapColorDimension(options => options.ValueFormat = ChartValueFormat.Number());
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        chart.Options.HeatmapColorDimension.ValueFormat = null;
        Assert.NotNull(Prepare(chart));
    }
}
