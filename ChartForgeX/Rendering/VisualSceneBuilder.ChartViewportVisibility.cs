using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Keeps clipped discrete source facts from acquiring keyboard or transparent browser targets outside the native plot.</summary>
    internal void CaptureViewportVisibility(ChartRect plot) {
        var bounds = new Dictionary<string, ChartRect>();
        foreach (var region in _regions) if (region.Id != null) bounds[region.Id] = region.Bounds;
        for (var index = 0; index < _nodes.Count; index++) {
            if (_nodes[index] is not VisualSceneGroup group || group.Id == null || !group.Metadata.ContainsKey("data-cfx-point")
                || group.Metadata.ContainsKey("data-cfx-visible") || !bounds.TryGetValue(group.Id, out var mark)) continue;
            if (mark.Right >= plot.Left && mark.Left <= plot.Right && mark.Bottom >= plot.Top && mark.Top <= plot.Bottom) continue;
            var metadata = new Dictionary<string, string>();
            foreach (var pair in group.Metadata) metadata.Add(pair.Key, pair.Value);
            metadata["data-cfx-visible"] = "false";
            _nodes[index] = new VisualSceneGroup(group.Role, group.Id, group.Clip, metadata, group.PathClip, group.Rotation,
                group.Href, group.Tooltip, group.Translation, group.ImageResource, group.ColorScope, group.Opacity, group.SurfaceShadow, group.RetainedEffect);
        }
    }
}
