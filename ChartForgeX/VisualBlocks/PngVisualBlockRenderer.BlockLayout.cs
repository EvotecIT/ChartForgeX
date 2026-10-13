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
                DrawBlockLayoutEdge(canvas, edge, source, target, theme);
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
            var radius = VisualBlockRendering.BlockLayoutCircleRadius(rect.Width, rect.Height);
            canvas.DrawCircle(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, radius, accent.WithAlpha(42));
            canvas.DrawCircleOutline(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, radius, accent, 1);
        } else {
            var radius = item.Shape == BlockLayoutShape.Rounded ? Math.Min(14, rect.Height * 0.28) : Math.Min(7, rect.Height * 0.18);
            canvas.FillRoundedRect(rect.X, rect.Y, rect.Width, rect.Height, radius, accent.WithAlpha(42));
            canvas.StrokeRoundedRect(rect.X, rect.Y, rect.Width, rect.Height, radius, accent, 1);
            if (item.Shape == BlockLayoutShape.Database) {
                // The same curve the SVG block-layout-database-cap path draws.
                var cap = FormattableString.Invariant($"M {rect.X + 8} {rect.Y + 10} C {rect.X + rect.Width * 0.25} {rect.Y + 2} {rect.X + rect.Width * 0.75} {rect.Y + 2} {rect.X + rect.Width - 8} {rect.Y + 10}");
                canvas.StrokePathData(cap, accent, 1, RasterLineCap.Butt, RasterLineJoin.Miter);
            }
        }

        var caption = VisualBlockRendering.BlockLayoutCaptionBounds(item.Shape, rect.X, rect.Y, rect.Width, rect.Height);
        var style = VisualBlockRendering.BlockLayoutText(theme, item.Label, Math.Max(10, Math.Min(14, rect.Height * 0.26)), caption.Width, caption.Height);
        DrawAlignedTextAtBaseline(canvas, item.Label, caption.X, VisualBlockRendering.BlockLayoutBaseline(style, rect.Y + rect.Height / 2), caption.Width, TextAlignment.Center, theme.Text, style.FontSize, true);
    }

    private static void DrawBlockLayoutEdge(RgbaCanvas canvas, BlockLayoutEdge edge, BlockLayoutRect source, BlockLayoutRect target, ChartForgeX.Themes.ChartTheme theme) {
        var route = BlockEdgeRoute(source, target);
        canvas.DrawLine(route.X1, route.Y1, route.X2, route.Y2, theme.Axis, 1.6);
        if (edge.Directed) DrawBlockArrowHead(canvas, route, theme.Axis);
        if (edge.Label.Length == 0) return;
        var style = VisualBlockRendering.BlockLayoutText(theme, edge.Label, 10.5, 90, Math.Min(16, Math.Min(source.Height, target.Height) * .4));
        DrawAlignedTextAtBaseline(canvas, edge.Label, (route.X1 + route.X2) / 2 - 45, VisualBlockRendering.BlockLayoutBaseline(style, (route.Y1 + route.Y2) / 2 - 7), 90, TextAlignment.Center, theme.MutedText, style.FontSize, true);
    }

    private static void DrawBlockArrowHead(RgbaCanvas canvas, VisualBlockRendering.BlockLayoutEdgeRoute route, ChartColor color) {
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
            rects[placement.Item.Id] = new BlockLayoutRect(x, y, width, geometry.CellHeight, placement.Item.Shape);
        }

        return rects;
    }

    private static VisualBlockRendering.BlockLayoutEdgeRoute BlockEdgeRoute(BlockLayoutRect source, BlockLayoutRect target) =>
        VisualBlockRendering.BlockLayoutRoute(source.Bounds, source.Shape, target.Bounds, target.Shape);

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
        public BlockLayoutRect(double x, double y, double width, double height, BlockLayoutShape shape) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Shape = shape;
        }

        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        public BlockLayoutShape Shape { get; }
        public ChartRect Bounds => new(X, Y, Width, Height);
    }

}
