using System;
using System.IO;
using System.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void GraphSceneStagesRenderDeterministicStaticImages() {
        var image = "data:image/svg+xml;charset=utf-8,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'%3E%3Crect width='24' height='24' rx='5' fill='%23ef4444'/%3E%3C/svg%3E";
        var scene = GraphScene.Create("staged-estate", "Staged estate")
            .AddNode("estate", "Estate", node => { node.Shape = GraphNodeShape.Image; node.ImageUrl = image; node.Size = 24; })
            .AddNode("region", "Region", node => node.ParentId = "estate")
            .AddNode("site", "Site", node => node.ParentId = "region")
            .AddNode("service", "Service", node => node.ParentId = "site")
            .AddEdge("estate-region", "estate", "region", configure: edge => edge.Directed = true)
            .AddEdge("region-site", "region", "site", configure: edge => edge.Directed = true)
            .AddEdge("site-service", "site", "service", configure: edge => edge.Directed = true);
        scene.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        var stages = scene.CreateStages(options => {
            options.Depths.AddRange(new[] { 0, 1, 2, 5 });
            options.IncludeFullScene = true;
        });

        Assert(stages.Count == 4 && stages.Select(stage => stage.Depth).SequenceEqual(new[] { 0, 1, 2, 3 }), "Explicit graph stage depths should be deterministic, clamp beyond the deepest hierarchy, and retain the full view.");
        Assert(stages[0].VisibleNodeIds.SequenceEqual(new[] { "estate" }) && stages[0].FrontierNodeIds.SequenceEqual(new[] { "estate" }) && stages[0].HiddenNodeCount == 3, "Overview stages should retain top-level nodes as summaries of hidden descendants.");
        Assert(stages[3].IsFullScene && stages[3].VisibleNodeIds.Count == 4 && stages[3].VisibleEdgeIds.Count == 3, "The final stage should contain the full selected hierarchy and all in-scope relationships.");

        var svg = scene.ToGraphSvg(stages[0], options => { options.Width = 1200; options.Height = 675; });
        var mark = svg.IndexOf("data-cfx-role=\"graph-node\"", StringComparison.Ordinal);
        var details = svg.IndexOf("data-cfx-role=\"graph-node-details-layer\"", StringComparison.Ordinal);
        Assert(svg.StartsWith("<svg", StringComparison.Ordinal) && svg.Contains("width=\"1200\" height=\"675\"", StringComparison.Ordinal) && !svg.Contains("<script", StringComparison.OrdinalIgnoreCase), "Static graph SVG should be a complete, script-free image with caller-selected dimensions.");
        Assert(mark >= 0 && details > mark && svg.Contains(">+3</text>", StringComparison.Ordinal), "Static hierarchy stages should paint all node details above image marks and badge frontier nodes with hidden descendant counts.");
        Assert(!svg.Contains("data-node-id=\"region\"", StringComparison.Ordinal), "Static overview stages should omit deeper nodes instead of relying on interactive CSS visibility.");

        var png = scene.ToGraphPng(stages[1], options => { options.Width = 640; options.Height = 360; });
        Assert(png.Length > 8 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "Static graph stages should rasterize to dependency-free PNG bytes.");
        var decoded = RasterImageDecoder.Decode(png);
        Assert(Enumerable.Range(0, decoded.Pixels.Length / 4).Any(index => decoded.Pixels[index * 4] > 210 && decoded.Pixels[index * 4 + 1] < 110 && decoded.Pixels[index * 4 + 2] < 110 && decoded.Pixels[index * 4 + 3] > 180), "Dependency-free PNG export should preserve self-contained SVG image nodes.");
        Assert(Enumerable.Range(0, decoded.Pixels.Length / 4).All(index => decoded.Pixels[index * 4 + 3] == 255), "Static graph PNG stages should composite transparent letterboxing onto an opaque report-safe background.");

        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-stage-export-" + Guid.NewGuid().ToString("N"));
        try {
            var files = scene.SaveGraphStageImages(output, "estate", options => {
                options.Stages.StageCount = 3;
                options.Formats = GraphSceneStaticImageFormat.Both;
                options.Render.Width = 640;
                options.Render.Height = 360;
            });
            Assert(files.Count == 3 && files.All(file => File.Exists(file.SvgPath!) && File.Exists(file.PngPath!)), "Stage image export should write the requested deterministic SVG and PNG series.");
            Assert(Path.GetFileName(files[0].PngPath) == "estate-01-overview.png" && Path.GetFileName(files[2].PngPath) == "estate-03-full.png", "Stage image files should use stable ordered names suitable for reports and C# automation.");
        } finally {
            if (Directory.Exists(output)) Directory.Delete(output, true);
        }
    }
}
