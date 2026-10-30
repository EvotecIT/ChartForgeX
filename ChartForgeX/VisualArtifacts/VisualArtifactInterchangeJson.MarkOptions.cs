using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    /// <summary>Includes host contracts in detached replay identity without changing metadata-free source keys.</summary>
    private static void VisibilitySourceMarkOptions(VisualArtifactInterchangeJsonWriter writer, ChartVisibilitySource.Series source) {
        var any = source.MarkOptions != null || source.RowOptions != null;
        foreach (var point in source.Points) any |= point.MarkOptions != null;
        foreach (var range in source.Ranges) any |= range.MarkOptions != null;
        if (!any) return;
        writer.Property("markOptions"); writer.StartObject();
        WriteMarkOptions(writer, "series", source.MarkOptions);
        WriteMarkOptions(writer, "row", source.RowOptions);
        writer.Property("points"); writer.StartArray();
        foreach (var point in source.Points) {
            writer.StartObject(); WriteMarkOptions(writer, "options", point.MarkOptions); writer.EndObject();
        }
        writer.EndArray();
        writer.Property("ranges"); writer.StartArray();
        foreach (var range in source.Ranges) {
            writer.StartObject(); WriteMarkOptions(writer, "options", range.MarkOptions); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
    }

    private static void WriteMarkOptions(VisualArtifactInterchangeJsonWriter writer, string name, ChartMarkOptions? options) {
        if (options == null) return;
        writer.Property(name); writer.StartObject();
        OptionalString(writer, "href", options.Href); String(writer, "target", options.LinkTarget.ToString());
        OptionalString(writer, "tooltip", options.Tooltip); OptionalString(writer, "accessibleLabel", options.AccessibleLabel);
        OptionalString(writer, "class", options.CssClass); Boolean(writer, "current", options.Current); Boolean(writer, "selected", options.Selected);
        writer.Property("data"); writer.StartObject();
        foreach (var attribute in options.DataAttributes) String(writer, attribute.Key, attribute.Value);
        writer.EndObject();
        if (options.Keys.Count > 0) {
            writer.Property("keys"); writer.StartArray();
            foreach (var key in options.Keys) writer.String(key);
            writer.EndArray();
        }
        if (options.Fields.Count > 0) String(writer, "fields", ChartLinkRecord.FieldsJson(options.Fields));
        writer.EndObject();
    }
}
