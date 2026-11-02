using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Chart look v2 engine defaults, each proven on the scene its shared owner produces.</summary>
public sealed class ChartLookV2DefaultsTests {
    [Fact]
    public void ShortenedTextKeepsSixCharactersOrIsHidden() {
        static double Measure(string text) => text.Length * 7d;
        static double Sized(string text, double _) => Measure(text);
        Assert.Equal("Engine…", ChartTextFitting.TrimEnd("Engineering", 12, 7 * 7, Sized));
        Assert.Equal(string.Empty, ChartTextFitting.TrimEnd("Engineering", 12, 7 * 5, Sized));
        Assert.Equal("Short", ChartTextFitting.TrimEnd("Short", 12, 7 * 5, Sized));

        var style = new TextStyle { FontSize = 12 };
        TextMetrics Metrics(string text, TextStyle _) => new(Measure(text), 14, 14);
        LabelPlacementRequest Request() => new("Engineering", new ChartPoint(50, 20), style, new[] { new LabelCandidate(0, 0, .5, .5) }) {
            Fallback = LabelFallbackRule.EllipsisThenDrop
        };
        var service = new LabelPlacementService();
        var shortened = Assert.Single(service.Place(new[] { Request() }, new ChartRect(25, 0, 50, 40), null, 0, Metrics));
        Assert.Equal("Engine…", shortened.Text);
        var hidden = Assert.Single(service.Place(new[] { Request() }, new ChartRect(30, 0, 40, 40), null, 0, Metrics));
        Assert.True(hidden.IsDropped, "A caption that would keep fewer than six characters is hidden rather than shown as \"Engi…\".");
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void CompactTreeWidensNodesForTheirLongestCaptionWord(VisualThemeMode mode) {
        var prepared = Prepare(TreeRecipe.Create("compact", mode), 360, 360, mode);
        var labels = Texts(prepared, "tree-node-label").Select(text => string.Join(" ", text.Text.Lines.Select(line => line.Text))).ToArray();
        Assert.Contains("Engineering", labels);
        Assert.DoesNotContain(labels, label => label.Contains('…'));
        var nodes = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "tree-node-mark").Select(node => node.Bounds)
            .OrderBy(bounds => bounds.Left).ToArray();
        var columns = nodes.Select(bounds => bounds.Left).Distinct().ToArray();
        for (var index = 1; index < columns.Length; index++)
            Assert.True(columns[index] - columns[index - 1] - nodes[0].Width >= ChartTreeLayout.MinimumLinkRun - .001, "Links keep a readable horizontal run.");
    }

