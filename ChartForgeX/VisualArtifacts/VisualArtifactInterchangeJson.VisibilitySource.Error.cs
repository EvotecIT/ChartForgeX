using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    /// <summary>Nullable native intervals retain their real presence even when changing scalar values are excluded from structure identity.</summary>
    private static void VisibilitySourceErrorBounds(VisualArtifactInterchangeJsonWriter writer, string name, ChartErrorBounds? source, bool includeValues) {
        writer.Property(name);
        if (source is not { } interval) { writer.String(null); return; }
        writer.StartObject(); if (includeValues) { Number(writer, "lower", interval.Lower); Number(writer, "upper", interval.Upper); } writer.EndObject();
    }
}
