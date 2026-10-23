using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartAxisProjectionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProjectionUsesTheNativeMappingAndExtrapolatesWithoutClamping(bool reversed) {
        var chart = Line().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        chart.Options.XAxis.Reversed = reversed;
        var prepared = chart.Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(2, 6)));
        var projection = Assert.IsType<ChartAxisProjection>(prepared.XProjection);
        var plot = projection.PlotBounds;
        Assert.Equal(reversed ? plot.Right : plot.Left, projection.Project(2), 9);
        Assert.Equal(reversed ? plot.Left : plot.Right, projection.Project(6), 9);
        Assert.Equal(plot.Left + plot.Width / 2, projection.Project(4), 9);
        foreach (var value in new[] { -2d, 2, 4, 6, 10 }) Assert.Equal(value, projection.Unproject(projection.Project(value)), 9);
        Assert.Equal(reversed ? 10 : -2, projection.Unproject(plot.Left - plot.Width), 9);
        Assert.Equal(reversed ? -2 : 10, projection.Unproject(plot.Right + plot.Width), 9);
        foreach (var region in prepared.Regions.Where(region => region.Role == "point")) {
            var source = chart.Series[0].Points[int.Parse(region.Id.Substring(region.Id.LastIndexOf('-') + 1), System.Globalization.CultureInfo.InvariantCulture)];
            Assert.Equal(projection.Project(source.X), region.Bounds.Left + region.Bounds.Width / 2, 9);
        }
    }

    [Fact]
    public void ProjectionFactsDoNotChangeScenePaintOrSvgIdentity() {
        var chart = Line();
        var prepared = chart.Prepare(Context());
        Assert.NotNull(prepared.XProjection);
        var without = new PreparedVisual(prepared.Scene, prepared.Accessibility,
            svgOptions: new VisualSvgOptions(colorVariables: chart.Options.SvgColorVariables));
        Assert.Null(without.XProjection);
        Assert.Equal(without.ToSvg(), prepared.ToSvg()); Assert.Equal(without.ToPng(), prepared.ToPng());
    }

    [Theory]
    [InlineData(double.NaN, 1)]
    [InlineData(0, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 1)]
    public void ViewportRequiresFiniteIncreasingBounds(double minimum, double maximum) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxisWindow(minimum, maximum));

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(5, 11)]
    [InlineData(11, 12)]
    public void ViewportCannotExtendBeyondTheResolvedFullDomain(double minimum, double maximum) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Line().WithXAxisBounds(0, 10).Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(minimum, maximum))));

    [Theory]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.HorizontalBar)]
    public void BarFamiliesCapturePhysicalProjectionsAndAcceptAnXWindow(ChartSeriesKind kind) {
        var points = new[] { new ChartPoint(1, 2), new ChartPoint(3, 4) };
        var chart = kind switch {
            ChartSeriesKind.Bar => Chart.Create().AddBar("Observed", points),
            ChartSeriesKind.HorizontalBar => Chart.Create().AddHorizontalBar("Observed", points),
            ChartSeriesKind.Area => Chart.Create().AddArea("Observed", points),
            _ => Chart.Create().AddScatter("Observed", points)
        };
        Assert.NotNull(chart.Prepare(Context()).XProjection);
        var window = chart.Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(1, 2)));
        Assert.Equal(1, window.XProjection!.Minimum); Assert.Equal(2, window.XProjection.Maximum);
        Assert.NotEmpty(window.ToPng());
    }

    [Fact]
    public void NonlinearAndMixedCartesianChartsCaptureProjectionsWhileSparklinesRemainExcluded() {
        var nonlinear = Chart.Create().AddLine("Positive", new[] { new ChartPoint(1, 2), new ChartPoint(10, 4) })
            .WithXAxisBounds(1, 10).WithXAxisScale(ChartScaleKind.Logarithmic);
        var mixed = Line().AddBar("Other", new[] { new ChartPoint(1, 2) });
        var sparkline = Line(); sparkline.Options.IsSparkline = true;
        foreach (var chart in new[] { nonlinear, mixed }) {
            var window = chart.Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(1, 2)));
            Assert.Equal(1, window.XProjection!.Minimum); Assert.Equal(2, window.XProjection.Maximum);
            Assert.Equal(1.5, window.XProjection.Unproject(window.XProjection.Project(1.5)), 10);
        }
        Assert.Null(sparkline.Prepare(Context()).XProjection);
        Assert.Throws<NotSupportedException>(() => sparkline.Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(1, 2))));
    }

    [Fact]
    public void EmptyExhaustedAndUnrepresentableWindowsFailClearly() {
        var empty = Chart.Create().AddLine("Empty", Array.Empty<ChartPoint>());
        Assert.Null(empty.Prepare(Context()).XProjection);
        Assert.Throws<InvalidOperationException>(() => empty.Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(0, 1))));
        var context = Context(); var style = new TextStyle { Font = context.Font, FontSize = 20, LineHeight = 1 };
        var headerHeight = Math.Max(26, new VisualSceneBuilder(context.Layout.Size, context.Font).MeasureText("Mg", style).Height);
        // One heading fits, but its ordinary trailing frame gap exhausts the content rectangle.
        var tiny = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, headerHeight + 1), padding: 0),
            frame: new VisualFrame(title: "Header", showLegend: false, titleStyle: style));
        var exhausted = Line().Prepare(tiny);
        Assert.Null(exhausted.XProjection); Assert.Contains(exhausted.Diagnostics, item => item.Code == "cartesian.insufficient-space");
        Assert.Throws<InvalidOperationException>(() => Line().Prepare(tiny, new ChartCartesianViewport(x: new ChartAxisWindow(1, 2))));
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Line().WithXAxisBounds(0, 10)
            .Prepare(Context(), new ChartCartesianViewport(x: new ChartAxisWindow(0, double.Epsilon))));
        Assert.Equal("viewport", error.ParamName);
    }

    [Fact]
    public void PublicPreparationAcceptsFullDomainResetAndStillRequiresContext() {
        var chart = Line();
        Assert.Equal(chart.Prepare(Context()).ToSvg(), chart.Prepare(Context(), null).ToSvg());
        Assert.Throws<ArgumentNullException>(() => chart.Prepare(null!, new ChartCartesianViewport(x: new ChartAxisWindow(1, 2))));
    }

    [Fact]
    public void ProjectionRejectsNonfiniteInputsAndOverflowingExtrapolation() {
        var projection = new ChartAxisProjection(-double.MaxValue, double.MaxValue, -double.MaxValue, double.MaxValue,
            new ChartRect(10, 20, 100, 80), new ChartAxis { Reversed = false }, ChartAxisDimension.X);
        Assert.Equal(0, projection.Unproject(60));
        Assert.Equal(-double.MaxValue, projection.Unproject(10)); Assert.Equal(double.MaxValue, projection.Unproject(110));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Unproject(210));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Project(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.Unproject(double.PositiveInfinity));
        var cancellation = new ChartAxisProjection(-1e308, -9e307, -1e308, -9e307, new ChartRect(10, 20, 100, 80), new ChartAxis { Reversed = false }, ChartAxisDimension.X);
        Assert.Equal(1, cancellation.Unproject(2010) / 1e308, 10);
        var narrow = new ChartAxisProjection(0, 1, 0, double.Epsilon, new ChartRect(10, 20, 100, 80), new ChartAxis { Reversed = false }, ChartAxisDimension.X);
        Assert.Throws<ArgumentOutOfRangeException>(() => narrow.Project(1));
    }

    private static Chart Line() => Chart.Create().WithLegend(false)
        .AddLine("Observed", new[] { new ChartPoint(0, 2), new ChartPoint(5, 5), new ChartPoint(10, 8) });
    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(420, 280)), frame: new VisualFrame(showLegend: false));
}
