using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ComparisonOrientationTests {
    public static IEnumerable<object[]> PhysicalCases() {
        foreach (var dumbbell in new[] { false, true }) foreach (var horizontal in new[] { false, true })
            foreach (var reversed in new[] { false, true }) foreach (var secondary in new[] { false, true })
                yield return new object[] { dumbbell, horizontal, reversed, secondary };
    }

    [Theory]
    [MemberData(nameof(PhysicalCases))]
    public void AuthoredComparisonsMapThroughTheirPhysicalValueAxisWithoutSortingOrChangingIdentity(bool dumbbell, bool horizontal, bool reversed, bool secondary) {
        var chart = Bare();
        if (dumbbell) chart.AddDumbbell("Comparison", new[] { new ChartDumbbell(1, -10, 20), new ChartDumbbell(3, 25, -15) });
        else chart.AddLollipop("Comparison", new[] { new ChartPoint(1, 20), new ChartPoint(3, -15) });
        chart.Options.LollipopOrientation = chart.Options.DumbbellOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
        var series = chart.Series[0]; series.WithObservationIds(new[] { new ChartObservationId("north"), new ChartObservationId("south") });
        if (secondary) series.UseSecondaryValueAxis();
        var category = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var primary = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        var alternate = horizontal ? chart.Options.SecondaryXAxis : chart.Options.SecondaryYAxis;
        category.WithBounds(0, 4).WithReversal(!reversed); primary.WithBounds(-40, 40).WithReversal(reversed);
        alternate.WithBounds(-80, 80).WithReversal(reversed);
        var scene = Scene(chart); var lines = scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == (dumbbell ? "dumbbell-connector" : "lollipop-stem")).ToArray();
        var ends = scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == (dumbbell ? "dumbbell-end" : "lollipop-marker")).ToArray();
        Assert.Equal(2, lines.Length); Assert.Equal(2, ends.Length);
        var values = new[] { 20d, -15 }; var starts = dumbbell ? new[] { -10d, 25 } : new[] { 0d, 0 }; var categories = new[] { 1d, 3 };
        var limit = secondary ? 80d : 40;
        double Value(double value) => horizontal ? (reversed ? limit - value : value + limit) / (2 * limit) * 800
            : (reversed ? value + limit : limit - value) / (2 * limit) * 400;
        double Category(double value) => horizontal ? (reversed ? 4 - value : value) * 100 : (reversed ? value : 4 - value) * 200;
        for (var index = 0; index < lines.Length; index++) {
            Assert.Equal(Value(starts[index]), horizontal ? lines[index].Start.X : lines[index].Start.Y, 8);
            Assert.Equal(Value(values[index]), horizontal ? lines[index].End.X : lines[index].End.Y, 8);
            Assert.Equal(Category(categories[index]), horizontal ? lines[index].Start.Y : lines[index].Start.X, 8);
            Assert.Equal(lines[index].End.X, ends[index].Cx, 8); Assert.Equal(lines[index].End.Y, ends[index].Cy, 8);
        }
        var facts = chart.Prepare(Context()).Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point").ToArray();
        Assert.Equal(new[] { "north", "south" }, facts.Select(node => node.Metadata["data-cfx-observation-id"]));
        Assert.Equal(new[] { "1", "3" }, facts.Select(node => node.Metadata["data-cfx-x"]));
        Assert.All(facts, node => Assert.Equal(horizontal ? "horizontal" : "vertical", node.Metadata["data-cfx-orientation"]));
        if (dumbbell) { Assert.Equal(new[] { "-10", "25" }, facts.Select(node => node.Metadata["data-cfx-start"])); Assert.Equal(new[] { "20", "-15" }, facts.Select(node => node.Metadata["data-cfx-end"])); }
    }

    [Fact]
    public void EndpointOverridesInheritSharedMarkersAndCaptureCallbackAndPreparedState() {
        var chart = Bare().AddDumbbell("Comparison", new[] { new ChartDumbbell(1, 10, 30) });
        var series = chart.Series[0]; var red = ChartColor.FromHex("#D02030"); var blue = ChartColor.FromHex("#2040C0");
        series.ConfigureMarkers(marker => { marker.Shape = ChartMarkerShape.Triangle; marker.Radius = 9; marker.StrokeWidth = 2; });
        ChartDumbbellOptions? captured = null;
        series.ConfigureDumbbell(options => { captured = options; options.StartMarker.Fill = red; options.EndMarker.Shape = ChartMarkerShape.Circle; options.EndMarker.Fill = blue; });
        captured!.StartMarker.Radius = 50;
        var scene = Scene(chart); var start = Assert.Single(scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "dumbbell-start");
        var end = Assert.Single(scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "dumbbell-end");
        Assert.Equal(red, start.Fill); Assert.Equal(blue, end.Fill); Assert.Equal(9, end.Rx); Assert.Equal(2, start.StrokeWidth);
        Assert.Throws<InvalidOperationException>(() => series.ConfigureDumbbell(options => { options.StartMarker.Radius = 30; throw new InvalidOperationException("Do not install."); }));
        Assert.Null(series.Dumbbell.StartMarker.Radius);
        chart.Options.DumbbellOrientation = ChartOrientation.Horizontal;
        var prepared = chart.Prepare(Context()); var svg = prepared.ToSvg("captured-comparison"); var png = prepared.ToPng();
        series.Dumbbell.EndMarker.Radius = 100; series.Markers.Shape = ChartMarkerShape.Square;
        chart.Options.DumbbellOrientation = ChartOrientation.Vertical; chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg("captured-comparison")); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 1200)]
    public void UnequalEndpointGeometryPaintAndVisibilityRemainIndependentAndContained(bool dark, int width) {
        var chart = Bare().WithLegend().AddDumbbell("Comparison", new[] { new ChartDumbbell(1, 10, 30), new ChartDumbbell(2, 25, 15) });
        chart.Options.DumbbellOrientation = ChartOrientation.Horizontal;
        var series = chart.Series[0]; var red = ChartColor.FromHex("#D02030"); var blue = ChartColor.FromHex("#2040C0");
        series.ConfigureDumbbell(options => {
            options.StartMarker.Shape = ChartMarkerShape.Circle; options.StartMarker.Radius = 4; options.StartMarker.Fill = red;
            options.EndMarker.Shape = ChartMarkerShape.Square; options.EndMarker.Radius = 17; options.EndMarker.Fill = blue;
            options.EndMarker.Stroke = ChartColor.Black; options.EndMarker.StrokeWidth = 4;
        });
        var prepared = chart.Prepare(Context(width, dark)); var starts = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dumbbell-start").ToArray();
        var ends = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "dumbbell-end").ToArray();
        Assert.Equal(2, starts.Length); Assert.Equal(2, ends.Length);
        Assert.All(starts, marker => { Assert.Equal(4, marker.Rx); Assert.Equal(red, marker.Fill); });
        Assert.All(ends, marker => {
            Assert.Equal(blue, marker.Fill); Assert.Equal(4, marker.StrokeWidth);
            Assert.All(marker.Commands, command => { Assert.InRange(command.X - 2, prepared.XProjection!.PlotBounds.Left - 1e-6, prepared.XProjection.PlotBounds.Right); Assert.InRange(command.X + 2, prepared.XProjection.PlotBounds.Left, prepared.XProjection.PlotBounds.Right + 1e-6); });
        });
        var legendStart = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "legend-dumbbell-start");
        var legendEnd = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "legend-dumbbell-end");
        Assert.Equal(red, legendStart.Fill); Assert.Equal(blue, legendEnd.Fill);
        series.Dumbbell.StartMarker.Enabled = false;
        var hidden = chart.Prepare(Context(width, dark)); Assert.DoesNotContain(hidden.Scene.Nodes, node => node.Role == "dumbbell-start");
        Assert.Equal(2, hidden.Scene.Nodes.Count(node => node.Role == "dumbbell-connector")); Assert.Equal(2, hidden.Regions.Count(node => node.Role == "point"));
        series.Markers.Enabled = false; series.Dumbbell.StartMarker.Enabled = true;
        var onlyStart = chart.Prepare(Context(width, dark)); Assert.Equal(2, onlyStart.Scene.Nodes.Count(node => node.Role == "dumbbell-start"));
        Assert.DoesNotContain(onlyStart.Scene.Nodes, node => node.Role == "dumbbell-end");
        Save("endpoint-style-" + width + "-" + (dark ? "dark" : "light"), prepared);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void HorizontalLollipopCaptionsStayBeyondTheirOwnMappedValueEndpoints(bool reversed, bool secondary) {
        var chart = Bare().WithDataLabels().AddLollipop("Changes", new[] { new ChartPoint(1, 40), new ChartPoint(2, -60) });
        chart.Options.LollipopOrientation = ChartOrientation.Horizontal;
        if (secondary) chart.Series[0].UseSecondaryValueAxis();
        chart.Options.XAxis.WithBounds(-100, 100).WithReversal(reversed);
        chart.Options.SecondaryXAxis.WithBounds(-100, 100).WithReversal(reversed);
        var prepared = chart.Prepare(Context());
        var marks = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "lollipop-marker").ToArray();
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert.Equal(2, marks.Length); Assert.Equal(2, captions.Length);
        for (var index = 0; index < marks.Length; index++) {
            var bounds = CartesianReversalLabelTests.Bounds(captions[index]); var right = index == 0 != reversed;
            Assert.True(right ? bounds.Left > marks[index].Cx + marks[index].Rx : bounds.Right < marks[index].Cx - marks[index].Rx,
                "Automatic horizontal captions must follow the value endpoint and must not move to the remote baseline.");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhysicalSecondaryWindowsClipWithoutLosingTuplesAndResetDetachedFullDomains(bool dumbbell) {
        var chart = Bare();
        if (dumbbell) chart.AddDumbbell("Comparison", new[] { new ChartDumbbell(1, 10, 30), new ChartDumbbell(2, 40, 70), new ChartDumbbell(3, 80, 90) });
        else chart.AddLollipop("Comparison", new[] { new ChartPoint(1, 20), new ChartPoint(2, 50), new ChartPoint(3, 85) });
        chart.Options.LollipopOrientation = chart.Options.DumbbellOrientation = ChartOrientation.Horizontal; chart.Series[0].UseSecondaryValueAxis();
        var full = chart.Prepare(Context()); var window = new ChartCartesianViewport(y: new ChartAxisWindow(1.5, 2.5), secondaryX: new ChartAxisWindow(35, 75));
        var view = chart.Prepare(Context(), window); Assert.Equal(ChartOrientation.Horizontal, view.CartesianOrientation);
        Assert.NotNull(view.SecondaryXProjection); Assert.Null(view.SecondaryYProjection);
        Assert.Equal(35, view.SecondaryXProjection!.Minimum); Assert.Equal(75, view.SecondaryXProjection.Maximum);
        Assert.Equal(full.SecondaryXProjection!.FullMinimum, view.SecondaryXProjection.FullMinimum);
        var facts = view.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point").ToArray();
        Assert.Equal(new[] { "false", "true", "false" }, facts.Select(node => node.Metadata.TryGetValue("data-cfx-visible", out var flag) ? flag : "true"));
        Assert.Equal(3, chart.Series[0].ObservationCount);
        var fitted = chart.Prepare(Context(), new ChartCartesianViewport(y: window.Y, valueDomain: ChartValueDomain.VisibleCategories));
        Assert.True(fitted.SecondaryXProjection!.Maximum < 75);
        if (dumbbell) Assert.True(fitted.SecondaryXProjection.Minimum > 35);
        else Assert.True(fitted.SecondaryXProjection.Minimum <= 0);
        Assert.Equal(full.ToSvg("full-comparison"), chart.Prepare(Context()).ToSvg("full-comparison"));
        Assert.Throws<NotSupportedException>(() => chart.Prepare(Context(), new ChartCartesianViewport(secondaryY: new ChartAxisWindow(35, 75))));
        Assert.NotEmpty(view.ToPng());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void HorizontalNegativeLogComparisonsKeepDomainSignAndMappedEndpointOrder(bool dumbbell, bool reversed) {
        var chart = Bare();
        if (dumbbell) chart.AddDumbbell("Comparison", new[] { new ChartDumbbell(1, -100, -10), new ChartDumbbell(2, -5, -50) });
        else chart.AddLollipop("Comparison", new[] { new ChartPoint(1, -100), new ChartPoint(2, -10) });
        chart.Options.LollipopOrientation = chart.Options.DumbbellOrientation = ChartOrientation.Horizontal;
        chart.Options.XAxis.WithLogarithmicScale(domain: ChartLogarithmicDomain.Negative).WithReversal(reversed);
        var prepared = chart.Prepare(Context()); Assert.True(prepared.XProjection!.FullMaximum < 0); Assert.True(prepared.XProjection.FullMinimum < 0);
        var lines = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == (dumbbell ? "dumbbell-connector" : "lollipop-stem")).ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Equal(prepared.XProjection.Project(-100), dumbbell ? lines[0].Start.X : lines[0].End.X, 8);
        if (dumbbell) Assert.Equal(prepared.XProjection.Project(-10), lines[0].End.X, 8);
        Assert.NotEmpty(prepared.ToPng());
        if (dumbbell) chart.Series[0].UpdateDumbbell(0, new ChartDumbbell(1, 0, -10));
        else chart.Series[0].Points[0] = new ChartPoint(1, 0);
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
    }

    [Fact]
    public void UnsupportedComparisonDirectionsAndMarkerDimensionsFailAtTheirOwningBoundary() {
        var chart = Bare().AddDumbbell("Comparison", new[] { new ChartDumbbell(1, 1, 2) });
        Assert.Equal(ChartOrientation.Vertical, chart.Options.DumbbellOrientation); Assert.Equal(ChartOrientation.Vertical, chart.Options.LollipopOrientation);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.DumbbellOrientation = (ChartOrientation)42);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.LollipopOrientation = (ChartOrientation)42);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[0].Dumbbell.StartMarker.Radius = -1);
        Assert.Throws<ArgumentNullException>(() => chart.Series[0].ConfigureDumbbell(null!));
        Assert.Throws<InvalidOperationException>(() => Chart.Create().AddLollipop("Single", new[] { new ChartPoint(1, 1) }).Series[0].ConfigureDumbbell(_ => { }));
        chart.Options.DumbbellOrientation = ChartOrientation.Horizontal; chart.AddLine("Vertical", new[] { new ChartPoint(1, 2) });
        Assert.Throws<NotSupportedException>(() => chart.Prepare(Context()));
        chart.RemoveSeries(chart.Series[1]); chart.Options.YAxis.WithScale(ChartScaleKind.Logarithmic);
        Assert.Throws<NotSupportedException>(() => chart.Prepare(Context()));
    }

    private static Chart Bare() => Chart.Create().WithSize(800, 400).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false);
    private static VisualRenderContext Context(int width = 800, bool dark = false) => new(layout: new VisualLayoutOptions(new VisualSize(width, 400)),
        theme: VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), themeMode: dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
    private static VisualScene Scene(Chart chart) {
        var context = Context(); var builder = new VisualSceneBuilder(new VisualSize(800, 400), context.Font);
        VisualCartesianCompiler.Build(chart, context, builder, new ChartRect(0, 0, 800, 400)); return builder.Build();
    }
    internal static void Save(string name, PreparedVisual prepared) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "comparison-" + name + ".svg"), prepared.ToSvg("comparison-capture"));
        File.WriteAllBytes(Path.Combine(directory, "comparison-" + name + ".png"), prepared.ToPng());
    }
}
