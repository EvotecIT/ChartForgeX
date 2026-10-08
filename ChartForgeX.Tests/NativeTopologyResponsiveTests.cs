using System.Xml.Linq;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyResponsiveTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResponsivePolicySurvivesConveniencePreparationFittingAndMotion(bool responsive) {
        var chart = TopologyChart.Create().WithViewport(1200, 360, 20).WithLegend(null)
            .AddNode("source", "Source", 40, 100, width: 100, height: 60)
            .AddNode("target", "Target", 900, 100, width: 100, height: 60)
            .AddEdge("route", "source", "target");
        var options = new TopologyRenderOptions { IncludeLegend = false, UseResponsiveSvg = responsive };
        var expected = responsive ? "max-width:100%;height:auto;display:block" : null;
        Assert.Equal(expected, (string?)XDocument.Parse(chart.ToSvg(options)).Root!.Attribute("style"));
        var prepared = chart.Prepare(options);
        Assert.Equal(expected, (string?)XDocument.Parse(prepared.ToSvg()).Root!.Attribute("style"));
        var fitted = XDocument.Parse(prepared.WithOutputSize(600, 200).ToSvg());
        Assert.Equal(expected, (string?)fitted.Root!.Attribute("style"));
        Assert.Equal(600, (double)fitted.Root.Attribute("width")!);
        options.Motion = TopologyMotionOptions.RoutePulseForEdges("route");
        var animated = XDocument.Parse(chart.ToSvg(options));
        Assert.Equal(expected, (string?)animated.Root!.Attribute("style"));
        Assert.Contains(animated.Descendants(), element => element.Name.LocalName == "animateMotion");
    }
}
