using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Guards the library-wide visual rules of <c>docs/design-language.md</c> §2 over every chart family in the gallery
/// (every <see cref="ChartSeriesKind"/> with a gallery recipe, wide and compact, light and dark). A new family inherits
/// these checks as soon as it has a gallery recipe; a renderer that bypasses the shared mechanisms fails here.
/// </summary>
public sealed class DesignRuleGuardTests {
    private static readonly string[] Variants = { "wide", "compact" };

    public static IEnumerable<object[]> GalleryFamilies() {
        foreach (var kind in Enum.GetValues(typeof(ChartSeriesKind)).Cast<ChartSeriesKind>())
            foreach (var variant in Variants)
                if (TryCreate(kind, variant, VisualThemeMode.Light) != null) yield return new object[] { kind, variant };
    }

    [Fact]
    public void GalleryManifest_CoversTheCartesianScheduleScalarHierarchyAndMatrixFamilies() {
        var kinds = GalleryFamilies().Select(row => (ChartSeriesKind)row[0]).Distinct().ToArray();
        Assert.True(kinds.Length >= 40, "Only " + kinds.Length + " gallery families resolved.");
        foreach (var kind in new[] { ChartSeriesKind.Bar, ChartSeriesKind.HorizontalBar, ChartSeriesKind.Line, ChartSeriesKind.Waterfall,
            ChartSeriesKind.Gantt, ChartSeriesKind.Bullet, ChartSeriesKind.Heatmap, ChartSeriesKind.Treemap, ChartSeriesKind.Gauge })
            Assert.Contains(kind, kinds);
    }

    /// <summary>Rule 1: an automatic linear value axis ends on a gridline (its next clean tick), never between ticks.</summary>
    [Theory]
    [MemberData(nameof(GalleryFamilies))]
    public void ValueAxis_EndsOnANiceTick(ChartSeriesKind kind, string variant) {
        foreach (var mode in Modes) {
            var chart = TryCreate(kind, variant, mode)!;
            var prepared = Prepare(chart, mode);
            if (prepared.Scene.Regions.FirstOrDefault(region => region.Role == "cartesian-plot") is not { } plotRegion) continue;
            var plot = plotRegion.Bounds;
            var lines = prepared.Scene.Nodes.OfType<VisualSceneLine>().ToArray();
            var horizontalGrid = lines.Where(line => line.Role == "grid-y").ToArray();
            var verticalGrid = lines.Where(line => line.Role == "grid-x").ToArray();
            var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
            var axis = ChartCartesianOrientation.ValueAxis(chart, ChartAxisSide.Primary);
            if (!ChartValueAxisRange.Applies(horizontal ? Clean(axis) : axis) || axis.Maximum.HasValue || chart.Options.IsSparkline) continue;
            if (chart.Series.Any(series => series.ValueAxis == ChartAxisSide.Secondary)) continue;
            if (!horizontal && horizontalGrid.Length > 1)
                Assert.True(Math.Abs(horizontalGrid.Min(line => line.Start.Y) - plot.Top) < .75,
                    $"{kind}/{variant}/{mode}: the top gridline ({horizontalGrid.Min(line => line.Start.Y):0.##}) is not the plot top ({plot.Top:0.##}); the value axis ends between ticks.");
            if (horizontal && verticalGrid.Length > 1 && axis.Maximum == null) {
                var right = verticalGrid.Max(line => line.Start.X); var left = verticalGrid.Min(line => line.Start.X);
                Assert.True(Math.Abs(right - plot.Right) < .75 || Math.Abs(left - plot.Left) < .75 && chart.Options.XAxis.Reversed,
                    $"{kind}/{variant}/{mode}: the outer value gridline ({right:0.##}) is not the plot edge ({plot.Right:0.##}).");
            }
        }
    }

