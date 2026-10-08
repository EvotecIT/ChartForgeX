using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Motion;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Stories;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

/// <summary>Exercises peer presentation-package entrypoints in the existing native AOT executable.</summary>
internal static class PresentationPackageSmoke {
    internal static void Run() {
        var canvas = VisualCanvas.Create(240, 160).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddInfoTile(12, 12, 216, 136, "CPU", "Processor", "42%", progress: .42,
                surfaceStyle: VisualCanvasInfoTileSurfaceStyle.Raised, miniChartKind: VisualCanvasInfoTileMiniChartKind.Bars,
                miniChartValues: new[] { 18d, 28d, 42d }, miniChartMaximum: 100);
        Require(canvas.ToSvg().Contains("visual-canvas-info-tile-mini-chart", StringComparison.Ordinal), "Visuals canvas SVG failed.");
        Require(canvas.ToPng().Length > 64, "Visuals canvas PNG failed.");
        var artifact = canvas.ToVisualArtifact("aot-canvas");
        artifact.WithWatermarks(VisualWatermark.FromText("AOT"));
        Require(artifact.ToSvg().Contains("data-cfx-role=\"watermark\"", StringComparison.Ordinal), "Visuals watermark attachment failed.");
        Require(artifact.ToPng().Length > 64, "Visuals watermark raster failed.");

        var metric = MetricCard.Create().WithMetric("Ready", "42").WithSize(240, 140);
        var grid = VisualGrid.Create().Add("metric", metric);
        var motion = VisualMotionPresentation.Create(grid, VisualMotionTimeline.Create().Fade("metric"));
        Require(motion.ToSvg().Contains("@keyframes", StringComparison.Ordinal), "Stories motion SVG failed.");
        Require(motion.ToPng().SequenceEqual(grid.ToPng()), "Stories motion completed raster differs from the static source.");
        var story = VisualStory.Create("AOT story").WithSize(480, 320);
        story.Scene("ready", "Ready").Panel("result", new VisualStoryTextSurface("Ready", emphasized: true));
        Require(story.ToSvg().Contains("Ready", StringComparison.Ordinal), "Stories scene SVG failed.");
        Require(story.ToPng().Length > 64, "Stories scene raster failed.");
        var topology = TopologyChart.Create().WithViewport(240, 160).WithLegend(null)
            .AddNode("a", "A", 20, 60, width: 60, height: 40).AddNode("b", "B", 160, 60, width: 60, height: 40)
            .AddEdge("route", "a", "b");
        var route = topology.WithMotion(TopologyMotionOptions.RoutePulseForEdges("route").WithDuration(.2).WithFrameRate(10).WithFrameLimit(2),
            new TopologyRenderOptions { IncludeLegend = false, PngSupersamplingScale = 1 });
        Require(route.ToSvg().Contains("animateMotion", StringComparison.Ordinal), "Stories topology motion SVG failed.");
        var gif = route.ToGif();
        Require(gif.Length > 64 && gif[0] == 'G' && gif[1] == 'I' && gif[2] == 'F', "Stories topology GIF failed.");
        Require(route.ToApng().Length > 64, "Stories topology APNG failed.");
        Require(ImageComposition.Create(8, 8, ChartColor.FromRgb(255, 0, 0)).ToImage().ToGif().Length > 32, "Stories composition GIF failed.");
        Console.WriteLine("Presentation packages AOT smoke passed: Visuals facts/canvas/watermarks and Stories scenes/motion/GIF/APNG.");
    }

    private static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
}
