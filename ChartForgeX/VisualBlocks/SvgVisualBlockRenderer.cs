using System;
using System.Globalization;
using System.Threading;
using ChartForgeX.Core;
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
        else if (block is RadialMetricCard radialCard) RenderRadialMetric(writer, radialCard);

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
        if (card.Icon != VisualIcon.None || card.Symbol.Length > 0) {
            var badgeColor = card.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, 0) : statusColor;
            var badgeRadius = Math.Min(24, Math.Max(15, options.Size.Height * 0.11));
            var cx = options.Size.Width - options.Padding.Right - badgeRadius;
            var cy = options.Padding.Top + badgeRadius;
            writer.StartElement("circle").Attribute("data-cfx-role", "metric-symbol-badge").Attribute("cx", cx).Attribute("cy", cy).Attribute("r", badgeRadius).Attribute("fill", badgeColor.WithAlpha(48).ToCss()).Attribute("stroke", badgeColor.ToCss()).EndEmptyElement().Line();
            if (card.Icon != VisualIcon.None) WriteIcon(writer, card.Icon, cx, cy, badgeRadius * 0.62, badgeColor);
            else writer.StartElement("text").Attribute("data-cfx-role", "metric-symbol").Attribute("x", cx).Attribute("y", cy + Math.Max(10, badgeRadius * 0.46) / 3.0).Attribute("text-anchor", "middle").Attribute("fill", badgeColor.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", Math.Max(10, badgeRadius * 0.46)).Attribute("font-weight", "850").Text(VisualBlockRendering.FitText(card.Symbol, Math.Max(10, badgeRadius * 0.46), badgeRadius * 1.45)).EndElement().Line();
        }

        var labelSize = Math.Max(11, theme.SubtitleFontSize);
        var valueSize = Math.Min(54, Math.Max(26, options.Size.Height * 0.22));
        writer.StartElement("text").Attribute("data-cfx-role", "metric-label").Attribute("x", content.X).Attribute("y", content.Y + labelSize).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", labelSize).Attribute("font-weight", "700").Text(VisualBlockRendering.FitText(card.Label, labelSize, content.Width)).EndElement().Line();
        writer.StartElement("text").Attribute("data-cfx-role", "metric-value").Attribute("x", content.X).Attribute("y", content.Y + labelSize + valueSize + 14).Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", valueSize).Attribute("font-weight", "850").Text(VisualBlockRendering.FitText(card.Value, valueSize, content.Width)).EndElement().Line();
        if (card.Trend.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "metric-trend").Attribute("x", content.X).Attribute("y", options.Size.Height - options.Padding.Bottom - 18).Attribute("fill", statusColor.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", theme.SubtitleFontSize).Attribute("font-weight", "700").Text(VisualBlockRendering.FitText(card.Trend, theme.SubtitleFontSize, content.Width)).EndElement().Line();
        if (card.Caption.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "metric-caption").Attribute("x", content.X).Attribute("y", options.Size.Height - options.Padding.Bottom).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", Math.Max(10, theme.SubtitleFontSize - 1)).Text(VisualBlockRendering.FitText(card.Caption, Math.Max(10, theme.SubtitleFontSize - 1), content.Width)).EndElement().Line();
    }

    private static void RenderRadialMetric(SvgMarkupWriter writer, RadialMetricCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, card, ref y, content.X, content.Width);
        var availableHeight = Math.Max(1, options.Size.Height - options.Padding.Bottom - y);
        var cx = content.X + content.Width / 2;
        var cy = y + availableHeight * 0.48;
        var outerRadius = Math.Max(24, Math.Min(content.Width, availableHeight) * 0.42);
        writer.StartElement("g").Attribute("data-cfx-role", "radial-metric-card").Attribute("data-cfx-label", card.Label).EndStartElement().Line();
        for (var i = 0; i < card.Layers.Count; i++) {
            var layer = card.Layers[i];
            var ratio = RadialLayerRatio(layer);
            if (ratio <= 0) continue;
            var radius = Math.Max(1, outerRadius * layer.RadiusRatio);
            var stroke = Math.Max(1, outerRadius * layer.StrokeRatio);
            var start = DegreesToRadians(layer.StartAngleDegrees);
            var end = start + DegreesToRadians(layer.SweepAngleDegrees) * ratio;
            var color = layer.Color ?? VisualBlockRendering.PaletteAt(theme, i);
            writer.StartElement("path")
                .Attribute("data-cfx-role", "radial-metric-layer")
                .Attribute("data-cfx-layer", i)
                .Attribute("data-cfx-label", layer.Name)
                .Attribute("data-cfx-value", layer.Value)
                .Attribute("data-cfx-min", layer.Minimum)
                .Attribute("data-cfx-max", layer.Maximum)
                .Attribute("data-cfx-percent", ratio)
                .Attribute("d", ArcPath(cx, cy, radius, start, end))
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", stroke)
                .Attribute("stroke-linecap", layer.LineCap == ChartRadialLayerCap.Butt ? "butt" : "round");
            if (layer.Opacity < 1) writer.Attribute("opacity", layer.Opacity);
            writer.EndEmptyElement().Line();
            WriteRadialSeparators(writer, layer, theme, cx, cy, radius, stroke, start, end);
        }

        var valueSize = Math.Min(48, Math.Max(23, outerRadius * 0.28));
        var labelSize = Math.Min(18, Math.Max(10, outerRadius * 0.115));
        if (card.Icon != VisualIcon.None) WriteIcon(writer, card.Icon, cx, cy - valueSize * 1.02, Math.Max(12, outerRadius * 0.10), theme.MutedText);
        WriteText(writer, card.Value, cx - outerRadius * 0.72, cy - valueSize * 0.12, outerRadius * 1.44, VisualTextAlignment.Center, theme.Text, theme.FontFamily, valueSize, "850");
        WriteText(writer, card.Label, cx - outerRadius * 0.72, cy + valueSize * 0.72, outerRadius * 1.44, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, labelSize, "700");
        writer.EndElement().Line();
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

    private static void WriteRadialSeparators(SvgMarkupWriter writer, ChartRadialLayer layer, ChartForgeX.Themes.ChartTheme theme, double cx, double cy, double radius, double stroke, double start, double end) {
        if (layer.SeparatorCount <= 0) return;
        var separator = layer.SeparatorColor ?? theme.CardBackground;
        var inset = Math.Min(stroke / 2 - 0.5, Math.Max(0, stroke * layer.SeparatorInsetRatio));
        var inner = Math.Max(0, radius - stroke / 2 + inset);
        var outer = radius + stroke / 2 - inset;
        for (var i = 1; i <= layer.SeparatorCount; i++) {
            var angle = start + (end - start) * i / (layer.SeparatorCount + 1);
            writer.StartElement("line")
                .Attribute("data-cfx-role", "radial-metric-separator")
                .Attribute("x1", cx + Math.Cos(angle) * inner)
                .Attribute("y1", cy + Math.Sin(angle) * inner)
                .Attribute("x2", cx + Math.Cos(angle) * outer)
                .Attribute("y2", cy + Math.Sin(angle) * outer)
                .Attribute("stroke", separator.ToCss())
                .Attribute("stroke-width", layer.SeparatorStrokeWidth)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement().Line();
        }
    }

    private static void WriteIcon(SvgMarkupWriter writer, VisualIcon icon, double x, double y, double size, ChartColor color) {
        var stroke = Math.Max(1.6, size * 0.16);
        if (icon == VisualIcon.ForkKnife) {
            writer.StartElement("path")
                .Attribute("data-cfx-role", "visual-icon")
                .Attribute("data-cfx-icon", "fork-knife")
                .Attribute("d", "M " + F(x - size * 0.42) + " " + F(y - size * 0.54) + " V " + F(y + size * 0.48) + " M " + F(x - size * 0.66) + " " + F(y - size * 0.56) + " V " + F(y - size * 0.12) + " M " + F(x - size * 0.42) + " " + F(y - size * 0.56) + " V " + F(y - size * 0.12) + " M " + F(x - size * 0.18) + " " + F(y - size * 0.56) + " V " + F(y - size * 0.12) + " M " + F(x - size * 0.66) + " " + F(y - size * 0.12) + " Q " + F(x - size * 0.42) + " " + F(y + size * 0.18) + " " + F(x - size * 0.18) + " " + F(y - size * 0.12) + " M " + F(x + size * 0.34) + " " + F(y + size * 0.48) + " V " + F(y - size * 0.52) + " Q " + F(x + size * 0.70) + " " + F(y - size * 0.24) + " " + F(x + size * 0.40) + " " + F(y + size * 0.04))
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", stroke)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement().Line();
            return;
        }

        if (icon == VisualIcon.Flame) {
            writer.StartElement("path")
                .Attribute("data-cfx-role", "visual-icon")
                .Attribute("data-cfx-icon", "flame")
                .Attribute("d", "M " + F(x) + " " + F(y + size * 0.62) + " C " + F(x - size * 0.70) + " " + F(y + size * 0.24) + " " + F(x - size * 0.38) + " " + F(y - size * 0.46) + " " + F(x - size * 0.08) + " " + F(y - size * 0.82) + " C " + F(x + size * 0.04) + " " + F(y - size * 0.30) + " " + F(x + size * 0.52) + " " + F(y - size * 0.24) + " " + F(x + size * 0.38) + " " + F(y - size * 0.88) + " C " + F(x + size * 0.98) + " " + F(y - size * 0.30) + " " + F(x + size * 0.82) + " " + F(y + size * 0.48) + " " + F(x) + " " + F(y + size * 0.62) + " Z")
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", stroke)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement().Line();
            return;
        }

        writer.StartElement("path")
            .Attribute("data-cfx-role", "visual-icon")
            .Attribute("data-cfx-icon", "lightning")
            .Attribute("d", "M " + F(x - size * 0.52) + " " + F(y - size * 0.32) + " L " + F(x + size * 0.10) + " " + F(y - size * 0.92) + " L " + F(x) + " " + F(y - size * 0.26) + " L " + F(x + size * 0.58) + " " + F(y - size * 0.08) + " L " + F(x - size * 0.20) + " " + F(y + size * 0.82) + " L " + F(x - size * 0.04) + " " + F(y + size * 0.08) + " Z")
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", stroke)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .EndEmptyElement().Line();
    }

    private static string ArcPath(double cx, double cy, double radius, double start, double end) {
        if (Math.Abs(end - start) >= Math.PI * 2 - 0.000001) {
            var mid = start + Math.PI;
            return new SvgPathDataBuilder()
                .MoveTo(cx + Math.Cos(start) * radius, cy + Math.Sin(start) * radius)
                .ArcTo(radius, radius, 0, false, true, cx + Math.Cos(mid) * radius, cy + Math.Sin(mid) * radius)
                .ArcTo(radius, radius, 0, false, true, cx + Math.Cos(start + Math.PI * 2) * radius, cy + Math.Sin(start + Math.PI * 2) * radius)
                .Build();
        }

        return new SvgPathDataBuilder()
            .MoveTo(cx + Math.Cos(start) * radius, cy + Math.Sin(start) * radius)
            .ArcTo(radius, radius, 0, end - start > Math.PI, true, cx + Math.Cos(end) * radius, cy + Math.Sin(end) * radius)
            .Build();
    }

    private static double RadialLayerRatio(ChartRadialLayer layer) => Math.Max(0, Math.Min(1, (layer.Value - layer.Minimum) / (layer.Maximum - layer.Minimum)));

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "visual-block-" + value.ToString(CultureInfo.InvariantCulture);
    }
}
