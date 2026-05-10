using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderScheduleTimeline(SvgMarkupWriter writer, ScheduleTimelineBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        RenderScheduleHeaderActions(writer, block, ref y, content.X, content.Width);
        var axisHeight = 36.0;
        var plotTop = y + axisHeight;
        var plotHeight = Math.Max(1, options.Size.Height - options.Padding.Bottom - plotTop);
        var laneCount = VisualBlockRendering.ScheduleLaneCount(block);
        var laneGap = 10.0;
        var laneHeight = Math.Max(26, (plotHeight - laneGap * Math.Max(0, laneCount - 1)) / laneCount);
        writer.StartElement("g").Attribute("data-cfx-role", "schedule-timeline-block").Attribute("data-cfx-start", block.Start).Attribute("data-cfx-end", block.End).EndStartElement().Line();
        foreach (var tick in VisualBlockRendering.ScheduleTicks(block)) {
            var x = content.X + content.Width * VisualBlockRendering.ScheduleRatio(block, tick);
            if (block.ShowGrid) writer.StartElement("line").Attribute("data-cfx-role", "schedule-grid-line").Attribute("x1", x).Attribute("y1", plotTop).Attribute("x2", x).Attribute("y2", plotTop + plotHeight).Attribute("stroke", theme.Grid.ToCss()).Attribute("stroke-opacity", 0.56).Attribute("stroke-dasharray", "4 5").EndEmptyElement().Line();
            WriteText(writer, VisualBlockRendering.FormatScheduleHour(tick), Math.Max(content.X, Math.Min(content.X + content.Width - 88, x - 44)), y + 18, 88, VisualTextAlignment.Center, theme.MutedText, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize), "600");
        }

        for (var lane = 0; lane < laneCount; lane++) {
            var laneY = plotTop + lane * (laneHeight + laneGap);
            writer.StartElement("line").Attribute("data-cfx-role", "schedule-lane-line").Attribute("x1", content.X).Attribute("y1", laneY + laneHeight / 2).Attribute("x2", content.X + content.Width).Attribute("y2", laneY + laneHeight / 2).Attribute("stroke", theme.PlotBorder.WithAlpha(70).ToCss()).EndEmptyElement().Line();
        }

        if (block.CurrentTime.HasValue && VisualBlockRendering.IsScheduleTimeInRange(block, block.CurrentTime.Value)) {
            var currentX = content.X + content.Width * VisualBlockRendering.ScheduleRatio(block, block.CurrentTime.Value);
            writer.StartElement("line").Attribute("data-cfx-role", "schedule-current-time").Attribute("x1", currentX).Attribute("y1", y + 24).Attribute("x2", currentX).Attribute("y2", plotTop + plotHeight).Attribute("stroke", theme.Warning.ToCss()).Attribute("stroke-width", 2).Attribute("stroke-dasharray", "6 5").EndEmptyElement().Line();
        }

        for (var i = 0; i < block.Events.Count; i++) {
            var item = block.Events[i];
            if (!VisualBlockRendering.ScheduleEventIntersects(block, item)) continue;
            var color = item.Color ?? (item.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, item.Status));
            var x1 = content.X + content.Width * VisualBlockRendering.ScheduleRatio(block, item.Start);
            var x2 = content.X + content.Width * VisualBlockRendering.ScheduleRatio(block, item.End);
            var clippedStart = item.Start < block.Start;
            var clippedEnd = item.End > block.End;
            var left = Math.Max(content.X, Math.Min(x1, x2));
            var right = Math.Min(content.X + content.Width, Math.Max(x1, x2));
            var width = Math.Max(10, right - left);
            var laneY = plotTop + item.Lane * (laneHeight + laneGap);
            var eventHeight = Math.Min(34, laneHeight);
            var eventY = laneY + Math.Max(2, (laneHeight - eventHeight) / 2);
            var radius = Math.Min(8, eventHeight / 2);
            writer.StartElement("g")
                .Attribute("data-cfx-role", "schedule-event")
                .Attribute("data-cfx-title", item.Title)
                .Attribute("data-cfx-start", item.Start)
                .Attribute("data-cfx-end", item.End)
                .Attribute("data-cfx-lane", item.Lane)
                .Attribute("data-cfx-clipped-start", clippedStart ? "true" : "false")
                .Attribute("data-cfx-clipped-end", clippedEnd ? "true" : "false")
                .EndStartElement().Line();
            writer.StartElement("rect").Attribute("data-cfx-role", "schedule-event-pill").Attribute("x", left).Attribute("y", eventY).Attribute("width", width).Attribute("height", eventHeight).Attribute("rx", radius).Attribute("fill", color.WithAlpha(34).ToCss()).Attribute("stroke", color.WithAlpha(130).ToCss()).EndEmptyElement().Line();
            writer.StartElement("rect").Attribute("data-cfx-role", "schedule-event-stripe").Attribute("x", left).Attribute("y", eventY).Attribute("width", Math.Min(5, width)).Attribute("height", eventHeight).Attribute("rx", Math.Min(3, radius)).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
            var avatarReserve = Math.Min(width * 0.34, item.Avatars.Count == 0 ? 0 : 18 + Math.Min(3, item.Avatars.Count) * 14);
            var badgeReserve = item.Badge.Length == 0 ? 0 : Math.Min(78, VisualBlockRendering.EstimateTextWidth(item.Badge, theme.SubtitleFontSize) + 18);
            WriteText(writer, item.Title, left + 14, eventY + eventHeight * 0.62, Math.Max(1, width - 20 - avatarReserve - badgeReserve), VisualTextAlignment.Left, color, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize), "750");
            if (item.Badge.Length > 0) {
                var badgeWidth = Math.Min(78, badgeReserve);
                writer.StartElement("rect").Attribute("data-cfx-role", "schedule-event-badge").Attribute("x", left + width - avatarReserve - badgeWidth - 6).Attribute("y", eventY + 7).Attribute("width", badgeWidth).Attribute("height", eventHeight - 14).Attribute("rx", 7).Attribute("fill", color.WithAlpha(50).ToCss()).EndEmptyElement().Line();
                WriteText(writer, item.Badge, left + width - avatarReserve - badgeWidth, eventY + eventHeight * 0.62, badgeWidth - 10, VisualTextAlignment.Center, color, theme.FontFamily, Math.Max(9, theme.SubtitleFontSize - 1), "800");
            }

            RenderScheduleAvatars(writer, item, color, theme, left + width - avatarReserve + 3, eventY + eventHeight / 2);
            writer.EndElement().Line();
        }

        writer.EndElement().Line();
    }

    private static void RenderScheduleHeaderActions(SvgMarkupWriter writer, ScheduleTimelineBlock block, ref double y, double x, double width) {
        if (block.HeaderActions.Count == 0) return;
        var theme = block.Options.Theme;
        var cursor = x + width;
        for (var i = block.HeaderActions.Count - 1; i >= 0; i--) {
            var action = block.HeaderActions[i];
            var actionWidth = Math.Min(140, Math.Max(62, VisualBlockRendering.EstimateTextWidth(action, theme.SubtitleFontSize) + 28));
            actionWidth = Math.Min(actionWidth, Math.Max(0, cursor - x));
            if (actionWidth < 36) break;
            cursor -= actionWidth;
            writer.StartElement("rect").Attribute("data-cfx-role", "schedule-header-action").Attribute("x", cursor).Attribute("y", y).Attribute("width", actionWidth).Attribute("height", 30).Attribute("rx", 8).Attribute("fill", ChartColor.White.ToCss()).Attribute("stroke", theme.CardBorder.ToCss()).EndEmptyElement().Line();
            WriteText(writer, action, cursor + 12, y + 20, actionWidth - 24, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize), "650");
            cursor -= 8;
        }

        y += 42;
    }

    private static void RenderScheduleAvatars(SvgMarkupWriter writer, ScheduleTimelineEvent item, ChartColor color, ChartForgeX.Themes.ChartTheme theme, double x, double y) {
        var count = Math.Min(3, item.Avatars.Count);
        for (var i = 0; i < count; i++) {
            var cx = x + i * 14;
            writer.StartElement("circle").Attribute("data-cfx-role", "schedule-avatar").Attribute("cx", cx).Attribute("cy", y).Attribute("r", 8).Attribute("fill", theme.CardBackground.ToCss()).Attribute("stroke", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, item.Avatars[i], cx - 7, y + 3, 14, VisualTextAlignment.Center, color, theme.FontFamily, 8, "800");
        }

        if (item.Avatars.Count > count) {
            var cx = x + count * 14;
            writer.StartElement("circle").Attribute("data-cfx-role", "schedule-avatar-more").Attribute("cx", cx).Attribute("cy", y).Attribute("r", 8).Attribute("fill", color.WithAlpha(45).ToCss()).Attribute("stroke", color.ToCss()).EndEmptyElement().Line();
            WriteText(writer, "+" + (item.Avatars.Count - count).ToString(CultureInfo.InvariantCulture), cx - 7, y + 3, 14, VisualTextAlignment.Center, color, theme.FontFamily, 8, "800");
        }
    }
}
