using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianTinySegmentTests {
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, -1)]
    [InlineData(true, 1)]
    [InlineData(true, -1)]
    public void NonzeroCapsulesRetainColoredInkWithoutInventingZeroMarks(bool horizontal, int sign) {
        var chart = Tiny(horizontal, sign);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var role = horizontal ? "horizontal-bar" : "bar";
        var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == role);
        Assert.Equal(1, horizontal ? mark.Bounds.Width : mark.Bounds.Height);
        Assert.Single(prepared.Regions, region => region.Role == "point");
        var source = XDocument.Parse(prepared.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "point");
        Assert.Equal(sign.ToString(CultureInfo.InvariantCulture), (string?)source.Attribute("data-cfx-y"));
        var cap = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == role + "-cap");
        Assert.Equal(1, cap.StrokeWidth);
        Assert.True(BluePixels(prepared.ToRgba().Pixels) >= 10, "The tiny source value must leave visible colored native pixels at ordinary output density.");
        Assert.True(prepared.ToPng().Length > 64);

        var zero = Tiny(horizontal, 0);
        var empty = zero.Prepare(VisualExportRequest.ForChart(zero).Context);
        var zeroMark = Assert.Single(empty.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == role);
        Assert.Equal(0, horizontal ? zeroMark.Bounds.Width : zeroMark.Bounds.Height);
        Assert.DoesNotContain(empty.Scene.Nodes, node => node.Role == role + "-cap");
        Assert.Equal(0, BluePixels(empty.ToRgba().Pixels));
    }

    private static Chart Tiny(bool horizontal, int value) {
        var chart = Chart.Create().WithTheme(ChartTheme.Light()).WithSize(220, 140).WithPadding(12, 12, 12, 12)
            .WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithCard(false)
            .WithBarStyle(ChartBarStyle.SegmentedCapsule);
        chart.Options.ShowPlotBackground = false;
        if (horizontal) chart.WithXAxisBounds(-100000, 100000).AddHorizontalBar("Tiny", new[] { new ChartPoint(1, value) }, ChartColor.FromHex("#2563EB"));
        else chart.WithYAxisBounds(-100000, 100000).AddBar("Tiny", new[] { new ChartPoint(1, value) }, ChartColor.FromHex("#2563EB"));
        return chart;
    }

    private static int BluePixels(byte[] pixels) {
        var count = 0;
        for (var index = 0; index < pixels.Length; index += 4)
            if (pixels[index + 2] > pixels[index] + 30 && pixels[index + 2] > pixels[index + 1] + 20 && pixels[index + 3] > 0) count++;
        return count;
    }
}
