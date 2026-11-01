using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Installs captured plain-text tooltips on identified authored marks after host metadata.</summary>
    private void CaptureSeriesFormatting(Chart chart) {
        for (var index = 0; index < _nodes.Count; index++) {
            if (_nodes[index] is not VisualSceneGroup group
                || !group.Metadata.TryGetValue("data-cfx-series", out var ordinal)
                || !int.TryParse(ordinal, NumberStyles.None, CultureInfo.InvariantCulture, out var seriesIndex)
                || seriesIndex < 0 || seriesIndex >= chart.Series.Count) continue;
            var series = chart.Series[seriesIndex];
            string? tooltip, label;
            if (group.Metadata.TryGetValue("data-cfx-target-kind", out var kind) && kind == "link"
                && group.Metadata.TryGetValue("data-cfx-source-link-index", out var link)
                && int.TryParse(link, NumberStyles.None, CultureInfo.InvariantCulture, out var relationship)) {
                tooltip = relationship < series.PreparedRelationshipTooltips.Count ? series.PreparedRelationshipTooltips[relationship] : null;
                label = relationship < series.PreparedRelationshipLabels.Count ? series.PreparedRelationshipLabels[relationship] : null;
            } else {
                var authored = TryAuthoredObservationIndex(group.Metadata, out var observation);
                // Relationship nodes carry authored IDs instead of numeric point identities.
                if (!authored && group.Metadata.TryGetValue("data-cfx-source-node-index", out var node))
                    authored = int.TryParse(node, NumberStyles.None, CultureInfo.InvariantCulture, out observation);
                // Decimation retains original source IDs; captured text still uses the display ordinal.
                if (authored && !group.Metadata.ContainsKey("data-cfx-observation-index")
                    && group.Metadata.TryGetValue("data-cfx-point", out var point)
                    && int.TryParse(point, NumberStyles.None, CultureInfo.InvariantCulture, out var displayed)) observation = displayed;
                if (!authored) continue;
                tooltip = series.PreparedTooltip(observation);
                label = series.PreparedLabel(observation);
            }
            if (tooltip == null && label == null) continue;
            var metadata = new Dictionary<string, string>();
            foreach (var pair in group.Metadata) metadata.Add(pair.Key, pair.Value);
            if (label != null) metadata["data-cfx-full-label"] = label;
            if (tooltip != null) metadata["data-cfx-tooltip-text"] = tooltip;
            _nodes[index] = CopyMarkGroup(group, metadata, group.Href, tooltip == null ? group.Tooltip : tooltip.Length == 0 ? null : tooltip);
            if (tooltip != null) SuppressFormattingTooltips(index);
        }
    }

    // Hierarchy children are independent marks. Replacing a parent's tooltip must retain their defaults.
    private void SuppressFormattingTooltips(int start) {
        var depth = 1; var protectedDepth = 0;
        for (var index = start + 1; index < _nodes.Count && depth > 0; index++) {
            if (_nodes[index] is VisualSceneEndGroup) {
                if (depth == protectedDepth) protectedDepth = 0;
                depth--; continue;
            }
            if (_nodes[index] is not VisualSceneGroup child) continue;
            depth++;
            if (protectedDepth != 0) continue;
            if (child.Metadata.ContainsKey("data-cfx-target-kind") || child.Metadata.ContainsKey("data-cfx-observation-index")
                || child.Metadata.ContainsKey("data-cfx-point")) { protectedDepth = depth; continue; }
            if (child.Tooltip != null) _nodes[index] = CopyMarkGroup(child, child.Metadata, child.Href, null);
        }
    }
}
