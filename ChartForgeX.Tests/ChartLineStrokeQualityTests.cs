using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Chart, axis, and topology lines in PNG output are outlined by the same stroker as the SVG raster,
/// so they keep one width along their length, never double-blend where segments meet, dash like
/// <c>stroke-dasharray</c>, and land where the SVG draws them. Each measurement renders the scene
/// with and without the line, so everything else cancels out and only the line's coverage is left.
/// </summary>
public sealed class ChartLineStrokeQualityTests {
    private static readonly ChartColor Ink = ChartColor.FromRgb(20, 40, 160);

    [Fact]
    public void ThreefoldSupersamplingKeepsFractionalHorizontalStrokeWidth() {
        var canvas = new RgbaCanvas(40, 25, 3);
        canvas.DrawLine(5, 10.5, 35, 10.5, ChartColors.White, 1.4);
        var pixels = canvas.ToOutputPixels();
        var coverage = 0.0;
        for (var y = 0; y < 25; y++) coverage += pixels[(y * 40 + 20) * 4 + 3] / 255.0;
        Assert.InRange(coverage, 1.34, 1.47);
    }

    [Theory]
    [InlineData(1.5, false)]
    [InlineData(4, false)]
    [InlineData(3, true)]
    [InlineData(9, true)]
    public void SeriesLineKeepsOneCrossSectionAlongItsLength(double width, bool smooth) {
        var points = Wave(24);
        var coverage = Coverage(LineChart(points, Ink, width, smooth).ToRgbaImage(), LineChart(points, Hidden(Ink), width, smooth).ToRgbaImage(), Ink);
        var sections = CrossSections(coverage);
        Assert.True(sections.Count > 300, $"Expected the line to cross most columns, found {sections.Count}.");
        var tolerance = Math.Max(0.16, width * 0.04);
        var least = sections.Min();
        var most = sections.Max();
        Assert.True(least > width - tolerance && most < width + tolerance,
            FormattableString.Invariant($"A {width} px series line should be {width} px across everywhere, but ranged from {least:0.###} to {most:0.###}."));
    }

    [Fact]
    public void TranslucentSeriesDoesNotDarkenWhereItsSegmentsJoin() {
        const byte alpha = 110;
        var color = ChartColor.FromRgba(Ink.R, Ink.G, Ink.B, alpha);
        var zigzag = Enumerable.Range(0, 30).Select(i => new ChartPoint(i, i % 2 == 0 ? 20 : 80)).ToArray();
        var coverage = Coverage(LineChart(zigzag, color, 7, false).ToRgbaImage(), LineChart(zigzag, Hidden(color), 7, false).ToRgbaImage(), Ink);
        var strongest = coverage.Cast<double>().Max();
        var solid = coverage.Cast<double>().Count(value => value > alpha / 255.0 - 0.02);
        // One blend of a 110/255 colour covers 43% of the way to it; a second blend at a join would reach 68%.
        Assert.InRange(strongest, alpha / 255.0 - 0.02, alpha / 255.0 + 0.02);
        Assert.True(solid > 5000, "The zigzag should be painted as a solid translucent band.");
    }

    [Fact]
    public void ReferenceLineDashesMatchTheSvgDashPattern() {
        var magenta = ChartColor.FromRgb(200, 0, 200);
        var withLine = ReferenceLineChart(magenta);
        var withoutLine = ReferenceLineChart(Hidden(magenta));
        var png = DashRuns(Coverage(withLine.ToRgbaImage(), withoutLine.ToRgbaImage(), magenta), 1.6);
        var svg = DashRuns(Coverage(SvgRasterizer.ToImage(withLine.ToSvg()), SvgRasterizer.ToImage(withoutLine.ToSvg()), magenta), 1.6);
        // stroke-dasharray="6 5" with butt caps: 6 px dashes and 5 px gaps.
        Assert.True(png.Dashes.Count > 20 && svg.Dashes.Count > 20);
        Assert.InRange(png.Dashes.Average(), 5.6, 6.4);
        Assert.InRange(png.Gaps.Average(), 4.6, 5.4);
        Assert.InRange(Math.Abs(png.Dashes.Average() - svg.Dashes.Average()), 0, 0.3);
        Assert.InRange(Math.Abs(png.Gaps.Average() - svg.Gaps.Average()), 0, 0.3);
    }

