using System;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>One retained surface-only effect scope; the source painter supplies its existing numeric object and ink bounds.</summary>
internal sealed class VisualSceneSurfaceShadow {
    internal VisualSceneSurfaceShadow(VisualDropShadow effect, ChartRect objectBounds, ChartRect inkBounds) {
        Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        Validate(objectBounds);
        Validate(inkBounds);
        ObjectBounds = objectBounds;
        InkBounds = inkBounds;
    }

    internal VisualDropShadow Effect { get; }
    internal ChartRect ObjectBounds { get; }
    internal ChartRect InkBounds { get; }

    private static void Validate(ChartRect bounds) {
        if (!Finite(bounds.X) || !Finite(bounds.Y) || !Finite(bounds.Right) || !Finite(bounds.Bottom) ||
            bounds.Width <= 0 || bounds.Height <= 0) {
            throw new ArgumentOutOfRangeException(nameof(bounds));
        }
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
