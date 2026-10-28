using System.Globalization;
using System.Text;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    /// <summary>Only already-captured typed native facts cross this adapter boundary; no tuple geometry is reconstructed.</summary>
    private static string PreparedNativeComponentSource(IVisualComponentValuePlan plan, PreparedVisual complete) {
        var json = new StringBuilder("{\"kind\":").Append(HtmlJsonString.Encode(plan.SourceFamily))
            .Append(",\"sourceKey\":").Append(HtmlJsonString.Encode(plan.SourceKey(complete)))
            .Append(",\"structureKey\":").Append(HtmlJsonString.Encode(plan.StructureKey));
        if (plan.SizePolicy is { } sizes) json.Append(",\"mapping\":").Append(HtmlJsonString.Encode(sizes.Mapping)).Append(",\"reversed\":").Append(sizes.Reversed ? "true" : "false");
        json.Append(",\"readings\":[");
        for (var index = 0; index < plan.Readings.Count; index++) {
            if (index > 0) json.Append(','); var row = plan.Readings[index];
            json.Append("{\"seriesKey\":").Append(HtmlJsonString.Encode(row.SeriesKey ?? plan.SeriesKey))
                .Append(",\"observationId\":").Append(HtmlJsonString.Encode(row.ObservationId!)).Append(",\"component\":").Append(HtmlJsonString.Encode(row.Component!))
                .Append(",\"value\":").Append(N(row.Value)).Append(",\"minimum\":").Append(N(row.Minimum)).Append(",\"maximum\":").Append(N(row.Maximum)).Append('}');
        }
        return json.Append("]}").ToString();
        string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
