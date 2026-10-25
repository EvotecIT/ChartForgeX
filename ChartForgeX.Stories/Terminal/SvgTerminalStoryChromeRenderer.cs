using System;
using System.Globalization;
using ChartForgeX.Svg;

namespace ChartForgeX.Terminal;

internal static class SvgTerminalStoryChromeRenderer {
    internal static void Write(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout, string shadowId, string id) {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        var theme = story.ResolvedStyle;
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
                case TerminalWindowStyle.Linux:
                    WriteActiveTitles(writer, story, layout, layout.Width / 2.0, 8 + layout.HeaderHeightValue / 2 + 4, "middle");
                    WriteWindowControls(writer, story, layout);
                    break;
                case TerminalWindowStyle.Minimal:
                    WriteActiveTitles(writer, story, layout, 20, 26, "start");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(story.WindowStyle));
            }
        }

        writer.EndElement().Line();
    }

    private static string TrafficLight(TerminalStory story, string light, string dark) =>
        story.Tabs[0].ThemeMode == ChartForgeX.Themes.VisualThemeMode.Light ? light : dark;

    private static void WriteMacOS(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout) {
        writer.StartElement("g").Attribute("data-cfx-role", "terminal-macos-controls").EndStartElement().Line();
        WriteCircle(writer, 26, 22, TrafficLight(story, "#FF5F57", "#CE534C"));
        WriteCircle(writer, 46, 22, TrafficLight(story, "#FEBC2E", "#C59A2A"));
        WriteCircle(writer, 66, 22, TrafficLight(story, "#28C840", "#26A443"));
        writer.EndElement().Line();
        WriteActiveTitles(writer, story, layout, layout.Width / 2.0, 8 + layout.HeaderHeightValue / 2 + 4, "middle");
    }

    private static void WriteWindowsTerminal(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout) {
        var theme = story.ResolvedStyle;
        var tabCount = layout.Tabs.Count;
        var tabWidth = TerminalWindowChrome.WindowsTabWidth(layout.Width, tabCount);
        var tabRight = TerminalWindowChrome.WindowsTabRight(layout.Width, tabCount);
        for (var index = 0; index < tabCount; index++) {
            var tab = layout.Tabs[index].Tab;
            var tabX = TerminalWindowChrome.WindowsTabX(layout.Width, tabCount, index);
            var finalClass = string.Equals(tab.Id, layout.FinalTabId, StringComparison.OrdinalIgnoreCase) ? " cfx-terminal-tab-final" : string.Empty;
            writer.StartElement("g").Attribute("data-cfx-role", "terminal-tab").Attribute("data-cfx-tab", tab.Id).Attribute("class", "cfx-terminal-tab-presence-" + index).Attribute("opacity", 1).EndStartElement().Line()
                .StartElement("rect").Attribute("x", tabX).Attribute("y", 12).Attribute("width", tabWidth).Attribute("height", 28).Attribute("rx", 6).Attribute("fill", theme.HeaderBackground.ToCss()).EndEmptyElement().Line()
                .StartElement("rect").Attribute("data-cfx-role", "terminal-tab-active").Attribute("class", "cfx-terminal-tab-active cfx-terminal-tab-state-" + index + finalClass).Attribute("opacity", string.Equals(tab.Id, layout.FinalTabId, StringComparison.OrdinalIgnoreCase) ? 1 : 0).Attribute("x", tabX).Attribute("y", 12).Attribute("width", tabWidth).Attribute("height", 28).Attribute("rx", 6).Attribute("fill", tab.ResolvedStyle.Background.ToCss()).EndEmptyElement().Line();
            WriteTabIcon(writer, tab, tabX + 12, 17);
            writer.StartElement("text")
                .Attribute("data-cfx-role", "terminal-tab-title")
                .Attribute("x", tabX + 40)
                .Attribute("y", 31)
                .Attribute("fill", theme.Text.ToCss())
                .Attribute("font-family", tab.ResolvedStyle.FontFamily)
                .Attribute("font-size", TerminalWindowChrome.TitleFontSize)
                .Attribute("font-weight", 400)
                .Text(TerminalWindowChrome.FitTabTitle(tab.Title, layout.Width, tabCount))
                .EndElement().Line();
            WriteCross(writer, TerminalWindowChrome.WindowsTabCloseX(layout.Width, tabCount, index), 26, TerminalWindowChrome.WindowsTabCloseRadius, theme.Muted.ToCss(), "terminal-tab-close");
            writer.EndElement().Line();
        }
        WritePlus(writer, tabRight + 25, 26, theme.Muted.ToCss());
        writer.StartElement("path").Attribute("data-cfx-role", "terminal-tab-menu").Attribute("d", "M" + (tabRight + 52).ToString(CultureInfo.InvariantCulture) + " 23l4 4 4-4").Attribute("fill", "none").Attribute("stroke", theme.Muted.ToCss()).Attribute("stroke-width", 1.5).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();

        WriteWindowControls(writer, story, layout);
    }

    private static void WriteWindowControls(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout) {
        var theme = story.ResolvedStyle;
        var controlY = 26d;
        if (story.WindowStyle == TerminalWindowStyle.Linux) {
            writer.StartElement("circle").Attribute("data-cfx-role", "terminal-window-close-ring")
                .Attribute("cx", layout.Width - 30).Attribute("cy", controlY).Attribute("r", 11)
                .Attribute("fill", "none").Attribute("stroke", theme.Muted.ToCss()).Attribute("stroke-width", 1.2).EndEmptyElement().Line();
            WriteCross(writer, layout.Width - 30, controlY, 4, theme.Muted.ToCss(), "terminal-window-close");
            return;
        }
        writer.StartElement("line").Attribute("data-cfx-role", "terminal-window-minimize").Attribute("x1", layout.Width - 106).Attribute("y1", controlY).Attribute("x2", layout.Width - 94).Attribute("y2", controlY).Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 1.3).EndEmptyElement().Line()
            .StartElement("rect").Attribute("data-cfx-role", "terminal-window-maximize").Attribute("x", layout.Width - 66).Attribute("y", controlY - 6).Attribute("width", 12).Attribute("height", 12).Attribute("fill", "none").Attribute("stroke", theme.Text.ToCss()).Attribute("stroke-width", 1.3).EndEmptyElement().Line();
        WriteCross(writer, layout.Width - 25, controlY, 6, theme.Text.ToCss(), "terminal-window-close");
    }

    private static void WriteActiveTitles(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout, double x, double y, string anchor) {
        for (var index = 0; index < layout.Tabs.Count; index++) {
            var tab = layout.Tabs[index].Tab;
            var finalClass = string.Equals(tab.Id, layout.FinalTabId, StringComparison.OrdinalIgnoreCase) ? " cfx-terminal-tab-final" : string.Empty;
            writer.StartElement("text")
                .Attribute("data-cfx-role", "terminal-title")
                .Attribute("data-cfx-tab", tab.Id)
                .Attribute("class", "cfx-terminal-tab-active cfx-terminal-tab-state-" + index + finalClass)
                .Attribute("opacity", string.Equals(tab.Id, layout.FinalTabId, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .Attribute("x", x)
                .Attribute("y", y)
                .Attribute("fill", story.ResolvedStyle.Muted.ToCss())
                .Attribute("font-family", tab.ResolvedStyle.FontFamily)
                .Attribute("font-size", TerminalWindowChrome.TitleFontSize)
                .Attribute("font-weight", 400)
                .Attribute("text-anchor", anchor)
                .Text(TerminalStoryLayout.FitTitle(tab.Title, layout.Width, story.WindowStyle))
                .EndElement().Line();
        }
    }

    private static void WriteTabIcon(SvgMarkupWriter writer, TerminalTab tab, double x, double y) {
        if (tab.Icon == TerminalTabIcon.None) return;
        if (tab.Icon == TerminalTabIcon.Ubuntu) {
            writer.StartElement("circle").Attribute("data-cfx-role", "terminal-shell-icon").Attribute("cx", x + 9).Attribute("cy", y + 9).Attribute("r", 9).Attribute("fill", tab.ResolvedStyle.Accent.ToCss()).EndEmptyElement().Line()
                .StartElement("circle").Attribute("cx", x + 9).Attribute("cy", y + 9).Attribute("r", 3).Attribute("fill", "none").Attribute("stroke", tab.ResolvedStyle.Background.ToCss()).Attribute("stroke-width", 1.6).EndEmptyElement().Line();
            return;
        }

        writer.StartElement("rect").Attribute("data-cfx-role", "terminal-shell-icon").Attribute("x", x).Attribute("y", y).Attribute("width", 18).Attribute("height", 18).Attribute("rx", 3).Attribute("fill", tab.ResolvedStyle.Accent.ToCss()).EndEmptyElement().Line()
            .StartElement("path").Attribute("d", "M" + (x + 4).ToString(CultureInfo.InvariantCulture) + " " + (y + 5).ToString(CultureInfo.InvariantCulture) + "l4 4-4 4M" + (x + 9.5).ToString(CultureInfo.InvariantCulture) + " " + (y + 13).ToString(CultureInfo.InvariantCulture) + "h5").Attribute("fill", "none").Attribute("stroke", tab.ResolvedStyle.Background.ToCss()).Attribute("stroke-width", 1.7).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
    }

    private static string HeaderPath(int width, double headerHeight, double radius) {
        var left = 8 + radius;
        var right = width - 8 - radius;
        return "M" + left.ToString(CultureInfo.InvariantCulture) + " 8H" + right.ToString(CultureInfo.InvariantCulture) + "A" + radius.ToString(CultureInfo.InvariantCulture) + " " + radius.ToString(CultureInfo.InvariantCulture) + " 0 0 1 " + (width - 8).ToString(CultureInfo.InvariantCulture) + " " + left.ToString(CultureInfo.InvariantCulture) + "V" + (headerHeight + 8).ToString(CultureInfo.InvariantCulture) + "H8V" + left.ToString(CultureInfo.InvariantCulture) + "A" + radius.ToString(CultureInfo.InvariantCulture) + " " + radius.ToString(CultureInfo.InvariantCulture) + " 0 0 1 " + left.ToString(CultureInfo.InvariantCulture) + " 8Z";
    }

    private static void WriteCircle(SvgMarkupWriter writer, double x, double y, string color) {
        writer.StartElement("circle").Attribute("cx", x).Attribute("cy", y).Attribute("r", 6).Attribute("fill", color).EndEmptyElement().Line();
    }

    private static void WriteCross(SvgMarkupWriter writer, double x, double y, double radius, string color, string role) {
        writer.StartElement("path").Attribute("data-cfx-role", role).Attribute("d", "M" + (x - radius).ToString(CultureInfo.InvariantCulture) + " " + (y - radius).ToString(CultureInfo.InvariantCulture) + "L" + (x + radius).ToString(CultureInfo.InvariantCulture) + " " + (y + radius).ToString(CultureInfo.InvariantCulture) + "M" + (x + radius).ToString(CultureInfo.InvariantCulture) + " " + (y - radius).ToString(CultureInfo.InvariantCulture) + "L" + (x - radius).ToString(CultureInfo.InvariantCulture) + " " + (y + radius).ToString(CultureInfo.InvariantCulture)).Attribute("fill", "none").Attribute("stroke", color).Attribute("stroke-width", 1.4).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
    }

    private static void WritePlus(SvgMarkupWriter writer, double x, double y, string color) {
        writer.StartElement("path").Attribute("data-cfx-role", "terminal-new-tab").Attribute("d", "M" + (x - 6).ToString(CultureInfo.InvariantCulture) + " " + y.ToString(CultureInfo.InvariantCulture) + "H" + (x + 6).ToString(CultureInfo.InvariantCulture) + "M" + x.ToString(CultureInfo.InvariantCulture) + " " + (y - 6).ToString(CultureInfo.InvariantCulture) + "V" + (y + 6).ToString(CultureInfo.InvariantCulture)).Attribute("fill", "none").Attribute("stroke", color).Attribute("stroke-width", 1.4).Attribute("stroke-linecap", "round").EndEmptyElement().Line();
    }
}