    /// <summary>Rule 4: bars and columns are square at the baseline; only the data end is rounded.</summary>
    [Theory]
    [MemberData(nameof(GalleryFamilies))]
    public void Bars_AreSquareAtTheBaseline(ChartSeriesKind kind, string variant) {
        foreach (var mode in Modes) {
            var chart = TryCreate(kind, variant, mode)!;
            var prepared = Prepare(chart, mode);
            foreach (var sceneNode in prepared.Scene.Nodes) {
                VisualSceneNode node = sceneNode is VisualSceneGradient gradient ? gradient.Shape : sceneNode;
                if (node.Role is not ("bar" or "horizontal-bar" or "waterfall-bar")) continue;
                var horizontal = node.Role == "horizontal-bar" || node.Role == "waterfall-bar" && chart.Options.Waterfall.Orientation == ChartOrientation.Horizontal;
                if (node is VisualSceneRectangle rectangle) {
                    if (rectangle.Radius <= 0 || rectangle.Bounds.Width < 1 || rectangle.Bounds.Height < 1) continue;
                    var dataEnd = horizontal ? rectangle.Corners is VisualCorners.Left or VisualCorners.Right
                        : rectangle.Corners is VisualCorners.Top or VisualCorners.Bottom;
                    Assert.True(dataEnd, $"{kind}/{variant}/{mode}: a {node.Role} rounds {rectangle.Corners} corners; only its data end may be rounded.");
                    continue;
                }
                if (node is not VisualScenePath path) continue;
                var vertices = Vertices(path).ToArray();
                var box = Box(vertices);
                if (box.Width < 1 || box.Height < 1) continue;
                bool Square(double x, double y) => vertices.Any(point => Math.Abs(point.X - x) < .01 && Math.Abs(point.Y - y) < .01);
                var squareEnd = horizontal
                    ? Square(box.Left, box.Top) && Square(box.Left, box.Bottom) || Square(box.Right, box.Top) && Square(box.Right, box.Bottom)
                    : Square(box.Left, box.Bottom) && Square(box.Right, box.Bottom) || Square(box.Left, box.Top) && Square(box.Right, box.Top);
                Assert.True(squareEnd, $"{kind}/{variant}/{mode}: a {node.Role} is rounded at its baseline as well as its data end.");
            }
        }
    }

    /// <summary>Rule 4: stacked segments and touching bars are separated by a 2px surface gap.</summary>
    [Theory]
    [MemberData(nameof(GalleryFamilies))]
    public void AdjacentBars_KeepASurfaceGap(ChartSeriesKind kind, string variant) {
        foreach (var mode in Modes) {
            var prepared = Prepare(TryCreate(kind, variant, mode)!, mode);
            var boxes = prepared.Scene.Nodes.Where(node => node.Role is "bar" or "horizontal-bar")
                .Select(node => node is VisualSceneRectangle rectangle ? rectangle.Bounds : node is VisualScenePath path ? Box(Vertices(path).ToArray()) : default)
                .Where(box => box.Width >= 1 && box.Height >= 1).ToArray();
            for (var first = 0; first < boxes.Length; first++)
                for (var second = first + 1; second < boxes.Length; second++) {
                    var a = boxes[first]; var b = boxes[second];
                    var across = Overlap(a.Left, a.Right, b.Left, b.Right) >= Math.Min(a.Width, b.Width) * .5;
                    var along = Overlap(a.Top, a.Bottom, b.Top, b.Bottom) >= Math.Min(a.Height, b.Height) * .5;
                    var gap = across ? Math.Max(b.Top - a.Bottom, a.Top - b.Bottom) : along ? Math.Max(b.Left - a.Right, a.Left - b.Right) : double.NaN;
                    Assert.False(gap >= -.01 && gap < ChartMarkSurface.SurfaceGap - .1,
                        $"{kind}/{variant}/{mode}: two bars touch with a {gap:0.##}px gap; adjacent marks need a {ChartMarkSurface.SurfaceGap}px surface gap.");
                }
        }
    }

