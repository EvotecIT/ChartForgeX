using ChartForgeX.Composition;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Stroked curves must carry the same ink everywhere along their length: a faceted or badly
/// joined stroke shows up as a stroke that is thicker at some angles than at others.
/// </summary>
public sealed class RasterStrokeQualityTests {
    [Theory]
    [InlineData(1.5)]
    [InlineData(4)]
    [InlineData(18)]
    public void StrokedCircleCarriesEvenInkAtEveryAngle(double strokeWidth) {
        var image = Render(200, 200, $"<circle cx='100.3' cy='99.6' r='70' fill='none' stroke='#fff' stroke-width='{Number(strokeWidth)}'/>");
        AssertEvenRing(image, 100.3, 99.6, 70, strokeWidth, 0, 360);
    }

    [Fact]
    public void StrokedEllipseKeepsItsWidthAlongBothAxes() {
        var image = Render(240, 140, "<ellipse cx='120' cy='70' rx='100' ry='50' fill='none' stroke='#fff' stroke-width='3'/>");
        Assert.InRange(InkAcross(image, 120, 70, 1, 0, 90, 110), 2.8, 3.2);
        Assert.InRange(InkAcross(image, 120, 70, 0, 1, 40, 60), 2.8, 3.2);
    }

    [Fact]
    public void ThickArcPathIsSmoothAndHonoursRoundCaps() {
        // Three quarters of a ring, clockwise from twelve o'clock, as a progress gauge draws it.
        const string arc = "<path d='M100 30 A70 70 0 1 1 30 100' fill='none' stroke='#fff' stroke-width='18' stroke-linecap='{0}'/>";
        var round = Render(200, 200, string.Format(arc, "round"));
        AssertEvenRing(round, 100, 100, 70, 18, -85, 175);
        Assert.Equal(0, Alpha(round, 60, 60));

        var butt = Render(200, 200, string.Format(arc, "butt"));
        AssertEvenRing(butt, 100, 100, 70, 18, -85, 175);
        // The round cap reaches half a stroke width past the start of the arc; the butt cap stops there.
        Assert.True(Alpha(round, 94, 30) > 200, "A round cap should cover the area just before the arc's start.");
        Assert.Equal(0, Alpha(butt, 94, 30));
        Assert.Equal(0, Alpha(round, 88, 30));
    }

    [Fact]
    public void StrokedRoundedRectangleKeepsItsWidthThroughTheCorners() {
        var image = Render(200, 100, "<rect x='20.5' y='20.5' width='160' height='42' rx='21' fill='none' stroke='#fff' stroke-width='1.5'/>");
        // Straight top edge, then the left end cap of the pill sampled through its corner arcs.
        Assert.InRange(InkAcross(image, 100, 20.5, 0, 1, -5, 5), 1.4, 1.6);
        AssertEvenRing(image, 41.5, 41.5, 21, 1.5, 95, 265);
    }

    [Fact]
    public void CurvedPathStrokeKeepsAnEvenTranslucentTone() {
        var image = Render(140, 160, "<path d='M10 150 C20 20 100 20 130 150' fill='none' stroke='#fff' stroke-width='7' stroke-opacity='.5' stroke-linejoin='round'/>");
        var strongest = 0;
        var inked = 0;
        for (var index = 3; index < image.Pixels.Length; index += 4) {
            strongest = Math.Max(strongest, image.Pixels[index]);
            if (image.Pixels[index] >= 126) inked++;
        }

        // Segments and joins are united before painting, so nothing is blended twice.
        Assert.InRange(strongest, 126, 129);
        Assert.True(inked > 1000, "The curve should be painted as a solid translucent band.");
    }

    [Fact]
    public void LineCapsChangeWhereAStrokeEnds() {
        const string line = "<path d='M30 20 L70 20' fill='none' stroke='#fff' stroke-width='10' stroke-linecap='{0}'/>";
        var butt = Render(100, 40, string.Format(line, "butt"));
        var square = Render(100, 40, string.Format(line, "square"));
        var round = Render(100, 40, string.Format(line, "round"));
        Assert.Equal(0, Alpha(butt, 27, 20));
        Assert.Equal(255, Alpha(square, 27, 20));
        Assert.Equal(255, Alpha(round, 27, 20));
        // The corner of the extension belongs to a square cap only.
        Assert.Equal(255, Alpha(square, 25, 15));
        Assert.Equal(0, Alpha(round, 25, 15));
        Assert.Equal(0, Alpha(square, 23, 20));
    }

