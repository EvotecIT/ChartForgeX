using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSeriesVisibilityCapture {
    private static IReadOnlyDictionary<(ChartVisibilityTargetKind Kind, string Series, string Id), VisualSceneGroup> ReflowPointFacts(PreparedVisual complete) {
        var rows = new Dictionary<(ChartVisibilityTargetKind Kind, string Series, string Id), VisualSceneGroup>();
        foreach (var point in complete.Scene.Nodes.OfType<VisualSceneGroup>().Where(point => point.Role == "point" || point.Metadata.ContainsKey("data-cfx-node"))) {
            if (!point.Metadata.TryGetValue("data-cfx-series-key", out var series)) continue;
            var hierarchy = point.Metadata.TryGetValue("data-cfx-node", out var nodeId);
            var group = point.Metadata.TryGetValue("data-cfx-derived", out var derived) && derived == "five-number-summary";
            var id = nodeId; if (!hierarchy && !point.Metadata.TryGetValue(group ? "data-cfx-group-id" : "data-cfx-observation-id", out id)) continue;
            var key = (hierarchy ? ChartVisibilityTargetKind.HierarchyItem : group ? ChartVisibilityTargetKind.BoxPlotGroup : ChartVisibilityTargetKind.Observation, series, id!); if (rows.ContainsKey(key)) throw new NotSupportedException("Captured native Reflow point identities must be unique within their series and namespace.");
            rows.Add(key, point);
        }
        return rows;
    }
    // Full source native legend/readout facts precede filtered ordinal identity capture. No browser formatter or mark lookup is needed.
    private static IReadOnlyDictionary<string, string> ReflowLegendFacts(VisualSceneGroup visible,
        IReadOnlyDictionary<string, VisualSceneGroup> fullLegend, IReadOnlyDictionary<(ChartVisibilityTargetKind Kind, string Series, string Id), VisualSceneGroup> fullPoints) {
        if (visible.Id == null || !fullLegend.TryGetValue(visible.Id, out var source))
            throw new NotSupportedException("A retained native Reflow legend row needs its exact full-source native owner.");
        var metadata = new Dictionary<string, string>(); foreach (var pair in source.Metadata) metadata.Add(pair.Key, pair.Value);
        if (!metadata.TryGetValue("data-cfx-series-key", out var key)) return metadata;
        if (metadata.TryGetValue("data-cfx-derived", out var derived) && derived == "radial-slice") return metadata;
        metadata.TryGetValue("data-cfx-group-id", out var groupId); metadata.TryGetValue("data-cfx-observation-id", out var observationId);
        var hierarchy = metadata.TryGetValue("data-cfx-legend-target-kind", out var kind) && kind == "node";
        if (groupId == null && observationId == null && !hierarchy) return metadata;
        var id = hierarchy ? metadata["data-cfx-legend-target-id"] : groupId ?? observationId!;
        if (!fullPoints.TryGetValue((hierarchy ? ChartVisibilityTargetKind.HierarchyItem : groupId != null ? ChartVisibilityTargetKind.BoxPlotGroup : ChartVisibilityTargetKind.Observation, key, id), out var point))
            throw new NotSupportedException("A retained native Reflow point/group legend needs its captured full-source readout owner.");
        foreach (var pair in point.Metadata) if (pair.Key.StartsWith("data-cfx-label", StringComparison.Ordinal)
            || pair.Key.StartsWith("data-cfx-formatted-", StringComparison.Ordinal)
            || pair.Key is "data-cfx-x" or "data-cfx-y" or "data-cfx-value" or "data-cfx-minimum" or "data-cfx-q1" or "data-cfx-median" or "data-cfx-q3" or "data-cfx-maximum" or "data-cfx-category-label" or "data-cfx-authored-label" or "data-cfx-authored-value" or "data-cfx-remainder-value" or "data-cfx-parent" or "data-cfx-node")
            metadata[pair.Key] = pair.Value;
        return metadata;
    }
}
