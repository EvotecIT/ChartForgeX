using System;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Controls script-free SVG and PNG graph output.</summary>
public sealed class GraphSceneStaticRenderOptions {
    /// <summary>Gets or sets the output width in pixels.</summary>
    public int Width { get; set; } = 1600;

    /// <summary>Gets or sets the output height in pixels.</summary>
    public int Height { get; set; } = 900;

    /// <summary>Gets or sets the maximum number of node labels painted into a static image. Important summaries and high-degree nodes are selected deterministically.</summary>
    public int MaximumNodeLabels { get; set; } = 240;

    internal void Validate() {
        if (Width < 64 || Width > 16384) throw new InvalidOperationException("Static graph width must be between 64 and 16384 pixels.");
        if (Height < 64 || Height > 16384) throw new InvalidOperationException("Static graph height must be between 64 and 16384 pixels.");
        if (MaximumNodeLabels < 0 || MaximumNodeLabels > 10000) throw new InvalidOperationException("Static graph maximum node labels must be between 0 and 10000.");
    }
}

/// <summary>Controls a set of deterministic graph stage image files.</summary>
public sealed class GraphSceneStageImageOptions {
    /// <summary>Gets hierarchy stage planning options.</summary>
    public GraphSceneStageOptions Stages { get; } = new();

    /// <summary>Gets static image rendering options.</summary>
    public GraphSceneStaticRenderOptions Render { get; } = new();

    /// <summary>Gets or sets the output image formats.</summary>
    public GraphSceneStaticImageFormat Formats { get; set; } = GraphSceneStaticImageFormat.Png;

    internal void Validate() {
        Render.Validate();
        if (Formats == GraphSceneStaticImageFormat.None || (Formats & ~GraphSceneStaticImageFormat.Both) != 0) throw new InvalidOperationException("At least one supported static graph image format is required.");
    }
}

/// <summary>Names dependency-free static graph image formats.</summary>
[Flags]
public enum GraphSceneStaticImageFormat {
    /// <summary>Do not write an image.</summary>
    None = 0,

    /// <summary>Write script-free scalable vector graphics.</summary>
    Svg = 1,

    /// <summary>Write dependency-free portable network graphics.</summary>
    Png = 2,

    /// <summary>Write both SVG and PNG files.</summary>
    Both = Svg | Png
}

/// <summary>Reports the files written for one deterministic graph stage.</summary>
public sealed class GraphSceneStageImageResult {
    internal GraphSceneStageImageResult(GraphSceneStage stage, string? svgPath, string? pngPath) {
        Stage = stage;
        SvgPath = svgPath;
        PngPath = pngPath;
    }

    /// <summary>Gets the stage represented by the written files.</summary>
    public GraphSceneStage Stage { get; }

    /// <summary>Gets the SVG file path when SVG output was requested.</summary>
    public string? SvgPath { get; }

    /// <summary>Gets the PNG file path when PNG output was requested.</summary>
    public string? PngPath { get; }
}
