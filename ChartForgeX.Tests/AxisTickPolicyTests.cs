using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Authored tick steps, explicit values, spacing bounds, minor ticks and per-axis grids at the tick-generation boundary.</summary>
public sealed class AxisTickPolicyTests {
    [Fact]
    public void FixedStepPlacesWholeMultiplesInsideTheDomainAndOneOuterTickForAutomaticBounds() {
        var axis = new ChartAxis().WithTickStep(25);
        Assert.Equal(new double[] { 25, 50, 75, 100 }, ChartTicks.GenerateInside(axis, 3, 110));
        Assert.Equal(new double[] { 0, 25, 50, 75, 100, 125 }, ChartTicks.Generate(axis, 3, 110));
        Assert.Equal(new double[] { -50, -25, 0 }, ChartTicks.GenerateInside(axis, -60, 10));
        // Decimal steps keep their decimal values instead of accumulating binary error (0.30000000000000004).
        Assert.Equal(new[] { 0, .1, .2, .3, .4, .5 }, ChartTicks.GenerateInside(new ChartAxis().WithTickStep(.1), 0, .5));
    }

    [Fact]
    public void LogarithmicStepCountsWholePowersInEitherSignDomain() {
        var positive = new ChartAxis().WithLogarithmicScale().WithTickStep(2);
        Assert.Equal(new double[] { 1, 100, 10_000, 1_000_000 }, ChartTicks.GenerateInside(positive, 1, 2_000_000));
        var negative = new ChartAxis().WithLogarithmicScale(domain: ChartLogarithmicDomain.Negative).WithTickStep(1);
        Assert.Equal(new double[] { -1000, -100, -10 }, ChartTicks.GenerateInside(negative, -1000, -5));
    }

    [Fact]
    public void ExplicitValuesKeepTheirOrderInsideTheDomainAndTheNearestOuterValues() {
        var axis = new ChartAxis().WithTickValues(500, 0, 10, 100, 50, 10);
        Assert.Equal(new double[] { 0, 10, 50, 100, 500 }, axis.TickValues);
        Assert.Equal(new double[] { 10, 50, 100 }, ChartTicks.GenerateInside(axis, 5, 120));
        Assert.Equal(new double[] { 0, 10, 50, 100, 500 }, ChartTicks.Generate(axis, 5, 120));
        Assert.Equal(new double[] { 10, 50, 100 }, ChartTicks.ForAxis(axis, 5, 120));
    }

    [Fact]
    public void CalendarStepPlacesTimeTicksOnMonthBoundaries() {
        var axis = new ChartAxis().WithTimeScale().WithTickStep(new ChartTimeTickInterval(ChartTimeTickUnit.Month));
        var ticks = ChartTicks.GenerateInside(axis, new DateTime(2026, 1, 10).ToOADate(), new DateTime(2026, 4, 20).ToOADate());
        Assert.Equal(new[] { new DateTime(2026, 2, 1), new DateTime(2026, 3, 1), new DateTime(2026, 4, 1) }, ticks.Select(DateTime.FromOADate));
    }

    [Fact]
    public void MinorTicksDivideEachMajorIntervalAndThePartialIntervalsAtBothEnds() {
        var linear = new ChartAxis().WithMinorTicks(4);
        Assert.Equal(new double[] { -10, -5, 5, 10, 15, 20, 30, 35, 40, 45, 55, 60 },
            ChartAxisTickPolicy.Minor(linear, new double[] { 0, 25, 50 }, -10, 60));
        // A logarithmic decade divides in data units: 2…9 between 1 and 10, 20…90 between 10 and 100.
        var logarithmic = new ChartAxis().WithLogarithmicScale().WithMinorTicks(8);
        Assert.Equal(new double[] { 2, 3, 4, 5, 6, 7, 8, 9, 20, 30, 40, 50, 60, 70, 80, 90 },
            ChartAxisTickPolicy.Minor(logarithmic, new double[] { 1, 10, 100 }, 1, 100));
    }

