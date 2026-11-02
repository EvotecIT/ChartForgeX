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
    [Fact]
    public void AutomaticSecondBoundaryUsesAnUnoccupiedPaletteWithoutRecoloringOrdinarySeries() {
        var green = ChartColor.FromHex("#189060");
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite().WithPalette(Red, Blue, green))
            .AddDifferenceArea("Comparison", new[] { new ChartDifferencePoint(0, 8, 2), new ChartDifferencePoint(1, 2, 5) })
            .AddLine("Other", new[] { new ChartPoint(0, 3), new ChartPoint(1, 4) });
        var prepared = Prepare(chart);
        Assert.Equal(Red, Boundary(prepared, "difference-first").Stroke);
        Assert.Equal(green, Boundary(prepared, "difference-second").Stroke);
        Assert.Equal(Blue, Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "line").Stroke);
        var fill = Boundary(prepared, "difference-negative").Fill!.Value;
        Assert.Equal((green.R, green.G, green.B), (fill.R, fill.G, fill.B));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExhaustedPalettesDistinguishTheSecondBoundaryAndItsLegendWithDashes(bool twoColors) {
        var chart = Chart.Create().WithLegend().WithTheme(VisualTheme.Graphite().WithPalette(twoColors ? new[] { Red, Blue } : new[] { Red }))
            .AddDifferenceArea("Comparison", new[] { new ChartDifferencePoint(0, 8, 2), new ChartDifferencePoint(1, 2, 5) })
            .AddLine("Other", new[] { new ChartPoint(0, 3), new ChartPoint(1, 4) });
        var document = System.Xml.Linq.XDocument.Parse(Prepare(chart).ToSvg());
        var second = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "difference-second");
        var lines = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "legend-line").ToArray();
        var dash = (string?)second.Attribute("stroke-dasharray");
        Assert.False(string.IsNullOrEmpty(dash));
        Assert.Equal(dash, (string?)lines[1].Attribute("stroke-dasharray"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DuplicateRgbPaletteSlotsRetainTheSelectedSecondBoundaryToken(bool explicitFirst) {
        var palette = new[] { Red, Blue, Red };
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite().WithPalette(palette))
            .AddLine("Other", new[] { new ChartPoint(0, 3), new ChartPoint(1, 4) }, ChartColor.FromHex("#606060"))
            .AddDifferenceArea("Comparison", new[] { new ChartDifferencePoint(0, 8, 2), new ChartDifferencePoint(1, 2, 5) });
        if (explicitFirst) chart.Series[1].WithColor(Blue);
        var variables = new VisualDesignTokens { Palette = palette }.ToSvgColorVariables();
        var svg = Prepare(chart).ToSvg(new VisualSvgOptions(colorVariables: variables));
        var second = System.Xml.Linq.XDocument.Parse(svg).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "difference-second");
        Assert.Contains("var(--cfx-series-3,", (string?)second.Attribute("stroke"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutomaticSecondBoundariesReserveOtherPairsColors(bool authoredSecond) {
        var green = ChartColor.FromHex("#189060"); var purple = ChartColor.FromHex("#8450C0");
        var pairs = new[] { new ChartDifferencePoint(0, 8, 2), new ChartDifferencePoint(1, 2, 5) };
        var chart = Chart.Create().WithTheme(VisualTheme.Graphite().WithPalette(Red, Blue, green, purple))
            .AddDifferenceArea("North", pairs).AddDifferenceArea("South", pairs);
        if (authoredSecond) chart.Series[0].DifferenceArea.NegativeFill = green;
        var prepared = Prepare(chart);
        var first = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "difference-first").ToArray();
        var second = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "difference-second").ToArray();
        Assert.Equal(new ChartColor?[] { Red, Blue }, first.Select(node => node.Stroke));
        Assert.Equal(new ChartColor?[] { green, purple }, second.Select(node => node.Stroke));
    }

    [Fact]
    public void MultipleDifferenceLegendsIncludeTheirOwningSeriesNames() {
        var pairs = new[] { new ChartDifferencePoint(0, 8, 2), new ChartDifferencePoint(1, 2, 5) };
        var chart = Chart.Create().WithLegend().AddDifferenceArea("North", pairs).AddDifferenceArea("South", pairs);
        var svg = Prepare(chart).ToSvg();
        foreach (var series in new[] { "North", "South" }) foreach (var boundary in new[] { "First", "Second" })
            Assert.Contains(">" + series + ": " + boundary + "<", svg);
    }
}
