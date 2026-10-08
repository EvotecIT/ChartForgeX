using System.Linq;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyViewFrameTests {
    [Fact]
    public void FocusedViewHeadingsUseTheSameSingleFrameReservationAsSourceHeadings() {
        var source = Diagram().WithTitle("Source").WithSubtitle("Source detail");
        var view = new TopologyView { Id = "focused", Title = "Focused network", Subtitle = "Selected services" };
        var options = new TopologyRenderOptions { IncludeLegend = false, View = view };
        var focused = source.Prepare(options);
        var plain = Diagram().WithTitle(view.Title!).WithSubtitle(view.Subtitle!).Prepare(new TopologyRenderOptions { IncludeLegend = false });
        var envelope = focused.ToInterchangeEnvelope(); var ordinary = plain.ToInterchangeEnvelope();
        Assert.Equal("network-focused", envelope.Id);
        Assert.Equal(view.Title, envelope.Title); Assert.Equal(view.Subtitle, envelope.Subtitle);
        Assert.Equal(view.Title, focused.Visual.Accessibility.Name);
        Assert.Equal(ordinary.Width, envelope.Width); Assert.Equal(ordinary.Height, envelope.Height);
        foreach (var node in envelope.Nodes) {
            var expected = ordinary.Nodes.Single(item => item.Id == node.Id);
            Assert.Equal(expected.X, node.X); Assert.Equal(expected.Y, node.Y);
            Assert.Equal(expected.Width, node.Width); Assert.Equal(expected.Height, node.Height);
        }
        Assert.Equal("Source", source.Title); Assert.Equal("Source detail", source.Subtitle);
        Assert.Equal("Focused network", view.Title); Assert.Equal("Selected services", options.View!.Subtitle);
    }

    private static TopologyChart Diagram() => TopologyChart.Create().WithId("network").WithViewport(480, 300, 20).WithLegend(null)
        .AddNode("left", "Left", 20, 20, width: 100, height: 64)
        .AddNode("right", "Right", 300, 20, width: 100, height: 64)
        .AddEdge("link", "left", "right", routing: TopologyEdgeRouting.Straight);
}
