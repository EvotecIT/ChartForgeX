using System.Collections.Generic;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Captures raw point eligibility after the shared identity, host-option and static-state owners finish.</summary>
    /// <remarks>The native point emitter supplies these identities and coordinates; no chart survives in the bank.</remarks>
    internal void CaptureXZoomSourcePoints(IReadOnlyDictionary<string, double> sourcePoints) {
        for (var index = 0; index < _nodes.Count; index++) {
            if (_nodes[index] is not VisualSceneGroup { Role: "point" } group || group.Id is not { } id
                || !sourcePoints.TryGetValue(id, out var sourceX)) continue;
            _nodes[index] = new VisualSceneGroup(group.Role, group.Id, group.Clip, group.Metadata,
                group.PathClip, group.Rotation, group.Href, group.Tooltip, group.Translation, group.ImageResource,
                group.ColorScope, group.Opacity, group.SurfaceShadow, group.RetainedEffect, CartesianXSourceAdmission.Point(sourceX));
        }
    }
}
