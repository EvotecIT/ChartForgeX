using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ScalarBarCapacityTests {
    private static readonly double[] Categories = { 0, 1, 1.000001, 2 };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NearAdjacentNumericCategoriesRetainMinimumThicknessAndBothPublicExports(bool horizontal) {
        var chart = Chart.Create().WithSize(480, 300).WithBarStyle(ChartBarStyle.Flat);
        Add(chart, horizontal, "Observations", 3);

        var document = XDocument.Parse(chart.ToSvg());
        var groups = document.Descendants().Where(element => Role(element) == "point").ToArray();
        Assert.Equal(Categories.Length, groups.Length);
        for (var point = 0; point < Categories.Length; point++) {
            Assert.Equal(Categories[point], Number(groups[point], "data-cfx-x"));
            Assert.Equal(point, Number(groups[point], "data-cfx-point"));
            var bar = Assert.Single(groups[point].Descendants(), element => Role(element) == (horizontal ? "horizontal-bar" : "bar"));
            Assert.Equal(.1, Number(bar, horizontal ? "height" : "width"), 8);
        }

        var image = PngReader.Decode(chart.ToPng());
        Assert.Equal(480, image.Width); Assert.Equal(300, image.Height);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void DenseGroupedAndStackedBarsKeepSlotOwnershipAndTotals(bool horizontal, bool stacked) {
        var chart = Chart.Create().WithHeader(false).WithAxes(false).WithLegend(false).WithDataLabels(false)
            .WithBarStyle(ChartBarStyle.Flat);
        Add(chart, horizontal, "First", 2); Add(chart, horizontal, "Second", 5);
        var categoryAxis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        var valueAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        categoryAxis.WithBounds(-.5, 2.5); valueAxis.WithBounds(0, 20);
        if (stacked) { chart.WithStackedBars(); chart.Options.ShowStackTotals = true; }
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(480, 300), 0),
            frame: new VisualFrame(showLegend: false, showSurface: false, transparentBackground: true)));
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(Categories.Length * 2, groups.Length);
        for (var point = 0; point < Categories.Length; point++) {
            var first = Bounds(0, point); var second = Bounds(1, point);
            Assert.Equal(.1, horizontal ? first.Height : first.Width, 8);
            Assert.Equal(.1, horizontal ? second.Height : second.Width, 8);
            var category = horizontal ? (Categories[point] + .5) * 100 : (Categories[point] + .5) * 160;
            if (stacked) {
                Assert.Equal(Center(first), Center(second), 8);
                Assert.Equal(category, Center(first), 8);
                Assert.Equal(horizontal ? first.Right : first.Top, horizontal ? second.Left : second.Bottom, 8);
            } else {
                Assert.True(Center(first) < Center(second));
                Assert.Equal(category, (Center(first) + Center(second)) / 2, 8);
            }
        }
        var totals = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "stack-total").ToArray();
        Assert.Equal(stacked ? Categories.Length : 0, totals.Length);
        for (var point = 0; point < totals.Length; point++) {
            Assert.Equal(Categories[point], double.Parse(totals[point].Metadata["data-cfx-x"], CultureInfo.InvariantCulture));
            Assert.Equal((7 + 2 * point).ToString(CultureInfo.InvariantCulture), totals[point].Metadata["data-cfx-y"]);
        }

        ChartRect Bounds(int series, int point) => prepared.Regions.Single(region => region.Role == "point"
            && region.Id == "series-" + series + "-point-" + point).Bounds;
        double Center(ChartRect bounds) => horizontal ? bounds.Top + bounds.Height / 2 : bounds.Left + bounds.Width / 2;
    }

    private static void Add(Chart chart, bool horizontal, string name, double initialValue) {
        var points = Categories.Select((category, point) => new ChartPoint(category, initialValue + point));
        if (horizontal) chart.AddHorizontalBar(name, points); else chart.AddBar(name, points);
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
