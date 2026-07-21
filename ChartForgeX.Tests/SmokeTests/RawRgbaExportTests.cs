using System.Linq;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void RawRgbaRenderingAvoidsEncodedRoundTrips() {
        var chart = SampleChart();
        AssertRgbaParity(chart.ToRgbaImage(), chart.ToPng(), "Chart");

        var chartGrid = ChartGrid.Create()
            .WithPanelSize(180, 120)
            .Add(Chart.Create().WithSize(180, 120).AddLine("Values", Points(1, 2, 3)));
        AssertRgbaParity(chartGrid.ToRgbaImage(), chartGrid.ToPng(), "ChartGrid");

        var metric = MetricCard.Create().WithSize(180, 100).WithMetric("CPU", "42%");
        AssertRgbaParity(metric.ToRgbaImage(), metric.ToPng(), "IVisualBlock");

        var visualGrid = VisualGrid.CreateMetricStrip("Endpoint", new[] { metric });
        AssertRgbaParity(visualGrid.ToRgbaImage(), visualGrid.ToPng(), "VisualGrid");

        var canvas = VisualCanvas.Create(240, 120)
            .WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddText(20, 32, 200, "Direct RGBA", 20, ChartColor.FromHex("#60A5FA"));
        AssertRgbaParity(canvas.ToRgbaImage(), canvas.ToPng(), "VisualCanvas");

        var topology = CreateSampleTopologyChart();
        var topologyOptions = new TopologyRenderOptions { IncludeLegend = false };
        AssertRgbaParity(topology.ToRgbaImage(topologyOptions), topology.ToPng(topologyOptions), "TopologyChart");
    }

    private static void AssertRgbaParity(RgbaImage direct, byte[] png, string surface) {
        var decoded = RasterImageDecoder.Decode(png);
        Assert(direct.Width == decoded.Width && direct.Height == decoded.Height, surface + " direct RGBA dimensions should match PNG rendering.");
        Assert(direct.Pixels.Length == checked(direct.Width * direct.Height * 4), surface + " direct RGBA output should expose a complete pixel buffer.");
        Assert(direct.Pixels.SequenceEqual(decoded.Pixels), surface + " direct RGBA pixels should match lossless PNG rendering.");
    }
}
