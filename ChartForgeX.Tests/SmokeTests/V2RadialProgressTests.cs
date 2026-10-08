using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2RadialProgressTests {
    [Fact]
    public void RadialBars_PreserveZeroFullAndPartialRingsWithPointPaintAndAverage() {
        var chart = Chart.Create().AddRadialBar("Uptime", new[] { new ChartPoint(1, 0), new ChartPoint(2, 100), new ChartPoint(3, 50) }).WithXLabels("A", "B", "C");
        var paint = ChartColor.FromHex("#456789"); chart.Series[0].WithPointColor(1, paint);
        var scene = Compile(chart);
        var rings = scene.Nodes.OfType<VisualSceneSlice>().Where(slice => slice.Role == "radial-bar-ring").ToArray();
        Assert.Equal(3, scene.Nodes.Count(node => node.Role == "radial-bar-track"));
        Assert.Equal(2, rings.Length); Assert.Equal(Math.PI * 2, rings[0].Sweep, 10); Assert.Equal(Math.PI, rings[1].Sweep, 10);
        Assert.Equal(paint, rings[0].Fill);
        Assert.Contains(scene.Regions, region => region.Id == "series-0-center-value" && region.Label == "50");
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-0" && region.Label == "A: 0");
        chart.Options.ShowRadialBarCenterLabel = false;
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role is "radial-bar-value" or "radial-bar-title");
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void LayeredRadial_PreservesIndependentRangesAnglesCapsOpacityAndSeparators() {
        var color = ChartColor.FromHex("#2468AC");
        var lower = ChartRadialLayer.Create("Base", 80).WithGeometry(1, .12).WithAngles(30, 270).WithLineCap(ChartRadialLayerCap.Butt).WithSeparators(3);
        var upper = ChartRadialLayer.Create("Overlay", 15, 10, 20, color).WithGeometry(.6, .2).WithAngles(-90, 180).WithOpacity(.5);
        var chart = Chart.Create().AddLayeredRadial("Layers", new[] { lower, upper });
        var scene = Compile(chart);
        var arcs = scene.Nodes.OfType<VisualSceneSlice>().Where(slice => slice.Role == "layered-radial-layer").ToArray();
        Assert.Equal(2, arcs.Length); Assert.Equal(Math.PI / 6, arcs[0].Start, 10); Assert.Equal(Math.PI * 1.2, arcs[0].Sweep, 10);
        Assert.Equal(Math.PI / 2, arcs[1].Sweep, 10); Assert.Equal(ChartColorMath.WithOpacity(color, .5), arcs[1].Fill);
        Assert.Equal(3, scene.Nodes.Count(node => node.Role == "layered-radial-separator"));
        Assert.Equal(2, scene.Nodes.Count(node => node.Role == "layered-radial-layer-cap"));
        Assert.Contains(scene.Regions, region => region.Label == "Overlay: 15");
        var svg = VisualSceneSvgRenderer.Render(scene);
        lower.Value = 0; upper.Maximum = 10;
        Assert.Equal(svg, VisualSceneSvgRenderer.Render(scene));
        Assert.Throws<InvalidOperationException>(() => Compile(chart));
    }

    [Fact]
    public void RadiusAndStrokeScalingHaveVisibleEffectsWithoutEscapingTheFixedPlot() {
        var chart = Chart.Create().AddLayeredRadial("Scaled", new[] { ChartRadialLayer.Create("A", 75).WithGeometry(1.5, .8) });
        chart.Series[0].ShowDataLabels = false;
        var first = Assert.Single(Compile(chart).Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "layered-radial-layer");
        chart.Options.RadialBarStrokeScale = 1.8;
        var second = Assert.Single(Compile(chart).Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "layered-radial-layer");
        Assert.True(second.Outer - second.Inner > first.Outer - first.Inner);
        Assert.True(second.Cx - second.Outer >= 0 && second.Cy - second.Outer >= 0 && second.Cx + second.Outer <= 420 && second.Cy + second.Outer <= 320);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MeasuredCenterRowsPaintTheirValueAndCaptionInsideTheRealRingHole(bool layered) {
        var chart = layered ? Chart.Create().AddLayeredRadial("Completion", new[] {
            new ChartRadialLayer("Reviewed", 76) { RadiusRatio = 1, StrokeRatio = .13 },
            new ChartRadialLayer("Verified", 58) { RadiusRatio = .7, StrokeRatio = .13 },
            new ChartRadialLayer("Complete", 42) { RadiusRatio = .4, StrokeRatio = .13 }
        }) : Chart.Create().AddRadialBar("Completion", new[] { 20d, 40, 60, 80, 90, 70 }
            .Select((value, index) => new ChartPoint(index + 1, value)));
        var scene = Compile(chart);
        var role = layered ? "layered-radial" : "radial-bar";
        var value = Assert.Single(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == role + "-value");
        var caption = Assert.Single(scene.Nodes.OfType<VisualSceneText>(), text => text.Role == role + "-title");
        Assert.Equal(layered ? "42" : "60", Assert.Single(value.Text.Lines).Text);
        Assert.Equal("Completion", Assert.Single(caption.Text.Lines).Text);
        Assert.True(value.Baseline - value.Text.Ascent + value.Text.Metrics.Height <= caption.Baseline - caption.Text.Ascent);
        var radius = layered ? scene.Nodes.OfType<VisualSceneSlice>().Where(mark => mark.Role == "layered-radial-layer").Min(mark => mark.Inner)
            : Assert.Single(scene.Nodes.OfType<VisualSceneEllipse>(), mark => mark.Role == "radial-bar-center").Rx;
        foreach (var text in new[] { value, caption }) {
            var vertical = Math.Max(Math.Abs(text.Baseline - text.Text.Ascent - 160),
                Math.Abs(text.Baseline - text.Text.Ascent + text.Text.Metrics.Height - 160));
            Assert.True(Math.Sqrt(Math.Pow(text.Text.Metrics.Width / 2, 2) + vertical * vertical) <= radius + .001);
        }
        Assert.Contains(scene.Regions, region => region.Id == "series-0-center-value" && region.Label == (layered ? "42" : "60"));
        chart.Series[0].ShowDataLabels = false;
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role == role + "-value" || node.Role == role + "-title");
    }

    private static VisualScene Compile(Chart chart) {
        var context = new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(420, 320), context.Font);
        VisualRadialProgressCompiler.Build(chart, context, builder, new ChartRect(0, 0, 420, 320));
        return builder.Build();
    }
}
