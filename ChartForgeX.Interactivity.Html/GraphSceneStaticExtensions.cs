using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Provides script-free SVG and PNG export helpers for graph scenes and hierarchy stages.</summary>
public static class GraphSceneStaticExtensions {
    /// <summary>Renders a graph scene or planned stage to script-free SVG.</summary>
    public static string ToGraphSvg(this GraphScene scene, GraphSceneStage? stage = null, Action<GraphSceneStaticRenderOptions>? configure = null) => new HtmlGraphExplorerRenderer().RenderStaticSvg(scene, stage, configure);

    /// <summary>Renders a graph scene or planned stage to PNG bytes without opening a browser.</summary>
    public static byte[] ToGraphPng(this GraphScene scene, GraphSceneStage? stage = null, Action<GraphSceneStaticRenderOptions>? configure = null) => new HtmlGraphExplorerRenderer().RenderStaticPng(scene, stage, configure);

    /// <summary>Saves deterministic overview-to-detail graph stage images without creating interactive HTML.</summary>
    public static IReadOnlyList<GraphSceneStageImageResult> SaveGraphStageImages(this GraphScene scene, string outputDirectory, string fileNamePrefix, Action<GraphSceneStageImageOptions>? configure = null) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
        if (string.IsNullOrWhiteSpace(fileNamePrefix)) throw new ArgumentException("A file name prefix is required.", nameof(fileNamePrefix));
        var options = new GraphSceneStageImageOptions();
        configure?.Invoke(options);
        options.Validate();
        Directory.CreateDirectory(outputDirectory);
        var stages = scene.CreateStages(stageOptions => CopyStageOptions(options.Stages, stageOptions));
        var renderer = new HtmlGraphExplorerRenderer();
        var results = new List<GraphSceneStageImageResult>(stages.Count);
        var prefix = SafeFileName(fileNamePrefix);
        foreach (var stage in stages) {
            var baseName = prefix + "-" + stage.Index.ToString("00", CultureInfo.InvariantCulture) + "-" + stage.Name;
            string? svgPath = null;
            string? pngPath = null;
            if ((options.Formats & GraphSceneStaticImageFormat.Svg) != 0) {
                svgPath = Path.GetFullPath(Path.Combine(outputDirectory, baseName + ".svg"));
                File.WriteAllText(svgPath, renderer.RenderStaticSvg(scene, stage, render => CopyRenderOptions(options.Render, render)));
            }
            if ((options.Formats & GraphSceneStaticImageFormat.Png) != 0) {
                pngPath = Path.GetFullPath(Path.Combine(outputDirectory, baseName + ".png"));
                File.WriteAllBytes(pngPath, renderer.RenderStaticPng(scene, stage, render => CopyRenderOptions(options.Render, render)));
            }
            results.Add(new GraphSceneStageImageResult(stage, svgPath, pngPath));
        }
        return results;
    }

    private static void CopyStageOptions(GraphSceneStageOptions source, GraphSceneStageOptions target) {
        target.StageCount = source.StageCount;
        target.RootNodeId = source.RootNodeId;
        target.IncludeFullScene = source.IncludeFullScene;
        target.Depths.AddRange(source.Depths);
    }

    private static void CopyRenderOptions(GraphSceneStaticRenderOptions source, GraphSceneStaticRenderOptions target) {
        target.Width = source.Width;
        target.Height = source.Height;
        target.MaximumNodeLabels = source.MaximumNodeLabels;
    }

    private static string SafeFileName(string value) {
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
        var characters = value.Trim().ToCharArray();
        for (var index = 0; index < characters.Length; index++) if (invalid.Contains(characters[index])) characters[index] = '-';
        return new string(characters);
    }
}
