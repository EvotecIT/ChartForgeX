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
        Assert(svg.Contains("<g class=\"cfx-graph-explorer cfx-graph-svg cfx-graph-static-export\"><title>", StringComparison.Ordinal), "Static graph exports should establish the graph CSS variable scope around markers, edges, nodes, and labels.");
        Assert(mark >= 0 && details > mark && svg.Contains(">+3</text>", StringComparison.Ordinal), "Static hierarchy stages should paint all node details above image marks and badge frontier nodes with hidden descendant counts.");
        Assert(!svg.Contains("data-node-id=\"region\"", StringComparison.Ordinal), "Static overview stages should omit deeper nodes instead of relying on interactive CSS visibility.");

        var png = scene.ToGraphPng(stages[1], options => { options.Width = 640; options.Height = 360; });
        Assert(png.Length > 8 && png[0] == 137 && png[1] == 80 && png[2] == 78 && png[3] == 71, "Static graph stages should rasterize to dependency-free PNG bytes.");
        var decoded = RasterImageDecoder.Decode(png);
        Assert(Enumerable.Range(0, decoded.Pixels.Length / 4).Any(index => decoded.Pixels[index * 4] > 210 && decoded.Pixels[index * 4 + 1] < 110 && decoded.Pixels[index * 4 + 2] < 110 && decoded.Pixels[index * 4 + 3] > 180), "Dependency-free PNG export should preserve self-contained SVG image nodes.");
        Assert(Enumerable.Range(0, decoded.Pixels.Length / 4).All(index => decoded.Pixels[index * 4 + 3] == 255), "Static graph PNG stages should composite transparent letterboxing onto an opaque report-safe background.");

        var clustered = GraphScene.Create("static-node-clusters", "Static node-side clusters")
            .AddNode("a", "A", node => node.ClusterId = "shared")
            .AddNode("b", "B", node => node.ClusterId = "shared")
            .AddCluster("shared", "Shared", Array.Empty<string>());
        var clusteredSvg = clustered.ToGraphSvg();
        Assert(clusteredSvg.Contains("data-cluster-id=\"shared\"", StringComparison.Ordinal) && clusteredSvg.Contains("data-cluster-node-ids=\"a,b\"", StringComparison.Ordinal), "Static stage projection should preserve node-side ClusterId membership without duplicated cluster member lists.");

        var grouped = GraphScene.Create("static-group-clusters", "Static group clusters")
            .AddNode("group-a", "Group A", node => node.GroupId = "platform")
            .AddNode("group-b", "Group B", node => node.GroupId = "platform");
        grouped.Options.Cluster.Mode = GraphClusterMode.ByGroup;
        var groupedSvg = grouped.ToGraphSvg();
        Assert(groupedSvg.Contains("data-cluster-id=\"group-platform\"", StringComparison.Ordinal) && groupedSvg.Contains("data-cluster-node-ids=\"group-a,group-b\"", StringComparison.Ordinal), "Static exports should materialize the same group-derived cluster hulls as the interactive renderer.");

        var leveled = GraphScene.Create("static-levels", "Static declared levels");
        for (var index = 0; index < 44; index++) leveled.AddNode("level-" + index, "Level " + index, node => node.Level = index % 4);
        leveled.AddEdge("rebased-route", "level-0", "level-3", configure: edge => {
            edge.Shape = GraphEdgeShape.Polyline;
            edge.RoutePoints.Add(new GraphScenePoint(9100, 9200));
            edge.RoutePoints.Add(new GraphScenePoint(9300, 9400));
        });
        leveled.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        var leveledSvg = leveled.ToGraphSvg();
        Assert(!leveledSvg.Contains("data-edge-route-points=", StringComparison.Ordinal) && !leveledSvg.Contains("9100", StringComparison.Ordinal), "Dense static hierarchy rebasing should discard absolute route points from the source coordinate system.");
        var rootPoint = ExtractGraphNodePoint(leveledSvg, "level-0");
        var deepPoint = ExtractGraphNodePoint(leveledSvg, "level-3");
        var rootRadius = Math.Sqrt(Math.Pow(rootPoint.X - 480, 2) + Math.Pow(rootPoint.Y - 280, 2));
        var deepRadius = Math.Sqrt(Math.Pow(deepPoint.X - 480, 2) + Math.Pow(deepPoint.Y - 280, 2));
        Assert(rootRadius < 100 && deepRadius > 150, "Dense static hierarchy bands should preserve explicit node levels even when callers do not provide parent links.");
        var leveledStages = leveled.CreateStages(options => options.Depths.AddRange(new[] { 0, 1, 3 }));
        Assert(leveledStages.Select(stage => stage.Depth).SequenceEqual(new[] { 0, 1, 3 }) && leveledStages.Select(stage => stage.VisibleNodeIds.Count).SequenceEqual(new[] { 11, 22, 44 }), "Stage planning should use explicit node levels when a hierarchy has no ParentId links instead of collapsing every requested view into one full scene.");

        var rasterParity = GraphScene.Create("static-raster-parity", "Static raster parity")
            .AddNode("left", "Left", node => { node.X = 280; node.Y = 280; node.Size = 24; node.Status = "healthy"; node.BadgeText = "42"; node.Style.LabelBackgroundColor = "#fef3c7"; })
            .AddNode("right", "Right", node => { node.X = 680; node.Y = 280; node.Size = 24; })
            .AddEdge("left-right", "left", "right", configure: edge => edge.Dashed = true);
        var rasterParitySvg = rasterParity.ToGraphSvg();
        Assert(rasterParitySvg.Contains("style=\"stroke-dasharray:8 6\"", StringComparison.Ordinal), "Static SVG should inline the default dashed-edge pattern so dependency-free PNG rendering does not depend on unsupported attribute selectors.");
        Assert(rasterParitySvg.Contains("class=\"cfx-graph-node-label-bg\"", StringComparison.Ordinal) && rasterParitySvg.Contains("fill:#fef3c7", StringComparison.Ordinal), "Static SVG should preserve explicit node label backgrounds above node marks.");
        Assert(rasterParitySvg.Contains("style=\"fill:#22c55e;stroke:var(--cfx-color-paper);stroke-width:2\"", StringComparison.Ordinal), "Static SVG should inline semantic status colors so dependency-free PNG rendering does not depend on unsupported attribute selectors.");
        Assert(rasterParitySvg.Contains("<circle r=\"8\" style=\"fill:var(--cfx-color-text);stroke:var(--cfx-color-paper);stroke-width:2\"", StringComparison.Ordinal) && rasterParitySvg.Contains("style=\"fill:var(--cfx-color-paper);stroke:none\"", StringComparison.Ordinal), "Static SVG should emit rasterizable badge fills without CSS priority tokens in color values.");
        var rasterParityPixels = RasterImageDecoder.Decode(rasterParity.ToGraphPng()).Pixels;
        Assert(Enumerable.Range(0, rasterParityPixels.Length / 4).Any(index => Math.Abs(rasterParityPixels[index * 4] - 96) <= 3 && Math.Abs(rasterParityPixels[index * 4 + 1] - 165) <= 3 && Math.Abs(rasterParityPixels[index * 4 + 2] - 250) <= 3), "Static PNG should preserve the plain default node outline color instead of dropping color-mix strokes during rasterization.");
        Assert(Enumerable.Range(0, rasterParityPixels.Length / 4).Any(index => Math.Abs(rasterParityPixels[index * 4] - 34) <= 3 && Math.Abs(rasterParityPixels[index * 4 + 1] - 197) <= 3 && Math.Abs(rasterParityPixels[index * 4 + 2] - 94) <= 3), "Static PNG should preserve healthy status-dot colors from the SVG node-detail contract.");

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
