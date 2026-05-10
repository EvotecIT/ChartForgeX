using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderHeatmapInsightCard(SvgMarkupWriter writer, HeatmapInsightCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, card, ref y, content.X, content.Width);
        RenderHeatmapControls(writer, card, content.X, y, content.Width);
        y += card.LeftControl.Length > 0 || card.SelectedControl.Length > 0 || card.PeriodLabel.Length > 0 ? 34 : 2;
        var railGap = card.Insights.Count == 0 ? 0 : Math.Min(24, Math.Max(8, content.Width * 0.06));
        var minimumMatrixWidth = Math.Min(content.Width, 92 + card.Columns.Count * 12);
        var desiredRailWidth = card.Insights.Count == 0 ? 0 : Math.Min(190, Math.Max(90, content.Width * 0.28));
        var railWidth = card.Insights.Count == 0 ? 0 : Math.Min(desiredRailWidth, Math.Max(0, content.Width - railGap - minimumMatrixWidth));
        if (railWidth < 72) { railWidth = 0; railGap = 0; }
        var matrixWidth = Math.Max(1, content.Width - railWidth - railGap);
        var labelWidth = Math.Min(54, Math.Max(44, matrixWidth * 0.13));
        var cellGap = 6.0;
        var cellWidth = Math.Max(10, (matrixWidth - labelWidth - cellGap * Math.Max(0, card.Columns.Count - 1)) / card.Columns.Count);
        var cellHeight = Math.Max(16, Math.Min(24, (options.Size.Height - options.Padding.Bottom - y - 22) / Math.Max(1, card.Rows.Count + 1)));
        var matrixX = content.X;
        var headerY = y;
        var gridY = headerY + cellHeight + 8;
        writer.StartElement("g").Attribute("data-cfx-role", "heatmap-insight-card").EndStartElement().Line();
        for (var column = 0; column < card.Columns.Count; column++) {
            WriteText(writer, card.Columns[column], matrixX + labelWidth + column * (cellWidth + cellGap), headerY + cellHeight * 0.68, cellWidth, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 2), "750");
        }

        for (var row = 0; row < card.Rows.Count; row++) {
            var item = card.Rows[row];
            var rowY = gridY + row * (cellHeight + 5);
            WriteText(writer, item.Label, matrixX, rowY + cellHeight * 0.68, labelWidth - 6, VisualTextAlignment.Right, theme.MutedText, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 2), "650");
            for (var column = 0; column < card.Columns.Count; column++) {
                var value = item.Values[column];
                var color = HeatmapInsightColor(card, value);
                writer.StartElement("rect")
                    .Attribute("data-cfx-role", "heatmap-insight-cell")
                    .Attribute("data-cfx-row", item.Label)
                    .Attribute("data-cfx-column", card.Columns[column])
                    .Attribute("data-cfx-value", value)
                    .Attribute("x", matrixX + labelWidth + column * (cellWidth + cellGap))
                    .Attribute("y", rowY)
                    .Attribute("width", cellWidth)
                    .Attribute("height", cellHeight)
                    .Attribute("rx", Math.Min(5, cellHeight * 0.32))
                    .Attribute("fill", color.ToCss())
                    .EndEmptyElement().Line();
                WriteText(writer, value.ToString("0", CultureInfo.InvariantCulture), matrixX + labelWidth + column * (cellWidth + cellGap), rowY + cellHeight * 0.68, cellWidth, VisualTextAlignment.Center, ChartColorMath.TextOnBackground(color), theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 2), "800");
            }
        }

        if (railWidth > 0) RenderHeatmapInsightRail(writer, card, content.X + matrixWidth + railGap, gridY, railWidth, Math.Max(1, options.Size.Height - options.Padding.Bottom - gridY));
        writer.EndElement().Line();
    }

    private static void RenderHeatmapControls(SvgMarkupWriter writer, HeatmapInsightCard card, double x, double y, double width) {
        var theme = card.Options.Theme;
        var buttonHeight = 24.0;
        var cursor = x;
        if (card.LeftControl.Length > 0) {
            var w = Math.Min(70, VisualBlockRendering.EstimateTextWidth(card.LeftControl, theme.SubtitleFontSize) + 22);
            writer.StartElement("rect").Attribute("data-cfx-role", "heatmap-control").Attribute("x", cursor).Attribute("y", y).Attribute("width", w).Attribute("height", buttonHeight).Attribute("rx", 8).Attribute("fill", theme.PlotBackground.ToCss()).EndEmptyElement().Line();
            WriteText(writer, card.LeftControl, cursor, y + 16, w, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "650");
            cursor += w - 2;
        }

        if (card.SelectedControl.Length > 0) {
            var w = Math.Min(80, VisualBlockRendering.EstimateTextWidth(card.SelectedControl, theme.SubtitleFontSize) + 24);
            writer.StartElement("rect").Attribute("data-cfx-role", "heatmap-control-selected").Attribute("x", cursor).Attribute("y", y).Attribute("width", w).Attribute("height", buttonHeight).Attribute("rx", 8).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            WriteText(writer, card.SelectedControl, cursor, y + 16, w, VisualTextAlignment.Center, VisualBlockRendering.PaletteAt(theme, 0), theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "850");
            cursor += w + 14;
        }

        if (card.PeriodLabel.Length > 0) {
            var remaining = Math.Max(0, width - (cursor - x));
            if (remaining < 24) return;
            var w = Math.Min(remaining, VisualBlockRendering.EstimateTextWidth(card.PeriodLabel, theme.SubtitleFontSize) + 34);
            writer.StartElement("rect").Attribute("data-cfx-role", "heatmap-period-control").Attribute("x", cursor).Attribute("y", y).Attribute("width", w).Attribute("height", buttonHeight).Attribute("rx", 8).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            WriteText(writer, card.PeriodLabel, cursor + 12, y + 16, w - 24, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "650");
        }
    }

    private static void RenderHeatmapInsightRail(SvgMarkupWriter writer, HeatmapInsightCard card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        writer.StartElement("g").Attribute("data-cfx-role", "heatmap-insight-rail").EndStartElement().Line();
        WriteText(writer, card.InsightTitle, x, y + 14, width, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "800");
        var itemY = y + 42;
        foreach (var item in card.Insights) {
            WriteText(writer, item.Label, x, itemY, width, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "800");
            WriteText(writer, item.Detail, x, itemY + 18, width, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 2), "500");
            itemY += 48;
        }

        var keyY = Math.Min(y + height - 46, Math.Max(itemY + 8, y + 150));
        WriteText(writer, card.ColorKeyLabel, x, keyY, width, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(11, theme.SubtitleFontSize), "800");
        keyY += 24;
        WriteText(writer, card.Minimum.ToString("0", CultureInfo.InvariantCulture), x, keyY + 6, 18, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 2), "600");
        var keyX = x + 22;
        var keyWidth = Math.Max(30, width - 48);
        var steps = 16;
        var stepWidth = keyWidth / steps;
        writer.StartElement("g").Attribute("data-cfx-role", "heatmap-color-key").EndStartElement().Line();
        for (var step = 0; step < steps; step++) {
            var value = card.Minimum + (card.Maximum - card.Minimum) * (step + 0.5) / steps;
            writer.StartElement("rect").Attribute("x", keyX + step * stepWidth).Attribute("y", keyY).Attribute("width", stepWidth + 0.6).Attribute("height", 7).Attribute("rx", step == 0 || step == steps - 1 ? 4 : 0).Attribute("fill", HeatmapInsightColor(card, value).ToCss()).EndEmptyElement().Line();
        }

        writer.EndElement().Line();
        WriteText(writer, card.Maximum.ToString("0", CultureInfo.InvariantCulture), x + width - 20, keyY + 6, 20, VisualTextAlignment.Right, theme.MutedText, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 2), "600");
        writer.EndElement().Line();
    }

    private static ChartColor HeatmapInsightColor(HeatmapInsightCard card, double value) {
        var ratio = Math.Max(0, Math.Min(1, (value - card.Minimum) / (card.Maximum - card.Minimum)));
        return ChartColor.FromRgba((byte)Math.Round(card.LowColor.R + (card.HighColor.R - card.LowColor.R) * ratio), (byte)Math.Round(card.LowColor.G + (card.HighColor.G - card.LowColor.G) * ratio), (byte)Math.Round(card.LowColor.B + (card.HighColor.B - card.LowColor.B) * ratio), 255);
    }
}
