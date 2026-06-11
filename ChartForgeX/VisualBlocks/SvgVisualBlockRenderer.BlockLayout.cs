using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderBlockLayout(SvgMarkupWriter writer, BlockLayoutBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var placements = VisualBlockRendering.BlockLayoutPlacements(block);
        if (placements.Count == 0) return;

        var geometry = CreateBlockLayoutGeometry(block, placements, content, y, options.Padding.Bottom, options.Size.Height);
        var rects = BlockLayoutRects(block, placements, geometry);
        if (block.ShowEdges) {
            foreach (var edge in block.Edges) {
                if (!rects.TryGetValue(edge.SourceId, out var source) || !rects.TryGetValue(edge.TargetId, out var target)) continue;
                WriteBlockLayoutEdge(writer, edge, source, target, theme);
            }
        }

        for (var index = 0; index < placements.Count; index++) {
            var placement = placements[index];
            if (placement.Item.IsSpace || !rects.TryGetValue(placement.Item.Id, out var rect)) continue;
            WriteBlockLayoutNode(writer, placement.Item, index, rect, theme);
        }
    }

    private static void WriteBlockLayoutNode(SvgMarkupWriter writer, BlockLayoutItem item, int index, BlockLayoutRect rect, ChartForgeX.Themes.ChartTheme theme) {
        var accent = VisualBlockRendering.PaletteAt(theme, index);
        if (item.Shape == BlockLayoutShape.Circle) {
            var radius = Math.Max(8, Math.Min(rect.Width, rect.Height) / 2);
            writer.StartElement("circle")
                .Attribute("data-cfx-role", "block-layout-node")
                .Attribute("data-block-id", item.Id)
                .Attribute("cx", F(rect.X + rect.Width / 2))
                .Attribute("cy", F(rect.Y + rect.Height / 2))
                .Attribute("r", F(radius))
                .Attribute("fill", accent.WithAlpha(42).ToCss())
                .Attribute("stroke", accent.ToCss())
                .EndEmptyElement()
                .Line();
        } else {
            var radius = item.Shape == BlockLayoutShape.Rounded ? Math.Min(14, rect.Height * 0.28) : Math.Min(7, rect.Height * 0.18);
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "block-layout-node")
                .Attribute("data-block-id", item.Id)
                .Attribute("x", F(rect.X))
                .Attribute("y", F(rect.Y))
                .Attribute("width", F(rect.Width))
                .Attribute("height", F(rect.Height))
                .Attribute("rx", F(radius))
                .Attribute("fill", accent.WithAlpha(42).ToCss())
                .Attribute("stroke", accent.ToCss())
                .EndEmptyElement()
                .Line();
            if (item.Shape == BlockLayoutShape.Database) {
                writer.StartElement("path")
                    .Attribute("data-cfx-role", "block-layout-database-cap")
                    .Attribute("d", "M " + F(rect.X + 8) + " " + F(rect.Y + 10) + " C " + F(rect.X + rect.Width * 0.25) + " " + F(rect.Y + 2) + ", " + F(rect.X + rect.Width * 0.75) + " " + F(rect.Y + 2) + ", " + F(rect.X + rect.Width - 8) + " " + F(rect.Y + 10))
                    .Attribute("fill", "none")
                    .Attribute("stroke", accent.ToCss())
                    .EndEmptyElement()
                    .Line();
            }
        }

        var fontSize = Math.Max(10, Math.Min(14, rect.Height * 0.26));
        WriteText(writer, item.Label, rect.X + 8, rect.Y + rect.Height * 0.56, Math.Max(1, rect.Width - 16), VisualTextAlignment.Center, theme.Text, theme.FontFamily, fontSize, "700");
    }

    private static void WriteBlockLayoutEdge(SvgMarkupWriter writer, BlockLayoutEdge edge, BlockLayoutRect source, BlockLayoutRect target, ChartForgeX.Themes.ChartTheme theme) {
        var route = BlockEdgeRoute(source, target);
        writer.StartElement("line")
            .Attribute("data-cfx-role", "block-layout-edge")
            .Attribute("data-source-id", edge.SourceId)
            .Attribute("data-target-id", edge.TargetId)
            .Attribute("x1", F(route.X1))
            .Attribute("y1", F(route.Y1))
            .Attribute("x2", F(route.X2))
            .Attribute("y2", F(route.Y2))
            .Attribute("stroke", theme.Axis.ToCss())
            .Attribute("stroke-width", 1.6)
            .Attribute("stroke-linecap", "round")
            .EndEmptyElement()
            .Line();

        if (edge.Directed) WriteBlockArrowHead(writer, route, theme.Axis);
        if (edge.Label.Length == 0) return;
        WriteText(writer, edge.Label, (route.X1 + route.X2) / 2 - 45, (route.Y1 + route.Y2) / 2 - 5, 90, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, 10.5, "600");
    }

    private static void WriteBlockArrowHead(SvgMarkupWriter writer, BlockLayoutEdgeRoute route, ChartColor color) {
        var angle = Math.Atan2(route.Y2 - route.Y1, route.X2 - route.X1);
        var a1 = angle + Math.PI * 0.84;
        var a2 = angle - Math.PI * 0.84;
        var size = 8.0;
        var p1x = route.X2 + Math.Cos(a1) * size;
        var p1y = route.Y2 + Math.Sin(a1) * size;
        var p2x = route.X2 + Math.Cos(a2) * size;
        var p2y = route.Y2 + Math.Sin(a2) * size;
        writer.StartElement("path")
            .Attribute("data-cfx-role", "block-layout-edge-arrow")
            .Attribute("d", "M " + F(route.X2) + " " + F(route.Y2) + " L " + F(p1x) + " " + F(p1y) + " L " + F(p2x) + " " + F(p2y) + " Z")
            .Attribute("fill", color.ToCss())
            .EndEmptyElement()
            .Line();
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
