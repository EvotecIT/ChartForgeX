using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AutomaticAxisPrecisionTests {
    [Fact]
    public void DefaultScaleCaptionsDistinguishTinySignedAndCloseValuesWithinTheCaptionBound() {
        var scales = new[] {
            new[] { -.0009, -.0005, 0, .0001, .0005 },
            new[] { 1_000_001d, 1_000_002, 1_000_003 },
            new[] { 1d, Math.BitIncrement(1d) },
            new[] { -double.MaxValue, -double.Epsilon, 0, double.Epsilon, double.MaxValue }
        };
        foreach (var values in scales) {
            var format = ChartAxisValueFormatter.Create(new ChartAxis(), values);
            var captions = values.Select(format).ToArray();
            Assert.Equal(values.Length, captions.Distinct(StringComparer.Ordinal).Count());
            Assert.All(captions, caption => Assert.InRange(caption.Length, 1, 24));
            for (var index = 0; index < values.Length; index++) {
                var displayed = double.Parse(captions[index], NumberStyles.Float, CultureInfo.InvariantCulture);
                Assert.Equal(Math.Sign(values[index]), Math.Sign(displayed));
            }
        }
        Assert.Equal(new[] { "0", "1k", "2k" }, new[] { 0d, 1000, 2000 }
            .Select(ChartAxisValueFormatter.Create(new ChartAxis(), new[] { 0d, 1000, 2000 })));
        Assert.Equal("-0.0009", ChartAxisValueFormatter.Format(new ChartAxis(), -.0009));
        Assert.Equal(new[] { "0", "0.0001" }, ChartNumericFormatter.FormatScaleValues(Chart.Create().Options, new[] { -0d, .0001 }));
    }

    [Fact]
    public void ExplicitLabelsAndAxisThenChartFormattersRemainAuthoritativeAndLazy() {
        var ticks = new[] { .0001, .0002, .0003 }; var axisCalls = 0; var chartCalls = 0;
        var axis = new ChartAxis().WithLabelFormatter(_ => { axisCalls++; return "Axis"; });
        axis.Labels.Add(new ChartAxisLabel(ticks[0], "Authored"));
        string Fallback(double value) { chartCalls++; return "Chart"; }
        var format = ChartAxisValueFormatter.Create(axis, ticks, Fallback);
        Assert.Equal(0, axisCalls); Assert.Equal(0, chartCalls);
        Assert.Equal(new[] { "Authored", "Axis", "Axis" }, ticks.Select(format));
        Assert.Equal(2, axisCalls); Assert.Equal(0, chartCalls);
        axis.LabelFormatter = null;
        Assert.Equal(new[] { "Authored", "Chart", "Chart" }, ticks.Select(ChartAxisValueFormatter.Create(axis, ticks, Fallback)));
        Assert.Equal(2, chartCalls);
        axis.WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture));
        Assert.Equal(new[] { "Authored", "0.00", "0.00" }, ticks.Select(ChartAxisValueFormatter.Create(axis, ticks)));
        Assert.Equal("-0.00", ChartAxisValueFormatter.Create(axis, new[] { -.0001 })(-.0001));
        var chart = Chart.Create().WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture));
        Assert.Equal(new[] { "0.00", "0.00", "0.00" }, ChartNumericFormatter.FormatScaleValues(chart.Options, ticks));
        Assert.Equal(new[] { "-0.00" }, ChartNumericFormatter.FormatScaleValues(chart.Options, new[] { -.0001 }));
    }

    [Theory]
    [InlineData(.0001, "0.0001")]
    [InlineData(-.0001, "-0.0001")]
    [InlineData(double.Epsilon, "4.94066E-324")]
    public void DefaultPointDisplayKeepsNonzeroValuesAndPreservesAuthoredNumericPolicies(double value, string expected) {
        var chart = Chart.Create();
        Assert.Equal(expected, chart.Options.ValueFormat.Format(value));
        Assert.Equal(expected, ChartNumericFormatter.FormatValue(chart.Options, value));
        chart.WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture));
        Assert.Equal(value < 0 ? "-0.00" : "0.00", ChartNumericFormatter.FormatValue(chart.Options, value));
        chart.WithValueFormat(ChartValueFormat.Compact());
        Assert.Equal(value < 0 ? "-0" : "0", ChartNumericFormatter.FormatValue(chart.Options, value));
        var calls = 0;
        chart.WithValueFormatter(number => { calls++; return "Authored zero"; });
        Assert.Equal("Authored zero", ChartNumericFormatter.FormatValue(chart.Options, value));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void TinyTrendPointSemanticsAndVisibleLabelsShareTruthfulDefaultDisplayAndDetachedFacts() {
        var observations = Enumerable.Range(0, 4096).Select(index => new ChartPoint(index % 10 + .5, index % 7 - 3));
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).WithDataLabels().AddTrendLine("Regression", observations);
        // Leave room around both endpoints: this contract checks display text, not edge-label fitting.
        chart.Options.XAxis.WithBounds(-1, 11);
        chart.Options.YAxis.WithBounds(-.0012, -.0002);
        var points = chart.Series[0].Points.ToArray();
        var captions = points.Select(point => point.Y.ToString("G6", CultureInfo.InvariantCulture)).ToArray();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var marks = XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(captions, marks.Select(mark => (string?)mark.Attribute("data-cfx-label")));
        Assert.Equal(captions.OrderBy(caption => caption, StringComparer.Ordinal), Texts(svg, "data-label").OrderBy(caption => caption, StringComparer.Ordinal));
        for (var index = 0; index < points.Length; index++) {
            Assert.Equal(points[index].Y, double.Parse((string)marks[index].Attribute("data-cfx-value")!, CultureInfo.InvariantCulture));
            Assert.Contains(captions[index], (string?)marks[index].Attribute("aria-label"));
        }
        chart.WithValueFormatter(_ => throw new InvalidOperationException("Late formatter"));
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedPrimaryAndSecondaryTicksRetainSmallSignedFactsAndDetachedExports(bool secondary) {
        var observations = Enumerable.Range(0, 4096).Select(index => new ChartPoint(index % 10 + .5, index % 7 - 3));
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).AddTrendLine("Regression", observations);
        if (secondary) chart.Series[0].UseSecondaryYAxis();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var role = secondary ? "axis-secondary-y-label" : "axis-y-label";
        var captions = Captions(prepared, role);
        Assert.Equal(new[] { "-0.0009", "-0.0008", "-0.0007", "-0.0006", "-0.0005" }, captions);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal(captions, Texts(svg, role));
        (secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis).WithLabelFormatter(_ => throw new InvalidOperationException("Late formatter"));
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void PreparedCloseNumericXAxisAndProjectedValueAxisKeepDistinctCaptions() {
        var x = Chart.Create().WithSize(900, 400).WithLegend(false)
            .AddLine("Values", new[] { new ChartPoint(1_000_001, 1), new ChartPoint(1_000_005, 2) });
        x.Options.XAxis.WithBounds(1_000_001, 1_000_005);
        Assert.Equal(new[] { "1000001", "1000002", "1000003", "1000004", "1000005" },
            Captions(x.Prepare(VisualExportRequest.ForChart(x).Context), "axis-x-label"));
        var horizontal = Chart.Create().WithSize(640, 400).WithLegend(false)
            .AddHorizontalBar("Values", new[] { new ChartPoint(1, .0001), new ChartPoint(2, .0005) });
        horizontal.Options.XAxis.WithBounds(0, .0005);
        var captions = Captions(horizontal.Prepare(VisualExportRequest.ForChart(horizontal).Context), "axis-x-label");
        Assert.True(captions.Length > 1);
        Assert.Equal(captions.Length, captions.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("0.0005", captions);
    }

    [Fact]
    public void AxisCallbacksRunOncePerPreparedTickAndAreNotInvokedByDetachedExports() {
        var calls = new Dictionary<double, int>();
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false)
            .AddLine("Values", new[] { new ChartPoint(1, .0001), new ChartPoint(2, .0005) });
        chart.Options.YAxis.WithBounds(.0001, .0005).WithLabelFormatter(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return value.ToString("0.0000", CultureInfo.InvariantCulture);
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(Captions(prepared, "axis-y-label").Length, calls.Count);
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.YAxis.LabelFormatter = _ => throw new InvalidOperationException("Late formatter");
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic, .0001, .01)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, -.0005, .0005)]
    public void NonlinearNumericAxesRetainSmallSignedTickFacts(ChartScaleKind scale, double minimum, double maximum) {
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false)
            .AddLine("Values", new[] { new ChartPoint(1, minimum), new ChartPoint(2, maximum) });
        chart.Options.YAxis.WithScale(scale).WithBounds(minimum, maximum);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var regions = prepared.Regions.Where(region => region.Role == "axis-y-label").ToArray();
        Assert.True(regions.Length > 1);
        Assert.Equal(regions.Length, Captions(prepared, "axis-y-label").Distinct(StringComparer.Ordinal).Count());
        foreach (var region in regions) {
            var split = region.Label!.LastIndexOf(" (", StringComparison.Ordinal);
            var raw = double.Parse(region.Label[(split + 2)..^1], CultureInfo.InvariantCulture);
            var displayed = double.Parse(region.Label[..split], CultureInfo.InvariantCulture);
            Assert.Equal(Math.Sign(raw), Math.Sign(displayed));
        }
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void NumericScheduleTicksKeepDefaultPrecisionAndAuthoredValueNotation() {
        var chart = Chart.Create().WithSize(640, 400).WithLegend(false).AddTimelineRange("Window", .0001, .0005);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var captions = prepared.Regions.Where(region => region.Role == "schedule-tick-label").Select(region => region.Label).ToArray();
        Assert.Equal(new[] { "0.0001", "0.0002", "0.0003", "0.0004", "0.0005" }, captions);
        chart.WithValueFormat(ChartValueFormat.Number("0.00", CultureInfo.InvariantCulture));
        prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.All(prepared.Regions.Where(region => region.Role == "schedule-tick-label"), region => Assert.Equal("0.00", region.Label));
    }

    [Fact]
    public void MatrixColumnsShareHonestAutomaticCaptionsWithoutChangingExplicitCallbackInvocations() {
        var values = new[] { .0001, .0002, .0003 };
        Chart Matrix() => Chart.Create().WithSize(640, 400).WithLegend(false)
            .AddHeatmapRow("First", values.Select(value => new ChartPoint(value, 1)))
            .AddHeatmapRow("Second", values.Select(value => new ChartPoint(value, 2)));
        var chart = Matrix(); var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(new[] { "0.0001", "0.0002", "0.0003" }, prepared.Regions
            .Where(region => region.Role == "heatmap-column-label").Select(region => region.Label));
        Assert.Contains(prepared.Regions, region => region.Label == "First, 0.0001: 1");
        var calls = 0; chart = Matrix();
        chart.Options.XAxis.WithLabelFormatter(value => { calls++; return "Column " + value; });
        prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(15, calls); // Three measurement, six cell, three width, three placement resolutions.
        _ = prepared.ToSvg(); _ = prepared.ToPng();
        Assert.Equal(15, calls);
    }

    private static string[] Captions(PreparedVisual prepared, string role) => prepared.Regions.Where(region => region.Role == role)
        .Select(region => region.Label![..region.Label!.LastIndexOf(" (", StringComparison.Ordinal)]).ToArray();

    private static string[] Texts(string svg, string role) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == role)
        .Select(element => element.Value).ToArray();
}
