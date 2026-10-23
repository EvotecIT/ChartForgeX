using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CanonicalBarAutomaticBoundsTests {
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, true)]
    public void EquivalentEndpointCategoriesKeepWholeBarsInsideAutomaticPlot(bool horizontal, bool stacked, bool upper, bool secondary) {
        var lower = 1.5; var higher = 3.5;
        var shifted = upper ? higher : lower;
        var peer = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(shifted) + 3);
        var chart = Chart.Create().WithHeader(false).WithAxes(false).WithLegend(false).WithDataLabels(false)
            .WithBarStyle(ChartBarStyle.Flat);
        if (stacked) chart.WithStackedBars();
        var first = new[] { new ChartPoint(lower, 2), new ChartPoint(higher, 3) };
        var second = new[] { new ChartPoint(upper ? lower : peer, 4), new ChartPoint(upper ? peer : higher, 5) };
        if (horizontal) chart.AddHorizontalBar("First", first).AddHorizontalBar("Second", second);
        else chart.AddBar("First", first).AddBar("Second", second);
        if (secondary) chart.Series[1].UseSecondaryValueAxis();
        var colors = new[] { ChartColor.FromHex("#235AD7"), ChartColor.FromHex("#D73582") };
        for (var i = 0; i < colors.Length; i++) chart.Series[i].Color = colors[i];
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 400), 0)));
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        var points = prepared.Regions.Where(region => region.Role == "point").ToArray();
        Assert.Equal(4, points.Length);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            File.WriteAllBytes(Path.Combine(capture, $"automatic-category-{horizontal}-{stacked}-{upper}-{secondary}.native.png"), prepared.ToPng());
        }
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var point in points) {
            var bounds = point.Bounds;
            Assert.True(horizontal ? bounds.Top >= plot.Top && bounds.Bottom <= plot.Bottom
                : bounds.Left >= plot.Left && bounds.Right <= plot.Right,
                $"Bar {point.Id}: [{bounds.Left}, {bounds.Top}, {bounds.Right}, {bounds.Bottom}], plot [{plot.Left}, {plot.Top}, {plot.Right}, {plot.Bottom}]");
            Assert.True((horizontal ? bounds.Height : bounds.Width) >= 10);
            var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Id == point.Id);
            var color = colors[int.Parse(group.Metadata["data-cfx-series"], CultureInfo.InvariantCulture)];
            foreach (var fraction in new[] { .1, .9 }) {
                var x = bounds.Left + bounds.Width * (horizontal ? .5 : fraction);
                var y = bounds.Top + bounds.Height * (horizontal ? fraction : .5);
                var offset = ((int)y * image.Width + (int)x) * 4;
                Assert.Equal(color.R, image.Pixels[offset]); Assert.Equal(color.G, image.Pixels[offset + 1]); Assert.Equal(color.B, image.Pixels[offset + 2]);
            }
        }
        var peerPoint = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), point => point.Id == $"series-1-point-{(upper ? 1 : 0)}");
        Assert.Equal(peer, double.Parse(peerPoint.Metadata["data-cfx-x"], CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DistinctSmallCategoriesKeepTheirOwnReadableAutomaticSlots(bool horizontal) {
        var chart = Chart.Create().WithHeader(false).WithAxes(false).WithLegend(false).WithDataLabels(false);
        var points = new[] { new ChartPoint(1e-8, 2), new ChartPoint(2e-8, 3) };
        if (horizontal) chart.AddHorizontalBar("Observations", points); else chart.AddBar("Observations", points);
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 400), 0)));
        var bars = prepared.Regions.Where(region => region.Role == "point").OrderBy(region => region.Id).ToArray();
        Assert.Equal(2, bars.Length);
        var separation = horizontal ? Math.Abs(bars[1].Bounds.Top - bars[0].Bounds.Top) : Math.Abs(bars[1].Bounds.Left - bars[0].Bounds.Left);
        Assert.True(separation > 100);
        Assert.All(bars, bar => Assert.True((horizontal ? bar.Bounds.Height : bar.Bounds.Width) >= 10));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MixedMarkFamiliesDoNotReduceScalarBarCategoryMargins(bool interval, bool equivalent) {
        var peer = equivalent ? BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1.5) + 3) : 2.5;
        var chart = Chart.Create().WithHeader(false).WithAxes(false).WithLegend(false).WithDataLabels(false)
            .WithBarStyle(ChartBarStyle.Flat).AddBar("Bars", new[] { new ChartPoint(1.5, 2), new ChartPoint(3.5, 3) });
        if (interval) chart.AddRangeBar("Reference", new[] { new ChartInterval(peer, 1, 4) });
        else chart.AddLollipop("Reference", new[] { new ChartPoint(peer, 4) });
        var color = ChartColor.FromHex("#235AD7"); chart.Series[0].Color = color;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(800, 400), 0)));
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        var bars = prepared.Regions.Where(region => region.Role == "point" && region.Id.StartsWith("series-0-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, bars.Length);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            File.WriteAllBytes(Path.Combine(capture, $"mixed-category-{interval}-{equivalent}.native.png"), prepared.ToPng());
        }
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var bar in bars) {
            Assert.True(bar.Bounds.Left >= plot.Left && bar.Bounds.Right <= plot.Right);
            foreach (var fraction in new[] { .1, .9 }) {
                var offset = ((int)(bar.Bounds.Top + bar.Bounds.Height / 2) * image.Width + (int)(bar.Bounds.Left + bar.Bounds.Width * fraction)) * 4;
                Assert.Equal(color.R, image.Pixels[offset]); Assert.Equal(color.G, image.Pixels[offset + 1]); Assert.Equal(color.B, image.Pixels[offset + 2]);
            }
        }
    }
}