    [Fact]
    public void AxisLineKeepsItsStrokeWidth() {
        var png = Coverage(AxisChart(true).ToRgbaImage(), AxisChart(false).ToRgbaImage());
        var svg = Coverage(SvgRasterizer.ToImage(AxisChart(true).ToSvg()), SvgRasterizer.ToImage(AxisChart(false).ToSvg()));
        var pngWidths = ColumnInk(png).Where(value => value > 0.5).ToList();
        var svgWidths = ColumnInk(svg).Where(value => value > 0.5).ToList();
        Assert.True(pngWidths.Count > 300 && svgWidths.Count > 300);
        // ChartVisualPrimitives.AxisStrokeWidth is 1.2; the axis colour is opaque.
        Assert.InRange(Median(pngWidths), 1.1, 1.3);
        Assert.InRange(Math.Abs(Median(pngWidths) - Median(svgWidths)), 0, 0.1);
    }

    [Fact]
    public void TopologyEdgesKeepTheirWidthAndLandWhereTheSvgDrawsThem() {
        var withEdges = Topology(includeEdges: true);
        var withoutEdges = Topology(includeEdges: false);
        var options = new TopologyRenderOptions { IncludeLegend = false };
        var png = Coverage(withEdges.ToRgbaImage(options), withoutEdges.ToRgbaImage(options));
        var svg = Coverage(SvgRasterizer.ToImage(withEdges.ToSvg(options)), SvgRasterizer.ToImage(withoutEdges.ToSvg(options)));
        Assert.Equal(svg.GetLength(0), png.GetLength(0));
        Assert.Equal(svg.GetLength(1), png.GetLength(1));

        // The straight edge runs between the node cards; compare its vertical profile column by column.
        var compared = 0;
        for (var x = 230; x < 410; x += 4) {
            var pngProfile = Profile(png, x, 60, 220);
            var svgProfile = Profile(svg, x, 60, 220);
            if (pngProfile.Ink < 0.5 || svgProfile.Ink < 0.5) continue;
            compared++;
            Assert.InRange(Math.Abs(pngProfile.Center - svgProfile.Center), 0, 1);
            Assert.InRange(Math.Abs(pngProfile.Ink - svgProfile.Ink), 0, 0.25);
        }

        Assert.True(compared > 30, $"Expected the straight edge in most sampled columns, found {compared}.");
        // The bent edge's two vertical runs, x 517.3 over y 171-222 and x 562.7 over y 222-273: compare their horizontal profiles row by row.
        compared = 0;
        for (var y = 178; y < 268; y += 3) {
            if (y > 214 && y < 230) continue;
            var pngProfile = y < 222 ? RowProfile(png, y, 490, 545) : RowProfile(png, y, 535, 590);
            var svgProfile = y < 222 ? RowProfile(svg, y, 490, 545) : RowProfile(svg, y, 535, 590);
            if (pngProfile.Ink < 0.5 || svgProfile.Ink < 0.5) continue;
            compared++;
            Assert.InRange(Math.Abs(pngProfile.Center - svgProfile.Center), 0, 1);
            Assert.InRange(Math.Abs(pngProfile.Ink - svgProfile.Ink), 0, 0.25);
        }

        Assert.True(compared > 20, $"Expected the bent edge in most sampled rows, found {compared}.");
    }

    [Fact]
    public void SparklinePathLandsWhereTheSvgDrawsIt() {
        var amber = ChartColor.FromRgb(217, 119, 6);
        Chart Sparkline(ChartColor color) => Chart.Create().WithSize(360, 90).WithSparkline().WithLineVisualStyle(ChartLineVisualStyle.Plain()).AddLine("Signal", Wave(16), color);
        var png = Coverage(Sparkline(amber).ToRgbaImage(), Sparkline(Hidden(amber)).ToRgbaImage(), amber);
        var svg = Coverage(SvgRasterizer.ToImage(Sparkline(amber).ToSvg()), SvgRasterizer.ToImage(Sparkline(Hidden(amber)).ToSvg()), amber);
        var compared = 0;
        for (var x = 20; x < 340; x += 5) {
            var pngProfile = Profile(png, x, 0, 89);
            var svgProfile = Profile(svg, x, 0, 89);
            if (pngProfile.Ink < 0.5 || svgProfile.Ink < 0.5) continue;
            compared++;
            Assert.InRange(Math.Abs(pngProfile.Center - svgProfile.Center), 0, 1);
            Assert.InRange(Math.Abs(pngProfile.Ink - svgProfile.Ink), 0, 0.3);
        }

        Assert.True(compared > 40, $"Expected the sparkline in most sampled columns, found {compared}.");
    }

