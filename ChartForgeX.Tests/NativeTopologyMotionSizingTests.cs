using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTopologyMotionSizingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnimatedConvenienceExportsShareNaturalOrFittedCommonFrameWithoutMutatingSource(bool fit) {
        var chart = TopologyChart.Create().WithId("motion-frame").WithViewport(420, 240, 20)
            .WithTitle("Service route").WithSubtitle("A shared heading and legend surround the complete route")
            .WithLegend(TopologyLegend.Create("Route health").AddStatus("Healthy", TopologyHealthStatus.Healthy))
            .AddNode("source", "Source", 20, 80, width: 100, height: 60)
            .AddNode("target", "Target", 280, 210, width: 100, height: 60)
            .AddEdge("route", "source", "target", routing: TopologyEdgeRouting.Straight)
            .AddScenario("delivery", "Delivery", scenario => scenario.AddEdgeStep("route"));
        var options = new TopologyRenderOptions {
            FitContentToViewport = fit, PngSupersamplingScale = 1,
            LegendMode = TopologyLegendMode.Explicit
        };
        var motion = new TopologyMotionOptions {
            ScenarioId = "delivery", DurationSeconds = .2, FramesPerSecond = 10, MaximumRasterFrames = 2, Progress = .375
        };
        var presentation = chart.WithMotion(motion, options);
        var prepared = chart.Prepare(options);
        if (fit) {
            Assert.Equal(420, prepared.Width);
            Assert.Equal(240, prepared.Height);
        } else Assert.True(prepared.Height > 240, "Natural animation must reserve the shared frame around the complete resolved topology.");

        var gifBytes = presentation.ToGif();
        var apngBytes = presentation.ToApng();
        var gif = GifReader.Decode(gifBytes);
        var apng = PngReader.Decode(apngBytes);
        var png = PngReader.Decode(prepared.ToPng());
        Assert.Equal((int)Math.Ceiling(prepared.Width), gif.Width);
        Assert.Equal((int)Math.Ceiling(prepared.Height), gif.Height);
        Assert.Equal(png.Width, apng.Width);
        Assert.Equal(png.Height, apng.Height);
        Assert.Contains("NETSCAPE2.0", Encoding.ASCII.GetString(gifBytes));
        Assert.Contains("acTL", Encoding.ASCII.GetString(apngBytes));
        Assert.Contains("fdAT", Encoding.ASCII.GetString(apngBytes));

        var html = presentation.ToHtmlFragment();
        var svgStart = html.IndexOf("<svg", StringComparison.Ordinal);
        var svgEnd = html.IndexOf("</svg>", svgStart, StringComparison.Ordinal) + "</svg>".Length;
        var svg = XDocument.Parse(html.Substring(svgStart, svgEnd - svgStart));
        Assert.Equal(prepared.Width, (double)svg.Root!.Attribute("width")!, 3);
        Assert.Equal(prepared.Height, (double)svg.Root.Attribute("height")!, 3);
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "animateMotion");
        Assert.Contains(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "legend-title");
        Assert.Contains(svg.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Service route");

        Assert.Equal(420, chart.Viewport.Width); Assert.Equal(240, chart.Viewport.Height);
        Assert.Equal(20, chart.Nodes[0].X); Assert.Equal(80, chart.Nodes[0].Y);
        Assert.Equal(280, chart.Nodes[1].X); Assert.Equal(210, chart.Nodes[1].Y);
        Assert.Empty(chart.Edges.Single().Waypoints);
        Assert.Equal("Service route", chart.Title); Assert.Equal("Route health", chart.Legend!.Title);
        Assert.Equal(.375, motion.Progress);
        Assert.Equal(fit, options.FitContentToViewport);
    }
}
