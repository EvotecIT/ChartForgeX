using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Natural exports can use outer padding without losing route strokes at the inner plot edge.</summary>
public sealed class TopologyNaturalRouteInkTests {
    [Theory]
    [InlineData(4.4, false)]
    [InlineData(80, true)]
    public void NativeClipContainsFullInkAndGrowsOnlyBeyondAvailablePadding(double strokeWidth, bool grows) {
        var chart = TopologyChart.Create().WithViewport(640, 300, 24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("a", "Source", 120, 100, width: 80, height: 40)
            .AddNode("b", "Target", 400, 100, width: 80, height: 40)
            .AddEdge("e", "a", "b", direction: VisualLinkDirection.None, routing: TopologyEdgeRouting.Orthogonal)
            .WithEdgeWaypoints("e", new ChartPoint(24, 120), new ChartPoint(24, 220), new ChartPoint(400, 220))
            .WithEdgeStroke("e", strokeWidth);
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeEdgeLabels = false });
        if (grows) Assert.True(prepared.Width > 640);
        else Assert.Equal(640, prepared.Width);
        Assert.Equal(Math.Ceiling(prepared.Width), prepared.Width);
        var xml = XDocument.Parse(prepared.ToSvg());
        var edge = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge");
        var reference = (string)edge.Ancestors().First(element => element.Attribute("clip-path") != null).Attribute("clip-path")!;
        var clip = xml.Descendants().Single(element => element.Name.LocalName == "clipPath"
            && (string?)element.Attribute("id") == reference.Substring(5, reference.Length - 6)).Elements().Single();
        var left = Number(clip, "x");
        var right = left + Number(clip, "width");
        var paths = edge.Descendants().Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("topology-edge-line", StringComparison.Ordinal) == true).ToArray();
        Assert.NotEmpty(paths);
        Assert.All(paths, path => {
            var halfStroke = Number(path, "stroke-width") / 2;
            var coordinates = Regex.Matches((string)path.Attribute("d")!, @"-?\d+(?:\.\d+)?")
                .Cast<Match>().Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
            foreach (var x in coordinates.Where((_, index) => index % 2 == 0)) {
                Assert.InRange(x - halfStroke, left, right);
                Assert.InRange(x + halfStroke, left, right);
            }
        });
        var raster = ChartForgeX.Raster.PngReader.Decode(prepared.ToPng());
        Assert.Equal((int)prepared.Width, raster.Width);

        static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
    }
}
