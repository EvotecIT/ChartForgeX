using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    // Browser targets are a host concern. Source identities and completed layout come from the detached scene.
    private static string PreparedChartMetadata(Chart chart, PreparedVisual prepared) {
        var reflow = prepared.Scene.VisibilityCapture?.ReflowSource;
        var fillInteriors = PreparedFillInteriors(prepared);
        var json = new StringBuilder("{\"series\":[");
        for (var index = 0; index < chart.Series.Count; index++) {
            if (index > 0) json.Append(',');
            var series = chart.Series[index];
            var retained = reflow?.SeriesRows[index];
            var indices = prepared.Scene.VisibilityCapture?.RenderedSourceIndices(index) ?? series.SourcePointIndices;
            var observationIds = retained?.ObservationIds ?? series.ObservationIds;
            json.Append("{\"name\":").Append(HtmlJsonString.Encode(retained?.Name ?? series.Name))
                .Append(",\"key\":").Append(HtmlJsonString.Encode(retained?.Id.Value ?? series.InteractionIdentityKey))
                .Append(",\"local\":").Append(retained == null && series.Id.IsAutomatic ? "true" : "false")
                .Append(",\"kind\":").Append(HtmlJsonString.Encode((retained?.Kind ?? series.Kind).ToString().ToLowerInvariant()))
                .Append(",\"state\":").Append(HtmlJsonString.Encode((retained?.State ?? series.StateRole).ToString().ToLowerInvariant()))
                .Append(",\"indices\":[");
            for (var point = 0; point < indices.Count; point++) {
                if (point > 0) json.Append(',');
                json.Append(indices[point].ToString(CultureInfo.InvariantCulture));
            }
            json.Append("],\"observationIds\":[");
            var firstId = true;
            foreach (var observation in observationIds.OrderBy(observation => observation.Key)) {
                if (!firstId) json.Append(',');
                firstId = false;
                json.Append("{\"index\":").Append(observation.Key.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"id\":").Append(HtmlJsonString.Encode(observation.Value.Value)).Append('}');
            }
            json.Append("]}");
        }
        json.Append(']');
        AppendPreparedVisibility(json, prepared);
        var nativeValues = PreparedNativeValueSource(prepared);
        if (nativeValues != null) json.Append(",\"nativeValueSource\":").Append(nativeValues);
        AppendPreparedAnnotations(json, prepared);
        AppendPreparedHierarchy(json, prepared);
        json.Append(",\"xLabels\":[");
        var categoryLabels = reflow?.CategoryLabels ?? ChartCartesianOrientation.CategoryLabels(chart);
        for (var index = 0; index < categoryLabels.Count; index++) {
            if (index > 0) json.Append(',');
            var label = categoryLabels[index];
            json.Append("{\"value\":").Append(label.Value.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"text\":").Append(HtmlJsonString.Encode(label.Text)).Append('}');
        }
        json.Append("],\"regions\":[");
        for (var index = 0; index < prepared.Regions.Count; index++) {
            if (index > 0) json.Append(',');
            var region = prepared.Regions[index]; var bounds = region.Bounds;
            json.Append("{\"id\":").Append(HtmlJsonString.Encode(region.Id))
                .Append(",\"role\":").Append(HtmlJsonString.Encode(region.Role))
                .Append(",\"label\":").Append(HtmlJsonString.Encode(region.Label ?? string.Empty))
                .Append(",\"x\":").Append(bounds.X.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"y\":").Append(bounds.Y.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"width\":").Append(bounds.Width.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"height\":").Append(bounds.Height.ToString("R", CultureInfo.InvariantCulture));
            if (fillInteriors.TryGetValue(region.Id, out var interiors)) {
                json.Append(",\"fillInteriors\":[");
                for (var candidate = 0; candidate < interiors.Count; candidate++) {
                    if (candidate > 0) json.Append(',');
                    var box = interiors[candidate];
                    json.Append('[').Append((box.X + box.Width / 2).ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append((box.Y + box.Height / 2).ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append((box.Width / 2).ToString("R", CultureInfo.InvariantCulture)).Append(']');
                }
                json.Append(']');
            }
            json.Append('}');
        }
        json.Append(']');
        if (prepared.Scene.DataProjection is ChartDataProjection dataProjection) {
            if (dataProjection.CategoryLabels != null) {
                json.Append(",\"categoryLabels\":[");
                for (var index = 0; index < dataProjection.CategoryLabels.Count; index++) {
                    if (index > 0) json.Append(',');
                    var label = dataProjection.CategoryLabels[index];
                    json.Append("{\"value\":").Append(label.Value.ToString("R", CultureInfo.InvariantCulture))
                        .Append(",\"text\":").Append(HtmlJsonString.Encode(label.Text));
                    if (label.Groups.Count > 0) {
                        json.Append(",\"groups\":[");
                        for (var group = 0; group < label.Groups.Count; group++) {
                            if (group > 0) json.Append(',');
                            json.Append(HtmlJsonString.Encode(label.Groups[group]));
                        }
                        json.Append(']');
                    }
                    json.Append('}');
                }
                json.Append(']');
            }
            var plot = dataProjection.Plot;
            json.Append(",\"dataProjection\":{");
            if (dataProjection.HasX) { json.Append("\"x\":"); AppendDataAxis(json, dataProjection.X); }
            if (dataProjection.Y != null) {
                json.Append(dataProjection.HasX ? ",\"y\":" : "\"y\":");
                AppendDataAxis(json, dataProjection.Y);
            }
            json.Append(",\"horizontal\":").Append(dataProjection.Horizontal ? "true" : "false")
                .Append(",\"plotBounds\":{\"x\":").Append(plot.X.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"y\":").Append(plot.Y.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"width\":").Append(plot.Width.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"height\":").Append(plot.Height.ToString("R", CultureInfo.InvariantCulture)).Append("}}");
        }
        if (prepared.CartesianOrientation == ChartOrientation.Horizontal) json.Append(",\"orientation\":\"horizontal\"");
        if (prepared.CartesianViewport?.ValueDomain == ChartValueDomain.VisibleCategories)
            json.Append(",\"valueDomain\":\"visibleCategories\"");
        AppendPreparedPolar(json, prepared);
        AppendCapturedAxis(json, "xProjection", prepared.XProjection);
        AppendCapturedAxis(json, "yProjection", prepared.YProjection);
        AppendCapturedAxis(json, "secondaryXProjection", prepared.SecondaryXProjection);
        AppendCapturedAxis(json, "secondaryYProjection", prepared.SecondaryYProjection);
        return json.Append('}').ToString();
    }

    private static void AppendCapturedAxis(StringBuilder json, string name, ChartAxisProjection? axis) {
        if (axis == null) return;
        json.Append(',').Append(HtmlJsonString.Encode(name)).Append(':');
        AppendViewportAxis(json, axis.FullMinimum, axis.FullMaximum, axis.Minimum, axis.Maximum,
            axis.TransformedFullMinimum, axis.TransformedFullMaximum, axis.TransformedMinimum, axis.TransformedMaximum,
            axis.Scale, axis.Threshold, axis.Reversed, axis.PlotBounds, axis.TimeSessions, axis.LogarithmBase, axis.LogarithmicDomain, axis.TimeDomain, axis.MaximumInclusive);
    }

    private static void AppendViewportAxis(StringBuilder json, double fullMinimum, double fullMaximum, double minimum, double maximum,
        double transformedFullMinimum, double transformedFullMaximum, double transformedMinimum, double transformedMaximum,
        ChartScaleKind scale, double threshold, bool reversed, ChartForgeX.Primitives.ChartRect plot, ChartTimeSessionDomain? sessions,
        double logarithmBase = 10, ChartLogarithmicDomain logarithmicDomain = ChartLogarithmicDomain.Positive, ChartTimeSpacingDomain? timeDomain = null, bool maximumInclusive = true,
        double? innerRadius = null, double? outerRadius = null) {
        json.Append("{\"fullMinimum\":").Append(fullMinimum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"fullMaximum\":").Append(fullMaximum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"minimum\":").Append(minimum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"maximum\":").Append(maximum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"transformedMinimum\":").Append(transformedMinimum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"transformedMaximum\":").Append(transformedMaximum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"transformedFullMinimum\":").Append(transformedFullMinimum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"transformedFullMaximum\":").Append(transformedFullMaximum.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"scale\":").Append(HtmlJsonString.Encode(scale.ToString().ToLowerInvariant()))
            .Append(",\"threshold\":").Append(threshold.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"reversed\":").Append(reversed ? "true" : "false");
        if (!maximumInclusive) json.Append(",\"maximumInclusive\":false");
        if (innerRadius.HasValue && outerRadius.HasValue)
            json.Append(",\"innerRadius\":").Append(innerRadius.Value.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"outerRadius\":").Append(outerRadius.Value.ToString("R", CultureInfo.InvariantCulture));
        AppendLogarithmicPolicy(json, logarithmBase, logarithmicDomain);
        AppendTimeSessions(json, sessions);
        AppendTimeSpacing(json, timeDomain);
        json.Append(",\"plotBounds\":{\"x\":").Append(plot.X.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"y\":").Append(plot.Y.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"width\":").Append(plot.Width.ToString("R", CultureInfo.InvariantCulture))
            .Append(",\"height\":").Append(plot.Height.ToString("R", CultureInfo.InvariantCulture)).Append("}}");
    }

    private static void AppendDataAxis(StringBuilder json, ChartDataAxisProjection axis) {
        json.Append("{\"minimum\":")
        .Append(axis.Minimum.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"maximum\":").Append(axis.Maximum.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"transformedMinimum\":").Append(axis.TransformedMinimum.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"transformedMaximum\":").Append(axis.TransformedMaximum.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"scale\":").Append(HtmlJsonString.Encode(axis.Scale.ToString().ToLowerInvariant()))
        .Append(",\"threshold\":").Append(axis.Threshold.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"reversed\":").Append(axis.Reversed ? "true" : "false");
        if (!axis.MaximumInclusive) json.Append(",\"maximumInclusive\":false");
        AppendLogarithmicPolicy(json, axis.LogarithmBase, axis.LogarithmicDomain);
        AppendTimeSessions(json, axis.TimeSessions);
        AppendTimeSpacing(json, axis.TimeDomain);
        json.Append(",\"displayTicks\":[");
        for (var index = 0; index < axis.DisplayTicks.Count; index++) {
            if (index > 0) json.Append(',');
            var tick = axis.DisplayTicks[index];
            json.Append("{\"value\":").Append(tick.Value.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"text\":").Append(HtmlJsonString.Encode(tick.Text));
            if (tick.Instant != null) json.Append(",\"instant\":").Append(HtmlJsonString.Encode(tick.Instant));
            json.Append('}');
        }
        json.Append("],\"displayBounds\":[");
        for (var index = 0; index < axis.DisplayBounds.Count; index++) {
            if (index > 0) json.Append(',');
            var bound = axis.DisplayBounds[index];
            json.Append("{\"value\":").Append(bound.Value.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"text\":").Append(HtmlJsonString.Encode(bound.Text));
            if (bound.Instant != null) json.Append(",\"instant\":").Append(HtmlJsonString.Encode(bound.Instant));
            json.Append('}');
        }
        json.Append("]}");
    }

    private static void AppendLogarithmicPolicy(StringBuilder json, double logarithmBase, ChartLogarithmicDomain logarithmicDomain) {
        // The ordinary base-ten positive representation remains byte-identical.
        if (logarithmBase != 10) json.Append(",\"logarithmBase\":").Append(logarithmBase.ToString("R", CultureInfo.InvariantCulture));
        if (logarithmicDomain != ChartLogarithmicDomain.Positive)
            json.Append(",\"logarithmicDomain\":").Append(HtmlJsonString.Encode(logarithmicDomain.ToString().ToLowerInvariant()));
    }

    private static void AppendTimeSpacing(StringBuilder json, ChartTimeSpacingDomain? domain) {
        if (domain == null) return;
        json.Append(",\"timeSpacing\":{\"kind\":").Append(HtmlJsonString.Encode(domain.Unit ? "unit" : "ordinal")).Append(",\"instants\":[");
        for (var index = 0; index < domain.Instants.Count; index++) {
            if (index > 0) json.Append(',');
            json.Append(domain.Instants[index].ToString("R", CultureInfo.InvariantCulture));
        }
        json.Append("]}");
    }

    private static void AppendTimeSessions(StringBuilder json, ChartTimeSessionDomain? domain) {
        if (domain == null) return;
        json.Append(",\"timeSessions\":[");
        for (var index = 0; index < domain.Sessions.Count; index++) {
            if (index > 0) json.Append(',');
            var session = domain.Sessions[index];
            json.Append("{\"minimum\":").Append(session.Minimum.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"maximum\":").Append(session.Maximum.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"offset\":").Append(domain.Offsets[index].ToString("R", CultureInfo.InvariantCulture)).Append('}');
        }
        json.Append(']');
    }
}
