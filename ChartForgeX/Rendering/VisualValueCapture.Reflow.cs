using System;
using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualValueCapture {
    /// <summary>One qualified native identity set per frame; destination is installed semantically before transient presentation.</summary>
    internal static IReadOnlyList<(PreparedVisual Frame, bool Source, double Opacity)> ReflowOpacityFrames(
        PreparedVisual from, PreparedVisual to, int frameCount, CancellationToken cancellationToken = default) {
        if (from == null) throw new ArgumentNullException(nameof(from));
        if (to == null) throw new ArgumentNullException(nameof(to));
        if (frameCount < 3 || frameCount > 16) throw new ArgumentOutOfRangeException(nameof(frameCount), "Native Reflow fade needs 3..16 frames including the zero switch.");
        if (from.Size.Width != to.Size.Width || from.Size.Height != to.Size.Height)
            throw new NotSupportedException("Native Reflow retains the captured visual viewport size.");
        cancellationToken.ThrowIfCancellationRequested(); var zero = (frameCount - 1) / 2;
        var rows = new (PreparedVisual Frame, bool Source, double Opacity)[frameCount];
        for (var index = 0; index < rows.Length; index++) {
            cancellationToken.ThrowIfCancellationRequested(); var source = index < zero;
            var opacity = source ? 1 - index / (double)zero : (index - zero) / (double)(frameCount - 1 - zero);
            rows[index] = (OpacityFrame(source ? from : to, opacity), source, opacity);
        }
        cancellationToken.ThrowIfCancellationRequested(); return Array.AsReadOnly(rows);
    }
}
