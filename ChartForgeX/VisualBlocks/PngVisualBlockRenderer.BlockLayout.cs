using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawBlockLayout(RgbaCanvas canvas, BlockLayoutBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        var placements = VisualBlockRendering.BlockLayoutPlacements(block);
        if (placements.Count == 0) return;

        var geometry = CreateBlockLayoutGeometry(block, placements, content, y, options.Padding.Bottom, options.Size.Height);
        var rects = BlockLayoutRects(block, placements, geometry);
        if (block.ShowEdges) {
            foreach (var edge in block.Edges) {
                if (!rects.TryGetValue(edge.SourceId, out var source) || !rects.TryGetValue(edge.TargetId, out var target)) continue;
                DrawBlockLayoutEdge(canvas, edge, source, target, theme.Axis, theme.MutedText);
            }
        }

        for (var index = 0; index < placements.Count; index++) {
            var placement = placements[index];
            if (placement.Item.IsSpace || !rects.TryGetValue(placement.Item.Id, out var rect)) continue;
            DrawBlockLayoutNode(canvas, placement.Item, index, rect, theme);
        }
    }

    private static void DrawBlockLayoutNode(RgbaCanvas canvas, BlockLayoutItem item, int index, BlockLayoutRect rect, ChartForgeX.Themes.ChartTheme theme) {
        var accent = VisualBlockRendering.PaletteAt(theme, index);
        if (item.Shape == BlockLayoutShape.Circle) {
            var radius = Math.Max(8, Math.Min(rect.Width, rect.Height) / 2);
            canvas.DrawCircle(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, radius, accent.WithAlpha(42));
            canvas.DrawCircleOutline(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, radius, accent, 1);
        } else {
            var radius = item.Shape == BlockLayoutShape.Rounded ? Math.Min(14, rect.Height * 0.28) : Math.Min(7, rect.Height * 0.18);
            canvas.FillRoundedRect(rect.X, rect.Y, rect.Width, rect.Height, radius, accent.WithAlpha(42));
            canvas.StrokeRoundedRect(rect.X, rect.Y, rect.Width, rect.Height, radius, accent, 1);
            if (item.Shape == BlockLayoutShape.Database) {
                var capY = rect.Y + 10;
                canvas.DrawLine(rect.X + 8, capY, rect.X + rect.Width * 0.28, rect.Y + 4, accent, 1);
                canvas.DrawLine(rect.X + rect.Width * 0.28, rect.Y + 4, rect.X + rect.Width * 0.72, rect.Y + 4, accent, 1);
                canvas.DrawLine(rect.X + rect.Width * 0.72, rect.Y + 4, rect.X + rect.Width - 8, capY, accent, 1);
            }
        }

        var fontSize = Math.Max(10, Math.Min(14, rect.Height * 0.26));
        DrawCenteredTextMiddle(canvas, item.Label, rect.X + rect.Width / 2, rect.Y + rect.Height / 2, fontSize, theme.Text, true, Math.Max(1, rect.Width - 16));
    }

    private static void DrawBlockLayoutEdge(RgbaCanvas canvas, BlockLayoutEdge edge, BlockLayoutRect source, BlockLayoutRect target, ChartColor lineColor, ChartColor textColor) {
        var route = BlockEdgeRoute(source, target);
        canvas.DrawLine(route.X1, route.Y1, route.X2, route.Y2, lineColor, 1.6);
        if (edge.Directed) DrawBlockArrowHead(canvas, route, lineColor);
        if (edge.Label.Length == 0) return;
        DrawCenteredTextMiddle(canvas, edge.Label, (route.X1 + route.X2) / 2, (route.Y1 + route.Y2) / 2 - 7, 10.5, textColor, true, 90);
    }

    private static void DrawBlockArrowHead(RgbaCanvas canvas, BlockLayoutEdgeRoute route, ChartColor color) {
        var angle = Math.Atan2(route.Y2 - route.Y1, route.X2 - route.X1);
        var size = 8.0;
        var a1 = angle + Math.PI * 0.84;
        var a2 = angle - Math.PI * 0.84;
        var p1 = new ChartPoint(route.X2 + Math.Cos(a1) * size, route.Y2 + Math.Sin(a1) * size);
        var p2 = new ChartPoint(route.X2 + Math.Cos(a2) * size, route.Y2 + Math.Sin(a2) * size);
        canvas.FillPolygon(new[] { new ChartPoint(route.X2, route.Y2), p1, p2 }, color);
    }

    private static BlockLayoutGeometry CreateBlockLayoutGeometry(BlockLayoutBlock block, IReadOnlyList<VisualBlockRendering.BlockLayoutPlacement> placements, ChartRect content, double y, double bottomPadding, double totalHeight) {
        var rows = 0;
        for (var index = 0; index < placements.Count; index++) rows = Math.Max(rows, placements[index].Row + 1);
        var columnGap = Math.Min(18, Math.Max(8, content.Width * 0.018));
        var rowGap = Math.Min(22, Math.Max(10, content.Height * 0.035));
        var cellWidth = Math.Max(8, (content.Width - columnGap * (block.Columns - 1)) / block.Columns);
        var available = Math.Max(24, totalHeight - bottomPadding - y);
        var cellHeight = Math.Max(26, Math.Min(78, (available - rowGap * Math.Max(0, rows - 1)) / Math.Max(1, rows)));
        return new BlockLayoutGeometry(content.X, y, cellWidth, cellHeight, columnGap, rowGap);
    }

    private static Dictionary<string, BlockLayoutRect> BlockLayoutRects(BlockLayoutBlock block, IReadOnlyList<VisualBlockRendering.BlockLayoutPlacement> placements, BlockLayoutGeometry geometry) {
        var rects = new Dictionary<string, BlockLayoutRect>(StringComparer.Ordinal);
        for (var index = 0; index < placements.Count; index++) {
            var placement = placements[index];
            if (placement.Item.IsSpace) continue;
            var x = geometry.X + placement.Column * (geometry.CellWidth + geometry.ColumnGap);
            var y = geometry.Y + placement.Row * (geometry.CellHeight + geometry.RowGap);
            var width = placement.ColumnSpan * geometry.CellWidth + (placement.ColumnSpan - 1) * geometry.ColumnGap;
            rects[placement.Item.Id] = new BlockLayoutRect(x, y, width, geometry.CellHeight);
        }

        return rects;
    }

    private static BlockLayoutEdgeRoute BlockEdgeRoute(BlockLayoutRect source, BlockLayoutRect target) {
        var sx = source.X + source.Width / 2;
        var sy = source.Y + source.Height / 2;
        var tx = target.X + target.Width / 2;
        var ty = target.Y + target.Height / 2;
        if (Math.Abs(ty - sy) < Math.Max(source.Height, target.Height) * 0.65) {
            if (tx >= sx) return new BlockLayoutEdgeRoute(source.X + source.Width, sy, target.X, ty);
            return new BlockLayoutEdgeRoute(source.X, sy, target.X + target.Width, ty);
        }

        if (ty >= sy) return new BlockLayoutEdgeRoute(sx, source.Y + source.Height, tx, target.Y);
        return new BlockLayoutEdgeRoute(sx, source.Y, tx, target.Y + target.Height);
    }

    private readonly struct BlockLayoutGeometry {
        public BlockLayoutGeometry(double x, double y, double cellWidth, double cellHeight, double columnGap, double rowGap) {
            X = x;
            Y = y;
            CellWidth = cellWidth;
            CellHeight = cellHeight;
            ColumnGap = columnGap;
            RowGap = rowGap;
        }

        public double X { get; }
        public double Y { get; }
        public double CellWidth { get; }
        public double CellHeight { get; }
        public double ColumnGap { get; }
        public double RowGap { get; }
    }

    private readonly struct BlockLayoutRect {
        public BlockLayoutRect(double x, double y, double width, double height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
    }

    private readonly struct BlockLayoutEdgeRoute {
        public BlockLayoutEdgeRoute(double x1, double y1, double x2, double y2) {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }
    }
}
