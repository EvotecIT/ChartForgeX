using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Stage slopes reuse captured Cartesian axes, gaps and ordinary numeric source ownership.</summary>
public sealed class SlopeStagesTests {
    [Theory]
    [InlineData(false, false, false, 0)]
    [InlineData(false, true, true, 0)]
    [InlineData(true, false, false, 0)]
    [InlineData(true, true, true, 0)]
    [InlineData(false, false, true, 1)]
    [InlineData(true, true, true, 1)]
    [InlineData(false, true, true, -1)]
    [InlineData(true, false, true, -1)]
    public void OrderedStagesAndDisconnectedContoursUseCapturedPhysicalAxes(bool horizontal, bool reversed, bool secondary, int logDomain) {
        var values = logDomain == 0 ? new[] { -40d, 10, 30, -20 } : logDomain > 0 ? new[] { 10d, 20, 80, 40 } : new[] { -10d, -20, -80, -40 };
        var points = new[] { new ChartPoint(3, values[0]), new ChartPoint(1, values[1]), new ChartPoint(4, values[2], true), new ChartPoint(2, values[3]) };
        var chart = Bare().AddSlope("Stages", points); var series = chart.Series[0];
        series.WithObservationIds(new[] { "third", "first", "fourth", "second" }.Select(id => new ChartObservationId(id)));
        if (secondary) series.UseSecondaryValueAxis();
        chart.Options.SlopeOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        var category = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        category.WithBounds(0, 5).WithReversal(!reversed);
        var value = horizontal ? secondary ? chart.Options.SecondaryXAxis : chart.Options.XAxis
            : secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        if (logDomain == 0) value.WithBounds(-100, 100).WithReversal(reversed);
        else value.WithLogarithmicScale(domain: logDomain > 0 ? ChartLogarithmicDomain.Positive : ChartLogarithmicDomain.Negative)
            .WithBounds(logDomain > 0 ? 1 : -100, logDomain > 0 ? 100 : -1).WithReversal(reversed);
        var prepared = chart.Prepare(Context());
        Assert.Equal(horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical, prepared.CartesianOrientation);
        var valueProjection = horizontal ? secondary ? prepared.SecondaryXProjection! : prepared.XProjection!
            : secondary ? prepared.SecondaryYProjection! : prepared.YProjection!;
        var categoryProjection = horizontal ? prepared.YProjection! : prepared.XProjection!;
        var path = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "slope-line");
        Assert.Equal(new[] { ChartPathCommandKind.MoveTo, ChartPathCommandKind.LineTo, ChartPathCommandKind.MoveTo, ChartPathCommandKind.LineTo }, path.Commands.Select(command => command.Kind));
        var markers = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "slope-marker").ToArray();
        Assert.Equal(4, markers.Length);
        for (var index = 0; index < points.Length; index++) {
            var x = horizontal ? valueProjection.Project(points[index].Y) : categoryProjection.Project(points[index].X);
            var y = horizontal ? categoryProjection.Project(points[index].X) : valueProjection.Project(points[index].Y);
            Assert.Equal(x, path.Commands[index].X, 8); Assert.Equal(y, path.Commands[index].Y, 8);
            Assert.Equal(x, markers[index].Cx, 8); Assert.Equal(y, markers[index].Cy, 8);
        }
        var facts = Facts(prepared); Assert.Equal(4, facts.Length);
        Assert.Equal(new[] { "third", "first", "fourth", "second" }, facts.Select(node => (string?)node.Attribute("data-cfx-observation-id")));
        Assert.Equal(new[] { "0", "1", "2", "3" }, facts.Select(node => (string?)node.Attribute("data-cfx-source-point")));
        Assert.Equal(new[] { "3", "1", "4", "2" }, facts.Select(node => (string?)node.Attribute("data-cfx-x")));
        Assert.Equal(values.Select(number => number.ToString("G17", CultureInfo.InvariantCulture)), facts.Select(node => (string?)node.Attribute("data-cfx-value")));
        Assert.Equal(new[] { "false", "false", "true", "false" }, facts.Select(node => (string?)node.Attribute("data-cfx-break-before")));
        var svg = prepared.ToSvg("stages"); var png = prepared.ToPng();
        if (logDomain != 0) {
            series.Points[1] = new ChartPoint(1, 0);
            Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
        }
        chart.Options.SlopeOrientation = horizontal ? ChartOrientation.Vertical : ChartOrientation.Horizontal;
        value.Reversed = !value.Reversed; series.Points.Clear();
        Assert.Equal(svg, prepared.ToSvg("stages")); Assert.Equal(png, prepared.ToPng());
        Save(prepared, "ordered-" + horizontal + "-" + reversed + "-" + secondary + "-" + logDomain);
    }

    [Fact]
    public void CollectionFactoryCapturesOnceAndRejectsMalformedInputWithoutChangingChartState() {
        var chart = Bare().WithXLabels("Retained"); var labels = chart.Options.XAxisLabels.ToArray(); var enumerations = 0;
        IEnumerable<ChartPoint> Source() {
            if (++enumerations > 1) throw new InvalidOperationException("Do not enumerate the caller twice.");
            yield return new ChartPoint(3, 20); yield return new ChartPoint(1, 40); yield return new ChartPoint(4, 30, true);
        }
        chart.AddSlope("Captured", Source()); Assert.Equal(1, enumerations); Assert.Equal(labels, chart.Options.XAxisLabels);
        var series = Assert.Single(chart.Series); var svg = chart.Prepare(Context()).ToSvg("capture");
        Assert.Throws<ArgumentException>(() => chart.AddSlope("Single", new[] { new ChartPoint(1, 4) }));
        Assert.Throws<ArgumentNullException>(() => chart.AddSlope("Null", (IEnumerable<ChartPoint>)null!));
        Assert.Throws<ArgumentNullException>(() => chart.AddSlope(null!, new[] { new ChartPoint(1, 4), new ChartPoint(2, 8) }));
        Assert.Throws<InvalidOperationException>(() => chart.AddSlope("Interrupted", Interrupted()));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.AddSlope("Nonfinite", Nonfinite()));
        Assert.Same(series, Assert.Single(chart.Series)); Assert.Equal(labels, chart.Options.XAxisLabels);
        Assert.Equal(svg, chart.Prepare(Context()).ToSvg("capture"));
        var input = new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) };
        var captured = Bare().AddSlope("Array", input); input[0] = new ChartPoint(1, 999);
        Assert.Equal(2, captured.Series[0].Points[0].Y);
    }

    [Fact]
    public void EmptySlopeRetainsItsHandleAndRefillsWhileOneStageAndTrendLineArityStayExplicit() {
        var chart = Bare().AddSlope("Retained", Array.Empty<ChartPoint>()); var series = Assert.Single(chart.Series); var id = series.Id;
        var empty = chart.Prepare(Context()); Assert.Contains(empty.Diagnostics, item => item.Code == "cartesian.no-data");
        Assert.DoesNotContain(empty.Scene.Nodes, node => node.Role == "slope-line"); Assert.NotEmpty(empty.ToPng());
        series.Points.Add(new ChartPoint(1, 12)); Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
        series.Points.Add(new ChartPoint(2, 24)); series.Points.Add(new ChartPoint(4, 18));
        var refill = chart.Prepare(Context()); Assert.Equal(3, Facts(refill).Length); Assert.Equal(id, series.Id);
        Assert.Equal(3, refill.Scene.Nodes.Count(node => node.Role == "slope-marker"));
        series.Points.Clear(); Assert.Contains(chart.Prepare(Context()).Diagnostics, item => item.Code == "cartesian.no-data");
        var malformed = Bare().AddSeries(new ChartSeries(new ChartSeriesId("slope"), "Slope", ChartSeriesKind.Slope, new[] { new ChartPoint(1, 2) }));
        Assert.Throws<InvalidOperationException>(() => malformed.Prepare(Context()));
        var trend = Bare().AddSeries(new ChartSeries(new ChartSeriesId("trend"), "Trend", ChartSeriesKind.TrendLine,
            new[] { new ChartPoint(1, 2), new ChartPoint(2, 3), new ChartPoint(3, 4) }));
        Assert.Contains("exactly two", Assert.Throws<InvalidOperationException>(() => trend.Prepare(Context())).Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhysicalCategoryAndSecondaryWindowsRetainEveryStageAndDetachedExports(bool horizontal) {
        var chart = Bare().WithDataLabels().AddSlope("Stages", new[] { new ChartPoint(1, 20), new ChartPoint(2, 45), new ChartPoint(3, 35, true), new ChartPoint(4, 70) });
        var series = chart.Series[0].UseSecondaryValueAxis().WithPointLabel(1, "Middle recorded stage");
        chart.Options.SlopeOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        if (horizontal) chart.WithXAxisBounds(0, 100).WithYAxisBounds(0, 5).WithSecondaryXAxisBounds(0, 100);
        else chart.WithXAxisBounds(0, 5).WithYAxisBounds(0, 100).WithSecondaryYAxisBounds(0, 100);
        var full = chart.Prepare(Context());
        var viewport = horizontal ? new ChartCartesianViewport(y: new ChartAxisWindow(1.5, 3.5), secondaryX: new ChartAxisWindow(25, 60))
            : new ChartCartesianViewport(x: new ChartAxisWindow(1.5, 3.5), secondaryY: new ChartAxisWindow(25, 60));
        var view = chart.Prepare(Context(), viewport); var facts = Facts(view);
        Assert.Equal(4, facts.Length); Assert.Equal(new[] { "false", null, null, "false" }, facts.Select(node => (string?)node.Attribute("data-cfx-visible")));
        Assert.Equal(new[] { "1", "2", "3", "4" }, facts.Select(node => (string?)node.Attribute("data-cfx-x")));
        Assert.Equal(2, view.Scene.Nodes.Count(node => node.Role == "slope-marker"));
        Assert.Contains(view.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label" && node.Text.Lines.Any(line => line.Text == "Middle recorded stage"));
        var category = horizontal ? view.YProjection! : view.XProjection!;
        var value = horizontal ? view.SecondaryXProjection! : view.SecondaryYProjection!;
        Assert.Equal(1.5, category.Minimum); Assert.Equal(3.5, category.Maximum); Assert.Equal(25, value.Minimum); Assert.Equal(60, value.Maximum);
        var saved = view.ToSvg("window"); Assert.NotEmpty(view.ToPng()); series.Points[1] = new ChartPoint(2, 50);
        Assert.Equal(saved, view.ToSvg("window")); Assert.Equal(4, series.SourcePointCount);
        Assert.Equal(full.ToSvg("reset"), BareWindowClone(horizontal).Prepare(Context()).ToSvg("reset"));
        Save(view, "window-" + horizontal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VisibleCategoryFitDoesNotInventAConnectionAcrossAnExplicitStageGap(bool horizontal) {
        var chart = Bare().AddSlope("Gaps", new[] { new ChartPoint(0, 10), new ChartPoint(1, 20), new ChartPoint(9, 80, true), new ChartPoint(10, 90) });
        chart.Options.SlopeOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        var full = chart.Prepare(Context());
        var viewport = horizontal ? new ChartCartesianViewport(y: new ChartAxisWindow(4, 6), valueDomain: ChartValueDomain.VisibleCategories)
            : new ChartCartesianViewport(x: new ChartAxisWindow(4, 6), valueDomain: ChartValueDomain.VisibleCategories);
        var gap = chart.Prepare(Context(), viewport);
        var fullValue = horizontal ? full.XProjection! : full.YProjection!;
        var gapValue = horizontal ? gap.XProjection! : gap.YProjection!;
        Assert.Equal(fullValue.Minimum, gapValue.Minimum); Assert.Equal(fullValue.Maximum, gapValue.Maximum);
        Assert.Equal(4, Facts(gap).Length); Assert.All(Facts(gap), node => Assert.Equal("false", (string?)node.Attribute("data-cfx-visible")));
        Assert.DoesNotContain(gap.Scene.Nodes, node => node.Role == "slope-marker"); Assert.NotEmpty(gap.ToPng());
        chart.Series[0].Points[2] = new ChartPoint(9, 80);
        var joined = chart.Prepare(Context(), viewport); var joinedValue = horizontal ? joined.XProjection! : joined.YProjection!;
        Assert.True(joinedValue.Minimum > fullValue.Minimum); Assert.True(joinedValue.Maximum < fullValue.Maximum);
        Assert.Equal(4, Facts(joined).Length); Assert.Equal(4, chart.Series[0].SourcePointCount);
    }

    [Fact]
    public void HorizontalDatedStagesReuseTheCapturedTimeCategoryProjection() {
        var origin = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var source = new[] { new ChartPoint(origin, 10), new ChartPoint(origin.AddDays(3), 40), new ChartPoint(origin.AddDays(8), 25) };
        var chart = Bare().AddSlope("Dated stages", source); chart.Options.SlopeOrientation = ChartOrientation.Horizontal;
        chart.Options.YAxis.WithScale(ChartScaleKind.Time).WithBounds(source[0].X - 1, source[2].X + 1).WithReversal();
        chart.Options.XAxis.WithBounds(0, 50);
        var prepared = chart.Prepare(Context()); Assert.Equal(ChartScaleKind.Time, prepared.YProjection!.Scale);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "slope-marker").ToArray();
        Assert.Equal(3, marks.Length);
        for (var index = 0; index < source.Length; index++) {
            Assert.Equal(prepared.YProjection.Project(source[index].X), marks[index].Cy, 8);
            Assert.Equal(prepared.XProjection!.Project(source[index].Y), marks[index].Cx, 8);
        }
        Assert.Equal(source.Select(point => point.X.ToString("G17", CultureInfo.InvariantCulture)), Facts(prepared).Select(node => (string?)node.Attribute("data-cfx-x")));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void ExistingTwoValueFactoriesKeepTheirEndpointLabelsAndDefaultGeometry() {
        var old = Bare().WithDataLabels().AddSlope("Legacy", 28, 64);
        var captured = Bare().WithDataLabels().WithXLabels("Start", "End").AddSlope("Legacy", new[] { new ChartPoint(1, 28), new ChartPoint(2, 64) });
        Assert.Equal(ChartOrientation.Vertical, old.Options.SlopeOrientation);
        Assert.Equal(old.Prepare(Context()).ToSvg("legacy"), captured.Prepare(Context()).ToSvg("legacy"));
        var named = Bare().WithDataLabels().AddSlope("Named", 28, 64, "Before", "After");
        Assert.Equal(new[] { "Before", "After" }, named.Options.XAxisLabels.Select(label => label.Text));
        Assert.Equal(2, named.Prepare(Context()).Scene.Nodes.Count(node => node.Role == "data-label"));
        Assert.Throws<ArgumentOutOfRangeException>(() => old.Options.SlopeOrientation = (ChartOrientation)42);
        old.Options.SlopeOrientation = ChartOrientation.Horizontal; old.AddLine("Vertical", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) });
        Assert.Throws<NotSupportedException>(() => old.Prepare(Context()));
    }

    private static Chart BareWindowClone(bool horizontal) {
        var chart = Bare().WithDataLabels().AddSlope("Stages", new[] { new ChartPoint(1, 20), new ChartPoint(2, 45), new ChartPoint(3, 35, true), new ChartPoint(4, 70) });
        chart.Series[0].UseSecondaryValueAxis().WithPointLabel(1, "Middle recorded stage");
        chart.Options.SlopeOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        return horizontal ? chart.WithXAxisBounds(0, 100).WithYAxisBounds(0, 5).WithSecondaryXAxisBounds(0, 100)
            : chart.WithXAxisBounds(0, 5).WithYAxisBounds(0, 100).WithSecondaryYAxisBounds(0, 100);
    }
    private static IEnumerable<ChartPoint> Interrupted() { yield return new ChartPoint(1, 2); yield return new ChartPoint(2, 3); throw new InvalidOperationException("Interrupted source."); }
    private static IEnumerable<ChartPoint> Nonfinite() { yield return new ChartPoint(1, 2); yield return new ChartPoint(2, double.NaN); }
    private static Chart Bare() => Chart.Create().WithSize(800, 400).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false);
    internal static VisualRenderContext Context(int width = 800, VisualThemeMode mode = VisualThemeMode.Light) => new(new VisualLayoutOptions(new VisualSize(width, 400)),
        VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
    private static XElement[] Facts(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
    internal static void Save(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Slope captures require an absolute task path.");
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "slope-" + name + ".svg"), prepared.ToSvg("slope-capture"));
        File.WriteAllBytes(Path.Combine(directory, "slope-" + name + ".png"), prepared.ToPng());
    }
}
