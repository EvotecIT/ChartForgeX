using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class EventModelingLaneHeaderCausalRegressionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingSwimlaneFitsTheFullNamespaceHeadingIndependentlyOfFrameOrderConversion(bool longNamespace) {
        var name = longNamespace ? string.Concat(Enumerable.Repeat("Operations", 20)) : "Orders";
        var source = "eventmodeling\ntf 01 ui " + name + ".X";
        var parsed = new MermaidParser().ParseEventModeling(source); Assert.False(parsed.HasErrors);
        // Explicitly select the existing native owner so this measures lane sizing even
        // before the Event Modeling converter changes from DenseGrouped to Swimlane.
        var chart = parsed.Document!.ToTopologyChart(new MermaidTopologyRenderOptions { Width = 800, Height = 400 })
            .WithLayout(TopologyLayoutMode.Swimlane, TopologyLayoutDirection.LeftToRight);
        var expected = Assert.Single(chart.Groups).Label;
        var svg = chart.ToSvg(); var text = XDocument.Parse(svg).Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var path = Path.Combine(directory, longNamespace ? "event-lane-long-before" : "event-lane-control-before");
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", chart.ToPng());
        }
        Assert.Contains(expected, text);
    }
}
