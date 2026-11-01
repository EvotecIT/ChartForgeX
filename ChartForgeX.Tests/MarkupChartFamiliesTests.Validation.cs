using ChartForgeX.Core;
using ChartForgeX.Markup;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MarkupChartFamiliesTests {
    private static readonly IReadOnlyDictionary<string, (string Source, int Line)> InvalidRows =
        new Dictionary<string, (string, int)> {
            ["numeric-table-extra-cell"] = ("type gauge\n| label | value |\n| Reading | 10 | 20 |", 3),
            ["series-table-extra-cell"] = ("type heatmap\n| label | Actual | Budget |\n| Reading | 10 | 20 | 30 |", 3),
            ["typed-table-extra-cell"] = ("type treemap\nitems:\n| id | label | value |\n| a | Leaf | 2 | 3 |\nend", 4),
            ["relationship-table-extra-cell"] = ("type sankey\nnode a A\nnode b B\nflows:\n| id | source | target | value |\n| ab | a | b | 1 | 2 |\nend", 6),
            ["numeric-table-duplicate-header"] = ("type gauge\n| label | value | VALUE |\n| Reading | 10 | 20 |", 2),
            ["typed-table-duplicate-header"] = ("type candlestick\ncandles:\n| x | open | high | low | close | CLOSE |\n| 1 | 10 | 15 | 8 | 12 | 13 |\nend", 3),
            ["numeric-table-competing-labels"] = ("type line\n| label | category | value |\n| January | February | 10 |", 2),
            ["options-table-duplicate-header"] = ("type gauge\nvalues 10\noptions:\n| option | value | VALUE |\n| gaugeMaximum | 20 | 30 |\nend", 4),
            ["options-table-competing-keys"] = ("type gauge\nvalues 10\noptions:\n| option | key | value |\n| gaugeMinimum | gaugeMaximum | 0 |\nend", 4),
            ["options-table-unknown-column"] = ("type gauge\nvalues 10\noptions:\n| option | value | gaugeMaximum |\n| gaugeMinimum | 0 | 200 |\nend", 4),
            ["options-table-extra-cell"] = ("type gauge\nvalues 10\noptions:\n| option | value |\n| gaugeMaximum | 20 | 30 |\nend", 5),
            ["candle-impossible-date"] = ("type candlestick\ncandle 2026-02-30 10 15 8 12", 2),
            ["candle-mixed-coordinate-kinds"] = ("type candlestick\ncandle 2026-06-01 10 15 8 12\ncandle 2 12 16 9 14", 3),
            ["candle-table-mixed-coordinate-kinds"] = ("type ohlc\ncandles:\n| x | open | high | low | close |\n| 1 | 10 | 15 | 8 | 12 |\n| 2026-06-08 | 12 | 16 | 9 | 14 |\nend", 5),
            ["candle-incomplete"] = ("type candlestick\ncandle 1 10 15 8", 2),
            ["candle-extra-field"] = ("type ohlc\ncandle 1 10 15 8 12 99", 2),
            ["candle-high-below-open"] = ("type candlestick\ncandle 1 10 9 8 9", 2),
            ["candle-low-above-close"] = ("type ohlc\ncandle 1 10 15 11 12", 2),
            ["candle-nonfinite-x"] = ("type candlestick\ncandle NaN 10 15 8 12", 2),
            ["candle-nonfinite-close"] = ("type ohlc\ncandle 1 10 15 8 Infinity", 2),
            ["candle-table-invalid-high"] = ("type candlestick\ncandles:\n| x | open | high | low | close |\n| 1 | 10 | 9 | 8 | 12 |\nend", 4),
            ["candle-table-missing-close"] = ("type ohlc\ncandles:\n| x | open | high | low | close |\n| 1 | 10 | 15 | 8 | |\nend", 4),
            ["item-duplicate-id"] = ("type treemap\nitem a A value=2\nitem a Again value=3", 3),
            ["item-missing-parent"] = ("type treemap\nitem a A parent=missing value=2", 2),
            ["item-negative-size"] = ("type treemap\nitem a A value=-1", 2),
            ["item-nonfinite-color"] = ("type treemap\nitem a A value=2 colorValue=NaN", 2),
            ["item-unknown-attribute"] = ("type treemap\nitem a A value=2 ignored=3", 2),
            ["item-table-unknown-parent-column"] = ("type treemap\nitems:\n| id | label | value | Parent ID |\n| a | Leaf | 2 | root |\nend", 3),
            ["candle-table-unknown-column"] = ("type candlestick\ncandles:\n| x | open | high | low | close | volume |\n| 1 | 10 | 15 | 8 | 12 | 20 |\nend", 3),
            ["node-table-unknown-column"] = ("type sankey\nnodes:\n| id | label | group |\n| a | A | Left |\n| b | B | Right |\nend\nflow ab a b 1", 3),
            ["flow-table-unknown-color-column"] = ("type sankey\nnode a A\nnode b B\nflows:\n| id | source | target | value | color amount |\n| ab | a | b | 1 | 5 |\nend", 5),
            ["item-table-missing-id"] = ("type treemap\nitems:\n| id | label | value |\n| | Leaf | 2 |\nend", 4),
            ["item-table-missing-parent"] = ("type treemap\nitems:\n| id | label | parent | value |\n| a | Leaf | missing | 2 |\nend", 4),
            ["node-duplicate-id"] = ("type sankey\nnode a A\nnode a Again\nnode b B\nflow ab a b 1", 3),
            ["flow-duplicate-id"] = ("type sankey\nnode a A\nnode b B\nflow ab a b 1\nflow ab a b 2", 5),
            ["flow-unknown-endpoint"] = ("type sankey\nnode a A\nnode b B\nflow ab a missing 1", 4),
            ["flow-negative-weight"] = ("type sankey\nnode a A\nnode b B\nflow ab a b -1", 4),
            ["flow-nonfinite-color"] = ("type sankey\nnode a A\nnode b B\nflow ab a b 1 colorValue=Infinity", 4),
            ["flow-unknown-attribute"] = ("type sankey\nnode a A\nnode b B\nflow ab a b 1 ignored=3", 4),
            ["flow-table-missing-weight"] = ("type sankey\nnode a A\nnode b B\nflows:\n| id | source | target | value |\n| ab | a | b | |\nend", 6),
            ["candle-section-not-ended"] = ("type candlestick\ncandles:\ntitle Accidental", 3),
            ["item-section-not-ended"] = ("type treemap\nitems:\ntitle Accidental", 3),
            ["candle-on-numeric-family"] = ("type line\ncandle 1 10 15 8 12\nvalues 1", 2),
            ["item-on-numeric-family"] = ("type bar\nitem a A value=2\nvalues 1", 2),
            ["node-on-financial-family"] = ("type candlestick\nnode a A\ncandle 1 10 15 8 12", 2)
        };

    public static IEnumerable<object[]> InvalidRowCases => InvalidRows.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(InvalidRowCases))]
    public void InvalidTypedRowsReturnLocatedErrorsWithoutThrowingOrPartialCharts(string caseName) {
        var (source, line) = InvalidRows[caseName];
        AssertLocatedError(source, line);
        // Markdown preamble and opening fence must shift the error to its real source row.
        AssertLocatedError("Introduction\n\n" + Fence(source), line + 3);
    }

    [Theory]
    [InlineData("treemap-cycle")]
    [InlineData("sankey-cycle")]
    [InlineData("missing-leaf-size")]
    [InlineData("group-with-size")]
    [InlineData("zero-flow")]
    [InlineData("self-flow")]
    public void NativeCollectionInvariantsReportTheBlockLineAndRetainNativeMessages(string caseName) {
        var (source, message) = caseName switch {
            "treemap-cycle" => ("type treemap\nitem a A parent=b\nitem b B parent=a", "acyclic"),
            "sankey-cycle" => ("type sankey\nnode a A\nnode b B\nflow ab a b 1\nflow ba b a 1", "acyclic"),
            "missing-leaf-size" => ("type treemap\nitem a A", "leaves"),
            "group-with-size" => ("type treemap\nitem root Root value=10\nitem leaf Leaf parent=root value=3", "null size"),
            "zero-flow" => ("type sankey\nnode a A\nnode b B\nflow ab a b 0", "positive"),
            "self-flow" => ("type sankey\nnode a A\nflow self a a 1", "distinct nodes"),
            _ => throw new ArgumentOutOfRangeException(nameof(caseName))
        };
        foreach (var fenced in new[] { false, true }) {
            var parsed = new MarkupChartParser().Parse(fenced ? "Introduction\n\n" + Fence(source) : source);
            Assert.True(parsed.HasErrors);
            Assert.Null(parsed.Document);
            Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error
                && diagnostic.Line == (fenced ? 3 : 1) && diagnostic.Message.Contains(message, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("candlestick")]
    [InlineData("ohlc")]
    [InlineData("treemap")]
    [InlineData("sankey")]
    public void TypedFamiliesRejectNumericValuesAndNumericSeriesInsteadOfDiscardingThem(string family) {
        var body = family switch {
            "treemap" => "item a A value=2",
            "sankey" => "node a A\nnode b B\nflow ab a b 1",
            _ => "candle 1 10 15 8 12"
        };
        // Each fixture is independently accepted before adding the incompatible numeric declaration.
        ParseChart("type " + family + "\n" + body);
        foreach (var numeric in new[] { "values 1 2", "series Accidental values 1 2" }) {
            var parsed = new MarkupChartParser().Parse("type " + family + "\n" + body + "\n" + numeric);
            Assert.True(parsed.HasErrors);
            Assert.Null(parsed.Document);
            Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error);
        }
    }

    [Theory]
    [InlineData("gauge")]
    [InlineData("lineargauge")]
    public void GaugesRejectMultipleReadingsInBothDefaultAndNamedSeries(string family) {
        foreach (var body in new[] { "values 10 20", "series Reading values 10 20", "series Reading values 10\nseries Invalid values 20 30" }) {
            var parsed = new MarkupChartParser().Parse("type " + family + "\n" + body);
            Assert.True(parsed.HasErrors);
            Assert.Null(parsed.Document);
            Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error
                && diagnostic.Message.Contains("one", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [InlineData("gauge")]
    [InlineData("lineargauge")]
    public void GaugesRejectMultipleSeriesBeforeRendering(string family) {
        var parsed = new MarkupChartParser().Parse("type " + family + "\nseries Actual values 10\nseries Budget values 20");
        Assert.True(parsed.HasErrors);
        Assert.Null(parsed.Document);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error
            && diagnostic.Message.Contains("one series", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("gaugeMinimum", "NaN")]
    [InlineData("gaugeMaximum", "Infinity")]
    [InlineData("gaugeMaximum", "0")]
    [InlineData("gaugeMaximum", "-1")]
    [InlineData("theme", "unknown-preset")]
    [InlineData("themeMode", "sepia")]
    [InlineData("themeMode", "42")]
    public void InvalidOptionsAreErrorsAtTheAuthoredOptionLine(string option, string value) {
        var source = "type gauge\nvalues 10\noption " + option + " " + value;
        AssertLocatedError(source, 3);
        AssertLocatedError("Introduction\n\n" + Fence(source), 6);
    }

    [Theory]
    [InlineData("theme", "unknown-preset")]
    [InlineData("themeMode", "sepia")]
    public void InvalidFenceThemesReportTheOpeningFence(string option, string value) {
        AssertLocatedError("Introduction\n\n" + Fence("type gauge\nvalues 10", " " + option + "=" + value), 3);
    }

    private static void AssertLocatedError(string source, int line) {
        var parsed = new MarkupChartParser().Parse(source);
        Assert.True(parsed.HasErrors);
        Assert.Null(parsed.Document);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Line == line && diagnostic.Severity == MarkupDiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("gauge")]
    [InlineData("lineargauge")]
    [InlineData("heatmap")]
    [InlineData("percentstackedbar")]
    [InlineData("line")]
    public void DefaultAndNamedNumericDataAreRejectedInsteadOfDiscardingTheDefaultReading(string family) {
        foreach (var declaration in new[] { "values 10", "value Baseline 10" }) {
            var parsed = new MarkupChartParser().Parse(Fence("type " + family + "\n" + declaration + "\nseries Actual values 20"));
            Assert.True(parsed.HasErrors);
            Assert.Null(parsed.Document);
            Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Message.Contains("default numeric values", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("heatmap")]
    [InlineData("line")]
    [InlineData("percentstackedbar")]
    public void IncompleteNumericTableRowsAreLocatedErrorsInsteadOfMovingLaterValues(string family) {
        foreach (var row in new[] { "| Feb | | 2 |", "| Feb | 20 |" }) {
            var source = "type " + family + "\n| label | Actual | Budget |\n| --- | --- | --- |\n| Jan | 10 | 1 |\n" + row + "\n| Mar | 30 | 3 |";
            AssertLocatedError(source, 5);
            AssertLocatedError("Introduction\n\n" + Fence(source), 8);
        }
    }

    [Theory]
    [InlineData("gauge")]
    [InlineData("lineargauge")]
    [InlineData("heatmap")]
    [InlineData("percentstackedbar")]
    [InlineData("line")]
    public void ReservedValueTablesRejectAdditionalColumnsInsteadOfDiscardingObservations(string family) {
        ParseChart("type " + family + "\n| label | value |\n| Reading | 10 |");
        var source = "type " + family + "\n| label | value | Budget |\n| Reading | 10 | 20 |";
        AssertLocatedError(source, 2);
        AssertLocatedError("Introduction\n\n" + Fence(source), 5);
    }

    [Theory]
    [InlineData("heatmap-with-line")]
    [InlineData("line-with-heatmap")]
    [InlineData("heatmap-with-gauge")]
    [InlineData("bar-normalization")]
    [InlineData("horizontal-bar-normalization")]
    [InlineData("area-normalization")]
    public void IncompatibleNativeFamiliesAndStackUnitsAreDiagnosedBeforeExport(string caseName) {
        var source = caseName switch {
            "heatmap-with-line" => "type heatmap\nseries Actual values 10 20\nseries Target type line values 15 15",
            "line-with-heatmap" => "type line\nseries Actual values 10 20\nseries Target type heatmap values 15 15",
            "heatmap-with-gauge" => "type heatmap\nseries Actual values 10\nseries Target type gauge values 15",
            "bar-normalization" => "type percentstackedbar\nseries Actual values 10\nseries Target type stackedbar values 15",
            "horizontal-bar-normalization" => "type percentstackedhorizontalbar\nseries Actual values 10\nseries Target type horizontalbar values 15",
            "area-normalization" => "type percentstackedarea\nseries Actual values 10\nseries Target type stackedarea values 15",
            _ => throw new ArgumentOutOfRangeException(nameof(caseName))
        };
        AssertLocatedError(source, 1);
    }
}
