using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    // Browser targets are a host concern. Source identities and completed layout come from the detached scene.
    private static string PreparedChartMetadata(Chart chart, PreparedVisual prepared) {
        var fillInteriors = PreparedFillInteriors(prepared);
        var json = new StringBuilder("{\"series\":[");
        for (var index = 0; index < chart.Series.Count; index++) {
            if (index > 0) json.Append(',');
            var series = chart.Series[index];
            json.Append("{\"name\":").Append(HtmlJsonString.Encode(series.Name))
                .Append(",\"key\":").Append(HtmlJsonString.Encode(series.InteractionIdentityKey))
                .Append(",\"local\":").Append(series.Id.IsAutomatic ? "true" : "false")
                .Append(",\"kind\":").Append(HtmlJsonString.Encode(series.Kind.ToString().ToLowerInvariant()))
                .Append(",\"state\":").Append(HtmlJsonString.Encode(series.StateRole.ToString().ToLowerInvariant()))
                .Append(",\"indices\":[");
            for (var point = 0; point < series.SourcePointIndices.Count; point++) {
                if (point > 0) json.Append(',');
                json.Append(series.SourcePointIndices[point].ToString(CultureInfo.InvariantCulture));
            }
            json.Append("],\"observationIds\":[");
            var firstId = true;
            foreach (var observation in series.ObservationIds.OrderBy(observation => observation.Key)) {
                if (!firstId) json.Append(',');
                firstId = false;
                json.Append("{\"index\":").Append(observation.Key.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"id\":").Append(HtmlJsonString.Encode(observation.Value.Value)).Append('}');
            }
            json.Append("]}");
        }
        json.Append("],\"xLabels\":[");
        var categoryLabels = ChartCartesianOrientation.CategoryLabels(chart);
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
            var plot = dataProjection.Plot;
            json.Append(",\"dataProjection\":{\"x\":");
            AppendDataAxis(json, dataProjection.X);
            if (dataProjection.Y != null) {
                json.Append(",\"y\":");
                AppendDataAxis(json, dataProjection.Y);
            }
            json.Append(",\"horizontal\":").Append(dataProjection.Horizontal ? "true" : "false")
                .Append(",\"plotBounds\":{\"x\":").Append(plot.X.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"y\":").Append(plot.Y.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"width\":").Append(plot.Width.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"height\":").Append(plot.Height.ToString("R", CultureInfo.InvariantCulture)).Append("}}");
        }
        if (prepared.XProjection is ChartXProjection projection) {
            var plot = projection.PlotBounds;
            json.Append(",\"xProjection\":{\"fullMinimum\":").Append(projection.FullMinimum.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"fullMaximum\":").Append(projection.FullMaximum.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"minimum\":").Append(projection.Minimum.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"maximum\":").Append(projection.Maximum.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"reversed\":").Append(projection.Reversed ? "true" : "false")
                .Append(",\"plotBounds\":{\"x\":").Append(plot.X.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"y\":").Append(plot.Y.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"width\":").Append(plot.Width.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"height\":").Append(plot.Height.ToString("R", CultureInfo.InvariantCulture)).Append("}}");
        }
        return json.Append('}').ToString();
    }

    private static void AppendDataAxis(StringBuilder json, ChartDataAxisProjection axis) {
        json.Append("{\"minimum\":")
        .Append(axis.Minimum.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"maximum\":").Append(axis.Maximum.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"transformedMinimum\":").Append(axis.TransformedMinimum.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"transformedMaximum\":").Append(axis.TransformedMaximum.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"scale\":").Append(HtmlJsonString.Encode(axis.Scale.ToString().ToLowerInvariant()))
        .Append(",\"threshold\":").Append(axis.Threshold.ToString("R", CultureInfo.InvariantCulture))
        .Append(",\"reversed\":").Append(axis.Reversed ? "true" : "false").Append(",\"displayTicks\":[");
        for (var index = 0; index < axis.DisplayTicks.Count; index++) {
            if (index > 0) json.Append(',');
            var tick = axis.DisplayTicks[index];
            json.Append("{\"value\":").Append(tick.Value.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"text\":").Append(HtmlJsonString.Encode(tick.Text)).Append('}');
        }
        json.Append("],\"displayBounds\":[");
        for (var index = 0; index < axis.DisplayBounds.Count; index++) {
            if (index > 0) json.Append(',');
            var bound = axis.DisplayBounds[index];
            json.Append("{\"value\":").Append(bound.Value.ToString("R", CultureInfo.InvariantCulture))
                .Append(",\"text\":").Append(HtmlJsonString.Encode(bound.Text)).Append('}');
        }
        json.Append("]}");
    }
}
