using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual blocks to dependency-free PNG images.
/// </summary>
public sealed class PngVisualBlockRenderer {
    /// <summary>Renders a visual block to PNG bytes.</summary>
    public byte[] Render(IVisualBlock block) {
        var canvas = RenderCanvas(block);
        return PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels());
    }

    internal RgbaCanvas RenderCanvas(IVisualBlock block) {
        VisualBlockRendering.Validate(block);
        var options = block.Options;
        var theme = options.Theme;
        var canvas = new RgbaCanvas(options.Size.Width, options.Size.Height, 2, null, options.PngOutputScale);
        canvas.Clear(VisualBlockRendering.SurfaceBackground(options));
        if (options.ShowCard && theme.UseCard) {
            canvas.FillRoundedRect(0, 0, options.Size.Width, options.Size.Height, theme.CornerRadius, theme.CardBackground);
            canvas.StrokeRoundedRect(0.5, 0.5, Math.Max(1, options.Size.Width - 1), Math.Max(1, options.Size.Height - 1), theme.CornerRadius, theme.CardBorder, 1);
            canvas.StrokeRoundedRect(1.5, 1.5, Math.Max(1, options.Size.Width - 3), Math.Max(1, options.Size.Height - 3), Math.Max(0, theme.CornerRadius - 1.5), ChartColor.FromRgba(255, 255, 255, 92), 1);
        }

        if (block is ChartTable table) DrawTable(canvas, table);
        else if (block is ChartList list) DrawList(canvas, list);
        else if (block is MetricCard card) DrawMetric(canvas, card);
        else if (block is RadialMetricCard radialCard) DrawRadialMetric(canvas, radialCard);
        return canvas;
    }

    private static void DrawHeading(RgbaCanvas canvas, IVisualBlock block, ref double y, double x, double width) {
        var theme = block.Options.Theme;
        if (block.Title.Length > 0) {
            canvas.DrawTextEmphasized(x, y, FitText(block.Title, theme.TitleFontSize, width), theme.Text, theme.TitleFontSize);
            y += theme.TitleFontSize + 8;
        }

        if (block.Subtitle.Length > 0) {
            canvas.DrawText(x, y, FitText(block.Subtitle, theme.SubtitleFontSize, width), theme.MutedText, theme.SubtitleFontSize);
            y += theme.SubtitleFontSize + 13;
        } else if (block.Title.Length > 0) {
            y += 8;
        }
    }

    private static void DrawTable(RgbaCanvas canvas, ChartTable table) {
        var options = table.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, table, ref y, content.X, content.Width);
        var headerHeight = table.Dense ? 26.0 : 32.0;
        var rowHeight = table.Dense ? 24.0 : 31.0;
        var widths = ColumnWidths(table, content.Width);
        if (table.ShowHeader) {
            canvas.FillRoundedRect(content.X, y, content.Width, headerHeight, Math.Min(6, theme.PlotCornerRadius), theme.PlotBackground);
            canvas.StrokeRoundedRect(content.X, y, content.Width, headerHeight, Math.Min(6, theme.PlotCornerRadius), theme.PlotBorder, 1);
            var x = content.X;
            for (var i = 0; i < table.Columns.Count; i++) {
                DrawAlignedText(canvas, table.Columns[i].Header, x + 9, y + 8, widths[i] - 18, table.Columns[i].Alignment, theme.Text, theme.SubtitleFontSize, true);
                x += widths[i];
            }

            y += headerHeight + 4;
        }

        var maxRows = Math.Max(0, (int)Math.Floor((options.Size.Height - options.Padding.Bottom - y) / rowHeight));
        for (var rowIndex = 0; rowIndex < table.Rows.Count && rowIndex < maxRows; rowIndex++) {
            var row = table.Rows[rowIndex];
            var rowBackground = row.Background ?? (table.RowStriping && rowIndex % 2 == 1 ? theme.PlotBackground.WithAlpha(110) : ChartColor.Transparent);
            if (rowBackground.A > 0) canvas.FillRoundedRect(content.X, y, content.Width, rowHeight, 4, rowBackground);
            var x = content.X;
            for (var i = 0; i < row.Cells.Count; i++) {
                var cell = row.Cells[i];
                if (cell.Background.HasValue) canvas.FillRoundedRect(x + 2, y + 2, Math.Max(1, widths[i] - 4), Math.Max(1, rowHeight - 4), 4, cell.Background.Value);
                var textX = x + 9;
                var status = CellStatus(table, i, cell);
                if (status != VisualStatus.None && table.StatusColumnIndex == i) {
                    canvas.DrawCircle(x + 10, y + rowHeight / 2, 4, VisualBlockRendering.StatusColor(theme, status));
                    textX += 12;
                }

                DrawAlignedText(canvas, cell.Text, textX, y + 8, widths[i] - (textX - x) - 7, cell.Alignment ?? table.Columns[i].Alignment, cell.Foreground ?? row.Foreground ?? theme.Text, table.Dense ? 10.5 : 11.5, false);
                x += widths[i];
            }

            y += rowHeight;
        }
    }

    private static void DrawList(RgbaCanvas canvas, ChartList list) {
        var options = list.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, list, ref y, content.X, content.Width);
        var rowHeight = list.Dense ? 25.0 : 33.0;
        var markerWidth = list.Marker == VisualListMarker.None ? 0 : 24;
        var maxRows = Math.Max(0, (int)Math.Floor((options.Size.Height - options.Padding.Bottom - y) / rowHeight));
        for (var i = 0; i < list.Items.Count && i < maxRows; i++) {
            var item = list.Items[i];
            DrawMarker(canvas, list, item, i, content.X + 8, y + rowHeight / 2);
            var valueWidth = string.IsNullOrEmpty(item.Value) ? 0 : Math.Min(content.Width * 0.36, RgbaCanvas.MeasureTextWidth(item.Value!, 11.5, null) + 10);
            DrawAlignedText(canvas, item.Text, content.X + markerWidth, y + 8, content.Width - markerWidth - valueWidth - 6, VisualTextAlignment.Left, theme.Text, list.Dense ? 11 : 12.5, true);
            if (!string.IsNullOrEmpty(item.Value)) DrawAlignedText(canvas, item.Value!, content.X + content.Width - valueWidth, y + 8, valueWidth, VisualTextAlignment.Right, theme.MutedText, list.Dense ? 10.5 : 11.5, true);
            y += rowHeight;
        }
    }

    private static void DrawMetric(RgbaCanvas canvas, MetricCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var statusColor = VisualBlockRendering.StatusColor(theme, card.Status);
        var hasAction = card.ActionLabel.Length > 0;
        var footerHeight = hasAction ? Math.Min(46, Math.Max(36, options.Size.Height * 0.24)) : 0;
        var footerY = options.Size.Height - footerHeight;
        var detailBottom = hasAction ? footerY - 12 : options.Size.Height - options.Padding.Bottom;
        var labelX = content.X;
        var labelWidth = content.Width;
        var valueYOffset = 0.0;
        if (card.Status != VisualStatus.None) canvas.FillRect(0, 0, 7, options.Size.Height, statusColor);
        if (card.Icon != VisualIcon.None || card.Symbol.Length > 0) {
            var badgeColor = card.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, 0) : statusColor;
            var badgeRadius = Math.Min(24, Math.Max(15, options.Size.Height * 0.11));
            var leftBadge = card.BadgePlacement == MetricCardBadgePlacement.TopLeft;
            var cx = leftBadge ? content.X + badgeRadius : options.Size.Width - options.Padding.Right - badgeRadius;
            var cy = options.Padding.Top + badgeRadius;
            if (leftBadge) {
                labelX = cx + badgeRadius + 12;
                labelWidth = Math.Max(1, content.X + content.Width - labelX);
                valueYOffset = Math.Max(8, badgeRadius * 0.55);
            }

            canvas.DrawCircle(cx, cy, badgeRadius, badgeColor.WithAlpha(48));
            canvas.DrawCircleOutline(cx, cy, badgeRadius, badgeColor, 1);
            if (card.Icon != VisualIcon.None) DrawIcon(canvas, card.Icon, cx, cy, badgeRadius * 0.62, badgeColor);
            else DrawCenteredText(canvas, card.Symbol, cx, cy, Math.Max(10, badgeRadius * 0.46), badgeColor, true);
        }

        var labelSize = Math.Max(11, theme.SubtitleFontSize);
        var baseValueSize = Math.Min(54, Math.Max(26, options.Size.Height * 0.22));
        var hasMicroVisual = card.MiniBars.Count > 0 || card.MiniSparkline.Count > 0;
        var microWidth = hasMicroVisual ? Math.Min(112, Math.Max(58, content.Width * 0.32)) : 0;
        var microHeight = Math.Min(56, Math.Max(34, options.Size.Height * 0.24));
        var valueWidth = hasMicroVisual ? Math.Max(1, content.Width - microWidth - 18) : content.Width;
        var valueSize = MetricValueFontSize(card.Value, baseValueSize, valueWidth);
        canvas.DrawTextEmphasized(labelX, content.Y, FitText(card.Label, labelSize, labelWidth), theme.MutedText, labelSize);
        canvas.DrawTextEmphasized(content.X, content.Y + labelSize + 18 + valueYOffset, FitText(card.Value, valueSize, valueWidth), theme.Text, valueSize);
        if (card.MiniSparkline.Count > 0) DrawMetricMiniSparkline(canvas, card, content.X + content.Width - microWidth, content.Y + labelSize + Math.Max(20, valueSize * 0.52) + valueYOffset, microWidth, microHeight);
        else if (card.MiniBars.Count > 0) DrawMetricMiniBars(canvas, card, content.X + content.Width - microWidth, content.Y + labelSize + Math.Max(20, valueSize * 0.52) + valueYOffset, microWidth, microHeight);
        var detailsTop = content.Y + labelSize + valueSize + 24 + valueYOffset;
        DrawMetricDetails(canvas, card, content, detailsTop, detailBottom);
        DrawMetricDetail(canvas, card, detailBottom, content.X, content.Width);
        if (hasAction) DrawMetricAction(canvas, card, footerY, footerHeight, content.X, content.Width);
    }

    private static void DrawMetricDetail(RgbaCanvas canvas, MetricCard card, double bottom, double x, double width) {
        var theme = card.Options.Theme;
        var statusColor = VisualBlockRendering.StatusColor(theme, card.Status);
        var trendSize = Math.Max(10, theme.SubtitleFontSize);
        var captionSize = Math.Max(10, theme.SubtitleFontSize - 1);
        var y = bottom - (card.Trend.Length > 0 && card.Caption.Length > 0 ? trendSize + 1 : trendSize);
        if (card.Trend.Length > 0) {
            var trendWidth = Math.Min(width * 0.42, RgbaCanvas.MeasureTextEmphasizedWidth(card.Trend, trendSize, null) + 10);
            canvas.DrawTextEmphasized(x, y, FitText(card.Trend, trendSize, trendWidth), statusColor, trendSize);
            if (card.Caption.Length > 0) canvas.DrawText(x + trendWidth + 5, y + 1, FitText(card.Caption, captionSize, Math.Max(1, width - trendWidth - 5)), theme.MutedText, captionSize);
        } else if (card.Caption.Length > 0) {
            canvas.DrawText(x, y, FitText(card.Caption, captionSize, width), theme.MutedText, captionSize);
        }
    }

    private static void DrawMetricAction(RgbaCanvas canvas, MetricCard card, double footerY, double footerHeight, double x, double width) {
        var theme = card.Options.Theme;
        canvas.DrawLine(0, footerY, card.Options.Size.Width, footerY, theme.PlotBorder, 1);
        var fontSize = Math.Max(10, theme.SubtitleFontSize);
        var symbolWidth = Math.Min(24, RgbaCanvas.MeasureTextEmphasizedWidth(card.ActionSymbol, fontSize, null) + 6);
        var y = footerY + (footerHeight - fontSize) * 0.52;
        canvas.DrawText(x, y, FitText(card.ActionLabel, fontSize, Math.Max(1, width - symbolWidth - 8)), theme.MutedText, fontSize);
        DrawAlignedText(canvas, card.ActionSymbol, x + width - symbolWidth, y, symbolWidth, VisualTextAlignment.Right, theme.Text, fontSize, true);
    }

    private static void DrawMetricMiniBars(RgbaCanvas canvas, MetricCard card, double x, double y, double width, double height) {
        foreach (var bar in VisualBlockRendering.CreateMiniBars(card, x, y, width, height)) {
            canvas.FillRoundedRect(bar.X, bar.Y, bar.Width, bar.Height, bar.Radius, bar.Color);
        }
    }

    private static double MetricValueFontSize(string value, double requestedSize, double maxWidth) {
        var measuredWidth = RgbaCanvas.MeasureTextEmphasizedWidth(value, requestedSize, null);
        if (measuredWidth <= maxWidth) return requestedSize;
        return Math.Max(22, Math.Floor(requestedSize * maxWidth / Math.Max(1, measuredWidth)));
    }

    private static void DrawMetricMiniSparkline(RgbaCanvas canvas, MetricCard card, double x, double y, double width, double height) {
        var sparkline = VisualBlockRendering.CreateMiniSparkline(card, x, y, width, height);
        canvas.FillPolygon(sparkline.Area, sparkline.FillColor);
        for (var i = 1; i < sparkline.Points.Length; i++) canvas.DrawLine(sparkline.Points[i - 1].X, sparkline.Points[i - 1].Y, sparkline.Points[i].X, sparkline.Points[i].Y, sparkline.LineColor, sparkline.StrokeWidth);
        canvas.DrawCircle(sparkline.Current.X, sparkline.Current.Y, sparkline.CurrentRadius, sparkline.LineColor);
    }

    private static void DrawMetricDetails(RgbaCanvas canvas, MetricCard card, ChartRect content, double top, double bottom) {
        if (card.Details.Count == 0 || bottom <= top + 18) return;
        var theme = card.Options.Theme;
        var count = Math.Min(card.Details.Count, 4);
        var columns = count <= 2 ? count : 2;
        var rows = (int)Math.Ceiling(count / (double)columns);
        var rowHeight = Math.Min(28, Math.Max(21, (bottom - top) / rows));
        var gap = 8.0;
        var cellWidth = (content.Width - gap * (columns - 1)) / columns;
        var labelSize = Math.Max(9, theme.SubtitleFontSize - 3);
        var valueSize = Math.Max(10, theme.SubtitleFontSize - 1);
        for (var i = 0; i < count; i++) {
            var detail = card.Details[i];
            var column = i % columns;
            var row = i / columns;
            var x = content.X + column * (cellWidth + gap);
            var y = top + row * rowHeight;
            var height = rowHeight - 4;
            var marker = VisualBlockRendering.StatusColor(theme, detail.Status);
            canvas.FillRoundedRect(x, y, cellWidth, height, Math.Min(8, height / 2), theme.PlotBackground.WithAlpha(150));
            canvas.StrokeRoundedRect(x, y, cellWidth, height, Math.Min(8, height / 2), theme.CardBorder.WithAlpha(120));
            canvas.DrawCircle(x + 10, y + rowHeight / 2 - 2, 3.2, marker);
            canvas.DrawTextEmphasized(x + 18, y + rowHeight / 2 - labelSize * 0.45, FitText(detail.Label, labelSize, cellWidth * 0.55), theme.MutedText, labelSize);
            DrawAlignedText(canvas, detail.Value, x + cellWidth * 0.58, y + rowHeight / 2 - valueSize * 0.35, cellWidth * 0.36, VisualTextAlignment.Right, theme.Text, valueSize, true);
        }
    }

    private static void DrawRadialMetric(RgbaCanvas canvas, RadialMetricCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, card, ref y, content.X, content.Width);
        var availableHeight = Math.Max(1, options.Size.Height - options.Padding.Bottom - y);
        var cx = content.X + content.Width / 2;
        var cy = y + availableHeight * 0.48;
        var outerRadius = Math.Max(24, Math.Min(content.Width, availableHeight) * 0.42);
        for (var i = 0; i < card.Layers.Count; i++) {
            var layer = card.Layers[i];
            var ratio = RadialLayerRatio(layer);
            if (ratio <= 0) continue;
            var radius = Math.Max(1, outerRadius * layer.RadiusRatio);
            var stroke = Math.Max(1, outerRadius * layer.StrokeRatio);
            var start = DegreesToRadians(layer.StartAngleDegrees);
            var end = start + DegreesToRadians(layer.SweepAngleDegrees) * ratio;
            var color = ApplyOpacity(layer.Color ?? VisualBlockRendering.PaletteAt(theme, i), layer.Opacity);
            if (layer.LineCap == ChartRadialLayerCap.Butt) canvas.FillRingSlice(cx, cy, radius + stroke / 2, Math.Max(0, radius - stroke / 2), start, end, color);
            else canvas.DrawArc(cx, cy, radius, start, end, color, stroke);
            DrawRadialSeparators(canvas, layer, theme, cx, cy, radius, stroke, start, end);
        }

        var valueSize = Math.Min(48, Math.Max(23, outerRadius * 0.28));
        var labelSize = Math.Min(18, Math.Max(10, outerRadius * 0.115));
        if (card.Icon != VisualIcon.None) DrawIcon(canvas, card.Icon, cx, cy - valueSize * 1.02, Math.Max(12, outerRadius * 0.10), theme.MutedText);
        DrawCenteredText(canvas, card.Value, cx, cy - valueSize * 0.68, valueSize, theme.Text, true);
        DrawCenteredText(canvas, card.Label, cx, cy + valueSize * 0.48, labelSize, theme.MutedText, true);
    }

    private static void DrawMarker(RgbaCanvas canvas, ChartList list, ChartListItem item, int index, double x, double y) {
        if (list.Marker == VisualListMarker.None) return;
        var theme = list.Options.Theme;
        var color = item.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, index) : VisualBlockRendering.StatusColor(theme, item.Status);
        if (list.Marker == VisualListMarker.Number) {
            canvas.DrawTextEmphasized(x - 5, y - 7, (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), color, 11);
            return;
        }

        if (list.Marker == VisualListMarker.Check) {
            canvas.DrawCircle(x, y, 7, color.WithAlpha(55));
            canvas.DrawCircleOutline(x, y, 7, color, 1);
            if (item.IsChecked != false) {
                canvas.DrawLine(x - 4, y, x - 1, y + 4, color, 1.6);
                canvas.DrawLine(x - 1, y + 4, x + 5, y - 4, color, 1.6);
            }

            return;
        }

        canvas.DrawCircle(x, y, list.Marker == VisualListMarker.Status ? 6 : 4, color);
    }

    private static void DrawAlignedText(RgbaCanvas canvas, string text, double x, double y, double width, VisualTextAlignment alignment, ChartColor color, double fontSize, bool emphasized) {
        var fitted = FitText(text, fontSize, Math.Max(1, width));
        var textWidth = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(fitted, fontSize, null) : RgbaCanvas.MeasureTextWidth(fitted, fontSize, null);
        var textX = alignment == VisualTextAlignment.Center ? x + (width - textWidth) / 2 : alignment == VisualTextAlignment.Right ? x + width - textWidth : x;
        if (emphasized) canvas.DrawTextEmphasized(textX, y, fitted, color, fontSize);
        else canvas.DrawText(textX, y, fitted, color, fontSize);
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

    private static void DrawRadialSeparators(RgbaCanvas canvas, ChartRadialLayer layer, ChartForgeX.Themes.ChartTheme theme, double cx, double cy, double radius, double stroke, double start, double end) {
        if (layer.SeparatorCount <= 0) return;
        var separator = layer.SeparatorColor ?? theme.CardBackground;
        var inset = Math.Min(stroke / 2 - 0.5, Math.Max(0, stroke * layer.SeparatorInsetRatio));
        var inner = Math.Max(0, radius - stroke / 2 + inset);
        var outer = radius + stroke / 2 - inset;
        for (var i = 1; i <= layer.SeparatorCount; i++) {
            var angle = start + (end - start) * i / (layer.SeparatorCount + 1);
            canvas.DrawLine(cx + Math.Cos(angle) * inner, cy + Math.Sin(angle) * inner, cx + Math.Cos(angle) * outer, cy + Math.Sin(angle) * outer, separator, layer.SeparatorStrokeWidth);
        }
    }

    private static void DrawIcon(RgbaCanvas canvas, VisualIcon icon, double x, double y, double size, ChartColor color) {
        var stroke = Math.Max(1.6, size * 0.16);
        if (icon == VisualIcon.ForkKnife) {
            canvas.DrawLine(x - size * 0.42, y - size * 0.54, x - size * 0.42, y + size * 0.48, color, stroke);
            canvas.DrawLine(x - size * 0.66, y - size * 0.56, x - size * 0.66, y - size * 0.12, color, stroke);
            canvas.DrawLine(x - size * 0.42, y - size * 0.56, x - size * 0.42, y - size * 0.12, color, stroke);
            canvas.DrawLine(x - size * 0.18, y - size * 0.56, x - size * 0.18, y - size * 0.12, color, stroke);
            canvas.DrawLine(x + size * 0.34, y + size * 0.48, x + size * 0.34, y - size * 0.52, color, stroke);
            canvas.DrawArc(x + size * 0.48, y - size * 0.22, size * 0.25, -Math.PI / 2, Math.PI / 2, color, stroke);
            return;
        }

        if (icon == VisualIcon.Flame) {
            canvas.DrawLine(x, y + size * 0.62, x - size * 0.42, y + size * 0.10, color, stroke);
            canvas.DrawLine(x - size * 0.42, y + size * 0.10, x - size * 0.08, y - size * 0.82, color, stroke);
            canvas.DrawLine(x - size * 0.08, y - size * 0.82, x + size * 0.22, y - size * 0.22, color, stroke);
            canvas.DrawLine(x + size * 0.22, y - size * 0.22, x + size * 0.54, y - size * 0.78, color, stroke);
            canvas.DrawLine(x + size * 0.54, y - size * 0.78, x + size * 0.72, y + size * 0.18, color, stroke);
            canvas.DrawLine(x + size * 0.72, y + size * 0.18, x, y + size * 0.62, color, stroke);
            return;
        }

        canvas.DrawLine(x - size * 0.52, y - size * 0.32, x + size * 0.10, y - size * 0.92, color, stroke);
        canvas.DrawLine(x + size * 0.10, y - size * 0.92, x, y - size * 0.26, color, stroke);
        canvas.DrawLine(x, y - size * 0.26, x + size * 0.58, y - size * 0.08, color, stroke);
        canvas.DrawLine(x + size * 0.58, y - size * 0.08, x - size * 0.20, y + size * 0.82, color, stroke);
        canvas.DrawLine(x - size * 0.20, y + size * 0.82, x - size * 0.04, y + size * 0.08, color, stroke);
    }

    private static void DrawCenteredText(RgbaCanvas canvas, string text, double x, double y, double size, ChartColor color, bool emphasized) {
        var fitted = FitText(text, size, Math.Max(1, x * 2));
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(fitted, size, null) : RgbaCanvas.MeasureTextWidth(fitted, size, null);
        if (emphasized) canvas.DrawTextEmphasized(x - width / 2, y, fitted, color, size);
        else canvas.DrawText(x - width / 2, y, fitted, color, size);
    }

    private static ChartColor ApplyOpacity(ChartColor color, double opacity) {
        var alpha = (byte)Math.Max(0, Math.Min(255, Math.Round(color.A * Math.Max(0, Math.Min(1, opacity)))));
        return ChartColor.FromRgba(color.R, color.G, color.B, alpha);
    }

    private static double RadialLayerRatio(ChartRadialLayer layer) => Math.Max(0, Math.Min(1, (layer.Value - layer.Minimum) / (layer.Maximum - layer.Minimum)));

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static string FitText(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || RgbaCanvas.MeasureTextWidth(value, fontSize, null) <= maxWidth) return value;
        const string suffix = "...";
        if (RgbaCanvas.MeasureTextWidth(suffix, fontSize, null) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (RgbaCanvas.MeasureTextWidth(value.Substring(0, mid) + suffix, fontSize, null) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }
}
