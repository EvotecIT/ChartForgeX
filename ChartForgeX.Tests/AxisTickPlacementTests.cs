using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AxisTickPlacementTests {
    [Fact]
    public void FractionalPlotMapsExactYEndpointsAndPreservesExtrapolation() {
        var chart = Create(320, 220).WithXAxisBounds(0, 100).WithYAxisBounds(0, 100)
            .AddLine("Values", new[] { new ChartPoint(0, 0), new ChartPoint(100, 100) });
        var range = ChartRange.FromChart(chart);
        var plot = new ChartRect(40, 6.6, 200, 176);
        foreach (var reversed in new[] { false, true }) {
            chart.Options.YAxis.Reversed = reversed;
            var map = new ChartMapper(plot, range, chart.Options.XAxis, chart.Options.YAxis);
            Assert.Equal(reversed ? plot.Top : plot.Bottom, map.Y(0));
            Assert.Equal(reversed ? plot.Bottom : plot.Top, map.Y(100));
            Assert.True(reversed ? map.Y(-100) < plot.Top : map.Y(-100) > plot.Bottom);
            Assert.True(reversed ? map.Y(200) > plot.Bottom : map.Y(200) < plot.Top);
        }
    }

    [Theory]
    [InlineData(60, 90, 6)]
    [InlineData(0, 62, 6)]
    [InlineData(0, 3.4, 6)]
    [InlineData(1.499, 12.499, 6)]
    [InlineData(.1, .3, 3)]
    [InlineData(-3.1, 17.3, 6)]
    [InlineData(-1, 1, 2)]
    [InlineData(0, 7e180, 5)]
    public void NumericTicksNeverExceedRequestedCountOrUseAnUndersizedStep(double minimum, double maximum, int count) {
        foreach (var inside in new[] { false, true }) {
            var ticks = inside ? ChartTicks.GenerateInside(minimum, maximum, count) : ChartTicks.Generate(minimum, maximum, count);
            Assert.InRange(ticks.Count, 1, count);
            Assert.All(ticks, tick => Assert.True(double.IsFinite(tick)));
            if (inside) Assert.All(ticks, tick => Assert.InRange(tick, minimum, maximum));
            else Assert.True(ticks[0] <= minimum && ticks[^1] >= maximum);
            for (var index = 1; index < ticks.Count; index++) {
                Assert.True(ticks[index] > ticks[index - 1]);
                Assert.True((ticks[index] - ticks[index - 1]) / ((maximum - minimum) / (count - 1)) >= .999999);
                if (index > 1) Assert.InRange((ticks[index] - ticks[index - 1]) / (ticks[1] - ticks[0]), .999999, 1.000001);
            }
        }
    }

    [Theory]
    [InlineData(1, 1000, 2)]
    [InlineData(1, 1e7, 3)]
    [InlineData(2, 1e7, 3)]
    [InlineData(2, 1000, 2)]
    [InlineData(.0001, .01, 6)]
    public void LogarithmicTickEnvelopesRespectTheCountWhenAStrideSkipsTheUpperPower(double minimum, double maximum, int count) {
        var axis = new ChartAxis().WithScale(ChartScaleKind.Logarithmic); axis.TickCount = count;
        foreach (var inside in new[] { false, true }) {
            var ticks = inside ? ChartTicks.GenerateInside(axis, minimum, maximum) : ChartTicks.Generate(axis, minimum, maximum);
            Assert.InRange(ticks.Count, 1, count);
            Assert.All(ticks, tick => Assert.InRange(tick, minimum, maximum));
            for (var index = 1; index < ticks.Count; index++) Assert.True(ticks[index] > ticks[index - 1]);
            if (!inside) { Assert.Equal(minimum, ticks[0]); Assert.Equal(maximum, ticks[^1]); }
        }
    }

    [Theory]
    [InlineData(2026, 9, 1, 18, 6)]
    [InlineData(2026, 7, 25, 12, 6)]
    [InlineData(2026, 1, 25, 9, 6)]
    [InlineData(2021, 1, 1, 4200, 6)]
    [InlineData(1900, 1, 1, 2_900_000, 2)]
    public void CalendarTicksNeverExceedRequestedCountAcrossPartialAndMonthBoundaryRanges(int year, int month, int day, int days, int count) {
        var axis = new ChartAxis().WithTimeScale(); axis.TickCount = count;
        var minimum = new DateTime(year, month, day).ToOADate(); var maximum = minimum + days;
        foreach (var inside in new[] { false, true }) {
            var ticks = inside ? ChartTicks.GenerateInside(axis, minimum, maximum) : ChartTicks.Generate(axis, minimum, maximum);
            Assert.InRange(ticks.Count, 2, count);
            for (var index = 1; index < ticks.Count; index++) Assert.True(ticks[index] > ticks[index - 1]);
            if (inside) Assert.All(ticks, tick => Assert.InRange(tick, minimum, maximum));
            else Assert.True(ticks[0] <= minimum && ticks[^1] >= maximum);
        }
        if (days == 18) {
            var ticks = ChartTicks.GenerateInside(axis, minimum, maximum);
            Assert.InRange(ticks.Count, 4, 6);
            for (var index = 1; index < ticks.Count; index++) Assert.Equal(4, ticks[index] - ticks[index - 1]);
        }
        if (days == 4200) {
            var ticks = ChartTicks.GenerateInside(axis, minimum, maximum);
            for (var index = 1; index < ticks.Count; index++) Assert.True(ticks[index] - ticks[index - 1] >= days / (double)(count - 1));
        }
    }

    [Theory]
    [InlineData(800, 360, 0, 100, false)]
    [InlineData(480, 240, 0, 100, false)]
    [InlineData(320, 180, 0, 100, false)]
    [InlineData(640, 300, 60, 90, false)]
    [InlineData(420, 160, 0, 1200, true)]
    public void ValueLabelsStayCenteredOnTheirGridLinesWithoutGapsOrEllipsis(int width, int height, double minimum, double maximum, bool reversed) {
        var chart = Create(width, height).WithYAxisBounds(minimum, maximum)
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
            .AddLine("Load", Enumerable.Range(1, 6).Select(index => new ChartPoint(index, minimum + (maximum - minimum) * index / 7)));
        chart.Options.YAxis.Reversed = reversed;
        chart.Options.YAxis.TickCount = height == 160 ? 12 : 6;
        var prepared = Prepare(chart);
        AssertAxis(prepared, "axis-y-label", "grid-y", horizontal: false);
        var captions = Texts(prepared, "axis-y-label");
        Assert.InRange(captions.Length, 2, chart.Options.YAxis.TickCount);
        Assert.All(captions, label => Assert.Equal(VisualExportRequest.ForChart(chart).Context.Theme.Typography.AxisSize, label.Text.Size));
        var numbers = captions.Select(label => ParseCompact(label.Text.Lines.Single().Text)).ToArray();
        for (var index = 2; index < numbers.Length; index++) Assert.Equal(numbers[1] - numbers[0], numbers[index] - numbers[index - 1], 7);
        AssertExports(prepared);
    }

    [Theory]
    [InlineData(false, 320, 220)]
    [InlineData(false, 640, 320)]
    [InlineData(true, 360, 240)]
    [InlineData(true, 800, 360)]
    public void TimeAndHorizontalValueLabelsKeepTickCentersInsideTheViewport(bool horizontal, int width, int height) {
        var chart = Create(width, height);
        if (horizontal) {
            chart.AddHorizontalBar("Orders", new[] { new ChartPoint(1, 30), new ChartPoint(2, 80), new ChartPoint(3, 100) });
            chart.Options.XAxis.WithBounds(0, 100);
        } else {
            var first = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            chart.WithXAxisTimeScale().AddBar("Orders", Enumerable.Range(0, 19).Select(index => new ChartPoint(first.AddDays(index), 40 + index % 5)));
        }
        var prepared = Prepare(chart);
        AssertAxis(prepared, "axis-x-label", "grid-x", horizontal: true);
        AssertExports(prepared);
    }

    [Fact]
    public void CrowdedCategoryLabelsUseARegularStrideWithoutShrinking() {
        var chart = Create(420, 240).WithXLabels(Enumerable.Range(0, 24).Select(hour => hour.ToString("00", CultureInfo.InvariantCulture) + ":00").ToArray())
            .AddBar("Load", Enumerable.Range(1, 24).Select(index => new ChartPoint(index, index % 5 + 20)));
        var prepared = Prepare(chart); var labels = Texts(prepared, "axis-x-label");
        Assert.InRange(labels.Length, 2, 23);
        Assert.All(labels, label => Assert.Equal(VisualExportRequest.ForChart(chart).Context.Theme.Typography.AxisSize, label.Text.Size));
        var hours = labels.Select(label => int.Parse(label.Text.Lines.Single().Text[..2], CultureInfo.InvariantCulture)).ToArray();
        for (var index = 2; index < hours.Length; index++) Assert.Equal(hours[1] - hours[0], hours[index] - hours[index - 1]);
        AssertAxis(prepared, "axis-x-label", "grid-x", horizontal: true, allTicksLabelled: false);
        AssertExports(prepared);
    }

    [Fact]
    public void DurationAndSecondaryCaptionsKeepCompleteAnchoredEndValues() {
        var chart = Create(640, 300).WithYAxisBounds(0, 100).AddLine("Latency", new[] { new ChartPoint(0, 20), new ChartPoint(5, 90) });
        chart.AddLine("Other", new[] { new ChartPoint(0, 80), new ChartPoint(5, 20) });
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.SecondaryYAxis.WithBounds(0, 100);
        chart.Options.YAxis.WithLabelFormatter(value => value == 100 ? "1 min 40 s" : value.ToString("0", CultureInfo.InvariantCulture) + " s");
        var prepared = Prepare(chart);
        AssertAxis(prepared, "axis-y-label", "grid-y", horizontal: false);
        Assert.Contains(Texts(prepared, "axis-y-label"), label => label.Text.Lines.Single().Text == "1 min 40 s");
        Assert.NotEmpty(Texts(prepared, "axis-secondary-y-label"));
        AssertExports(prepared);
    }

    [Fact]
    public void CrowdedGeneratedTicksCaptureEachFormattedValueOnceAndKeepDetachedSourceFacts() {
        var calls = new Dictionary<double, int>();
        var chart = Create(420, 160).WithYAxisBounds(0, 100)
            .AddLine("Latency", new[] { new ChartPoint(0, 20), new ChartPoint(5, 90) });
        chart.Options.YAxis.TickCount = 30;
        chart.Options.YAxis.WithLabelFormatter(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return value.ToString("0", CultureInfo.InvariantCulture) + " milliseconds";
        });
        var prepared = Prepare(chart);
        var drawn = Texts(prepared, "axis-y-label");
        Assert.InRange(drawn.Length, 2, ChartTicks.GenerateInside(chart.Options.YAxis, 0, 100).Count - 1);
        Assert.Equal(calls.Count, prepared.Regions.Count(region => region.Role == "axis-y-label"));
        Assert.All(calls, pair => {
            Assert.Equal(1, pair.Value);
            var value = pair.Key.ToString("0", CultureInfo.InvariantCulture);
            Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label == value + " milliseconds (" + value + ")");
        });
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.YAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed formatter"));
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    [Theory]
    [InlineData("primary")]
    [InlineData("secondary")]
    [InlineData("horizontal-category")]
    public void SelectedAxisCaptionsReserveTheirFinalMeasuredGutter(string mode) {
        const string wideCaption = "Twenty-five milliseconds";
        var calls = new Dictionary<double, int>();
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf")));
        // At the pinned font's measured height, five 25-step captions fit and six
        // 20-step captions crowd. The coarser set introduces the wider caption.
        var chart = Create(420, 170);
        ChartAxis axis;
        if (mode == "horizontal-category") {
            chart.AddLine("Latency", new[] { new ChartPoint(20, 0), new ChartPoint(90, 5) });
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            axis = chart.Options.YAxis;
        } else {
            chart.AddLine("Latency", new[] { new ChartPoint(0, 20), new ChartPoint(5, 90) });
            axis = mode == "secondary" ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            if (mode == "secondary") { chart.Series[0].UseSecondaryValueAxis(); chart.Options.YAxis.Visible = false; }
        }
        axis.WithBounds(0, 100); axis.TickCount = 6;
        axis.WithLabelFormatter(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return value == 25 ? wideCaption : value.ToString("0", CultureInfo.InvariantCulture);
        });
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context);
        var role = mode == "secondary" ? "axis-secondary-y-label" : "axis-y-label";
        var drawn = Texts(prepared, role);
        Assert.InRange(drawn.Length, 2, axis.TickCount);
        Assert.Contains(drawn, label => label.Text.Lines.Single().Text == wideCaption);
        Assert.Contains(drawn, label => label.Text.Lines.Single().Text == "0");
        Assert.Contains(drawn, label => label.Text.Lines.Single().Text == "100");
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.axis-label-overflow");
        var trackRole = mode == "secondary" ? "axis-secondary-y" : "axis-y";
        var track = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == trackRole);
        var selected = new List<double>();
        foreach (var label in drawn) {
            var text = label.Text.Lines.Single().Text;
            var fact = Assert.Single(prepared.Regions, region => region.Role == role && region.Label!.StartsWith(text + " (", StringComparison.Ordinal));
            var split = fact.Label!.LastIndexOf(" (", StringComparison.Ordinal);
            var value = double.Parse(fact.Label[(split + 2)..^1], CultureInfo.InvariantCulture);
            selected.Add(value);
            Assert.Equal(track.End.Y - (track.End.Y - track.Start.Y) * value / 100, fact.Bounds.Top, 7);
            var center = label.Baseline - label.Text.Ascent + label.Text.Metrics.Height / 2;
            Assert.InRange(Math.Abs(center - fact.Bounds.Top), 0, .5);
            Assert.Equal(context.Theme.Typography.AxisSize, label.Text.Size);
            Assert.InRange(label.X, 14, prepared.Size.Width - 14);
            Assert.InRange(label.X + label.Text.Metrics.Width, 14, prepared.Size.Width - 14);
        }
        for (var index = 2; index < selected.Count; index++) Assert.Equal(selected[1] - selected[0], selected[index] - selected[index - 1], 7);
        if (mode != "secondary") AssertAxis(prepared, role, "grid-y", horizontal: false);
        Assert.Contains(prepared.Regions, region => region.Role == role && region.Label == "20 (20)");
        Assert.Equal(0d, axis.Minimum!.Value); Assert.Equal(100d, axis.Maximum!.Value);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        axis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed formatter"));
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    private static Chart Create(int width, int height) => Chart.Create().WithSize(width, height).WithHeader(false).WithLegend(false)
        .WithPadding(14, 14, 14, 14).ConfigureGridStyle(style => { style.ShowHorizontalLines = true; style.ShowVerticalLines = true; })
        .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Texts(PreparedVisual prepared, string role) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == role).ToArray();
    private static double ParseCompact(string text) => text.EndsWith('k') ? double.Parse(text[..^1], CultureInfo.InvariantCulture) * 1000 : double.Parse(text, CultureInfo.InvariantCulture);

    private static void AssertAxis(PreparedVisual prepared, string role, string gridRole, bool horizontal, bool allTicksLabelled = true) {
        var labels = Texts(prepared, role);
        var lines = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == gridRole).ToArray();
        Assert.NotEmpty(labels);
        if (allTicksLabelled) Assert.Equal(lines.Length, labels.Length);
        var facts = prepared.Regions.Where(region => region.Role == role).ToArray();
        if (!allTicksLabelled) Assert.Equal(facts.Length, lines.Length);
        for (var index = 0; index < labels.Length; index++) {
            var label = labels[index];
            var center = horizontal ? label.X + label.Text.Metrics.Width / 2 : label.Baseline - label.Text.Ascent + label.Text.Metrics.Height / 2;
            var source = allTicksLabelled ? index : Array.FindIndex(facts, fact => fact.Label!.StartsWith(label.Text.Lines.Single().Text + " (", StringComparison.Ordinal));
            Assert.InRange(source, 0, lines.Length - 1);
            var ownTick = horizontal ? lines[source].Start.X : lines[source].Start.Y;
            Assert.InRange(Math.Abs(center - ownTick), 0, .5);
            Assert.DoesNotContain("…", string.Concat(label.Text.Lines.Select(line => line.Text)), StringComparison.Ordinal);
            Assert.InRange(label.X, -.0001, prepared.Scene.Size.Width);
            Assert.InRange(label.X + label.Text.Metrics.Width, 0, prepared.Scene.Size.Width + .0001);
            Assert.InRange(label.Baseline - label.Text.Ascent, -.0001, prepared.Scene.Size.Height);
            Assert.InRange(label.Baseline - label.Text.Ascent + label.Text.Metrics.Height, 0, prepared.Scene.Size.Height + .0001);
        }
    }
    private static void AssertExports(PreparedVisual prepared) {
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.DoesNotContain(svg.Descendants(), element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("axis-", StringComparison.Ordinal) == true && element.Value.Contains('…'));
        Assert.NotEmpty(prepared.ToPng());
    }
}
