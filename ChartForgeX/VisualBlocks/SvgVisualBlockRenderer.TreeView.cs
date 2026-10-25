using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderTreeView(SvgMarkupWriter writer, TreeViewBlock block) {
        var theme = block.Options.ResolvedTheme;
        var content = VisualBlockRendering.ContentRect(block.Options); var y = content.Top;
        RenderBlockHeading(writer, block, ref y, content.Left, content.Width);
        var rows = VisualBlockRendering.TreeViewRows(block, content.Width);
        if (block.Options.Size.Width > 8192 || block.Options.Size.Height > 8192 || rows[rows.Count - 1].Bottom > content.Bottom - y + .001)
            throw new InvalidOperationException("The tree does not fit its native frame. Call WithNaturalSize after changing rows or typography.");
        var byId = new Dictionary<string, VisualBlockRendering.TreeViewRowPlan>(StringComparer.Ordinal);
        var lastChildren = new Dictionary<string, VisualBlockRendering.TreeViewRowPlan>(StringComparer.Ordinal);
        foreach (var row in rows) { byId.Add(row.Item.Id, row); if (row.Item.ParentId != null) lastChildren[row.Item.ParentId] = row; }
        foreach (var pair in lastChildren) {
            var parent = byId[pair.Key]; var child = pair.Value; var trunk = content.Left + (child.Depth - .5) * block.RowIndent;
            TreeViewLine(writer, trunk, y + parent.Bottom, trunk, y + child.Top + block.RowPadding + child.LabelHeight / 2, theme.CardBorder, pair.Key);
        }
        foreach (var row in rows) {
            var item = row.Item; var labelTop = y + row.Top + block.RowPadding;
            var labelX = content.Left + row.X; var center = labelTop + row.LabelHeight / 2;
            writer.StartElement("g").Attribute("data-cfx-role", "treeview-row").Attribute("data-item-id", item.Id).Attribute("data-parent-id", item.ParentId)
                .Attribute("data-depth", row.Depth).Attribute("data-kind", row.Directory ? "directory" : "file")
                .Attribute("data-source-classes", item.Classes.Count == 0 ? null : string.Join(" ", item.Classes))
                .Attribute("data-icon-reference", item.IconReference).EndStartElement();
            writer.StartElement("title").Text(item.Description.Length == 0 ? item.Label : item.Label + ": " + item.Description).EndElement();
            if (item.Highlighted) writer.StartElement("rect").Attribute("data-cfx-role", "treeview-highlight").Attribute("x", content.Left)
                .Attribute("y", y + row.Top).Attribute("width", content.Width).Attribute("height", row.Height).Attribute("rx", 3)
                .Attribute("fill", theme.Info.WithAlpha(28).ToCss()).Attribute("stroke", theme.Info.ToCss()).EndEmptyElement();
            if (item.ParentId != null) TreeViewLine(writer, content.Left + (row.Depth - .5) * block.RowIndent, center,
                content.Left + row.Depth * block.RowIndent - 4, center, theme.CardBorder, item.ParentId);
            if (row.Icon != TreeViewIcon.None) RenderTreeViewIcon(writer, row.Icon, labelX - row.IconSize - 7, center - row.IconSize / 2, row.IconSize, theme.MutedText, row.Label);
            TreeViewText(writer, item.Label, labelX, labelTop + (row.LabelHeight - TextLayoutEngine.Measure("Mg", row.Label).Height) / 2,
                row.Width, row.Label, theme.Text, "treeview-label");
            if (item.Description.Length > 0) {
                var advance = row.InlineDescription ? TextLayoutEngine.Measure(VisualBlockRendering.SingleLineText(item.Label), row.Label).Width + 16 : 0;
                var descriptionTop = row.InlineDescription ? labelTop + (row.LabelHeight - TextLayoutEngine.Measure("Mg", row.Description).Height) / 2 : labelTop + row.LabelHeight + 3;
                TreeViewText(writer, item.Description, labelX + advance, descriptionTop, row.Width - advance, row.Description, theme.MutedText, "treeview-description");
            }
            writer.EndElement().Line();
        }
    }

    private static void TreeViewText(SvgMarkupWriter writer, string text, double x, double top, double width, TextStyle style, ChartColor color, string role) {
        var value = ChartForgeX.Rendering.ChartTextFitting.FitEnd(VisualBlockRendering.SingleLineText(text), width,
            candidate => TextLayoutEngine.Measure(candidate, style).Width, "...");
        writer.StartElement("text").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", top + VisualBlockRendering.TextAscent(style))
            .Attribute("font-family", style.Font.Family).Attribute("font-size", style.FontSize).Attribute("font-weight", style.Font.Weight)
            .Attribute("font-style", style.Font.Italic ? "italic" : null).Attribute("fill", color.ToCss()).Text(value).EndElement();
    }
    private static void TreeViewLine(SvgMarkupWriter writer, double x1, double y1, double x2, double y2, ChartColor color, string parentId) =>
        writer.StartElement("line").Attribute("data-cfx-role", "treeview-connector").Attribute("data-parent-id", parentId)
            .Attribute("x1", x1).Attribute("y1", y1).Attribute("x2", x2).Attribute("y2", y2).Attribute("stroke", color.ToCss()).Attribute("stroke-width", 1).EndEmptyElement();
    private static void RenderTreeViewIcon(SvgMarkupWriter writer, TreeViewIcon icon, double x, double y, double size, ChartColor color, TextStyle labelStyle) {
        writer.StartElement("g").Attribute("data-cfx-role", "treeview-icon").Attribute("data-icon", icon.ToString()).EndStartElement();
        if (icon == TreeViewIcon.Unknown) {
            TreeViewText(writer, "?", x + size * .2, y, size, labelStyle, color, "treeview-unknown-icon");
        } else {
            var k = size / 16;
            var path = icon == TreeViewIcon.Folder
                ? "M" + FormatPoint(x, y + 3 * k) + "h" + F(6 * k) + "l" + FormatPoint(2 * k, 2 * k) + "h" + F(8 * k) + "v" + F(9 * k) + "H" + F(x) + "Z"
                : "M" + FormatPoint(x + 3 * k, y + k) + "h" + F(7 * k) + "l" + FormatPoint(4 * k, 4 * k) + "v" + F(10 * k) + "H" + F(x + 3 * k) + "Z M" + FormatPoint(x + 10 * k, y + k) + "v" + F(4 * k) + "h" + F(4 * k);
            writer.StartElement("path").Attribute("d", path).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", 1.25).EndEmptyElement();
        }
        writer.EndElement();
    }
}
