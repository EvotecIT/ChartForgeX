using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual blocks to dependency-free PNG images.
/// </summary>
public sealed partial class PngVisualBlockRenderer {
    /// <summary>Renders a visual block to PNG bytes.</summary>
    public byte[] Render(IVisualBlock block) => PngWriter.WriteRgba(RenderImage(block));

    internal RgbaImage RenderImage(IVisualBlock block) => RenderCanvas(block).ToImage();

    internal RgbaCanvas RenderCanvas(IVisualBlock block) {
        VisualBlockRendering.Validate(block);
        var options = block.Options;
        var theme = options.Theme;
        var canvas = new RgbaCanvas(options.Size.Width, options.Size.Height, 2, null, options.PngOutputScale);
        canvas.Clear(VisualBlockRendering.SurfaceBackground(options));
        if (options.ShowCard && theme.UseCard) {
            canvas.FillRoundedRectVerticalGradient(0, 0, options.Size.Width, options.Size.Height, theme.CornerRadius, ChartSurfacePolish.GradientTop(theme.CardBackground), ChartSurfacePolish.GradientBottom(theme.CardBackground));
            canvas.StrokeRoundedRect(0.5, 0.5, Math.Max(1, options.Size.Width - 1), Math.Max(1, options.Size.Height - 1), theme.CornerRadius, theme.CardBorder, 1);
            if (theme.CardBackground.A > 0) canvas.StrokeRoundedRect(ChartVisualPrimitives.CardInnerHighlightInset, ChartVisualPrimitives.CardInnerHighlightInset, Math.Max(1, options.Size.Width - ChartVisualPrimitives.CardInnerHighlightInset * 2), Math.Max(1, options.Size.Height - ChartVisualPrimitives.CardInnerHighlightInset * 2), Math.Max(0, theme.CornerRadius - ChartVisualPrimitives.CardInnerHighlightInset), ApplyOpacity(ChartColor.White, ChartVisualPrimitives.CardInnerHighlightOpacity), 1);
        }

        if (block is ChartTable table) DrawTable(canvas, table);
        else if (block is ChartList list) DrawList(canvas, list);
        else if (block is PacketLayoutBlock packet) DrawPacketLayout(canvas, packet);
        else if (block is BlockLayoutBlock blockLayout) DrawBlockLayout(canvas, blockLayout);
        else if (block is GitGraphBlock gitGraph) DrawGitGraph(canvas, gitGraph);
        else if (block is VennDiagramBlock venn) DrawVennDiagram(canvas, venn);
        else if (block is FishboneDiagramBlock fishbone) DrawFishboneDiagram(canvas, fishbone);
        else if (block is WardleyMapBlock wardleyMap) DrawWardleyMap(canvas, wardleyMap);
        else if (block is MetricCard card) DrawMetric(canvas, card);
        else if (block is RadialMetricCard radialCard) DrawRadialMetric(canvas, radialCard);
        else if (block is SegmentedMetricBlock segmentedMetric) DrawSegmentedMetric(canvas, segmentedMetric);
        else if (block is HeatmapInsightCard heatmapCard) DrawHeatmapInsightCard(canvas, heatmapCard);
        else if (block is DateStripBlock dateStrip) DrawDateStrip(canvas, dateStrip);
        else if (block is EntityStripBlock entityStrip) DrawEntityStrip(canvas, entityStrip);
        else if (block is SectionHeaderBlock sectionHeader) DrawSectionHeader(canvas, sectionHeader);
        else if (block is WorkloadListBlock workloadBlock) DrawWorkloadList(canvas, workloadBlock);
        else if (block is ActivityTimelineBlock activityBlock) DrawActivityTimeline(canvas, activityBlock);
        else if (block is ScheduleTimelineBlock scheduleBlock) DrawScheduleTimeline(canvas, scheduleBlock);
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
            canvas.FillRoundedRectVerticalGradient(content.X, y, content.Width, headerHeight, Math.Min(6, theme.PlotCornerRadius), ChartSurfacePolish.GradientTop(theme.PlotBackground), ChartSurfacePolish.GradientBottom(theme.PlotBackground));
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

                var cellWidth = widths[i] - (textX - x) - 7;
                if (cell.MicroVisualKind != ChartTableCellMicroVisualKind.None) DrawTableCellMicroVisual(canvas, table, cell, textX, y + 4, cellWidth, rowHeight - 8, i);
                else if (cell.BadgeText.Length > 0) DrawVisualBadge(canvas, table, cell, textX, y + 4, cellWidth, rowHeight - 8);
                else DrawAlignedText(canvas, cell.Text, textX, y + 8, cellWidth, cell.Alignment ?? table.Columns[i].Alignment, cell.Foreground ?? row.Foreground ?? theme.Text, table.Dense ? 10.5 : 11.5, false);
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
        var hasMicroVisual = card.MiniBars.Count > 0 || card.MiniSparkline.Count > 0;
        var heroMicroVisual = hasMicroVisual && card.MicroVisualPlacement == MetricCardMicroVisualPlacement.Hero;
        var valueInsetSurface = !hasMicroVisual && card.MicroVisualSurface == MetricCardMicroVisualSurface.Inset;
        var labelX = content.X;
        var labelWidth = content.Width;
        var valueYOffset = 0.0;
        if (card.Status != VisualStatus.None) {
            if (options.ShowCard && theme.UseCard) {
                var barInset = ChartVisualPrimitives.CardInnerHighlightInset;
                canvas.FillRectClippedToRoundedRect(barInset, barInset, ChartVisualPrimitives.MetricStatusBarWidth, Math.Max(1, options.Size.Height - barInset * 2), 0, 0, options.Size.Width, options.Size.Height, theme.CornerRadius, statusColor);
            }
            else canvas.FillRect(0, 0, ChartVisualPrimitives.MetricStatusBarWidth, options.Size.Height, statusColor);
        }

        if (!valueInsetSurface && (card.Icon != VisualIcon.None || card.Symbol.Length > 0)) {
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
            else {
                var symbolMaxWidth = badgeRadius * 1.62;
                var symbolSize = MetricSymbolFontSize(card.Symbol, Math.Max(10, badgeRadius * 0.46), symbolMaxWidth);
                DrawCenteredTextMiddle(canvas, card.Symbol, cx, cy, symbolSize, ChartColorMath.TextOnBackground(badgeColor), true, symbolMaxWidth);
            }
        }

        var labelSize = Math.Max(11, theme.SubtitleFontSize);
        var baseValueSize = Math.Min(54, Math.Max(26, options.Size.Height * 0.22));
        var microWidth = hasMicroVisual ? heroMicroVisual ? Math.Max(92, content.Width - 48) : Math.Min(112, Math.Max(58, content.Width * 0.32)) : 0;
        var microHeight = heroMicroVisual ? Math.Min(66, Math.Max(50, options.Size.Height * 0.30)) : Math.Min(56, Math.Max(34, options.Size.Height * 0.24));
        var valueWidth = hasMicroVisual ? Math.Max(1, content.Width - microWidth - 18) : content.Width;
        if (heroMicroVisual) valueWidth = content.Width;
        if (valueInsetSurface && (card.Icon != VisualIcon.None || card.Symbol.Length > 0)) valueWidth = Math.Max(1, content.Width - 106);
        var valueSize = MetricValueFontSize(card, baseValueSize, valueWidth);
        canvas.DrawTextEmphasized(labelX, content.Y, FitText(card.Label, labelSize, labelWidth), theme.MutedText, labelSize);
        if (valueInsetSurface) DrawMetricValueSurface(canvas, card, content, detailBottom, labelSize, valueSize, valueWidth, statusColor);
        else DrawMetricValueText(canvas, card, content.X, content.Y + labelSize + 18 + valueYOffset, valueSize, valueWidth, theme.Text, theme.MutedText);
        var microX = heroMicroVisual ? content.X + 24 : content.X + content.Width - microWidth;
        var microY = heroMicroVisual ? content.Y + Math.Max(66, options.Size.Height * 0.36) : content.Y + labelSize + Math.Max(20, valueSize * 0.52) + valueYOffset;
        if (heroMicroVisual && card.MicroVisualSurface == MetricCardMicroVisualSurface.Inset) {
            var surfaceX = content.X;
            var surfaceY = microY - 18;
            var surfaceWidth = content.Width;
            var availableSurfaceHeight = Math.Max(1, detailBottom - surfaceY - 4);
            var surfaceHeight = Math.Min(Math.Max(88, detailBottom - surfaceY - 24), availableSurfaceHeight);
            canvas.FillRoundedRectVerticalGradient(surfaceX, surfaceY, surfaceWidth, surfaceHeight, Math.Min(18, theme.PlotCornerRadius + 6), ChartSurfacePolish.GradientTop(theme.PlotBackground.WithAlpha(170)), ChartSurfacePolish.GradientBottom(theme.PlotBackground.WithAlpha(170)));
            canvas.StrokeRoundedRect(surfaceX, surfaceY, surfaceWidth, surfaceHeight, Math.Min(18, theme.PlotCornerRadius + 6), theme.PlotBorder.WithAlpha(125), 1);
            microX = surfaceX + 28;
            microY = surfaceY + Math.Min(32, Math.Max(10, surfaceHeight * 0.36));
            microWidth = Math.Max(1, surfaceWidth - 56);
            microHeight = Math.Max(1, surfaceHeight - (microY - surfaceY) - 18);
        }

        if (card.MiniSparkline.Count > 0) DrawMetricMiniSparkline(canvas, card, microX, microY, microWidth, microHeight);
        else if (card.MiniBars.Count > 0) DrawMetricMiniBars(canvas, card, microX, microY, microWidth, microHeight);
        var detailsTop = heroMicroVisual ? microY + microHeight + 10 : content.Y + labelSize + valueSize + 24 + valueYOffset;
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
        DrawActionSymbol(canvas, card.ActionSymbol, x + width - 14, footerY + footerHeight * 0.5, 12, theme.Text, fontSize);
    }

