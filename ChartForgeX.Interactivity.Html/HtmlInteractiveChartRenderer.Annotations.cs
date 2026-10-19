using System.Text;
using ChartForgeX.Rendering;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    // Native preparation owns these bindings. Do not reread mutable annotation models after layout.
    private static void AppendPreparedAnnotations(StringBuilder json, PreparedVisual prepared) {
        var annotations = prepared.Scene.Annotations;
        if (annotations.Count == 0) return;
        json.Append(",\"annotations\":[");
        for (var index = 0; index < annotations.Count; index++) {
            if (index > 0) json.Append(',');
            var annotation = annotations[index];
            json.Append("{\"id\":").Append(HtmlJsonString.Encode(annotation.AnnotationId))
                .Append(",\"sourceId\":").Append(HtmlJsonString.Encode(annotation.SourceId))
                .Append(",\"kind\":").Append(HtmlJsonString.Encode(annotation.Kind)).Append('}');
        }
        json.Append(']');
    }
}