    private static Chart LineChart(IReadOnlyList<ChartPoint> points, ChartColor color, double width, bool smooth) {
        var chart = Chart.Create().WithSize(640, 400).WithHeader(false).WithLegend(false).WithGrid(false).WithAxes(false).WithLineVisualStyle(ChartLineVisualStyle.Plain());
        if (smooth) chart.AddSmoothLine("Series", points, color);
        else chart.AddLine("Series", points, color);
        chart.Series[0].WithStrokeWidth(width).WithMarkerRadius(0);
        return chart;
    }

    private static Chart ReferenceLineChart(ChartColor color) =>
        Chart.Create().WithSize(640, 300).WithHeader(false).WithLegend(false).WithGrid(false).WithLineVisualStyle(ChartLineVisualStyle.Plain())
            .AddLine("Series", new[] { new ChartPoint(0, 10), new ChartPoint(10, 12) }, ChartColor.FromRgba(0, 0, 0, 0))
            .AddHorizontalLine(40, "", color);

    private static Chart AxisChart(bool showLine) {
        var chart = Chart.Create().WithSize(640, 300).WithHeader(false).WithLegend(false).WithGrid(false)
            .AddLine("Series", new[] { new ChartPoint(0, 10), new ChartPoint(10, 12) }, ChartColor.FromRgba(0, 0, 0, 0));
        chart.Options.XAxis.ShowLine = showLine;
        chart.Options.YAxis.ShowLine = false;
        return chart;
    }

