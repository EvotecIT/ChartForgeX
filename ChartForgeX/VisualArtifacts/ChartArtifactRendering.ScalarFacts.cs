using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

public static partial class ChartArtifactRendering {
    internal static bool IsScalarFact(string key) => key.StartsWith("chart.gauge.", StringComparison.Ordinal)
        || key.StartsWith("chart.bullet.", StringComparison.Ordinal)
        || key.StartsWith("chart.series.", StringComparison.Ordinal) && key.EndsWith(".scalar.sourceFacts", StringComparison.Ordinal);

    /// <summary>Captures bounded descriptive scalar facts. These extensions accompany SVG fallback; they are not an editable chart schema.</summary>
    private static void WriteScalarMetadata(Chart chart, IDictionary<string, string> target) {
        if (chart.Series.Any(series => series.Kind == ChartSeriesKind.Gauge)) {
            var options = chart.Options.Gauge; var writer = new VisualArtifactInterchangeJsonWriter(); writer.StartObject();
            writer.Property("form"); writer.String(options.Form.ToString());
            writer.Property("orientation"); writer.String(options.Orientation.ToString());
            writer.Property("startAngleDegrees"); writer.Number(options.StartAngleDegrees);
            writer.Property("sweepAngleDegrees"); writer.Number(options.SweepAngleDegrees);
            writer.Property("bandGap"); writer.Number(options.BandGap);
            writer.Property("caption"); writer.String(options.Caption);
            writer.Property("tickCount"); if (options.TickCount.HasValue) writer.Number(options.TickCount.Value); else writer.String(null);
            writer.Property("tickLabels"); writer.StartArray();
            foreach (var label in options.TickLabels) { writer.StartObject(); writer.Property("value"); writer.Number(label.Value); writer.Property("text"); writer.String(label.Text); writer.EndObject(); }
            writer.EndArray();
            writer.Property("needleColor"); writer.String(options.NeedleColor?.ToCss());
            writer.Property("bands"); writer.StartArray();
            foreach (var band in options.Bands) {
                if (band == null) throw new ArgumentException("Gauge bands must not contain null.");
                writer.StartObject(); writer.Property("minimum"); writer.Number(band.Minimum); writer.Property("maximum"); writer.Number(band.Maximum);
                writer.Property("state"); writer.String(band.State.ToString()); writer.Property("color"); writer.String(band.Color?.ToCss());
                writer.Property("opacity"); writer.Number(band.Opacity); writer.EndObject();
            }
            writer.EndArray(); writer.EndObject(); AddScalarFact(target, "chart.gauge.options", writer.ToString());
        }
        if (chart.Series.Any(series => series.Kind == ChartSeriesKind.Bullet)) {
            AddScalarFact(target, "chart.bullet.orientation", chart.Options.Bullet.Orientation.ToString());
            AddScalarFact(target, "chart.bullet.independentAxes", chart.Options.Bullet.IndependentAxes ? "true" : "false");
        }
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.Kind is not (ChartSeriesKind.Gauge or ChartSeriesKind.Bullet or ChartSeriesKind.ProgressBar)) continue;
            var writer = new VisualArtifactInterchangeJsonWriter(); writer.StartArray();
            for (var index = 0; index < series.ObservationCount; index++) {
                writer.StartObject(); writer.Property("sourceIndex"); writer.Number(index);
                var identity = series.ObservationIds.TryGetValue(index, out var id) ? id.Value : null;
                if (identity?.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters) throw new ArgumentException("Scalar observation identity exceeds the common identity budget.");
                writer.Property("observationId"); writer.String(identity);
                if (series.Kind == ChartSeriesKind.Gauge) {
                    var value = series.GaugeValues[index]; ScalarReading(writer, value.Value, value.Minimum, value.Maximum);
                    writer.Property("targets"); writer.StartArray();
                    foreach (var reference in value.Targets) { writer.StartObject(); writer.Property("value"); writer.Number(reference.Value); writer.Property("label"); writer.String(reference.Label); writer.EndObject(); }
                    writer.EndArray();
                } else if (series.Kind == ChartSeriesKind.Bullet) {
                    var value = series.BulletValues[index]; ScalarReading(writer, value.Value, value.Minimum, value.Maximum);
                    writer.Property("target"); writer.Number(value.Target); writer.Property("rangeEnds"); writer.StartArray();
                    foreach (var boundary in value.RangeEnds) writer.Number(boundary); writer.EndArray();
                    if (series.BulletAxis is { } axis) {
                        writer.Property("axis"); writer.StartObject(); writer.Property("minimum"); if (axis.Minimum.HasValue) writer.Number(axis.Minimum.Value); else writer.String(null);
                        writer.Property("maximum"); if (axis.Maximum.HasValue) writer.Number(axis.Maximum.Value); else writer.String(null);
                        writer.Property("scale"); writer.String(axis.Scale.ToString()); writer.Property("reversed"); writer.Boolean(axis.Reversed);
                        writer.Property("logarithmBase"); writer.Number(axis.LogarithmBase); writer.Property("logarithmicDomain"); writer.String(axis.LogarithmicDomain.ToString());
                        writer.Property("symmetricLogarithmThreshold"); writer.Number(axis.SymmetricLogarithmThreshold);
                        writer.Property("tickCount"); writer.Number(axis.TickCount); writer.Property("visible"); writer.Boolean(axis.Visible);
                        writer.Property("labels"); writer.StartArray();
                        foreach (var label in axis.Labels) { writer.StartObject(); writer.Property("value"); writer.Number(label.Value); writer.Property("text"); writer.String(label.Text); writer.EndObject(); }
                        writer.EndArray(); writer.EndObject();
                    }
                } else {
                    var item = series.IsTypedProgress ? series.ProgressItems[index] : (ChartProgressItem?)null;
                    ScalarReading(writer, item?.Value ?? series.RenderPoints[index].Y, item?.Minimum ?? 0, item?.Maximum ?? chart.Options.ProgressMaximum);
                    writer.Property("label"); writer.String(item?.Label ?? ChartForgeX.Rendering.ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxisLabels, series.RenderPoints[index].X));
                    writer.Property("color"); writer.String(item?.Color?.ToCss());
                }
                writer.EndObject();
                if (writer.CharactersWritten + 1 > VisualArtifactInterchangeValidation.MaximumTextCharacters) throw new ArgumentException("Scalar source facts exceed the common interchange text budget; export a bounded dataset.");
            }
            writer.EndArray(); AddScalarFact(target, "chart.series." + seriesIndex.ToString(CultureInfo.InvariantCulture) + ".scalar.sourceFacts", writer.ToString());
        }
    }

    private static void ScalarReading(VisualArtifactInterchangeJsonWriter writer, double value, double minimum, double maximum) {
        writer.Property("value"); writer.Number(value); writer.Property("minimum"); writer.Number(minimum); writer.Property("maximum"); writer.Number(maximum);
    }

    private static void AddScalarFact(IDictionary<string, string> target, string key, string value) {
        if (value.Length > VisualArtifactInterchangeValidation.MaximumTextCharacters || target.Count >= VisualArtifactInterchangeValidation.MaximumExtensionEntries
            || target.Sum(pair => (long)pair.Key.Length + pair.Value.Length) + key.Length + value.Length > VisualArtifactInterchangeValidation.MaximumJsonCharacters)
            throw new ArgumentException("Scalar source facts exceed the common interchange metadata budget.");
        target.Add(key, value);
    }
}
