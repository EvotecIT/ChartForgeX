using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    // Keep descriptions as host data even when a static image's generic descendants cannot be named.
    private static void WriteGroupMetadata(SvgMarkupWriter writer, VisualSceneGroup group, bool explorable, bool decorative, bool semanticTarget, string? regionLabel) {
        group.Metadata.TryGetValue("role", out var role);
        var namedRole = !string.IsNullOrEmpty(role) && role != "generic" && role != "none" && role != "presentation";
        var label = group.Metadata.TryGetValue("aria-label", out var captured) ? captured : regionLabel;
        var labelled = label != null;
        if (explorable && (labelled || semanticTarget) && string.IsNullOrEmpty(role) && group.Href == null) {
            writer.Attribute("role", "group");
            namedRole = true;
        }
        foreach (var item in group.Metadata) {
            if (item.Key != "aria-label") writer.Attribute(item.Key, item.Value);
        }
        if (!labelled) return;
        var accessible = !decorative && (namedRole || group.Href != null);
        if (accessible) writer.Attribute("aria-label", label);
        if ((!explorable || !accessible) && !group.Metadata.ContainsKey("data-cfx-description")) writer.Attribute("data-cfx-description", label);
    }
}
