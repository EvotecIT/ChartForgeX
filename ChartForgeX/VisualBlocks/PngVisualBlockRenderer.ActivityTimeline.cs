using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawActivityTimeline(RgbaCanvas canvas, ActivityTimelineBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        var bottom = options.Size.Height - options.Padding.Bottom;
        var spineX = content.X + 18;
        var spineEnd = ActivitySpineEnd(block, y, bottom);
        canvas.DrawLine(spineX, y + 6, spineX, spineEnd, theme.PlotBorder, 2);
        for (var i = 0; i < block.Items.Count && y < bottom - 12; i++) {
            var item = block.Items[i];
            var rowHeight = ActivityRowHeight(item);
            if (y + rowHeight > bottom + 1) break;
            var color = item.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, item.Status);
            if (item.Kind == ActivityTimelineItemKind.Section) DrawActivitySection(canvas, block, item, y, spineX, content.X, content.Width);
            else if (item.Kind == ActivityTimelineItemKind.ChecklistItem) DrawActivityChecklistItem(canvas, block, item, y, spineX, content.X, content.Width, color);
            else if (item.Kind == ActivityTimelineItemKind.HiddenSummary) DrawActivityHiddenSummary(canvas, block, item, y, spineX, content.X, content.Width, color);
            else DrawActivityEvent(canvas, block, item, y, rowHeight, spineX, content.X, content.Width, color);
            y += rowHeight;
        }
    }

    private static void DrawActivitySection(RgbaCanvas canvas, ActivityTimelineBlock block, ActivityTimelineItem item, double y, double spineX, double x, double width) {
        var theme = block.Options.Theme;
        canvas.DrawCircle(spineX, y + 10, 4, theme.PlotBorder);
        DrawAlignedText(canvas, item.Title.ToUpperInvariant(), x + 40, y + 4, width - 40, VisualTextAlignment.Left, theme.MutedText, Math.Max(10, theme.SubtitleFontSize - 1), true);
    }

    private static void DrawActivityChecklistItem(RgbaCanvas canvas, ActivityTimelineBlock block, ActivityTimelineItem item, double y, double spineX, double x, double width, ChartColor color) {
        var theme = block.Options.Theme;
        canvas.DrawCircle(spineX + 24, y + 11, 4, color);
        if (item.Completed) {
            canvas.DrawLine(spineX + 20, y + 11, spineX + 23, y + 15, color, 1.4);
            canvas.DrawLine(spineX + 23, y + 15, spineX + 29, y + 7, color, 1.4);
        }

        var textColor = item.Muted ? theme.MutedText.WithAlpha(150) : theme.Text;
        DrawAlignedText(canvas, item.Title, x + 58, y + 5, width - 58, VisualTextAlignment.Left, textColor, theme.SubtitleFontSize, item.Completed ? false : true);
        if (item.Completed) canvas.DrawLine(x + 58, y + 10, x + Math.Min(width, 58 + RgbaCanvas.MeasureTextWidth(FitText(item.Title, theme.SubtitleFontSize, width - 58), theme.SubtitleFontSize, null)), y + 10, textColor.WithAlpha(140), 1);
    }

    private static void DrawActivityHiddenSummary(RgbaCanvas canvas, ActivityTimelineBlock block, ActivityTimelineItem item, double y, double spineX, double x, double width, ChartColor color) {
        var theme = block.Options.Theme;
        canvas.DrawCircle(spineX, y + 13, 8, color.WithAlpha(42));
        canvas.DrawCircleOutline(spineX, y + 13, 8, color, 1);
        DrawAlignedText(canvas, item.HiddenCount.ToString(CultureInfo.InvariantCulture) + " " + item.Title, x + 40, y + 6, width - 40, VisualTextAlignment.Left, color, Math.Max(12, theme.SubtitleFontSize), true);
    }

    private static void DrawActivityEvent(RgbaCanvas canvas, ActivityTimelineBlock block, ActivityTimelineItem item, double y, double rowHeight, double spineX, double x, double width, ChartColor color) {
        var theme = block.Options.Theme;
        if (block.ShowEventSurfaces) {
            canvas.FillRoundedRect(x + 32, y - 6, Math.Max(1, width - 32), Math.Max(26, rowHeight - 8), 10, theme.PlotBackground.WithAlpha(130));
            canvas.StrokeRoundedRect(x + 32, y - 6, Math.Max(1, width - 32), Math.Max(26, rowHeight - 8), 10, theme.PlotBorder.WithAlpha(150), 1);
        }

        var nodeRadius = item.Symbol.Length > 0 ? 11 : 10;
        if (item.Symbol.Length > 0) {
            canvas.DrawCircle(spineX, y + 15, nodeRadius, color);
            DrawAlignedText(canvas, item.Symbol, spineX - nodeRadius, y + 9, nodeRadius * 2, VisualTextAlignment.Center, ChartColor.White, Math.Max(8, theme.SubtitleFontSize - 3), true);
        } else {
            canvas.DrawCircle(spineX, y + 15, 10, color.WithAlpha(52));
            canvas.DrawCircleOutline(spineX, y + 15, 10, color, 2);
        }

        var trailingWidth = ActivityTrailingWidth(item, theme.SubtitleFontSize);
        DrawAlignedText(canvas, item.Title, x + 40, y + 5, width - 56 - trailingWidth, VisualTextAlignment.Left, theme.Text, Math.Max(12, theme.SubtitleFontSize + 1), true);
        if (item.Badge.Length > 0) {
            var badgeWidth = Math.Min(116, RgbaCanvas.MeasureTextEmphasizedWidth(item.Badge, theme.SubtitleFontSize, null) + 18);
            canvas.FillRoundedRect(x + width - badgeWidth, y, badgeWidth, 22, 8, color.WithAlpha(38));
            DrawAlignedText(canvas, item.Badge, x + width - badgeWidth + 7, y + 5, badgeWidth - 14, VisualTextAlignment.Center, color, theme.SubtitleFontSize, true);
        } else if (item.Timestamp.Length > 0) {
            var timestampWidth = ActivityTimestampWidth(item.Timestamp, theme.SubtitleFontSize);
            DrawAlignedText(canvas, item.Timestamp, x + width - timestampWidth, y + 5, timestampWidth, VisualTextAlignment.Right, theme.MutedText, theme.SubtitleFontSize, false);
        }

        if (item.Detail.Length > 0) DrawAlignedText(canvas, item.Detail, x + 40, y + 24, width - 40, VisualTextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, false);
    }

    private static double ActivityTrailingWidth(ActivityTimelineItem item, double fontSize) {
        if (item.Badge.Length > 0) return Math.Min(116, RgbaCanvas.MeasureTextEmphasizedWidth(item.Badge, fontSize, null) + 26);
        if (item.Timestamp.Length > 0) return ActivityTimestampWidth(item.Timestamp, fontSize) + 10;
        return 0;
    }

    private static double ActivityTimestampWidth(string timestamp, double fontSize) => Math.Min(190, Math.Max(110, RgbaCanvas.MeasureTextWidth(timestamp, fontSize, null) + 8));

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
