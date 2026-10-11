using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Owns normalized marker contours for scene marks, pattern clipping and legend glyphs.</summary>
internal static class ChartMarkerGeometry {
    internal static ChartPath Path(ChartMarkerShape shape, double x, double y, double radius) {
        if (shape == ChartMarkerShape.Circle) return ChartPathBuilder.Ellipse(x, y, radius, radius);
        var commands = new List<ChartPathCommand>();
        void Move(double px, double py) => commands.Add(ChartPathCommand.MoveTo(x + px * radius, y + py * radius));
        void Line(double px, double py) => commands.Add(ChartPathCommand.LineTo(x + px * radius, y + py * radius));
        void Cubic(double x1, double y1, double x2, double y2, double px, double py) => commands.Add(
            ChartPathCommand.CubicTo(x + x1 * radius, y + y1 * radius, x + x2 * radius, y + y2 * radius,
                x + px * radius, y + py * radius));
        void Polygon(params double[] coordinates) {
            Move(coordinates[0], coordinates[1]);
            for (var index = 2; index < coordinates.Length; index += 2) Line(coordinates[index], coordinates[index + 1]);
        }
        switch (shape) {
            case ChartMarkerShape.Square: Polygon(-1, -1, 1, -1, 1, 1, -1, 1); break;
            case ChartMarkerShape.Diamond: Polygon(0, -1, 1, 0, 0, 1, -1, 0); break;
            case ChartMarkerShape.Triangle: Polygon(0, -1, 1, 1, -1, 1); break;
            case ChartMarkerShape.Plus:
            case ChartMarkerShape.Cross:
                var vertices = new[] { -.3, -1, .3, -1, .3, -.3, 1, -.3, 1, .3, .3, .3,
                    .3, 1, -.3, 1, -.3, .3, -1, .3, -1, -.3, -.3, -.3 };
                for (var index = 0; index < vertices.Length; index += 2) {
                    var px = vertices[index]; var py = vertices[index + 1];
                    if (shape == ChartMarkerShape.Cross) { var rotatedX = (px - py) / 1.3; py = (px + py) / 1.3; px = rotatedX; }
                    if (index == 0) Move(px, py); else Line(px, py);
                }
                break;
            case ChartMarkerShape.Star:
                for (var index = 0; index < 10; index++) {
                    var angle = -Math.PI / 2 + index * Math.PI / 5; var length = index % 2 == 0 ? 1 : .44;
                    if (index == 0) Move(Math.Cos(angle) * length, Math.Sin(angle) * length);
                    else Line(Math.Cos(angle) * length, Math.Sin(angle) * length);
                }
                break;
            case ChartMarkerShape.Heart:
                Move(0, .72); Cubic(-1.12, -.08, -.9, -.82, -.34, -.64); Cubic(-.12, -.56, 0, -.34, 0, -.18);
                Cubic(0, -.34, .12, -.56, .34, -.64); Cubic(.9, -.82, 1.12, -.08, 0, .72); break;
            case ChartMarkerShape.Pin:
                Move(0, 1); Cubic(-.18, .56, -.75, .12, -.75, -.25); Cubic(-.75, -.66, -.42, -1, 0, -1);
                Cubic(.42, -1, .75, -.66, .75, -.25); Cubic(.75, .12, .18, .56, 0, 1);
                commands.AddRange(ChartPathBuilder.Ellipse(x, y - radius * .25, radius * .25, radius * .25).Commands);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(shape));
        }
        return new ChartPath(commands);
    }
}
