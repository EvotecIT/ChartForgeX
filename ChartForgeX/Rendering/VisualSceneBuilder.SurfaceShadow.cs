using System;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Wraps numeric surface geometry only. Null or transparent policy adds no scene node or identity fact.</summary>
    internal IDisposable? PushSurfaceShadow(VisualDropShadow? shadow, ChartRect objectBounds, ChartRect inkBounds) {
        if (shadow == null || shadow.Color.A == 0 || shadow.Opacity == 0 ||
            objectBounds.Width <= 0 || objectBounds.Height <= 0) {
            return null;
        }
        var surface = new VisualSceneSurfaceShadow(shadow, objectBounds, inkBounds);
        return OpenGroup(new VisualSceneGroup(null, null, null, null, null, null,
            null, null, null, null, null, 1, surface));
    }
}