    /// <summary>Rule 10 (and §1): a status colour appears only on status-encoded data, which carries its status in the DOM.</summary>
    [Theory]
    [MemberData(nameof(GalleryFamilies))]
    public void StatusRoles_OnlyPaintStatusEncodedData(ChartSeriesKind kind, string variant) {
        foreach (var mode in Modes) {
            var svg = TryCreate(kind, variant, mode)!.WithThemeMode(mode).WithSvgColorVariables(SvgColorVariables.SharedRoles(mode: mode)).ToSvg();
            foreach (var element in XDocument.Parse(svg).Descendants()) {
                var paint = string.Join(" ", element.Attributes().Where(attribute => attribute.Name.LocalName is "fill" or "stroke").Select(attribute => attribute.Value));
                if (!StatusVariable.IsMatch(paint)) continue;
                Assert.True(element.AncestorsAndSelf().Any(StatusEncoded),
                    $"{kind}/{variant}/{mode}: {Role(element)} is painted with a status role ({StatusVariable.Match(paint).Value}) but carries no status encoding.");
            }
        }
    }

    /// <summary>Rule 8: a legend never lists the same words as the row or category labels beside the marks.</summary>
    [Theory]
    [MemberData(nameof(GalleryFamilies))]
    public void Legend_DoesNotRepeatRowLabels(ChartSeriesKind kind, string variant) {
        foreach (var mode in Modes) {
            var prepared = Prepare(TryCreate(kind, variant, mode)!, mode);
            var texts = prepared.Scene.Nodes.OfType<VisualSceneText>().ToArray();
            var legend = texts.Where(text => text.Role == "legend-label").Select(Words).Distinct().ToArray();
            if (legend.Length < 2) continue;
            var rows = new HashSet<string>(texts.Where(text => RowLabelRoles.Contains(text.Role ?? string.Empty)).Select(Words));
            Assert.False(legend.All(rows.Contains), $"{kind}/{variant}/{mode}: the legend ({string.Join(", ", legend)}) repeats the row labels.");
        }
    }

    /// <summary>Rule 9: a numeric heatmap's colour key has a title and starts at the plot's left edge, under the first column.</summary>
    [Theory]
    [MemberData(nameof(GalleryFamilies))]
    public void HeatmapColorKey_HasATitleAndStartsAtThePlot(ChartSeriesKind kind, string variant) {
        foreach (var mode in Modes) {
            var prepared = Prepare(TryCreate(kind, variant, mode)!, mode);
            var nodes = prepared.Scene.Nodes;
            var steps = nodes.OfType<VisualSceneRectangle>().Where(node => node.Id != null && node.Id.StartsWith("matrix-scale", StringComparison.Ordinal)).ToArray();
            if (steps.Length == 0) continue;
            Assert.Contains(nodes.OfType<VisualSceneText>(), text => text.Role == "heatmap-scale-title" && Words(text).Length > 0);
            var plot = prepared.Regions.Single(region => region.Role == "matrix-plot").Bounds;
            Assert.InRange(steps.Min(step => step.Bounds.Left) - plot.Left, -.5, 12.5);
        }
    }

    /// <summary>Accessible names read like the chart ("Sat 24, Updates: 2"), never as a bare "(x, y)" coordinate tuple.</summary>
    [Theory]
    [MemberData(nameof(GalleryFamilies))]
    public void AccessibleNames_NeverExposeACoordinateTuple(ChartSeriesKind kind, string variant) {
        var tuple = new System.Text.RegularExpressions.Regex(@"\(\s*-?[\d.,E+-]+\s*,\s*-?[\d.,E+-]+\s*\)");
        foreach (var mode in Modes) {
            var prepared = Prepare(TryCreate(kind, variant, mode)!, mode);
            var names = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Select(group => group.Metadata.TryGetValue("aria-label", out var name) ? name : null)
                .Concat(prepared.Regions.Select(region => region.Label)).Where(name => !string.IsNullOrEmpty(name));
            var raw = names.FirstOrDefault(name => tuple.IsMatch(name!));
            Assert.True(raw == null, $"{kind}/{variant}/{mode}: \"{raw}\" exposes a coordinate tuple.");
        }
    }

