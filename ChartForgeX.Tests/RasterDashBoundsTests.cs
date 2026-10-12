using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RasterDashBoundsTests {
    [Theory]
    [InlineData("round", 5, 6, 0, true)]
    [InlineData("square", 5, 6, 0, true)]
    [InlineData("butt", 5, 6, 0, false)]
    [InlineData("round", 5, 6, 4.9, true)]
    [InlineData("round", 5, 6, 5, false)]
    [InlineData("round", 5, 6, 6, false)]
    [InlineData("square", 5, 6, 6, false)]
    [InlineData("round", 5, 6, -1, false)]
    [InlineData("round", 5, 6, 11, true)]
    [InlineData("round", 0, 6, 0, true)]
    [InlineData("round", 0, 6, 1, false)]
    [InlineData("square", 0, 6, 0, true)]
    [InlineData("butt", 0, 6, 0, false)]
    [InlineData("round", .0001, .0002, 0, true)]
    [InlineData("square", .0001, .0002, 0, true)]
    [InlineData("round", .0001, .0002, .00015, false)]
    [InlineData("square", .0001, .0002, .00015, false)]
    [InlineData("round", .0001, 0, .00015, true)]
    [InlineData("square", .0001, 0, .00015, true)]
    public void ZeroLengthDashedSubpathsKeepTheirOwnCapAndInitialPhase(string cap, double dash, double gap, double phase, bool painted) {
        foreach (var points in new[] { new[] { new ChartPoint(40, 40) }, new[] { new ChartPoint(40, 40), new ChartPoint(40, 40) } }) {
            var canvas = new RgbaCanvas(80, 80, 1);
            canvas.StrokePolylines(new[] { points }, ChartColors.Black, 8, (RasterLineCap)Enum.Parse(typeof(RasterLineCap), cap, true),
                RasterLineJoin.Miter, new[] { dash, gap }, dashOffset: phase);
            Assert.Equal(painted, canvas.Pixels[(40 * 80 + 40) * 4 + 3] > 0);
        }
        string Number(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        var image = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='80' height='80'>"
            + "<path d='M40 40 L40 40' fill='none' stroke='black' stroke-width='8' stroke-linecap='" + cap
            + "' stroke-dasharray='" + Number(dash) + " " + Number(gap) + "' stroke-dashoffset='" + Number(phase) + "'/></svg>");
        Assert.Equal(painted, image.Pixels[(40 * 80 + 40) * 4 + 3] > 0);
    }

    [Fact]
    public void UnrepresentableMixedEntriesKeepZeroEntryCapContracts() {
        byte[] Render(double[] pattern) {
            var canvas = new RgbaCanvas(80, 30, 1);
            canvas.StrokePolylines(new[] { new[] { new ChartPoint(4, 15), new ChartPoint(76, 15) } },
                ChartColors.Black, 2, RasterLineCap.Round, RasterLineJoin.Miter, pattern);
            return canvas.Pixels;
        }
        Assert.Equal(Render(new[] { 0d, 8d }), Render(new[] { 1e-20, 8d }));
        Assert.Equal(Render(new[] { 8d, 0d }), Render(new[] { 8d, 1e-20 }));
    }

    [Theory]
    [InlineData(0.0001, 0.0001, .5)]
    [InlineData(0.00001, 0.00009, .1)]
    [InlineData(1e-20, 1e-20, .5)]
    public void SubpixelButtDashesRetainTheirPaintedFraction(double dash, double gap, double duty) {
        var pattern = dash.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " "
            + gap.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        var image = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='120' height='40'>"
            + "<path d='M10 20 H110' fill='none' stroke='black' stroke-width='4' stroke-linecap='butt' stroke-dasharray='" + pattern + "'/></svg>");
        foreach (int x in Enumerable.Range(20, 80))
            Assert.InRange(image.Pixels[(20 * 120 + x) * 4 + 3], (int)(255 * duty) - 4, (int)(255 * duty) + 4);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void SubpixelOddManyEntryPatternsBoundWideHighDpiClosedStrokes(int scale) {
        var points = new[] { new ChartPoint(0, 0), new ChartPoint(1200 * scale, 0),
            new ChartPoint(1200 * scale, 100 * scale), new ChartPoint(0, 100 * scale), new ChartPoint(0, 0) };
        var pattern = Enumerable.Repeat(.0001 * scale, 65).ToArray();
        var runs = RasterStroker.Dash(points, pattern, -20 * scale, -20 * scale, 1220 * scale, 120 * scale, double.MaxValue % (.013 * scale));
        Assert.NotEmpty(runs);
        Assert.InRange(runs.Count, 1, 50000);
        var image = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='" + 1200 * scale
            + "' height='" + 100 * scale + "' viewBox='0 0 1200 100'><path d='M10 50 H1190' fill='none' stroke='black'"
            + " stroke-width='2' stroke-dasharray='" + string.Join(" ", Enumerable.Repeat(".0001", 65)) + "'/></svg>");
        Assert.Equal(1200 * scale, image.Width);
        Assert.Equal(100 * scale, image.Height);
        double meanAlpha = Enumerable.Range(100 * scale, 1000 * scale)
            .Average(x => image.Pixels[(50 * scale * image.Width + x) * 4 + 3]);
        Assert.InRange(meanAlpha, 120, 135);
    }

    [Fact]
    public void ExactRepeatedPatternsRetainOddParityZeroCapsAndOffset() {
        byte[] Render(double[] pattern) {
            var canvas = new RgbaCanvas(80, 40, 1);
            canvas.StrokePolylines(new[] { new[] { new ChartPoint(-10, 10), new ChartPoint(70, 10),
                new ChartPoint(70, 30), new ChartPoint(-10, 30), new ChartPoint(-10, 10) } },
                ChartColors.Black, 3, RasterLineCap.Round, RasterLineJoin.Miter, pattern, dashOffset: -17.25);
            return canvas.Pixels;
        }
        foreach (var primitive in new[] { new[] { 0d, 8d }, new[] { 3d, 1d, 2d } })
            Assert.Equal(Render(primitive), Render(Enumerable.Range(0, 3).SelectMany(_ => primitive).ToArray()));
    }

    [Fact]
    public void ExcessiveResolvedDashGeometryStillKeepsItsResourceGuard() {
        var points = new[] { new ChartPoint(0, 0), new ChartPoint(200002, 0) };
        Assert.Throws<InvalidOperationException>(() => RasterStroker.Dash(points, new[] { 1d, 1d }, -1, -1, 200003, 1));
        var repeated = Enumerable.Range(0, 100002).Select(index => new ChartPoint(index % 2 == 0 ? 0 : 100, 10)).ToArray();
        Assert.Throws<InvalidOperationException>(() => RasterStroker.CreateOutline(new[] { repeated }, 2,
            RasterLineCap.Round, RasterLineJoin.Miter, 4, new[] { .0001, .0001 }, 0, 1, 0, 0, 120, 40));
    }

    [Theory]
    [InlineData("round", .1)]
    [InlineData("square", .1)]
    [InlineData("round", 2)]
    [InlineData("square", 2)]
    [InlineData("round", .01)]
    [InlineData("square", .01)]
    [InlineData("round", .0001)]
    public void DenseLowDutyCapsKeepTheirOverlappingEnvelope(string cap, double width) {
        string Svg(string dash) => "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='40'>"
            + "<path d='M10 20.5 H110' fill='none' stroke='black' stroke-width='"
            + width.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "' stroke-linecap='" + cap + "'" + dash + "/></svg>";
        // Infinitesimal repeated caps overlap to the solid segment envelope, irrespective of duty.
        var actual = SvgRasterizer.ToImage(Svg(" stroke-dasharray='1e-23 9.99e-21'"));
        var expected = SvgRasterizer.ToImage(Svg(""));
        for (int i = 3; i < actual.Pixels.Length; i += 4)
            Assert.InRange(Math.Abs(actual.Pixels[i] - expected.Pixels[i]), 0, 1);
    }

    [Theory]
    [InlineData("butt", .00005, 255)]
    [InlineData("round", .00005, 255)]
    [InlineData("butt", .00015, 0)]
    [InlineData("round", .00015, 0)]
    public void DenseOddPatternKeepsOriginalJoinAndClosedSeamPhase(string cap, double phase, int alpha) {
        var image = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='120' height='100'>"
            + "<path d='M20 20 H100 V80 H20 Z' fill='none' stroke='black' stroke-width='10' stroke-linecap='" + cap
            + "' stroke-linejoin='miter' stroke-dasharray='.0001 .0002 .0001' stroke-dashoffset='"
            + phase.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "'/></svg>");
        // This miter corner lies beyond the two segment bodies and their round caps.
        Assert.Equal(alpha, image.Pixels[(15 * 120 + 15) * 4 + 3]);
        Assert.Equal(alpha, image.Pixels[(15 * 120 + 104) * 4 + 3]);
    }

    [Fact]
    public void SvgWideStrokeBleedUsesItsBoundedVisibleGeometry() {
        var result = SvgRasterizer.Rasterize("<svg xmlns='http://www.w3.org/2000/svg' width='1048576' height='1'><path d='M0 .5 H2048' fill='none' stroke='#000' stroke-width='1048576' stroke-dasharray='1 1'/></svg>");
        Assert.Empty(result.Diagnostics);
        Assert.Equal(4 * 1048576, result.Image.Pixels.Length);
        Assert.True(result.Image.Pixels[3] > 0);
        Assert.Equal(0, result.Image.Pixels[(1048576 - 1) * 4 + 3]);
    }

    [Theory]
    [InlineData(64, 1)]
    [InlineData(256, 1)]
    [InlineData(1024, 1)]
    [InlineData(1024, 2)]
    [InlineData(1024, 4)]
    public void ThickAcceptedDashesShareOnePaintedGeometryBudget(double width, int density) {
        var canvas = new RgbaCanvas(30000, 4, density);
        canvas.StrokePolylines(new[] { new[] { new ChartPoint(0, 2), new ChartPoint(30000, 2) } },
            ChartColors.Black, width, RasterLineCap.Butt, RasterLineJoin.Miter, new[] { .25, .25 });
        Assert.Contains(canvas.Pixels, value => value > 0);
    }

    [Fact]
    public void SvgThickDashesUseTheSamePaintedGeometryBudget() {
        var result = SvgRasterizer.Rasterize("<svg xmlns='http://www.w3.org/2000/svg' width='30000' height='4'><path d='M0 2 H30000' fill='none' stroke='#000' stroke-width='1024' stroke-dasharray='.25 .25'/></svg>");
        Assert.Empty(result.Diagnostics);
        Assert.Contains(result.Image.Pixels, value => value > 0);
    }

    [Fact]
    public void ResolvedDashWorkLimitAppliesAcrossWideAndMultipleContours() {
        var canvas = new RgbaCanvas(300000, 4, 1);
        var wide = new[] { new[] { new ChartPoint(0, 2), new ChartPoint(300000, 2) } };
        Assert.Throws<InvalidOperationException>(() => canvas.StrokePolylines(wide, ChartColors.Black, 1,
            RasterLineCap.Butt, RasterLineJoin.Miter, new[] { .05, .05 }));
        canvas = new RgbaCanvas(3000, 8, 1);
        var lines = Enumerable.Range(1, 5).Select(y => (System.Collections.Generic.IReadOnlyList<ChartPoint>)new[] { new ChartPoint(0, y), new ChartPoint(3000, y) }).ToArray();
        Assert.Throws<InvalidOperationException>(() => canvas.StrokePolylines(lines, ChartColors.Black, 1,
            RasterLineCap.Butt, RasterLineJoin.Miter, new[] { .05, .05 }));
    }

    [Fact]
    public void SvgSubpixelStrokesRetainTheResolvedGeometryLimit() => Assert.Throws<NotSupportedException>(() =>
        SvgRasterizer.Rasterize("<svg xmlns='http://www.w3.org/2000/svg' width='300000' height='4'><path d='M0 2 H300000' fill='none' stroke='#000' stroke-width='1' stroke-dasharray='.05 .05'/></svg>"));

    [Fact]
    public void AcceptedQuarterPixelDashesRenderOnALargeValidCanvas() {
        var canvas = new RgbaCanvas(30000, 4, 1);
        canvas.StrokePolylines(new[] { new[] { new ChartPoint(0, 2), new ChartPoint(30000, 2) } },
            ChartColors.Black, 1, RasterLineCap.Butt, RasterLineJoin.Miter, new[] { .25, .25 });
        Assert.Contains(canvas.Pixels, value => value > 0);
    }

    [Fact]
    public void TileBoundariesPreserveDashPhaseAndTranslucentStrokeUnions() {
        var actual = new RgbaCanvas(4200, 12, 1);
        var color = new ChartColor(11, 40, 90, 128);
        var line = new[] { new ChartPoint(-5.13, 6), new ChartPoint(4210, 6) };
        actual.StrokePolylines(new[] { line, line }, color, 2.5, RasterLineCap.Round, RasterLineJoin.Miter, new[] { 3.3, 4.7 });
        foreach (var boundary in new[] { 2048, 4096 }) {
            var left = boundary - 20;
            var expected = new RgbaCanvas(40, 12, 1);
            var translated = new[] { new ChartPoint(-5.13 - left, 6), new ChartPoint(4210 - left, 6) };
            expected.StrokePolylines(new[] { translated, translated }, color, 2.5, RasterLineCap.Round, RasterLineJoin.Miter, new[] { 3.3, 4.7 });
            for (var y = 0; y < 12; y++) for (var x = 0; x < 40; x++) {
                var alpha = actual.Pixels[(y * 4200 + left + x) * 4 + 3];
                Assert.InRange(alpha, 0, 128);
                Assert.InRange(System.Math.Abs(alpha - expected.Pixels[(y * 40 + x) * 4 + 3]), 0, 1);
            }
        }
    }

    [Fact]
    public void UntrustedSubpixelDashesResolveWithinABoundedOutline() {
        var canvas = new RgbaCanvas(120, 4, 1);
        var line = new[] { new[] { new ChartPoint(0, 2), new ChartPoint(120, 2) } };
        var outline = RasterStroker.CreateOutline(line, 1, RasterLineCap.Butt, RasterLineJoin.Miter, 4,
            new[] { .00001, .00001 }, 0, 1, 0, 0, 120, 4);
        // One period per eighth pixel plus the two clipped boundaries, with four vertices per butt dash.
        Assert.InRange(outline.Sum(contour => contour.Count), 1, 4 * (120 * 8 + 2));
        canvas.StrokePolylines(line, ChartColors.Black, 1, RasterLineCap.Butt, RasterLineJoin.Miter, new[] { .00001, .00001 });
        Assert.Contains(canvas.Pixels, value => value > 0);
    }

    [Fact]
    public void FractionalDashLengthsFinishRoundedBoundaries() {
        var canvas = new RgbaCanvas(120, 40, 1);
        canvas.StrokePolylines(new[] { new[] { new ChartPoint(3.7, 10), new ChartPoint(53.8, 10), new ChartPoint(116.3, 10) } },
            ChartColors.Black, 1.2, RasterLineCap.Round, RasterLineJoin.Miter, new[] { 1.3, 2.7 });
        Assert.Contains(canvas.Pixels, value => value > 0);
        Assert.Contains(Enumerable.Range(10, 90), x => canvas.Pixels[(10 * 120 + x) * 4 + 3] == 0);
    }

    [Fact]
    public void LongInvisibleLineKeepsTheVisibleDashPhase() {
        var actual = new RgbaCanvas(80, 20, 1);
        var expected = new RgbaCanvas(80, 20, 1);
        actual.StrokePolylines(new[] { new[] { new ChartPoint(-10000000, 10), new ChartPoint(100, 10) } }, ChartColors.Black, 2, RasterLineCap.Butt, RasterLineJoin.Miter, new[] { 4D, 4D });
        expected.StrokePolylines(new[] { new[] { new ChartPoint(0, 10), new ChartPoint(100, 10) } }, ChartColors.Black, 2, RasterLineCap.Butt, RasterLineJoin.Miter, new[] { 4D, 4D });
        Assert.Equal(expected.Pixels, actual.Pixels);
    }

    [Fact]
    public void HugeFiniteBezierRetainsTheBoundedMaximumDetail() {
        int count = ChartCurveFlattening.QuadraticSegments(new ChartPoint(0, 0), new ChartPoint(1E30, 1E30), new ChartPoint(1, 1), 1);
        Assert.Equal(2048, count);
    }
}
