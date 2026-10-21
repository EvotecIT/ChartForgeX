using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class IntegerAxisTickTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SmallCountDomainsGenerateWholeUnitPositions(int maximum) {
        var axis = new ChartAxis().WithIntegerTicks();
        var expected = Enumerable.Range(0, maximum + 1).Select(value => (double)value).ToArray();
        Assert.Equal(expected, ChartTicks.Generate(axis, 0, maximum));
        Assert.Equal(expected, ChartTicks.GenerateInside(axis, 0, maximum));
    }

    [Theory]
    [InlineData(0, 12, 5)]
    [InlineData(0, 1000000, 200000)]
    [InlineData(-4, 4, 2)]
    [InlineData(-1e308, 1e308, 5e307)]
    public void LargerCountDomainsKeepBoundedRegularWholeNumberSteps(double minimum, double maximum, double expectedStep) {
        var axis = new ChartAxis().WithIntegerTicks();
        foreach (var ticks in new[] { ChartTicks.Generate(axis, minimum, maximum), ChartTicks.GenerateInside(axis, minimum, maximum) }) {
            Assert.InRange(ticks.Count, 2, axis.TickCount);
            Assert.All(ticks, tick => { Assert.True(double.IsFinite(tick)); Assert.Equal(Math.Round(tick), tick); });
            for (var i = 1; i < ticks.Count; i++) Assert.InRange(Math.Abs((ticks[i] - ticks[i - 1]) / expectedStep - 1), 0, 1e-12);
        }
    }

    [Theory]
    [InlineData(.2, 2.8, new double[] { 1, 2 })]
    [InlineData(.2, .8, new double[] { })]
    [InlineData(-.2, .2, new double[] { 0 })]
    [InlineData(-2.8, -.2, new double[] { -2, -1 })]
    public void FractionalBoundsNeverIntroduceFractionalFallbackTicks(double minimum, double maximum, double[] expected) {
        var axis = new ChartAxis().WithBounds(minimum, maximum).WithIntegerTicks();
        Assert.Equal(expected, ChartTicks.Generate(axis, minimum, maximum));
        Assert.Equal(expected, ChartTicks.GenerateInside(axis, minimum, maximum));
        Assert.Equal(minimum, axis.Minimum); Assert.Equal(maximum, axis.Maximum);
    }

    [Fact]
    public void IntegerPolicyIsOptInAndPreservesAuthoredLabelsAndOtherScales() {
        var axis = new ChartAxis();
        Assert.Contains(.2, ChartTicks.GenerateInside(axis, 0, 1));
        axis.WithIntegerTicks().Labels.Add(new ChartAxisLabel(.5, "Half"));
        Assert.Equal(new[] { .5 }, ChartTicks.ForAxis(axis, 0, 1));
        Assert.Equal(new[] { 0d, .5, 1d }, ChartTicks.ForValueAxis(axis, 0, 1));
        foreach (var scale in new[] { ChartScaleKind.Logarithmic, ChartScaleKind.SymmetricLogarithmic, ChartScaleKind.Time }) {
            var ordinary = new ChartAxis().WithScale(scale);
            axis.WithScale(scale);
            Assert.Equal(ChartTicks.GenerateInside(ordinary, .1, .9), ChartTicks.GenerateInside(axis, .1, .9));
        }
        axis.WithScale(ChartScaleKind.Linear).WithIntegerTicks(false);
        Assert.Contains(.2, ChartTicks.GenerateInside(axis, 0, 1));
    }

    [Theory]
    [InlineData(2, new double[] { 0, 4 })]
    [InlineData(100, new double[] { 0, 1, 2, 3, 4 })]
    public void TickCountPreferenceNeverRequiresFractionalPositions(int count, double[] expected) {
        var axis = new ChartAxis().WithIntegerTicks(); axis.TickCount = count;
        Assert.Equal(expected, ChartTicks.GenerateInside(axis, 0, 4));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Radar, "radar-ring-label")]
    [InlineData(ChartSeriesKind.Polar, "polar-radius-label")]
    [InlineData(ChartSeriesKind.RadialBar, "radial-value-label")]
    [InlineData(ChartSeriesKind.RadialColumn, "radial-value-label")]
    public void RadialCountChartsReuseTheSharedIntegerValueTicks(ChartSeriesKind kind, string role) {
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).WithDataLabels(false)
            .WithXLabels("Ready", "Running", "Complete").WithYAxisIntegerTicks();
        var values = new[] { new ChartPoint(1, 2), new ChartPoint(2, 0), new ChartPoint(3, 1) };
        switch (kind) {
            case ChartSeriesKind.Radar: chart.AddRadar("Items", values); break;
            case ChartSeriesKind.Polar: chart.AddPolar("Items", values); break;
            case ChartSeriesKind.RadialBar: chart.AddRadialBar("Items", values); break;
            case ChartSeriesKind.RadialColumn: chart.AddRadialColumn("Items", values); break;
        }
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Regions.Where(region => region.Role == role).Select(region => region.Label).ToArray();
        Assert.NotEmpty(labels);
        Assert.Contains("1", labels);
        Assert.All(labels, label => Assert.Contains(label, new[] { "0", "1", "2" }));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void FractionalViewportWithoutAnIntegerRendersWithoutInventingEndpoints() {
        var chart = Chart.Create().WithSize(480, 240).WithYAxisIntegerTicks().WithYAxisBounds(.2, .8)
            .AddLine("Measured", new[] { new ChartPoint(0, .4), new ChartPoint(1, .6) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "axis-y-label");
        Assert.DoesNotContain(XDocument.Parse(prepared.ToSvg()).Descendants(), node => (string?)node.Attribute("data-cfx-role") == "axis-y-label");
        Assert.NotEmpty(prepared.ToPng());
        var expanded = ChartTicks.Generate(new ChartAxis().WithIntegerTicks(), 0, 0);
        Assert.True(expanded[0] < expanded[^1]);
        Assert.All(expanded, value => Assert.Equal(Math.Round(value), value));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("y")]
    [InlineData("secondary-y")]
    [InlineData("horizontal-value")]
    public void SmallCountsKeepIntegerGridPositionsAndCaptionsInSvgAndNativePng(string location) {
        foreach (var maximum in new[] { 1, 2, 3, 4 }) foreach (var dark in new[] { false, true }) {
            var chart = Chart.Create().WithSize(640, 300).WithHeader(false).WithLegend(false)
                .WithPadding(20, 20, 20, 20)
                .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
                .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
            ChartAxis axis;
            if (location == "horizontal-value") {
                chart.AddHorizontalBar("Items", new[] { new ChartPoint(1, 1), new ChartPoint(2, maximum) }).WithXAxisIntegerTicks();
                axis = chart.Options.XAxis;
            } else if (location == "x") {
                chart.AddLine("Items", new[] { new ChartPoint(0, 1), new ChartPoint(maximum, 2) }).WithXAxisIntegerTicks();
                axis = chart.Options.XAxis;
            } else {
                chart.AddBar("Items", new[] { new ChartPoint(1, 1), new ChartPoint(2, maximum) });
                if (location == "secondary-y") { chart.Series[0].UseSecondaryYAxis(); chart.WithSecondaryYAxisIntegerTicks(); axis = chart.Options.SecondaryYAxis; }
                else { chart.WithYAxisIntegerTicks(); axis = chart.Options.YAxis; }
            }
            if (dark) axis.WithBounds(0, maximum);
            axis.Reversed = dark;
            var points = chart.Series[0].Points.Select(point => (point.X, point.Y)).ToArray();
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            var horizontal = location is "x" or "horizontal-value";
            var role = horizontal ? "axis-x-label" : location == "secondary-y" ? "axis-secondary-y-label" : "axis-y-label";
            var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role).ToArray();
            var expected = Enumerable.Range(0, maximum + 1).Select(value => value.ToString(CultureInfo.InvariantCulture)).ToArray();
            Assert.Equal(expected, captions.Select(node => node.Text.Lines.Single().Text));
            var svg = XDocument.Parse(prepared.ToSvg());
            Assert.Equal(expected, svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).Select(node => node.Value));
            var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == role.Replace("-label", ""));
            var facts = prepared.Regions.Where(region => region.Role == role).ToArray();
            Assert.Equal(captions.Length, facts.Length);
            var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
            var without = VisualSceneRasterRenderer.Render(new VisualScene(prepared.Scene.Size,
                prepared.Scene.Nodes.Where(node => node is not VisualSceneText text || text.Role != role).ToArray(),
                prepared.Scene.Diagnostics, prepared.Scene.Regions), supersampling: 1);
            for (var i = 0; i < captions.Length; i++) {
                var caption = captions[i];
                Assert.EndsWith("(" + expected[i] + ")", facts[i].Label!);
                var expectedPosition = horizontal ? facts[i].Bounds.Left : facts[i].Bounds.Top;
                if (dark) {
                    var ratio = 1 - i / (double)maximum;
                    var reversedPosition = horizontal ? track.Start.X + (track.End.X - track.Start.X) * ratio
                        : track.End.Y - (track.End.Y - track.Start.Y) * ratio;
                    Assert.Equal(reversedPosition, expectedPosition, 7);
                }
                var actual = horizontal ? caption.X + caption.Text.Metrics.Width / 2
                    : caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height / 2;
                Assert.InRange(Math.Abs(actual - expectedPosition), 0, .5);
                var left = Math.Max(0, (int)Math.Floor(caption.X));
                var right = Math.Min(image.Width, (int)Math.Ceiling(caption.X + caption.Text.Metrics.Width));
                var top = Math.Max(0, (int)Math.Floor(caption.Baseline - caption.Text.Ascent));
                var bottom = Math.Min(image.Height, (int)Math.Ceiling(caption.Baseline - caption.Text.Ascent + caption.Text.Metrics.Height));
                Assert.True(Enumerable.Range(top, bottom - top).Any(y => Enumerable.Range(left, right - left).Any(x => {
                    var offset = (y * image.Width + x) * 4;
                    return Enumerable.Range(0, 4).Any(channel => image.Pixels[offset + channel] != without.Pixels[offset + channel]);
                })), "Every whole-number caption must paint in native raster output at its prepared position.");
            }
            Assert.NotEmpty(prepared.ToPng());
            Assert.Equal(points, chart.Series[0].Points.Select(point => (point.X, point.Y)));
        }
    }
}
