using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal static class PngTerminalStoryChromeRenderer {
    internal static void Draw(RgbaCanvas canvas, TerminalStory story, TerminalStoryLayout layout, TrueTypeFont? outlineFont) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        var theme = story.Theme;
        var radius = TerminalWindowChrome.FrameRadius(story.WindowStyle);
        canvas.FillRoundedRect(12, 18, layout.Width - 24, layout.Height - 24, radius + 2, ChartColor.Black.WithOpacity(0.18));
        canvas.FillRoundedRect(10, 14, layout.Width - 20, layout.Height - 20, radius + 1, ChartColor.Black.WithOpacity(0.12));
        canvas.FillRoundedRect(8, 8, layout.Width - 16, layout.Height - 16, radius, theme.Background);
        canvas.StrokeRoundedRect(8, 8, layout.Width - 16, layout.Height - 16, radius, theme.Border, 1.2);
        if (story.WindowStyle == TerminalWindowStyle.None) return;

        canvas.FillRoundedRect(8, 8, layout.Width - 16, layout.HeaderHeightValue, radius, theme.HeaderBackground);
        canvas.FillRect(8, layout.HeaderHeightValue, layout.Width - 16, 8, theme.HeaderBackground);
        canvas.DrawLine(8, layout.HeaderHeightValue + 8, layout.Width - 8, layout.HeaderHeightValue + 8, theme.Border, 1);
        switch (story.WindowStyle) {
            case TerminalWindowStyle.MacOS:
                DrawMacOS(canvas, story, layout, outlineFont);
                break;
            case TerminalWindowStyle.WindowsTerminal:
                DrawWindowsTerminal(canvas, story, layout, outlineFont);
                break;
            case TerminalWindowStyle.Minimal:
                DrawTitle(canvas, story, layout, outlineFont, 28, 19, false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(story.WindowStyle));
        }
    }

    private static void DrawMacOS(RgbaCanvas canvas, TerminalStory story, TerminalStoryLayout layout, TrueTypeFont? outlineFont) {
        canvas.DrawCircle(29, 29, 5.5, ChartColor.FromHex("#FF5F57"));
        canvas.DrawCircle(49, 29, 5.5, ChartColor.FromHex("#FEBC2E"));
        canvas.DrawCircle(69, 29, 5.5, ChartColor.FromHex("#28C840"));
        DrawTitle(canvas, story, layout, outlineFont, layout.Width / 2d, 19, true);
    }

    private static void DrawWindowsTerminal(RgbaCanvas canvas, TerminalStory story, TerminalStoryLayout layout, TrueTypeFont? outlineFont) {
        var theme = story.Theme;
        var tabWidth = TerminalWindowChrome.WindowsTabWidth(layout.Width);
        var tabRight = TerminalWindowChrome.WindowsTabRight(layout.Width);
        canvas.FillRoundedRect(TerminalWindowChrome.WindowsTabLeft, 13, tabWidth, 37, 9, theme.Background);
        canvas.FillRoundedRect(28, 22, 18, 18, 3, theme.Accent);
        canvas.DrawLine(32, 27, 36, 31, theme.Background, 1.7);
        canvas.DrawLine(36, 31, 32, 35, theme.Background, 1.7);
        canvas.DrawLine(37.5, 35, 42.5, 35, theme.Background, 1.7);
        DrawTitle(canvas, story, layout, outlineFont, TerminalWindowChrome.WindowsTitleX, 23, false);
        DrawCross(canvas, TerminalWindowChrome.WindowsTabCloseX(layout.Width), 31, TerminalWindowChrome.WindowsTabCloseRadius, theme.Muted);
        DrawPlus(canvas, tabRight + 25, 31, theme.Muted);
        canvas.DrawLine(tabRight + 52, 28, tabRight + 56, 32, theme.Muted, 1.5);
        canvas.DrawLine(tabRight + 56, 32, tabRight + 60, 28, theme.Muted, 1.5);
        canvas.DrawLine(layout.Width - 106, 31, layout.Width - 94, 31, theme.Text, 1.3);
        canvas.StrokeRect(layout.Width - 66, 25, 12, 12, theme.Text, 1.3);
        DrawCross(canvas, layout.Width - 25, 31, 6, theme.Text);
    }

    private static void DrawTitle(RgbaCanvas canvas, TerminalStory story, TerminalStoryLayout layout, TrueTypeFont? outlineFont, double x, double y, bool centered) {
        var title = TerminalStoryLayout.FitTitle(TerminalPngTextPreserver.Preserve(story.Title, outlineFont), layout.Width, story.WindowStyle);
        if (centered) {
            var width = TerminalPngTextPreserver.Measure(title, canvas, TerminalWindowChrome.TitleFontSize);
            x -= width / 2;
        }
        TerminalPngTextPreserver.Draw(canvas, x, y, title, story.Theme.Muted, TerminalWindowChrome.TitleFontSize);
    }

    private static void DrawCross(RgbaCanvas canvas, double x, double y, double radius, ChartColor color) {
        canvas.DrawLine(x - radius, y - radius, x + radius, y + radius, color, 1.4);
        canvas.DrawLine(x + radius, y - radius, x - radius, y + radius, color, 1.4);
    }

    private static void DrawPlus(RgbaCanvas canvas, double x, double y, ChartColor color) {
        canvas.DrawLine(x - 6, y, x + 6, y, color, 1.4);
        canvas.DrawLine(x, y - 6, x, y + 6, color, 1.4);
    }
}
