using System;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.Mermaid;

public sealed partial class MermaidFlowchartRenderOptions {
    /// <summary>Gets or sets caller-owned images keyed by exact source img identities. Null leaves image references inert.</summary>
    /// <remarks>Option snapshots detach pixels for at most 256 keys and at most 256 MiB of distinct copied RGBA views.
    /// Identical borrowed arrays with the same required length share one owned copy inside that snapshot; descriptors remain independent.
    /// This snapshot ceiling is separate from native decode, scene, export and encoder admission and is not a total-memory guarantee.
    /// Keys are opaque identities and never trigger URL, file, network or provider requests.</remarks>
    public Dictionary<string, RgbaImage>? ImageBindings { get; set; }

    private void SnapshotImageBindings() {
        if (ImageBindings == null) return;
        if (ImageBindings.Count > 256) throw new ArgumentException("A source image binding catalog cannot exceed 256 entries.", nameof(ImageBindings));
        var copy = new Dictionary<string, RgbaImage>(ImageBindings.Count, StringComparer.Ordinal);
        var copiedViews = new Dictionary<byte[], Dictionary<int, byte[]>>();
        long declared = 0;
        foreach (var pair in ImageBindings) {
            if (string.IsNullOrWhiteSpace(pair.Key)) throw new ArgumentException("Source image binding keys must contain text.", nameof(ImageBindings));
            var image = pair.Value;
            if (image.Width <= 0 || image.Height <= 0 || image.Pixels == null)
                throw new ArgumentException("Source image binding views require positive dimensions and RGBA pixels.", nameof(ImageBindings));
            var required = checked((long)image.Width * image.Height * 4);
            if (required > image.Pixels.LongLength)
                throw new ArgumentException("Source image binding pixels are smaller than their declared dimensions.", nameof(ImageBindings));
            if (!copiedViews.TryGetValue(image.Pixels, out var lengths)) {
                lengths = new Dictionary<int, byte[]>(); copiedViews.Add(image.Pixels, lengths);
            }
            if (required > RasterEffectPreflight.MaximumLiveBytes)
                throw new VisualResourceLimitException("SourceImageSnapshotBytes", RasterEffectPreflight.MaximumLiveBytes, required, pair.Key);
            var length = checked((int)required);
            if (!lengths.TryGetValue(length, out var pixels)) {
                var observed = checked(declared + required);
                if (observed > RasterEffectPreflight.MaximumLiveBytes)
                    throw new VisualResourceLimitException("SourceImageSnapshotBytes", RasterEffectPreflight.MaximumLiveBytes, observed, pair.Key);
                // Admission precedes every copied allocation; only this exact source-array/required-length view can reuse it.
                pixels = new byte[length]; Array.Copy(image.Pixels, pixels, length); lengths.Add(length, pixels); declared = observed;
            }
            copy.Add(pair.Key, new RgbaImage(image.Width, image.Height, pixels));
        }
        ImageBindings = copy;
    }
}
