using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    /// <summary>Includes detached callback paint in the finite source identity, including currently hidden observations.</summary>
    private static void VisibilitySourceItemStyles(VisualArtifactInterchangeJsonWriter writer, ChartVisibilitySource.Series source) {
        if (!source.ItemStyles.Any(style => style != null && (style.Color.HasValue || style.Stroke != null || style.MarkerSnapshot != null))) return;
        writer.Property("itemStyles"); writer.StartArray();
        foreach (var style in source.ItemStyles) {
            writer.StartObject();
            if (style != null) {
                OptionalString(writer, "color", WaterfallColor(style.Color));
                VisibilitySourceStroke(writer, "stroke", style.Stroke);
                if (style.MarkerSnapshot is { } marker) VisibilitySourceMarker(writer, marker);
            }
            writer.EndObject();
        }
        writer.EndArray();
    }
}