    [Fact]
    public void LineJoinsChangeTheOutsideOfACorner() {
        const string corner = "<path d='M20 60 L50 30 L80 60' fill='none' stroke='#fff' stroke-width='12' stroke-linejoin='{0}'/>";
        var miter = Render(100, 80, string.Format(corner, "miter"));
        var round = Render(100, 80, string.Format(corner, "round"));
        var bevel = Render(100, 80, string.Format(corner, "bevel"));
        // The apex is at y 30; a miter extends the outside to a point, round stops at half a width, bevel cuts it flat.
        Assert.Equal(255, Alpha(miter, 50, 23));
        Assert.Equal(0, Alpha(round, 50, 22));
        Assert.Equal(255, Alpha(round, 50, 25));
        Assert.Equal(0, Alpha(bevel, 50, 24));
        Assert.Equal(255, Alpha(bevel, 50, 27));
        // A miter longer than the limit falls back to a bevel.
        var limited = Render(100, 80, string.Format(corner, "miter").Replace("stroke-width", "stroke-miterlimit='1' stroke-width"));
        Assert.Equal(0, Alpha(limited, 50, 24));
    }

    [Fact]
    public void DashedStrokesKeepGapsOnCurves() {
        var image = Render(200, 200, "<circle cx='100' cy='100' r='70' fill='none' stroke='#fff' stroke-width='6' stroke-dasharray='20 20'/>");
        var inked = 0;
        var blank = 0;
        for (var degrees = 0; degrees < 360; degrees++) {
            var ink = RadialInk(image, 100, 100, degrees, 60, 80);
            if (ink > 5.5) inked++;
            else if (ink < 0.05) blank++;
        }

        Assert.InRange(inked, 120, 180);
        Assert.InRange(blank, 120, 180);
    }

    [Fact]
    public void RasterizerReturnsTheSamePixelsAsItsPngOutput() {
        const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='60' height='40'><rect width='60' height='40' fill='#123456'/><circle cx='30' cy='20' r='12' fill='none' stroke='#fedcba' stroke-width='3'/></svg>";
        var direct = SvgRasterizer.ToImage(svg);
        var decoded = RasterImageDecoder.Decode(SvgRasterizer.ToPng(svg));
        Assert.Equal(60, direct.Width);
        Assert.Equal(40, direct.Height);
        Assert.Equal(decoded.Pixels, direct.Pixels);

        var scaled = SvgRasterizer.ToImage(System.Text.Encoding.UTF8.GetBytes(svg), 120);
        Assert.Equal(120, scaled.Width);
        Assert.Equal(80, scaled.Height);
        Assert.Throws<ArgumentException>(() => SvgRasterizer.ToImage(" "));
        Assert.Throws<FormatException>(() => SvgRasterizer.ToImage("<svg"));
    }

    [Fact]
    public void CompositionDrawsAntialiasedCirclesAndEllipses() {
        var image = ImageComposition.CreateTransparent(200, 200)
            .FillCircle(60, 60, 30, ChartColors.White)
            .StrokeCircle(140, 60, 30, ChartColors.White, 4)
            .FillEllipse(60, 150, 40, 20, ChartColors.White)
            .StrokeEllipse(140, 150, 40, 20, ChartColors.White, 2)
            .ToImage();
        Assert.Equal(255, Alpha(image, 60, 60));
        Assert.Equal(0, Alpha(image, 25, 25));
        // The disc's area is its coverage: pi * r^2.
        var area = 0.0;
        for (var y = 20; y < 100; y++) for (var x = 20; x < 100; x++) area += Alpha(image, x, y) / 255.0;
        Assert.InRange(area, Math.PI * 900 - 12, Math.PI * 900 + 12);

        AssertEvenRing(image, 140, 60, 30, 4, 0, 360);
        Assert.Equal(0, Alpha(image, 140, 60));
        Assert.Equal(255, Alpha(image, 60, 150));
        Assert.Equal(0, Alpha(image, 60, 125));
        Assert.InRange(InkAcross(image, 140, 150, 1, 0, 30, 50), 1.85, 2.15);
        Assert.InRange(InkAcross(image, 140, 150, 0, 1, 10, 30), 1.85, 2.15);
    }

