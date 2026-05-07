using System;
using System.Globalization;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual blocks to self-contained SVG.
/// </summary>
public sealed class SvgVisualBlockRenderer {
    private static long ScopeCounter;

    /// <summary>Renders a visual block to SVG markup.</summary>
    public string Render(IVisualBlock block) => Render(block, NextScope());

    /// <summary>Renders a visual block to SVG markup with a caller-provided ID scope.</summary>
    public string Render(IVisualBlock block, string idScope) {
        VisualBlockRendering.Validate(block);
        var options = block.Options;
        var theme = options.Theme;
        var id = "cfx-visual-" + VisualBlockRendering.StableHash(idScope ?? string.Empty, block.AccessibleName, options.Size.Width.ToString(CultureInfo.InvariantCulture), options.Size.Height.ToString(CultureInfo.InvariantCulture));
        var writer = new SvgMarkupWriter(4096);
        writer.StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("width", options.Size.Width)
            .Attribute("height", options.Size.Height)
            .Attribute("viewBox", "0 0 " + options.Size.Width.ToString(CultureInfo.InvariantCulture) + " " + options.Size.Height.ToString(CultureInfo.InvariantCulture))
            .Attribute("role", "img")
            .Attribute("aria-labelledby", id + "-title " + id + "-desc")
            .Attribute("preserveAspectRatio", "xMidYMid meet")
            .Attribute("shape-rendering", "geometricPrecision")
            .Attribute("text-rendering", "geometricPrecision")
            .Attribute("style", "max-width:100%;height:auto;display:block")
            .EndStartElement()
            .Line()
            .StartElement("title").Attribute("id", id + "-title").Text(block.AccessibleName).EndElement()
            .Line()
            .StartElement("desc").Attribute("id", id + "-desc").Text("Static ChartForgeX visual block.").EndElement()
            .Line();

        if (!options.TransparentBackground && theme.Background.A > 0) writer.StartElement("rect").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", theme.Background.ToCss()).EndEmptyElement().Line();
        if (options.ShowCard && theme.UseCard) writer.StartElement("rect").Attribute("data-cfx-role", "visual-card").Attribute("x", 0).Attribute("y", 0).Attribute("width", options.Size.Width).Attribute("height", options.Size.Height).Attribute("rx", theme.CornerRadius).Attribute("fill", theme.CardBackground.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();

        if (block is ChartTable table) RenderTable(writer, table);
        else if (block is ChartList list) RenderList(writer, list);
        else if (block is MetricCard card) RenderMetric(writer, card);

        writer.EndElement().Line();
        return writer.Build();
    }

    private static void RenderBlockHeading(SvgMarkupWriter writer, IVisualBlock block, ref double y, double contentX, double contentWidth) {
        var theme = block.Options.Theme;
        if (block.Title.Length > 0) {
            writer.StartElement("text").Attribute("data-cfx-role", "visual-title").Attribute("x", contentX).Attribute("y", y + theme.TitleFontSize * 0.75).Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", theme.TitleFontSize).Attribute("font-weight", "800").Text(VisualBlockRendering.FitText(block.Title, theme.TitleFontSize, contentWidth)).EndElement().Line();
            y += theme.TitleFontSize + 7;
        }

        if (block.Subtitle.Length > 0) {
            writer.StartElement("text").Attribute("data-cfx-role", "visual-subtitle").Attribute("x", contentX).Attribute("y", y + theme.SubtitleFontSize * 0.75).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", theme.SubtitleFontSize).Text(VisualBlockRendering.FitText(block.Subtitle, theme.SubtitleFontSize, contentWidth)).EndElement().Line();
            y += theme.SubtitleFontSize + 13;
        } else if (block.Title.Length > 0) {
            y += 8;
        }
    }

    private static void RenderTable(SvgMarkupWriter writer, ChartTable table) {
        var options = table.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, table, ref y, content.X, content.Width);
        var headerHeight = table.Dense ? 26.0 : 32.0;
        var rowHeight = table.Dense ? 24.0 : 31.0;
        var widths = ColumnWidths(table, content.Width);
        if (table.ShowHeader) {
            writer.StartElement("rect").Attribute("data-cfx-role", "table-header").Attribute("x", content.X).Attribute("y", y).Attribute("width", content.Width).Attribute("height", headerHeight).Attribute("rx", Math.Min(6, theme.PlotCornerRadius)).Attribute("fill", theme.PlotBackground.ToCss()).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
            var x = content.X;
            for (var i = 0; i < table.Columns.Count; i++) {
                WriteText(writer, table.Columns[i].Header, x + 9, y + headerHeight * 0.66, widths[i] - 18, table.Columns[i].Alignment, theme.Text, theme.FontFamily, theme.SubtitleFontSize, "700");
                x += widths[i];
            }

            y += headerHeight + 4;
        }

        var maxRows = Math.Max(0, (int)Math.Floor((options.Size.Height - options.Padding.Bottom - y) / rowHeight));
        for (var rowIndex = 0; rowIndex < table.Rows.Count && rowIndex < maxRows; rowIndex++) {
            var row = table.Rows[rowIndex];
            var rowBackground = row.Background ?? (table.RowStriping && rowIndex % 2 == 1 ? theme.PlotBackground.WithAlpha(110) : ChartColor.Transparent);
            if (rowBackground.A > 0) writer.StartElement("rect").Attribute("data-cfx-role", "table-row").Attribute("x", content.X).Attribute("y", y).Attribute("width", content.Width).Attribute("height", rowHeight).Attribute("rx", 4).Attribute("fill", rowBackground.ToCss()).EndEmptyElement().Line();
            var x = content.X;
            for (var i = 0; i < row.Cells.Count; i++) {
                var cell = row.Cells[i];
                if (cell.Background.HasValue) writer.StartElement("rect").Attribute("data-cfx-role", "table-cell-background").Attribute("x", x + 2).Attribute("y", y + 2).Attribute("width", Math.Max(1, widths[i] - 4)).Attribute("height", Math.Max(1, rowHeight - 4)).Attribute("rx", 4).Attribute("fill", cell.Background.Value.ToCss()).EndEmptyElement().Line();
                var status = CellStatus(table, i, cell);
                var textX = x + 9;
                if (status != VisualStatus.None && table.StatusColumnIndex == i) {
                    var color = VisualBlockRendering.StatusColor(theme, status);
                    writer.StartElement("circle").Attribute("data-cfx-role", "table-status").Attribute("cx", x + 10).Attribute("cy", y + rowHeight / 2).Attribute("r", 4).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
                    textX += 12;
                }

                WriteText(writer, cell.Text, textX, y + rowHeight * 0.66, widths[i] - (textX - x) - 7, cell.Alignment ?? table.Columns[i].Alignment, cell.Foreground ?? row.Foreground ?? theme.Text, theme.FontFamily, table.Dense ? 10.5 : 11.5, "400");
                x += widths[i];
            }

            y += rowHeight;
        }
    }

