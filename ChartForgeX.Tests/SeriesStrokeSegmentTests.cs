using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SeriesStrokeSegmentTests {
    private static readonly ChartColor Stroke = ChartColor.FromHex("#2563EB");
    private static readonly double[] Dash = { 6, 5 };

    [Theory]
    [InlineData(ChartSeriesKind.Line, ChartInterpolation.Linear, false)]
    [InlineData(ChartSeriesKind.Line, ChartInterpolation.Smooth, false)]
    [InlineData(ChartSeriesKind.StepLine, ChartInterpolation.Step, false)]
    [InlineData(ChartSeriesKind.Area, ChartInterpolation.Linear, false)]
    [InlineData(ChartSeriesKind.Line, ChartInterpolation.Smooth, true)]
    public void SegmentDashStartsAtItsSourceXOnTheUnsplitStroke(ChartSeriesKind kind, ChartInterpolation interpolation, bool horizontal) {
        var chart = Create(kind, interpolation, horizontal);
        var plain = Prepare(chart);
        var original = Assert.Single(Lines(plain));
        chart.Series[0].WithStrokeStyleFrom(2.5, new ChartStrokeStyle(dash: Dash));
        var prepared = Prepare(chart);
        var pieces = Lines(prepared);
        Assert.Equal(2, pieces.Length);
        Assert.Null(pieces[0].Dash);
        Assert.Equal(Dash, pieces[1].Dash);

        // The dashed piece starts exactly where the solid one stops, halfway between observations 2 and 3.
        var stop = pieces[0].Commands.Last(); var start = pieces[1].Commands.First();
        Assert.Equal(ChartPathCommandKind.MoveTo, start.Kind);
        Assert.Equal((stop.X, stop.Y), (start.X, start.Y));
        double Along(ChartRect bounds) => horizontal ? bounds.Top + bounds.Height / 2 : bounds.Left + bounds.Width / 2;
        var observations = prepared.Scene.Regions.Where(region => region.Role == "point").Select(region => Along(region.Bounds)).ToArray();
        Assert.Equal((observations[1] + observations[2]) / 2, horizontal ? start.Y : start.X, 9);
        // The cut follows the unsplit curve and keeps both outer ends.
        var curve = new ChartPath(original.Commands).Flatten(1);
        Assert.InRange(curve.Zip(curve.Skip(1), (a, b) => Distance(a, b, new ChartPoint(start.X, start.Y))).Min(), 0, .1);
        Assert.Equal((original.Commands.First().X, original.Commands.First().Y), (pieces[0].Commands.First().X, pieces[0].Commands.First().Y));
        Assert.Equal((original.Commands.Last().X, original.Commands.Last().Y), (pieces[1].Commands.Last().X, pieces[1].Commands.Last().Y));
        Assert.Equal(Markers(plain), Markers(prepared));
    }

    [Fact]
    public void NativePngAndSvgRasterChangeOnlyAfterTheSegmentStarts() {
        var chart = Chart.Create().WithSize(420, 90).WithSparkline().WithYAxisBounds(0, 100).WithLineVisualStyle(ChartLineVisualStyle.Plain())
            .AddLine("Signal", new[] { new ChartPoint(0, 50), new ChartPoint(100, 50) }, Stroke);
        var series = chart.Series[0].WithMarkerRadius(0).WithStrokeStyle(new ChartStrokeStyle(width: 4));
        var solid = Prepare(chart);
        series.WithStrokeStyleFrom(40, new ChartStrokeStyle(dash: new[] { 9d, 15d }));
        var segmented = Prepare(chart);
        var join = Lines(segmented)[1].Commands.First().X;
        foreach (var (before, after) in new[] {
            (RasterImageDecoder.Decode(solid.ToPng()), RasterImageDecoder.Decode(segmented.ToPng())),
            (SvgRasterizer.ToImage(solid.ToSvg()), SvgRasterizer.ToImage(segmented.ToSvg())) }) {
            var changed = Enumerable.Range(0, after.Width).Where(x => Column(before, x) != Column(after, x)).ToArray();
            Assert.NotEmpty(changed);
            // Round caps may reach half the 4px stroke back across the join, never further.
            Assert.True(changed.Min() >= join - 2.5, "Stroke before the segment must stay solid; first change at x=" + changed.Min() + ", join " + join);
        }
    }

    [Fact]
    public void LegendKeyShowsTheSolidStartThenTheDashAndCapturedVisibilityKeepsSegments() {
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, 60), new ChartPoint(3, 40) };
        var series = ChartSeries.CreateLine(new ChartSeriesId("demand"), "Demand", points, Stroke)
            .WithStrokeStyleFrom(2, new ChartStrokeStyle(dash: Dash));
        var chart = Chart.Create().WithSize(420, 300).WithHeader(false).WithDataLabels(false).AddSeries(series)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("other"), "Other", points, Stroke));
        var keys = Prepare(chart).Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "legend-line").ToArray();
        Assert.Equal(2, keys.Length);
        Assert.Null(keys[0].Dash);
        Assert.Equal(Dash, keys[1].Dash);
        Assert.Equal(keys[0].End, keys[1].Start);
        Assert.Equal(Dash[0], keys[1].DashOffset);

        var context = VisualExportRequest.ForChart(chart).Context;
        var reflowed = chart.PrepareVisibility(context, new ChartVisibilityView(new[] { ChartVisibilityTarget.Series(new ChartSeriesId("other")) },
            layout: ChartVisibilityLayout.Reflow));
        Assert.Equal(new[] { (IReadOnlyList<double>?)null, Dash }, Lines(reflowed).Select(line => line.Dash));

        // A custom line key draws the same two parts.
        chart.Options.Legend.KeyShape = ChartLegendKeyShape.Line;
        var custom = Prepare(chart).Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "legend-line").ToArray();
        Assert.Equal(3, custom.Length);
        Assert.Null(custom[0].Dash);
        Assert.Equal(Dash, custom[1].Dash);
        Assert.Equal(custom[0].End, custom[1].Start);
    }

    private static VisualScenePath[] Lines(PreparedVisual prepared) =>
        prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "line").ToArray();

    private static string[] Markers(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneMark>()
        .Where(mark => mark.Role?.StartsWith("marker", StringComparison.Ordinal) == true)
        .Select(mark => mark.GetType().Name + ":" + mark.Fill + ":" + VisualSceneBounds(mark)).ToArray();

    private static string VisualSceneBounds(VisualSceneMark mark) => mark switch {
        VisualSceneEllipse ellipse => ellipse.Cx + "," + ellipse.Cy,
        VisualScenePath path => string.Join(" ", path.Commands.Select(command => command.X + "," + command.Y)),
        _ => string.Empty
    };

    private static long Column(RgbaImage image, int x) {
        long sum = 0;
        for (var y = 0; y < image.Height; y++) {
            var offset = (y * image.Width + x) * 4;
            sum = sum * 31 + image.Pixels[offset] + image.Pixels[offset + 1] * 7 + image.Pixels[offset + 2] * 13 + image.Pixels[offset + 3] * 17;
        }
        return sum;
    }

    private static double Distance(ChartPoint a, ChartPoint b, ChartPoint point) {
        double dx = b.X - a.X, dy = b.Y - a.Y, length = dx * dx + dy * dy;
        var t = length == 0 ? 0 : Math.Max(0, Math.Min(1, ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / length));
        double x = a.X + t * dx - point.X, y = a.Y + t * dy - point.Y;
        return Math.Sqrt(x * x + y * y);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);

    private static Chart Create(ChartSeriesKind kind, ChartInterpolation interpolation, bool horizontal) {
        var chart = Chart.Create().WithSize(420, 300).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithLineVisualStyle(ChartLineVisualStyle.Plain());
        if (horizontal) chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, 60), new ChartPoint(3, 40), new ChartPoint(4, 70) };
        if (kind == ChartSeriesKind.StepLine) chart.AddStepLine("Series", points, Stroke);
        else if (kind == ChartSeriesKind.Area) chart.AddArea("Series", points, Stroke);
        else chart.AddLine("Series", points, Stroke);
        if (kind == ChartSeriesKind.Line) chart.Series[0].WithInterpolation(interpolation);
        chart.Series[0].WithMarkerRadius(4);
        return chart;
    }
}
