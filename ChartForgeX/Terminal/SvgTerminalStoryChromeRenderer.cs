using System;
using System.Globalization;
using ChartForgeX.Svg;

namespace ChartForgeX.Terminal;

internal static class SvgTerminalStoryChromeRenderer {
    internal static void Write(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout, string shadowId) {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        var theme = story.Theme;
        var radius = TerminalWindowChrome.FrameRadius(story.WindowStyle);
        writer.StartElement("g")
            .Attribute("data-cfx-role", "terminal-window-chrome")
            .Attribute("data-cfx-window-style", story.WindowStyle.ToString())
            .EndStartElement().Line()
            .StartElement("rect").Attribute("data-cfx-role", "terminal-frame").Attribute("x", 8).Attribute("y", 8).Attribute("width", layout.Width - 16).Attribute("height", layout.Height - 16).Attribute("rx", radius).Attribute("fill", theme.Background.ToCss()).Attribute("stroke", theme.Border.ToCss()).Attribute("stroke-width", 1.2).Attribute("filter", "url(#" + shadowId + ")").EndEmptyElement().Line();

        if (story.WindowStyle != TerminalWindowStyle.None) {
            writer.StartElement("path").Attribute("data-cfx-role", "terminal-titlebar").Attribute("d", HeaderPath(layout.Width, layout.HeaderHeightValue, radius)).Attribute("fill", theme.HeaderBackground.ToCss()).EndEmptyElement().Line()
                .StartElement("line").Attribute("x1", 8).Attribute("y1", layout.HeaderHeightValue + 8).Attribute("x2", layout.Width - 8).Attribute("y2", layout.HeaderHeightValue + 8).Attribute("stroke", theme.Border.ToCss()).Attribute("stroke-width", 1).EndEmptyElement().Line();
            switch (story.WindowStyle) {
                case TerminalWindowStyle.MacOS:
                    WriteMacOS(writer, story, layout);
                    break;
                case TerminalWindowStyle.WindowsTerminal:
                    WriteWindowsTerminal(writer, story, layout);
                    break;
                case TerminalWindowStyle.Minimal:
                    WriteTitle(writer, story, layout, 28, 31, "start");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(story.WindowStyle));
            }
        }

