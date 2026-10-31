using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Applies authored host metadata to detached semantic groups after identity capture.</summary>
    private void CaptureMarkOptions(Chart chart) {
        var regionIndices = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var regionIndex = 0; regionIndex < _regions.Count; regionIndex++) regionIndices[_regions[regionIndex].Id] = regionIndex;
        for (var index = 0; index < _nodes.Count; index++) {
            if (_nodes[index] is not VisualSceneGroup group) continue;
            ChartSeries? series = null;
            if (group.Metadata.TryGetValue("data-cfx-series", out var ordinal)
                && int.TryParse(ordinal, NumberStyles.None, CultureInfo.InvariantCulture, out var seriesIndex)
                && seriesIndex >= 0 && seriesIndex < chart.Series.Count) series = chart.Series[seriesIndex];
            else if (group.Role == ChartDomRoles.ScheduleRow && group.Metadata.TryGetValue(ChartDomRoles.LaneIdAttribute, out var laneId)) {
                foreach (var lane in chart.Series) if (lane.InteractionIdentityKey == laneId) { series = lane; break; }
            }
            if (series == null) continue;
            var isMark = TryAuthoredObservationIndex(group.Metadata, out var observation);
            var isRun = group.Metadata.TryGetValue("data-cfx-derived", out var derived) && derived == "timeline-run";
            // Decimation keeps original source IDs but the retained point still owns its options.
            if (isMark && !group.Metadata.ContainsKey("data-cfx-observation-index")
                && group.Metadata.TryGetValue("data-cfx-point", out var pointOrdinal)
                && int.TryParse(pointOrdinal, NumberStyles.None, CultureInfo.InvariantCulture, out var displayed)) observation = displayed;
            var isRow = group.Role == ChartDomRoles.ScheduleRow || group.Role == "series" && series.Kind is ChartSeriesKind.Heatmap or ChartSeriesKind.HexbinHeatmap;
            var options = isMark ? ObservationOptions(series, observation) ?? series.MarkOptions
                : isRun ? series.MarkOptions
                : group.Role == ChartDomRoles.ScheduleRow ? series.RowOptions
                : isRow ? series.RowOptions ?? series.MarkOptions : group.Role == "series" ? series.MarkOptions : null;
            if (options == null) continue;
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in group.Metadata) metadata.Add(entry.Key, entry.Value);
            if (options.CssClass != null) metadata["class"] = options.CssClass;
            foreach (var entry in options.DataAttributes) metadata[entry.Key] = entry.Value;
            if (options.Current || options.Selected) metadata["aria-current"] = "true";
            if (options.Current) metadata["data-cfx-current"] = "true";
            if (options.Selected) metadata["data-cfx-selected"] = "true";
            var legacyHref = isMark && series.IsCategoricalHeatmapRow && observation < series.HeatmapCells.Count
                ? series.HeatmapCells[observation]?.Href : null;
            var href = isMark || isRun || isRow ? options.Href ?? group.Href ?? legacyHref : group.Href;
            var tooltip = options.Tooltip ?? group.Tooltip;
            if (options.AccessibleLabel != null) {
                metadata["aria-label"] = options.AccessibleLabel;
                if (group.Id != null && regionIndices.TryGetValue(group.Id, out var regionIndex)) {
                    var region = _regions[regionIndex];
                    _regions[regionIndex] = new VisualSemanticRegion(region.Id, region.Role, region.Bounds, options.AccessibleLabel);
                }
            }
            if (metadata.TryGetValue("data-cfx-visible", out var visible) && visible == "false") href = null;
            if (href != null) {
                metadata["role"] = "link";
                if (!metadata.ContainsKey("aria-label")) metadata["aria-label"] = options.Tooltip ?? GeneratedMarkLabel(series, group.Metadata);
                // The default options value deliberately overrides a render-wide new-context policy.
                metadata["target"] = options.LinkTarget == VisualSvgLinkTarget.NewContext ? "_blank" : "_self";
            }
            _nodes[index] = CopyMarkGroup(group, metadata, href, tooltip);
            if (isMark && (href != null || options.Tooltip != null))
                SuppressNestedNavigation(index, href != null, options.Tooltip != null);
        }
        CaptureMarkStateOutlines(chart);
    }

    private static string GeneratedMarkLabel(ChartSeries series, IReadOnlyDictionary<string, string> metadata) {
        if (metadata.TryGetValue("data-cfx-label", out var label)) return label;
        if (metadata.TryGetValue("data-cfx-x", out var x) && metadata.TryGetValue("data-cfx-y", out var y)) return series.Name + ": " + x + ", " + y;
        return series.Name;
    }

    private static ChartMarkOptions? ObservationOptions(ChartSeries series, int index) {
        if (index < 0 || index >= series.ObservationCount) return null;
        if (series.IsCategoricalHeatmapRow) return series.HeatmapCells[index]?.MarkOptions;
        return series.Kind switch {
            ChartSeriesKind.Gantt => series.GanttItems[index].MarkOptions,
            ChartSeriesKind.GanttLane => series.GanttLaneItems[index].MarkOptions,
            ChartSeriesKind.StateTimeline => series.StateTimelineSegments[index].MarkOptions,
            ChartSeriesKind.CalendarHeatmap => series.CalendarItems[index].MarkOptions,
            ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea => series.Ranges[index].MarkOptions,
            ChartSeriesKind.RangeBar => series.Intervals[index].MarkOptions,
            ChartSeriesKind.Dumbbell => series.Dumbbells[index].MarkOptions,
            _ => index < series.Points.Count ? series.Points[index].MarkOptions : null
        };
    }

    // State/matrix producers retain an inner tooltip/link scope. Move navigation onto the semantic
    // mark when it has authored metadata, so one mark remains one focus target and never nests anchors.
    private void SuppressNestedNavigation(int start, bool links, bool tooltips) {
        var depth = 1;
        for (var index = start + 1; index < _nodes.Count && depth > 0; index++) {
            if (_nodes[index] is VisualSceneEndGroup) { depth--; continue; }
            if (_nodes[index] is not VisualSceneGroup child) continue;
            depth++;
            if (links && child.Href != null || tooltips && child.Tooltip != null)
                _nodes[index] = CopyMarkGroup(child, child.Metadata, links ? null : child.Href, tooltips ? null : child.Tooltip);
        }
    }

    private static VisualSceneGroup CopyMarkGroup(VisualSceneGroup group, IReadOnlyDictionary<string, string> metadata, string? href, string? tooltip) =>
        new(group.Role, group.Id, group.Clip, metadata, group.PathClip, group.Rotation, href, tooltip,
            group.Translation, group.ImageResource, group.ColorScope, group.Opacity, group.SurfaceShadow, group.RetainedEffect);
}
