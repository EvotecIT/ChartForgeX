using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual blocks to self-contained SVG.
/// </summary>
public sealed partial class SvgVisualBlockRenderer {
    private static long ScopeCounter;

    /// <summary>Renders a visual block to SVG markup.</summary>
    public string Render(IVisualBlock block) => Render(block, NextScope());

    /// <summary>Renders a visual block to SVG markup with a caller-provided ID scope.</summary>
    public string Render(IVisualBlock block, string idScope) {
        VisualBlockRendering.Validate(block);
        var options = block.Options;
        var theme = options.Theme;
        var surfaceBackground = VisualBlockRendering.SurfaceBackground(options);
        var id = "cfx-visual-" + VisualBlockRendering.StableHash(idScope ?? string.Empty, block.AccessibleName, options.Size.Width.ToString(CultureInfo.InvariantCulture), options.Size.Height.ToString(CultureInfo.InvariantCulture));
        var writer = new SvgMarkupWriter(4096);
        writer.StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("id", id)
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

        writer.StartElement("defs").EndStartElement().Line();
        SvgSurfacePolish.WriteScopedStrokeStyle(writer, id);
        SvgSurfacePolish.WriteSurfaceGradient(writer, id, "visualBackground", surfaceBackground);
        SvgSurfacePolish.WriteSurfaceGradient(writer, id, "visualCard", theme.CardBackground);
        SvgSurfacePolish.WriteSurfaceGradient(writer, id, "visualPlot", theme.PlotBackground);
        writer.StartElement("clipPath").Attribute("id", id + "-visualCardClip").EndStartElement()
            .StartElement("rect").Attribute("x", 0).Attribute("y", 0).Attribute("width", options.Size.Width).Attribute("height", options.Size.Height).Attribute("rx", Math.Max(0, theme.CornerRadius)).EndEmptyElement()
            .EndElement()
            .Line();
        writer.EndElement().Line();

        if (!options.TransparentBackground && surfaceBackground.A > 0) writer.StartElement("rect").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", "url(#" + id + "-visualBackground)").EndEmptyElement().Line();
        if (options.ShowCard && theme.UseCard) {
            writer.StartElement("rect").Attribute("data-cfx-role", "visual-card").Attribute("class", ChartVisualPrimitives.SvgGuideStrokeClass).Attribute("x", 0.5).Attribute("y", 0.5).Attribute("width", Math.Max(0, options.Size.Width - 1)).Attribute("height", Math.Max(0, options.Size.Height - 1)).Attribute("rx", Math.Max(0, theme.CornerRadius - 0.5)).Attribute("fill", "url(#" + id + "-visualCard)").Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            if (theme.CardBackground.A > 0) writer.StartElement("rect").Attribute("data-cfx-role", "visual-card-highlight").Attribute("class", ChartVisualPrimitives.SvgGuideStrokeClass).Attribute("x", ChartVisualPrimitives.CardInnerHighlightInset).Attribute("y", ChartVisualPrimitives.CardInnerHighlightInset).Attribute("width", Math.Max(0, options.Size.Width - ChartVisualPrimitives.CardInnerHighlightInset * 2)).Attribute("height", Math.Max(0, options.Size.Height - ChartVisualPrimitives.CardInnerHighlightInset * 2)).Attribute("rx", Math.Max(0, theme.CornerRadius - ChartVisualPrimitives.CardInnerHighlightInset)).Attribute("fill", "none").Attribute("stroke", "#fff").Attribute("stroke-opacity", ChartVisualPrimitives.CardInnerHighlightOpacity).EndEmptyElement().Line();
        }

        if (block is ChartTable table) RenderTable(writer, table, id);
        else if (block is ChartList list) RenderList(writer, list);
        else if (block is MetricCard card) RenderMetric(writer, card, id);
        else if (block is RadialMetricCard radialCard) RenderRadialMetric(writer, radialCard);
        else if (block is SegmentedProgressCard segmentedCard) RenderSegmentedProgress(writer, segmentedCard);
        else if (block is CompositionStatusCard compositionCard) RenderCompositionStatus(writer, compositionCard);
        else if (block is DistributionStripCard distributionCard) RenderDistributionStripCard(writer, distributionCard);
        else if (block is HeatmapInsightCard heatmapCard) RenderHeatmapInsightCard(writer, heatmapCard);
        else if (block is DateStripBlock dateStrip) RenderDateStrip(writer, dateStrip);
        else if (block is EntityStripBlock entityStrip) RenderEntityStrip(writer, entityStrip);
        else if (block is SectionHeaderBlock sectionHeader) RenderSectionHeader(writer, sectionHeader);
        else if (block is WorkloadListBlock workloadBlock) RenderWorkloadList(writer, workloadBlock);
        else if (block is ActivityTimelineBlock activityBlock) RenderActivityTimeline(writer, activityBlock);
        else if (block is ScheduleTimelineBlock scheduleBlock) RenderScheduleTimeline(writer, scheduleBlock);

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
    private static void RenderTable(SvgMarkupWriter writer, ChartTable table, string id) {
        var options = table.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, table, ref y, content.X, content.Width);
        var headerHeight = table.Dense ? 26.0 : 32.0;
        var rowHeight = table.Dense ? 24.0 : 31.0;
        var widths = ColumnWidths(table, content.Width);
        if (table.ShowHeader) {
            writer.StartElement("rect").Attribute("data-cfx-role", "table-header").Attribute("class", ChartVisualPrimitives.SvgGuideStrokeClass).Attribute("x", content.X).Attribute("y", y).Attribute("width", content.Width).Attribute("height", headerHeight).Attribute("rx", Math.Min(6, theme.PlotCornerRadius)).Attribute("fill", "url(#" + id + "-visualPlot)").Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
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

                var cellWidth = widths[i] - (textX - x) - 7;
                if (cell.MicroVisualKind != ChartTableCellMicroVisualKind.None) RenderTableCellMicroVisual(writer, table, cell, textX, y + 4, cellWidth, rowHeight - 8, i);
                else if (cell.BadgeText.Length > 0) RenderVisualBadge(writer, table, cell, textX, y + 4, cellWidth, rowHeight - 8);
                else WriteText(writer, cell.Text, textX, y + rowHeight * 0.66, cellWidth, cell.Alignment ?? table.Columns[i].Alignment, cell.Foreground ?? row.Foreground ?? theme.Text, theme.FontFamily, table.Dense ? 10.5 : 11.5, "400");
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
    private static void RenderMetric(SvgMarkupWriter writer, MetricCard card, string id) {
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
            var barInset = options.ShowCard && theme.UseCard ? ChartVisualPrimitives.CardInnerHighlightInset : 0;
            writer.StartElement("rect").Attribute("data-cfx-role", "metric-status-bar").Attribute("x", barInset).Attribute("y", barInset).Attribute("width", ChartVisualPrimitives.MetricStatusBarWidth).Attribute("height", Math.Max(1, options.Size.Height - barInset * 2)).Attribute("fill", statusColor.ToCss());
            if (options.ShowCard && theme.UseCard) writer.Attribute("clip-path", "url(#" + id + "-visualCardClip)");
            writer.EndEmptyElement().Line();
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

            var symbolMaxWidth = badgeRadius * 1.62;
            var symbolSize = VisualBlockRendering.FitFontSize(card.Symbol, symbolMaxWidth, Math.Max(10, badgeRadius * 0.46), 7.5);
            var symbolColor = ChartColorMath.TextOnBackground(badgeColor);
            writer.StartElement("circle").Attribute("data-cfx-role", "metric-symbol-badge").Attribute("data-cfx-placement", leftBadge ? "top-left" : "top-right").Attribute("cx", cx).Attribute("cy", cy).Attribute("r", badgeRadius).Attribute("fill", badgeColor.WithAlpha(48).ToCss()).Attribute("stroke", badgeColor.ToCss()).EndEmptyElement().Line();
            if (card.Icon != VisualIcon.None) WriteIcon(writer, card.Icon, cx, cy, badgeRadius * 0.62, badgeColor);
            else writer.StartElement("text").Attribute("data-cfx-role", "metric-symbol").Attribute("x", cx).Attribute("y", cy).Attribute("text-anchor", "middle").Attribute("dominant-baseline", "central").Attribute("alignment-baseline", "central").Attribute("fill", symbolColor.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", symbolSize).Attribute("font-weight", "850").Text(VisualBlockRendering.FitText(card.Symbol, symbolSize, symbolMaxWidth)).EndElement().Line();
        }

        var labelSize = Math.Max(11, theme.SubtitleFontSize);
        var baseValueSize = Math.Min(54, Math.Max(26, options.Size.Height * 0.22));
        var microWidth = hasMicroVisual ? heroMicroVisual ? Math.Max(92, content.Width - 48) : Math.Min(112, Math.Max(58, content.Width * 0.32)) : 0;
        var microHeight = heroMicroVisual ? Math.Min(66, Math.Max(50, options.Size.Height * 0.30)) : Math.Min(56, Math.Max(34, options.Size.Height * 0.24));
        var valueWidth = hasMicroVisual ? Math.Max(1, content.Width - microWidth - 18) : content.Width;
        if (heroMicroVisual) valueWidth = content.Width;
        if (valueInsetSurface && (card.Icon != VisualIcon.None || card.Symbol.Length > 0)) valueWidth = Math.Max(1, content.Width - 106);
        var valueSize = MetricValueFontSize(card, baseValueSize, valueWidth);
        writer.StartElement("text").Attribute("data-cfx-role", "metric-label").Attribute("x", labelX).Attribute("y", content.Y + labelSize).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", labelSize).Attribute("font-weight", "700").Text(VisualBlockRendering.FitText(card.Label, labelSize, labelWidth)).EndElement().Line();
        if (valueInsetSurface) RenderMetricValueSurface(writer, card, content, detailBottom, labelSize, valueSize, valueWidth, statusColor);
        else RenderMetricValueText(writer, card, content.X, content.Y + labelSize + valueSize + 14 + valueYOffset, valueSize, valueWidth, theme.Text, theme.MutedText);
        var microX = heroMicroVisual ? content.X + 24 : content.X + content.Width - microWidth;
        var microY = heroMicroVisual ? content.Y + Math.Max(66, options.Size.Height * 0.36) : content.Y + labelSize + Math.Max(20, valueSize * 0.52) + valueYOffset;
        if (heroMicroVisual && card.MicroVisualSurface == MetricCardMicroVisualSurface.Inset) {
            var surfaceX = content.X;
            var surfaceY = microY - 18;
            var surfaceWidth = content.Width;
            var availableSurfaceHeight = Math.Max(1, detailBottom - surfaceY - 4);
            var surfaceHeight = Math.Min(Math.Max(88, detailBottom - surfaceY - 24), availableSurfaceHeight);
            writer.StartElement("rect").Attribute("data-cfx-role", "metric-micro-surface").Attribute("x", surfaceX).Attribute("y", surfaceY).Attribute("width", surfaceWidth).Attribute("height", surfaceHeight).Attribute("rx", Math.Min(18, theme.PlotCornerRadius + 6)).Attribute("fill", theme.PlotBackground.WithAlpha(170).ToCss()).Attribute("stroke", theme.PlotBorder.WithAlpha(125).ToCss()).EndEmptyElement().Line();
            microX = surfaceX + 28;
            microY = surfaceY + Math.Min(32, Math.Max(10, surfaceHeight * 0.36));
            microWidth = Math.Max(1, surfaceWidth - 56);
            microHeight = Math.Max(1, surfaceHeight - (microY - surfaceY) - 18);
        }

        if (card.MiniSparkline.Count > 0) RenderMetricMiniSparkline(writer, card, microX, microY, microWidth, microHeight);
        else if (card.MiniBars.Count > 0) RenderMetricMiniBars(writer, card, microX, microY, microWidth, microHeight);
        var detailsTop = heroMicroVisual ? microY + microHeight + 10 : content.Y + labelSize + valueSize + 22 + valueYOffset;
        RenderMetricDetails(writer, card, content, detailsTop, detailBottom);
        RenderMetricDetail(writer, card, detailBottom, content.X, content.Width);
        if (hasAction) RenderMetricAction(writer, card, footerY, footerHeight, content.X, content.Width);
    }

    private static void RenderMetricDetail(SvgMarkupWriter writer, MetricCard card, double bottom, double x, double width) {
        var theme = card.Options.Theme;
        var statusColor = VisualBlockRendering.StatusColor(theme, card.Status);
        var trendSize = Math.Max(10, theme.SubtitleFontSize);
        var captionSize = Math.Max(10, theme.SubtitleFontSize - 1);
        var baseline = bottom - (card.Trend.Length > 0 && card.Caption.Length > 0 ? 5 : 0);
        if (card.Trend.Length > 0) {
            var trendWidth = Math.Min(width * 0.42, VisualBlockRendering.EstimateTextWidth(card.Trend, trendSize) + 10);
            writer.StartElement("text").Attribute("data-cfx-role", "metric-trend").Attribute("x", x).Attribute("y", baseline).Attribute("fill", statusColor.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", trendSize).Attribute("font-weight", "700").Text(VisualBlockRendering.FitText(card.Trend, trendSize, trendWidth)).EndElement().Line();
            if (card.Caption.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "metric-caption").Attribute("x", x + trendWidth + 5).Attribute("y", baseline).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", captionSize).Text(VisualBlockRendering.FitText(card.Caption, captionSize, Math.Max(1, width - trendWidth - 5))).EndElement().Line();
        } else if (card.Caption.Length > 0) {
            writer.StartElement("text").Attribute("data-cfx-role", "metric-caption").Attribute("x", x).Attribute("y", baseline).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", captionSize).Text(VisualBlockRendering.FitText(card.Caption, captionSize, width)).EndElement().Line();
        }
    }

    private static void RenderMetricAction(SvgMarkupWriter writer, MetricCard card, double footerY, double footerHeight, double x, double width) {
        var theme = card.Options.Theme;
        writer.StartElement("line").Attribute("data-cfx-role", "metric-action-divider").Attribute("x1", 0).Attribute("y1", footerY).Attribute("x2", card.Options.Size.Width).Attribute("y2", footerY).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        var fontSize = Math.Max(10, theme.SubtitleFontSize);
        var symbolWidth = Math.Min(24, VisualBlockRendering.EstimateTextWidth(card.ActionSymbol, fontSize) + 6);
        var baseline = footerY + footerHeight * 0.64;
        if (card.ActionUrl.Length > 0) writer.StartElement("a").Attribute("data-cfx-role", "metric-action-link").Attribute("href", card.ActionUrl).Attribute("target", "_top").EndStartElement().Line();
        writer.StartElement("text").Attribute("data-cfx-role", "metric-action-label").Attribute("x", x).Attribute("y", baseline).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", fontSize).Text(VisualBlockRendering.FitText(card.ActionLabel, fontSize, Math.Max(1, width - symbolWidth - 8))).EndElement().Line();
        writer.StartElement("text").Attribute("data-cfx-role", "metric-action-symbol").Attribute("x", x + width).Attribute("y", baseline).Attribute("text-anchor", "end").Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", fontSize).Attribute("font-weight", "700").Text(VisualBlockRendering.FitText(card.ActionSymbol, fontSize, symbolWidth)).EndElement().Line();
        if (card.ActionUrl.Length > 0) writer.EndElement().Line();
    }

    private static void RenderMetricMiniBars(SvgMarkupWriter writer, MetricCard card, double x, double y, double width, double height) {
        var bounds = VisualBlockRendering.MiniBarBounds(card);
        var bars = VisualBlockRendering.CreateMiniBars(card, x, y, width, height);
        writer.StartElement("g").Attribute("data-cfx-role", "metric-mini-bars").Attribute("data-cfx-min", bounds.Minimum).Attribute("data-cfx-max", bounds.Maximum).EndStartElement().Line();
        foreach (var bar in bars) {
            writer.StartElement("rect")
                .Attribute("data-cfx-role", bar.Highlighted ? "metric-mini-bar-highlight" : "metric-mini-bar")
                .Attribute("data-cfx-index", bar.Index)
                .Attribute("data-cfx-value", bar.Value)
                .Attribute("x", bar.X)
                .Attribute("y", bar.Y)
                .Attribute("width", bar.Width)
                .Attribute("height", bar.Height)
                .Attribute("rx", bar.Radius)
                .Attribute("fill", bar.Color.ToCss());
            writer.EndEmptyElement().Line();
        }

        writer.EndElement().Line();
    }

    private static void RenderMetricMiniSparkline(SvgMarkupWriter writer, MetricCard card, double x, double y, double width, double height) {
        var bounds = VisualBlockRendering.MiniSparklineBounds(card);
        var sparkline = VisualBlockRendering.CreateMiniSparkline(card, x, y, width, height);

        writer.StartElement("g").Attribute("data-cfx-role", "metric-mini-sparkline").Attribute("data-cfx-style", card.MiniSparklineStyle.ToString().ToLowerInvariant()).Attribute("data-cfx-min", bounds.Minimum).Attribute("data-cfx-max", bounds.Maximum).EndStartElement().Line();
        if (card.MiniSparklineStyle == MetricCardSparklineStyle.Area) {
            writer.StartElement("polygon")
                .Attribute("data-cfx-role", "metric-mini-sparkline-fill")
                .Attribute("points", SparklinePoints(sparkline.Area))
                .Attribute("fill", sparkline.FillColor.ToCss())
                .EndEmptyElement()
                .Line();
            writer.StartElement("polyline")
                .Attribute("data-cfx-role", "metric-mini-sparkline-line")
                .Attribute("points", SparklinePoints(sparkline.Points))
                .Attribute("fill", "none")
                .Attribute("stroke", sparkline.LineColor.ToCss())
                .Attribute("stroke-width", sparkline.StrokeWidth)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement()
                .Line();
        } else {
            var path = SparklineSmoothPath(sparkline.Points, 0);
            if (card.SecondaryMiniSparkline.Count > 0) {
                var secondary = VisualBlockRendering.CreateSecondaryMiniSparkline(card, x, y, width, height);
                writer.StartElement("path").Attribute("data-cfx-role", "metric-mini-sparkline-secondary").Attribute("d", SparklineSmoothPath(secondary.Points, 0)).Attribute("fill", "none").Attribute("stroke", secondary.LineColor.ToCss()).Attribute("stroke-width", Math.Max(1.8, secondary.StrokeWidth * 0.72)).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
            }

            writer.StartElement("path").Attribute("data-cfx-role", "metric-mini-sparkline-line").Attribute("d", path).Attribute("fill", "none").Attribute("stroke", sparkline.LineColor.ToCss()).Attribute("stroke-width", sparkline.StrokeWidth).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
            writer.StartElement("circle").Attribute("data-cfx-role", "metric-mini-sparkline-start").Attribute("cx", sparkline.Points[0].X).Attribute("cy", sparkline.Points[0].Y).Attribute("r", sparkline.CurrentRadius * 0.82).Attribute("fill", sparkline.LineColor.ToCss()).EndEmptyElement().Line();
        }

        var last = sparkline.Current;
        writer.StartElement("circle").Attribute("data-cfx-role", "metric-mini-sparkline-current").Attribute("cx", last.X).Attribute("cy", last.Y).Attribute("r", sparkline.CurrentRadius).Attribute("fill", sparkline.LineColor.ToCss()).EndEmptyElement().Line();
        writer.EndElement().Line();
    }

    private static void RenderMetricDetails(SvgMarkupWriter writer, MetricCard card, ChartRect content, double top, double bottom) {
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
            var marker = VisualBlockRendering.StatusColor(theme, detail.Status);
            writer.StartElement("g").Attribute("data-cfx-role", "metric-detail").Attribute("data-cfx-label", detail.Label).EndStartElement().Line();
            writer.StartElement("rect").Attribute("x", x).Attribute("y", y).Attribute("width", cellWidth).Attribute("height", rowHeight - 4).Attribute("rx", Math.Min(8, (rowHeight - 4) / 2)).Attribute("fill", theme.PlotBackground.WithAlpha(150).ToCss()).Attribute("stroke", theme.CardBorder.WithAlpha(120).ToCss()).EndEmptyElement().Line();
            writer.StartElement("circle").Attribute("cx", x + 10).Attribute("cy", y + rowHeight / 2 - 2).Attribute("r", 3.2).Attribute("fill", marker.ToCss()).EndEmptyElement().Line();
            writer.StartElement("text").Attribute("x", x + 18).Attribute("y", y + rowHeight / 2 - 4).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", labelSize).Attribute("font-weight", "650").Text(VisualBlockRendering.FitText(detail.Label, labelSize, cellWidth * 0.55)).EndElement().Line();
            writer.StartElement("text").Attribute("x", x + cellWidth - 9).Attribute("y", y + rowHeight / 2 + 7).Attribute("text-anchor", "end").Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", valueSize).Attribute("font-weight", "800").Text(VisualBlockRendering.FitText(detail.Value, valueSize, cellWidth * 0.42)).EndElement().Line();
            writer.EndElement().Line();
        }
    }

    private static void RenderSegmentedProgress(SvgMarkupWriter writer, SegmentedProgressCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        if (card.HeaderSymbol.Length > 0 || card.ShowMenu) RenderSegmentedProgressHeader(writer, card, ref y, content.X, content.Width);
        else RenderBlockHeading(writer, card, ref y, content.X, content.Width);
        var hasAction = card.ActionLabel.Length > 0;
        var footerHeight = hasAction ? Math.Min(58, Math.Max(42, options.Size.Height * 0.16)) : 0;
        var bottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var rowHeight = Math.Max(48, Math.Min(72, (bottom - y) / Math.Max(1, card.Rows.Count)));
        writer.StartElement("g").Attribute("data-cfx-role", "segmented-progress-card").EndStartElement().Line();
        for (var rowIndex = 0; rowIndex < card.Rows.Count && y + 38 <= bottom; rowIndex++) {
            var row = card.Rows[rowIndex];
            var accent = row.Color ?? (row.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, rowIndex) : VisualBlockRendering.StatusColor(theme, row.Status));
            var valueText = Math.Round(VisualBlockRendering.SegmentRatio(row.Value, row.Maximum) * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
            var deltaWidth = row.Delta.Length == 0 ? 0 : Math.Min(92, VisualBlockRendering.EstimateTextWidth(row.Delta, theme.SubtitleFontSize) + 18);
            WriteText(writer, row.Label, content.X, y + theme.SubtitleFontSize, content.Width - deltaWidth - 58, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "600");
            if (row.Delta.Length > 0) {
                writer.StartElement("rect").Attribute("data-cfx-role", "segmented-progress-delta-pill").Attribute("x", content.X + content.Width - deltaWidth - 54).Attribute("y", y).Attribute("width", deltaWidth).Attribute("height", 22).Attribute("rx", 11).Attribute("fill", accent.WithAlpha(34).ToCss()).EndEmptyElement().Line();
                WriteText(writer, row.Delta, content.X + content.Width - deltaWidth - 48, y + 15.5, deltaWidth - 12, VisualTextAlignment.Center, accent, theme.FontFamily, theme.SubtitleFontSize, "800");
            }

            WriteText(writer, valueText, content.X + content.Width - 46, y + theme.SubtitleFontSize, 46, VisualTextAlignment.Right, theme.Text, theme.FontFamily, Math.Max(13, theme.SubtitleFontSize + 1), "850");
            var stripY = y + 30;
            var stripHeight = Math.Max(12, Math.Min(22, rowHeight * 0.28));
            RenderSegmentedStrip(writer, row, content.X, stripY, content.Width, stripHeight, accent, theme);
            y += rowHeight;
        }

        writer.EndElement().Line();
        if (hasAction) {
            if (card.ActionBackground.HasValue) RenderSegmentedFooterAction(writer, card, options.Size.Height - footerHeight, footerHeight, content.X, content.Width);
            else RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - footerHeight, footerHeight, content.X, content.Width, theme);
        }
    }

    private static void RenderSegmentedFooterAction(SvgMarkupWriter writer, SegmentedProgressCard card, double footerY, double footerHeight, double x, double width) {
        var theme = card.Options.Theme;
        var fill = card.ActionBackground ?? theme.PlotBackground;
        var foreground = card.ActionForeground ?? theme.Text;
        var inset = Math.Min(8, Math.Max(1, footerHeight * 0.16));
        writer.StartElement("rect").Attribute("data-cfx-role", "segmented-progress-action-band").Attribute("x", x).Attribute("y", footerY + 1).Attribute("width", width).Attribute("height", Math.Max(1, footerHeight - inset - 1)).Attribute("rx", Math.Min(10, Math.Max(2, footerHeight * 0.18))).Attribute("fill", fill.ToCss()).EndEmptyElement().Line();
        var fontSize = Math.Max(10, theme.SubtitleFontSize);
        var baseline = footerY + footerHeight * 0.64;
        if (card.ActionUrl.Length > 0) writer.StartElement("a").Attribute("data-cfx-role", "visual-action-link").Attribute("href", card.ActionUrl).Attribute("target", "_top").EndStartElement().Line();
        WriteText(writer, card.ActionLabel, x, baseline, Math.Max(1, width - 38), VisualTextAlignment.Left, foreground, theme.FontFamily, fontSize, "500");
        WriteText(writer, card.ActionSymbol, x + width - 28, baseline, 28, VisualTextAlignment.Right, foreground, theme.FontFamily, fontSize, "800");
        if (card.ActionUrl.Length > 0) writer.EndElement().Line();
    }

    private static void RenderSegmentedStrip(SvgMarkupWriter writer, SegmentedProgressRow row, double x, double y, double width, double height, ChartColor accent, ChartForgeX.Themes.ChartTheme theme) {
        var metrics = VisualBlockRendering.FitRepeatedItems(row.Segments, width, row.Segments > 50 ? 2.0 : 3.0, 2);
        var gap = metrics.Gap;
        var segmentWidth = metrics.ItemWidth;
        var filled = VisualBlockRendering.FilledSegments(row);
        var empty = theme.CardBackground.A > 0 ? theme.CardBackground : ChartColor.White;
        var emptyStroke = theme.PlotBorder.WithAlpha(120);
        writer.StartElement("g").Attribute("data-cfx-role", "segmented-progress-strip").Attribute("data-cfx-segments", row.Segments).Attribute("data-cfx-filled", filled).EndStartElement().Line();
        for (var i = 0; i < row.Segments; i++) {
            var segmentX = x + i * (segmentWidth + gap);
            var role = i < filled ? "segmented-progress-segment-filled" : "segmented-progress-segment-empty";
            var color = i < filled ? accent : empty;
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "segmented-progress-segment-shadow")
                .Attribute("data-cfx-index", i)
                .Attribute("x", segmentX + 0.6)
                .Attribute("y", y + 1.2)
                .Attribute("width", segmentWidth)
                .Attribute("height", height)
                .Attribute("rx", Math.Min(4, segmentWidth * 0.35))
                .Attribute("fill", theme.MutedText.WithAlpha(i < filled ? (byte)28 : (byte)18).ToCss())
                .EndEmptyElement().Line();
            writer.StartElement("rect")
                .Attribute("data-cfx-role", role)
                .Attribute("data-cfx-index", i)
                .Attribute("x", segmentX)
                .Attribute("y", y)
                .Attribute("width", segmentWidth)
                .Attribute("height", height)
                .Attribute("rx", Math.Min(4, segmentWidth * 0.35))
                .Attribute("fill", color.ToCss())
                .Attribute("stroke", i < filled ? accent.WithAlpha(120).ToCss() : emptyStroke.ToCss())
                .Attribute("stroke-width", 0.8)
                .EndEmptyElement().Line();
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "segmented-progress-segment-highlight")
                .Attribute("data-cfx-index", i)
                .Attribute("x", segmentX + 1)
                .Attribute("y", y + 1)
                .Attribute("width", Math.Max(1, segmentWidth - 2))
                .Attribute("height", Math.Max(1, height * 0.32))
                .Attribute("rx", Math.Min(3, segmentWidth * 0.28))
                .Attribute("fill", ChartColor.White.WithAlpha(i < filled ? (byte)48 : (byte)92).ToCss())
                .EndEmptyElement().Line();
        }

        writer.EndElement().Line();
    }

