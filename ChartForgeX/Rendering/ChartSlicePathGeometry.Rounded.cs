using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class ChartSlicePathGeometry {
    /// <summary>Builds the inward-rounded outline, or null when the existing sharp/full-turn slice is appropriate.</summary>
    internal static ChartPath? RoundedPath(double cx, double cy, double outer, double inner, double start, double sweep, double cornerRadius) {
        if (cornerRadius <= 0 || sweep >= Math.PI * 2 - .000001) return null;
        var commands = new List<ChartPathCommand>();
        if (outer <= inner || sweep <= 0) return new ChartPath(commands);
        var halfThickness = (outer - inner) / 2;
        var sine = Math.Sin(Math.Min(sweep, Math.PI) / 2);
        var outerCorner = Math.Min(Math.Min(cornerRadius, halfThickness), outer * sine / (1 + sine));
        var innerCorner = inner <= 0 ? 0 : Math.Min(cornerRadius, halfThickness);
        if (inner > 0 && sweep < Math.PI) innerCorner = Math.Min(innerCorner, inner * sine / (1 - sine));
        var outerShift = Math.Asin(Math.Min(1, outerCorner / (outer - outerCorner)));
        var innerShift = innerCorner <= 0 ? 0 : Math.Asin(Math.Min(1, innerCorner / (inner + innerCorner)));
        var outerTangent = Math.Sqrt(Math.Max(0, outer * (outer - 2 * outerCorner)));
        var innerTangent = Math.Sqrt(inner * (inner + 2 * innerCorner));
        var end = start + sweep;
        Move(outer, start + outerShift);
        Arc(cx, cy, outer, start + outerShift, Math.Max(0, sweep - 2 * outerShift));
        var outerEnd = Center(outer - outerCorner, end - outerShift);
        Arc(outerEnd.X, outerEnd.Y, outerCorner, end - outerShift, Math.PI / 2 + outerShift);
        Line(innerTangent, end);
        if (inner > 0) {
            var innerEnd = Center(inner + innerCorner, end - innerShift);
            Arc(innerEnd.X, innerEnd.Y, innerCorner, end + Math.PI / 2, Math.PI / 2 - innerShift);
            Arc(cx, cy, inner, end - innerShift, -Math.Max(0, sweep - 2 * innerShift));
            var innerStart = Center(inner + innerCorner, start + innerShift);
            Arc(innerStart.X, innerStart.Y, innerCorner, start + innerShift + Math.PI, Math.PI / 2 - innerShift);
        }
        Line(outerTangent, start);
        var outerStart = Center(outer - outerCorner, start + outerShift);
        Arc(outerStart.X, outerStart.Y, outerCorner, start - Math.PI / 2, Math.PI / 2 + outerShift);
        // Both backends paint these same encoded coordinates. Precision collapse
        // must not leave a native fill or pattern where the SVG has no enclosed area.
        for (var index = 0; index < commands.Count; index++) {
            var command = commands[index];
            commands[index] = command.Kind == ChartPathCommandKind.MoveTo ? ChartPathCommand.MoveTo(Q(command.X), Q(command.Y))
                : command.Kind == ChartPathCommandKind.LineTo ? ChartPathCommand.LineTo(Q(command.X), Q(command.Y))
                : ChartPathCommand.CubicTo(Q(command.Control1X), Q(command.Control1Y), Q(command.Control2X), Q(command.Control2Y), Q(command.X), Q(command.Y));
        }
        return new ChartPath(commands);

        ChartPoint Center(double radius, double angle) => new(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius);
        void Move(double radius, double angle) { var p = Center(radius, angle); commands.Add(ChartPathCommand.MoveTo(p.X, p.Y)); }
        void Line(double radius, double angle) { var p = Center(radius, angle); commands.Add(ChartPathCommand.LineTo(p.X, p.Y)); }
        // Smaller circular segments keep approximation error below the coordinate
        // encoding precision at ordinary chart sizes, without changing existing paths.
        void Arc(double x, double y, double radius, double angle, double span) {
            if (radius > 0) ChartPathBuilder.AddCircularArc(commands, x, y, radius, angle, span, Math.PI / 8);
        }
    }

    internal static bool HasEncodedFillArea(double cx, double cy, double outer, double inner, double start, double sweep, double cornerRadius) {
        if (!HasEncodedFillArea(cx, cy, outer, inner, start, sweep)) return false;
        var rounded = RoundedPath(cx, cy, outer, inner, start, sweep, cornerRadius);
        return rounded == null || VisualSceneGeometry.HasEvenOddFillArea(VisualSceneGeometry.Flatten(ScenePath(rounded), 8));
    }

    /// <summary>The same contour used by native paint, pattern clips and label containment.</summary>
    internal static List<List<ChartPoint>> Contours(double cx, double cy, double outer, double inner, double start, double sweep, double cornerRadius, double pixelsPerUnit) {
        var rounded = RoundedPath(cx, cy, outer, inner, start, sweep, cornerRadius);
        return rounded == null
            ? VisualSceneGeometry.Flatten(new VisualSceneSlice(cx, cy, outer, inner, start, sweep, ChartColor.Transparent, null, 0, null, null), pixelsPerUnit)
            : VisualSceneGeometry.Flatten(ScenePath(rounded), pixelsPerUnit);
    }

    private static VisualScenePath ScenePath(ChartPath path) => new(path, true, ChartColor.Transparent, null, 0, null, null);
    private static double Q(double value) => double.Parse(F(value), CultureInfo.InvariantCulture);
}
