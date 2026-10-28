using System;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>The selected portable decoded-root budget; native capture continues to share the existing immutable font roots.</summary>
internal sealed class OwnedFontResources {
    internal const int MaximumRootBytes = 2 * 1024 * 1024;
    internal const int MaximumDistinctBytes = 4 * 1024 * 1024;
    internal const int MaximumRoots = 32;
    private readonly List<TrueTypeFont> _roots = new();
    private long _bytes;
    internal IReadOnlyList<TrueTypeFont> Roots => _roots.AsReadOnly();
    internal static OwnedFontResources Capture(PreparedDiagramFont source, VisualFrame? frame = null) {
        var result = new OwnedFontResources(); result.Add(source.Font.OwnedFileFont);
        result.Add(frame?.TitleStyle?.Font.OwnedFileFont); result.Add(frame?.SubtitleStyle?.Font.OwnedFileFont); result.Add(frame?.LegendStyle?.Font.OwnedFileFont);
        if (source.Text is {} text) {
            if (text.Requests.Count > 0 && !text.SupportsCapturedReplay)
                throw new NotSupportedException("Portable diagram fonts require the existing covered, finite captured request/cluster contract.");
            foreach (var request in text.Requests) {
                result.Add(request.Style.Font.OwnedFileFont); result.Add(request.Face.Font);
                foreach (var decision in request.Clusters.Decisions) result.Add(decision.Face);
            }
        }
        return result;
    }
    private void Add(TrueTypeFont? font) {
        if (font == null) return;
        var root = font.Root; var length = root.CapturedByteLength;
        if (length > MaximumRootBytes) throw new NotSupportedException("Owned font bytes exceed the portable 2 MiB per-root limit.");
        foreach (var existing in _roots)
            if (existing.CapturedByteIdentity == root.CapturedByteIdentity && existing.SameCapturedBytes(root)) return;
        if (_roots.Count >= MaximumRoots) throw new NotSupportedException("Owned font roots exceed the portable 32-root limit.");
        var total = _bytes + length;
        if (total > MaximumDistinctBytes) throw new NotSupportedException("Distinct owned font bytes exceed the portable 4 MiB aggregate limit.");
        _roots.Add(root); _bytes = total;
    }
    internal string Reference(TrueTypeFont font) {
        foreach (var root in _roots) if (root.CapturedByteIdentity == font.CapturedByteIdentity && root.SameCapturedBytes(font)) return root.CapturedByteIdentity;
        throw new ArgumentException("A captured font view references an unadmitted owned root.");
    }
}