    /// <summary>Rule 8: a label drawn on a mark fits inside it; labels that do not fit move outside or to the tooltip, never clipped.</summary>
    [Theory]
    [MemberData(nameof(GalleryFamilies))]
    public void Text_DoesNotOverflowItsMark(ChartSeriesKind kind, string variant) {
        foreach (var mode in Modes) {
            var prepared = Prepare(TryCreate(kind, variant, mode)!, mode);
            var size = prepared.Scene.Size;
            var marks = prepared.Scene.Nodes.Where(node => MarkRoles.Contains(node.Role ?? string.Empty))
                .Select(node => node is VisualSceneRectangle rectangle ? rectangle.Bounds : node is VisualScenePath path ? Box(Vertices(path).ToArray()) : default)
                .Where(box => box.Width > 0 && box.Height > 0).ToArray();
            foreach (var text in UnrotatedTexts(prepared.Scene.Nodes)) {
                foreach (var line in text.Text.Lines) {
                    if (line.Text.Trim().Length == 0) continue;
                    var left = text.LineLeft(line); var top = text.Baseline - text.Text.Ascent; var right = left + line.Width;
                    Assert.True(left >= -1 && right <= size.Width + 1, $"{kind}/{variant}/{mode}: '{line.Text}' ({text.Role}) runs off the canvas.");
                    if (!InMarkRoles.Contains(text.Role ?? string.Empty)) continue;
                    var center = new ChartPoint((left + right) / 2, top + text.Text.Metrics.LineHeight / 2);
                    var host = marks.Where(box => center.X >= box.Left && center.X <= box.Right && center.Y >= box.Top && center.Y <= box.Bottom)
                        .OrderBy(box => box.Width * box.Height).Cast<ChartRect?>().FirstOrDefault();
                    if (host is not { } mark) continue;
                    // A label centred on a mark is inside it; one only touching it from outside is an outside label.
                    if (left < mark.Left - 1 && right > mark.Right + 1 || right - left > mark.Width + 1 && left >= mark.Left - 1)
                        Assert.Fail($"{kind}/{variant}/{mode}: '{line.Text}' ({text.Role}) is wider than its {mark.Width:0.#}px mark.");
                }
            }
        }
    }

    /// <summary>Texts outside rotated or translated groups, whose scene coordinates are their painted coordinates.</summary>
    private static IEnumerable<VisualSceneText> UnrotatedTexts(IReadOnlyList<VisualSceneNode> nodes) {
        var transformed = new Stack<bool>();
        foreach (var node in nodes) {
            if (node is VisualSceneGroup group) transformed.Push(group.Rotation != null || group.Translation != null || transformed.Count > 0 && transformed.Peek());
            else if (node is VisualSceneEndGroup) { if (transformed.Count > 0) transformed.Pop(); }
            else if (node is VisualSceneText text && (transformed.Count == 0 || !transformed.Peek())) yield return text;
        }
    }

