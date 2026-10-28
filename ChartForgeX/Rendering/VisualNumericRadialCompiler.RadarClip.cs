using System;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualNumericRadialCompiler {
    /// <summary>Retains the full-turn rectangle/hole bytes; a partial sweep adds only angular clipping, preserving outer marker extents.</summary>
    private static ChartPath RadarClip(RadialSeriesGeometry geometry, ChartRect plot) {
        if (geometry.Sweep == Math.PI * 2) {
            var commands = ChartPathBuilder.RoundedRectangle(plot, 0).Commands.ToList();
            if (geometry.Inner > 0) commands.AddRange(ChartPathBuilder.Ellipse(geometry.Cx, geometry.Cy, geometry.Inner, geometry.Inner).Commands);
            return new ChartPath(commands);
        }
        // The surrounding plot clip bounds the outer edge. Do not chop a maximum-radius glyph at the value radius.
        return ChartSlicePathGeometry.ClipPath(geometry.Cx, geometry.Cy, Math.Max(plot.Width, plot.Height), geometry.Inner, geometry.Start, geometry.Sweep);
    }
}
