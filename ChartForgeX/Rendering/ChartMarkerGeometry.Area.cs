using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class ChartMarkerGeometry {
    private static readonly Lazy<double[]> AreaFactors = new(CreateAreaFactors);

    /// <summary>Scales a built-in marker to the fill area of a reference circle, independently of stroke width.</summary>
    internal static double CircleAreaRadiusFactor(ChartMarkerShape shape) => AreaFactors.Value[(int)shape];

    private static double[] CreateAreaFactors() {
        var factors = new double[(int)ChartMarkerShape.Star + 1];
        for (var index = 0; index < factors.Length; index++) {
            var shape = (ChartMarkerShape)index;
            factors[index] = shape == ChartMarkerShape.Circle ? 1
            : Math.Sqrt(Math.PI / UnitFillArea(shape));
        }
        return factors;
    }

    /// <summary>Integrates the canonical line/cubic contours exactly; the pin's second contour is its even-odd hole.</summary>
    private static double UnitFillArea(ChartMarkerShape shape) {
        var path = Path(shape, 0, 0, 1);
        var current = default(ChartPoint); var start = default(ChartPoint);
        var contour = 0d; var area = 0d; var hasContour = false; var firstContour = true;
        void Finish() {
            if (!hasContour) return;
            contour += Cross(current, start);
            area += (firstContour ? 1 : -1) * Math.Abs(contour) / 2;
            firstContour = false; contour = 0;
        }
        foreach (var command in path.Commands) {
            var end = new ChartPoint(command.X, command.Y);
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                Finish(); start = end; hasContour = true;
            } else if (command.Kind == ChartPathCommandKind.LineTo) contour += Cross(current, end);
            else contour += CubicCrossIntegral(current, new ChartPoint(command.Control1X, command.Control1Y),
                new ChartPoint(command.Control2X, command.Control2Y), end);
            current = end;
        }
        Finish();
        return area;
    }

    private static double Cross(ChartPoint first, ChartPoint second) => first.X * second.Y - first.Y * second.X;

    private static double CubicCrossIntegral(ChartPoint start, ChartPoint first, ChartPoint second, ChartPoint end) {
        double[] Coefficients(double p0, double p1, double p2, double p3) =>
            new[] { p0, 3 * (p1 - p0), 3 * (p2 - 2 * p1 + p0), p3 - 3 * p2 + 3 * p1 - p0 };
        var x = Coefficients(start.X, first.X, second.X, end.X);
        var y = Coefficients(start.Y, first.Y, second.Y, end.Y);
        var integral = 0d;
        for (var i = 0; i < 4; i++)
            for (var j = 1; j < 4; j++) integral += (x[i] * j * y[j] - y[i] * j * x[j]) / (i + j);
        return integral;
    }
}
