using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ShortCategoryTickTests {
    private static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    [Theory]
    [InlineData(240, false)]
    [InlineData(320, false)]
    [InlineData(390, false)]
    [InlineData(1120, false)]
    [InlineData(320, true)]
    public void NarrowCategoryAxesKeepWholeShortCaptionsAndBothEndpoints(int width, bool horizontal) {
        var chart = Chart.Create().WithSize(width, 280).WithHeader(false).WithLegend(false).WithDataLabels(false).WithXLabels(Months);
        var points = Enumerable.Range(1, 12).Select(index => new ChartPoint(index, index % 4 + 1)).ToArray();
        if (horizontal) chart.AddHorizontalBar("Count", points).WithXAxisIntegerTicks();
        else chart.AddBar("Count", points).WithYAxisIntegerTicks();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var role = horizontal ? "axis-y-label" : "axis-x-label";
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role).ToArray();
        var texts = captions.Select(caption => string.Join("", caption.Text.Lines.Select(line => line.Text))).ToArray();
        Assert.Contains("Jan", texts); Assert.Contains("Dec", texts);
        Assert.All(texts, text => Assert.Contains(text, Months));
        if (!horizontal && width <= 320) Assert.InRange(captions.Length, 2, 11);
        var ordered = captions.OrderBy(caption => horizontal ? caption.Baseline : caption.X).ToArray();
        for (var index = 1; index < ordered.Length; index++) {
            var before = ordered[index - 1]; var after = ordered[index];
            Assert.True(horizontal ? after.Baseline - after.Text.Ascent >= before.Baseline - before.Text.Ascent + before.Text.Metrics.Height
                : after.X >= before.X + before.Text.Metrics.Width, "Thinning leaves complete captions on their original ticks without overlap.");
        }
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(texts, svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role).Select(node => node.Value).ToArray());
        Assert.Equal(12, prepared.Regions.Count(region => region.Role == role));
    }
}
