using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StackedBarCornersTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SignedStacksKeepSquareJoinsAndRoundedOuterEndsInNativePngAndSvg(bool horizontal, bool dark) {
        var red = ChartColor.FromHex("#DE4239"); var blue = ChartColor.FromHex("#2877D7");
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithBarVisualStyle(ChartBarVisualStyle.Flat().WithCornerRadius(10)).WithStackedBars().WithMaximumBarWidth(50);
        var first = new[] { new ChartPoint(1, 3), new ChartPoint(2, -3) };
        var second = new[] { new ChartPoint(1, 2), new ChartPoint(2, -2) };
        if (horizontal) chart.AddHorizontalBar("First", first, red).AddHorizontalBar("Second", second, blue);
        else chart.AddBar("First", first, red).AddBar("Second", second, blue);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == (horizontal ? "horizontal-bar" : "bar")).ToArray();
        Assert.Equal(4, bars.Length);
        Assert.All(bars, bar => Assert.Equal(0, bar.Radius));
        var envelopes = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.PathClip != null).ToArray();
        Assert.Equal(4, envelopes.Length);
        Assert.All(envelopes, envelope => Assert.Contains(envelope.PathClip!.Commands, command => command.Kind == ChartPathCommandKind.CubicTo));
        var svg = System.Xml.Linq.XDocument.Parse(prepared.ToSvg());
        Assert.All(svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == (horizontal ? "horizontal-bar" : "bar")), node => Assert.Equal("0", (string?)node.Attribute("rx")));
        var image = PngReader.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        for (var category = 0; category < 2; category++) {
            var a = bars[category].Bounds; var b = bars[category + 2].Bounds;
            // Near both sides of each internal boundary, neither segment loses its corner to a notch.
            foreach (var bar in new[] { (Bounds: a, Color: red), (Bounds: b, Color: blue) }) {
                var join = horizontal ? (a.Left < b.Left ? a.Right : a.Left) : (a.Top < b.Top ? a.Bottom : a.Top);
                var inward = horizontal ? (bar.Bounds.Left < join ? -2 : 2) : (bar.Bounds.Top < join ? -2 : 2);
                var x = horizontal ? join + inward : bar.Bounds.Left + 2;
                var y = horizontal ? bar.Bounds.Top + 2 : join + inward;
                var offset = ((int)y * image.Width + (int)x) * 4;
                Assert.InRange(Math.Abs(image.Pixels[offset] - bar.Color.R), 0, 3);
                Assert.InRange(Math.Abs(image.Pixels[offset + 1] - bar.Color.G), 0, 3);
                Assert.InRange(Math.Abs(image.Pixels[offset + 2] - bar.Color.B), 0, 3);
            }
        }
    }
}
