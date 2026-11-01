using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class DifferenceAreaTests {
    [Fact]
    public void DirectionPaintMultipliesAlphaOnceAndKeepsOnePatternedLegendTarget() {
        var chart = Model(new(0, 8, 2), new(1, 2, 5)).WithLegend();
        var series = chart.Series[0];
        series.WithStrokeWidth(1).ConfigureMarkers(markers => markers.Enabled = false);
        series.DifferenceArea.PositiveFill = Red.WithAlpha(128);
        series.DifferenceArea.NegativeFillOpacity = 0;
        var plain = Prepare(chart);
        var positive = Boundary(plain, "difference-positive");
        Assert.Equal(64, positive.Fill!.Value.A);
        Assert.Equal(0, Boundary(plain, "difference-negative").Fill!.Value.A);
        var first = Boundary(plain, "difference-first"); var second = Boundary(plain, "difference-second");
        var image = plain.ToRgba(new VisualRenderOptions(supersampling: 1));
        AssertPaint(image, Sample(first, second, .25), Red, 64);
        var invisible = Sample(first, second, .85);
        Assert.Equal(0, Pixel(image, invisible.X, invisible.Y).A);

        series.FillPattern = ChartFillPattern.Crosshatch;
        var patterned = Prepare(chart);
        Assert.Contains(patterned.Scene.Nodes, node => node.Role == "difference-positive-pattern");
        Assert.DoesNotContain(patterned.Scene.Nodes, node => node.Role == "difference-negative-pattern");
        Assert.DoesNotContain(patterned.Scene.Nodes, node => node.Role is "difference-marker" or "legend-swatch");
        var halves = patterned.Scene.Nodes.OfType<VisualSceneRectangle>()
            .Where(node => node.Role is "legend-difference-positive" or "legend-difference-negative").ToArray();
        Assert.Equal(2, halves.Length);
        Assert.Equal(64, halves.Single(node => node.Role == "legend-difference-positive").Fill!.Value.A);
        Assert.Equal(0, halves.Single(node => node.Role == "legend-difference-negative").Fill!.Value.A);
        Assert.Equal(2, patterned.Scene.Nodes.OfType<VisualSceneGroup>().Count(node => node.Role == "legend-entry"));
        Assert.Contains(patterned.Scene.Nodes, node => node.Role == "legend-area-pattern");
        // Hatching must change actual native swatch pixels even though glyphs are disabled.
        var swatch = halves.Single(node => node.Role == "legend-difference-positive").Bounds;
        var patternedImage = patterned.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Contains(Enumerable.Range((int)Math.Ceiling(swatch.Top), (int)swatch.Height), y =>
            Enumerable.Range((int)Math.Ceiling(swatch.Left), (int)swatch.Width).Any(x =>
                !Pixel(image, x, y).Equals(Pixel(patternedImage, x, y))));
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void BoundariesKeepDistinctColoursAndTheLegendNamesBothObservations(VisualThemeMode mode) {
        var chart = Chart.Create().WithSize(600, 360).WithHeader(false).WithTheme(VisualTheme.Graphite(), mode)
            .AddDifferenceArea("Requests", new[] { new ChartDifferencePoint(0, 8, 2), new ChartDifferencePoint(1, 2, 5), new ChartDifferencePoint(2, 6, 4) });
        chart.Series[0].ConfigureDifferenceArea(options => { options.FirstName = "Actual"; options.SecondName = "Forecast"; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var first = Boundary(prepared, "difference-first").Stroke!.Value; var second = Boundary(prepared, "difference-second").Stroke!.Value;
        Assert.NotEqual((first.R, first.G, first.B), (second.R, second.G, second.B));
        // The shading where Second leads takes Second's colour, so the legend swatch, line and area agree.
        var negative = prepared.Scene.Nodes.OfType<VisualScenePath>().First(node => node.Role == "difference-negative").Fill!.Value;
        Assert.Equal((second.R, second.G, second.B), (negative.R, negative.G, negative.B));
        var entries = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "legend-entry").ToArray();
        Assert.Equal(2, entries.Length);
        var svg = prepared.ToSvg();
        Assert.Contains(">Actual<", svg); Assert.Contains(">Forecast<", svg);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}