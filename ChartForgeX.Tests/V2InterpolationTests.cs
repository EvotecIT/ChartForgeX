using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects interpolation choices through the shared prepared export boundary.</summary>
public sealed class V2InterpolationTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line, ChartStepPosition.Start)]
    [InlineData(ChartSeriesKind.Line, ChartStepPosition.Middle)]
    [InlineData(ChartSeriesKind.Line, ChartStepPosition.End)]
    [InlineData(ChartSeriesKind.Area, ChartStepPosition.Start)]
    [InlineData(ChartSeriesKind.Area, ChartStepPosition.Middle)]
    [InlineData(ChartSeriesKind.Area, ChartStepPosition.End)]
    [InlineData(ChartSeriesKind.StackedArea, ChartStepPosition.Middle)]
    public void StepTransitionsUseTheirDeclaredPositionForLineAndFill(ChartSeriesKind kind, ChartStepPosition position) {
        var chart = Model(kind, new[] { new ChartPoint(0, 2), new ChartPoint(2, 8) });
        chart.Series[0].WithInterpolation(ChartInterpolation.Step, position);
        var prepared = chart.Prepare(Context());
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "line");
        var start = line.Commands[0]; var end = line.Commands[^1];
        var transition = position == ChartStepPosition.Start ? start.X : position == ChartStepPosition.End ? end.X : (start.X + end.X) / 2;
        var vertical = Enumerable.Range(1, line.Commands.Count - 1).Single(index => line.Commands[index].Y != line.Commands[index - 1].Y);
        Assert.Equal(transition, line.Commands[vertical - 1].X, 8);
        Assert.Equal(transition, line.Commands[vertical].X, 8);
        Assert.Equal(end.Y, line.Commands[vertical].Y, 8);
        if (kind != ChartSeriesKind.Line) {
            var area = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "area");
            Assert.True(area.Close);
            Assert.Contains(area.Commands, command => Math.Abs(command.X - transition) < 1e-8 && Math.Abs(command.Y - end.Y) < 1e-8);
        }
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "line");
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
    }

    [Theory]
    [InlineData(ChartStepPosition.Start)]
    [InlineData(ChartStepPosition.Middle)]
    [InlineData(ChartStepPosition.End)]
    public void RangeBoundariesAndMidlineShareStepPlacement(ChartStepPosition position) {
        var chart = Chart.Create().AddRangeArea("Range", new[] { new ChartRangeBand(0, 1, 3), new ChartRangeBand(2, 5, 9) })
            .WithAxes(false).WithGrid(false).WithLegend(false);
        chart.Series[0].WithInterpolation(ChartInterpolation.Step, position);
        var prepared = chart.Prepare(Context());
        foreach (var role in new[] { "range-upper", "range-lower", "range-midline" }) {
            var path = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == role);
            var start = path.Commands[0]; var end = path.Commands[^1];
            var transition = position == ChartStepPosition.Start ? start.X : position == ChartStepPosition.End ? end.X : (start.X + end.X) / 2;
            var vertical = Enumerable.Range(1, path.Commands.Count - 1).Single(index => path.Commands[index].Y != path.Commands[index - 1].Y);
            Assert.Equal(transition, path.Commands[vertical - 1].X, 8);
            Assert.Equal(transition, path.Commands[vertical].X, 8);
        }
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "range-area" || region.Role == "point"));
    }

    [Theory]
    [InlineData(ChartInterpolation.Linear)]
    [InlineData(ChartInterpolation.Smooth)]
    [InlineData(ChartInterpolation.Step)]
    public void GapsRemainDisconnectedAndPreparedChoicesAreDetached(ChartInterpolation interpolation) {
        var chart = Model(ChartSeriesKind.Area, new[] {
            new ChartPoint(0, 2), new ChartPoint(1, 5), new ChartPoint(2, 3),
            new ChartPoint(4, 4, true), new ChartPoint(5, 7), new ChartPoint(6, 6)
        });
        chart.Series[0].WithInterpolation(interpolation, interpolation == ChartInterpolation.Step ? ChartStepPosition.Middle : ChartStepPosition.End);
        var prepared = chart.Prepare(Context());
        var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "line");
        Assert.Equal(2, line.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "area"));
        Assert.Equal(interpolation == ChartInterpolation.Smooth, line.Commands.Any(command => command.Kind == ChartPathCommandKind.CubicTo));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[0].WithInterpolation(ChartInterpolation.Linear);
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.Equal(6, prepared.Regions.Count(region => region.Role == "point"));
    }

    [Fact]
    public void NativeRasterPaintsTheSelectedMiddleTransitionAndLeavesOtherPositionsClear() {
        var chart = Model(ChartSeriesKind.Line, new[] { new ChartPoint(0, 2), new ChartPoint(2, 8) });
        chart.Series[0].WithColor(ChartColor.FromHex("#E45236")).WithMarkerRadius(0).WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
        var prepared = chart.Prepare(Context());
        var path = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "line");
        var first = path.Commands[0]; var last = path.Commands[^1];
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        bool RedAt(double x, double y) {
            var px = (int)Math.Round(x); var py = (int)Math.Round(y);
            for (var dy = -1; dy <= 1; dy++) for (var dx = -1; dx <= 1; dx++) {
                var offset = ((py + dy) * image.Width + px + dx) * 4;
                if (image.Pixels[offset] > image.Pixels[offset + 1] * 1.5 && image.Pixels[offset + 3] > 100) return true;
            }
            return false;
        }
        Assert.True(RedAt((first.X + last.X) / 2, (first.Y + last.Y) / 2));
        Assert.False(RedAt(first.X + (last.X - first.X) * .25, (first.Y + last.Y) / 2));
        Assert.False(RedAt(first.X + (last.X - first.X) * .75, (first.Y + last.Y) / 2));
    }

    [Fact]
    public void ExistingConveniencesResolveCanonicalDefaultsAndExplicitChoicesOverrideThem() {
        var chart = Chart.Create().AddStepLine("Steps", new[] { new ChartPoint(0, 2), new ChartPoint(1, 4) })
            .AddSmoothArea("Smooth", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2), new ChartPoint(2, 1) });
        Assert.Equal(ChartInterpolation.Step, chart.Series[0].Interpolation);
        Assert.Equal(ChartStepPosition.End, chart.Series[0].StepPosition);
        Assert.Equal(ChartInterpolation.Smooth, chart.Series[1].Interpolation);
        chart.Series[0].WithInterpolation(ChartInterpolation.Linear);
        Assert.Equal(ChartInterpolation.Linear, chart.Series[0].Interpolation);
        chart.Series[1].WithSmooth(false);
        Assert.Equal(ChartInterpolation.Linear, chart.Series[1].Interpolation);
    }

    [Fact]
    public void InvalidAndUnsupportedChoicesFailInsteadOfRenderingAnIgnoredOption() {
        var series = new ChartSeries("Data", ChartSeriesKind.Line, new[] { new ChartPoint(0, 1) });
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Interpolation = (ChartInterpolation)99);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.StepPosition = (ChartStepPosition)99);
        Assert.Throws<ArgumentException>(() => series.WithInterpolation(ChartInterpolation.Linear, ChartStepPosition.Middle));
        var bar = Chart.Create().AddBar("Data", new[] { new ChartPoint(0, 1) });
        bar.Series[0].WithInterpolation(ChartInterpolation.Step);
        Assert.Throws<InvalidOperationException>(() => bar.Prepare(Context()));
        var line = Model(ChartSeriesKind.Line, new[] { new ChartPoint(0, 1) });
        line.Series[0].StepPosition = ChartStepPosition.Middle;
        Assert.Throws<InvalidOperationException>(() => line.Prepare(Context()));
    }

    [Theory]
    [InlineData(ChartInterpolation.Smooth)]
    [InlineData(ChartInterpolation.Step)]
    public void RegressionLinesRejectNonlinearInterpolationRatherThanIgnoringIt(ChartInterpolation interpolation) {
        var chart = Chart.Create().AddTrendLine("Regression", new[] { new ChartPoint(0, 2), new ChartPoint(1, 5), new ChartPoint(2, 8) });
        chart.Series[0].WithInterpolation(interpolation);
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
        chart.Series[0].WithInterpolation(ChartInterpolation.Linear);
        var prepared = chart.Prepare(Context());
        var path = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "trend-line");
        Assert.Equal(2, path.Commands.Count);
        var regression = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "regression");
        Assert.Equal("3", regression.Metadata["data-cfx-slope"]);
        Assert.Equal("2", regression.Metadata["data-cfx-intercept"]);
        Assert.Equal("3", regression.Metadata["data-cfx-source-count"]);
    }

    private static Chart Model(ChartSeriesKind kind, IEnumerable<ChartPoint> points) {
        var chart = Chart.Create().WithAxes(false).WithGrid(false).WithLegend(false).WithDataLabels(false);
        chart.Series.Add(new ChartSeries("Observations", kind, points));
        chart.Series[0].WithMarkerRadius(0);
        return chart;
    }

    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(400, 260)),
        VisualTheme.Graphite(), frame: new VisualFrame("", "", showLegend: false, transparentBackground: true));
}
