using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HistogramLabelHeadroomTests {
    [Theory]
    [InlineData(VisualThemeMode.Light, 3d)]
    [InlineData(VisualThemeMode.Dark, 3d)]
    [InlineData(VisualThemeMode.Light, 3.5d)]
    [InlineData(VisualThemeMode.Dark, 3.5d)]
    [InlineData(VisualThemeMode.Light, null)]
    [InlineData(VisualThemeMode.Dark, null)]
    public void CompactHistogramKeepsCompleteCountsAboveNarrowMaximumHeightBins(VisualThemeMode mode, double? maximum) {
        var chart = HistogramRecipe.Create();
        if (maximum.HasValue) chart.Options.YAxis.WithBounds(0, maximum.Value);
        var prepared = chart.Prepare(Context(mode));
        ScatterBubbleColorRenderingTests.Capture(prepared, "histogram-complete-counts-" + mode + "-" + (maximum?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "auto"));
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert.Equal(new[] { "3 obs", "3 obs", "0 obs", "3 obs" }, labels.Select(node => Assert.Single(node.Text.Lines).Text));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        var firstBin = Assert.Single(prepared.Regions, region => region.Id == "series-0-point-0");
        if (maximum == 3) Assert.Equal(plot.Top, firstBin.Bounds.Top, 6);
        Assert.True(labels[0].Baseline < firstBin.Bounds.Top, "The complete caption needs a measured lane above its narrow peak bin.");
        Assert.All(labels, label => Assert.Equal(13.5, label.Text.Size));
        Assert.Equal(9, chart.Series[0].SourcePointCount);
        var png = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(400, png.Width); Assert.Equal(280, png.Height);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SignedNearBoundaryCaptionsReserveBothLanesAfterFinalMapping(bool reversed, bool secondary) {
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithDataLabels()
            .AddBar("Upper", new[] { new ChartPoint(1, 90) }).AddBar("Lower", new[] { new ChartPoint(1, -90) });
        var axis = secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        axis.WithBounds(-100, 100).WithReversal(reversed);
        for (var index = 0; index < 2; index++) {
            var series = chart.Series[index];
            if (secondary) series.UseSecondaryValueAxis();
            series.WithPointLabel(0, index == 0 ? "Upper edge" : "Lower edge");
            series.DataLabelPlacement = (index == 0) != reversed ? ChartDataLabelPlacement.Above : ChartDataLabelPlacement.Below;
        }
        var prepared = chart.Prepare(Context(VisualThemeMode.Light));
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert.Equal(new[] { "Upper edge", "Lower edge" }, labels.Select(label => Assert.Single(label.Text.Lines).Text));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        for (var index = 0; index < labels.Length; index++) {
            var mark = Assert.Single(prepared.Regions, region => region.Id == "series-" + index + "-point-0").Bounds;
            var label = labels[index];
            if ((index == 0) != reversed) Assert.True(label.Baseline - label.Text.Ascent + label.Text.Metrics.Height < mark.Top);
            else Assert.True(label.Baseline - label.Text.Ascent > mark.Bottom);
        }
        Assert.Equal(-100, axis.Minimum); Assert.Equal(100, axis.Maximum);
        Assert.Contains("Upper edge", prepared.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void ExplicitInsideLabelsDoNotReserveAnOutsideLane() {
        var chart = HistogramRecipe.Create().WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        var inside = chart.Prepare(Context(VisualThemeMode.Light));
        chart.WithDataLabels(false);
        var hidden = chart.Prepare(Context(VisualThemeMode.Light));
        Assert.Equal(Assert.Single(hidden.Regions, region => region.Role == "cartesian-plot").Bounds,
            Assert.Single(inside.Regions, region => region.Role == "cartesian-plot").Bounds);
    }

    [Fact]
    public void MeasuringBarCaptionLanesDoesNotRepeatFormattingDuringPaintingOrDetachedExports() {
        var calls = 0;
        var chart = Chart.Create().WithAxes(false).WithDataLabels()
            .AddBar("Requests", ChartPoints.FromValues(3, 4))
            .WithValueFormat(ChartValueFormat.Custom(value => { calls++; return value + " requests"; }));
        var prepared = chart.Prepare(Context(VisualThemeMode.Light));
        Assert.Equal(2, calls);
        chart.WithValueFormat(ChartValueFormat.Custom(_ => throw new InvalidOperationException("The source callback must stay detached.")));
        Assert.Contains("4 requests", prepared.ToSvg(), StringComparison.Ordinal);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(2, calls);
    }

    private static VisualRenderContext Context(VisualThemeMode mode) {
        const string family = "CFX Histogram Caption Carlito";
        var fonts = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito");
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Regular.ttf"), 400);
        FontRegistry.Register(family, Path.Combine(fonts, "Carlito-Bold.ttf"), 700);
        return new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 280), 16),
            VisualTheme.Graphite().WithTypography(new VisualTypography(axisSize: 13.5, legendSize: 13.5, dataLabelSize: 13.5)),
            mode, new VisualFrame("", "", showLegend: true), FontSpec.FromFamily(family));
    }
}
