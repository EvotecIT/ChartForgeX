using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Copies native mark paint without serializing or reinterpreting its geometry.</summary>
internal static class VisualSceneMarkStyle {
    internal static VisualSceneMark WithPaint(VisualSceneMark mark, ChartColor? fill, ChartColor? stroke, double width, double[]? dash) {
        var paint = new VisualScenePaintBinding(
            fill.HasValue ? Nullable.Equals(fill, mark.Fill) ? mark.Paint?.Fill : SvgPaint.Literal(fill.Value) : null,
            stroke.HasValue ? Nullable.Equals(stroke, mark.Stroke) ? mark.Paint?.Stroke : SvgPaint.Literal(stroke.Value) : null);
        switch (mark) {
            case VisualScenePath path:
                return new VisualScenePath(new ChartPath(path.Commands), path.Close, fill, stroke, width, path.Role, path.Id,
                    dash ?? path.Dash?.ToArray(), path.Cap, path.Join, paint, path.DashOffset, path.Opacity);
            case VisualSceneLine line:
                return new VisualSceneLine(line.Start, line.End, stroke ?? ChartColor.Transparent, width, line.Role, line.Id,
                    dash ?? line.Dash?.ToArray(), paint, line.DashOffset, line.Opacity);
            case VisualSceneRectangle rect when dash == null || dash.Length == 0:
                return new VisualSceneRectangle(rect.Bounds, rect.Radius, fill, stroke, width, rect.Role, rect.Id, paint, rect.Opacity, rect.Corners);
            case VisualSceneEllipse ellipse when dash == null || dash.Length == 0:
                return new VisualSceneEllipse(ellipse.Cx, ellipse.Cy, ellipse.Rx, ellipse.Ry, fill, stroke, width, ellipse.Role, ellipse.Id, paint, ellipse.Opacity);
            case VisualSceneSlice slice when dash == null || dash.Length == 0:
                return new VisualSceneSlice(slice.Cx, slice.Cy, slice.Outer, slice.Inner, slice.Start, slice.Sweep,
                    fill ?? ChartColor.Transparent, stroke, width, slice.Role, slice.Id, paint, slice.Opacity);
            default:
                return new VisualScenePath(Outline(mark), true, fill, stroke, width, mark.Role, mark.Id, dash,
                    VisualStrokeCap.Round, VisualStrokeJoin.Round, paint, 0, mark.Opacity);
        }
    }

    internal static ChartPath Outline(VisualSceneMark mark) {
        if (mark is VisualScenePath path) return new ChartPath(path.Commands);
        if (mark is VisualSceneRectangle rect) return rect.Outline();
        if (mark is VisualSceneEllipse ellipse) return ChartPathBuilder.Ellipse(ellipse.Cx, ellipse.Cy, ellipse.Rx, ellipse.Ry);
        if (mark is VisualSceneSlice slice) {
            var commands = new List<ChartPathCommand>();
            foreach (var contour in VisualSceneGeometry.Flatten(slice, 4))
                for (var point = 0; point < contour.Count; point++) commands.Add(point == 0
                    ? ChartPathCommand.MoveTo(contour[point].X, contour[point].Y) : ChartPathCommand.LineTo(contour[point].X, contour[point].Y));
            return new ChartPath(commands);
        }
        if (mark is VisualSceneLine line) return new ChartPath(new[] {
            ChartPathCommand.MoveTo(line.Start.X, line.Start.Y), ChartPathCommand.LineTo(line.End.X, line.End.Y) });
        throw new NotSupportedException("This native mark has no chart styling contour.");
    }
}
