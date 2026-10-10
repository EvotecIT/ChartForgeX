using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>Writes authored relationship identities without numeric point or label reconstruction.</summary>
internal static class ChartRelationshipMetadata {
    internal static string SourceId(string role, string id) => "series-0-" + role + "-" + Uri.EscapeDataString(id);

    internal static Dictionary<string, string> Node(ChartSeries series, string id, string label, int sourceIndex) {
        var metadata = Target(series, "node", id);
        metadata["data-cfx-node"] = id;
        metadata["data-cfx-label"] = label;
        metadata["data-cfx-full-label"] = label;
        metadata["data-cfx-source-node-index"] = sourceIndex.ToString(CultureInfo.InvariantCulture);
        return metadata;
    }

    internal static Dictionary<string, string> Link(ChartSeries series, string id, string sourceId, string targetId,
        string sourceLabel, string targetLabel, int sourceIndex, double value) {
        var metadata = Target(series, "link", id);
        metadata["data-cfx-source"] = sourceId;
        metadata["data-cfx-target"] = targetId;
        metadata["data-cfx-source-label"] = sourceLabel;
        metadata["data-cfx-target-label"] = targetLabel;
        metadata["data-cfx-label"] = sourceLabel + " to " + targetLabel;
        metadata["data-cfx-full-label"] = sourceLabel + " to " + targetLabel + ": " + value.ToString("R", CultureInfo.InvariantCulture);
        metadata["data-cfx-source-link-index"] = sourceIndex.ToString(CultureInfo.InvariantCulture);
        metadata["data-cfx-value"] = value.ToString("R", CultureInfo.InvariantCulture);
        return metadata;
    }

    private static Dictionary<string, string> Target(ChartSeries series, string kind, string id) => new() {
        ["data-cfx-id"] = id, ["data-cfx-target-kind"] = kind, ["data-cfx-target-id"] = id,
        ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey,
        ["data-cfx-series-name"] = series.Name, ["data-cfx-kind"] = series.Kind.ToString()
    };
}
