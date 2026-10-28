using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Relative contour measurement retains positive areas independently of authored coordinate scale.</summary>
public sealed class CustomMarkerMeasurementTests {
    [Theory]
    [InlineData(1e-5)] [InlineData(1e-150)] [InlineData(1e-200)]
    public void UniformlyScaledCurvesRetainReferenceAreaAndOriginalExportedCubicGeometry(double scale) {
        var unit = Lens(1); var scaled = Lens(scale); var commands = scaled.Commands.ToArray();
        Assert.Equal(unit.CircleAreaRadiusFactor, scaled.CircleAreaRadiusFactor * scale, 10);
        Assert.Equal(commands, scaled.Commands);
        var reference = Prepare(unit); var actual = Prepare(scaled);
        var expectedPath = Assert.Single(reference.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "bubble");
        var path = Assert.Single(actual.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "bubble");
        Assert.Equal(2, path.Commands.Count(command => command.Kind == ChartPathCommandKind.CubicTo));
        for (int index = 0; index < path.Commands.Count; index++) {
            Assert.Equal(expectedPath.Commands[index].Kind, path.Commands[index].Kind);
            Assert.Equal(expectedPath.Commands[index].X, path.Commands[index].X, 8); Assert.Equal(expectedPath.Commands[index].Y, path.Commands[index].Y, 8);
            Assert.Equal(expectedPath.Commands[index].Control1X, path.Commands[index].Control1X, 8); Assert.Equal(expectedPath.Commands[index].Control1Y, path.Commands[index].Control1Y, 8);
            Assert.Equal(expectedPath.Commands[index].Control2X, path.Commands[index].Control2X, 8); Assert.Equal(expectedPath.Commands[index].Control2Y, path.Commands[index].Control2Y, 8);
        }
        var image = RasterImageDecoder.Decode(actual.ToPng(new VisualRenderOptions(supersampling: 2)));
        var area = image.Pixels.Where((_, index) => index % 4 == 3).Sum(alpha => (double)alpha) / 255;
        Assert.InRange(area, Math.PI * 400 * .975, Math.PI * 400 * 1.025);
        ScatterBubbleColorRenderingTests.Capture(actual, "custom-marker-relative-lens-" + scale.ToString("0e0", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void TinyHolesAndCrossingsRetainTheirEvenOddAreasWithoutSquaringTheAuthoredExtent(bool crossing) {
        const double scale = 1e-200;
        var shape = crossing ? ChartMarkerPath.Start(-scale, -scale).LineTo(scale, scale).LineTo(-scale, scale).LineTo(scale, -scale)
            : ChartMarkerPath.Start(-scale, -scale).LineTo(scale, -scale).LineTo(scale, scale).LineTo(-scale, scale)
                .MoveTo(-scale / 2, -scale / 2).LineTo(scale / 2, -scale / 2).LineTo(scale / 2, scale / 2).LineTo(-scale / 2, scale / 2);
        Assert.Equal(Math.Sqrt(Math.PI / (crossing ? 2 : 3)), shape.CircleAreaRadiusFactor * scale, 10);
        var original = shape.Commands.ToArray(); Assert.NotEmpty(Prepare(shape).ToPng()); Assert.Equal(original, shape.Commands);
    }

    [Fact]
    public void TranslationAndSubnormalPositiveBandsKeepFiniteAreaFactorsAndEmptyContoursStillFail() {
        var translated = Lens(1e-5, .25); var centered = Lens(1e-5);
        Assert.InRange(Math.Abs(translated.CircleAreaRadiusFactor / centered.CircleAreaRadiusFactor - 1), 0, 1e-8);
        foreach (var height in new[] { 1e-320, double.Epsilon }) {
            var rectangle = ChartMarkerPath.Start(0, 0).LineTo(1, 0).LineTo(1, height).LineTo(0, height);
            Assert.True(double.IsFinite(rectangle.CircleAreaRadiusFactor));
            Assert.Equal(Math.Sqrt(Math.PI), rectangle.CircleAreaRadiusFactor * Math.Sqrt(height), 10);
        }
        Assert.Throws<InvalidOperationException>(() => { _ = ChartMarkerPath.Start(0, 0).CircleAreaRadiusFactor; });
        Assert.Throws<InvalidOperationException>(() => { _ = ChartMarkerPath.Start(-1, 0).LineTo(1, 0).CircleAreaRadiusFactor; });
    }

    private static ChartMarkerPath Lens(double scale, double offset = 0) => ChartMarkerPath.Start(offset, -scale)
        .CubicTo(offset + scale, -scale, offset + scale, scale, offset, scale)
        .CubicTo(offset - scale, scale, offset - scale, -scale, offset, -scale);
    private static PreparedVisual Prepare(ChartMarkerPath path) {
        var chart = Chart.Create().WithSize(400, 300).WithHeader(false).WithLegend(false).WithDataLabels(false).WithAxes(false).WithGrid(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).AddBubble("Area", new[] { new ChartBubble(5, 5, 100) })
            .ConfigureBubble(options => { options.WithSizeDomain(0, 100); options.MinimumRadius = 0; options.MaximumRadius = 20; options.SizeEncoding = ChartBubbleSizeEncoding.Area; });
        chart.Series[0].ConfigureMarkers(options => { options.CustomPath = path; options.Fill = ChartColor.Black; options.StrokeWidth = 0; });
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = true;
        return chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    }
}
