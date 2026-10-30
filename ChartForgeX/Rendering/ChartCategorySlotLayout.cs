using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Rendering;

/// <summary>Divides mapped category pitch into stable series slots, including marks that extend beyond their body.</summary>
internal readonly struct ChartCategorySlotLayout {
    private const double MaximumCrowdedBandFill = .68;
    private ChartCategorySlotLayout(double thickness, double offset, double pitch) { Thickness = thickness; Offset = offset; Pitch = pitch; }
    internal double Thickness { get; }
    internal double Offset { get; }
    internal double Pitch { get; }

    /// <summary>Resolves slots while allowing scalar bars to retain their minimum thickness at overlapping categories.</summary>
    /// <remarks>When <paramref name="requireMinimumFootprint"/> is true, interval marks must fit their minimum caps and strokes inside the slot.</remarks>
    internal static ChartCategorySlotLayout Create(IEnumerable<double> mappedCategories, double extent, double spacing,
        int count, int position, bool requireMinimumFootprint, double maximumThickness = double.PositiveInfinity, double minimumThickness = .1,
        int sampleSlots = 0, double footprintRatio = 1, double footprintPadding = 0, Func<double, double>? measuredFootprint = null, double maximumPitch = double.PositiveInfinity) {
        var centers = mappedCategories.Distinct().OrderBy(value => value).ToArray();
        var pitch = sampleSlots > 0 ? extent / sampleSlots : extent;
        if (sampleSlots == 0)
            for (var index = 1; index < centers.Length; index++) pitch = Math.Min(pitch, centers[index] - centers[index - 1]);
        pitch = Math.Min(pitch, maximumPitch);
        count = Math.Max(1, count);
        var minimumFootprint = measuredFootprint?.Invoke(minimumThickness) ?? minimumThickness * footprintRatio + footprintPadding;
        // Sparkline sample slots keep their dense legacy share; charts use the design band fill.
        var occupied = pitch * (sampleSlots > 0 ? MaximumCrowdedBandFill : ChartMarkSurface.BandFill);
        var gap = count > 1 ? Math.Min(spacing / 2, occupied / (count * 4)) : 0;
        var cell = (occupied - gap * (count - 1)) / count;
        if (requireMinimumFootprint && minimumFootprint > cell) {
            // A mark whose minimum painted footprint needs more of a crowded band may use up to the legacy share before failing.
            occupied = pitch * MaximumCrowdedBandFill;
            gap = count > 1 ? Math.Min(spacing / 2, occupied / (count * 4)) : 0;
            cell = (occupied - gap * (count - 1)) / count;
        }
        if (requireMinimumFootprint && minimumFootprint > cell)
            throw new InvalidOperationException("The category slot cannot fit the mark's minimum painted footprint. Increase category-axis space, reduce grouped series, or use a simpler mark style.");
        var thickness = Math.Max(minimumThickness, Math.Min(maximumThickness, (cell - footprintPadding) / footprintRatio));
        if (measuredFootprint != null) {
            // The caller measures a monotonic painted footprint that includes the body.
            // Solve in pixel space without changing the linear contract of other marks.
            var lower = minimumThickness; var upper = Math.Max(lower, Math.Min(maximumThickness, cell));
            if (measuredFootprint(upper) <= cell) lower = upper;
            else for (var iteration = 0; iteration < 40; iteration++) {
                var middle = (lower + upper) / 2;
                if (measuredFootprint(middle) <= cell) lower = middle;
                else upper = middle;
            }
            thickness = lower;
        }
        var footprint = measuredFootprint?.Invoke(thickness) ?? thickness * footprintRatio + footprintPadding;
        var offset = (position - (count - 1) / 2d) * (footprint + gap);
        return new ChartCategorySlotLayout(thickness, offset, pitch);
    }
}
