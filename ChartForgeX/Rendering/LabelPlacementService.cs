using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Places labels with the same font resolution and shaping used by PNG text, with a bounded measurement cache.</summary>
/// <remarks>For the same fonts and input order, placement is deterministic. Unresolvable fonts use portable measurements.
/// Intersections with other labels and marks are rejected. Intentional containment in an associated mark is allowed.</remarks>
public sealed class LabelPlacementService {
    private readonly Dictionary<MeasurementKey, TextMetrics> _measurements = new();
    private readonly object _gate = new();
    private const int CacheCapacity = 4096;
    private int _fontVersion = -1;

    /// <summary>Measures shaped text with real font metrics, or a portable estimate if no font is available.</summary>
    public TextMetrics Measure(string text, TextStyle style) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (style == null) throw new ArgumentNullException(nameof(style));
        return MeasureDisplayed(TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture), style);
    }

    private TextMetrics MeasureDisplayed(string text, TextStyle style) {
        var key = new MeasurementKey(text, style);
        lock (_gate) {
            var version = TypographyFontResolver.CacheVersion;
            if (version != _fontVersion) { _measurements.Clear(); _fontVersion = version; }
            if (_measurements.TryGetValue(key, out var cached)) return cached;
            var face = TypographyFontResolver.WithLanguage(TypographyFontResolver.ResolveFace(style.Font), style.OpenTypeLanguageTag);
            var lineHeight = face.Font == null ? style.EffectiveFontSize * style.LineHeight : TextLayoutEngine.ResolveLineHeight(style, face.Font);
            var width = 0d;
            var lines = 0;
            foreach (var line in TextLineScanner.Enumerate(text)) {
                var value = line.Read(text);
                width = Math.Max(width, face.Font == null ? value.Length * style.EffectiveFontSize * (style.Font.Weight >= 600 ? 0.62 : 0.56)
                    : TextLayoutEngine.MeasureWidth(value, style, face));
                lines++;
            }
            var measured = new TextMetrics(width, Math.Max(1, lines) * lineHeight, lineHeight);
            if (_measurements.Count >= CacheCapacity) _measurements.Clear();
            _measurements[key] = measured;
            return measured;
        }
    }

    /// <summary>Places a complete scene, returning results in the original request order.</summary>
    /// <param name="requests">Label requests in stable input order.</param>
    /// <param name="bounds">The usable scene rectangle.</param>
    /// <param name="obstacles">Mark rectangles to avoid.</param>
    /// <param name="gap">Minimum separation in logical pixels.</param>
    public IReadOnlyList<PlacedLabel> Place(IReadOnlyList<LabelPlacementRequest> requests, ChartRect bounds, IReadOnlyList<LabelObstacle>? obstacles = null, double gap = 2) {
        if (requests == null) throw new ArgumentNullException(nameof(requests));
        ChartGuards.Finite(gap, nameof(gap));
        if (gap < 0) throw new ArgumentOutOfRangeException(nameof(gap));
        ValidateBounds(bounds);
        var order = new List<int>(requests.Count);
        for (var i = 0; i < requests.Count; i++) {
            if (requests[i] == null) throw new ArgumentException("Label requests must not contain null.", nameof(requests));
            if (!Enum.IsDefined(typeof(LabelFallbackRule), requests[i].Fallback)) throw new ArgumentException("Unknown label fallback rule.", nameof(requests));
            if (requests[i].Bounds.HasValue) ValidateBounds(requests[i].Bounds!.Value);
            ChartGuards.Finite(requests[i].Padding, nameof(requests));
            if (requests[i].Padding < 0) throw new ArgumentOutOfRangeException(nameof(requests));
            order.Add(i);
        }
        order.Sort((a, b) => { var priority = requests[b].Priority.CompareTo(requests[a].Priority); return priority == 0 ? a.CompareTo(b) : priority; });
        var occupied = new LabelSpatialIndex(gap);
        if (obstacles != null) foreach (var obstacle in obstacles) { ValidateBounds(obstacle.Bounds); occupied.Add(obstacle.Bounds, obstacle.Id, obstacle.Shape); }
        var placed = new PlacedLabel[requests.Count];
        foreach (var index in order) {
            var request = requests[index];
            var text = TextCaseTransformer.Apply(request.Text, request.Style.TextCase, CultureInfo.InvariantCulture);
            var result = TryPlace(request, text, bounds, occupied);
            if (result == null && request.Fallback == LabelFallbackRule.EllipsisThenDrop) {
                // Text elements retain surrogate pairs and combining marks during shortening.
                var elements = StringInfo.ParseCombiningCharacters(text);
                for (var count = elements.Length - 1; count > 0 && result == null; count--) {
                    var shorter = text.Substring(0, elements[count]).TrimEnd() + "…";
                    result = TryPlace(request, shorter, bounds, occupied, ellipsized: true);
                }
            }
            result ??= new PlacedLabel(request, string.Empty, default, true, -1);
            placed[index] = result;
            if (!result.IsDropped) occupied.Add(result.Bounds, null);
        }
        return Array.AsReadOnly(placed);
    }

    private PlacedLabel? TryPlace(LabelPlacementRequest request, string text, ChartRect scene, LabelSpatialIndex occupied, bool ellipsized = false) {
        if (text.Length == 0) return null;
        var metrics = !ellipsized && request.MeasuredSize.HasValue ? request.MeasuredSize.Value : MeasureDisplayed(text, request.Style);
        var decoration = text != request.Text ? request.DecorationSize : null;
        var width = metrics.Width + request.Padding * 2 + (decoration?.Width ?? 0);
        var height = metrics.Height + request.Padding * 2 + (decoration?.Height ?? 0);
        for (var i = 0; i < request.Candidates.Count; i++) {
            var candidate = request.Candidates[i];
            var box = new ChartRect(request.Anchor.X + candidate.OffsetX - width * candidate.HorizontalAlignment,
                request.Anchor.Y + candidate.OffsetY - height * candidate.VerticalAlignment, width, height);
            if (!Contains(scene, box) || request.Bounds.HasValue && !Contains(request.Bounds.Value, box)) continue;
            if (occupied.Intersects(box, request.AssociatedMarkId)) continue;
            return new PlacedLabel(request, text, box, false, i, ellipsized);
        }
        return null;
    }

    internal static bool Contains(ChartRect outer, ChartRect inner) => inner.Left >= outer.Left - 0.0001 && inner.Right <= outer.Right + 0.0001 && inner.Top >= outer.Top - 0.0001 && inner.Bottom <= outer.Bottom + 0.0001;
    private static void ValidateBounds(ChartRect bounds) {
        ChartGuards.Finite(bounds.X, nameof(bounds)); ChartGuards.Finite(bounds.Y, nameof(bounds));
        ChartGuards.Finite(bounds.Width, nameof(bounds)); ChartGuards.Finite(bounds.Height, nameof(bounds));
        if (bounds.Width < 0 || bounds.Height < 0) throw new ArgumentOutOfRangeException(nameof(bounds));
    }

    private readonly struct MeasurementKey : IEquatable<MeasurementKey> {
        private readonly string _text, _family, _path, _face, _language;
        private readonly double _size, _lineHeight;
        private readonly int _weight, _index;
        private readonly bool _italic;
        private readonly FontVariationSettings _variations;
        public MeasurementKey(string text, TextStyle style) {
            _text = text; _family = style.Font.Family; _path = style.Font.FilePath ?? ""; _face = style.Font.FaceName ?? "";
            _language = style.OpenTypeLanguageTag ?? ""; _size = style.EffectiveFontSize; _lineHeight = style.LineHeight;
            _weight = style.Font.Weight; _index = style.Font.CollectionIndex ?? -1; _italic = style.Font.Italic; _variations = style.Font.Variations;
        }
        public bool Equals(MeasurementKey other) => _text == other._text && _family == other._family && _path == other._path && _face == other._face
            && _language == other._language && _size == other._size && _lineHeight == other._lineHeight && _weight == other._weight
            && _index == other._index && _italic == other._italic && _variations.Equals(other._variations);
        public override bool Equals(object? obj) => obj is MeasurementKey key && Equals(key);
        public override int GetHashCode() { unchecked { var hash = _text.GetHashCode(); hash = hash * 31 + _family.GetHashCode(); hash = hash * 31 + _size.GetHashCode(); return hash * 31 + _weight; } }
    }
}
