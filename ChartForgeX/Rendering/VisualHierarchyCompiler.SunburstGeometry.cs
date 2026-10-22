using System;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    /// <summary>The inward painted sector; original hierarchy allocation remains on the source node.</summary>
    private readonly struct SunburstSectorPaintGeometry {
        internal SunburstSectorPaintGeometry(double inner, double outer, double start, double sweep, bool paddingCollapsed = false) {
            InnerRadius = inner; OuterRadius = outer; StartAngle = start; Sweep = sweep; PaddingCollapsed = paddingCollapsed;
        }
        internal double InnerRadius { get; }
        internal double OuterRadius { get; }
        internal double StartAngle { get; }
        internal double Sweep { get; }
        internal double EndAngle => StartAngle + Sweep;
        internal bool PaddingCollapsed { get; }
    }

    /// <summary>Reserves a logical-pixel gap without changing authored sizes, angles, hierarchy or unused capacity.</summary>
    private static SunburstSectorPaintGeometry SunburstPaintGeometry(ChartSunburstNode node, double gap) {
        var sweep = node.EndAngle - node.StartAngle;
        if (gap == 0 || node.Value == 0 || sweep <= 0)
            return new SunburstSectorPaintGeometry(node.InnerRadius, node.OuterRadius, node.StartAngle, sweep);
        var half = gap / 2;
        var inner = node.InnerRadius == 0 ? 0 : node.InnerRadius + half;
        var outer = node.OuterRadius - half;
        if (outer <= inner) return Collapsed();
        // Full disks/rings have no authored angular boundary and must not acquire a seam.
        if (sweep >= Math.PI * 2 - .000001)
            return new SunburstSectorPaintGeometry(inner, outer, node.StartAngle, sweep);
        // In a Sunburst only the full-turn source/view root reaches the central apex.
        if (inner <= 0) return Collapsed();
        var inset = Math.Asin(Math.Min(1, half / inner));
        var paintedSweep = sweep - 2 * inset;
        return paintedSweep <= 0 ? Collapsed()
            : new SunburstSectorPaintGeometry(inner, outer, node.StartAngle + inset, paintedSweep);

        SunburstSectorPaintGeometry Collapsed() => new(0, 0, node.StartAngle, 0, paddingCollapsed: true);
    }
}