    private static void RenderList(SvgMarkupWriter writer, ChartList list) {
        var options = list.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, list, ref y, content.X, content.Width);
        var rowHeight = list.Dense ? 25.0 : 33.0;
        var markerWidth = list.Marker == VisualListMarker.None ? 0 : 24;
        var maxRows = Math.Max(0, (int)Math.Floor((options.Size.Height - options.Padding.Bottom - y) / rowHeight));
        for (var i = 0; i < list.Items.Count && i < maxRows; i++) {
            var item = list.Items[i];
            var centerY = y + rowHeight / 2;
            WriteMarker(writer, list, item, i, content.X + 8, centerY);
            var valueWidth = string.IsNullOrEmpty(item.Value) ? 0 : Math.Min(content.Width * 0.36, VisualBlockRendering.EstimateTextWidth(item.Value!, 11.5) + 10);
            WriteText(writer, item.Text, content.X + markerWidth, y + rowHeight * 0.66, content.Width - markerWidth - valueWidth - 6, VisualTextAlignment.Left, theme.Text, theme.FontFamily, list.Dense ? 11 : 12.5, "500");
            if (!string.IsNullOrEmpty(item.Value)) WriteText(writer, item.Value!, content.X + content.Width - valueWidth, y + rowHeight * 0.66, valueWidth, VisualTextAlignment.Right, theme.MutedText, theme.FontFamily, list.Dense ? 10.5 : 11.5, "600");
            y += rowHeight;
        }
    }

    private static void RenderMetric(SvgMarkupWriter writer, MetricCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var statusColor = VisualBlockRendering.StatusColor(theme, card.Status);
        if (card.Status != VisualStatus.None) writer.StartElement("rect").Attribute("data-cfx-role", "metric-status-bar").Attribute("x", 0).Attribute("y", 0).Attribute("width", 7).Attribute("height", options.Size.Height).Attribute("fill", statusColor.ToCss()).EndEmptyElement().Line();
        var labelSize = Math.Max(11, theme.SubtitleFontSize);
        var valueSize = Math.Min(54, Math.Max(26, options.Size.Height * 0.22));
        writer.StartElement("text").Attribute("data-cfx-role", "metric-label").Attribute("x", content.X).Attribute("y", content.Y + labelSize).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", labelSize).Attribute("font-weight", "700").Text(VisualBlockRendering.FitText(card.Label, labelSize, content.Width)).EndElement().Line();
        writer.StartElement("text").Attribute("data-cfx-role", "metric-value").Attribute("x", content.X).Attribute("y", content.Y + labelSize + valueSize + 14).Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", valueSize).Attribute("font-weight", "850").Text(VisualBlockRendering.FitText(card.Value, valueSize, content.Width)).EndElement().Line();
        if (card.Trend.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "metric-trend").Attribute("x", content.X).Attribute("y", options.Size.Height - options.Padding.Bottom - 18).Attribute("fill", statusColor.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", theme.SubtitleFontSize).Attribute("font-weight", "700").Text(VisualBlockRendering.FitText(card.Trend, theme.SubtitleFontSize, content.Width)).EndElement().Line();
        if (card.Caption.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "metric-caption").Attribute("x", content.X).Attribute("y", options.Size.Height - options.Padding.Bottom).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", Math.Max(10, theme.SubtitleFontSize - 1)).Text(VisualBlockRendering.FitText(card.Caption, Math.Max(10, theme.SubtitleFontSize - 1), content.Width)).EndElement().Line();
    }

    private static void WriteMarker(SvgMarkupWriter writer, ChartList list, ChartListItem item, int index, double x, double y) {
        var theme = list.Options.Theme;
        var color = item.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, index) : VisualBlockRendering.StatusColor(theme, item.Status);
        if (list.Marker == VisualListMarker.None) return;
        if (list.Marker == VisualListMarker.Number) {
            writer.StartElement("text").Attribute("data-cfx-role", "list-marker").Attribute("x", x - 5).Attribute("y", y + 4).Attribute("fill", color.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", 11).Attribute("font-weight", "800").Text((index + 1).ToString(CultureInfo.InvariantCulture)).EndElement().Line();
            return;
        }

        if (list.Marker == VisualListMarker.Check) {
            writer.StartElement("circle").Attribute("data-cfx-role", "list-marker").Attribute("cx", x).Attribute("cy", y).Attribute("r", 7).Attribute("fill", color.WithAlpha(55).ToCss()).Attribute("stroke", color.ToCss()).EndEmptyElement().Line();
            if (item.IsChecked != false) writer.StartElement("polyline").Attribute("data-cfx-role", "list-check").Attribute("points", (x - 4).ToString(CultureInfo.InvariantCulture) + "," + y.ToString(CultureInfo.InvariantCulture) + " " + (x - 1).ToString(CultureInfo.InvariantCulture) + "," + (y + 4).ToString(CultureInfo.InvariantCulture) + " " + (x + 5).ToString(CultureInfo.InvariantCulture) + "," + (y - 4).ToString(CultureInfo.InvariantCulture)).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", 1.8).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
            return;
        }

        writer.StartElement("circle").Attribute("data-cfx-role", "list-marker").Attribute("cx", x).Attribute("cy", y).Attribute("r", list.Marker == VisualListMarker.Status ? 6 : 4).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
    }

    private static void WriteText(SvgMarkupWriter writer, string text, double x, double y, double width, VisualTextAlignment alignment, ChartColor color, string fontFamily, double fontSize, string weight) {
        var fitted = VisualBlockRendering.FitText(text, fontSize, Math.Max(1, width));
        var anchor = "start";
        var textX = x;
        if (alignment == VisualTextAlignment.Center) { anchor = "middle"; textX = x + width / 2; }
        else if (alignment == VisualTextAlignment.Right) { anchor = "end"; textX = x + width; }
        writer.StartElement("text").Attribute("data-cfx-role", "visual-text").Attribute("x", textX).Attribute("y", y).Attribute("text-anchor", anchor).Attribute("fill", color.ToCss()).Attribute("font-family", fontFamily).Attribute("font-size", fontSize).Attribute("font-weight", weight).Text(fitted).EndElement().Line();
    }

    private static double[] ColumnWidths(ChartTable table, double totalWidth) {
        var widths = new double[table.Columns.Count];
        var fixedWidth = 0.0;
        var flexible = 0;
        for (var i = 0; i < table.Columns.Count; i++) {
            if (table.Columns[i].Width.HasValue) { widths[i] = table.Columns[i].Width!.Value; fixedWidth += widths[i]; }
            else flexible++;
        }

        var flexWidth = flexible == 0 ? 0 : Math.Max(24, (totalWidth - fixedWidth) / flexible);
        for (var i = 0; i < widths.Length; i++) if (widths[i] <= 0) widths[i] = flexWidth;
        return widths;
    }

    private static VisualStatus CellStatus(ChartTable table, int columnIndex, ChartTableCell cell) {
        if (cell.Status != VisualStatus.None) return cell.Status;
        return table.StatusColumnIndex == columnIndex ? VisualBlockRendering.ParseStatus(cell.Text) : VisualStatus.None;
    }

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "visual-block-" + value.ToString(CultureInfo.InvariantCulture);
    }
}