    [Fact]
    public void CompositionDrawsArcsAndProgressRings() {
        var arc = ImageComposition.CreateTransparent(200, 200).DrawArc(100, 100, 70, -90, 90, ChartColors.White, 16).ToImage();
        AssertEvenRing(arc, 100, 100, 70, 16, -85, -5);
        Assert.Equal(0, Alpha(arc, 100, 170));
        Assert.Equal(0, Alpha(arc, 30, 100));
        // Round caps by default: just before twelve o'clock is still covered.
        Assert.True(Alpha(arc, 95, 30) > 200);
        var butt = ImageComposition.CreateTransparent(200, 200).DrawArc(100, 100, 70, -90, 90, ChartColors.White, 16, ImageLineCap.Butt).ToImage();
        Assert.Equal(0, Alpha(butt, 95, 30));
        var counterClockwise = ImageComposition.CreateTransparent(200, 200).DrawArc(100, 100, 70, -90, -90, ChartColors.White, 16).ToImage();
        Assert.Equal(255, Alpha(counterClockwise, 51, 51));
        Assert.Equal(0, Alpha(counterClockwise, 149, 51));

        var track = ChartColor.FromHex("#222A38");
        var ring = ImageComposition.Create(200, 200, ChartColors.Black).DrawProgressRing(100, 100, 70, 16, 0.25, track, ChartColors.Red).ToImage();
        Assert.Equal((255, 0, 0), Rgb(ring, 149, 51));
        Assert.Equal((track.R, track.G, track.B), Rgb(ring, 100, 170));
        Assert.Equal((0, 0, 0), Rgb(ring, 100, 100));
        var full = ImageComposition.Create(200, 200, ChartColors.Black).DrawProgressRing(100, 100, 70, 16, 1.5, track, ChartColors.Red).ToImage();
        for (var degrees = 0; degrees < 360; degrees += 5) {
            var radians = degrees * Math.PI / 180;
            Assert.Equal((255, 0, 0), Rgb(full, (int)Math.Round(100 + Math.Cos(radians) * 70), (int)Math.Round(100 + Math.Sin(radians) * 70)));
        }

        var empty = ImageComposition.Create(200, 200, ChartColors.Black).DrawProgressRing(100, 100, 70, 16, 0, track, ChartColors.Red).ToImage();
        Assert.Equal((track.R, track.G, track.B), Rgb(empty, 100, 30));
    }

    [Fact]
    public void CompositionFillsLinearAndRadialGradients() {
        var horizontal = ImageComposition.CreateTransparent(101, 40).FillRectangleLinearGradient(0, 0, 101, 40, ChartColors.Black, ChartColors.White, 0).ToImage();
        Assert.InRange(horizontal.Pixels[0], 0, 3);
        Assert.InRange(horizontal.Pixels[(20 * 101 + 50) * 4], 125, 130);
        Assert.InRange(horizontal.Pixels[(20 * 101 + 100) * 4], 252, 255);

        var vertical = ImageComposition.CreateTransparent(40, 101).FillRectangleLinearGradient(0, 0, 40, 101, ChartColors.Black, ChartColors.White).ToImage();
        Assert.InRange(vertical.Pixels[(0 * 40 + 20) * 4], 0, 3);
        Assert.InRange(vertical.Pixels[(100 * 40 + 20) * 4], 252, 255);
        Assert.Equal(vertical.Pixels[(50 * 40 + 2) * 4], vertical.Pixels[(50 * 40 + 37) * 4]);

        var rounded = ImageComposition.CreateTransparent(60, 60).FillRectangleLinearGradient(0, 0, 60, 60, ChartColors.Red, ChartColors.Blue, 45, 20).ToImage();
        Assert.Equal(0, Alpha(rounded, 1, 1));
        Assert.Equal(255, Alpha(rounded, 30, 30));

        var accent = ChartColor.FromHex("#2C5FF0");
        var glow = ImageComposition.Create(120, 80, ChartColors.Black)
            .FillRectangleRadialGradient(0, 0, 120, 80, 120, 0, 60, accent, ChartColor.FromRgba(accent.R, accent.G, accent.B, 0))
            .ToImage();
        Assert.True(Rgb(glow, 118, 1).Item3 > 220, "The gradient center should take the inner color.");
        Assert.InRange(Rgb(glow, 90, 1).Item3, 100, 140);
        Assert.Equal((0, 0, 0), Rgb(glow, 20, 60));
    }