    private static void RenderCompositionStatus(SvgMarkupWriter writer, CompositionStatusCard card) {
        var options = card.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, card, ref y, content.X, content.Width);
        var hasAction = card.ActionLabel.Length > 0;
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, options.Size.Height * 0.16)) : 0;
        var metricSize = Math.Min(46, Math.Max(28, options.Size.Height * 0.16));
        WriteText(writer, card.Label, content.X, y + theme.SubtitleFontSize, content.Width * 0.58, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "600");
        WriteText(writer, card.Value, content.X + content.Width * 0.58, y + metricSize * 0.82, content.Width * 0.42, VisualTextAlignment.Right, theme.Text, theme.FontFamily, metricSize, "850");
        y += metricSize + 20;
        var stripHeight = Math.Max(20, Math.Min(34, options.Size.Height * 0.09));
        RenderCompositionStrip(writer, card, content.X, y, content.Width, stripHeight);
        y += stripHeight + 24;
        var legendBottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var rowHeight = Math.Max(28, Math.Min(38, (legendBottom - y) / Math.Max(1, card.Segments.Count)));
        var total = VisualBlockRendering.CompositionTotal(card);
        writer.StartElement("g").Attribute("data-cfx-role", "composition-status-card").EndStartElement().Line();
        for (var i = 0; i < card.Segments.Count && y + rowHeight <= legendBottom + 1; i++) {
            var segment = card.Segments[i];
            var color = segment.Color ?? VisualBlockRendering.StatusColor(theme, segment.Status);
            writer.StartElement("rect").Attribute("data-cfx-role", "composition-legend-swatch").Attribute("x", content.X).Attribute("y", y + rowHeight * 0.22).Attribute("width", 14).Attribute("height", 14).Attribute("rx", 4).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, segment.Label, content.X + 24, y + rowHeight * 0.66, content.Width * 0.58, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(12, theme.SubtitleFontSize), "500");
            var valueText = segment.Value.ToString("0.##", CultureInfo.InvariantCulture) + (card.Unit.Length > 0 ? " " + card.Unit : string.Empty);
            var share = total <= 0 ? string.Empty : "  " + (segment.Value / total * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
            WriteText(writer, valueText + share, content.X + content.Width * 0.66, y + rowHeight * 0.66, content.Width * 0.34, VisualTextAlignment.Right, theme.Text, theme.FontFamily, Math.Max(12, theme.SubtitleFontSize), "750");
            y += rowHeight;
        }

        writer.EndElement().Line();
        if (hasAction) RenderFooterAction(writer, card.ActionLabel, card.ActionSymbol, card.ActionUrl, options.Size.Height - footerHeight, footerHeight, content.X, content.Width, theme);
    }

    private static void RenderCompositionStrip(SvgMarkupWriter writer, CompositionStatusCard card, double x, double y, double width, double height) {
        var theme = card.Options.Theme;
        var total = VisualBlockRendering.CompositionTotal(card);
        var gap = VisualBlockRendering.EffectiveStackGap(card.Segments.Count, width, 4);
        var segmentArea = Math.Max(0, width - gap * Math.Max(0, card.Segments.Count - 1));
        var cursor = x;
        writer.StartElement("g").Attribute("data-cfx-role", "composition-strip").Attribute("data-cfx-total", total).EndStartElement().Line();
        for (var i = 0; i < card.Segments.Count; i++) {
            var segment = card.Segments[i];
            var segmentWidth = i == card.Segments.Count - 1 ? x + width - cursor : Math.Max(0, segmentArea * segment.Value / total);
            if (segmentWidth <= 0) continue;
            var color = segment.Color ?? VisualBlockRendering.StatusColor(theme, segment.Status);
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "composition-strip-segment")
                .Attribute("data-cfx-label", segment.Label)
                .Attribute("data-cfx-value", segment.Value)
                .Attribute("data-cfx-pattern", segment.Pattern.ToString())
                .Attribute("x", cursor)
                .Attribute("y", y)
                .Attribute("width", segmentWidth)
                .Attribute("height", height)
                .Attribute("rx", Math.Min(7, height / 2))
                .Attribute("fill", color.ToCss())
                .EndEmptyElement().Line();
            if (segment.Pattern != ChartFillPattern.None) {
                for (var lineX = cursor - height; lineX < cursor + segmentWidth; lineX += 10) {
                    writer.StartElement("line").Attribute("data-cfx-role", "composition-strip-pattern").Attribute("x1", lineX).Attribute("y1", y + height).Attribute("x2", lineX + height).Attribute("y2", y).Attribute("stroke", "#fff").Attribute("stroke-opacity", 0.18).Attribute("stroke-width", 2).EndEmptyElement().Line();
                }
            }

            cursor += segmentWidth + gap;
        }

        writer.EndElement().Line();
    }

    private static void RenderFooterAction(SvgMarkupWriter writer, string label, string symbol, string url, double footerY, double footerHeight, double x, double width, ChartForgeX.Themes.ChartTheme theme) {
        writer.StartElement("line").Attribute("data-cfx-role", "visual-action-divider").Attribute("x1", x).Attribute("y1", footerY).Attribute("x2", x + width).Attribute("y2", footerY).Attribute("stroke", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
        var fontSize = Math.Max(10, theme.SubtitleFontSize);
        var baseline = footerY + footerHeight * 0.64;
        if (url.Length > 0) writer.StartElement("a").Attribute("data-cfx-role", "visual-action-link").Attribute("href", url).Attribute("target", "_top").EndStartElement().Line();
        WriteText(writer, label, x, baseline, Math.Max(1, width - 38), VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, fontSize, "500");
        WriteText(writer, symbol, x + width - 28, baseline, 28, VisualTextAlignment.Right, theme.Text, theme.FontFamily, fontSize, "800");
        if (url.Length > 0) writer.EndElement().Line();
    }

    private static void RenderWorkloadList(SvgMarkupWriter writer, WorkloadListBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var hasAction = block.ActionLabel.Length > 0;
        var footerHeight = hasAction ? Math.Min(42, Math.Max(32, options.Size.Height * 0.14)) : 0;
        var bottom = options.Size.Height - options.Padding.Bottom - footerHeight;
        var rowHeight = Math.Max(44, Math.Min(64, (bottom - y) / Math.Max(1, block.Rows.Count)));
        writer.StartElement("g").Attribute("data-cfx-role", "workload-list-block").EndStartElement().Line();
        for (var i = 0; i < block.Rows.Count && y + rowHeight <= bottom + 1; i++) {
            var row = block.Rows[i];
            var color = row.Color ?? (row.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, row.Status));
            var avatarSize = Math.Min(34, rowHeight - 14);
            var avatarX = content.X;
            var centerY = y + rowHeight * 0.42;
            writer.StartElement("g")
                .Attribute("data-cfx-role", "workload-row")
                .Attribute("data-cfx-label", row.Label)
                .Attribute("data-cfx-value", row.Value)
                .Attribute("data-cfx-maximum", row.Maximum)
                .Attribute("data-cfx-ratio", VisualBlockRendering.WorkloadRatio(row))
                .EndStartElement().Line();
            writer.StartElement("circle").Attribute("data-cfx-role", "workload-avatar").Attribute("cx", avatarX + avatarSize / 2).Attribute("cy", centerY).Attribute("r", avatarSize / 2).Attribute("fill", color.WithAlpha(42).ToCss()).Attribute("stroke", color.WithAlpha(105).ToCss()).EndEmptyElement().Line();
            WriteText(writer, WorkloadAvatar(row), avatarX + 1, centerY + 4, avatarSize - 2, VisualTextAlignment.Center, color, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 1), "800");
            var checkWidth = block.ShowSelectionControls ? 28 : 0;
            var valueWidth = Math.Min(98, Math.Max(52, VisualBlockRendering.EstimateTextWidth(VisualBlockRendering.WorkloadDisplayValue(row), theme.SubtitleFontSize) + 10));
            var textX = avatarX + avatarSize + 14;
            var controlX = content.X + content.Width - checkWidth + 4;
            var textWidth = Math.Max(24, content.X + content.Width - textX - valueWidth - checkWidth - 10);
            WriteText(writer, row.Label, textX, y + 15, textWidth, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(12, theme.SubtitleFontSize + 1), "750");
            if (row.Subtitle.Length > 0) WriteText(writer, row.Subtitle, textX, y + 31, textWidth, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "500");
            var valueText = row.Note.Length > 0 ? row.Note + "  " + VisualBlockRendering.WorkloadDisplayValue(row) : VisualBlockRendering.WorkloadDisplayValue(row);
            WriteText(writer, valueText, content.X + content.Width - valueWidth - checkWidth, y + 19, valueWidth, VisualTextAlignment.Right, row.Status == VisualStatus.Negative || row.Status == VisualStatus.Warning ? color : theme.Text, theme.FontFamily, theme.SubtitleFontSize, "750");
            if (block.ShowProgressRails) {
                var railY = y + rowHeight - 13;
                var railX = textX;
                var railWidth = Math.Max(18, content.X + content.Width - railX - checkWidth);
                writer.StartElement("rect").Attribute("data-cfx-role", "workload-progress-rail").Attribute("x", railX).Attribute("y", railY).Attribute("width", railWidth).Attribute("height", 6).Attribute("rx", 3).Attribute("fill", theme.MutedText.WithAlpha(28).ToCss()).EndEmptyElement().Line();
                writer.StartElement("rect").Attribute("data-cfx-role", "workload-progress-fill").Attribute("x", railX).Attribute("y", railY).Attribute("width", Math.Max(0, railWidth * VisualBlockRendering.WorkloadRatio(row))).Attribute("height", 6).Attribute("rx", 3).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            }

            if (block.ShowSelectionControls) {
                writer.StartElement("rect").Attribute("data-cfx-role", "workload-selection-control").Attribute("x", controlX).Attribute("y", centerY - 8).Attribute("width", 16).Attribute("height", 16).Attribute("rx", 4).Attribute("fill", row.Selected ? color.WithAlpha(46).ToCss() : "none").Attribute("stroke", row.Selected ? color.ToCss() : theme.PlotBorder.ToCss()).EndEmptyElement().Line();
                if (row.Selected) writer.StartElement("polyline").Attribute("data-cfx-role", "workload-selection-check").Attribute("points", FormatPoint(controlX + 4, centerY) + " " + FormatPoint(controlX + 7, centerY + 4) + " " + FormatPoint(controlX + 13, centerY - 5)).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", 1.8).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
            }

            if (block.ShowDividers && i < block.Rows.Count - 1) writer.StartElement("line").Attribute("data-cfx-role", "workload-row-divider").Attribute("x1", textX).Attribute("y1", y + rowHeight - 1).Attribute("x2", content.X + content.Width).Attribute("y2", y + rowHeight - 1).Attribute("stroke", theme.PlotBorder.WithAlpha(120).ToCss()).EndEmptyElement().Line();
            writer.EndElement().Line();
            y += rowHeight;
        }

        writer.EndElement().Line();
        if (hasAction) RenderFooterAction(writer, block.ActionLabel, block.ActionSymbol, block.ActionUrl, options.Size.Height - footerHeight, footerHeight, content.X, content.Width, theme);
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

    private static string WorkloadAvatar(WorkloadListRow row) {
        if (row.AvatarText.Length > 0) return row.AvatarText;
        var parts = row.Label.Split(new[] { ' ', '\t', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return string.Empty;
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
    }

    private static void WriteText(SvgMarkupWriter writer, string text, double x, double y, double width, VisualTextAlignment alignment, ChartColor color, string fontFamily, double fontSize, string weight) {
        var fitted = VisualBlockRendering.FitText(text, fontSize, Math.Max(1, width));
        var anchor = "start";
        var textX = x;
        if (alignment == VisualTextAlignment.Center) { anchor = "middle"; textX = x + width / 2; }
        else if (alignment == VisualTextAlignment.Right) { anchor = "end"; textX = x + width; }
        writer.StartElement("text").Attribute("data-cfx-role", "visual-text").Attribute("x", textX).Attribute("y", y).Attribute("text-anchor", anchor).Attribute("fill", color.ToCss()).Attribute("font-family", fontFamily).Attribute("font-size", fontSize).Attribute("font-weight", weight).Text(fitted).EndElement().Line();
    }

    private static string SparklinePoints(IReadOnlyList<ChartPoint> points) {
        var values = new string[points.Count];
        for (var i = 0; i < points.Count; i++) values[i] = FormatPoint(points[i].X, points[i].Y);
        return string.Join(" ", values);
    }

    private static string SparklineSmoothPath(IReadOnlyList<ChartPoint> points, double yOffset) {
        if (points.Count == 0) return string.Empty;
        var path = new SvgPathDataBuilder().MoveTo(points[0].X, points[0].Y + yOffset);
        if (points.Count < 3) {
            for (var i = 1; i < points.Count; i++) path.LineTo(points[i].X, points[i].Y + yOffset);
            return path.Build();
        }

        for (var i = 0; i < points.Count - 1; i++) {
            var p0 = points[Math.Max(0, i - 1)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(points.Count - 1, i + 2)];
            path.CubicTo(
                p1.X + (p2.X - p0.X) / 6,
                p1.Y + yOffset + (p2.Y - p0.Y) / 6,
                p2.X - (p3.X - p1.X) / 6,
                p2.Y + yOffset - (p3.Y - p1.Y) / 6,
                p2.X,
                p2.Y + yOffset);
        }

        return path.Build();
    }

    private static string FormatPoint(double x, double y) => x.ToString("0.###", CultureInfo.InvariantCulture) + "," + y.ToString("0.###", CultureInfo.InvariantCulture);

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
