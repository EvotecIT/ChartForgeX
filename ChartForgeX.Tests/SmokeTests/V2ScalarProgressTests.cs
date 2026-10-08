using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2ScalarProgressTests {
    [Theory]
    [InlineData(-20, 0)]
    [InlineData(50, .5)]
    [InlineData(150, 1)]
    public void CircleRetainsRawValueWhileItsRingClampsToTheDeclaredDomain(double raw, double ratio) {
        var chart = Chart.Create().AddCircle("Capacity", raw);
        var scene = Compile(chart);
        var group = Assert.Single(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "circle-chart");
        Assert.Equal(raw.ToString("R", CultureInfo.InvariantCulture), group.Metadata["data-cfx-value"]);
        Assert.Equal(ratio.ToString("R", CultureInfo.InvariantCulture), group.Metadata["data-cfx-percent"]);
        var values = scene.Nodes.OfType<VisualSceneSlice>().Where(slice => slice.Role == "circle-value").ToArray();
        if (ratio == 0) Assert.Empty(values); else Assert.Equal(Math.PI * 2 * ratio, Assert.Single(values).Sweep, 8);
        Assert.Contains(scene.Regions, region => region.Role == "circle-label" && region.Label == raw.ToString(CultureInfo.InvariantCulture));
        Assert.Contains(scene.Nodes, node => node.Role == "circle-status-label");
        Assert.Contains(VisualSceneRasterRenderer.Render(scene).Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
    }

    [Fact]
    public void CircleScalingAndStateOverridesAffectGeometryAndPaintWithinCompactBounds() {
        var chart = Chart.Create().AddCircle("Load", 75);
        chart.Series[0].StateRole = ChartSeriesState.Info;
        var first = Assert.Single(Compile(chart).Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "circle-value");
        chart.Options.CircleRadiusScale = 1.35; chart.Options.CircleStrokeScale = 1.8;
        var scene = Compile(chart, new ChartRect(0, 0, 180, 140));
        var second = Assert.Single(scene.Nodes.OfType<VisualSceneSlice>(), slice => slice.Role == "circle-value");
        Assert.Equal(new VisualRenderContext().Theme.Resolve(VisualThemeMode.Light).Status.Info.Fill, second.Fill);
        Assert.NotEqual(first.Outer - first.Inner, second.Outer - second.Inner);
        Assert.True(second.Cx - second.Outer >= 0 && second.Cx + second.Outer <= 180 && second.Cy - second.Outer >= 0 && second.Cy + second.Outer <= 140);
        chart.Options.ShowCircleStatusLabel = false;
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role is "circle-status-label" or "circle-status-marker");
        chart.Series[0].ShowDataLabels = false;
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role is "circle-label" or "circle-title");
    }

    [Fact]
    public void BulletRowsUseOneTruthfulScaleAndPreserveRawTargetsRangesAndExplicitPaint() {
        var chart = Chart.Create().AddBullet("Small", 50, 120, 0, 100, new[] { 30d, 60d })
            .AddBullet("Large", 500, -100, 0, 1000, new[] { 200d, 700d });
        var paint = ChartColor.FromHex("#2468AC"); chart.Series[0].WithPointColor(0, paint);
        var scene = Compile(chart);
        var groups = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "bullet-row").ToArray();
        Assert.Equal(2, groups.Length); Assert.All(groups, group => Assert.Equal("1000", group.Metadata["data-cfx-scale-max"]));
        Assert.Equal("100", groups[0].Metadata["data-cfx-max"]); Assert.Equal("-100", groups[1].Metadata["data-cfx-target"]);
        var bars = scene.Nodes.OfType<VisualSceneRectangle>().Where(rect => rect.Role == "bullet-value").ToArray();
        Assert.Equal(paint, bars[0].Fill);
        Assert.InRange(Math.Abs(bars[0].Bounds.Width * 10 - bars[1].Bounds.Width), 0, 1e-8);
        var targets = scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "bullet-target").ToArray();
        Assert.Equal(bars[1].Bounds.Left, targets[1].Start.X, 8);
        Assert.Equal(5, scene.Nodes.Count(node => node.Role == "bullet-axis-tick"));
        Assert.Contains(scene.Regions, region => region.Id == "series-0" && region.Label == "Small: 50, target 120");
        Assert.Contains(scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "bullet-range-source" && group.Metadata["data-cfx-max"] == "30");
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Theory]
    [InlineData(160, 100)]
    [InlineData(640, 240)]
    public void ProgressRowsPreserveOverMaximumValuesAndFitHandlesAtBothTrackEnds(int width, int height) {
        const string full = "Complete progress category with retained additional context";
        var chart = Chart.Create().AddProgressBars("Work", new[] { new ChartProgressItem(full, 0), new ChartProgressItem("Done", 175) });
        chart.Options.ProgressBarThicknessRatio = .72;
        chart.Options.ValueFormatter = number => number.ToString("0.0", CultureInfo.GetCultureInfo("pl-PL")) + " units";
        var scene = Compile(chart, new ChartRect(0, 0, width, height));
        var groups = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "progress-row").ToArray();
        Assert.Equal("175", groups[1].Metadata["data-cfx-value"]); Assert.Equal("1", groups[1].Metadata["data-cfx-ratio"]);
        var tracks = scene.Nodes.OfType<VisualSceneRectangle>().Where(rect => rect.Role == "progress-track").ToArray();
        var fills = scene.Nodes.OfType<VisualSceneRectangle>().Where(rect => rect.Role == "progress-fill").ToArray();
        Assert.Equal(0, fills[0].Bounds.Width); Assert.Equal(tracks[1].Bounds.Width, fills[1].Bounds.Width, 8);
        foreach (var handle in scene.Nodes.OfType<VisualSceneEllipse>().Where(ellipse => ellipse.Role == "progress-handle")) {
            Assert.True(handle.Cx - handle.Rx - handle.StrokeWidth / 2 >= -.001 && handle.Cx + handle.Rx + handle.StrokeWidth / 2 <= width + .001);
            Assert.True(handle.Cy - handle.Ry - handle.StrokeWidth / 2 >= -.001 && handle.Cy + handle.Ry + handle.StrokeWidth / 2 <= height + .001);
        }
        var artifact = new PreparedVisual(scene).ToArtifact("progress", VisualArtifactKind.Chart);
        Assert.Contains(artifact.Regions, region => region.Label!.Contains(full));
        Assert.Contains("175,0 units", artifact.ToSvg());
        chart.Options.ShowProgressHandles = false; chart.Options.ShowProgressValues = false;
        Assert.DoesNotContain(Compile(chart).Nodes, node => node.Role is "progress-handle" or "progress-value");
        Assert.NotEmpty(VisualSceneRasterRenderer.Render(scene).Pixels);
    }

    [Fact]
    public void InvalidMutableRangesFailAndCompleteCustomLabelsSurviveCompactArtifactHandoff() {
        const string full = "An operator-provided measurement label which will be shortened on this compact circle";
        var chart = Chart.Create().AddCircle("Small", 70); chart.Series[0].WithPointLabel(0, full);
        var scene = Compile(chart, new ChartRect(0, 0, 100, 90));
        Assert.Contains(scene.Regions, region => region.Label == full);
        Assert.Contains(full, new PreparedVisual(scene).ToArtifact("circle", VisualArtifactKind.Chart).ToSvg());
        chart.Series[0].Points[1] = new ChartPoint(0, 70);
        Assert.Throws<InvalidOperationException>(() => Compile(chart));
    }

    private static VisualScene Compile(Chart chart, ChartRect? bounds = null) {
        var plot = bounds ?? new ChartRect(0, 0, 440, 330); var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(plot.Right, plot.Bottom), context.Font);
        VisualScalarProgressCompiler.Build(chart, context, builder, plot); return builder.Build();
    }
}
