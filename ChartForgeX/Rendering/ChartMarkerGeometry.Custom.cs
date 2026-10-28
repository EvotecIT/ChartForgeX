using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartMarkerGeometry {
    /// <summary>Measures custom even-odd fill with the existing native curve flattener, including holes and crossings.</summary>
    /// <remarks>Measurement removes translation and normalizes finite relative extent before using 1024 pixels per extent; straight contours integrate exactly.
    /// The finite contour budget bounds pairwise crossing discovery. Exporters still share the original typed curves.</remarks>
    internal static double CustomCircleAreaRadiusFactor(ChartMarkerPath custom) {
        var path = MeasurementPath(custom, out var extent);
        var contours = VisualSceneGeometry.Flatten(new VisualScenePath(path, true, null, null, 0, null, null), 1024);
        var edges = new List<(double X1, double Y1, double X2, double Y2)>();
        var levels = new SortedSet<double>();
        foreach (var contour in contours) {
            for (var i = 0; i < contour.Count; i++) {
                var first = contour[i]; var second = contour[(i + 1) % contour.Count];
                if (first.Y == second.Y) continue;
                if (edges.Count >= 1024) throw new InvalidOperationException("Area-encoded custom markers support at most 1024 flattened edges.");
                var top = first.Y < second.Y ? first : second;
                var bottom = first.Y < second.Y ? second : first;
                edges.Add((top.X, top.Y, bottom.X, bottom.Y)); levels.Add(top.Y); levels.Add(bottom.Y);
            }
        }
        for (var i = 0; i < edges.Count; i++) {
            var a = edges[i];
            for (var j = i + 1; j < edges.Count; j++) {
                var b = edges[j]; var low = Math.Max(a.Y1, b.Y1); var high = Math.Min(a.Y2, b.Y2);
                if (low >= high) continue;
                var lowDifference = XAt(a, low) - XAt(b, low);
                var highDifference = XAt(a, high) - XAt(b, high);
                if (!(lowDifference < 0 && highDifference > 0 || lowDifference > 0 && highDifference < 0)) continue;
                var fraction = Math.Abs(lowDifference) / (Math.Abs(lowDifference) + Math.Abs(highDifference));
                var crossing = low + (high - low) * fraction;
                if (crossing > low && crossing < high) {
                    levels.Add(crossing);
                    if (levels.Count > 32768) throw new InvalidOperationException("Area-encoded custom markers exceed the bounded intersection budget.");
                }
            }
        }
        var xs = new List<double>(); var area = 0d; var previous = double.NaN;
        foreach (var level in levels) {
            if (!double.IsNaN(previous) && level > previous) {
                xs.Clear();
                foreach (var edge in edges) {
                    if (edge.Y1 > previous || edge.Y2 < level) continue;
                    // Average the band endpoints instead of requiring a representable
                    // interior Y coordinate for a positive subnormal-height band.
                    xs.Add((XAt(edge, previous) + XAt(edge, level)) / 2);
                }
                xs.Sort();
                for (var i = 0; i + 1 < xs.Count; i += 2) area += (xs[i + 1] - xs[i]) * (level - previous);
            }
            previous = level;
        }
        if (!(area > 0) || double.IsInfinity(area)) throw new InvalidOperationException("An area-encoded custom marker must enclose non-zero even-odd fill area.");
        // Restore linear scale after the square root; squaring a small authored extent or
        // dividing Pi by a very small area first could underflow or overflow unnecessarily.
        var factor = Math.Sqrt(Math.PI) / Math.Sqrt(area) / extent;
        if (double.IsInfinity(factor)) throw new InvalidOperationException("The custom marker fill area is too small for finite area encoding.");
        return factor;
    }

    private static double XAt((double X1, double Y1, double X2, double Y2) edge, double y) =>
        edge.X1 + ((y - edge.Y1) / (edge.Y2 - edge.Y1)) * (edge.X2 - edge.X1);

    /// <summary>Builds a measurement-only path; original contour coordinates and exported cubic controls stay untouched.</summary>
    private static ChartPath MeasurementPath(ChartMarkerPath custom, out double extent) {
        double left = double.PositiveInfinity, top = double.PositiveInfinity, right = double.NegativeInfinity, bottom = double.NegativeInfinity;
        void Include(double x, double y) { left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y); }
        foreach (var command in custom.Commands) {
            Include(command.X, command.Y);
            if (command.Kind == ChartPathCommandKind.CubicTo) { Include(command.Control1X, command.Control1Y); Include(command.Control2X, command.Control2Y); }
        }
        extent = Math.Max(right - left, bottom - top);
        if (!(extent > 0)) throw new InvalidOperationException("An area-encoded custom marker must enclose non-zero even-odd fill area.");
        var scale = extent;
        var commands = new List<ChartPathCommand>(custom.Commands.Count);
        foreach (var command in custom.Commands) {
            double x = (command.X - left) / scale, y = (command.Y - top) / scale;
            commands.Add(command.Kind == ChartPathCommandKind.MoveTo ? ChartPathCommand.MoveTo(x, y)
                : command.Kind == ChartPathCommandKind.LineTo ? ChartPathCommand.LineTo(x, y)
                : ChartPathCommand.CubicTo((command.Control1X - left) / scale, (command.Control1Y - top) / scale,
                    (command.Control2X - left) / scale, (command.Control2Y - top) / scale, x, y));
        }
        return new ChartPath(commands);
    }
}
