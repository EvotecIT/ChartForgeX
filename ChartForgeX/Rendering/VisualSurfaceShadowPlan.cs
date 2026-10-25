using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

/// <summary>One immutable local capture, kernel and support plan shared by native raster and both SVG profiles.</summary>
internal sealed class VisualSurfaceShadowPlan {
    internal VisualSurfaceShadowPlan(VisualScene scene, int first, int last,
        VisualSceneSurfaceShadow scope, VisualRenderOptions options, bool rasterize = true) {
        Scope = scope;
        Options = options;
        // Existing rounded/ellipse paints feather at most one working sample outside numeric stroke ink.
        var feather = 1d / checked(options.Scale * options.Supersampling);
        var ink = scope.InkBounds;
        var capture = RasterEffectGrid.Cover(new ChartRect(ink.X - feather, ink.Y - feather,
            ink.Width + feather * 2, ink.Height + feather * 2), options.Scale);
        Origin = new ChartPoint((double)capture.X / options.Scale, (double)capture.Y / options.Scale);
        SourceSize = new VisualSize((double)capture.Width / options.Scale, (double)capture.Height / options.Scale);
        var allocation = VisualSceneRasterRenderer.CalculateAllocation(SourceSize,
            options.Scale, options.Supersampling, options.PixelBudget);
        // Use the incumbent canvas's actual rounded output dimensions. A rational logical
        // crop size must not under-debit a possible extra ceil cell after density conversion.
        var outputWidth = allocation.PixelWidth / options.Supersampling;
        var outputHeight = allocation.PixelHeight / options.Supersampling;
        var source = new RasterEffectRegion(0, 0, outputWidth, outputHeight);
        var effect = scope.Effect;
        LogicalClip = effect.ResolveRegion(scope.ObjectBounds);
        RasterEffectRegion? clip = null;
        if (LogicalClip is ChartRect box) {
            clip = RasterEffectGrid.Cover(new ChartRect(box.X - Origin.X, box.Y - Origin.Y,
                box.Width, box.Height), options.Scale);
        }
        Effect = RasterEffectPlan.SurfaceShadow(source,
            effect.HorizontalQ8(options.Scale), effect.VerticalQ8(options.Scale),
            checked(effect.OffsetX * options.Scale), checked(effect.OffsetY * options.Scale),
            effect.Color, checked(effect.Spread * options.Scale), effect.Opacity, clip);
        var result = Effect.ResultRegion;
        Bounds = new ChartRect(Origin.X + (double)result.X / options.Scale,
            Origin.Y + (double)result.Y / options.Scale,
            (double)result.Width / options.Scale, (double)result.Height / options.Scale);

        // Capture only this group's children. Its own transform and final clip apply to the completed effect.
        var nodes = new List<VisualSceneNode> {
            new VisualSceneGroup(null, null, null, null,
                translation: new ChartPoint(-Origin.X, -Origin.Y))
        };
        for (var index = first; index < last; index++) {
            var node = scene.Nodes[index];
            if (node is VisualSceneText || node is VisualSceneImage ||
                node is VisualSceneGroup group && (group.SurfaceShadow != null || group.RetainedEffect != null || group.ImageResource != null)) {
                throw new NotSupportedException("A retained surface effect scope accepts numeric surface geometry; " +
                    "text, image resources and nested surface effects stay outside it.");
            }
            nodes.Add(node);
        }
        nodes.Add(new VisualSceneEndGroup());
        Source = new VisualScene(SourceSize, nodes,
            Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var isolation = VisualSceneRasterRenderer.CaptureEffectAccounting(Source, allocation, options);
        PeakDepth = checked(isolation.PeakDepth + 1);
        SourceBytes = checked((long)outputWidth * outputHeight * 4);
        AdditionalBytes = checked(allocation.ByteCount + isolation.PixelBytes);
        AdditionalWork = checked((long)allocation.PixelWidth * allocation.PixelHeight * 4 +
            SourceBytes * 2 + isolation.WorkSamples + (long)result.Width * result.Height * 8);
        if (rasterize && Visible) {
            RasterEffectPreflight.ValidateCapture(Effect, SourceBytes, AdditionalBytes, AdditionalWork,
                out var work, out var bytes);
            WorkSamples = work;
            PixelBytes = bytes;
        }

    }

    internal VisualSceneSurfaceShadow Scope { get; }
    internal VisualRenderOptions Options { get; }
    internal VisualSize SourceSize { get; }
    internal ChartPoint Origin { get; }
    internal ChartRect Bounds { get; }
    internal ChartRect? LogicalClip { get; }
    internal VisualScene Source { get; }
    internal RasterEffectPlan Effect { get; }
    internal long SourceBytes { get; }
    internal long AdditionalBytes { get; }
    internal long AdditionalWork { get; }
    internal long PixelBytes { get; }
    internal long WorkSamples { get; }
    internal int PeakDepth { get; }
    internal bool Visible => Effect.ShadowVisibleRegion.HasValue;

    internal RgbaImage Capture(CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        var image = VisualSceneRasterRenderer.Render(Source, Options.Scale, Options.Supersampling,
            Options.PixelBudget, Options.TextHinting, cancellation);
        cancellation.ThrowIfCancellationRequested();
        var batch = RasterEffectPreflight.Prepare(new[] { Effect }, new[] { image },
            additionalPixelBytes: AdditionalBytes, additionalWorkSamples: AdditionalWork);
        return RasterEffects.Execute(batch, 0, Scope.Effect.OrderedDither, cancellation).Image;
    }
}
