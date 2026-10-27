using System;
using System.Collections.Generic;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    /// <summary>One Automatic emission's native non-outline line ink; explicit contours and structured ink retain strict admission.</summary>
    private sealed class NativeLineInkResources {
        private readonly Dictionary<VisualSceneText, Image> _images = new();
        internal ISet<(VisualSceneText Node, int Line)> Lines { get; } = new HashSet<(VisualSceneText Node, int Line)>();
        internal Image? ForNode(VisualSceneText node) => _images.TryGetValue(node, out var image) ? image : null;
        internal static NativeLineInkResources Prepare(VisualScene scene, VisualSvgOptions options, EffectExportPlan? effects, CancellationToken cancellation,
            int? maximumCharacters, long existingImageCharacters) {
            var result = new NativeLineInkResources();
            var plans = new List<(VisualSceneText Node, VisualSceneRasterRenderer.NativeTextInkPlan Plan)>();
            var placements = new Dictionary<VisualSceneText, int>();
            var raster = options.EffectRasterOptions ?? new VisualRenderOptions();
            // Join the existing SVG effect batch before either owner's private pixels materialize.
            var borrowed = new HashSet<byte[]>(); VisualSceneRasterRenderer.CollectBorrowedNativeImages(scene, borrowed);
            if (effects != null) foreach (var pixels in effects.Borrowed) borrowed.Add(pixels);
            long bytes = effects?.PixelBytes ?? 0, work = effects?.WorkSamples ?? 0;
            foreach (var pixels in borrowed) bytes = checked(bytes + pixels.LongLength);
            // Complete the declared pixel-array/work admission before any line canvas, output or immutable image copy.
            for (var index = 0; index < scene.Nodes.Count; index++) {
                cancellation.ThrowIfCancellationRequested();
                if (scene.Nodes[index] is VisualSceneGroup { ImageResource: not null } || effects?.BakedMasks.ContainsKey(index) == true) {
                    SkipImageFallback(scene, ref index); continue;
                }
                if (scene.Nodes[index] is not VisualSceneText node || node.Text.NativeGlyphInk) continue;
                var prepared = node.Text; var face = prepared.Face;
                var needsInk = false;
                for (var lineIndex = 0; lineIndex < prepared.Lines.Count; lineIndex++) {
                    var line = prepared.Lines[lineIndex];
                    if (line.Text.Length == 0 || face.Font == null) continue; // Empty/TinyFont lines retain the existing outline owner.
                    var bold = face.SynthesizeBold ? RgbaCanvas.EmphasisOffset(prepared.Size) : 0;
                    if (!face.Font.TryGetOutlineRun(line.Glyphs, node.LineLeft(line), node.Baseline + lineIndex * prepared.Metrics.LineHeight,
                        prepared.Size, face.SynthesizeItalic, bold, out _, allowSyntheticBold: true)) needsInk = true;
                }
                if (!needsInk) continue;
                if (placements.TryGetValue(node, out var count)) { placements[node] = checked(count + 1); continue; }
                placements.Add(node, 1);
                // Preserve a multiline node's existing overlap/stroke/downsampling composition in one native capture.
                var plan = VisualSceneRasterRenderer.PlanTextInk(node, raster);
                bytes = checked(bytes + plan.PixelBytes); work = checked(work + plan.WorkSamples);
                RasterEffectPreflight.ValidateCapture(null, 0, bytes, work, out _, out _);
                plans.Add((node, plan));
                for (var lineIndex = 0; lineIndex < prepared.Lines.Count; lineIndex++) result.Lines.Add((node, lineIndex));
            }
            long characters = existingImageCharacters;
            foreach (var entry in plans) {
                cancellation.ThrowIfCancellationRequested();
                var image = entry.Plan.Capture(cancellation);
                var before = characters; var count = placements[entry.Node];
                var uri = maximumCharacters.HasValue
                    ? image.BoundedDataUri((int)((maximumCharacters.Value - before) / count),
                        observed => new VisualResourceLimitException("MaximumOutputBytes", maximumCharacters.Value, checked(before + observed * count), entry.Node.Id))
                    : image.DataUri;
                cancellation.ThrowIfCancellationRequested(); characters = checked(characters + (long)uri.Length * count);
                result._images.Add(entry.Node, new Image(entry.Plan.Bounds, uri, image));
            }
            return result;
        }
        internal sealed class Image {
            internal Image(ChartRect bounds, string uri, VisualSceneImageData data) { Bounds = bounds; Uri = uri; Data = data; }
            internal ChartRect Bounds { get; }
            internal string Uri { get; }
            internal VisualSceneImageData Data { get; }
        }
    }
}