    [Fact]
    public void InvalidTickPoliciesRejectWithClearErrors() {
        foreach (var step in new[] { 0, -1, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxis().WithTickStep(step));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxis().WithTickValues(Enumerable.Range(0, ChartAxis.MaximumTickValues + 1).Select(value => (double)value)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxis().WithTickValues(1, double.NaN));
        Assert.Throws<ArgumentException>(() => new ChartAxis().WithTickValues());
        Assert.Throws<InvalidOperationException>(() => new ChartAxis().WithTickStep(5).WithTickValues(1, 2));
        Assert.Throws<InvalidOperationException>(() => new ChartAxis().WithTickValues(1, 2).WithTickStep(5));
        Assert.Throws<InvalidOperationException>(() => new ChartAxis().WithTickStep(5).WithTickSpacing(20, null));
        Assert.Throws<InvalidOperationException>(() => new ChartAxis().WithTickSpacing(20, null).WithTickValues(1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxis().WithTickSpacing(50, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxis().WithMinorTicks(ChartAxis.MaximumMinorTickCount + 1));
        Assert.Empty(new ChartAxis().WithTickStep(5).WithAutomaticTicks().WithTickValues(1).WithAutomaticTicks().TickValues);

        Assert.Contains("calendar step", Render(chart => chart.Options.XAxis.WithTimeScale().WithTickStep(7)).Message);
        Assert.Contains("not a time axis", Render(chart => chart.Options.YAxis.WithTickStep(new ChartTimeTickInterval(ChartTimeTickUnit.Day))).Message);
        Assert.Contains("symmetric logarithmic", Render(chart => chart.Options.YAxis.WithScale(ChartScaleKind.SymmetricLogarithmic).WithTickStep(10)).Message);
        Assert.Contains("whole powers", Render(chart => chart.Options.YAxis.WithLogarithmicScale().WithTickStep(1.5)).Message);
        Assert.Contains("limit is 1000", Render(chart => chart.Options.YAxis.WithBounds(0, 100).WithTickStep(.01)).Message);

        static InvalidOperationException Render(Action<Chart> configure) {
            var chart = Chart.Create().WithSize(480, 300).AddLine("Measured", new[] { new ChartPoint(1, 5), new ChartPoint(2, 50) });
            configure(chart);
            return Assert.Throws<InvalidOperationException>(() => chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        }
    }

    [Theory]
    [InlineData(null, 50d)]
    [InlineData(220d, null)]
    public void TickSpacingBoundsSteerTheAutomaticSearch(double? minimum, double? maximum) {
        var chart = Chart.Create().WithSize(900, 320).WithHeader(false).WithLegend(false)
            .AddLine("Measured", new[] { new ChartPoint(0, 10), new ChartPoint(100, 40) });
        var automatic = Pitches(chart);
        chart.Options.XAxis.WithTickSpacing(minimum, maximum);
        var bounded = Pitches(chart);
        // The default reads about one tick per 80 drawing units; a 50-unit ceiling makes it denser, a 220-unit floor sparser.
        if (maximum.HasValue) Assert.True(bounded.Max() <= maximum.Value + .001 && bounded.Length > automatic.Length, string.Join(",", bounded));
        else Assert.True(bounded.Min() >= minimum!.Value - .001 && bounded.Length < automatic.Length, string.Join(",", bounded));

        static double[] Pitches(Chart chart) {
            var ticks = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Regions
                .Where(region => region.Role == "axis-x-label").Select(region => region.Bounds.Left).OrderBy(value => value).ToArray();
            return ticks.Skip(1).Select((value, index) => value - ticks[index]).ToArray();
        }
    }

    [Fact]
    public void AxisGridStepMinorLinesAndBandsPaintTheSameGeometryInSvgAndNativePng() {
        var band = ChartColor.FromHex("#2F7D5B");
        var line = ChartColor.FromHex("#B04A3A");
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), VisualThemeMode.Light)
            .AddLine("Measured", new[] { new ChartPoint(0, 12), new ChartPoint(10, 96) })
            .ConfigureYAxis(axis => axis.WithTickStep(25).WithMinorTicks(4)
                .ConfigureGrid(grid => grid.WithColor(line).WithStrokeWidth(1.5).WithDash(4, 3).WithBands(band, .2)));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var majors = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "grid-y").ToArray();
        var captions = prepared.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label).ToArray();
        // The automatic value domain extends to the next multiple of the step: 0…100 in steps of 25.
        Assert.Equal(new[] { "0 (0)", "25 (25)", "50 (50)", "75 (75)", "100 (100)" }, captions);
        Assert.Equal(5, majors.Length);
        Assert.All(majors, node => { Assert.Equal(1.5, node.StrokeWidth); Assert.Equal(new double[] { 4, 3 }, node.Dash!); });
        Assert.Equal(16, prepared.Scene.Nodes.OfType<VisualSceneLine>().Count(node => node.Role == "grid-y-minor"));
        var bands = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "grid-y-band").ToArray();
        Assert.Equal(2, bands.Length);

        var svg = XDocument.Parse(prepared.ToSvg()).Descendants().ToArray();
        int Count(string role) => svg.Count(node => (string?)node.Attribute("data-cfx-role") == role);
        Assert.Equal((5, 16, 2), (Count("grid-y"), Count("grid-y-minor"), Count("grid-y-band")));
        Assert.All(svg.Where(node => (string?)node.Attribute("data-cfx-role") == "grid-y"),
            node => Assert.Equal("4 3", (string?)node.Attribute("stroke-dasharray")));

        // Each band paints native pixels inside its own rectangle.
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var without = VisualSceneRasterRenderer.Render(new VisualScene(prepared.Scene.Size,
            prepared.Scene.Nodes.Where(node => node.Role != "grid-y-band").ToArray(), prepared.Scene.Diagnostics, prepared.Scene.Regions), supersampling: 1);
        foreach (var rectangle in bands) {
            var x = (int)(rectangle.Bounds.Left + rectangle.Bounds.Width / 3);
            var y = (int)(rectangle.Bounds.Top + rectangle.Bounds.Height / 3);
            var offset = (y * image.Width + x) * 4;
            Assert.True(Enumerable.Range(0, 3).Any(channel => image.Pixels[offset + channel] != without.Pixels[offset + channel]),
                "A grid band must paint in native raster output at its prepared position.");
        }
    }
}
