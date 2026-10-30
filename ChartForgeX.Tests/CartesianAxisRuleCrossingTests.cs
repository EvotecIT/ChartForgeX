using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Rule-only crossings use primary physical coordinates without changing mark projection or edge-label geometry.</summary>
public sealed class CartesianAxisRuleCrossingTests {
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void BothPhysicalAxesCrossAtTheAuthoredPivotAndKeepSourceMarksAndCaptionGutters(bool horizontal, bool reversed) {
        var chart = Fixture(horizontal); chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        var before = Prepare(chart); var oldLabels = AxisText(before); var oldPoints = Marks(before); var plot = Plot(before);
        chart.Options.XAxis.WithRuleCrossAt(4); chart.Options.YAxis.WithRuleCrossAt(3);
        var after = Prepare(chart); var x = Rule(after, "axis-x"); var y = Rule(after, "axis-y");
        Assert.Equal(plot.Left, x.Start.X); Assert.Equal(plot.Right, x.End.X);
        // A horizontal chart's named categories read top-down (design rule 2).
        Assert.Equal(plot.Bottom - plot.Height * (reversed != horizontal ? .6 : .4), x.Start.Y, 7);
        Assert.Equal(plot.Left + plot.Width * (reversed ? .7 : .3), y.Start.X, 7);
        Assert.Equal(plot.Top, y.Start.Y); Assert.Equal(plot.Bottom, y.End.Y);
        Assert.NotEmpty(oldLabels); Assert.NotEmpty(oldPoints);
        Assert.Equal(oldLabels, AxisText(after)); Assert.Equal(oldPoints, Marks(after)); Assert.Equal(plot, Plot(after));
        Assert.Equal(3, chart.Options.YAxis.RuleCrossAt); Assert.Equal(4, chart.Options.XAxis.RuleCrossAt);
        var svg = after.ToSvg(); var png = after.ToPng();
        chart.Options.XAxis.WithRuleCrossAt(null); chart.Options.YAxis.WithRuleCrossAt(null);
        Assert.Equal(before.ToSvg(), Prepare(chart).ToSvg()); Assert.Equal(before.ToPng(), Prepare(chart).ToPng());
        Assert.Equal(svg, after.ToSvg()); Assert.Equal(png, after.ToPng());
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void SecondaryRulesUseTheOppositePrimaryPhysicalDomainInsteadOfTheirOwnValueScale(bool horizontal, bool reversed) {
        var chart = Fixture(horizontal); var secondary = new[] { new ChartPoint(2, 100), new ChartPoint(8, 900) };
        if (horizontal) { chart.AddHorizontalBar("Secondary", secondary); chart.WithSecondaryXAxisBounds(0, 1000); }
        else { chart.AddLine("Secondary", secondary); chart.WithSecondaryYAxisBounds(0, 1000); }
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        var before = Prepare(chart); var plot = Plot(before);
        if (horizontal) chart.Options.SecondaryXAxis.RuleCrossAt = 4; else chart.Options.SecondaryYAxis.RuleCrossAt = 3;
        var prepared = Prepare(chart); var rule = Rule(prepared, horizontal ? "axis-secondary-x" : "axis-secondary-y");
        if (horizontal) Assert.Equal(plot.Bottom - plot.Height * (reversed ? .4 : .6), rule.Start.Y, 7); // categories read top-down
        else Assert.Equal(plot.Left + plot.Width * (reversed ? .7 : .3), rule.Start.X, 7);
        Assert.Equal(AxisText(before), AxisText(prepared)); Assert.Equal(Marks(before), Marks(prepared));
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic, false)] [InlineData(ChartScaleKind.Logarithmic, true)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, false)] [InlineData(ChartScaleKind.SymmetricLogarithmic, true)]
    public void NonlinearAndReversedPivotsUseTheCanonicalOppositeAxisTransform(ChartScaleKind scale, bool reversed) {
        var minimum = scale == ChartScaleKind.Logarithmic ? 1 : -100d; var maximum = 100d;
        var chart = Fixture(false).WithXAxisBounds(minimum, maximum).WithYAxisBounds(minimum, maximum);
        chart.Options.XAxis.Scale = scale; chart.Options.YAxis.Scale = scale;
        chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        const double pivot = 10;
        chart.Options.XAxis.RuleCrossAt = pivot; chart.Options.YAxis.RuleCrossAt = pivot;
        var prepared = Prepare(chart);
        Assert.Equal(prepared.XProjection!.Project(pivot), Rule(prepared, "axis-y").Start.X, 7);
        Assert.Equal(prepared.YProjection!.Project(pivot), Rule(prepared, "axis-x").Start.Y, 7);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NegativeLogarithmicCrossingRetainsItsSourceSign(bool reversed) {
        var chart = Chart.Create().WithSize(600, 400).WithHeader(false).WithLegend(false).WithGrid(false).WithDataLabels(false)
            .AddScatter("Negative", new[] { new ChartPoint(-80, -80), new ChartPoint(-2, -2) });
        chart.Options.XAxis.WithLogarithmicScale(10, ChartLogarithmicDomain.Negative).WithBounds(-100, -1).WithReversal(reversed);
        chart.Options.YAxis.WithLogarithmicScale(10, ChartLogarithmicDomain.Negative).WithBounds(-100, -1).WithReversal(reversed);
        chart.Options.XAxis.RuleCrossAt = -10; chart.Options.YAxis.RuleCrossAt = -10;
        var prepared = Prepare(chart); var plot = Plot(prepared);
        Assert.Equal(plot.Left + plot.Width / 2, Rule(prepared, "axis-y").Start.X, 7);
        Assert.Equal(plot.Top + plot.Height / 2, Rule(prepared, "axis-x").Start.Y, 7);
        Assert.Equal(-10, chart.Options.XAxis.RuleCrossAt);
    }

    [Fact]
    public void OutOfWindowAndExclusiveMaximumCrossingsOmitWithoutClampingAndResetDoesNotRetainAWindow() {
        var chart = Fixture(false); chart.Options.YAxis.RuleCrossAt = 5; var context = VisualExportRequest.ForChart(chart).Context;
        var full = chart.Prepare(context); var fullSvg = full.ToSvg(); var fullPng = full.ToPng();
        var outside = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(6, 9)));
        var exclusive = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(1, 5, maximumInclusive: false)));
        foreach (var result in new[] { outside, exclusive }) {
            Assert.DoesNotContain(result.Scene.Nodes, node => node.Role == "axis-y");
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "cartesian.axis-rule-outside-domain");
            var raw = Assert.Single(result.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "axis-rule-crossing");
            Assert.Equal("5", raw.Metadata["data-cfx-rule-cross-at"]); Assert.Equal("false", raw.Metadata["data-cfx-visible"]);
        }
        Assert.Equal(fullSvg, chart.Prepare(context).ToSvg()); Assert.Equal(fullPng, chart.Prepare(context).ToPng());
        Assert.Equal(5, chart.Options.YAxis.RuleCrossAt);
    }

    [Fact]
    public void TimePivotsUseIncludedInstantsAndRejectClosedSessionAliases() {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate();
        var chart = Fixture(false).WithXAxisBounds(start, start + 4);
        chart.ClearSeries(); chart.AddLine("Time", new[] { new ChartPoint(start, 2), new ChartPoint(start + 4, 8) });
        chart.Options.XAxis.WithTimeSessions(new ChartTimeSessionDomain(new[] { new ChartTimeSession(start, start + 1), new ChartTimeSession(start + 3, start + 4) }));
        chart.Options.YAxis.RuleCrossAt = start + 1;
        var prepared = Prepare(chart); var plot = Plot(prepared);
        Assert.Equal(plot.Left + plot.Width / 2, Rule(prepared, "axis-y").Start.X, 7);
        chart.Options.YAxis.RuleCrossAt = start + 2;
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Fact]
    public void InvalidAndForeignCrossingsRejectAtTheOwningBoundary() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxis { RuleCrossAt = double.NaN });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxis { RuleCrossAt = double.PositiveInfinity });
        var logarithmic = Fixture(false).WithXAxisBounds(1, 10); logarithmic.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        logarithmic.Options.YAxis.RuleCrossAt = 0; Assert.Throws<InvalidOperationException>(() => Prepare(logarithmic));
        var pie = Chart.Create().AddPie("Parts", new[] { new ChartPoint(1, 1) }); pie.Options.XAxis.RuleCrossAt = 0;
        Assert.Throws<NotSupportedException>(() => Prepare(pie));
        var missingSecondary = Fixture(false); missingSecondary.Options.SecondaryYAxis.RuleCrossAt = 3;
        Assert.Throws<NotSupportedException>(() => Prepare(missingSecondary));
    }

    internal static Chart Fixture(bool horizontal) {
        var chart = Chart.Create().WithSize(600, 400).WithHeader(false).WithLegend(false).WithGrid(false).WithDataLabels(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        var points = new[] { new ChartPoint(2, 2), new ChartPoint(8, 8) };
        return horizontal ? chart.AddHorizontalBar("Observed", points) : chart.AddScatter("Observed", points);
    }
    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static ChartRect Plot(PreparedVisual prepared) => Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
    private static VisualSceneLine Rule(PreparedVisual prepared, string role) => Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == role);
    private static (string?, string, double, double)[] AxisText(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role?.StartsWith("axis-", StringComparison.Ordinal) == true).Select(text => (text.Role, string.Join("\n", text.Text.Lines.Select(line => line.Text)), text.X, text.Baseline)).ToArray();
    private static ChartRect[] Marks(PreparedVisual prepared) => prepared.Regions.Where(region => region.Role is "point" or "bar").Select(region => region.Bounds).ToArray();
}