    [Theory]
    [InlineData(ChartSeriesKind.Funnel, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Funnel, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Pyramid, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Pyramid, VisualThemeMode.Dark)]
    public void OrderedStagesShareOneHueAndLightenInOrder(ChartSeriesKind kind, VisualThemeMode mode) {
        var chart = kind == ChartSeriesKind.Funnel ? FunnelRecipe.Create() : PyramidRecipe.Create();
        var prepared = Prepare(chart, 800, 440, mode);
        var role = kind == ChartSeriesKind.Funnel ? "funnel-segment" : "pyramid-segment";
        var fills = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == role).Select(node => node.Fill!.Value).ToArray();
        Assert.True(fills.Length >= 3);
        var hues = fills.Select(Hue).ToArray();
        Assert.All(hues, hue => Assert.InRange(Math.Abs(hue - hues[0]), 0, 3));
        var lightness = fills.Select(Lightness).ToArray();
        for (var index = 1; index < lightness.Length; index++) Assert.True(lightness[index] > lightness[index - 1], "Each later stage is lighter.");
        Assert.True(lightness[lightness.Length - 1] <= .86, "The last stage keeps a visible fill.");
    }

    [Fact]
    public void PyramidKeepsAReadableSlopeSurfaceGapsAndOneCaptionTreatment() {
        var wide = Prepare(PyramidRecipe.Create(), 800, 440);
        var segments = wide.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "pyramid-segment")
            .Select(node => Bounds(node.Commands)).OrderBy(bounds => bounds.Top).ToArray();
        var height = segments.Last().Bottom - segments.First().Top;
        Assert.True(segments.Max(bounds => bounds.Width) <= height * ChartPyramidLayout.DefaultMaximumAspect + 1, "A wide viewport no longer flattens the triangle edge to edge.");
        var separators = wide.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "pyramid-separator").ToArray();
        Assert.Equal(segments.Length - 1, separators.Length);
        for (var index = 1; index < segments.Length; index++) {
            var separator = separators.Single(line => Math.Abs(line.Start.Y - segments[index].Top) < .01);
            Assert.Equal(ChartMarkSurface.SurfaceGap, separator.StrokeWidth, 6);
            Assert.Equal(segments[index - 1].Width, Math.Abs(separator.End.X - separator.Start.X), 1);
        }

        var compact = Prepare(PyramidRecipe.Create("compact", VisualThemeMode.Dark), 360, 360, VisualThemeMode.Dark);
        var labels = Texts(compact, "pyramid-label").Length;
        var leaders = compact.Scene.Nodes.Count(node => node.Role == "data-label-connector");
        Assert.Equal(3, labels);
        Assert.Equal(labels, leaders);
    }

    [Fact]
    public void HistogramBinsStayContiguousSquareAndEmptyBinsHaveNoCaption() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1, 3, 6, 10 });
        var series = ChartSeries.CreateHistogram(new ChartSeriesId("observations"), "Observations", new[] { .2, .4, .8, 1.2, 1.8, 2.5, 6.1, 7.5, 9 }, layout);
        var chart = Chart.Create().WithDataLabels().AddSeries(series);
        chart.Options.XAxis.WithBounds(0, 10);
        var prepared = Prepare(chart, 800, 440);
        var bars = BarBounds(prepared).Where(bounds => bounds.Height > 0).OrderBy(bounds => bounds.Left).ToArray();
        Assert.Equal(3, bars.Length);
        Assert.InRange(bars[1].Left - bars[0].Right, 0, ChartMarkSurface.HistogramSeam + .001);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar"), node => Assert.Equal(0, node.Radius));
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "bar"),
            node => node.Commands.Any(command => command.Kind == ChartPathCommandKind.CubicTo));
        Assert.Equal(3, Texts(prepared, "data-label").Length);
    }

    [Theory]
    [InlineData(ChartStepPosition.End)]
    [InlineData(ChartStepPosition.Start)]
    public void StepPathsCloseTheirOuterTread(ChartStepPosition position) {
        var points = new[] { new ChartPoint(0, 10), new ChartPoint(10, 5), new ChartPoint(20, 8) };
        var commands = ChartPathBuilder.FromPoints(points, ChartInterpolation.Step, position, false, (-100, 100)).Commands;
        // A range that ends at the outer observation (a numeric axis without padding) leaves no tread to draw.
        var bounded = ChartPathBuilder.FromPoints(points, ChartInterpolation.Step, position, false, (0, 20)).Commands;
        Assert.Equal(ChartPathBuilder.FromPoints(points, ChartInterpolation.Step, position).Commands.Count, bounded.Count);
        if (position == ChartStepPosition.End) {
            Assert.Equal(25, commands[commands.Count - 1].X, 6);
            Assert.Equal(8, commands[commands.Count - 1].Y, 6);
            Assert.Equal(20, commands[commands.Count - 2].X, 6);
        } else {
            Assert.Equal(-5, commands[0].X, 6);
            Assert.Equal(10, commands[0].Y, 6);
        }
    }

    [Fact]
    public void GroupedBarsUseTheV2BandAndASurfaceGap() {
        var prepared = Prepare(BarRecipe.Create(), 800, 440);
        var bars = BarBounds(prepared).OrderBy(bounds => bounds.Left).ToArray();
        Assert.Equal(ChartMarkSurface.SurfaceGap, bars[1].Left - bars[0].Right, 3);
        Assert.True(bars[2].Left - bars[1].Right > ChartMarkSurface.SurfaceGap * 4, "Categories keep their own band padding.");
    }

    [Fact]
    public void LegendMarkersMatchTheirMarks() {
        var line = Prepare(LineRecipe.Create(), 800, 440);
        var stroke = line.Scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "legend-swatch");
        Assert.Equal(VisualFrameLayout.LegendLineWidth, stroke.End.X - stroke.Start.X, 6);
        Assert.Equal(VisualFrameLayout.LegendLineStroke, stroke.StrokeWidth, 6);

        var bar = Prepare(BarRecipe.Create(), 800, 440);
        var square = bar.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "legend-swatch");
        Assert.Equal(VisualFrameLayout.LegendSwatchSize, square.Bounds.Width, 6);
        Assert.Equal(VisualFrameLayout.LegendSwatchRadius, square.Radius, 6);
    }

    [Theory]
    [InlineData(800, 440, VisualThemeMode.Light)]
    [InlineData(800, 440, VisualThemeMode.Dark)]
    [InlineData(360, 360, VisualThemeMode.Light)]
    [InlineData(360, 360, VisualThemeMode.Dark)]
    public void PartToWholeLegendListsBesideAWidePlotOrInTwoCompactColumns(int width, int height, VisualThemeMode mode) {
        var prepared = Prepare(PieRecipe.Create(width < 480 ? "compact" : "wide", mode), width, height, mode);
        var entries = prepared.Regions.Where(region => region.Role == "legend").Select(region => region.Bounds).ToArray();
        var slice = prepared.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role != null && node.Outer > 0);
        Assert.True(entries.Length >= 6);
        if (width >= VisualFrameLayout.LegendCompactFrameWidth) {
            Assert.All(entries, entry => Assert.True(entry.Left >= slice.Cx + slice.Outer, "The value list sits right of the pie."));
            Assert.Single(entries.Select(entry => Math.Round(entry.Width, 3)).Distinct());
            // The pie and its list form one group: a fixed gap apart, centred together, the list centred against the pie.
            Assert.InRange(entries.Min(entry => entry.Left) - (slice.Cx + slice.Outer) - VisualFrameLayout.LegendValueListGap, -1.5, 1.5);
            Assert.InRange((slice.Cx - slice.Outer + entries.Max(entry => entry.Right)) / 2 - width / 2d, -1.5, 1.5);
            Assert.InRange((entries.Min(entry => entry.Top) + entries.Max(entry => entry.Bottom)) / 2 - slice.Cy, -1.5, 1.5);
        } else {
            Assert.Equal(2, entries.Select(entry => Math.Round(entry.Left, 3)).Distinct().Count());
            Assert.All(entries, entry => Assert.True(entry.Bottom <= slice.Cy - slice.Outer + .001, "The compact grid stays above the pie."));
        }
        var authored = PieRecipe.Create(width < 480 ? "compact" : "wide", mode);
        authored.Options.LegendPosition = ChartLegendPosition.TopLeft;
        var kept = Prepare(authored, width, height, mode);
        var keptSlice = kept.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Role != null && node.Outer > 0);
        Assert.All(kept.Regions.Where(region => region.Role == "legend"), region =>
            Assert.True(region.Bounds.Bottom <= keptSlice.Cy - keptSlice.Outer + .001, "An authored position keeps the wrapped legend above the plot."));
    }

    [Fact]
    public void WideRadialPlotLimitsItsDiameterToSixTenthsOfTheWidth() {
        // 572 x 372 is wider than 1.4:1 but not so wide that the height alone already limits the pie.
        var prepared = PieRecipe.Create().Prepare(Context(600, 400, VisualThemeMode.Light, new VisualFrame("", "", showLegend: false)));
        var slice = prepared.Scene.Nodes.OfType<VisualSceneSlice>().First(node => node.Outer > 0);
        Assert.True(slice.Outer * 2 <= (600 - 2 * 14) * VisualRadialCompiler.WideDiameterShare + .001);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void GaugeStatusBandsAreMutedAndHealthyBandsQuiet(VisualThemeMode mode) {
        var prepared = Prepare(GaugeRecipe.Create("wide", mode), 800, 440, mode);
        var bands = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "gauge-band").ToArray();
        Assert.NotEmpty(bands);
        Assert.All(bands, band => Assert.InRange(band.Fill!.Value.A, 150, 156));
        var quiet = VisualTheme.Graphite().Resolve(mode).Quiet;
        Assert.Contains(bands, band => band.Fill!.Value.R == quiet.R && band.Fill.Value.G == quiet.G && band.Fill.Value.B == quiet.B);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void GanttLaneCaptionsClearHatchingAndOpenEndArrows(VisualThemeMode mode) {
        var prepared = Prepare(GanttLaneRecipe.Create("compact", mode), 360, 360, mode);
        var captions = Texts(prepared, "data-label");
        Assert.Contains(captions, text => text.Stroke.HasValue && text.StrokeWidth >= text.Text.Size * .25);
        var arrows = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "gantt-lane-open-end").Select(node => Bounds(node.Commands)).ToArray();
        Assert.All(captions, text => {
            var left = text.X; var right = text.X + text.Text.Metrics.Width;
            var top = text.Baseline - text.Text.Ascent; var bottom = top + text.Text.Metrics.Height;
            Assert.DoesNotContain(arrows, arrow => left < arrow.Right && arrow.Left < right && top < arrow.Bottom && arrow.Top < bottom);
        });
    }

    private static VisualRenderContext Context(int width, int height, VisualThemeMode mode, VisualFrame? frame = null) => new(
        new VisualLayoutOptions(new VisualSize(width, height), 14), VisualTheme.Graphite(), mode, frame ?? new VisualFrame(""),
        FontSpec.FromFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf")));

    private static PreparedVisual Prepare(Chart chart, int width, int height, VisualThemeMode mode = VisualThemeMode.Light) =>
        chart.Prepare(Context(width, height, mode));

    private static VisualSceneText[] Texts(PreparedVisual prepared, string role) =>
        prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role).ToArray();

    private static ChartRect[] BarBounds(PreparedVisual prepared) =>
        prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").Select(node => node.Bounds)
            .Concat(prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "bar").Select(node => Bounds(node.Commands))).ToArray();

    private static ChartRect Bounds(IReadOnlyList<ChartPathCommand> commands) {
        var points = new ChartPath(commands).Flatten(2);
        double left = points.Min(point => point.X), top = points.Min(point => point.Y);
        return new ChartRect(left, top, points.Max(point => point.X) - left, points.Max(point => point.Y) - top);
    }

    private static double Lightness(ChartColor color) =>
        (Math.Max(color.R, Math.Max(color.G, color.B)) + Math.Min(color.R, Math.Min(color.G, color.B))) / 510d;

    private static double Hue(ChartColor color) {
        double r = color.R / 255d, g = color.G / 255d, b = color.B / 255d;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), delta = max - min;
        if (delta == 0) return 0;
        var hue = max == r ? (g - b) / delta % 6 : max == g ? (b - r) / delta + 2 : (r - g) / delta + 4;
        return (hue * 60 + 360) % 360;
    }
}
