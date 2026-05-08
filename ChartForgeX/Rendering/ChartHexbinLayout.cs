using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartHexbinLayout {
    public static ChartHexbinGrid Build(ChartRect plot, int rowCount, int columnCount) {
        var widthUnits = Math.Sqrt(3) * (columnCount + 0.5);
        var heightUnits = rowCount <= 1 ? 2 : 1.5 * (rowCount - 1) + 2;
        var radius = Math.Max(2, Math.Min(plot.Width / Math.Max(1, widthUnits), plot.Height / Math.Max(1, heightUnits)) * 0.94);
        var hexWidth = Math.Sqrt(3) * radius;
        var gridWidth = hexWidth * (columnCount + 0.5);
        var gridHeight = radius * heightUnits;
        var left = plot.Left + Math.Max(0, (plot.Width - gridWidth) / 2);
        var top = plot.Top + Math.Max(0, (plot.Height - gridHeight) / 2);
        return new ChartHexbinGrid(left, top, radius, hexWidth, hexWidth, radius * 1.5);
    }

    public static IReadOnlyList<ChartPoint> Points(double cx, double cy, double radius) {
        var points = new List<ChartPoint>(6);
        for (var i = 0; i < 6; i++) {
            var angle = Math.PI / 6 + i * Math.PI / 3;
            points.Add(new ChartPoint(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius));
        }

        return points;
    }
}

internal readonly struct ChartHexbinGrid {
    public ChartHexbinGrid(double left, double top, double radius, double hexWidth, double columnStep, double rowStep) {
        Left = left;
        Top = top;
        Radius = radius;
        HexWidth = hexWidth;
        ColumnStep = columnStep;
        RowStep = rowStep;
    }

    public double Left { get; }

    public double Top { get; }

    public double Radius { get; }

    public double HexWidth { get; }

    public double ColumnStep { get; }

    public double RowStep { get; }
}
