using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MarkupChartFamiliesTests {
    public static TheoryData<string, ChartSeriesKind, bool, bool> NumericFamilies => new() {
        { "stackedbar", ChartSeriesKind.Bar, false, false },
        { "stackedhorizontalbar", ChartSeriesKind.HorizontalBar, false, false },
        { "percentstackedbar", ChartSeriesKind.Bar, true, false },
        { "percentstackedhorizontalbar", ChartSeriesKind.HorizontalBar, true, false },
        { "percentstackedarea", ChartSeriesKind.StackedArea, true, false },
        { "smoothpercentstackedarea", ChartSeriesKind.StackedArea, true, true },
        { "heatmap", ChartSeriesKind.Heatmap, false, false },
        { "gauge", ChartSeriesKind.Gauge, false, false },
        { "lineargauge", ChartSeriesKind.Gauge, false, false }
    };

    [Theory]
    [MemberData(nameof(NumericFamilies))]
    public void NumericCommandsAndTablesPreserveKindsNamesSourceValuesAndNormalization(
        string family, ChartSeriesKind kind, bool percent, bool smooth) {
        foreach (var table in new[] { false, true }) {
            var chart = ParseChart(NumericSource(family.ToUpperInvariant(), table));
            Assert.Equal(kind == ChartSeriesKind.Gauge ? new[] { "Actual" } : new[] { "Actual", "Budget" }, chart.Series.Select(series => series.Name));
            Assert.All(chart.Series, series => {
                Assert.Equal(kind, series.Kind);
                Assert.Equal(percent ? (double?)100 : null, series.NormalizedTo);
                Assert.Equal(smooth, series.Smooth);
            });
            if (kind == ChartSeriesKind.Gauge) {
                Assert.Equal(family == "lineargauge" ? ChartGaugeForm.Linear : ChartGaugeForm.Arc, chart.Options.Gauge.Form);
                AssertGauge(chart.Series[0], 125, -20, 200);
            } else {
                Assert.Equal(new[] { 1d, 2, 3 }, chart.Series[0].Points.Select(point => point.X));
                Assert.Equal(new[] { 20d, 0, 10 }, chart.Series[0].Points.Select(point => point.Y));
                Assert.Equal(new[] { 5d, 0, 30 }, chart.Series[1].Points.Select(point => point.Y));
                Assert.Equal(new[] { "North East", "Zero", "South" }, chart.Options.XAxisLabels.Select(label => label.Text));
                Assert.Equal(new[] { 1d, 2, 3 }, chart.Options.XAxisLabels.Select(label => label.Value));
                if (kind is ChartSeriesKind.Bar or ChartSeriesKind.HorizontalBar)
                    Assert.Equal(ChartBarMode.Stacked, chart.Options.BarMode);
            }
        }
    }

    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(1000, VisualThemeMode.Light)]
    [InlineData(1000, VisualThemeMode.Dark)]
    public void EveryNumericFamilyMatchesNativeSvgAndDecodedPngAtCompactAndWideSizes(int width, VisualThemeMode mode) {
        foreach (var family in new[] { "stackedbar", "stackedhorizontalbar", "percentstackedbar",
            "percentstackedhorizontalbar", "percentstackedarea", "smoothpercentstackedarea", "heatmap", "gauge", "lineargauge" }) {
            var source = "size " + width + "x420\n" + NumericSource(family, false);
            var parsed = new VisualMarkupParser().Parse(Fence(source));
            Assert.Empty(parsed.Diagnostics);
            var chart = Assert.IsType<Chart>(Assert.Single(parsed.Artifacts).Model);
            AssertNativeArtifacts(NativeNumeric(family).WithSize(width, 420), chart, width, mode, family);
            // Preparation must not replace authored observations with normalized display values.
            if (family.Contains("percent", StringComparison.Ordinal)) {
                Assert.Equal(new[] { 20d, 0, 10 }, chart.Series[0].Points.Select(point => point.Y));
                var range = ChartRange.FromChart(chart);
                Assert.InRange(family.Contains("horizontal", StringComparison.Ordinal) ? range.MaxX : range.MaxY, 100, 120);
            }
        }
    }

    [Theory]
    [InlineData("percent-stacked-bar", ChartSeriesKind.Bar)]
    [InlineData("percent_stacked_horizontal_bar", ChartSeriesKind.HorizontalBar)]
    [InlineData("smooth-percent-stacked-area", ChartSeriesKind.StackedArea)]
    public void NormalizedFamilySpellingsRetainPercentageSemantics(string family, ChartSeriesKind kind) {
        var chart = ParseChart("type " + family + "\nvalues 3 9");
        var series = Assert.Single(chart.Series);
        Assert.Equal(kind, series.Kind);
        Assert.Equal(100d, series.NormalizedTo);
        Assert.Equal(new[] { 3d, 9 }, series.Points.Select(point => point.Y));
    }

    [Theory]
    [InlineData("gauge", -10)]
    [InlineData("gauge", 0)]
    [InlineData("gauge", 100)]
    [InlineData("lineargauge", 120)]
    public void GaugeDefaultScalePreservesAuthoredValuesAtAndBeyondDisplayBounds(string family, double value) {
        var chart = ParseChart("type " + family + "\nseries \"Reading\"\nvalues " + value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        AssertGauge(Assert.Single(chart.Series), value, 0, 100);
    }

    private static void AssertGauge(ChartSeries series, double value, double minimum, double maximum) {
        Assert.Empty(series.Points);
        var reading = Assert.Single(series.GaugeValues);
        Assert.Equal(value, reading.Value);
        Assert.Equal(minimum, reading.Minimum);
        Assert.Equal(maximum, reading.Maximum);
    }

    private static string NumericSource(string family, bool table) {
        var gauge = family.Replace("-", string.Empty).Equals("gauge", StringComparison.OrdinalIgnoreCase)
            || family.Equals("lineargauge", StringComparison.OrdinalIgnoreCase);
        var data = gauge
            ? table ? "data:\n| label | Actual |\n| --- | --- |\n| Reading | 125 |\nend"
                : "series Actual values 125"
            : table ? "data:\n| label | Actual | Budget |\n| --- | --- | --- |\n| North East | 20 | 5 |\n| Zero | 0 | 0 |\n| South | 10 | 30 |\nend"
                : "labels \"North East\" Zero South\nseries Actual values 20 0 10\nseries Budget values 5 0 30";
        return "type " + family + "\n" + (gauge ? "option gaugeMinimum -20\noption gaugeMaximum 200\n" : string.Empty) + data;
    }

    private static Chart NativeNumeric(string family) {
        var chart = Chart.Create();
        if (family is "gauge" or "lineargauge") {
            if (family == "lineargauge") return chart.AddLinearGauge("Actual", 125, -20, 200);
            return chart.AddGauge("Actual", 125, -20, 200);
        }
        chart.WithXLabels("North East", "Zero", "South");
        var actual = new[] { new ChartPoint(1, 20), new ChartPoint(2, 0), new ChartPoint(3, 10) };
        var budget = new[] { new ChartPoint(1, 5), new ChartPoint(2, 0), new ChartPoint(3, 30) };
        switch (family) {
            case "stackedbar":
            case "percentstackedbar": chart.WithStackedBars().AddBar("Actual", actual).AddBar("Budget", budget); break;
            case "stackedhorizontalbar":
            case "percentstackedhorizontalbar": chart.WithStackedHorizontalBars().AddHorizontalBar("Actual", actual).AddHorizontalBar("Budget", budget); break;
            case "percentstackedarea": chart.AddStackedArea("Actual", actual).AddStackedArea("Budget", budget); break;
            case "smoothpercentstackedarea": chart.AddSmoothStackedArea("Actual", actual).AddSmoothStackedArea("Budget", budget); break;
            case "heatmap": chart.AddHeatmapRow("Actual", actual).AddHeatmapRow("Budget", budget); break;
            default: throw new ArgumentOutOfRangeException(nameof(family));
        }
        if (family.Contains("percent", StringComparison.Ordinal)) foreach (var series in chart.Series) series.WithNormalization(100);
        return chart;
    }

    private static string Fence(string source, string attributes = "") => "```chartforgex chart v1"
        + (string.IsNullOrWhiteSpace(attributes) ? string.Empty : " {" + attributes.Trim() + "}") + "\n" + source + "\n```";

    private static Chart ParseChart(string source) {
        var parsed = new MarkupChartParser().Parse(source);
        Assert.Empty(parsed.Diagnostics);
        Assert.NotNull(parsed.Document);
        return parsed.Document.Chart;
    }

    private static void AssertNativeArtifacts(Chart expected, Chart actual, int width, VisualThemeMode mode, string name) {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 420)),
            theme: VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), themeMode: mode);
        AssertPreparedArtifacts(expected.Prepare(context), actual.Prepare(context), width, name + "-" + width + "-" + mode);
    }

    private static void AssertPreparedArtifacts(PreparedVisual expected, PreparedVisual actual, int width, string name) {
        var svg = actual.ToSvg("markup-family-parity");
        var png = actual.ToPng();
        Assert.Equal(expected.ToSvg("markup-family-parity"), svg);
        Assert.Equal(expected.ToPng(), png);
        var root = XDocument.Parse(svg).Root;
        Assert.NotNull(root);
        Assert.Equal("svg", root.Name.LocalName);
        Assert.DoesNotContain(root.Descendants(), element => element.Name.LocalName == "script");
        var image = RasterImageDecoder.Decode(png);
        Assert.Equal(width, image.Width);
        Assert.Equal(420, image.Height);
        Assert.Contains(image.Pixels, value => value != 0);
        var directory = Environment.GetEnvironmentVariable("CFX_MARKUP_PROOF_ROOT");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), png);
    }
}
