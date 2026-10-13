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
            if (!metadata.ContainsKey("data-cfx-derived") && !metadata.ContainsKey("data-cfx-source-points")
                && (metadata.TryGetValue("data-cfx-source-point", out var source) || metadata.TryGetValue("data-cfx-point", out source))
                && int.TryParse(source, NumberStyles.None, CultureInfo.InvariantCulture, out var sourceIndex)
                && series.ObservationIds.TryGetValue(sourceIndex, out var observationId))
                metadata["data-cfx-observation-id"] = observationId.Value;
            _nodes[index] = new VisualSceneGroup(group.Role, group.Id, group.Clip, metadata,
                group.PathClip, group.Rotation, group.Href, group.Tooltip, group.Translation, group.ImageResource);
        }
    }
}