    private static void DrawMetricMiniBars(RgbaCanvas canvas, MetricCard card, double x, double y, double width, double height) {
        foreach (var bar in VisualBlockRendering.CreateMiniBars(card, x, y, width, height)) {
            canvas.FillRoundedRect(bar.X, bar.Y, bar.Width, bar.Height, bar.Radius, bar.Color);
        }
    }

    private static double MetricSymbolFontSize(string value, double requestedSize, double maxWidth) {
        var fontSize = requestedSize;
        while (fontSize > 7.5 && RgbaCanvas.MeasureTextEmphasizedWidth(value, fontSize, null) > maxWidth) fontSize -= 0.5;
        return Math.Max(7.5, fontSize);
    }

    private static void DrawMetricMiniSparkline(RgbaCanvas canvas, MetricCard card, double x, double y, double width, double height) {
        var sparkline = VisualBlockRendering.CreateMiniSparkline(card, x, y, width, height);
        var points = card.MiniSparklineStyle == MetricCardSparklineStyle.Line ? VisualBlockRendering.SmoothMiniSparklinePoints(sparkline) : sparkline.Points;
        if (card.MiniSparklineStyle == MetricCardSparklineStyle.Area) canvas.FillPolygon(sparkline.Area, sparkline.FillColor);
        else if (card.SecondaryMiniSparkline.Count > 0) {
            var secondary = VisualBlockRendering.CreateSecondaryMiniSparkline(card, x, y, width, height);
            var secondaryPoints = VisualBlockRendering.SmoothMiniSparklinePoints(secondary);
            for (var i = 1; i < secondaryPoints.Count; i++) canvas.DrawLine(secondaryPoints[i - 1].X, secondaryPoints[i - 1].Y, secondaryPoints[i].X, secondaryPoints[i].Y, secondary.LineColor, Math.Max(1.8, secondary.StrokeWidth * 0.72));
        }

        for (var i = 1; i < points.Count; i++) canvas.DrawLine(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, sparkline.LineColor, sparkline.StrokeWidth);
        if (card.MiniSparklineStyle == MetricCardSparklineStyle.Line) canvas.DrawCircle(sparkline.Points[0].X, sparkline.Points[0].Y, sparkline.CurrentRadius * 0.82, sparkline.LineColor);
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
            canvas.FillRoundedRectVerticalGradient(x, y, cellWidth, height, Math.Min(8, height / 2), ChartSurfacePolish.GradientTop(theme.PlotBackground.WithAlpha(150)), ChartSurfacePolish.GradientBottom(theme.PlotBackground.WithAlpha(150)));
            canvas.StrokeRoundedRect(x, y, cellWidth, height, Math.Min(8, height / 2), theme.CardBorder.WithAlpha(120));
            canvas.DrawCircle(x + 10, y + rowHeight / 2 - 2, 3.2, marker);
            canvas.DrawTextEmphasized(x + 18, y + rowHeight / 2 - labelSize * 0.45, FitText(detail.Label, labelSize, cellWidth * 0.55), theme.MutedText, labelSize);
            DrawAlignedText(canvas, detail.Value, x + cellWidth * 0.58, y + rowHeight / 2 - valueSize * 0.35, cellWidth * 0.36, VisualTextAlignment.Right, theme.Text, valueSize, true);
        }
    }

    private static void DrawSegmentedMetric(RgbaCanvas canvas, SegmentedMetricBlock card) {
        if (card.Style == SegmentedMetricStyle.CapsuleLoop) {
            DrawSegmentedMetricCapsuleLoop(canvas, card);
            return;
        }

        if (card.Style == SegmentedMetricStyle.FunnelColumns) {
            DrawSegmentedMetricFunnelColumns(canvas, card);
            return;
        }

        if (card.Style == SegmentedMetricStyle.CompositionStrip) {
            DrawSegmentedMetricComposition(canvas, card);
            return;
        }

        if (card.Style == SegmentedMetricStyle.DistributionRows) {
            DrawDistributionRows(canvas, card);
            return;
        }

        DrawSegmentedMetricProgressRows(canvas, card);
    }

    private static void DrawWorkloadList(RgbaCanvas canvas, WorkloadListBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        var hasAction = block.ActionLabel.Length > 0;
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, options.Size.Height * 0.14)) : 0;
        var bottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var rowHeight = Math.Max(44, Math.Min(64, (bottom - y) / Math.Max(1, block.Rows.Count)));
        for (var i = 0; i < block.Rows.Count && y + rowHeight <= bottom + 1; i++) {
            var row = block.Rows[i];
            var color = row.Color ?? (row.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, row.Status));
            var avatarSize = Math.Min(34, rowHeight - 14);
            var centerY = y + rowHeight * 0.42;
            canvas.DrawCircle(content.X + avatarSize / 2, centerY, avatarSize / 2, color.WithAlpha(42));
            canvas.DrawCircleOutline(content.X + avatarSize / 2, centerY, avatarSize / 2, color.WithAlpha(105), 1);
            DrawAlignedText(canvas, WorkloadAvatar(row), content.X + 1, centerY - Math.Max(9, theme.SubtitleFontSize - 1) * 0.46, avatarSize - 2, VisualTextAlignment.Center, color, Math.Max(9, theme.SubtitleFontSize - 1), true);
            var checkWidth = block.ShowSelectionControls ? 28 : 0;
            var valueText = row.Note.Length > 0 ? row.Note + "  " + VisualBlockRendering.WorkloadDisplayValue(row) : VisualBlockRendering.WorkloadDisplayValue(row);
            var valueWidth = Math.Min(98, Math.Max(52, RgbaCanvas.MeasureTextEmphasizedWidth(valueText, theme.SubtitleFontSize, null) + 10));
            var textX = content.X + avatarSize + 14;
            var textWidth = Math.Max(24, content.X + content.Width - textX - valueWidth - checkWidth - 10);
            DrawAlignedText(canvas, row.Label, textX, y + 8, textWidth, VisualTextAlignment.Left, theme.Text, Math.Max(12, theme.SubtitleFontSize + 1), true);
            if (row.Subtitle.Length > 0) DrawAlignedText(canvas, row.Subtitle, textX, y + 24, textWidth, VisualTextAlignment.Left, theme.MutedText, Math.Max(10, theme.SubtitleFontSize - 1), false);
            DrawAlignedText(canvas, valueText, content.X + content.Width - valueWidth - checkWidth, y + 11, valueWidth, VisualTextAlignment.Right, row.Status == VisualStatus.Negative || row.Status == VisualStatus.Warning ? color : theme.Text, theme.SubtitleFontSize, true);
            if (block.ShowProgressRails) {
                var railY = y + rowHeight - 13;
                var railWidth = Math.Max(18, content.X + content.Width - textX - checkWidth);
                canvas.FillRoundedRect(textX, railY, railWidth, 6, 3, theme.MutedText.WithAlpha(28));
                canvas.FillRoundedRect(textX, railY, Math.Max(0, railWidth * VisualBlockRendering.WorkloadRatio(row)), 6, 3, color);
            }

            if (block.ShowSelectionControls) {
                var controlX = content.X + content.Width - checkWidth + 4;
                if (row.Selected) canvas.FillRoundedRect(controlX, centerY - 8, 16, 16, 4, color.WithAlpha(46));
                canvas.StrokeRoundedRect(controlX, centerY - 8, 16, 16, 4, row.Selected ? color : theme.PlotBorder, 1);
                if (row.Selected) {
                    canvas.DrawLine(controlX + 4, centerY, controlX + 7, centerY + 4, color, 1.6);
                    canvas.DrawLine(controlX + 7, centerY + 4, controlX + 13, centerY - 5, color, 1.6);
                }
            }

            if (block.ShowDividers && i < block.Rows.Count - 1) canvas.DrawLine(textX, y + rowHeight - 1, content.X + content.Width, y + rowHeight - 1, theme.PlotBorder.WithAlpha(120), 1);
            y += rowHeight;
        }

        if (hasAction) DrawFooterAction(canvas, block.ActionLabel, block.ActionSymbol, options.Size.Height - footerHeight, footerHeight, content.X, content.Width, theme);
    }

    private static void DrawScheduleTimeline(RgbaCanvas canvas, ScheduleTimelineBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        DrawScheduleHeaderActions(canvas, block, ref y, content.X, content.Width);
        var axisHeight = 36.0;
        var plotTop = y + axisHeight;
        var plotHeight = Math.Max(1, options.Size.Height - options.Padding.Bottom - plotTop);
        var laneCount = VisualBlockRendering.ScheduleLaneCount(block);
        var laneGap = 10.0;
        var laneHeight = Math.Max(26, (plotHeight - laneGap * Math.Max(0, laneCount - 1)) / laneCount);
        foreach (var tick in VisualBlockRendering.ScheduleTicks(block)) {
            var x = content.X + content.Width * VisualBlockRendering.ScheduleRatio(block, tick);
            if (block.ShowGrid) canvas.DrawDashedLine(x, plotTop, x, plotTop + plotHeight, theme.Grid.WithAlpha(140), 1, 4, 5);
            DrawAlignedText(canvas, VisualBlockRendering.FormatScheduleHour(tick), Math.Max(content.X, Math.Min(content.X + content.Width - 88, x - 44)), y + 7, 88, VisualTextAlignment.Center, theme.MutedText, Math.Max(10, theme.SubtitleFontSize), true);
        }

        for (var lane = 0; lane < laneCount; lane++) {
            var laneY = plotTop + lane * (laneHeight + laneGap);
            canvas.DrawLine(content.X, laneY + laneHeight / 2, content.X + content.Width, laneY + laneHeight / 2, theme.PlotBorder.WithAlpha(70), 1);
        }

        if (block.CurrentTime.HasValue && VisualBlockRendering.IsScheduleTimeInRange(block, block.CurrentTime.Value)) {
            var currentX = content.X + content.Width * VisualBlockRendering.ScheduleRatio(block, block.CurrentTime.Value);
            canvas.DrawDashedLine(currentX, y + 24, currentX, plotTop + plotHeight, theme.Warning, 2, 6, 5);
        }

        for (var i = 0; i < block.Events.Count; i++) {
            var item = block.Events[i];
            if (!VisualBlockRendering.ScheduleEventIntersects(block, item)) continue;
            var color = item.Color ?? (item.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, item.Status));
            var x1 = content.X + content.Width * VisualBlockRendering.ScheduleRatio(block, item.Start);
            var x2 = content.X + content.Width * VisualBlockRendering.ScheduleRatio(block, item.End);
            var left = Math.Max(content.X, Math.Min(x1, x2));
            var right = Math.Min(content.X + content.Width, Math.Max(x1, x2));
            var width = Math.Max(10, right - left);
            var laneY = plotTop + item.Lane * (laneHeight + laneGap);
            var eventHeight = Math.Min(34, laneHeight);
            var eventY = laneY + Math.Max(2, (laneHeight - eventHeight) / 2);
            var radius = Math.Min(8, eventHeight / 2);
            canvas.FillRoundedRect(left, eventY, width, eventHeight, radius, color.WithAlpha(34));
            canvas.StrokeRoundedRect(left, eventY, width, eventHeight, radius, color.WithAlpha(130), 1);
            canvas.FillRoundedRect(left, eventY, Math.Min(5, width), eventHeight, Math.Min(3, radius), color);
            var avatarReserve = Math.Min(width * 0.34, item.Avatars.Count == 0 ? 0 : 18 + Math.Min(3, item.Avatars.Count) * 14);
            var badgeReserve = item.Badge.Length == 0 ? 0 : Math.Min(78, RgbaCanvas.MeasureTextEmphasizedWidth(item.Badge, theme.SubtitleFontSize, null) + 18);
            DrawAlignedText(canvas, item.Title, left + 14, eventY + eventHeight * 0.34, Math.Max(1, width - 20 - avatarReserve - badgeReserve), VisualTextAlignment.Left, color, Math.Max(10, theme.SubtitleFontSize), true);
            if (item.Badge.Length > 0) {
                var badgeWidth = Math.Min(78, badgeReserve);
                canvas.FillRoundedRect(left + width - avatarReserve - badgeWidth - 6, eventY + 7, badgeWidth, eventHeight - 14, 7, color.WithAlpha(50));
                DrawAlignedText(canvas, item.Badge, left + width - avatarReserve - badgeWidth, eventY + eventHeight * 0.36, badgeWidth - 10, VisualTextAlignment.Center, color, Math.Max(9, theme.SubtitleFontSize - 1), true);
            }

            DrawScheduleAvatars(canvas, item, color, theme, left + width - avatarReserve + 3, eventY + eventHeight / 2);
        }
    }

    private static void DrawScheduleHeaderActions(RgbaCanvas canvas, ScheduleTimelineBlock block, ref double y, double x, double width) {
        if (block.HeaderActions.Count == 0) return;
        var theme = block.Options.Theme;
        var cursor = x + width;
        for (var i = block.HeaderActions.Count - 1; i >= 0; i--) {
            var action = block.HeaderActions[i];
            var actionWidth = Math.Min(140, Math.Max(62, RgbaCanvas.MeasureTextEmphasizedWidth(action, theme.SubtitleFontSize, null) + 28));
            actionWidth = Math.Min(actionWidth, Math.Max(0, cursor - x));
            if (actionWidth < 36) break;
            cursor -= actionWidth;
            canvas.FillRoundedRect(cursor, y, actionWidth, 30, 8, ChartColor.White);
            canvas.StrokeRoundedRect(cursor, y, actionWidth, 30, 8, theme.CardBorder, 1);
            DrawAlignedText(canvas, action, cursor + 12, y + 8, actionWidth - 24, VisualTextAlignment.Left, theme.Text, Math.Max(10, theme.SubtitleFontSize), true);
            cursor -= 8;
        }

        y += 42;
    }

    private static void DrawScheduleAvatars(RgbaCanvas canvas, ScheduleTimelineEvent item, ChartColor color, ChartForgeX.Themes.ChartTheme theme, double x, double y) {
        var count = Math.Min(3, item.Avatars.Count);
        for (var i = 0; i < count; i++) {
            var cx = x + i * 14;
            canvas.DrawCircle(cx, y, 8, theme.CardBackground);
            canvas.DrawCircleOutline(cx, y, 8, color, 1);
            DrawAlignedText(canvas, item.Avatars[i], cx - 7, y - 4, 14, VisualTextAlignment.Center, color, 8, true);
        }

        if (item.Avatars.Count > count) {
            var cx = x + count * 14;
            canvas.DrawCircle(cx, y, 8, color.WithAlpha(45));
            canvas.DrawCircleOutline(cx, y, 8, color, 1);
            DrawAlignedText(canvas, "+" + (item.Avatars.Count - count).ToString(System.Globalization.CultureInfo.InvariantCulture), cx - 7, y - 4, 14, VisualTextAlignment.Center, color, 8, true);
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

    private static string WorkloadAvatar(WorkloadListRow row) {
        if (row.AvatarText.Length > 0) return row.AvatarText;
        var parts = row.Label.Split(new[] { ' ', '\t', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return string.Empty;
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
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

    private static void DrawCenteredText(RgbaCanvas canvas, string text, double x, double y, double size, ChartColor color, bool emphasized, double? maxWidth = null) {
        var fitted = FitText(text, size, Math.Max(1, maxWidth ?? x * 2));
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(fitted, size, null) : RgbaCanvas.MeasureTextWidth(fitted, size, null);
        if (emphasized) canvas.DrawTextEmphasized(x - width / 2, y, fitted, color, size);
        else canvas.DrawText(x - width / 2, y, fitted, color, size);
    }

    private static void DrawCenteredTextMiddle(RgbaCanvas canvas, string text, double x, double y, double size, ChartColor color, bool emphasized, double? maxWidth = null) {
        var fitted = FitText(text, size, Math.Max(1, maxWidth ?? x * 2));
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(fitted, size, null) : RgbaCanvas.MeasureTextWidth(fitted, size, null);
        var height = RgbaCanvas.MeasureTextHeight(size, null);
        if (emphasized) canvas.DrawTextEmphasized(x - width / 2, y - height / 2, fitted, color, size);
        else canvas.DrawText(x - width / 2, y - height / 2, fitted, color, size);
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
