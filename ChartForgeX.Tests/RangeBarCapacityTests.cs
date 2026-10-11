using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RangeBarCapacityTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DenseFlatGroupsRejectOversizedStrokesAndKeepAllObservationsWhenTheStrokeFits(bool horizontal) {
        var chart = Chart.Create().WithHeader(false).WithAxes(false).WithLegend(false).WithDataLabels(false).WithBarStyle(ChartBarStyle.Flat)
            .ConfigureRangeBar(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
        for (var series = 0; series < 2; series++) {
            chart.AddRangeBar("Group " + series, Enumerable.Range(1, 30).Select(category => new ChartInterval(category, 10, 20)));
            chart.Series[series].StrokeWidth = 8;
        }
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(horizontal ? 800 : 400, horizontal ? 400 : 800), 0));
        Assert.Equal("The category slot cannot fit the mark's minimum painted footprint. Increase category-axis space, reduce grouped series, or use a simpler mark style.",
            Assert.Throws<InvalidOperationException>(() => chart.Prepare(context)).Message);
        foreach (var series in chart.Series) series.StrokeWidth = 2;
        var prepared = chart.Prepare(context); var document = XDocument.Parse(prepared.ToSvg());
        var groups = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(60, groups.Length); Assert.Equal(60, prepared.Regions.Count(region => region.Role == "point"));
        var footprints = groups.Select(group => {
            var body = group.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "range-bar");
            var minimum = Number(body, horizontal ? "y" : "x"); var maximum = minimum + Number(body, horizontal ? "height" : "width");
            var caps = group.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "range-bar-cap").ToArray();
            Assert.Equal(2, caps.Length);
            foreach (var cap in caps) {
                var radius = Number(cap, "stroke-width") / 2;
                minimum = Math.Min(minimum, Math.Min(Number(cap, horizontal ? "y1" : "x1"), Number(cap, horizontal ? "y2" : "x2")) - radius);
                maximum = Math.Max(maximum, Math.Max(Number(cap, horizontal ? "y1" : "x1"), Number(cap, horizontal ? "y2" : "x2")) + radius);
            }
            var clippedAncestors = group.Ancestors().Where(node => node.Attribute("clip-path") != null).ToArray();
            Assert.NotEmpty(clippedAncestors);
            foreach (var ancestor in clippedAncestors) {
                var reference = ancestor.Attribute("clip-path")!.Value; var id = reference.Substring(5, reference.Length - 6);
                var clip = document.Descendants().Single(node => node.Name.LocalName == "clipPath" && (string?)node.Attribute("id") == id).Elements().Single();
                Assert.Equal("rect", clip.Name.LocalName);
                var start = Number(clip, horizontal ? "y" : "x"); var end = start + Number(clip, horizontal ? "height" : "width");
                Assert.True(minimum >= start - .003 && maximum <= end + .003, "All category paint must survive the actual plot clip.");
            }
            return (Minimum: minimum, Maximum: maximum);
        }).OrderBy(bounds => bounds.Minimum).ToArray();
        for (var item = 1; item < footprints.Length; item++) Assert.True(footprints[item - 1].Maximum < footprints[item].Minimum);
    }

    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
