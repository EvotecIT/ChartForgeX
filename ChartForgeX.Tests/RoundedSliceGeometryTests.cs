using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RoundedSliceGeometryTests {
    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void PublicCornerRadiiRejectNonfiniteOrNegativePixels(double radius) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadialGeometryOptions(cornerRadius: radius));
        var options = new ChartSunburstOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.CornerRadius = radius);
        Assert.Equal(0, options.CornerRadius);
        Assert.Equal(0, new ChartRadialGeometryOptions().CornerRadius);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.ProgressRing)]
    [InlineData(ChartSeriesKind.Sunburst)]
    public void NumericRoundingCannotSilentlyApplyToAnotherRadialFamily(ChartSeriesKind kind) {
        var chart = V2GalleryModels.Create(kind);
        _ = chart.ToSvg();
        chart.WithRadialGeometry(new(cornerRadius: 6));
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        chart.WithRadialGeometry(new());
        if (kind == ChartSeriesKind.Sunburst) chart.Series.Clear();
        chart.ConfigureSunburst(options => options.CornerRadius = 6);
        if (chart.Series.Count == 0) _ = chart.ToSvg();
        else Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
    }

    [Theory]
    [InlineData(0, .02)]
    [InlineData(0, 1.5)]
    [InlineData(20, .02)]
    [InlineData(20, 1.5)]
    [InlineData(99, 1.5)]
    [InlineData(20, 5.5)]
    public void MaximumRoundingStaysWithinTheSectorAndClampsToItsAvailableSpace(double inner, double sweep) {
        const double start = -.8;
        var path = Assert.IsType<ChartPath>(ChartSlicePathGeometry.RoundedPath(120, 120, 100, inner, start, sweep, double.MaxValue));
        var scene = new VisualScenePath(path, true, ChartColor.Black, null, 0, null, null);
        var contour = Assert.Single(VisualSceneGeometry.Flatten(scene, 8));
        Assert.True(VisualSceneGeometry.HasEvenOddFillArea(new[] { contour }));
        Assert.All(contour, point => {
            Assert.True(double.IsFinite(point.X) && double.IsFinite(point.Y));
            var x = point.X - 120; var y = point.Y - 120; var radius = Math.Sqrt(x * x + y * y);
            Assert.InRange(radius, Math.Max(0, inner - .002), 100.002);
            if (radius < .002) return;
            var angle = (Math.Atan2(y, x) - start + Math.PI * 2) % (Math.PI * 2);
            Assert.True(angle <= sweep + .002 || angle >= Math.PI * 2 - .002, "Rounded paint must stay inside the angular envelope.");
        });
        if (inner == 0) Assert.Contains(path.Commands, command => command.Kind == ChartPathCommandKind.LineTo && command.X == 120 && command.Y == 120);
        var clamped = ChartSlicePathGeometry.RoundedPath(120, 120, 100, inner, start, sweep, 100)!;
        Assert.Equal(path.Commands, clamped.Commands);
    }

    [Fact]
    public void ZeroThicknessAndZeroSweepHaveNoRoundedFillAndFullAnnuliKeepTheirSeamlessOutline() {
        Assert.False(ChartSlicePathGeometry.HasEncodedFillArea(120, 120, 100, 100, 0, 1, 6));
        Assert.False(ChartSlicePathGeometry.HasEncodedFillArea(120, 120, 100, 20, 0, 0, 6));
        var sharp = Builder(); var rounded = Builder();
        sharp.Slice(120, 120, 100, 40, .7, Math.PI * 2, ChartColor.Black, ChartColor.White, 2);
        rounded.Slice(120, 120, 100, 40, .7, Math.PI * 2, ChartColor.Black, ChartColor.White, 2, cornerRadius: double.MaxValue);
        var reference = sharp.Build(); var actual = rounded.Build();
        Assert.IsType<VisualSceneSlice>(Assert.Single(actual.Nodes));
        Assert.Equal(VisualSceneSvgRenderer.Render(reference), VisualSceneSvgRenderer.Render(actual));
        Assert.Equal(VisualSceneRasterRenderer.Render(reference).Pixels, VisualSceneRasterRenderer.Render(actual).Pixels);
        Assert.Equal(0, Alpha(VisualSceneRasterRenderer.Render(actual), 120, 120));
    }

    [Fact]
    public void RoundedGradientAndPatternShareTheSameEncodedContourAndNativeCutaways() {
        var builder = Builder();
        var stops = new[] { new VisualGradientStop(0, ChartColor.FromHex("#2468AC")), new VisualGradientStop(1, ChartColor.FromHex("#60B0D0")) };
        builder.SliceGradient(120, 120, 100, 40, 0, Math.PI / 2, new ChartPoint(120, 120), new ChartPoint(220, 120), stops,
            ChartColor.Black, 1, role: "rounded-gradient", cornerRadius: 20);
        builder.PatternSlice(120, 120, 100, 40, 0, Math.PI / 2, ChartFillPattern.Crosshatch, ChartColor.White,
            role: "rounded-pattern", cornerRadius: 20);
        var scene = builder.Build(); var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        var gradient = svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "rounded-gradient");
        var clip = svg.Descendants().Single(node => node.Name.LocalName == "clipPath").Elements().Single();
        Assert.Equal(gradient.Attribute("d")!.Value, clip.Attribute("d")!.Value);
        Assert.Contains("C", gradient.Attribute("d")!.Value);
        Assert.StartsWith("url(#", gradient.Attribute("fill")!.Value);
        Assert.Equal("evenodd", gradient.Attribute("fill-rule")!.Value);
        var image = VisualSceneRasterRenderer.Render(scene);
        Assert.Equal(0, Alpha(image, 218, 122));
        Assert.Equal(0, Alpha(image, 162, 121));
        Assert.Equal(0, Alpha(image, 140, 140));
        Assert.Equal(255, Alpha(image, 170, 170));
    }

    private static VisualSceneBuilder Builder() => new(new VisualSize(240, 240), FontSpec.FromFamily("Missing rounded geometry test font"));
    private static byte Alpha(RgbaImage image, int x, int y) => image.Pixels[(y * image.Width + x) * 4 + 3];
}
