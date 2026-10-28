using System.Threading;
using ChartForgeX.Effects;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneLayerCapture {
    internal static CapturedVisualLayer Capture(PreparedVisual source, VisualInnerShadow effect,
        VisualRenderOptions options, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        return Capture(source, VisualSceneLayerCapturePlan.Create(source.Scene, effect, options),
            effect.OrderedDither, cancellation);
    }

    internal static CapturedVisualLayer Capture(PreparedVisual source, VisualOutline effect,
        VisualRenderOptions options, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        return Capture(source, VisualSceneLayerCapturePlan.Create(source.Scene, effect, options),
            effect.OrderedDither, cancellation);
    }

    internal static CapturedVisualLayer Capture(PreparedVisual source, VisualBloom effect,
        VisualRenderOptions options, CancellationToken cancellation) {
        cancellation.ThrowIfCancellationRequested();
        return Capture(source, VisualSceneLayerCapturePlan.Create(source.Scene, effect, options),
            effect.OrderedDither, cancellation);
    }
}
