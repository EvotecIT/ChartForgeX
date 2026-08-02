using System;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Terminal;

internal static class PngTerminalStoryChromeRenderer {
    internal static void Draw(RgbaCanvas canvas, TerminalStory story, TerminalStoryLayout layout, TerminalTabRasterFonts fonts, double? elapsedSeconds) {
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
                DrawMacOS(canvas, story, layout, fonts, elapsedSeconds);
                break;
            case TerminalWindowStyle.WindowsTerminal:
                DrawWindowsTerminal(canvas, story, layout, fonts, elapsedSeconds);
                break;
            case TerminalWindowStyle.Minimal:
                DrawActiveTitles(canvas, story, layout, fonts, elapsedSeconds, 28, 19, false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(story.WindowStyle));
        }
    }

    private static void DrawMacOS(RgbaCanvas canvas, TerminalStory story, TerminalStoryLayout layout, TerminalTabRasterFonts fonts, double? elapsedSeconds) {
        canvas.DrawCircle(29, 29, 5.5, ChartColor.FromHex("#FF5F57"));
        canvas.DrawCircle(49, 29, 5.5, ChartColor.FromHex("#FEBC2E"));
        canvas.DrawCircle(69, 29, 5.5, ChartColor.FromHex("#28C840"));
        DrawActiveTitles(canvas, story, layout, fonts, elapsedSeconds, layout.Width / 2d, 19, true);
    }

    private static void DrawWindowsTerminal(RgbaCanvas canvas, TerminalStory story, TerminalStoryLayout layout, TerminalTabRasterFonts fonts, double? elapsedSeconds) {
        var theme = story.Theme;
        var tabCount = layout.Tabs.Count;
        var tabWidth = TerminalWindowChrome.WindowsTabWidth(layout.Width, tabCount);
        var tabRight = TerminalWindowChrome.WindowsTabRight(layout.Width, tabCount);
        for (var index = 0; index < tabCount; index++) {
            var tab = layout.Tabs[index].Tab;
            if (!layout.TabVisible(tab.Id, elapsedSeconds)) continue;
            var tabX = TerminalWindowChrome.WindowsTabX(layout.Width, tabCount, index);
            var tabOpacity = layout.TabOpacity(tab.Id, elapsedSeconds);
            var tabFont = fonts.Outline(tab);
            canvas.FillRoundedRect(tabX, 13, tabWidth, 37, 9, theme.HeaderBackground);
            if (tabOpacity > 0) canvas.FillRoundedRect(tabX, 13, tabWidth, 37, 9, WithOpacity(tab.Theme.Background, tabOpacity));
            DrawTabIcon(canvas, tab, tabX + 12, 22);
            var title = TerminalPngTextPreserver.Preserve(TerminalWindowChrome.FitTabTitle(tab.Title, layout.Width, tabCount), tabFont);
            TerminalPngTextPreserver.Draw(canvas, tabX + 40, 23, title, theme.Text, TerminalWindowChrome.TitleFontSize, tabFont);
            DrawCross(canvas, TerminalWindowChrome.WindowsTabCloseX(layout.Width, tabCount, index), 31, TerminalWindowChrome.WindowsTabCloseRadius, theme.Muted);
        }
        DrawPlus(canvas, tabRight + 25, 31, theme.Muted);
        canvas.DrawLine(tabRight + 52, 28, tabRight + 56, 32, theme.Muted, 1.5);
        canvas.DrawLine(tabRight + 56, 32, tabRight + 60, 28, theme.Muted, 1.5);
        canvas.DrawLine(layout.Width - 106, 31, layout.Width - 94, 31, theme.Text, 1.3);
        canvas.StrokeRect(layout.Width - 66, 25, 12, 12, theme.Text, 1.3);
        DrawCross(canvas, layout.Width - 25, 31, 6, theme.Text);
    }

    private static void DrawActiveTitles(RgbaCanvas canvas, TerminalStory story, TerminalStoryLayout layout, TerminalTabRasterFonts fonts, double? elapsedSeconds, double x, double y, bool centered) {
        foreach (var renderedTab in layout.Tabs) {
            var tabOpacity = layout.TabOpacity(renderedTab.Tab.Id, elapsedSeconds);
            if (tabOpacity <= 0) continue;
            var tabFont = fonts.Outline(renderedTab.Tab);
            var title = TerminalStoryLayout.FitTitle(TerminalPngTextPreserver.Preserve(renderedTab.Tab.Title, tabFont), layout.Width, story.WindowStyle);
            var titleX = x;
            if (centered) {
                var width = TerminalPngTextPreserver.Measure(title, canvas, TerminalWindowChrome.TitleFontSize, tabFont);
                titleX -= width / 2;
            }
            TerminalPngTextPreserver.Draw(canvas, titleX, y, title, WithOpacity(story.Theme.Muted, tabOpacity), TerminalWindowChrome.TitleFontSize, tabFont);
        }
    }

    private static void DrawTabIcon(RgbaCanvas canvas, TerminalTab tab, double x, double y) {
        if (tab.Icon == TerminalTabIcon.None) return;
        if (tab.Icon == TerminalTabIcon.Ubuntu) {
            canvas.DrawCircle(x + 9, y + 9, 9, tab.Theme.Accent);
            canvas.DrawCircle(x + 9, y + 9, 3, tab.Theme.Background);
            return;
        }

        canvas.FillRoundedRect(x, y, 18, 18, 3, tab.Theme.Accent);
        canvas.DrawLine(x + 4, y + 5, x + 8, y + 9, tab.Theme.Background, 1.7);
        canvas.DrawLine(x + 8, y + 9, x + 4, y + 13, tab.Theme.Background, 1.7);
        canvas.DrawLine(x + 9.5, y + 13, x + 14.5, y + 13, tab.Theme.Background, 1.7);
    }

    private static ChartColor WithOpacity(ChartColor color, double opacity) {
        var bounded = Math.Max(0, Math.Min(1, opacity));
        return color.WithAlpha((byte)Math.Round(color.A * bounded));
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
