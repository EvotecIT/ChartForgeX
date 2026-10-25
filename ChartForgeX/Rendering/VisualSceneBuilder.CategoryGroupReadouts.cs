using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Adds detached parent context to existing observation facts without changing their leaf caption or identity.</summary>
    internal void CaptureCategoryGroupReadouts(IReadOnlyList<ChartAxisLabel> bindings) {
        var labels = bindings.Where(label => label.Groups.Count > 0).ToDictionary(label => label.Value);
        if (labels.Count == 0) return;
        var regions = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < _regions.Count; index++)
            if (_regions[index].Role == "point") regions[_regions[index].Id] = index;
        for (var index = 0; index < _nodes.Count; index++) {
            if (_nodes[index] is not VisualSceneGroup group || !group.Metadata.ContainsKey("data-cfx-point")
                || !group.Metadata.TryGetValue("data-cfx-x", out var coordinate)
                || !double.TryParse(coordinate, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || !labels.TryGetValue(value, out var label)) continue;
            var metadata = group.Metadata.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            metadata["data-cfx-category-group-count"] = label.Groups.Count.ToString(CultureInfo.InvariantCulture);
            for (var parent = 0; parent < label.Groups.Count; parent++)
                metadata["data-cfx-category-group-" + parent.ToString(CultureInfo.InvariantCulture)] = label.Groups[parent];
            var description = (metadata.TryGetValue("aria-label", out var previous) ? previous : string.Empty)
                + "; category: " + string.Join(" / ", label.Groups.Concat(new[] { label.Text }));
            metadata["aria-label"] = description;
            if (group.Id != null && regions.TryGetValue(group.Id, out var region)) {
                var original = _regions[region];
                _regions[region] = new VisualSemanticRegion(original.Id, original.Role, original.Bounds, description);
            }
            _nodes[index] = new VisualSceneGroup(group.Role, group.Id, group.Clip, metadata,
                group.PathClip, group.Rotation, group.Href, group.Tooltip, group.Translation, group.ImageResource, group.ColorScope, group.Opacity, group.SurfaceShadow, group.RetainedEffect);
        }
    }
}
