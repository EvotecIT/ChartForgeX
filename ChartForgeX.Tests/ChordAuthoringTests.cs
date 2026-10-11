using System.Collections;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChordAuthoringTests {
    [Fact]
    public void ChordCopiesDirectedCyclicParallelAndSelfFactsWithoutNumericPoints() {
        var nodes = new List<ChartNode> { new("a", "Support"), new("b", "Support") };
        var links = new List<ChartFlowLink> { new("ab", "a", "b", 7), new("ba", "b", "a", 3), new("parallel", "a", "b", 2), new("self", "a", "a", 1), new("zero", "b", "a", 0) };
        var chart = Chart.Create().AddChord("Transfers", nodes, links); var series = Assert.Single(chart.Series);
        nodes.Clear(); links.Clear();
        Assert.Equal(ChartSeriesKind.Chord, series.Kind); Assert.Equal(52, (int)series.Kind); Assert.Equal(48, (int)ChartSeriesKind.GanttLane);
        Assert.Equal(new[] { "a", "b" }, series.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { "ab", "ba", "parallel", "self", "zero" }, series.FlowLinks.Select(link => link.Id));
        Assert.Equal(new[] { 7d, 3, 2, 1, 0 }, series.FlowLinks.Select(link => link.Value));
        Assert.Empty(series.Points); Assert.Empty(series.TreeLinks); Assert.Equal(0, series.SourcePointCount);
        Assert.Throws<NotSupportedException>(() => ((IList)series.Nodes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)series.FlowLinks)[0] = new ChartFlowLink("changed", "a", "b", 2));
        Assert.True(ChartSeriesKindCapabilities.IsExclusive(series.Kind));
    }

    [Fact]
    public void InvalidIdentitiesEndpointsAndFiniteEndpointTotalsAreRejectedAtomically() {
        var chart = Chart.Create().AddLine("Existing", new[] { new ChartPoint(1, 2) }); var existing = chart.Series[0];
        var nodes = new[] { new ChartNode("a", "Same"), new ChartNode("b", "Same"), new ChartNode("c", "Other") };
        void Reject(IEnumerable<ChartNode> suppliedNodes, IEnumerable<ChartFlowLink> links) {
            Assert.Throws<ArgumentException>(() => chart.AddChord("Invalid", suppliedNodes, links));
            Assert.Same(existing, Assert.Single(chart.Series)); Assert.Equal(2, existing.Points[0].Y);
        }
        Reject(new[] { nodes[0], new ChartNode("a", "Renamed") }, Array.Empty<ChartFlowLink>());
        Reject(new[] { default(ChartNode) }, Array.Empty<ChartFlowLink>());
        Reject(nodes, new[] { default(ChartFlowLink) });
        Reject(nodes, new[] { new ChartFlowLink("missing", "a", "unknown", 0) });
        Reject(nodes, new[] { new ChartFlowLink("same", "a", "b", 1), new ChartFlowLink("same", "b", "a", 2) });
        Reject(nodes, new[] { new ChartFlowLink("ab", "a", "b", double.MaxValue), new ChartFlowLink("ac", "a", "c", double.MaxValue) });
        Reject(nodes, new[] { new ChartFlowLink("ba", "b", "a", double.MaxValue), new ChartFlowLink("ca", "c", "a", double.MaxValue) });
        Reject(nodes, new[] { new ChartFlowLink("ab", "a", "b", 1e308), new ChartFlowLink("ba", "b", "a", 1e308) });
        Reject(nodes, new[] { new ChartFlowLink("self", "a", "a", double.MaxValue) });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFlowLink("negative", "a", "b", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartFlowLink("infinite", "a", "b", double.PositiveInfinity));
    }

    [Fact]
    public void ZeroFlowFactsDoNotRelaxSankeyGraphAndWeightPolicy() {
        var nodes = new[] { new ChartNode("a", "A"), new ChartNode("b", "B") };
        var zero = new ChartFlowLink("zero", "a", "b", 0);
        Assert.Equal(0, Chart.Create().AddChord("Zero", nodes, new[] { zero }).Series[0].FlowLinks[0].Value);
        Assert.Throws<ArgumentException>(() => Chart.Create().AddSankey("Zero", nodes, new[] { zero }));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddSankey("Self", nodes, new[] { new ChartFlowLink("self", "a", "a", 1) }));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddSankey("Cycle", nodes, new[] { new ChartFlowLink("ab", "a", "b", 1), new ChartFlowLink("ba", "b", "a", 1) }));
    }

    [Fact]
    public void NodeStatesValidateIdentityAndEnumBeforeMutationAndExposeAReadonlyView() {
        var first = Chart.Create().AddChord("One", new[] { new ChartNode("a", "Support") }, new[] { new ChartFlowLink("self", "a", "a", 1) }).Series[0];
        var second = Chart.Create().AddChord("Two", new[] { new ChartNode("a", "Support") }, new[] { new ChartFlowLink("self", "a", "a", 1) }).Series[0];
        first.WithNodeState("a", ChartSeriesState.Warning);
        Assert.Throws<ArgumentException>(() => first.WithNodeState("Support", ChartSeriesState.Danger));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.WithNodeState("a", (ChartSeriesState)999));
        Assert.Equal(ChartSeriesState.Warning, Assert.Single(first.NodeStates).Value); Assert.Empty(second.NodeStates);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, ChartSeriesState>)first.NodeStates).Clear());
        Assert.Throws<ArgumentException>(() => new ChartSeries("Points", ChartSeriesKind.Line, new[] { new ChartPoint(1, 1) }).WithNodeState("1", ChartSeriesState.Warning));
    }

    [Fact]
    public void ChordRejectsRawPointGeometryAndMixedCoordinateSystems() {
        Assert.Throws<ArgumentException>(() => new ChartSeries("Raw", ChartSeriesKind.Chord, new[] { new ChartPoint(1, 2) }));
        var chart = Chart.Create().AddChord("Flows", new[] { new ChartNode("a", "A"), new ChartNode("b", "B") }, new[] { new ChartFlowLink("ab", "a", "b", 1) });
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(500, 350)));
        chart.Series[0].Points.Add(new ChartPoint(1, 2)); Assert.Throws<InvalidOperationException>(() => chart.Prepare(context));
        chart.Series[0].Points.Clear(); chart.AddLine("Other", new[] { new ChartPoint(1, 2) });
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(context));
    }

    [Fact]
    public void TypedChordOptionsRejectUnsupportedGeometryAndEnums() {
        var options = new ChartChordOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.StartAngleDegrees = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SweepAngleDegrees = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SweepAngleDegrees = 361);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.NodeGapDegrees = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.NodeThicknessRatio = 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.RibbonOpacity = 1.1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DirectionCue = (ChartChordDirectionCue)999);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.LabelContent = (ChartChordLabelContent)999);
    }

    [Fact]
    public void NumericMarkupCannotInventChordRelationshipFacts() {
        var result = new MarkupChartParser().Parse("""
            ```chartforgex chart v1
            series Transfers type chord values 18 7
            ```
            """);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Diagnostics, item => item.Message.Contains("Unknown chart type", StringComparison.Ordinal));
    }
}
