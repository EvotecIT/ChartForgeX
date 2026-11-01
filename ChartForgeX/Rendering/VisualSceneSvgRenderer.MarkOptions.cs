using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    private static bool HasNavigation(VisualScene scene) {
        foreach (var node in scene.Nodes) if (node is VisualSceneGroup { Href: not null }) return true;
        return false;
    }

    /// <summary>Emits one accessible keyboard target while retaining legacy render-wide link defaults.</summary>
    private static void WriteGroupNavigation(SvgMarkupWriter writer, VisualSceneGroup group, bool decorative, VisualSvgOptions? options) {
        if (group.Href == null) return;
        writer.Attribute("href", group.Href).Attribute("tabindex", decorative ? -1 : 0);
        if (!decorative) writer.Attribute("focusable", true);
        var newContext = group.Metadata.TryGetValue("target", out var target)
            ? target == "_blank" : options?.LinkTarget == VisualSvgLinkTarget.NewContext;
        if (target == null && newContext) writer.Attribute("target", "_blank");
        if (newContext) writer.Attribute("rel", "noopener noreferrer");
    }
}
