using System;
using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualValueCapture {
    /// <summary>Captures one accepted immutable visual as a whole-visual initial fade; no data baseline is synthesized.</summary>
    internal static IReadOnlyList<PreparedVisual> InitialFadeFrames(PreparedVisual final, int frameCount, CancellationToken cancellationToken = default) {
        if (final == null) throw new ArgumentNullException(nameof(final));
        if (frameCount < 2 || frameCount > 16) throw new ArgumentOutOfRangeException(nameof(frameCount));
        cancellationToken.ThrowIfCancellationRequested(); var frames = new PreparedVisual[frameCount];
        for (var frame = 0; frame < frameCount - 1; frame++) {
            cancellationToken.ThrowIfCancellationRequested(); frames[frame] = OpacityFrame(final, frame / (double)(frameCount - 1));
        }
        frames[frameCount - 1] = final; cancellationToken.ThrowIfCancellationRequested(); return Array.AsReadOnly(frames);
    }

    // Shared native opacity frame for whole-visual initial fade.
    // Completed child marks are isolated once; overlapping fill/stroke/text does not multiply alpha.
    internal static PreparedVisual OpacityFrame(PreparedVisual basis, double opacity) {
        if (basis == null) throw new ArgumentNullException(nameof(basis));
        if (double.IsNaN(opacity) || double.IsInfinity(opacity) || opacity < 0 || opacity > 1) throw new ArgumentOutOfRangeException(nameof(opacity));
        if (opacity == 1) return basis;
        var source = basis.Scene; var nodes = new List<VisualSceneNode>(source.Nodes.Count + 2) {
            new VisualSceneGroup("native-transition-opacity", null, null, null,
                null, null, null, null, null, null, null, opacity)
        };
        nodes.AddRange(source.Nodes); nodes.Add(new VisualSceneEndGroup());
        var scene = new VisualScene(source.Size, nodes, source.Diagnostics,
            opacity == 0 ? Array.Empty<VisualSemanticRegion>() : source.Regions, source.DataProjection);
        return basis.WithNativeFrame(scene);
    }
}
