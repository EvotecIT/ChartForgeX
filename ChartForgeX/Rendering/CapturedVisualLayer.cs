using System;
using System.Collections.Generic;
using ChartForgeX.Diagnostics;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>Immutable fixed-density native layer captured from one complete prepared child, with its support and source kept distinct.</summary>
/// <remarks>The source remains the editable/semantic prepared owner. Visual is a static baked image: it does not duplicate
/// individual source marks as interactive targets and does not recolor its pixels through later SVG host variables.</remarks>
public sealed partial class CapturedVisualLayer {
    private readonly IReadOnlyList<ChartForgeX.Effects.VisualMask> _postCaptureMasks;
    private readonly VisualSceneImageData _pixels;
    internal CapturedVisualLayer(PreparedVisual source, VisualSceneLayerCapturePlan plan, RgbaImage image)
        : this(source, plan, image, plan.LogicalBounds) { }

    internal CapturedVisualLayer(PreparedVisual source, VisualSceneLayerCapturePlan plan, RgbaImage image, ChartRect bounds,
        IReadOnlyList<ChartForgeX.Effects.VisualMask>? postCaptureMasks = null) {
        Source = source; SourceBounds = new ChartRect(0, 0, source.Size.Width, source.Size.Height);
        Bounds = bounds; PixelsPerUnit = plan.Options.Scale; Plan = plan;
        _pixels = new VisualSceneImageData(image);
        var masks = postCaptureMasks == null ? new ChartForgeX.Effects.VisualMask[0]
            : new ChartForgeX.Effects.VisualMask[postCaptureMasks.Count];
        for (var index = 0; index < masks.Length; index++) masks[index] = postCaptureMasks![index];
        _postCaptureMasks = Array.AsReadOnly(masks);
        var size = new VisualSize(Bounds.Width, Bounds.Height);
        var node = new VisualSceneImage(_pixels, new ChartRect(0, 0, size.Width, size.Height), "captured-native-layer", null);
        var diagnostics = new List<VisualDiagnostic>(source.Diagnostics) {
            new("effects.layer-baked", "This explicit layer uses one captured native image. Native SVG filters and arbitrary group/profile effects are not emitted.", VisualDiagnosticSeverity.Information)
        };
        var regions = new List<VisualSemanticRegion>();
        foreach (var region in source.Regions) regions.Add(new VisualSemanticRegion(region.Id, region.Role,
            new ChartRect(region.Bounds.X - Bounds.X, region.Bounds.Y - Bounds.Y, region.Bounds.Width, region.Bounds.Height), region.Label));
        Visual = new PreparedVisual(new VisualScene(size, new VisualSceneNode[] { node }, diagnostics, regions), source.Accessibility);
    }
    /// <summary>Gets the unchanged prepared source, retaining its original geometry, projections and semantic facts.</summary>
    public PreparedVisual Source { get; }
    /// <summary>Gets the original complete child viewport in its logical coordinates.</summary>
    public ChartRect SourceBounds { get; }
    /// <summary>Gets the outward-rounded source plus propagated kernel support, in original child logical coordinates.</summary>
    public ChartRect Bounds { get; }
    /// <summary>Gets the captured native device samples per logical unit. Later scaling resamples these fixed pixels.</summary>
    public int PixelsPerUnit { get; }
    /// <summary>Gets a self-contained static image scene for ordinary SVG/PNG export and composition.</summary>
    public PreparedVisual Visual { get; }
    /// <summary>Returns an independent mutable copy of the exact captured device pixels.</summary>
    /// <remarks>The caller owns this new copy; repeated copies and encoders are outside the capture operation's pixel budget.</remarks>
    public RgbaImage ToImage() => new(_pixels.Image.Width, _pixels.Image.Height, (byte[])_pixels.Image.Pixels.Clone());
    internal RgbaImage Image => _pixels.Image;
    internal VisualSceneLayerCapturePlan Plan { get; }
    internal IReadOnlyList<ChartForgeX.Effects.VisualMask> PostCaptureMasks => _postCaptureMasks;
}
