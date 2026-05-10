using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawDateStrip(RgbaCanvas canvas, DateStripBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, block, ref y, content.X, content.Width);
        var navReserve = block.ShowNavigation ? 72.0 : 0;
        if (block.Header.Length > 0 || block.ShowNavigation) {
            if (block.Header.Length > 0) {
                canvas.FillRoundedRect(content.X, y + 3, 22, 22, 6, theme.Text.WithAlpha(24));
                canvas.StrokeRoundedRect(content.X, y + 3, 22, 22, 6, theme.Text.WithAlpha(85));
                canvas.DrawLine(content.X + 5, y + 9, content.X + 17, y + 9, theme.Text, 1.4);
                canvas.DrawCircle(content.X + 8, y + 15, 1.5, theme.Text);
                canvas.DrawCircle(content.X + 14, y + 15, 1.5, theme.Text);
                DrawAlignedText(canvas, block.Header, content.X + 32, y + 7, Math.Max(1, content.Width - 32 - navReserve), VisualTextAlignment.Left, theme.Text, Math.Max(13, theme.SubtitleFontSize + 1), true);
            }

            if (block.ShowNavigation) {
                DrawDateNavButton(canvas, block.PreviousSymbol, content.X + content.Width - 66, y + 2, 28, theme, muted: true);
                DrawDateNavButton(canvas, block.NextSymbol, content.X + content.Width - 30, y + 2, 28, theme, muted: false);
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
            DrawAlignedText(canvas, item.Label, x, innerY + 7, pillWidth, VisualTextAlignment.Center, item.Selected ? textColor : theme.MutedText, Math.Max(10, theme.SubtitleFontSize - 1), true);
            canvas.DrawCircle(x + pillWidth / 2, innerY + innerHeight - 18, 17, item.Selected ? accent : theme.Background.WithAlpha(170));
            canvas.DrawCircleOutline(x + pillWidth / 2, innerY + innerHeight - 18, 17, item.Selected ? ChartColor.White.WithAlpha(130) : theme.CardBorder.WithAlpha(70), 1);
            DrawAlignedText(canvas, item.Value, x, innerY + innerHeight - 24, pillWidth, VisualTextAlignment.Center, valueTextColor, Math.Max(11, theme.SubtitleFontSize), true);
        }
    }

    private static void DrawDateNavButton(RgbaCanvas canvas, string symbol, double x, double y, double size, ChartForgeX.Themes.ChartTheme theme, bool muted) {
        canvas.DrawCircle(x + size / 2, y + size / 2, size / 2, muted ? ChartColor.White.WithAlpha(150) : ChartColor.White);
        canvas.DrawCircleOutline(x + size / 2, y + size / 2, size / 2, theme.CardBorder.WithAlpha(90), 1);
        DrawAlignedText(canvas, symbol, x, y + 6, size, VisualTextAlignment.Center, muted ? theme.MutedText : theme.Text, 15, true);
    }

    private static void DrawEntityStrip(RgbaCanvas canvas, EntityStripBlock block) {
        var options = block.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        var actionWidth = block.ActionLabel.Length == 0 ? 0 : Math.Min(120, RgbaCanvas.MeasureTextEmphasizedWidth(block.ActionSymbol + " " + block.ActionLabel, theme.SubtitleFontSize + 1, null) + 12);
        if (block.Title.Length > 0 || block.ActionLabel.Length > 0) {
            var titleSize = Math.Max(14, Math.Min(theme.TitleFontSize, theme.SubtitleFontSize + 8));
            if (block.Title.Length > 0) DrawAlignedText(canvas, block.Title, content.X, y, Math.Max(1, content.Width - actionWidth - 10), VisualTextAlignment.Left, theme.Text, titleSize, true);
            if (block.ActionLabel.Length > 0) DrawAlignedText(canvas, block.ActionSymbol + " " + block.ActionLabel, content.X + content.Width - actionWidth, y + 1, actionWidth, VisualTextAlignment.Right, VisualBlockRendering.PaletteAt(theme, 0), Math.Max(12, theme.SubtitleFontSize + 1), true);
            y += titleSize + 14;
        }

        if (block.Subtitle.Length > 0) {
            DrawAlignedText(canvas, block.Subtitle, content.X, y, content.Width, VisualTextAlignment.Left, theme.MutedText, theme.SubtitleFontSize, false);
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
            if (item.AvatarText.Length > 0) DrawAlignedText(canvas, item.AvatarText, centerX - avatarRadius, y + 28 - Math.Max(9, theme.SubtitleFontSize - 2) * 0.45, avatarRadius * 2, VisualTextAlignment.Center, color, Math.Max(9, theme.SubtitleFontSize - 2), true);
            else DrawIcon(canvas, VisualIcon.Person, centerX, y + 28, avatarRadius * 0.56, color);
            DrawAlignedText(canvas, item.Label, x, y + stripHeight - 24, cellWidth, VisualTextAlignment.Center, theme.Text, Math.Max(10, theme.SubtitleFontSize), false);
        }
    }

    private static void DrawSectionHeader(RgbaCanvas canvas, SectionHeaderBlock block) {
        var theme = block.Options.Theme;
        var content = VisualBlockRendering.ContentRect(block.Options);
        var titleSize = Math.Max(16, Math.Min(theme.TitleFontSize, theme.SubtitleFontSize + 10));
        var actionText = block.ActionLabel.Length == 0 ? string.Empty : (block.ActionSymbol.Length == 0 ? block.ActionLabel : block.ActionSymbol + " " + block.ActionLabel);
        var actionWidth = actionText.Length == 0 ? 0 : Math.Min(150, RgbaCanvas.MeasureTextEmphasizedWidth(actionText, Math.Max(12, theme.SubtitleFontSize + 1), null) + 12);
        var y = content.Y + (content.Height - titleSize) * 0.48;
        DrawAlignedText(canvas, block.Title, content.X, y, Math.Max(1, content.Width - actionWidth - 12), VisualTextAlignment.Left, theme.Text, titleSize, true);
        if (actionText.Length > 0) DrawAlignedText(canvas, actionText, content.X + content.Width - actionWidth, y + 1, actionWidth, VisualTextAlignment.Right, VisualBlockRendering.PaletteAt(theme, 0), Math.Max(12, theme.SubtitleFontSize + 1), true);
    }
}
