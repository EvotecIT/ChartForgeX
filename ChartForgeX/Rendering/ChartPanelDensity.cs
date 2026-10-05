using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Chooses panel density before composition so a grid never stretches a lower-resolution child.</summary>
internal static class ChartPanelDensity {
    internal static int OutputScale(ChartSize source, double targetWidth, double targetHeight, int gridScale) =>
        Math.Max(1, (int)Math.Ceiling(Math.Max(targetWidth / source.Width, targetHeight / source.Height) * gridScale));
}
