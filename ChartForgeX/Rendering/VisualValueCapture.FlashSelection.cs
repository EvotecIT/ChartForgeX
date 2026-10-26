using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualValueCapture {
    /// <summary>Facts come from actual source readings and native derived groups; derived rows never acquire invented observation IDs.</summary>
    private sealed class FlashSelection {
        internal bool CartesianPointInk { get; }
        internal bool RangeEnvelopeInk { get; }
        internal readonly HashSet<(string Series, string Id)> Readings = new();
        internal readonly HashSet<(string Series, string Id)> Nodes = new();
        internal readonly HashSet<string> Series = new(StringComparer.Ordinal);
        internal readonly HashSet<string> OtherSeries = new(StringComparer.Ordinal);
        internal FlashSelection(IVisualValuePlan first, IVisualValuePlan last, PreparedVisual from, PreparedVisual to) {
            CartesianPointInk = first.Kind is "cartesian-value" or "cartesian-component-value";
            RangeEnvelopeInk = first is IVisualComponentValuePlan { SourceFamily: "range-components" };
            var indices = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal); var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < first.Readings.Count; index++) {
                var row = first.Readings[index]; var key = row.SeriesKey ?? first.SeriesKey; ordinals.TryGetValue(key, out var ordinal); ordinals[key] = ordinal + 1;
                if (row.Value == last.Readings[index].Value) continue;
                Readings.Add((key, row.ObservationId!)); Series.Add(key);
                if (first.Kind == "hierarchy-value") Nodes.Add((key, row.ObservationId!));
                if (!indices.TryGetValue(key, out var changed)) { changed = new HashSet<int>(); indices.Add(key, changed); } changed.Add(ordinal);
            }
            if (first.Kind == "hierarchy-value") {
                var old = NodeValues(from); foreach (var pair in NodeValues(to)) if (!old.TryGetValue(pair.Key, out var value) || value != pair.Value) Nodes.Add(pair.Key);
            }
            if (first.Kind is "radial-value" or "radial-raw-value") {
                var old = OtherGroups(from);
                foreach (var pair in OtherGroups(to)) {
                    var group = pair.Value; var contributors = group.Metadata["data-cfx-source-points"];
                    var includesChanged = indices.TryGetValue(pair.Key, out var changed) && contributors.Split(',').Any(value => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var ordinal) && changed.Contains(ordinal));
                    if (includesChanged || !old.TryGetValue(pair.Key, out var previous) || contributors != previous.Metadata["data-cfx-source-points"]
                        || Fact(group, "data-cfx-value") != Fact(previous, "data-cfx-value") || Fact(group, "data-cfx-value-overflow") != Fact(previous, "data-cfx-value-overflow")) OtherSeries.Add(pair.Key);
                }
            }
        }
        private static string? Fact(VisualSceneGroup group, string key) => group.Metadata.TryGetValue(key, out var value) ? value : null;
        private static Dictionary<(string Series, string Id), string?> NodeValues(PreparedVisual value) => value.Scene.Nodes.OfType<VisualSceneGroup>()
            .Where(group => group.Metadata.ContainsKey("data-cfx-node") && group.Metadata.ContainsKey("data-cfx-series-key"))
            .ToDictionary(group => (group.Metadata["data-cfx-series-key"], group.Metadata["data-cfx-node"]), group => Fact(group, "data-cfx-value"));
        private static Dictionary<string, VisualSceneGroup> OtherGroups(PreparedVisual value) => value.Scene.Nodes.OfType<VisualSceneGroup>()
            .Where(group => group.Role == "radial-point" && Fact(group, "data-cfx-point") == "-1")
            .ToDictionary(group => group.Metadata["data-cfx-series-key"], group => group, StringComparer.Ordinal);
    }
}
