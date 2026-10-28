using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    /// <summary>Transport is thin: ordered raw rows and GetSlices membership come from the same detached complete native value plan.</summary>
    private static string? PreparedNativeValueSource(PreparedVisual drawing, PreparedVisual? valueBasis = null) {
        var basis = valueBasis ?? drawing;
        if (VisualValueCapture.TrySourceValues(basis, out var captured, out var componentBasis) && captured is IVisualComponentValuePlan components)
            return PreparedNativeComponentSource(components, componentBasis);
        if (!VisualValueCapture.TryRawSource(basis, out var plan, out var complete)) return null;
        var raw = plan!; var json = new StringBuilder("{\"kind\":\"radial\",\"sourceKey\":").Append(HtmlJsonString.Encode(raw.SourceKey(complete)))
            .Append(",\"structureKey\":").Append(HtmlJsonString.Encode(raw.StructureKey)).Append(",\"readings\":[");
        for (var index = 0; index < raw.Readings.Count; index++) {
            if (index > 0) json.Append(','); var row = raw.Readings[index];
            json.Append("{\"seriesKey\":").Append(HtmlJsonString.Encode(row.SeriesKey ?? raw.SeriesKey)).Append(",\"observationId\":").Append(HtmlJsonString.Encode(row.ObservationId!))
                .Append(",\"value\":").Append(row.Value.ToString("R", CultureInfo.InvariantCulture)).Append('}');
        }
        json.Append("],\"slices\":["); var slices = raw.CaptureSlices(drawing, basis.Scene.VisibilityCapture?.View);
        for (var index = 0; index < slices.Count; index++) {
            if (index > 0) json.Append(','); var slice = slices[index];
            json.Append("{\"seriesKey\":").Append(HtmlJsonString.Encode(slice.SeriesKey)).Append(",\"pointIndex\":").Append(slice.PointIndex.ToString(CultureInfo.InvariantCulture)).Append(",\"sourceIndices\":[");
            for (var source = 0; source < slice.SourceIndices.Count; source++) { if (source > 0) json.Append(','); json.Append(slice.SourceIndices[source].ToString(CultureInfo.InvariantCulture)); }
            json.Append("],\"paintedMetadata\":");
            if (slice.PaintedMetadata == null) json.Append("null");
            else { json.Append('{'); var comma = false; foreach (var pair in slice.PaintedMetadata.OrderBy(pair => pair.Key, StringComparer.Ordinal)) {
                if (comma) json.Append(','); comma = true; json.Append(HtmlJsonString.Encode(pair.Key)).Append(':').Append(HtmlJsonString.Encode(pair.Value));
            } json.Append('}'); }
            json.Append('}');
        }
        return json.Append("]}").ToString();
    }
}