    /// <summary>
    /// Rule 9: a sequential ramp covers a visible range in both modes. Its weakest step stands clearly off the surface,
    /// its steps keep moving, and a count heatmap (1–17) spreads across the whole ramp instead of collapsing to one tint.
    /// </summary>
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void SequentialRamp_CoversAVisibleRangeOnItsSurface(VisualThemeMode mode) {
        var colors = VisualTheme.Graphite().Resolve(mode);
        var ramp = colors.SequentialRamp;
        Assert.True(ramp.Count >= 4, "The sequential ramp needs at least four steps.");
        foreach (var surface in new[] { colors.Surface, colors.ElevatedSurface })
            Assert.True(ChartColorMath.ContrastRatio(ramp[0], surface) >= 1.5, mode + ": the weakest ramp step nearly vanishes into the surface.");
        Assert.True(ChartColorMath.ContrastRatio(ramp[0], ramp[ramp.Count - 1]) >= 3, mode + ": the ramp's ends are too close to tell apart.");
        var chart = Chart.Create().WithXLabels("A", "B", "C", "D", "E", "F")
            .AddHeatmapRow("R1", new[] { 1d, 4, 7, 10, 13, 17 }).AddHeatmapRow("R2", new[] { 2d, 5, 8, 11, 14, 16 });
        var cells = Prepare(chart, mode).Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "heatmap-cell-shape")
            .Select(node => node.Fill!.Value).ToArray();
        Assert.True(cells.Distinct().Count() >= 8, mode + ": count cells collapse to " + cells.Distinct().Count() + " fills.");
        var lightest = cells.OrderBy(cell => ChartColorMath.ContrastRatio(cell, colors.Surface)).First();
        var strongest = cells.OrderBy(cell => ChartColorMath.ContrastRatio(cell, colors.Surface)).Last();
        Assert.True(ChartColorMath.ContrastRatio(lightest, strongest) >= 2.5, mode + ": the heatmap uses only part of its ramp.");
    }

    private static readonly VisualThemeMode[] Modes = { VisualThemeMode.Light, VisualThemeMode.Dark };

    private static readonly HashSet<string> RowLabelRoles = new(StringComparer.Ordinal) {
        "schedule-row-label", "bullet-row-label", "progress-label", "heatmap-row-label", "axis-y-label", "axis-x-label"
    };

    private static readonly HashSet<string> MarkRoles = new(StringComparer.Ordinal) {
        "bar", "horizontal-bar", "waterfall-bar", "range-bar", "treemap-tile-mark", "heatmap-cell-shape", "calendar-cell-shape",
        "gantt-task", "timeline-item", "funnel-segment", "pyramid-segment"
    };

    private static readonly HashSet<string> InMarkRoles = new(StringComparer.Ordinal) { "data-label", "treemap-label", "heatmap-value", "gantt-label" };

    private static readonly Regex StatusVariable = new(@"--hfx-color-(severity|outcome|state|status)-[a-z-]+", RegexOptions.CultureInvariant);

    private static bool StatusEncoded(XElement element) {
        foreach (var name in new[] { "data-cfx-status", "data-cfx-state", "data-cfx-waterfall-kind", "data-cfx-semantic-role", "data-cfx-pattern",
            "data-cfx-direction", "data-cfx-rising" }) {
            var value = (string?)element.Attribute(name);
            if (!string.IsNullOrEmpty(value) && value is not ("none" or "None")) return true;
        }
        return (string?)element.Attribute("data-cfx-role") is "gauge-band" or "gantt-now" or "state-legend-swatch" or "bullet-row";
    }

    private static string Role(XElement element) => element.AncestorsAndSelf().Select(item => (string?)item.Attribute("data-cfx-role")).FirstOrDefault(role => role != null) ?? element.Name.LocalName;

    private static string Words(VisualSceneText text) => string.Join(" ", text.Text.Lines.Select(line => line.Text.Trim())).Trim();

    private static ChartAxis Clean(ChartAxis axis) { var copy = axis.Clone(); copy.Labels.Clear(); return copy; }

    private static double Overlap(double a1, double a2, double b1, double b2) => Math.Min(a2, b2) - Math.Max(a1, b1);

    private static IEnumerable<ChartPoint> Vertices(VisualScenePath path) => path.Commands
        .Select(command => new ChartPoint(command.X, command.Y));

    private static ChartRect Box(IReadOnlyList<ChartPoint> points) {
        if (points.Count == 0) return default;
        var left = points.Min(point => point.X); var top = points.Min(point => point.Y);
        return new ChartRect(left, top, points.Max(point => point.X) - left, points.Max(point => point.Y) - top);
    }

    private static PreparedVisual Prepare(Chart chart, VisualThemeMode mode) =>
        chart.WithThemeMode(mode).Prepare(new VisualRenderContext(theme: chart.Options.Theme, themeMode: mode));

    private static Chart? TryCreate(ChartSeriesKind kind, string variant, VisualThemeMode mode) {
        try { return V2GalleryModels.Create(kind, variant, mode); }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException or NotSupportedException or InvalidOperationException) { return null; }
    }
}
