using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal enum VisualStrokeCap { Butt, Round, Square }
internal enum VisualStrokeJoin { Miter, Round, Bevel }

/// <summary>Optional token provenance, detached with a mark while raster backends retain its resolved colors.</summary>
internal readonly struct VisualScenePaintBinding {
    internal VisualScenePaintBinding(SvgPaint? fill = null, SvgPaint? stroke = null) { Fill = fill; Stroke = stroke; }
    internal SvgPaint? Fill { get; }
    internal SvgPaint? Stroke { get; }
}

/// <summary>A detached stop in a logical-space linear scene gradient.</summary>
internal readonly struct VisualGradientStop {
    internal VisualGradientStop(double offset, ChartColor color, SvgPaint? paint = null) {
        ChartGuards.Finite(offset, nameof(offset));
        if (offset < 0 || offset > 1) throw new ArgumentOutOfRangeException(nameof(offset));
        Offset = offset; Color = color; Paint = paint;
    }
    internal double Offset { get; }
    internal ChartColor Color { get; }
    internal SvgPaint? Paint { get; }
}

internal sealed class VisualSceneGradient : VisualSceneNode {
    internal VisualSceneGradient(VisualSceneMark shape, ChartPoint start, ChartPoint end, IReadOnlyList<VisualGradientStop> stops)
        : base(shape.Role, shape.Id) {
        if (stops == null || stops.Count < 2) throw new ArgumentException("A gradient needs at least two ordered stops.", nameof(stops));
        var copy = new VisualGradientStop[stops.Count];
        for (var i = 0; i < copy.Length; i++) {
            if (i > 0 && stops[i].Offset < stops[i - 1].Offset) throw new ArgumentException("Gradient stops must be ordered.", nameof(stops));
            copy[i] = stops[i];
        }
        Shape = shape; Start = start; End = end; Stops = Array.AsReadOnly(copy);
    }
    internal VisualSceneMark Shape { get; }
    internal ChartPoint Start { get; }
    internal ChartPoint End { get; }
    internal IReadOnlyList<VisualGradientStop> Stops { get; }
}

internal readonly struct VisualRotation {
    internal VisualRotation(double degrees, double x, double y) {
        ChartGuards.Finite(degrees, nameof(degrees)); ChartGuards.Finite(x, nameof(x)); ChartGuards.Finite(y, nameof(y));
        Degrees = degrees; X = x; Y = y;
    }
    internal double Degrees { get; }
    internal double X { get; }
    internal double Y { get; }
}
