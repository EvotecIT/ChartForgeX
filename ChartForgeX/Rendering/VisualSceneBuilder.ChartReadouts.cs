using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    private static readonly string[] CoordinateReadoutMembers = { "x-lower", "x-upper" };
    private static readonly string[] ValueReadoutMembers = {
        "lower", "upper", "y-lower", "y-upper", "first", "second", "delta",
        "minimum", "q1", "median", "q3", "maximum",
        "source-minimum", "source-maximum", "inlier-minimum", "inlier-maximum",
        "open", "high", "low", "close"
    };

    // Capture display text while the renderer's final axis formatter/cache is available.
    // Exporters and browser hosts never re-run user callbacks or guess a culture/date format.
    internal void CaptureChartReadouts(Chart chart, Func<ChartSeries, double, string> category,
        Func<ChartSeries, double, string?, string> value, Func<ChartSeries, double, string>? displayCategory = null) {
        Dictionary<string, int>? regionIndices = null;
        for (var index = 0; index < _nodes.Count; index++) {
            if (_nodes[index] is not VisualSceneGroup group || !group.Metadata.ContainsKey("data-cfx-point")
                || !group.Metadata.TryGetValue("data-cfx-series", out var ordinal)
                || !int.TryParse(ordinal, NumberStyles.None, CultureInfo.InvariantCulture, out var seriesIndex)
                || seriesIndex < 0 || seriesIndex >= chart.Series.Count) continue;
            var series = chart.Series[seriesIndex];
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in group.Metadata) metadata.Add(entry.Key, entry.Value);
            bool Number(string key, out double number) {
                number = 0;
                return metadata.TryGetValue(key, out var text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                    && !double.IsNaN(number) && !double.IsInfinity(number);
            }
            string Value(string key, double number) => value(series, number,
                metadata.TryGetValue(key.Replace("data-cfx-", "data-cfx-formatted-"), out var captured) ? captured : null);
            if (Number("data-cfx-x", out var x)) {
                var formatted = category(series, x);
                metadata["data-cfx-formatted-x"] = formatted;
                metadata["data-cfx-display-label"] = metadata.TryGetValue("data-cfx-category-label", out var label) ? label
                    : displayCategory?.Invoke(series, x) ?? formatted;
            }
            foreach (var member in CoordinateReadoutMembers)
                if (Number("data-cfx-" + member, out var bound)) metadata["data-cfx-formatted-" + member] = category(series, bound);
            var valueKey = series.Kind == ChartSeriesKind.BoxPlot && metadata.ContainsKey("data-cfx-median") ? "data-cfx-median"
                : metadata.ContainsKey("data-cfx-value") ? "data-cfx-value" : "data-cfx-y";
            if (Number(valueKey, out var y)) metadata["data-cfx-formatted-value"] = Value(valueKey, y);
            foreach (var member in ValueReadoutMembers) {
                var key = "data-cfx-" + member;
                if (Number(key, out var number)) {
                    // The primary member and its value alias share one captured result.
                    metadata["data-cfx-formatted-" + member] = key == valueKey
                        ? metadata["data-cfx-formatted-value"] : Value(key, number);
                }
            }
            // Bin display uses the actual upper boundary; its closed logic endpoint remains exact.
            var lowerKey = series.HistogramBinLayout != null ? "data-cfx-bin-lower" : "data-cfx-start";
            var upperKey = series.HistogramBinLayout != null ? "data-cfx-bin-upper" : "data-cfx-end";
            if (Number(lowerKey, out var start) && Number(upperKey, out var end)) {
                // Cartesian comparison bounds are values, while schedule and bin bounds are X/category coordinates.
                var valueEndpoints = series.Kind is ChartSeriesKind.RangeBar or ChartSeriesKind.Waterfall or ChartSeriesKind.Dumbbell;
                metadata["data-cfx-formatted-start"] = valueEndpoints ? Value(lowerKey, start) : category(series, start);
                metadata["data-cfx-formatted-end"] = valueEndpoints ? Value(upperKey, end) : category(series, end);
                metadata["data-cfx-range-label"] = RangeLabel(metadata["data-cfx-formatted-start"], metadata["data-cfx-formatted-end"]);
                if (series.HistogramBinLayout != null) metadata["data-cfx-display-label"] = metadata["data-cfx-range-label"];
            } else if (series.TimeBucketInterval != null && Number("data-cfx-x", out var bucketStart)) {
                metadata["data-cfx-range-label"] = RangeLabel(category(series, bucketStart),
                    category(series, ChartTimeScale.BucketEnd(chart.Options.XAxis, bucketStart, series.TimeBucketInterval)));
            }
            // A dimensionless factor must not inherit a measurement axis's units or culture.
            if (Number("data-cfx-iqr-multiplier", out var multiplier))
                metadata["data-cfx-formatted-iqr-multiplier"] = ChartNumericFormatter.FormatCoordinate(multiplier);
            if (group.Role == "point" && BoxPlotReadoutDescription(series, metadata) is { } description) {
                metadata["aria-label"] = description;
                if (regionIndices == null) {
                    regionIndices = new Dictionary<string, int>(StringComparer.Ordinal);
                    for (var regionIndex = 0; regionIndex < _regions.Count; regionIndex++)
                        if (_regions[regionIndex].Role == "point") regionIndices[_regions[regionIndex].Id] = regionIndex;
                }
                if (regionIndices.TryGetValue(group.Id!, out var region)) {
                    var previous = _regions[region];
                    _regions[region] = new VisualSemanticRegion(previous.Id, previous.Role, previous.Bounds, description);
                }
            }
            _nodes[index] = new VisualSceneGroup(group.Role, group.Id, group.Clip, metadata,
                group.PathClip, group.Rotation, group.Href, group.Tooltip, group.Translation, group.ImageResource, group.ColorScope);
        }
    }

    private static string RangeLabel(string start, string end) => start.Length == 0 && end.Length == 0 ? string.Empty : start + " – " + end;
}
