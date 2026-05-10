using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderActivityTimeline(SvgMarkupWriter writer, ActivityTimelineBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, block, ref y, content.X, content.Width);
        var bottom = options.Size.Height - options.Padding.Bottom;
        var spineX = content.X + 18;
        var spineEnd = ActivitySpineEnd(block, y, bottom);
        writer.StartElement("g")
            .Attribute("data-cfx-role", "activity-timeline-block")
            .Attribute("data-cfx-event-surfaces", block.ShowEventSurfaces ? "true" : "false")
            .EndStartElement().Line();
        writer.StartElement("line").Attribute("data-cfx-role", "activity-spine").Attribute("x1", spineX).Attribute("y1", y + 6).Attribute("x2", spineX).Attribute("y2", spineEnd).Attribute("stroke", theme.PlotBorder.ToCss()).Attribute("stroke-width", 2).EndEmptyElement().Line();
        for (var i = 0; i < block.Items.Count && y < bottom - 12; i++) {
            var item = block.Items[i];
            var rowHeight = ActivityRowHeight(item);
            if (y + rowHeight > bottom + 1) break;
            var color = item.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, item.Status);
            if (item.Kind == ActivityTimelineItemKind.Section) RenderActivitySection(writer, block, item, y, spineX, content.X, content.Width);
            else if (item.Kind == ActivityTimelineItemKind.ChecklistItem) RenderActivityChecklistItem(writer, block, item, y, spineX, content.X, content.Width, color);
            else if (item.Kind == ActivityTimelineItemKind.HiddenSummary) RenderActivityHiddenSummary(writer, block, item, y, spineX, content.X, content.Width, color);
            else RenderActivityEvent(writer, block, item, y, rowHeight, spineX, content.X, content.Width, color);
            y += rowHeight;
        }

        writer.EndElement().Line();
    }

    private static void RenderActivitySection(SvgMarkupWriter writer, ActivityTimelineBlock block, ActivityTimelineItem item, double y, double spineX, double x, double width) {
        var theme = block.Options.Theme;
        WriteText(writer, item.Title.ToUpperInvariant(), x + 40, y + 14, width - 40, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, Math.Max(10, theme.SubtitleFontSize - 1), "800");
        writer.StartElement("circle").Attribute("data-cfx-role", "activity-section-node").Attribute("cx", spineX).Attribute("cy", y + 10).Attribute("r", 4).Attribute("fill", theme.PlotBorder.ToCss()).EndEmptyElement().Line();
    }

    private static void RenderActivityChecklistItem(SvgMarkupWriter writer, ActivityTimelineBlock block, ActivityTimelineItem item, double y, double spineX, double x, double width, ChartColor color) {
        var theme = block.Options.Theme;
        writer.StartElement("circle").Attribute("data-cfx-role", "activity-check-node").Attribute("cx", spineX + 24).Attribute("cy", y + 11).Attribute("r", 4).Attribute("fill", color.ToCss()).EndEmptyElement().Line();
        if (item.Completed) writer.StartElement("polyline").Attribute("data-cfx-role", "activity-checkmark").Attribute("points", FormatPoint(spineX + 20, y + 11) + " " + FormatPoint(spineX + 23, y + 15) + " " + FormatPoint(spineX + 29, y + 7)).Attribute("fill", "none").Attribute("stroke", color.ToCss()).Attribute("stroke-width", 1.5).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
        var textColor = item.Muted ? theme.MutedText.WithAlpha(150) : theme.Text;
        WriteText(writer, item.Title, x + 58, y + 15, width - 58, VisualTextAlignment.Left, textColor, theme.FontFamily, theme.SubtitleFontSize, item.Completed ? "500" : "650");
        if (item.Completed) writer.StartElement("line").Attribute("data-cfx-role", "activity-check-strike").Attribute("x1", x + 58).Attribute("y1", y + 10).Attribute("x2", x + Math.Min(width, 58 + VisualBlockRendering.EstimateTextWidth(item.Title, theme.SubtitleFontSize))).Attribute("y2", y + 10).Attribute("stroke", textColor.ToCss()).Attribute("stroke-opacity", 0.55).EndEmptyElement().Line();
    }

    private static void RenderActivityHiddenSummary(SvgMarkupWriter writer, ActivityTimelineBlock block, ActivityTimelineItem item, double y, double spineX, double x, double width, ChartColor color) {
        var theme = block.Options.Theme;
        writer.StartElement("circle").Attribute("data-cfx-role", "activity-hidden-node").Attribute("cx", spineX).Attribute("cy", y + 13).Attribute("r", 8).Attribute("fill", color.WithAlpha(42).ToCss()).Attribute("stroke", color.ToCss()).EndEmptyElement().Line();
        WriteText(writer, item.HiddenCount.ToString(CultureInfo.InvariantCulture) + " " + item.Title, x + 40, y + 17, width - 40, VisualTextAlignment.Left, color, theme.FontFamily, Math.Max(12, theme.SubtitleFontSize), "750");
    }

    private static void RenderActivityEvent(SvgMarkupWriter writer, ActivityTimelineBlock block, ActivityTimelineItem item, double y, double rowHeight, double spineX, double x, double width, ChartColor color) {
        var theme = block.Options.Theme;
        if (block.ShowEventSurfaces) {
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "activity-event-surface")
                .Attribute("class", ChartVisualPrimitives.SvgGuideStrokeClass)
                .Attribute("x", x + 32)
                .Attribute("y", y - 6)
                .Attribute("width", Math.Max(1, width - 32))
                .Attribute("height", Math.Max(26, rowHeight - 8))
                .Attribute("rx", 10)
                .Attribute("fill", theme.PlotBackground.WithAlpha(130).ToCss())
                .Attribute("stroke", theme.PlotBorder.WithAlpha(150).ToCss())
                .EndEmptyElement().Line();
        }

        var nodeRadius = item.Symbol.Length > 0 ? 11 : 10;
        var nodeFill = item.Symbol.Length > 0 ? color : color.WithAlpha(52);
        writer.StartElement("circle").Attribute("data-cfx-role", "activity-event-node").Attribute("cx", spineX).Attribute("cy", y + 15).Attribute("r", nodeRadius).Attribute("fill", nodeFill.ToCss()).Attribute("stroke", color.ToCss()).Attribute("stroke-width", item.Symbol.Length > 0 ? 0 : 2).EndEmptyElement().Line();
        if (item.Symbol.Length > 0) WriteText(writer, item.Symbol, spineX - nodeRadius, y + 19, nodeRadius * 2, VisualTextAlignment.Center, ChartColor.White, theme.FontFamily, Math.Max(8, theme.SubtitleFontSize - 3), "850");

        var trailingWidth = ActivityTrailingWidth(item, theme.SubtitleFontSize);
        WriteText(writer, item.Title, x + 40, y + 15, width - 56 - trailingWidth, VisualTextAlignment.Left, theme.Text, theme.FontFamily, Math.Max(12, theme.SubtitleFontSize + 1), "800");
        if (item.Badge.Length > 0) {
            var badgeWidth = Math.Min(116, VisualBlockRendering.EstimateTextWidth(item.Badge, theme.SubtitleFontSize) + 18);
            writer.StartElement("rect").Attribute("data-cfx-role", "activity-badge").Attribute("x", x + width - badgeWidth).Attribute("y", y).Attribute("width", badgeWidth).Attribute("height", 22).Attribute("rx", 8).Attribute("fill", color.WithAlpha(38).ToCss()).EndEmptyElement().Line();
            WriteText(writer, item.Badge, x + width - badgeWidth + 7, y + 15, badgeWidth - 14, VisualTextAlignment.Center, color, theme.FontFamily, theme.SubtitleFontSize, "750");
        } else if (item.Timestamp.Length > 0) {
            var timestampWidth = ActivityTimestampWidth(item.Timestamp, theme.SubtitleFontSize);
            WriteText(writer, item.Timestamp, x + width - timestampWidth, y + 15, timestampWidth, VisualTextAlignment.Right, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "500");
        }

        if (item.Detail.Length > 0) WriteText(writer, item.Detail, x + 40, y + 34, width - 40, VisualTextAlignment.Left, theme.MutedText, theme.FontFamily, theme.SubtitleFontSize, "500");
    }

    private static double ActivityTrailingWidth(ActivityTimelineItem item, double fontSize) {
        if (item.Badge.Length > 0) return Math.Min(116, VisualBlockRendering.EstimateTextWidth(item.Badge, fontSize) + 26);
        if (item.Timestamp.Length > 0) return ActivityTimestampWidth(item.Timestamp, fontSize) + 10;
        return 0;
    }

    private static double ActivityTimestampWidth(string timestamp, double fontSize) => Math.Min(190, Math.Max(110, VisualBlockRendering.EstimateTextWidth(timestamp, fontSize) + 8));

    private static double ActivitySpineEnd(ActivityTimelineBlock block, double y, double bottom) {
        var cursor = y;
        var end = y + 6;
        for (var i = 0; i < block.Items.Count && cursor < bottom - 12; i++) {
            var item = block.Items[i];
            var rowHeight = ActivityRowHeight(item);
            if (cursor + rowHeight > bottom + 1) break;
            end = item.Kind == ActivityTimelineItemKind.Section ? cursor + 10 : item.Kind == ActivityTimelineItemKind.ChecklistItem ? cursor + 11 : item.Kind == ActivityTimelineItemKind.HiddenSummary ? cursor + 13 : cursor + 15;
            cursor += rowHeight;
        }

        return Math.Min(bottom, Math.Max(y + 6, end + 22));
    }

    private static double ActivityRowHeight(ActivityTimelineItem item) {
        if (item.Kind == ActivityTimelineItemKind.Section) return 30;
        if (item.Kind == ActivityTimelineItemKind.ChecklistItem) return 28;
        if (item.Kind == ActivityTimelineItemKind.HiddenSummary) return 36;
        return item.Detail.Length > 0 ? 54 : 38;
    }
}
