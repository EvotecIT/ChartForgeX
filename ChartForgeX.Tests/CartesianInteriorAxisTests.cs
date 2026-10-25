using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Complete interior anatomy follows one physical track while preserving native data projection and detached source facts.</summary>
public sealed class CartesianInteriorAxisTests {
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void PrimaryRulesCaptionsAndTitlesMoveTogetherWithoutChangingMarksOrTheMeasuredPlot(bool horizontal, bool reversed) {
        var chart = CartesianAxisRuleCrossingTests.Fixture(horizontal).WithXAxis("Domain").WithYAxis("Reading");
        chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        var before = Prepare(chart); var beforeSvg = before.ToSvg(); var beforePng = before.ToPng();
        chart.Options.XAxis.CrossAt = 4; chart.Options.YAxis.CrossAt = 3;
        var after = Prepare(chart); var x = Rule(after, "axis-x"); var y = Rule(after, "axis-y");
        Assert.Equal(before.XProjection!.PlotBounds, after.XProjection!.PlotBounds);
        Assert.Equal(before.Regions.Where(region => region.Role == "point").Select(region => region.Bounds), after.Regions.Where(region => region.Role == "point").Select(region => region.Bounds));
        Assert.Equal(after.YProjection!.Project(4), x.Start.Y, 7); Assert.Equal(after.XProjection.Project(3), y.Start.X, 7);
        var xLabels = Ink(after, "axis-x-label"); var yLabels = Ink(after, "axis-y-label");
        Assert.NotEmpty(xLabels); Assert.NotEmpty(yLabels);
        Assert.All(xLabels, text => Assert.True(text.Baseline - text.Text.Ascent >= x.Start.Y));
        Assert.All(yLabels, text => Assert.True(text.X + text.Text.Metrics.Width <= y.Start.X));
        var xTitle = Assert.Single(Ink(after, "axis-x-title")); var yTitle = Assert.Single(Ink(after, "axis-y-title"));
        Assert.True(xTitle.Baseline - xTitle.Text.Ascent > x.Start.Y); Assert.True(yTitle.X >= y.Start.X);
        Assert.Equal(before.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label), after.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label));
        var svg = after.ToSvg(); var png = after.ToPng(); chart.Options.XAxis.CrossAt = null; chart.Options.YAxis.CrossAt = null;
        Assert.Equal(beforeSvg, Prepare(chart).ToSvg()); Assert.Equal(beforePng, Prepare(chart).ToPng());
        Assert.Equal(svg, after.ToSvg()); Assert.Equal(png, after.ToPng());
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void SecondaryCompleteAxesUseTheirOwnTicksButTheOppositePrimaryCrossingCoordinate(bool horizontal, bool reversed) {
        var chart = CartesianAxisRuleCrossingTests.Fixture(horizontal);
        var points = new[] { new ChartPoint(2, 100), new ChartPoint(8, 900) };
        if (horizontal) chart.AddHorizontalBar("Auxiliary", points).WithSecondaryXAxisBounds(0, 1000).WithSecondaryXAxis("Auxiliary scale");
        else chart.AddLine("Auxiliary", points).WithSecondaryYAxisBounds(0, 1000).WithSecondaryYAxis("Auxiliary scale");
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        var before = Prepare(chart); var axis = horizontal ? chart.Options.SecondaryXAxis : chart.Options.SecondaryYAxis;
        axis.CrossAt = horizontal ? 4 : 3; var prepared = Prepare(chart); var role = horizontal ? "axis-secondary-x" : "axis-secondary-y";
        var rule = Rule(prepared, role); Assert.NotEmpty(Ink(prepared, role + "-label"));
        Assert.Equal(horizontal ? prepared.YProjection!.Project(4) : prepared.XProjection!.Project(3), horizontal ? rule.Start.Y : rule.Start.X, 7);
        Assert.Equal(before.Regions.Where(region => region.Role == role + "-label").Select(region => region.Label), prepared.Regions.Where(region => region.Role == role + "-label").Select(region => region.Label));
        Assert.Contains(prepared.Regions, region => region.Role == role + "-title" && region.Label == "Auxiliary scale");
        if (horizontal) Assert.All(Ink(prepared, role + "-label"), text => Assert.True(text.Baseline <= rule.Start.Y));
        else Assert.All(Ink(prepared, role + "-label"), text => Assert.True(text.X >= rule.Start.X));
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic, false)] [InlineData(ChartScaleKind.Logarithmic, true)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, false)] [InlineData(ChartScaleKind.SymmetricLogarithmic, true)]
    public void CompleteAxesReuseNonlinearOppositeProjectionsAndRetainRealTickValues(ChartScaleKind scale, bool reversed) {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithXAxisBounds(scale == ChartScaleKind.Logarithmic ? 1 : -100, 100)
            .WithYAxisBounds(scale == ChartScaleKind.Logarithmic ? 1 : -100, 100);
        chart.Options.XAxis.Scale = scale; chart.Options.YAxis.Scale = scale;
        chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        var before = Prepare(chart); chart.Options.XAxis.CrossAt = 10; chart.Options.YAxis.CrossAt = 10;
        var after = Prepare(chart);
        Assert.Equal(after.XProjection!.Project(10), Rule(after, "axis-y").Start.X, 7);
        Assert.Equal(after.YProjection!.Project(10), Rule(after, "axis-x").Start.Y, 7);
        Assert.Equal(before.Regions.Where(region => region.Role == "axis-x-label").Select(region => region.Label), after.Regions.Where(region => region.Role == "axis-x-label").Select(region => region.Label));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NegativeLogarithmicCompleteAxesUseTheSameCapturedSignAndReversal(bool reversed) {
        var chart = Chart.Create().WithSize(600, 400).WithHeader(false).WithLegend(false).WithGrid(false).WithDataLabels(false)
            .AddScatter("Negative", new[] { new ChartPoint(-80, -80), new ChartPoint(-2, -2) });
        chart.Options.XAxis.WithLogarithmicScale(10, ChartLogarithmicDomain.Negative).WithBounds(-100, -1).WithReversal(reversed);
        chart.Options.YAxis.WithLogarithmicScale(10, ChartLogarithmicDomain.Negative).WithBounds(-100, -1).WithReversal(reversed);
        chart.Options.XAxis.CrossAt = -10; chart.Options.YAxis.CrossAt = -10;
        var prepared = Prepare(chart);
        Assert.Equal(prepared.XProjection!.Project(-10), Rule(prepared, "axis-y").Start.X, 7);
        Assert.Equal(prepared.YProjection!.Project(-10), Rule(prepared, "axis-x").Start.Y, 7);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label!.Contains("-10", StringComparison.Ordinal));
    }

    [Fact]
    public void CompleteAxisCrossingSharesIncludedTimeProjectionAndRejectsClosedAliases() {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate();
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithXAxisBounds(start, start + 4);
        chart.ClearSeries(); chart.AddLine("Included time", new[] { new ChartPoint(start, 2), new ChartPoint(start + 4, 8) });
        chart.Options.XAxis.WithTimeSessions(new ChartTimeSessionDomain(new[] { new ChartTimeSession(start, start + 1), new ChartTimeSession(start + 3, start + 4) }));
        chart.Options.YAxis.CrossAt = start + 1;
        var prepared = Prepare(chart); Assert.Equal(prepared.XProjection!.Project(start + 1), Rule(prepared, "axis-y").Start.X, 7);
        chart.Options.YAxis.CrossAt = start + 2; Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Fact]
    public void OutsideAndExclusiveMaximumWindowsOmitTheCompleteAnatomyButKeepRawFactsAndReset() {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false).WithYAxis("Complete reading"); chart.Options.YAxis.CrossAt = 5;
        var context = VisualExportRequest.ForChart(chart).Context; var full = chart.Prepare(context); var svg = full.ToSvg(); var png = full.ToPng();
        foreach (var window in new[] { new ChartAxisWindow(6, 9), new ChartAxisWindow(1, 5, maximumInclusive: false) }) {
            var hidden = chart.Prepare(context, new ChartCartesianViewport(x: window));
            Assert.DoesNotContain(hidden.Scene.Nodes, node => node.Role is "axis-y" or "axis-y-label" or "axis-y-title");
            var metadata = Assert.Single(hidden.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "axis-crossing");
            Assert.Equal("5", metadata.Metadata["data-cfx-cross-at"]); Assert.Equal("false", metadata.Metadata["data-cfx-visible"]);
            Assert.Contains(hidden.Regions, region => region.Role == "axis-y-label");
            Assert.Contains(hidden.Regions, region => region.Role == "axis-y-title" && region.Label == "Complete reading");
            Assert.Contains(hidden.Diagnostics, item => item.Code == "cartesian.axis-crossing-outside-domain");
        }
        Assert.Equal(svg, chart.Prepare(context).ToSvg()); Assert.Equal(png, chart.Prepare(context).ToPng()); Assert.Equal(5, chart.Options.YAxis.CrossAt);
    }

    [Fact]
    public void ActualMarksCanOmitConflictingTickInkWhileCompleteCaptionsRemainSemanticFacts() {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false); chart.ClearSeries(); chart.AddScatter("Blocking mark", new[] { new ChartPoint(5, 5) });
        chart.Series[0].WithMarkerRadius(50); chart.Options.YAxis.Labels.Add(new ChartAxisLabel(5, "CENTER")); chart.Options.YAxis.CrossAt = 5;
        var prepared = Prepare(chart);
        Assert.DoesNotContain(Ink(prepared, "axis-y-label"), text => string.Join("", text.Text.Lines.Select(line => line.Text)) == "CENTER");
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label!.Contains("CENTER", StringComparison.Ordinal));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.axis-label-overflow");
        Assert.Single(prepared.Regions, region => region.Role == "point");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CategoryParentFactsAndPaintMoveThroughTheExistingBandOwner(bool horizontal) {
        var chart = CartesianAxisRuleCrossingTests.Fixture(horizontal);
        var category = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        category.Labels.Add(new ChartAxisLabel(2, "First", "Group")); category.Labels.Add(new ChartAxisLabel(8, "Second", "Group"));
        var before = Prepare(chart); category.CrossAt = 5; var after = Prepare(chart);
        var role = horizontal ? "axis-y-category-group" : "axis-x-category-group";
        Assert.Contains(after.Regions, region => region.Role == role);
        Assert.Equal(before.Regions.Where(region => region.Role == role).Select(region => region.Label), after.Regions.Where(region => region.Role == role).Select(region => region.Label));
        Assert.NotEqual(before.Regions.First(region => region.Role == role).Bounds, after.Regions.First(region => region.Role == role).Bounds);
    }

    [Fact]
    public void RuleVisibilityIsIndependentAndTheTwoCrossingPoliciesAreAtomicAndMutuallyExclusive() {
        var chart = CartesianAxisRuleCrossingTests.Fixture(false); var axis = chart.Options.XAxis;
        axis.WithCrossAt(4); axis.ShowLine = false; var prepared = Prepare(chart);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "axis-x"); Assert.NotEmpty(Ink(prepared, "axis-x-label"));
        Assert.Throws<InvalidOperationException>(() => axis.RuleCrossAt = 3); Assert.Equal(4, axis.CrossAt); Assert.Null(axis.RuleCrossAt);
        axis.WithCrossAt(null).WithRuleCrossAt(3);
        Assert.Throws<InvalidOperationException>(() => axis.CrossAt = 4); Assert.Equal(3, axis.RuleCrossAt); Assert.Null(axis.CrossAt);
        Assert.Throws<ArgumentOutOfRangeException>(() => axis.CrossAt = double.NaN);
        axis.RuleCrossAt = null; axis.CrossAt = 4; axis.Visible = false;
        Assert.DoesNotContain(Prepare(chart).Scene.Nodes, node => node.Role is "axis-x" or "axis-x-label");
    }

    [Theory]
    [InlineData(360, false)] [InlineData(360, true)] [InlineData(800, false)] [InlineData(800, true)]
    public void CompleteRecipeCapturesBothNativeFormatsBeforeAssertingInteriorAnatomy(int width, bool dark) {
        var chart = QuadrantRegionDepthRecipe.Create(width == 360 ? "compact-interior-quadrants" : "interior-quadrants", dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var path = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(path)) { Directory.CreateDirectory(path); var name = "quadrant-interior-axes-" + width + "-" + dark;
            File.WriteAllText(Path.Combine(path, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(path, name + ".png"), png); }
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "axis-crossing"));
        Assert.NotEmpty(Ink(prepared, "axis-x-label")); Assert.NotEmpty(Ink(prepared, "axis-y-label"));
        Assert.Equal(6, prepared.Regions.Count(region => region.Role == "point"));
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-title" && region.Label == "Effort (days)");
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-title" && region.Label == "Impact (score)");
    }

    [Fact]
    public void MermaidWholeAxisOptionMapsThinlyAndRejectsConflictingRuleOnlyPlacement() {
        var document = new MermaidParser().ParseQuadrant("quadrantChart\nquadrant-1 Plan\nObserved: [0.2, 0.8]").Document!;
        var chart = document.ToChart(new MermaidQuadrantRenderOptions { ShowPivotAxes = true });
        Assert.Equal(.5, chart.Options.XAxis.CrossAt); Assert.Equal(.5, chart.Options.YAxis.CrossAt);
        Assert.Equal(new ChartPoint(.2, .8), Assert.Single(chart.Series[0].Points));
        Assert.Throws<ArgumentException>(() => document.ToChart(new MermaidQuadrantRenderOptions { ShowPivotAxes = true, ShowPivotRules = true }));
    }

    private static PreparedVisual Prepare(Chart chart) => CartesianAxisRuleCrossingTests.Prepare(chart);
    private static VisualSceneLine Rule(PreparedVisual prepared, string role) => Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == role);
    private static VisualSceneText[] Ink(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == role).ToArray();
}
