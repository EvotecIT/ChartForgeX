using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    private static ChartPath PictorialPath(ChartPictorialShape shape, ChartRect bounds, IReadOnlyList<IReadOnlyList<ChartPoint>>? custom) {
        var commands = new List<ChartPathCommand>();
        var cx = bounds.Left + bounds.Width / 2; var cy = bounds.Top + bounds.Height / 2; var r = bounds.Width / 2;
        void Move(double x, double y) => commands.Add(ChartPathCommand.MoveTo(cx + x * r, cy + y * r));
        void Line(double x, double y) => commands.Add(ChartPathCommand.LineTo(cx + x * r, cy + y * r));
        void Cubic(double x1, double y1, double x2, double y2, double x, double y) => commands.Add(
            ChartPathCommand.CubicTo(cx + x1 * r, cy + y1 * r, cx + x2 * r, cy + y2 * r, cx + x * r, cy + y * r));
        void Polygon(params double[] coordinates) {
            Move(coordinates[0], coordinates[1]);
            for (var index = 2; index < coordinates.Length; index += 2) Line(coordinates[index], coordinates[index + 1]);
        }
        void Circle(double x, double y, double radius) {
            const double k = .5522847498307936;
            Move(x, y - radius);
            Cubic(x + radius * k, y - radius, x + radius, y - radius * k, x + radius, y);
            Cubic(x + radius, y + radius * k, x + radius * k, y + radius, x, y + radius);
            Cubic(x - radius * k, y + radius, x - radius, y + radius * k, x - radius, y);
            Cubic(x - radius, y - radius * k, x - radius * k, y - radius, x, y - radius);
        }
        if (custom != null) {
            foreach (var contour in custom) for (var index = 0; index < contour.Count; index++) {
                var x = bounds.Left + contour[index].X * bounds.Width; var y = bounds.Top + contour[index].Y * bounds.Height;
                commands.Add(index == 0 ? ChartPathCommand.MoveTo(x, y) : ChartPathCommand.LineTo(x, y));
            }
            return new ChartPath(commands);
        }
        switch (shape) {
            case ChartPictorialShape.Square: return ChartPathBuilder.RoundedRectangle(bounds, r * .14);
            case ChartPictorialShape.Diamond: Polygon(0, -1, 1, 0, 0, 1, -1, 0); break;
            case ChartPictorialShape.Triangle: Polygon(0, -1, 1, 1, -1, 1); break;
            case ChartPictorialShape.Star:
                for (var index = 0; index < 10; index++) {
                    var angle = -Math.PI / 2 + index * Math.PI / 5; var radius = index % 2 == 0 ? 1 : .44;
                    if (index == 0) Move(Math.Cos(angle) * radius, Math.Sin(angle) * radius);
                    else Line(Math.Cos(angle) * radius, Math.Sin(angle) * radius);
                }
                break;
            case ChartPictorialShape.Heart:
                Move(0, .72); Cubic(-1.12, -.08, -.9, -.82, -.34, -.64); Cubic(-.12, -.56, 0, -.34, 0, -.18);
                Cubic(0, -.34, .12, -.56, .34, -.64); Cubic(.9, -.82, 1.12, -.08, 0, .72); break;
            case ChartPictorialShape.Shield:
                Move(0, -1); Cubic(.72, -.72, .82, -.64, .82, -.22); Cubic(.82, .48, .35, .85, 0, 1);
                Cubic(-.35, .85, -.82, .48, -.82, -.22); Cubic(-.82, -.64, -.72, -.72, 0, -1); break;
            case ChartPictorialShape.Check:
                Polygon(-.86, -.02, -.22, .7, .9, -.45, .65, -.69, -.22, .24, -.6, -.26); break;
            case ChartPictorialShape.Person:
                Circle(0, -.48, .34); Move(-.64, .74); Cubic(-.56, .02, -.34, -.04, 0, -.04);
                Cubic(.34, -.04, .56, .02, .64, .74); break;
            case ChartPictorialShape.PersonDress:
                Circle(0, -.55, .30); Polygon(-.3, -.18, .3, -.18, .3, .18, .62, .72, -.62, .72, -.3, .18); break;
            case ChartPictorialShape.Circle: Circle(0, 0, 1); break;
            default: throw new InvalidOperationException("Unknown pictorial symbol shape.");
        }
        return new ChartPath(commands);
    }
}
