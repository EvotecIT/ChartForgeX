using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

using static ChartForgeX.VisualBlocks.PngVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngFactBlockRenderer {
    private static void DrawDateStrip(RgbaCanvas canvas, DateStripBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        if (block.Header.Length > 0) {
            if (block.Header.Length > 0) {
                canvas.FillRoundedRect(content.X, y + 3, 22, 22, 6, theme.Text.WithAlpha(24));
                canvas.StrokeRoundedRect(content.X, y + 3, 22, 22, 6, theme.Text.WithAlpha(85));
                canvas.DrawLine(content.X + 5, y + 9, content.X + 17, y + 9, theme.Text, 1.4, RasterLineCap.Butt);
                canvas.DrawCircle(content.X + 8, y + 15, 1.5, theme.Text);
                canvas.DrawCircle(content.X + 14, y + 15, 1.5, theme.Text);
                DrawAlignedText(canvas, block.Header, content.X + 32, y + 7, Math.Max(1, content.Width - 32), TextAlignment.Left, theme.Text, Math.Max(13, theme.SubtitleFontSize + 1), true);
            }


            y += 36;
        }

        var stripHeight = Math.Max(50, options.Size.Height - options.Padding.Bottom - y);
        canvas.FillRoundedRect(content.X, y, content.Width, stripHeight, Math.Min(18, Math.Max(8, theme.PlotCornerRadius + 6)), theme.PlotBackground.WithAlpha(150));
        canvas.StrokeRoundedRect(content.X, y, content.Width, stripHeight, Math.Min(18, Math.Max(8, theme.PlotCornerRadius + 6)), theme.PlotBorder.WithAlpha(130), 1);
        var innerX = content.X + 12;
        var innerY = y + 10;
        var innerWidth = Math.Max(1, content.Width - 24);
        var innerHeight = Math.Max(1, stripHeight - 20);
        var cellWidth = innerWidth / block.Items.Count;
        var pillWidth = Math.Min(54, Math.Max(42, cellWidth * 0.68));
        for (var i = 0; i < block.Items.Count; i++) {
            var item = block.Items[i];
            var cellX = innerX + i * cellWidth;
            var x = cellX + (cellWidth - pillWidth) / 2;
            var accent = item.Color ?? VisualBlockRendering.PaletteAt(theme, 0);
            var textColor = theme.Text;
            var valueTextColor = item.Selected ? ChartColorMath.TextOnBackground(accent) : theme.Text;
            var itemRadius = Math.Min(24, pillWidth * 0.48);
            canvas.FillRoundedRect(x, innerY, pillWidth, innerHeight, itemRadius, item.Selected ? theme.CardBackground.WithAlpha(230) : theme.CardBackground.WithAlpha(160));
            canvas.StrokeRoundedRect(x, innerY, pillWidth, innerHeight, itemRadius, item.Selected ? theme.CardBorder.WithAlpha(95) : theme.CardBorder.WithAlpha(70), 1);
            DrawAlignedText(canvas, item.Label, x, innerY + 7, pillWidth, TextAlignment.Center, item.Selected ? textColor : theme.MutedText, Math.Max(10, theme.SubtitleFontSize - 1), true);
            canvas.DrawCircle(x + pillWidth / 2, innerY + innerHeight - 18, 17, item.Selected ? accent : theme.Background.WithAlpha(170));
            canvas.DrawCircleOutline(x + pillWidth / 2, innerY + innerHeight - 18, 17, item.Selected ? ChartColor.White.WithAlpha(130) : theme.CardBorder.WithAlpha(70), 1);
            DrawAlignedText(canvas, item.Value, x, innerY + innerHeight - 24, pillWidth, TextAlignment.Center, valueTextColor, Math.Max(11, theme.SubtitleFontSize), true);
        }
    }


    private static void DrawEntityStrip(RgbaCanvas canvas, EntityStripBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        if (block.Title.Length > 0) {
            var titleSize = Math.Max(14, Math.Min(theme.TitleFontSize, theme.SubtitleFontSize + 8));
            if (block.Title.Length > 0) DrawAlignedText(canvas, block.Title, content.X, y, Math.Max(1, content.Width), TextAlignment.Left, theme.Text, titleSize, true);
            y += titleSize + 14;
        }

        if (block.Subtitle.Length > 0) {
            DrawAlignedText(canvas, block.Subtitle, content.X, y, content.Width, TextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, false);
            y += theme.SubtitleFontSize + 12;
        }

        var stripHeight = Math.Max(56, options.Size.Height - options.Padding.Bottom - y);
        canvas.FillRoundedRect(content.X, y, content.Width, stripHeight, Math.Min(18, Math.Max(8, theme.PlotCornerRadius + 6)), theme.PlotBackground.WithAlpha(150));
        canvas.StrokeRoundedRect(content.X, y, content.Width, stripHeight, Math.Min(18, Math.Max(8, theme.PlotCornerRadius + 6)), theme.PlotBorder.WithAlpha(130), 1);
        var cellWidth = Math.Max(1, (content.Width - 20) / block.Items.Count);
        var avatarRadius = Math.Min(22, Math.Max(16, cellWidth * 0.18));
        var startX = content.X + 10;
        for (var i = 0; i < block.Items.Count; i++) {
            var item = block.Items[i];
            var x = startX + i * cellWidth;
            var centerX = x + cellWidth / 2;
            var color = item.Color ?? (item.Status == VisualStatus.None ? VisualBlockRendering.PaletteAt(theme, i) : VisualBlockRendering.StatusColor(theme, item.Status));
            canvas.DrawCircle(centerX, y + 28, avatarRadius, color.WithAlpha(45));
            canvas.DrawCircleOutline(centerX, y + 28, avatarRadius, color.WithAlpha(115), 1);
            if (item.AvatarText.Length > 0) DrawAlignedText(canvas, item.AvatarText, centerX - avatarRadius, y + 28 - Math.Max(9, theme.SubtitleFontSize - 2) * 0.45, avatarRadius * 2, TextAlignment.Center, color, Math.Max(9, theme.SubtitleFontSize - 2), true);
            else DrawIcon(canvas, VisualIcon.Person, centerX, y + 28, avatarRadius * 0.56, color);
            DrawAlignedText(canvas, item.Label, x, y + stripHeight - 24, cellWidth, TextAlignment.Center, theme.Text, Math.Max(10, theme.SubtitleFontSize), false);
        }
    }

    private static void DrawSectionHeader(RgbaCanvas canvas, SectionHeaderBlock block) {
        var theme = block.Options.Theme;
        var content = VisualBlockRendering.ContentRect(block.Options);
        var titleSize = Math.Max(16, Math.Min(theme.TitleFontSize, theme.SubtitleFontSize + 10));
        var y = content.Y + (content.Height - titleSize) * 0.48;
        DrawAlignedText(canvas, block.Title, content.X, y, Math.Max(1, content.Width), TextAlignment.Left, theme.Text, titleSize, true);
    }
}
