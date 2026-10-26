using System;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneRasterRenderer {
    /// <summary>Plans an actual already-shaped text capture; the incumbent native painter owns every glyph and stroke.</summary>
    internal static NativeTextInkPlan PlanTextInk(VisualSceneText node, VisualRenderOptions options) {
        var text = node.Text;
        var leftEdge = node.Alignment == TextAlignment.Center ? node.X - text.Metrics.Width / 2
            : node.Alignment == TextAlignment.Right ? node.X - text.Metrics.Width : node.X;
        var margin = TextInkMargin(text) + (node.Stroke.HasValue ? node.StrokeWidth / 2 : 0);
        // Integer logical origins preserve the global output/coverage/bitmap-strike phase at every supported integer density.
        var left = Math.Floor(leftEdge - margin); var top = Math.Floor(node.Baseline - text.Ascent - margin);
        var width = Dimension(leftEdge - left + text.Metrics.Width + margin);
        var height = Dimension(node.Baseline - text.Ascent - top + text.Metrics.Height + margin);
        var bounds = new ChartRect(left, top, width, height);
        var local = new VisualSceneText(text, node.X - left, node.Baseline - top, node.Color, node.Alignment, node.Role, node.Id,
            node.Paint, node.Stroke, node.StrokeWidth, node.StrokePaint);
        var scene = new VisualScene(new VisualSize(width, height), new VisualSceneNode[] { local },
            Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var allocation = CalculateAllocation(scene.Size, options.Scale, options.Supersampling, options.PixelBudget);
        var workingPixels = checked((long)allocation.PixelWidth * allocation.PixelHeight);
        var outputPixels = checked((long)(allocation.PixelWidth / options.Supersampling) * (allocation.PixelHeight / options.Supersampling));
        // Count the canvas, output view (even where it aliases) and immutable image copy without assuming collection/reuse.
        var bytes = checked((long)allocation.ByteCount + outputPixels * 8);
        var work = checked(workingPixels * 4 + outputPixels * 12);
        if (node.Stroke.HasValue && node.StrokeWidth > 0) {
            // The SAME PaintTransformedText owner allocates its own ink/mask and uses RasterAlphaExtrema for the stroke.
            var innerWidth = Dimension(text.Metrics.Width + margin * 2); var innerHeight = Dimension(text.Metrics.Height + margin * 2);
            var inner = RasterAllocationGuard.Calculate(innerWidth, innerHeight, 1, allocation.CombinedScale);
            var pixels = checked((long)inner.PixelWidth * inner.PixelHeight);
            if (pixels > RasterTextStroke.MaximumIntermediatePixels)
                throw new ArgumentOutOfRangeException(nameof(node), "Native text paint exceeds the supported intermediate raster budget.");
            // Ink/mask/output RGBA; two extrema calls each allocate horizontal/result bytes and an integer deque.
            bytes = checked(bytes + inner.ByteCount * 3L + pixels * 4 + Math.Max(inner.PixelWidth, inner.PixelHeight) * 8L);
            work = checked(work + pixels * 32);
        }
        return new NativeTextInkPlan(scene, node, bounds, options, bytes, work);
    }

    /// <summary>Private capture facts are finite, detached and scoped to one SVG emission; no new font resolution is performed.</summary>
    internal sealed class NativeTextInkPlan {
        private readonly VisualScene _scene;
        private readonly VisualSceneText _original;
        private readonly VisualRenderOptions _options;
        internal NativeTextInkPlan(VisualScene scene, VisualSceneText original, ChartRect bounds, VisualRenderOptions options, long pixelBytes, long workSamples) {
            _scene = scene; _original = original; Bounds = bounds; _options = options; PixelBytes = pixelBytes; WorkSamples = workSamples;
        }
        internal ChartRect Bounds { get; }
        internal long PixelBytes { get; }
        internal long WorkSamples { get; }
        internal VisualSceneImageData Capture(CancellationToken cancellation) {
            cancellation.ThrowIfCancellationRequested();
            RasterEffectPreflight.ValidateCapture(null, 0, PixelBytes, WorkSamples, out _, out _);
            RgbaImage image;
            if (_original.Stroke.HasValue && _original.StrokeWidth > 0) {
                // Keep the original node/buffer baseline arithmetic. Only the destination canvas is
                // cropped; the shared stroke painter and sampler retain the global pixel phase.
                var canvas = new RgbaCanvas(Bounds.Width, Bounds.Height, _options.Supersampling, null,
                    _options.Scale, useDefaultOutlineFont: false);
                PaintTransformedText(canvas, _original, VisualSceneTransform.Identity, _options.TextHinting,
                    new ChartPoint(Bounds.X, Bounds.Y));
                image = canvas.ToImage();
            } else image = Render(_scene, _options.Scale, _options.Supersampling, _options.PixelBudget, _options.TextHinting, cancellation);
            cancellation.ThrowIfCancellationRequested();
            var captured = new VisualSceneImageData(image);
            cancellation.ThrowIfCancellationRequested(); return captured;
        }
    }
}
