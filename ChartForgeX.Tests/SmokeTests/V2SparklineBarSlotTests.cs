using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2SparklineBarSlotTests {
    public static IEnumerable<object[]> Slots() {
        yield return new object[] { new double?[] { 2, 2, 2 } };
        yield return new object[] { new double?[] { null, 2, 2 } };
        yield return new object[] { new double?[] { 2, null, 2 } };
        yield return new object[] { new double?[] { 2, 2, null } };
        yield return new object[] { new double?[] { null, 2, null } };
        yield return new object[] { new double?[] { null, null, null } };
        yield return new object[] { new double?[] { 2 } };
        yield return new object[] { new double?[] { null } };
    }

    [Theory]
    [MemberData(nameof(Slots))]
    public void PreparedBarsKeepCompleteEqualWidthSlotsInSvgAndNativeRaster(double?[] slots) {
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(300, 100), 0),
            frame: new VisualFrame(showLegend: false, showSurface: false, transparentBackground: true));
        var data = new SparklineData(slots, 0, 4);
        var visual = new Sparkline(data).WithStyle(SparklineStyle.Bars).Prepare(context);
        var observed = Enumerable.Range(0, slots.Length).Where(index => slots[index].HasValue).ToArray();
        var bars = XDocument.Parse(visual.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "bar").ToArray();
        Assert.Equal(observed.Length, bars.Length);
        Assert.Equal(observed.Length, visual.Regions.Count(region => region.Role == "point"));
        var slotWidth = 300d / slots.Length;
        for (var index = 0; index < bars.Length; index++) {
            var x = Number(bars[index], "x"); var width = Number(bars[index], "width");
            Assert.Equal(slotWidth * .68, width, 3);
            Assert.Equal(slotWidth * (observed[index] + .16), x, 3);
            Assert.InRange(x, 0, 300); Assert.InRange(x + width, 0, 300);
        }
        var raster = visual.ToRgba(new VisualRenderOptions(scale: 2, supersampling: 1));
        var spans = InkSpans(raster, 150);
        Assert.Equal(observed.Length, spans.Length);
        for (var index = 0; index < spans.Length; index++) {
            Assert.Equal((int)Math.Round(slotWidth * .68 * 2), spans[index].Width);
            Assert.Equal((int)Math.Round(slotWidth * (observed[index] + .16) * 2), spans[index].Left);
        }
        if (observed.Length == 0) {
            Assert.Empty(data.ToPoints());
            Assert.Contains(visual.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
        }
        Assert.NotEmpty(visual.ToPng());
    }

    [Fact]
    public void VeryDenseSparseSlotsDoNotGrowToTheOrdinaryChartMinimumBarWidth() {
        var slots = new double?[10000]; slots[0] = 2; slots[slots.Length - 1] = 2;
        var visual = new Sparkline(new SparklineData(slots, 0, 4)).WithStyle(SparklineStyle.Bars)
            .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(100, 100), 0),
                frame: new VisualFrame(showLegend: false, showSurface: false, transparentBackground: true)));
        var regions = visual.Regions.Where(region => region.Role == "point").ToArray();
        Assert.Equal(2, regions.Length);
        Assert.All(regions, region => {
            Assert.Equal(.0068, region.Bounds.Width, 9);
            Assert.InRange(region.Bounds.Left, 0, 100); Assert.InRange(region.Bounds.Right, 0, 100);
        });
    }

    [Theory]
    [MemberData(nameof(Slots))]
    public void CanvasBarEmbeddingAlsoReservesMissingSlotsWithoutMovingNeighbours(double?[] slots) {
        var tile = new VisualCanvasInfoTileLayer(20, 20, 500, 150, "CPU", "Load", "2")
            .WithSparkline(new SparklineData(slots, 0, 4), SparklineStyle.Bars);
        var canvas = VisualCanvas.Create(560, 210).AddLayer(tile);
        XElement MiniChart() => Assert.Single(XDocument.Parse(canvas.ToSvg()).Descendants(),
            element => (string?)element.Attribute("data-cfx-role") == "visual-canvas-info-tile-mini-chart");
        var sparse = MiniChart().Elements().Where(element => element.Name.LocalName == "rect").Skip(1).ToArray();
        tile.WithSparkline(new SparklineData(Enumerable.Repeat<double?>(2, slots.Length), 0, 4), SparklineStyle.Bars);
        var full = MiniChart().Elements().Where(element => element.Name.LocalName == "rect").Skip(1).ToArray();
        var observed = Enumerable.Range(0, slots.Length).Where(index => slots[index].HasValue).ToArray();
        Assert.Equal(observed.Length, sparse.Length); Assert.Equal(slots.Length, full.Length);
        for (var index = 0; index < sparse.Length; index++) {
            Assert.Equal(Number(full[observed[index]], "x"), Number(sparse[index], "x"));
            Assert.Equal(Number(full[observed[index]], "width"), Number(sparse[index], "width"));
        }
    }

    [Fact]
    public void DenseSparseCanvasBarsKeepBothEndpointMarksInsideTheirMiniChart() {
        var slots = new double?[10000]; slots[0] = 2; slots[slots.Length - 1] = 2;
        var tile = new VisualCanvasInfoTileLayer(20, 20, 500, 150, "CPU", "Load", "2")
            .WithSparkline(new SparklineData(slots, 0, 4), SparklineStyle.Bars);
        var canvas = VisualCanvas.Create(560, 210).AddLayer(tile);
        var group = Assert.Single(XDocument.Parse(canvas.ToSvg()).Descendants(),
            element => (string?)element.Attribute("data-cfx-role") == "visual-canvas-info-tile-mini-chart");
        var rectangles = group.Elements().Where(element => element.Name.LocalName == "rect").ToArray();
        Assert.Equal(3, rectangles.Length);
        var left = Number(rectangles[0], "x") + 7;
        var right = Number(rectangles[0], "x") + Number(rectangles[0], "width") - 7;
        foreach (var bar in rectangles.Skip(1)) {
            Assert.InRange(Number(bar, "x"), left - .002, right + .002);
            Assert.InRange(Number(bar, "x") + Number(bar, "width"), left - .002, right + .002);
        }
        Assert.Equal(Number(rectangles[1], "width"), Number(rectangles[2], "width"));
    }

    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);

    private static (int Left, int Width)[] InkSpans(RgbaImage image, int y) {
        var spans = new List<(int Left, int Width)>(); var start = -1;
        for (var x = 0; x <= image.Width; x++) {
            var ink = x < image.Width && image.Pixels[(y * image.Width + x) * 4 + 3] != 0;
            if (ink && start < 0) start = x;
            if (!ink && start >= 0) { spans.Add((start, x - start)); start = -1; }
        }
        return spans.ToArray();
    }
}
