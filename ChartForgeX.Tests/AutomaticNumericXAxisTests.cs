using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AutomaticNumericXAxisTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void DocumentSizedCapacitySpecimenPaintsAllSixAutomaticCaptions(VisualThemeMode mode) {
        var observed = new[] { 42, 58, 73, 91, 67, 82 };
        var chart = Chart.Create().AddLine("Observed", observed.Select((value, index) => new ChartPoint(index + 1, value)))
            .AddLine("Capacity", Enumerable.Range(1, 6).Select(index => new ChartPoint(index, 100)));
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(600, 360), padding: 24),
            VisualTheme.Graphite().WithTypography(new VisualTypography("Arial", 24, 16, 15, 15, 15)), mode,
            new VisualFrame("Service capacity", "Events per minute · six observation intervals", showLegend: true,
                legendPosition: ChartLegendPosition.BottomLeft, showSurface: true));
        Assert.False(chart.Options.XAxis.HasExplicitTickCount);
        Assert.Empty(chart.Options.XAxis.Labels);
        var prepared = chart.Prepare(context);
        var labels = Labels(prepared);
        Assert.Equal(new[] { "1", "2", "3", "4", "5", "6" }, labels.Select(Text));
        Assert.Equal(labels.Select(Text), XDocument.Parse(prepared.ToSvg()).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "axis-x-label").Select(element => element.Value));
        var png = PngReader.Decode(prepared.ToPng());
        Assert.Equal(600, png.Width); Assert.Equal(360, png.Height);
        foreach (var label in labels) {
            var ink = 0;
            for (var y = Math.Max(0, (int)Math.Floor(label.Baseline - label.Text.Ascent) - 1);
                y < Math.Min(png.Height, Math.Ceiling(label.Baseline - label.Text.Ascent + label.Text.Metrics.Height) + 1); y++)
                for (var x = Math.Max(0, (int)Math.Floor(label.X) - 1);
                    x < Math.Min(png.Width, Math.Ceiling(label.X + label.Text.Metrics.Width) + 1); x++) {
                    var pixel = (y * png.Width + x) * 4;
                    if (mode == VisualThemeMode.Light ? png.Pixels[pixel] < 160 && png.Pixels[pixel + 1] < 160 && png.Pixels[pixel + 2] < 160
                        : png.Pixels[pixel] > 100 && png.Pixels[pixel + 1] > 100 && png.Pixels[pixel + 2] > 100) ink++;
                }
            Assert.True(ink >= 5, "Native PNG must paint caption " + Text(label) + " at its prepared tick.");
        }
    }

    [Theory]
    [InlineData(1, 6, false)]
    [InlineData(1, 8, true)]
    [InlineData(-6, -1, false)]
    public void AutomaticIntegerCaptionsIncludeEveryFittingUnitAndRemainOnTheirTicks(int first, int last, bool reversed) {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false)
            .AddLine("Observed", Enumerable.Range(first, last - first + 1).Select(x => new ChartPoint(x, x * x)));
        chart.Options.XAxis.Reversed = reversed;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = Labels(prepared);
        Assert.Equal(Enumerable.Range(first, last - first + 1).Select(x => x.ToString(CultureInfo.InvariantCulture)), labels.Select(Text));
        AssertNoCollisions(labels);
        foreach (var label in labels) {
            var tick = prepared.Regions.Single(region => region.Role == "axis-x-label" && region.Label!.StartsWith(Text(label) + " (", StringComparison.Ordinal));
            Assert.InRange(Math.Abs(label.X + label.Text.Metrics.Width / 2 - tick.Bounds.Left), 0, .5);
        }
        var svg = prepared.ToSvg();
        Assert.Equal(labels.Select(Text), XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "axis-x-label").Select(element => element.Value));
        var png = prepared.ToPng();
        Assert.True(png.Length > 64);
        chart.Options.XAxis.LabelFormatter = _ => throw new InvalidOperationException("Detached export must use prepared labels.");
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(240, false)]
    [InlineData(240, true)]
    [InlineData(600, true)]
    public void AutomaticCaptionsCoarsenWhenMeasuredLabelsCannotFit(int width, bool longLabels) {
        var chart = NumericAxisExamples.Create(width, VisualThemeMode.Light, longLabels);
        var labels = Labels(chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        Assert.NotEmpty(labels);
        if (longLabels) Assert.True(labels.Length < 6);
        AssertNoCollisions(labels);
    }

    [Fact]
    public void ExplicitCountAndAuthoredCategoriesRetainTheirOwnPolicies() {
        var chart = NumericAxisExamples.Create(600, VisualThemeMode.Light);
        chart.Options.XAxis.TickCount = 3;
        var explicitLabels = Labels(chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        Assert.InRange(explicitLabels.Length, 1, 3);
        chart.Options.XAxis.WithAutomaticTickCount();
        chart.WithXLabels(Enumerable.Range(1, 6).Select(x => new ChartAxisLabel(x, "Run " + x)));
        Assert.Equal(Enumerable.Range(1, 6).Select(x => "Run " + x), Labels(chart.Prepare(VisualExportRequest.ForChart(chart).Context)).Select(Text));
    }

    [Fact]
    public void ShortFractionalDomainsKeepUsefulSubunitCaptions() {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithXAxisBounds(.1, .5)
            .AddLine("Observed", new[] { new ChartPoint(.1, 1), new ChartPoint(.5, 2) });
        var labels = Labels(chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        Assert.Equal(new[] { "0.1", "0.2", "0.3", "0.4", "0.5" }, labels.Select(Text));
        AssertNoCollisions(labels);
    }

    [Theory]
    [InlineData(0, 1e200)]
    [InlineData(1e100, 1e100 + 1e87)]
    [InlineData(-1e308, 1e308)]
    public void AutomaticSearchKeepsExtremeDomainsFiniteDistinctAndBounded(double minimum, double maximum) {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithXAxisBounds(minimum, maximum)
            .AddLine("Observed", new[] { new ChartPoint(minimum, 1), new ChartPoint(maximum, 2) });
        var labels = Labels(chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        Assert.InRange(labels.Length, 2, 100);
        Assert.Equal(labels.Length, labels.Select(Text).Distinct(StringComparer.Ordinal).Count());
        Assert.All(labels, label => Assert.True(double.IsFinite(label.X)));
        AssertNoCollisions(labels);
    }

    [Fact]
    public void WideAutomaticScalesKeepBreathingRoomWhileDenseCanUseMoreTicks() {
        var chart = Chart.Create().WithSize(1200, 360).WithLegend(false).WithXAxisBounds(0, 100)
            .AddLine("Observed", new[] { new ChartPoint(0, 1), new ChartPoint(100, 2) });
        var automatic = Labels(chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        Assert.InRange(automatic.Length, 2, 20);
        AssertNoCollisions(automatic);
        chart.Options.XAxis.LabelDensity = ChartLabelDensity.Dense;
        var dense = Labels(chart.Prepare(VisualExportRequest.ForChart(chart).Context));
        Assert.True(dense.Length > automatic.Length);
        AssertNoCollisions(dense);
    }

    private static VisualSceneText[] Labels(PreparedVisual chart) => chart.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "axis-x-label").ToArray();
    private static string Text(VisualSceneText label) => string.Join("", label.Text.Lines.Select(line => line.Text));
    private static void AssertNoCollisions(VisualSceneText[] labels) {
        var ordered = labels.OrderBy(label => label.X).ToArray();
        for (var index = 1; index < ordered.Length; index++)
            Assert.True(ordered[index - 1].X + ordered[index - 1].Text.Metrics.Width <= ordered[index].X + .01, "Measured axis labels must not overlap.");
    }
}
