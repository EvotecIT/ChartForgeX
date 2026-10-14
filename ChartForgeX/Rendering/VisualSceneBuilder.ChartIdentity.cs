using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Captures stable authored identities once, independently of each family's ordinal geometry IDs.</summary>
    internal void CaptureChartIdentity(Chart chart) {
        for (var index = 0; index < _nodes.Count; index++) {
            if (_nodes[index] is not VisualSceneGroup group || !group.Metadata.TryGetValue("data-cfx-series", out var ordinal)
                || !int.TryParse(ordinal, NumberStyles.None, CultureInfo.InvariantCulture, out var seriesIndex)
                || seriesIndex < 0 || seriesIndex >= chart.Series.Count) continue;
            var series = chart.Series[seriesIndex];
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in group.Metadata) metadata.Add(entry.Key, entry.Value);
            metadata["data-cfx-series-key"] = series.Id.Value;
            metadata["data-cfx-series-scope"] = series.Id.IsAutomatic ? "local" : "shared";
            if (TryAuthoredObservationIndex(metadata, out var sourceIndex)
                && series.ObservationIds.TryGetValue(sourceIndex, out var observationId))
                metadata["data-cfx-observation-id"] = observationId.Value;
            _nodes[index] = new VisualSceneGroup(group.Role, group.Id, group.Clip, metadata,
                group.PathClip, group.Rotation, group.Href, group.Tooltip, group.Translation, group.ImageResource, group.ColorScope);
        }
    }

    private static bool TryAuthoredObservationIndex(IReadOnlyDictionary<string, string> metadata, out int sourceIndex) {
        sourceIndex = -1;
        // Some authored items calculate their displayed value from other inputs, while still owning their own identity.
        if (metadata.TryGetValue("data-cfx-observation-index", out var observation))
            return int.TryParse(observation, NumberStyles.None, CultureInfo.InvariantCulture, out sourceIndex);
        if (metadata.ContainsKey("data-cfx-derived") || metadata.ContainsKey("data-cfx-source-points")) {
            // A direct slice or one-interval run still represents exactly one authored observation.
            // Aggregates retain their derived identity rather than borrowing a contributor's ID.
            return metadata.TryGetValue("data-cfx-derived", out var derived) && (derived == "radial-slice" || derived == "timeline-run")
                && metadata.TryGetValue("data-cfx-point", out var point)
                && int.TryParse(point, NumberStyles.None, CultureInfo.InvariantCulture, out sourceIndex)
                && metadata.TryGetValue("data-cfx-source-points", out var sources)
                && int.TryParse(sources, NumberStyles.None, CultureInfo.InvariantCulture, out var contributor)
                && contributor == sourceIndex;
        }
        return (metadata.TryGetValue("data-cfx-source-point", out var source) || metadata.TryGetValue("data-cfx-point", out source))
            && int.TryParse(source, NumberStyles.None, CultureInfo.InvariantCulture, out sourceIndex);
    }
}
