using ChartForgeX.Core;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MarkupChartFamiliesTests {
    private const string CandleCommands = "candle 2.5 10 15 8 12\ncandle 7 12 14 6 9\ncandle 11 9 9 9 9";
    private const string CandleTable = """
        candles:
        | close | low | x | high | open |
        | --- | --- | --- | --- | --- |
        | 12 | 8 | 2.5 | 15 | 10 |
        | 9 | 6 | 7 | 14 | 12 |
        | 9 | 9 | 11 | 9 | 9 |
        end
        """;
    private const string ItemCommands = """
        item root "All work" colorValue=0
        item group "Shared label" parent=root
        item a "Shared label" parent=group value=30 colorValue=-5
        item b "Shared label" parent=group value=70 colorValue=0
        item zero "No work" parent=root value=0
        item standalone "Other root" value=5 colorValue=12
        """;
    private const string ItemTable = """
        items:
        | colorValue | parent | value | label | id |
        | --- | --- | --- | --- | --- |
        | 0 | | | All work | root |
        | | root | | Shared label | group |
        | -5 | group | 30 | Shared label | a |
        | 0 | group | 70 | Shared label | b |
        | | root | 0 | No work | zero |
        | 12 | | 5 | Other root | standalone |
        end
        """;
    private const string FlowCommands = """
        node in "Shared label"
        node middle "Shared label"
        node out "Output"
        flow first in middle 7 colorValue=-10
        flow parallel in middle 2
        flow onward middle out 9 colorValue=0
        """;
    private const string FlowTable = """
        nodes:
        | label | id |
        | --- | --- |
        | Shared label | in |
        | Shared label | middle |
        | Output | out |
        end
        flows:
        | target | id | colorValue | value | source |
        | --- | --- | --- | --- | --- |
        | middle | first | -10 | 7 | in |
        | middle | parallel | | 2 | in |
        | out | onward | 0 | 9 | middle |
        end
        """;

    [Theory]
    [InlineData("candlestick", ChartSeriesKind.Candlestick)]
    [InlineData("ohlc", ChartSeriesKind.Ohlc)]
    public void FinancialCommandsAndReorderedTablesPreserveEveryCandleField(string family, ChartSeriesKind kind) {
        foreach (var body in new[] { CandleCommands, CandleTable }) {
            var chart = ParseChart("type " + family + "\nseries \"Daily prices\"\n" + body);
            var series = Assert.Single(chart.Series);
            Assert.Equal(kind, series.Kind);
            Assert.Equal("Daily prices", series.Name);
            Assert.Empty(series.Points);
            Assert.Equal(3, series.ObservationCount);
            Assert.Equal(new[] { (2.5, 10d, 15d, 8d, 12d), (7d, 12d, 14d, 6d, 9d), (11d, 9d, 9d, 9d, 9d) },
                series.FinancialValues.Select(value => (value.X, value.Open, value.High, value.Low, value.Close)));
        }
    }

    [Fact]
    public void TreemapCommandsAndReorderedTablesPreserveHierarchyNullGroupsAndIndependentColors() {
        foreach (var body in new[] { ItemCommands, ItemTable }) {
            var chart = ParseChart("type treemap\nseries \"Work distribution\"\n" + body);
            var series = Assert.Single(chart.Series);
            Assert.Equal(ChartSeriesKind.Treemap, series.Kind);
            Assert.Equal("Work distribution", series.Name);
            Assert.Empty(series.Points);
            Assert.Equal(new[] { "root", "group", "a", "b", "zero", "standalone" }, series.HierarchyItems.Select(item => item.Id));
            Assert.Equal(new[] { "All work", "Shared label", "Shared label", "Shared label", "No work", "Other root" }, series.HierarchyItems.Select(item => item.Label));
            Assert.Equal(new string?[] { null, "root", "group", "group", "root", null }, series.HierarchyItems.Select(item => item.ParentId));
            Assert.Equal(new double?[] { null, null, 30, 70, 0, 5 }, series.HierarchyItems.Select(item => item.Value));
            Assert.Equal(new double?[] { 0, null, -5, 0, null, 12 }, series.HierarchyItems.Select(item => item.ColorValue));
        }
    }

    [Fact]
    public void SankeyCommandsAndReorderedTablesPreserveIdsEndpointsParallelWeightsAndColors() {
        foreach (var body in new[] { FlowCommands, FlowTable }) {
            var chart = ParseChart("type sankey\nseries \"Transfers\"\n" + body);
            var series = Assert.Single(chart.Series);
            Assert.Equal(ChartSeriesKind.Sankey, series.Kind);
            Assert.Equal("Transfers", series.Name);
            Assert.Empty(series.Points);
            Assert.Equal(new[] { "in", "middle", "out" }, series.Nodes.Select(node => node.Id));
            Assert.Equal(new[] { "Shared label", "Shared label", "Output" }, series.Nodes.Select(node => node.Label));
            Assert.Equal(new[] { "first", "parallel", "onward" }, series.FlowLinks.Select(flow => flow.Id));
            Assert.Equal(new[] { "in", "in", "middle" }, series.FlowLinks.Select(flow => flow.SourceId));
            Assert.Equal(new[] { "middle", "middle", "out" }, series.FlowLinks.Select(flow => flow.TargetId));
            Assert.Equal(new[] { 7d, 2, 9 }, series.FlowLinks.Select(flow => flow.Value));
            Assert.Equal(new double?[] { -10, null, 0 }, series.FlowLinks.Select(flow => flow.ColorValue));
        }
    }

    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(1000, VisualThemeMode.Light)]
    [InlineData(1000, VisualThemeMode.Dark)]
    public void TypedCommandsAndTablesMatchNativeSvgAndDecodedPngAtCompactAndWideSizes(int width, VisualThemeMode mode) {
        foreach (var family in new[] { "candlestick", "ohlc", "treemap", "sankey" }) {
            var bodies = family switch {
                "treemap" => new[] { ItemCommands, ItemTable },
                "sankey" => new[] { FlowCommands, FlowTable },
                _ => new[] { CandleCommands, CandleTable }
            };
            for (var index = 0; index < bodies.Length; index++) {
                var source = "type " + family + "\nseries \"Observations\"\nsize " + width + "x420\n" + bodies[index];
                var parsed = new ChartForgeX.Markup.VisualMarkupParser().Parse(Fence(source));
                Assert.Empty(parsed.Diagnostics);
                var chart = Assert.IsType<Chart>(Assert.Single(parsed.Artifacts).Model);
                AssertNativeArtifacts(NativeTyped(family).WithSize(width, 420), chart, width, mode, family + (index == 0 ? "-commands" : "-table"));
            }
        }
    }

    private static Chart NativeTyped(string family) {
        var chart = Chart.Create();
        if (family == "treemap") return chart.AddTreemap("Observations", new[] {
            new ChartHierarchyItem("root", "All work", colorValue: 0),
            new ChartHierarchyItem("group", "Shared label", "root"),
            new ChartHierarchyItem("a", "Shared label", "group", 30, -5),
            new ChartHierarchyItem("b", "Shared label", "group", 70, 0),
            new ChartHierarchyItem("zero", "No work", "root", 0),
            new ChartHierarchyItem("standalone", "Other root", value: 5, colorValue: 12)
        });
        if (family == "sankey") return chart.AddSankey("Observations", new[] {
            new ChartNode("in", "Shared label"), new ChartNode("middle", "Shared label"), new ChartNode("out", "Output")
        }, new[] {
            new ChartFlowLink("first", "in", "middle", 7, -10), new ChartFlowLink("parallel", "in", "middle", 2),
            new ChartFlowLink("onward", "middle", "out", 9, 0)
        });
        var candles = new[] { new ChartCandlestick(2.5, 10, 15, 8, 12), new ChartCandlestick(7, 12, 14, 6, 9), new ChartCandlestick(11, 9, 9, 9, 9) };
        return family == "candlestick" ? chart.AddCandlestick("Observations", candles) : chart.AddOhlc("Observations", candles);
    }
}
