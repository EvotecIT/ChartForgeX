using System;
using System.Collections.Generic;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// The selected portable decoded-root budget; native capture continues to share the existing immutable font roots.
/// A face from a collection larger than the per-root budget is owned as its own member bytes, so a platform fallback
/// such as one Helvetica Neue face is captured as drawn; a single face over the budget is still rejected.
/// </summary>
internal sealed class OwnedFontResources {
    internal const int MaximumRootBytes = 2 * 1024 * 1024;
    internal const int MaximumDistinctBytes = 4 * 1024 * 1024;
    internal const int MaximumRoots = 32;
    private readonly List<TrueTypeFont> _roots = new();
    private readonly Dictionary<string, TrueTypeFont> _members = new(StringComparer.Ordinal);
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
        var root = Owned(font); var length = root.CapturedByteLength;
        if (length > MaximumRootBytes) throw new NotSupportedException("Owned font bytes exceed the portable 2 MiB per-root limit.");
        foreach (var existing in _roots)
            if (existing.CapturedByteIdentity == root.CapturedByteIdentity && existing.SameCapturedBytes(root)) return;
        if (_roots.Count >= MaximumRoots) throw new NotSupportedException("Owned font roots exceed the portable 32-root limit.");
        var total = _bytes + length;
        if (total > MaximumDistinctBytes) throw new NotSupportedException("Distinct owned font bytes exceed the portable 4 MiB aggregate limit.");
        _roots.Add(root); _bytes = total;
    }
    /// <summary>The collection index a captured view records; a member owned as its own bytes has none.</summary>
    internal int? CollectionIndex(TrueTypeFont font) => ReferenceEquals(Owned(font), font.Root) ? font.CollectionIndex : null;
    internal string Reference(TrueTypeFont font) {
        font = Owned(font);
        foreach (var root in _roots) if (root.CapturedByteIdentity == font.CapturedByteIdentity && root.SameCapturedBytes(font)) return root.CapturedByteIdentity;
        throw new ArgumentException("A captured font view references an unadmitted owned root.");
    }
    private TrueTypeFont Owned(TrueTypeFont font) {
        var root = font.Root;
        if (root.CapturedByteLength <= MaximumRootBytes || root.IsSingleCapturedFace) return root;
        var key = root.CapturedByteIdentity + "#" + root.CollectionIndex.GetValueOrDefault().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!_members.TryGetValue(key, out var member)) _members.Add(key, member = root.CopyCollectionMember(MaximumRootBytes) ?? root);
        return member;
    }
}
