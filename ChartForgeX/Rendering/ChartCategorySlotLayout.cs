using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Rendering;

/// <summary>Divides mapped category pitch into stable series slots, including marks that extend beyond their body.</summary>
internal readonly struct ChartCategorySlotLayout {
    private ChartCategorySlotLayout(double thickness, double offset, double pitch) { Thickness = thickness; Offset = offset; Pitch = pitch; }
    internal double Thickness { get; }
    internal double Offset { get; }
    internal double Pitch { get; }

    internal static ChartCategorySlotLayout Create(IEnumerable<double> mappedCategories, double extent, double spacing,
        int count, int position, double maximumThickness = double.PositiveInfinity, double minimumThickness = .1,
        int sampleSlots = 0, double footprintRatio = 1, double footprintPadding = 0) {
        var centers = mappedCategories.Distinct().OrderBy(value => value).ToArray();
        var pitch = sampleSlots > 0 ? extent / sampleSlots : extent;
        if (sampleSlots == 0)
            for (var index = 1; index < centers.Length; index++) pitch = Math.Min(pitch, centers[index] - centers[index - 1]);
        count = Math.Max(1, count);
        var occupied = pitch * .68;
        var gap = count > 1 ? Math.Min(spacing / 2, occupied / (count * 4)) : 0;
        var cell = (occupied - gap * (count - 1)) / count;
        var thickness = Math.Max(minimumThickness, Math.Min(maximumThickness, (cell - footprintPadding) / footprintRatio));
        var footprint = thickness * footprintRatio + footprintPadding;
        var offset = (position - (count - 1) / 2d) * (footprint + gap);
        return new ChartCategorySlotLayout(thickness, offset, pitch);
    }
}
