using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

internal static partial class TextShaper {
    /// <summary>Actual finite fallback decisions of this native shaper, retained per captured resolver request.</summary>
    internal sealed class CapturedClusters {
        internal const int MaximumClusters = MaximumCachedTextLength;
        internal const int MaximumScalars = MaximumCachedGlyphs;
        private readonly Dictionary<string, ClusterDecision> _decisions;
        private readonly bool _immutable;
        private readonly bool _preferCaptured;
        // A cache key retains only this empty identity token, never the captured cluster dictionary or scope.
        internal object CacheIdentity { get; } = new object();
        private int _scalars;
        private bool _supported = true;
        internal CapturedClusters() { _decisions = new(StringComparer.Ordinal); }
        private CapturedClusters(CapturedClusters source, bool replay, bool immutable = true, bool preferCaptured = false) {
            _decisions = new(source._decisions, StringComparer.Ordinal); _scalars = source._scalars;
            _supported = source._supported; _immutable = immutable; IsReplay = replay; _preferCaptured = preferCaptured;
        }
        internal bool IsReplay { get; }
        internal bool Supported => _supported;
        internal IReadOnlyList<ClusterDecision> Decisions => _decisions.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray();
        internal CapturedClusters ContinueCaptured() => new(this, false, immutable: false, preferCaptured: true);
        internal bool HasCaptured(IReadOnlyList<int> points, int start, int count) => _preferCaptured && count <= MaximumScalars && _decisions.ContainsKey(Key(points, start, count));
        internal CapturedClusters Freeze() => new(this, false);
        internal CapturedClusters ContinueRecording() => new(this, false, immutable: false);
        internal CapturedClusters Replay() {
            if (!_supported) throw new NotSupportedException("Native fallback capture exceeds its finite cluster/scalar bound or contains conflicting/uncovered decisions.");
            return new CapturedClusters(this, true);
        }
        internal void Observe(IReadOnlyList<int> points, int start, int count, TrueTypeFont face, IReadOnlyList<int> output) {
            if (_immutable) throw new InvalidOperationException("A captured native cluster snapshot is immutable.");
            if (!_supported) return;
            if (count > MaximumScalars || output.Count > MaximumScalars || count > MaximumScalars - output.Count) { _supported = false; return; }
            var key = Key(points, start, count);
            if (_decisions.TryGetValue(key, out var previous)) {
                if (!ReferenceEquals(previous.Face, face) || !previous.Output.SequenceEqual(output)) _supported = false;
                return;
            }
            if (_decisions.Count >= MaximumClusters || _scalars + count + output.Count > MaximumScalars
                || output.Any(point => !IsIgnorable(point) && !face.HasGlyph(point))) { _supported = false; return; }
            var raw = new int[count]; for (var index = 0; index < count; index++) raw[index] = points[start + index];
            _decisions.Add(key, new ClusterDecision(raw, face, output.ToArray())); _scalars += count + output.Count;
        }
        internal ClusterDecision Lookup(IReadOnlyList<int> points, int start, int count) {
            if ((!IsReplay && !_preferCaptured) || count > MaximumScalars || !_decisions.TryGetValue(Key(points, start, count), out var decision))
                throw new NotSupportedException("A native frame requested an uncaptured fallback cluster; changing fallback repertoires require a separately captured admission.");
            return decision;
        }
        private static string Key(IReadOnlyList<int> points, int start, int count) {
            var key = new System.Text.StringBuilder();
            for (var index = 0; index < count; index++) { if (index != 0) key.Append(','); key.Append(points[start + index].ToString(CultureInfo.InvariantCulture)); }
            return key.ToString();
        }
    }
    internal sealed class ClusterDecision {
        internal ClusterDecision(int[] raw, TrueTypeFont face, int[] output) {
            Raw = Array.AsReadOnly(raw); Face = face; Output = Array.AsReadOnly(output);
        }
        internal IReadOnlyList<int> Raw { get; }
        internal TrueTypeFont Face { get; }
        internal IReadOnlyList<int> Output { get; }
    }
    private static void ApplyCapturedCluster(CapturedClusters captured, IReadOnlyList<int> points, Cluster cluster) {
        var decision = captured.Lookup(points, cluster.Start, cluster.Count);
        cluster.Face = decision.Face; cluster.Output.AddRange(decision.Output); cluster.FallbackAttempted = true;
    }
}
