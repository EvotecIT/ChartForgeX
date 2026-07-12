using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class ChartAxisDensity {
    internal static bool ShowVerticalLabel(int index, int count, double availableHeight, double fontSize, ChartLabelDensity density) {
        if (index < 0 || index >= count) return false;
        if (density == ChartLabelDensity.All || count < 3) return true;
        var spacing = availableHeight / Math.Max(1, count - 1);
        var densityFactor = density == ChartLabelDensity.Dense ? 0.78 : density == ChartLabelDensity.Relaxed ? 1.5 : 1.15;
        var requiredSpacing = Math.Max(1, fontSize * densityFactor);
        if (spacing >= requiredSpacing) return true;

        var stride = Math.Max(2, (int)Math.Ceiling(requiredSpacing / Math.Max(0.001, spacing)));
        if (index == 0 || index == count - 1) return true;
        return index % stride == 0 && count - 1 - index >= stride;
    }
}
