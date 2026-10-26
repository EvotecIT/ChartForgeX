using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    /// <summary>Only opt-in typed-source policy uses this finite projection; ordinary envelope families stay unchanged.</summary>
    private static void VisibilitySourceBubble(VisualArtifactInterchangeJsonWriter writer, ChartBubbleOptions source) {
        writer.Property("bubblePolicy"); writer.StartObject();
        String(writer, "encoding", source.SizeEncoding.ToString()); Boolean(writer, "reversed", source.Reversed);
        Number(writer, "minimumRadius", source.MinimumRadius); OptionalNumber(writer, "maximumRadius", source.MaximumRadius);
        OptionalNumber(writer, "minimumValue", source.MinimumValue); OptionalNumber(writer, "maximumValue", source.MaximumValue);
        OptionalString(writer, "sizeName", source.SizeDimension.Name); Boolean(writer, "sizeLegend", source.SizeDimension.ShowLegend);
        if (source.SizeDimension.ValueFormat is { } format) {
            var policy = format.PortablePolicy(); String(writer, "notation", policy.Notation); String(writer, "format", policy.Format); String(writer, "culture", policy.Culture);
        }
        writer.EndObject();
    }
}
