using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderBlockHierarchy(SvgMarkupWriter writer, BlockLayoutBlock block, ChartRect frame, string id) {
        var plan = VisualBlockRendering.BlockHierarchy(block, frame);
        var theme = block.Options.ResolvedTheme;
        var paletteRules = TypographyPaletteCss.Rules(theme.FontFamily, block.Items.Select(item => item.TextStyle).ToArray());
        if (paletteRules.Length > 0) writer.StartElement("style").Text(paletteRules).EndElement();
        // Containers paint before connectors; leaf marks then cover only the line interior.
        foreach (var cell in plan.Cells.Where(cell => cell.Item.IsComposite).OrderBy(cell => cell.Depth)) WriteHierarchyCell(writer, cell, theme, id);
        if (block.ShowEdges) foreach (var edge in block.Edges) {
            if (!edge.Visible) continue;
            var stroke = edge.Stroke; var edgeColor = stroke?.Color ?? theme.Axis;
            var source = plan.ById[edge.SourceId]; var target = plan.ById[edge.TargetId];
            var s = Center(source.Bounds); var t = Center(target.Bounds);
            var toward = s.X == t.X && s.Y == t.Y ? new ChartPoint(s.X + 1, s.Y) : t;
            var start = BlockCellBoundary(source, s, toward);
            var end = BlockCellBoundary(target, t, start);
            // Ancestor/descendant surfaces can be nested. Attach to the actual opposite
            // port, rather than pointing a child port through its own opaque interior.
            start = BlockCellBoundary(source, s, end);
            end = BlockCellBoundary(target, t, start);
            var route = new VisualBlockRendering.BlockLayoutEdgeRoute(start.X, start.Y, end.X, end.Y);
            writer.StartElement("line").Attribute("data-cfx-role", "block-layout-edge")
                .Attribute("data-source-id", edge.SourceId).Attribute("data-target-id", edge.TargetId)
                .Attribute("x1", F(start.X)).Attribute("y1", F(start.Y)).Attribute("x2", F(end.X)).Attribute("y2", F(end.Y))
                .Attribute("stroke", edgeColor.ToCss()).Attribute("stroke-width", stroke?.Width ?? 1.6)
                .Attribute("stroke-opacity", stroke == null || stroke.Opacity == 1 ? null : F(stroke.Opacity))
                .Attribute("stroke-dasharray", stroke?.Dash == null || stroke.Dash.Count == 0 ? null : string.Join(" ", stroke.Dash.Select(F)))
                .EndEmptyElement().Line();
            if (edge.Directed && (stroke?.Width ?? 1.6) > 0)
                WriteBlockArrowHead(writer, route, edgeColor.WithAlpha((byte)Math.Round(edgeColor.A * (stroke?.Opacity ?? 1))));
            if (edge.Label.Length > 0) {
                var style = VisualBlockRendering.BlockLayoutText(theme, edge.Label, 10.5, 90, 16);
                WriteText(writer, edge.Label, (start.X + end.X) / 2 - 45,
                    VisualBlockRendering.BlockLayoutBaseline(style, (start.Y + end.Y) / 2 - 7), 90,
                    TextAlignment.Center, theme.MutedText, theme.FontFamily, style.FontSize, "700", "block-layout-connector-label");
            }
        }
        foreach (var cell in plan.Cells) if (!cell.Item.IsSpace && !cell.Item.IsComposite) WriteHierarchyCell(writer, cell, theme, id);
    }

    private static void WriteHierarchyCell(SvgMarkupWriter writer, VisualBlockRendering.BlockHierarchyCell cell, ResolvedVisualTheme theme, string id) {
        var item = cell.Item; var bounds = cell.Bounds;
        var accent = VisualBlockRendering.PaletteAt(theme, cell.SourceIndex);
        var stroke = item.Stroke; var strokeColor = stroke?.Color ?? accent;
        var fill = item.Fill ?? accent.WithAlpha(item.IsComposite ? (byte)15 : (byte)42);
        writer.StartElement("g").Attribute("data-cfx-role", item.IsComposite ? "block-layout-composite" : "block-layout-item")
            .Attribute("data-block-id", item.Id).Attribute("data-parent-id", item.ParentId)
            .Attribute("data-source-index", cell.SourceIndex).Attribute("data-depth", cell.Depth)
            .Attribute("data-source-classes", item.Metadata.TryGetValue("mermaid.classes", out var classes) ? classes : null)
            .Attribute("data-source-line", item.Metadata.TryGetValue("mermaid.source.line", out var line) ? line : null)
            .Attribute("data-source-column", item.Metadata.TryGetValue("mermaid.source.column", out var column) ? column : null)
            .Attribute("data-x", F(bounds.X)).Attribute("data-y", F(bounds.Y)).Attribute("data-width", F(bounds.Width)).Attribute("data-height", F(bounds.Height))
            .EndStartElement();
        writer.StartElement("title").Text(item.Label.Length == 0 ? item.Id : item.Label).EndElement();
        writer.StartElement("g").Attribute("data-cfx-role", "block-layout-node").Attribute("data-block-id", item.Id)
            .Attribute("data-shape", item.Shape.ToString()).EndStartElement();
        if (item.Shape is BlockLayoutShape.Circle or BlockLayoutShape.DoubleCircle) {
            var radius = Math.Min(bounds.Width, bounds.Height) / 2;
            Circle(radius, fill);
            if (item.Shape == BlockLayoutShape.DoubleCircle && radius > 5) Circle(radius - 5, null);
        } else if (item.Shape == BlockLayoutShape.Arrow) Path(BlockCellPath(cell), fill);
        else foreach (var part in TopologyNodeShapeGeometry.SceneParts(BlockCellNode(cell)))
            Path(new SvgPathDataBuilder().Append(part.Path.Commands, part.Close).Build(), part.Fill ? fill : null);
        writer.EndElement();

        void PaintStroke() => writer.Attribute("stroke", strokeColor.ToCss()).Attribute("stroke-width", stroke?.Width ?? 1)
            .Attribute("stroke-opacity", stroke == null || stroke.Opacity == 1 ? null : F(stroke.Opacity))
            .Attribute("stroke-dasharray", stroke?.Dash == null || stroke.Dash.Count == 0 ? null : string.Join(" ", stroke.Dash.Select(F)));
        void Path(string path, ChartColor? paint) {
            writer.StartElement("path").Attribute("d", path).Attribute("fill", paint?.ToCss() ?? "none");
            PaintStroke(); writer.EndEmptyElement().Line();
        }
        void Circle(double radius, ChartColor? paint) {
            writer.StartElement("circle").Attribute("cx", F(bounds.Left + bounds.Width / 2)).Attribute("cy", F(bounds.Top + bounds.Height / 2))
                .Attribute("r", F(radius)).Attribute("fill", paint?.ToCss() ?? "none");
            PaintStroke(); writer.EndEmptyElement().Line();
        }
        if (item.Label.Length > 0) {
            ChartRect caption;
            if (item.IsComposite) {
                var inset = Math.Min(12, Math.Min(bounds.Width, bounds.Height) * .1);
                caption = new ChartRect(bounds.Left + inset, bounds.Top + inset, Math.Max(0, bounds.Width - 2 * inset), Math.Min(22, bounds.Height * .25));
            } else if (item.Shape == BlockLayoutShape.Arrow) {
                var directions = item.ArrowDirections;
                caption = TopologyNodeShapeGeometry.DirectionalArrowCaption(bounds,
                    (directions & BlockLayoutArrowDirections.Horizontal) != 0, (directions & BlockLayoutArrowDirections.Vertical) != 0);
            } else caption = TopologyNodeShapeGeometry.CaptionBounds(BlockCellNode(cell), Math.Min(26, bounds.Height * .5));
            var clipId = id + "-block-caption-" + cell.SourceIndex.ToString(CultureInfo.InvariantCulture);
            writer.StartElement("defs").EndStartElement().StartElement("clipPath").Attribute("id", clipId).EndStartElement()
                .StartElement("rect").Attribute("x", F(caption.X)).Attribute("y", F(caption.Y)).Attribute("width", F(caption.Width)).Attribute("height", F(caption.Height))
                .EndEmptyElement().EndElement().EndElement();
            writer.StartElement("g").Attribute("clip-path", "url(#" + clipId + ")").EndStartElement();
            var style = FitBlockCaption(item, theme, caption);
            var value = ChartTextFitting.FitEnd(VisualBlockRendering.SingleLineText(item.TextStyle.TransformText(item.Label)),
                caption.Width, candidate => TextLayoutEngine.Measure(candidate, style).Width, "...");
            writer.StartElement("text").Attribute("data-cfx-role", "block-layout-node-label").Attribute("data-block-id", item.Id)
                .Attribute("x", F(caption.Left + caption.Width / 2)).Attribute("y", F(VisualBlockRendering.BlockLayoutBaseline(style, caption.Top + caption.Height / 2)))
                .Attribute("text-anchor", "middle").Attribute("fill", style.Color.ToCss()).Attribute("font-family", style.Font.Family)
                .Attribute("font-size", F(style.FontSize)).Attribute("font-weight", style.Font.Weight)
                .Attribute("font-style", style.Font.Italic ? "italic" : null)
                .Attribute("style", TypographyCss.Role(item.TextStyle)).Text(value).EndElement().Line();
            writer.EndElement();
        }
        writer.EndElement().Line();
    }

    private static TextStyle FitBlockCaption(BlockLayoutItem item, ResolvedVisualTheme theme, ChartRect caption) {
        var fallback = VisualBlockRendering.BlockLayoutText(theme, item.Label, 14, Math.Max(1, caption.Width), Math.Max(1, caption.Height));
        fallback.Color = theme.Text;
        var style = item.TextStyle.Resolve(fallback);
        // Font fitting starts from a finite frame-related ceiling, so a valid but enormous
        // authored size cannot overflow the first measurement before fitting begins.
        style.FontSize = Math.Min(style.FontSize, Math.Max(1, Math.Max(caption.Width, caption.Height)) * 2);
        var text = VisualBlockRendering.SingleLineText(item.TextStyle.TransformText(item.Label));
        if (!Fits(style.FontSize)) style.FontSize = Math.Max(.001, ChartTextFitting.FitFontSize(0, style.FontSize, Fits));
        return style;
        bool Fits(double size) {
            style.FontSize = size;
            return TextLayoutEngine.Measure(text, style).Width <= Math.Max(0, caption.Width) && TextLayoutEngine.Measure("Mg", style).Height <= Math.Max(0, caption.Height);
        }
    }

    private static ChartPoint BlockCellBoundary(VisualBlockRendering.BlockHierarchyCell cell, ChartPoint center, ChartPoint toward) =>
        cell.Item.Shape is BlockLayoutShape.Circle or BlockLayoutShape.DoubleCircle
            ? VisualBlockRendering.CircleBoundary(center.X, center.Y, toward.X, toward.Y, cell.Bounds, minimumRadius: false)
            : cell.Item.Shape == BlockLayoutShape.Arrow
                ? TopologyNodeShapeGeometry.PathBoundaryPoint(BlockCellPath(cell), center, toward)
                : TopologyNodeShapeGeometry.FilledBoundaryPoint(BlockCellNode(cell), center, toward);

    private static ChartPoint Center(ChartRect bounds) => new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
    private static TopologyNode BlockCellNode(VisualBlockRendering.BlockHierarchyCell cell) => new() {
        Id = cell.Item.Id, Label = cell.Item.Label, X = cell.Bounds.X, Y = cell.Bounds.Y, Width = cell.Bounds.Width, Height = cell.Bounds.Height,
        Shape = cell.Item.Shape switch {
            BlockLayoutShape.Rounded => TopologyNodeShape.Rounded,
            BlockLayoutShape.Circle => TopologyNodeShape.Circle,
            BlockLayoutShape.Database => TopologyNodeShape.Cylinder,
            BlockLayoutShape.Diamond => TopologyNodeShape.Diamond,
            BlockLayoutShape.Hexagon => TopologyNodeShape.Hexagon,
            BlockLayoutShape.Stadium => TopologyNodeShape.Stadium,
            BlockLayoutShape.Subroutine => TopologyNodeShape.Subroutine,
            BlockLayoutShape.DoubleCircle => TopologyNodeShape.DoubleCircle,
            BlockLayoutShape.Parallelogram => TopologyNodeShape.Parallelogram,
            BlockLayoutShape.ParallelogramAlt => TopologyNodeShape.ParallelogramAlt,
            BlockLayoutShape.Trapezoid => TopologyNodeShape.Trapezoid,
            BlockLayoutShape.TrapezoidAlt => TopologyNodeShape.TrapezoidAlt,
            BlockLayoutShape.Asymmetric => TopologyNodeShape.Asymmetric,
            _ => TopologyNodeShape.Rectangle
        }
    };
    private static string BlockCellPath(VisualBlockRendering.BlockHierarchyCell cell) {
        if (cell.Item.Shape != BlockLayoutShape.Arrow) throw new InvalidOperationException("Only directional arrows use an explicit block contour.");
        var directions = cell.Item.ArrowDirections;
        return TopologyNodeShapeGeometry.DirectionalArrow(cell.Bounds,
            (directions & BlockLayoutArrowDirections.Left) != 0, (directions & BlockLayoutArrowDirections.Right) != 0,
            (directions & BlockLayoutArrowDirections.Up) != 0, (directions & BlockLayoutArrowDirections.Down) != 0);
    }
}