    [Fact]
    public void CompositionShapesValidateTheirArguments() {
        var image = ImageComposition.CreateTransparent(10, 10);
        Assert.Throws<ArgumentOutOfRangeException>(() => image.FillCircle(5, 5, 0, ChartColors.White));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.StrokeCircle(5, 5, 3, ChartColors.White, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.FillEllipse(double.NaN, 5, 3, 3, ChartColors.White));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.DrawArc(5, 5, 3, 0, double.PositiveInfinity, ChartColors.White));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.DrawArc(5, 5, 3, 0, 90, ChartColors.White, 1, (ImageLineCap)9));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.FillRectangleLinearGradient(0, 0, 0, 10, ChartColors.White, ChartColors.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.FillRectangleRadialGradient(0, 0, 10, 10, 5, 5, 0, ChartColors.White, ChartColors.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => image.DrawProgressRing(5, 5, 3, 1, double.NaN, ChartColors.White, ChartColors.Black));
    }

    private static RgbaImage Render(int width, int height, string body) =>
        SvgRasterizer.ToImage($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}' viewBox='0 0 {width} {height}'>{body}</svg>");

    private static string Number(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Requires the ink crossed by every radius in the sweep to equal the stroke width, with little variation.</summary>
    private static void AssertEvenRing(RgbaImage image, double cx, double cy, double radius, double strokeWidth, int startDegrees, int endDegrees) {
        var least = double.MaxValue;
        var most = double.MinValue;
        for (var degrees = startDegrees; degrees < endDegrees; degrees++) {
            var ink = RadialInk(image, cx, cy, degrees, radius - strokeWidth / 2 - 4, radius + strokeWidth / 2 + 4);
            least = Math.Min(least, ink);
            most = Math.Max(most, ink);
        }

        var tolerance = Math.Max(0.12, strokeWidth * 0.03);
        Assert.True(least > strokeWidth - tolerance && most < strokeWidth + tolerance,
            FormattableString.Invariant($"A {strokeWidth} px stroke should carry {strokeWidth} px of ink across every radius, but it ranged from {least:0.###} to {most:0.###}."));
    }

    private static double RadialInk(RgbaImage image, double cx, double cy, double degrees, double from, double to) {
        var radians = degrees * Math.PI / 180;
        return InkAcross(image, cx, cy, Math.Cos(radians), Math.Sin(radians), from, to);
    }

    /// <summary>Integrates coverage along a ray, which for a stroke crossed at a right angle is its width in pixels.</summary>
    private static double InkAcross(RgbaImage image, double x, double y, double directionX, double directionY, double from, double to) {
        const double step = 0.125;
        var ink = 0.0;
        for (var distance = from; distance < to; distance += step) ink += Coverage(image, x + directionX * distance, y + directionY * distance) * step;
        return ink;
    }

    private static double Coverage(RgbaImage image, double x, double y) {
        x -= 0.5;
        y -= 0.5;
        var left = (int)Math.Floor(x);
        var top = (int)Math.Floor(y);
        var fx = x - left;
        var fy = y - top;
        return (Alpha(image, left, top) * (1 - fx) * (1 - fy) + Alpha(image, left + 1, top) * fx * (1 - fy) + Alpha(image, left, top + 1) * (1 - fx) * fy + Alpha(image, left + 1, top + 1) * fx * fy) / 255.0;
    }

    private static int Alpha(RgbaImage image, int x, int y) =>
        x < 0 || y < 0 || x >= image.Width || y >= image.Height ? 0 : image.Pixels[(y * image.Width + x) * 4 + 3];

    private static (int, int, int) Rgb(RgbaImage image, int x, int y) {
        var offset = (y * image.Width + x) * 4;
        return (image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2]);
    }
}
