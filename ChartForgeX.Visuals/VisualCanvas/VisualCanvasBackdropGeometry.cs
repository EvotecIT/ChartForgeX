using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.Composition;

/// <summary>Keeps the authored backdrop outline common to vector and density-aware raster output.</summary>
internal static class VisualCanvasBackdropGeometry {
    private static ChartPath Horizon(double width, double height) => new(new[] {
        ChartPathCommand.MoveTo(0, height * .78),
        ChartPathCommand.CubicTo(width * .20, height * .72, width * .38, height * .82, width * .55, height * .76),
        ChartPathCommand.CubicTo(width * .74, height * .70, width * .84, height * .85, width, height * .73),
        ChartPathCommand.LineTo(width, height),
        ChartPathCommand.LineTo(0, height)
    });

    internal static string HorizonSvg(double width, double height) {
        var data = new SvgPathDataBuilder();
        foreach (var command in Horizon(width, height).Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) data.MoveTo(command.X, command.Y);
            else if (command.Kind == ChartPathCommandKind.LineTo) data.LineTo(command.X, command.Y);
            else data.CubicTo(command.Control1X, command.Control1Y, command.Control2X, command.Control2Y, command.X, command.Y);
        }
        return data.Close().Build();
    }

    internal static List<List<ChartPoint>> HorizonContours(double width, double height, double pixelsPerUnit) =>
        VisualSceneGeometry.Flatten(new VisualScenePath(Horizon(width, height), true, null, null, 0, null, null), pixelsPerUnit);
}
