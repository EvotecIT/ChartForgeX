using System;

namespace ChartForgeX.Rendering;

/// <summary>Immutable local viewport and one finite completed-object operation on the existing native group.</summary>
internal sealed class VisualSceneRetainedEffect {
    internal VisualSceneRetainedEffect(VisualCapturedEffect effect, VisualSize sourceSize) {
        Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        if (effect.Blur != null)
            throw new ArgumentException("Root retained scopes do not represent an implicit blur; use explicit CaptureLayer.", nameof(effect));
        SourceSize = sourceSize;
    }
    internal VisualCapturedEffect Effect { get; }
    internal VisualSize SourceSize { get; }
}
