using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PlotClippingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutOfDomainSeriesInkStaysInThePlot(bool svg) {
        var chart = CreateChart().AddArea("outside", new[] { new ChartPoint(-5, -5), new ChartPoint(5, 15), new ChartPoint(15, -5) }, ChartColors.Red);
        var image = svg ? SvgRasterizer.ToImage(chart.ToSvg()) : chart.ToRgbaImage();
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) {
            if (x >= 40 && x < 200 && y >= 40 && y < 160) continue;
            Assert.Equal(0, image.Pixels[(y * image.Width + x) * 4 + 3]);
        }
        chart.WithPlotClipping(false);
        image = svg ? SvgRasterizer.ToImage(chart.ToSvg()) : chart.ToRgbaImage();
        Assert.Contains(Enumerable.Range(0, 30), y => image.Pixels[(y * image.Width + 120) * 4 + 3] > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EdgeMarkersSurviveButOutsideCentersAreDropped(bool svg) {
        var chart = CreateChart().AddScatter("edge", new[] { new ChartPoint(0, 5), new ChartPoint(-1, 5) }, ChartColors.Red);
        var image = svg ? SvgRasterizer.ToImage(chart.ToSvg()) : chart.ToRgbaImage();
        Assert.True(image.Pixels[(100 * image.Width + 38) * 4 + 3] > 0);
        Assert.Equal(0, image.Pixels[(100 * image.Width + 24) * 4 + 3]);
        var document = XDocument.Parse(chart.ToSvg());
        Assert.Single(document.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "scatter-point");
    }

    private static Chart CreateChart() {
        var chart = Chart.Create().WithSize(240, 200).WithPadding(40, 40, 40, 40)
            .WithHeader(false).WithCard(false).WithPlotBackground(false).WithLegend(false)
            .WithTransparentBackground().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false; chart.Options.Theme.MarkerRadius = 0;
        return chart;
    }
}
