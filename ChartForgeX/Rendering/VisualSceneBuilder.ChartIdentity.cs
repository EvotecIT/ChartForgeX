using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Captures stable authored identities once, independently of each family's ordinal geometry IDs.</summary>
    internal void CaptureChartIdentity(Chart chart) {
        var categoryProjection = DataProjection?.Horizontal == true ? DataProjection.Y : DataProjection?.X;
        var numericTimeDomain = categoryProjection?.Scale == ChartScaleKind.Time && categoryProjection.Minimum < 0;
        for (var index = 0; index < _nodes.Count; index++) {
            if (_nodes[index] is not VisualSceneGroup group || !group.Metadata.TryGetValue("data-cfx-series", out var ordinal)
                || !int.TryParse(ordinal, NumberStyles.None, CultureInfo.InvariantCulture, out var seriesIndex)
                || seriesIndex < 0 || seriesIndex >= chart.Series.Count) continue;
            var series = chart.Series[seriesIndex];
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in group.Metadata) metadata.Add(entry.Key, entry.Value);
            metadata["data-cfx-series-key"] = series.Id.Value;
            metadata["data-cfx-series-scope"] = series.Id.IsAutomatic ? "local" : "shared";
            if (metadata.TryGetValue("data-cfx-point", out var markIndex) && int.TryParse(markIndex, NumberStyles.None, CultureInfo.InvariantCulture, out var pointIndex)
                && pointIndex >= 0 && pointIndex < series.PointLabels.Count && series.PointLabels[pointIndex] != null)
                metadata["data-cfx-authored-label"] = series.PointLabels[pointIndex]!;
            CaptureChartInterval(chart, series, metadata, numericTimeDomain);
            if (TryAuthoredObservationIndex(metadata, out var sourceIndex)
                && series.ObservationIds.TryGetValue(sourceIndex, out var observationId))
                metadata["data-cfx-observation-id"] = observationId.Value;
            _nodes[index] = new VisualSceneGroup(group.Role, group.Id, group.Clip, metadata,
                group.PathClip, group.Rotation, group.Href, group.Tooltip, group.Translation, group.ImageResource, group.ColorScope);
        }
    }

    private static void CaptureChartInterval(Chart chart, ChartSeries series, IDictionary<string, string> metadata, bool numericTimeDomain) {
        if (ChartCartesianOrientation.CategoryAxis(chart).Scale == ChartScaleKind.Time && metadata.ContainsKey("data-cfx-x")) metadata["data-cfx-x-scale"] = "time";
        if (series.Kind is ChartSeriesKind.Timeline or ChartSeriesKind.Gantt or ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane
            && metadata.TryGetValue("data-cfx-start", out var start) && metadata.TryGetValue("data-cfx-end", out var end)) {
            metadata["data-cfx-range-start"] = start; metadata["data-cfx-range-end"] = end;
            if (chart.Options.XAxis.Scale == ChartScaleKind.Time) metadata["data-cfx-range-scale"] = "time";
        }
        if (series.TimeBucketInterval != null && metadata.TryGetValue("data-cfx-point", out _)
            && metadata.TryGetValue("data-cfx-x", out var x) && double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) {
            metadata["data-cfx-range-start"] = x;
            metadata["data-cfx-range-end"] = ChartTimeScale.BucketEnd(chart.Options.XAxis, value, series.TimeBucketInterval).ToString("R", CultureInfo.InvariantCulture);
            metadata["data-cfx-range-scale"] = "time";
            metadata["data-cfx-range-end-exclusive"] = "true";
        }
        if (series.Kind == ChartSeriesKind.StateTimeline && metadata.TryGetValue("data-cfx-range-end", out var stateEnd)) {
            if (chart.Options.XAxis.Scale == ChartScaleKind.Time) metadata["data-cfx-range-end-exclusive"] = "true";
            else if (double.TryParse(stateEnd, NumberStyles.Float, CultureInfo.InvariantCulture, out var endpoint))
                metadata["data-cfx-range-end"] = ChartMath.PreviousRepresentable(endpoint).ToString("R", CultureInfo.InvariantCulture);
        }
        if (!numericTimeDomain && metadata.TryGetValue("data-cfx-x-scale", out var xScale) && xScale == "time")
            CaptureTimeInstant(metadata, "data-cfx-x", "data-cfx-x-instant");
        if (metadata.TryGetValue("data-cfx-range-scale", out var rangeScale) && rangeScale == "time") {
            if (metadata.TryGetValue("data-cfx-range-start", out var rangeStart) && metadata.TryGetValue("data-cfx-range-end", out var rangeEnd)
                && double.TryParse(rangeStart, NumberStyles.Float, CultureInfo.InvariantCulture, out var lower)
                && double.TryParse(rangeEnd, NumberStyles.Float, CultureInfo.InvariantCulture, out var upper)
                && (numericTimeDomain || lower < 0 || upper < 0)) {
                if (metadata.TryGetValue("data-cfx-range-end-exclusive", out var exclusive) && exclusive == "true") {
                    metadata["data-cfx-range-end"] = ChartMath.PreviousRepresentable(upper).ToString("R", CultureInfo.InvariantCulture);
                    metadata.Remove("data-cfx-range-end-exclusive");
                }
            } else {
                CaptureTimeInstant(metadata, "data-cfx-range-start", "data-cfx-range-start-instant");
                CaptureTimeInstant(metadata, "data-cfx-range-end", "data-cfx-range-end-instant",
                    metadata.TryGetValue("data-cfx-range-end-exclusive", out var exclusive) && exclusive == "true");
            }
        }
    }

    private static void CaptureTimeInstant(IDictionary<string, string> metadata, string valueKey, string instantKey, bool captureInclusiveEnd = false) {
        if (!metadata.TryGetValue(valueKey, out var text) || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0) return;
        try {
            var instant = ChartDateTime.FromOrderedOaDate(value);
            metadata[instantKey] = instant.ToString("O", CultureInfo.InvariantCulture);
            // Preserve the exact native boundary while publishing the last DateTime tick inside a half-open interval.
            if (captureInclusiveEnd) metadata["data-cfx-range-inclusive-end-instant"] = instant.AddTicks(-1).ToString("O", CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is ArgumentException || exception is OverflowException) { }
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
