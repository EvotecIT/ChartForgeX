using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    // Browser targets are a host concern. Source identities and completed layout come from the detached scene.
    private static string PreparedChartMetadata(Chart chart, PreparedVisual prepared) {
        var json = new StringBuilder("{\"series\":[");
        for (var index = 0; index < chart.Series.Count; index++) {
            if (index > 0) json.Append(',');
            var series = chart.Series[index];
            json.Append("{\"name\":").Append(HtmlJsonString.Encode(series.Name))
                .Append(",\"key\":").Append(HtmlJsonString.Encode(series.InteractionIdentityKey))
                .Append(",\"kind\":").Append(HtmlJsonString.Encode(series.Kind.ToString().ToLowerInvariant()))
                .Append(",\"state\":").Append(HtmlJsonString.Encode(series.StateRole.ToString().ToLowerInvariant()))
                .Append(",\"indices\":[");
            for (var point = 0; point < series.SourcePointIndices.Count; point++) {
                if (point > 0) json.Append(',');
                json.Append(series.SourcePointIndices[point].ToString(CultureInfo.InvariantCulture));
            }
            json.Append("]}");
        }
        json.Append("],\"xLabels\":[");
        for (var index = 0; index < chart.Options.XAxisLabels.Count; index++) {
            if (index > 0) json.Append(',');
            var label = chart.Options.XAxisLabels[index];
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
                .Append(",\"height\":").Append(bounds.Height.ToString("R", CultureInfo.InvariantCulture)).Append('}');
        }
        return json.Append("]}").ToString();
    }
}