        writer.EndElement().Line();
    }

    private static void WriteMacOS(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout) {
        writer.StartElement("g").Attribute("data-cfx-role", "terminal-macos-controls").EndStartElement().Line();
        WriteCircle(writer, 29, 29, "#FF5F57");
        WriteCircle(writer, 49, 29, "#FEBC2E");
        WriteCircle(writer, 69, 29, "#28C840");
        writer.EndElement().Line();
        WriteTitle(writer, story, layout, layout.Width / 2.0, 33, "middle");
    }

    private static void WriteWindowsTerminal(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout) {
        var theme = story.Theme;
        var tabWidth = TerminalWindowChrome.WindowsTabWidth(layout.Width);
        var tabRight = TerminalWindowChrome.WindowsTabRight(layout.Width);
        writer.StartElement("rect").Attribute("data-cfx-role", "terminal-tab").Attribute("x", TerminalWindowChrome.WindowsTabLeft).Attribute("y", 13).Attribute("width", tabWidth).Attribute("height", 37).Attribute("rx", 9).Attribute("fill", theme.Background.ToCss()).EndEmptyElement().Line()
            .StartElement("rect").Attribute("data-cfx-role", "terminal-shell-icon").Attribute("x", 28).Attribute("y", 22).Attribute("width", 18).Attribute("height", 18).Attribute("rx", 3).Attribute("fill", theme.Accent.ToCss()).EndEmptyElement().Line()
            .StartElement("path").Attribute("d", "M32 27l4 4-4 4M37.5 35h5").Attribute("fill", "none").Attribute("stroke", theme.Background.ToCss()).Attribute("stroke-width", 1.7).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
        WriteTitle(writer, story, layout, TerminalWindowChrome.WindowsTitleX, 37, "start");
        WriteCross(writer, TerminalWindowChrome.WindowsTabCloseX(layout.Width), 31, TerminalWindowChrome.WindowsTabCloseRadius, theme.Muted.ToCss(), "terminal-tab-close");
        WritePlus(writer, tabRight + 25, 31, theme.Muted.ToCss());
        writer.StartElement("path").Attribute("data-cfx-role", "terminal-tab-menu").Attribute("d", "M" + (tabRight + 52).ToString(CultureInfo.InvariantCulture) + " 28l4 4 4-4").Attribute("fill", "none").Attribute("stroke", theme.Muted.ToCss()).Attribute("stroke-width", 1.5).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();

        var controlY = 31d;
        writer.StartElement("line").Attribute("data-cfx-role", "terminal-window-minimize").Attribute("x1", layout.Width - 106).Attribute("y1", controlY).Attribute("x2", layout.Width - 94).Attribute("y2", controlY).Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 1.3).EndEmptyElement().Line()
            .StartElement("rect").Attribute("data-cfx-role", "terminal-window-maximize").Attribute("x", layout.Width - 66).Attribute("y", controlY - 6).Attribute("width", 12).Attribute("height", 12).Attribute("fill", "none").Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 1.3).EndEmptyElement().Line();
        WriteCross(writer, layout.Width - 25, controlY, 6, theme.Text.ToCss(), "terminal-window-close");
    }

    private static void WriteTitle(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout, double x, double y, string anchor) {
        var visibleTitle = TerminalStoryLayout.FitTitle(story.Title, layout.Width, story.WindowStyle);
        writer.StartElement("text")
            .Attribute("data-cfx-role", "terminal-title")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("fill", story.Theme.Muted.ToCss())
            .Attribute("font-family", story.Theme.FontFamily)
            .Attribute("font-size", TerminalWindowChrome.TitleFontSize)
            .Attribute("font-weight", 600)
            .Attribute("text-anchor", anchor)
            .Text(visibleTitle)
            .EndElement().Line();
    }

    private static string HeaderPath(int width, double headerHeight, double radius) {
        var left = 8 + radius;
        var right = width - 8 - radius;
        return "M" + left.ToString(CultureInfo.InvariantCulture) + " 8H" + right.ToString(CultureInfo.InvariantCulture) + "A" + radius.ToString(CultureInfo.InvariantCulture) + " " + radius.ToString(CultureInfo.InvariantCulture) + " 0 0 1 " + (width - 8).ToString(CultureInfo.InvariantCulture) + " " + left.ToString(CultureInfo.InvariantCulture) + "V" + (headerHeight + 8).ToString(CultureInfo.InvariantCulture) + "H8V" + left.ToString(CultureInfo.InvariantCulture) + "A" + radius.ToString(CultureInfo.InvariantCulture) + " " + radius.ToString(CultureInfo.InvariantCulture) + " 0 0 1 " + left.ToString(CultureInfo.InvariantCulture) + " 8Z";
    }

    private static void WriteCircle(SvgMarkupWriter writer, double x, double y, string color) {
        writer.StartElement("circle").Attribute("cx", x).Attribute("cy", y).Attribute("r", 5.5).Attribute("fill", color).Attribute("fill-opacity", 0.92).EndEmptyElement().Line();
    }

    private static void WriteCross(SvgMarkupWriter writer, double x, double y, double radius, string color, string role) {
        writer.StartElement("path").Attribute("data-cfx-role", role).Attribute("d", "M" + (x - radius).ToString(CultureInfo.InvariantCulture) + " " + (y - radius).ToString(CultureInfo.InvariantCulture) + "L" + (x + radius).ToString(CultureInfo.InvariantCulture) + " " + (y + radius).ToString(CultureInfo.InvariantCulture) + "M" + (x + radius).ToString(CultureInfo.InvariantCulture) + " " + (y - radius).ToString(CultureInfo.InvariantCulture) + "L" + (x - radius).ToString(CultureInfo.InvariantCulture) + " " + (y + radius).ToString(CultureInfo.InvariantCulture)).Attribute("fill", "none").Attribute("stroke", color).Attribute("stroke-width", 1.4).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
    }

    private static void WritePlus(SvgMarkupWriter writer, double x, double y, string color) {
        writer.StartElement("path").Attribute("data-cfx-role", "terminal-new-tab").Attribute("d", "M" + (x - 6).ToString(CultureInfo.InvariantCulture) + " " + y.ToString(CultureInfo.InvariantCulture) + "H" + (x + 6).ToString(CultureInfo.InvariantCulture) + "M" + x.ToString(CultureInfo.InvariantCulture) + " " + (y - 6).ToString(CultureInfo.InvariantCulture) + "V" + (y + 6).ToString(CultureInfo.InvariantCulture)).Attribute("fill", "none").Attribute("stroke", color).Attribute("stroke-width", 1.4).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
    }
}