    private static TopologyChart Topology(bool includeEdges) {
        var topology = TopologyChart.Create();
        topology.Id = "stroke-quality";
        topology.Viewport.Width = 640;
        topology.Viewport.Height = 360;
        topology.AddNode("a", "Alpha", 60, 100, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("b", "Beta", 440, 100, TopologyNodeKind.Service, TopologyHealthStatus.Healthy)
            .AddNode("c", "Gamma", 520, 280, TopologyNodeKind.Service, TopologyHealthStatus.Healthy);
        if (includeEdges) {
            topology.AddEdge("ab", "a", "b", kind: TopologyEdgeKind.Dependency, status: TopologyHealthStatus.Healthy)
                .AddEdge("bc", "b", "c", kind: TopologyEdgeKind.Dependency, status: TopologyHealthStatus.Healthy);
        }

        return topology;
    }

    private static ChartPoint[] Wave(int count) => Enumerable.Range(0, count).Select(i => new ChartPoint(i, 50 + Math.Sin(i * 0.45) * 30)).ToArray();

    private static ChartColor Hidden(ChartColor color) => ChartColor.FromRgba(color.R, color.G, color.B, 0);

    /// <summary>
    /// Per-pixel coverage of the line: how far each pixel moved from the scene without the line toward the
    /// line colour, both composited over white. Without a known colour the most moved pixel is taken as fully covered.
    /// </summary>
    private static double[,] Coverage(RgbaImage with, RgbaImage without, ChartColor? color = null) {
        Assert.Equal(without.Width, with.Width);
        Assert.Equal(without.Height, with.Height);
        var target = color.HasValue ? new[] { (double)color.Value.R, color.Value.G, color.Value.B } : MostMoved(with, without);
        var coverage = new double[with.Width, with.Height];
        for (var y = 0; y < with.Height; y++) for (var x = 0; x < with.Width; x++) {
            var best = 0.0;
            var bestRange = 0.0;
            for (var channel = 0; channel < 3; channel++) {
                var background = OverWhite(without, x, y, channel);
                var range = target[channel] - background;
                if (Math.Abs(range) <= bestRange) continue;
                bestRange = Math.Abs(range);
                best = (OverWhite(with, x, y, channel) - background) / range;
            }

            coverage[x, y] = bestRange < 24 ? 0 : Math.Max(0, Math.Min(1, best));
        }

        return coverage;
    }

    private static double[] MostMoved(RgbaImage with, RgbaImage without) {
        var best = new double[3];
        var bestDistance = -1.0;
        for (var y = 0; y < with.Height; y++) for (var x = 0; x < with.Width; x++) {
            var distance = 0.0;
            for (var channel = 0; channel < 3; channel++) distance += Math.Abs(OverWhite(with, x, y, channel) - OverWhite(without, x, y, channel));
            if (distance <= bestDistance) continue;
            bestDistance = distance;
            for (var channel = 0; channel < 3; channel++) best[channel] = OverWhite(with, x, y, channel);
        }

        return best;
    }

    private static double OverWhite(RgbaImage image, int x, int y, int channel) {
        var offset = (y * image.Width + x) * 4;
        var alpha = image.Pixels[offset + 3] / 255.0;
        return image.Pixels[offset + channel] * alpha + 255 * (1 - alpha);
    }
    /// <summary>Width of the line perpendicular to itself in every column it crosses: vertical ink scaled by the local slope.</summary>
    private static List<double> CrossSections(double[,] coverage) {
        var width = coverage.GetLength(0);
        var centers = new double?[width];
        var ink = new double[width];
        for (var x = 0; x < width; x++) {
            var profile = Profile(coverage, x, 0, coverage.GetLength(1) - 1);
            ink[x] = profile.Ink;
            if (profile.Ink > 0.3) centers[x] = profile.Center;
        }

        const int window = 6;
        var sections = new List<double>();
        for (var x = window; x < width - window; x++) {
            if (centers[x - window] is not double left || centers[x + window] is not double right || centers[x] is not double center) continue;
            // A vertical cut through a corner is not a cross-section, so columns near a polyline vertex are skipped.
            var leftSlope = (center - left) / window;
            var rightSlope = (right - center) / window;
            if (Math.Abs(leftSlope - rightSlope) > 0.15) continue;
            var slope = (right - left) / (2 * window);
            sections.Add(ink[x] / Math.Sqrt(1 + slope * slope));
        }

        // The first and last few columns hold the round caps.
        return sections.Skip(12).Take(Math.Max(0, sections.Count - 24)).ToList();
    }

    private static IEnumerable<double> ColumnInk(double[,] coverage) {
        for (var x = 0; x < coverage.GetLength(0); x++) yield return Profile(coverage, x, 0, coverage.GetLength(1) - 1).Ink;
    }

    private static (double Ink, double Center) Profile(double[,] coverage, int x, int top, int bottom) {
        var ink = 0.0;
        var moment = 0.0;
        for (var y = top; y <= bottom; y++) {
            ink += coverage[x, y];
            moment += coverage[x, y] * (y + 0.5);
        }

        return (ink, ink > 0 ? moment / ink : 0);
    }

    private static (double Ink, double Center) RowProfile(double[,] coverage, int y, int left, int right) {
        var ink = 0.0;
        var moment = 0.0;
        for (var x = left; x <= right; x++) {
            ink += coverage[x, y];
            moment += coverage[x, y] * (x + 0.5);
        }

        return (ink, ink > 0 ? moment / ink : 0);
    }

    /// <summary>Lengths of the painted and blank runs along the dashed line, measured where half of its width is inked.</summary>
    private static (List<double> Dashes, List<double> Gaps) DashRuns(double[,] coverage, double strokeWidth) {
        var width = coverage.GetLength(0);
        var height = coverage.GetLength(1);
        var bestRow = 0;
        var bestInk = 0.0;
        for (var y = 0; y < height; y++) {
            var rowInk = 0.0;
            for (var x = 0; x < width; x++) rowInk += coverage[x, y];
            if (rowInk > bestInk) {
                bestInk = rowInk;
                bestRow = y;
            }
        }

        var column = new double[width];
        for (var x = 0; x < width; x++) for (var y = Math.Max(0, bestRow - 3); y <= Math.Min(height - 1, bestRow + 3); y++) column[x] += coverage[x, y];
        // Each dash is the ink of its run plus the partly covered pixel at either end; the gap is the
        // distance to the next dash's centroid less the dash lengths.
        var runs = new List<(double Length, double Center)>();
        for (var x = 1; x < width - 1; x++) {
            if (column[x] <= strokeWidth / 2 || column[x - 1] > strokeWidth / 2) continue;
            var end = x;
            while (end + 1 < width && column[end + 1] > strokeWidth / 2) end++;
            var ink = 0.0;
            var moment = 0.0;
            for (var i = x - 1; i <= Math.Min(width - 1, end + 1); i++) {
                var amount = Math.Min(1, column[i] / strokeWidth);
                ink += amount;
                moment += amount * (i + 0.5);
            }

            runs.Add((ink, moment / ink));
            x = end;
        }

        var dashes = new List<double>();
        var gaps = new List<double>();
        // Skip the first and last dash, which the plot edges may clip.
        for (var i = 1; i + 1 < runs.Count; i++) {
            dashes.Add(runs[i].Length);
            if (i + 2 < runs.Count) gaps.Add(runs[i + 1].Center - runs[i].Center - (runs[i].Length + runs[i + 1].Length) / 2);
        }
        return (dashes, gaps);
    }

    private static double Median(List<double> values) {
        var sorted = values.OrderBy(value => value).ToList();
        return sorted[sorted.Count / 2];
    }
}
