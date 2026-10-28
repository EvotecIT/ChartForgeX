using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

public static partial class ChartArtifactRendering {
    /// <summary>Refreshes only reserved numeric identity/radial facts; prepared envelopes remain detached snapshots.</summary>
    internal static void WriteSeriesMetadata(Chart chart, IDictionary<string, string> target) {
        foreach (var key in target.Keys.Where(IsOwnedSeriesFact).ToArray()) target.Remove(key);
        target["chart.series"] = chart.Series.Count.ToString(CultureInfo.InvariantCulture);
        var bands = ChartRadialBandResolver.Resolve(chart);
        if (chart.Options.RadialGeometry.ValueEncoding == ChartRadialValueEncoding.Area)
            target["chart.radial.valueEncoding"] = "area";
        var sourceFactCount = chart.Series.Count(series => NeedsRadialSourceFacts(chart, series));
        if (sourceFactCount > 0 && (long)target.Count + chart.Series.Count * 2L + bands.Count(band => band != null) * 2L
            + sourceFactCount + (chart.Options.RadialBandAllocation != null ? 3 : 1) > VisualArtifactInterchangeValidation.MaximumExtensionEntries)
            throw new ArgumentException("Radial source facts exceed the common interchange entry budget.");
        if (sourceFactCount > 0) {
            long sourceValues = sourceFactCount;
            foreach (var series in chart.Series.Where(series => NeedsRadialSourceFacts(chart, series))) {
                if (series.Id.Value.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters)
                    throw new ArgumentException("Radial series identity exceeds the common identity budget: source series '" + series.Name + "'.");
                sourceValues += (long)series.ObservationCount * (HasMultiRingOffsets(chart, series) ? 7 : 6);
                if (sourceValues > VisualArtifactInterchangeValidation.MaximumJsonValues)
                    throw new ArgumentException("Radial source facts exceed the common interchange aggregate value budget.");
            }
        }
        var remainingCharacters = (long)VisualArtifactInterchangeValidation.MaximumJsonCharacters
            - target.Sum(pair => (long)pair.Key.Length + pair.Value.Length);
        if (chart.Options.RadialBandAllocation is { } allocation) {
            target["chart.radial.auto.innerRatio"] = allocation.InnerRatio.ToString("R", CultureInfo.InvariantCulture);
            target["chart.radial.auto.gapRatio"] = allocation.GapRatio.ToString("R", CultureInfo.InvariantCulture);
        }
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            var prefix = "chart.series." + index.ToString(CultureInfo.InvariantCulture);
            target[prefix + ".id"] = series.Id.Value;
            target[prefix + ".scope"] = series.Id.IsAutomatic ? "local" : "shared";
            if (bands[index] is { } band) {
                target[prefix + ".radial.innerRatio"] = band.InnerRatio.ToString("R", CultureInfo.InvariantCulture);
                target[prefix + ".radial.outerRatio"] = band.OuterRatio.ToString("R", CultureInfo.InvariantCulture);
            }
            if (NeedsRadialSourceFacts(chart, series)) {
                var facts = SourceFacts(series, remainingCharacters, HasMultiRingOffsets(chart, series));
                remainingCharacters -= facts.Length;
                target[prefix + ".radial.sourceFacts"] = facts;
            }
        }
        WritePiePresentationFacts(chart, target);
        WriteScalarMetadata(chart, target);
        if (chart.Series.Count > 0) target["chart.kind"] = chart.Series[0].Kind.ToString();
    }

    internal static bool IsRadialFact(string key) => key.StartsWith("chart.radial.", StringComparison.Ordinal)
        || key.StartsWith("chart.series.", StringComparison.Ordinal) && key.IndexOf(".radial.", StringComparison.Ordinal) >= 0;

    private static bool IsOwnedSeriesFact(string key) {
        if (key is "chart.series" or "chart.kind" || key.StartsWith("chart.radial.", StringComparison.Ordinal) || IsScalarFact(key)) return true;
        const string prefix = "chart.series.";
        if (!key.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var split = key.IndexOf('.', prefix.Length);
        if (split < 0 || !int.TryParse(key.Substring(prefix.Length, split - prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out _)) return false;
        var field = key.Substring(split + 1);
        return field is "id" or "scope" || field.StartsWith("radial.", StringComparison.Ordinal);
    }

    private static bool HasMultiRingOffsets(Chart chart, ChartSeries series) => chart.Series.Count > 1
        && ChartSeriesKindTraits.IsPieLikeKind(series.Kind) && series.PointSliceOffsets.Any(value => value > 0);

    private static bool NeedsRadialSourceFacts(Chart chart, ChartSeries series) => chart.Options.RadialBandAllocation != null
        || series.HasSliceRadiusOverrides || HasMultiRingOffsets(chart, series);

    private static string SourceFacts(ChartSeries series, long remainingCharacters, bool includeOffsets) {
        // Reuse the canonical writer, including escaping and aggregate value limits. This is
        // a bounded metadata fact array; it introduces no typed Chart family or importer.
        var writer = new VisualArtifactInterchangeJsonWriter(); writer.StartArray();
        for (var index = 0; index < series.ObservationCount; index++) {
            var point = series.RenderPoints[index]; writer.StartObject();
            writer.Property("sourceIndex"); writer.Number(index);
            var observationId = series.ObservationIds.TryGetValue(index, out var id) ? id.Value : null;
            if (observationId != null && observationId.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters)
                throw new ArgumentException("Radial observation identity at source index " + index.ToString(CultureInfo.InvariantCulture) + " exceeds the common identity budget.");
            writer.Property("observationId"); writer.String(observationId);
            writer.Property("x"); writer.Number(point.X); writer.Property("value"); writer.Number(point.Y);
            writer.Property("radiusRatio");
            if (series.SliceRadiusRatio(index) is { } ratio) writer.Number(ratio); else writer.String(null);
            if (includeOffsets) { writer.Property("offsetRatio"); writer.Number(index < series.PointSliceOffsets.Count ? series.PointSliceOffsets[index] : 0); }
            writer.EndObject();
            if (writer.CharactersWritten + 1 > Math.Min(remainingCharacters, VisualArtifactInterchangeValidation.MaximumTextCharacters))
                throw new ArgumentException("Radial source facts exceed the common interchange text budget; retain the typed source model or export a bounded dataset.");
        }
        writer.EndArray(); return writer.ToString();
    }
}
