using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Svg;
using ChartForgeX.Themes;

namespace ChartForgeX.Terminal;

/// <summary>
/// Renders terminal stories as self-contained, script-free animated SVG.
/// </summary>
public sealed class SvgTerminalStoryRenderer {
    /// <summary>Renders a terminal story to SVG markup.</summary>
    public string Render(TerminalStory story) => Render(story, string.Empty);

    /// <summary>Renders a terminal story with a caller-provided deterministic ID scope.</summary>
    public string Render(TerminalStory story, string idScope) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        var layout = TerminalStoryLayout.Build(story);
        var provisionalId = SvgRenderedIdentity.CreateProvisionalId("cfx-terminal", idScope, story.Title, layout.Width.ToString(CultureInfo.InvariantCulture), layout.Lines.Count.ToString(CultureInfo.InvariantCulture));
        var svg = RenderCore(story, layout, provisionalId);
        return SvgRenderedIdentity.Bind(svg, provisionalId, "cfx-terminal", idScope);
    }

    private static string RenderCore(TerminalStory story, TerminalStoryLayout layout, string id) {
        var theme = story.Theme;
        var writer = new SvgMarkupWriter(8192);
        writer.StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("id", id)
            .Attribute("width", layout.Width)
            .Attribute("height", layout.Height)
            .Attribute("viewBox", "0 0 " + layout.Width.ToString(CultureInfo.InvariantCulture) + " " + layout.Height.ToString(CultureInfo.InvariantCulture))
            .Attribute("role", "img")
            .Attribute("aria-labelledby", id + "-title " + id + "-desc")
            .Attribute("preserveAspectRatio", "xMidYMid meet")
            .Attribute("shape-rendering", "geometricPrecision")
            .Attribute("text-rendering", "geometricPrecision")
            .Attribute("style", "max-width:100%;height:auto;display:block")
            .Attribute("data-cfx-terminal", story.Dialect.ToString())
            .Attribute("data-cfx-motion", "terminal-story")
            .Attribute("data-cfx-motion-duration", layout.DurationSeconds.ToString("0.###", CultureInfo.InvariantCulture))
            .EndStartElement().Line()
            .StartElement("title").Attribute("id", id + "-title").Text(story.Title).EndElement().Line()
            .StartElement("desc").Attribute("id", id + "-desc").Text(AccessibleDescription(layout)).EndElement().Line()
            .StartElement("defs").EndStartElement().Line()
            .StartElement("filter").Attribute("id", id + "-shadow").Attribute("x", "-15%").Attribute("y", "-15%").Attribute("width", "130%").Attribute("height", "140%").EndStartElement()
            .StartElement("feDropShadow").Attribute("dx", 0).Attribute("dy", 12).Attribute("stdDeviation", 18).Attribute("flood-color", "#000").Attribute("flood-opacity", 0.28).EndEmptyElement()
            .EndElement().Line()
            .StartElement("style").EndStartElement().Raw(BuildCss(id, layout)).EndElement().Line()
            .EndElement().Line()
            .StartElement("rect").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", theme.PageBackground.ToCss()).EndEmptyElement().Line();
        SvgTerminalStoryChromeRenderer.Write(writer, story, layout, id + "-shadow", id);
        writer.StartElement("rect")
            .Attribute("data-cfx-role", "terminal-tab-background")
            .Attribute("class", "cfx-terminal-tab-background")
            .Attribute("x", 9)
            .Attribute("y", layout.HeaderHeightValue + 9)
            .Attribute("width", layout.Width - 18)
            .Attribute("height", layout.Height - layout.HeaderHeightValue - 18)
            .Attribute("fill", layout.TabBackground(null).ToCss())
            .EndEmptyElement().Line();

        for (var tabIndex = 0; tabIndex < layout.Tabs.Count; tabIndex++) {
            var renderedTab = layout.Tabs[tabIndex];
            var tab = renderedTab.Tab;
            var finalClass = string.Equals(tab.Id, layout.FinalTabId, StringComparison.OrdinalIgnoreCase) ? " cfx-terminal-tab-final" : string.Empty;
            writer.StartElement("g")
                .Attribute("data-cfx-role", "terminal-tab-panel")
                .Attribute("data-cfx-tab", tab.Id)
                .Attribute("class", "cfx-terminal-tab-panel cfx-terminal-tab-state-" + tabIndex + finalClass)
                .Attribute("opacity", string.Equals(tab.Id, layout.FinalTabId, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .EndStartElement().Line();
            foreach (var line in renderedTab.Lines) {
                var y = layout.ContentTop + line.RowIndex * story.LineHeight + story.FontSize;
                WriteLine(writer, story, layout, id, tab, line, y);
            }
            writer.EndElement().Line();
        }

        writer.EndElement().Line();
        return writer.Build().Replace("\r\n", "\n");
    }

    private static void WriteLine(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout, string id, TerminalTab tab, TerminalRenderedLine line, double y) {
        var isFinalPrompt = line.IsFinalPrompt;
        var isTypedCommand = line.IsCommand && !isFinalPrompt;
        var cssClass = isTypedCommand ? "cfx-terminal-line cfx-terminal-type" : "cfx-terminal-line cfx-terminal-appear";
        var style = "--cfx-start:" + line.StartSeconds.ToString("0.###", CultureInfo.InvariantCulture) +
                    "s;--cfx-duration:" + Math.Max(0.01, line.DurationSeconds).ToString("0.###", CultureInfo.InvariantCulture) + "s";
        writer.StartElement("text")
            .Attribute("data-cfx-role", line.IsCommand ? "terminal-command" : "terminal-output")
            .Attribute("class", cssClass)
            .Attribute("x", layout.ContentX)
            .Attribute("y", y)
            .Attribute("fill", ToneColor(tab.Theme, line.Tone))
            .Attribute("font-family", line.IsTable ? ChartFontStacks.Mono : tab.Theme.FontFamily)
            .Attribute("font-size", story.FontSize)
            .Attribute("style", style)
            .Attribute("xml:space", "preserve")
            .EndStartElement();
        if (isTypedCommand) {
            WriteTypedCommand(writer, tab, line);
        } else if (line.IsCommand) {
            var prompt = line.Text.Substring(0, line.PromptLength);
            var command = line.Text.Substring(line.PromptLength);
            writer.StartElement("tspan").Attribute("fill", tab.Theme.Accent.ToCss()).Attribute("font-weight", "650").Text(prompt).EndElement();
            if (command.Length > 0) writer.StartElement("tspan").Attribute("fill", tab.Theme.Text.ToCss()).Text(command).EndElement();
        } else {
            writer.Text(line.Text);
        }

        if (isFinalPrompt) {
            writer.StartElement("tspan")
                .Attribute("data-cfx-role", "terminal-cursor")
                .Attribute("class", "cfx-terminal-cursor")
                .Attribute("dx", 2)
                .Attribute("fill", tab.Theme.Cursor.ToCss())
                .Attribute("font-family", "monospace")
                .Attribute("font-weight", 400)
                .Attribute("style", "--cfx-start:" + line.StartSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s")
                .Text("▌")
                .EndElement();
        }

        writer.EndElement().Line();
    }

    private static void WriteTypedCommand(SvgMarkupWriter writer, TerminalTab tab, TerminalRenderedLine line) {
        var promptLength = Math.Min(line.PromptLength, line.Text.Length);
        var promptElements = TerminalTextWidth.VisibleElements(line.Text.Substring(0, promptLength)).ToArray();
        var commandElements = TerminalTextWidth.VisibleElements(line.Text.Substring(promptLength)).ToArray();
        var elementCount = Math.Max(1, promptElements.Length + commandElements.Length);
        var elementIndex = 0;
        foreach (var element in promptElements) {
            WriteTypedElement(writer, tab, line, element, true, elementIndex++, elementCount);
        }
        foreach (var element in commandElements) {
            WriteTypedElement(writer, tab, line, element, false, elementIndex++, elementCount);
        }
    }

    private static void WriteTypedElement(SvgMarkupWriter writer, TerminalTab tab, TerminalRenderedLine line, string element, bool isPrompt, int elementIndex, int elementCount) {
        var revealSeconds = line.StartSeconds + line.DurationSeconds * (elementIndex + 1) / elementCount;
        writer.StartElement("tspan")
            .Attribute("class", "cfx-terminal-glyph")
            .Attribute("fill", isPrompt ? tab.Theme.Accent.ToCss() : tab.Theme.Text.ToCss());
        if (isPrompt) {
            writer.Attribute("font-weight", "650");
        }
        writer.Attribute("style", "--cfx-glyph-start:" + revealSeconds.ToString("0.######", CultureInfo.InvariantCulture) + "s")
            .Text(element)
            .EndElement();
    }

    private static string BuildCss(string id, TerminalStoryLayout layout) {
        var css = new StringBuilder();
        css.Append("@keyframes ").Append(id).Append("-motion-appear{0%{opacity:0;transform:translateY(3px)}100%{opacity:1;transform:none}}");
        css.Append("@keyframes ").Append(id).Append("-motion-glyph{0%{opacity:0}100%{opacity:1}}");
        css.Append("@keyframes ").Append(id).Append("-motion-cursor{0%{opacity:0}.01%,46%{opacity:1}47%,100%{opacity:0}}");
        css.Append("#").Append(id).Append(" .cfx-terminal-appear{animation:").Append(id).Append("-motion-appear var(--cfx-duration) ease-out var(--cfx-start) both}");
        css.Append("#").Append(id).Append(" .cfx-terminal-glyph{animation:").Append(id).Append("-motion-glyph 0s linear var(--cfx-glyph-start) both}");
        css.Append("#").Append(id).Append(" .cfx-terminal-cursor{animation:").Append(id).Append("-motion-cursor 1s steps(1,end) var(--cfx-start) infinite both}");
        AppendTabCss(css, id, layout);
        AppendBackgroundCss(css, id, layout);
        css.Append("@media (prefers-reduced-motion:reduce){#").Append(id).Append(" .cfx-terminal-line,#").Append(id).Append(" .cfx-terminal-glyph,#").Append(id).Append(" .cfx-terminal-cursor{opacity:1;clip-path:none;transform:none;animation:none}#").Append(id).Append(" .cfx-terminal-tab-panel,#").Append(id).Append(" .cfx-terminal-tab-active{opacity:0;animation:none}#").Append(id).Append(" [class*=cfx-terminal-tab-presence-],#").Append(id).Append(" .cfx-terminal-tab-final{opacity:1;animation:none}#").Append(id).Append(" .cfx-terminal-tab-background{animation:none}}");
        css.Append("@media print{#").Append(id).Append(" .cfx-terminal-line,#").Append(id).Append(" .cfx-terminal-glyph,#").Append(id).Append(" .cfx-terminal-cursor{opacity:1;clip-path:none;transform:none;animation:none}#").Append(id).Append(" .cfx-terminal-tab-panel,#").Append(id).Append(" .cfx-terminal-tab-active{opacity:0;animation:none}#").Append(id).Append(" [class*=cfx-terminal-tab-presence-],#").Append(id).Append(" .cfx-terminal-tab-final{opacity:1;animation:none}#").Append(id).Append(" .cfx-terminal-tab-background{animation:none}}");
        return css.ToString();
    }

    private static void AppendTabCss(StringBuilder css, string id, TerminalStoryLayout layout) {
        for (var index = 0; index < layout.Tabs.Count; index++) {
            var tabId = layout.Tabs[index].Tab.Id;
            var animationName = id + "-motion-tab-state-" + index;
            css.Append("@keyframes ").Append(animationName).Append('{');
            AppendTabKeyframe(css, layout, tabId, 0);
            foreach (var transition in layout.Transitions) {
                AppendTabKeyframe(css, layout, tabId, transition.StartSeconds);
                var transitionEnd = transition.StartSeconds + Math.Max(0.0001, transition.DurationSeconds);
                AppendTabKeyframe(css, layout, tabId, transitionEnd);
            }
            AppendTabKeyframe(css, layout, tabId, layout.DurationSeconds);
            css.Append('}');
            css.Append('#').Append(id).Append(" .cfx-terminal-tab-state-").Append(index)
                .Append("{animation:").Append(animationName).Append(' ')
                .Append(Math.Max(0.001, layout.DurationSeconds).ToString("0.######", CultureInfo.InvariantCulture))
                .Append("s linear 0s both}");
            AppendTabPresenceCss(css, id, layout, index);
        }
    }

    private static void AppendTabPresenceCss(StringBuilder css, string id, TerminalStoryLayout layout, int index) {
        var renderedTab = layout.Tabs[index];
        var animationName = id + "-motion-tab-presence-" + index;
        css.Append("@keyframes ").Append(animationName).Append('{');
        if (renderedTab.OpenSeconds <= 0) {
            css.Append("0%,100%{opacity:1}");
        } else {
            css.Append("0%{opacity:0}");
            AppendPresenceKeyframe(css, layout, renderedTab.OpenSeconds, 0);
            AppendPresenceKeyframe(css, layout, renderedTab.OpenSeconds + 0.1, 1);
            css.Append("100%{opacity:1}");
        }
        css.Append('}');
        css.Append('#').Append(id).Append(" .cfx-terminal-tab-presence-").Append(index)
            .Append("{animation:").Append(animationName).Append(' ')
            .Append(Math.Max(0.001, layout.DurationSeconds).ToString("0.######", CultureInfo.InvariantCulture))
            .Append("s linear 0s both}");
    }

    private static void AppendPresenceKeyframe(StringBuilder css, TerminalStoryLayout layout, double seconds, int opacity) {
        var boundedSeconds = Math.Max(0, Math.Min(layout.DurationSeconds, seconds));
        var percentage = layout.DurationSeconds <= 0 ? 100 : boundedSeconds / layout.DurationSeconds * 100;
        css.Append(percentage.ToString("0.######", CultureInfo.InvariantCulture)).Append("%{opacity:").Append(opacity).Append("}");
    }

    private static void AppendTabKeyframe(StringBuilder css, TerminalStoryLayout layout, string tabId, double seconds) {
        var boundedSeconds = Math.Max(0, Math.Min(layout.DurationSeconds, seconds));
        var percentage = layout.DurationSeconds <= 0 ? 100 : boundedSeconds / layout.DurationSeconds * 100;
        css.Append(percentage.ToString("0.######", CultureInfo.InvariantCulture)).Append("%{opacity:")
            .Append(layout.TabOpacity(tabId, boundedSeconds).ToString("0.######", CultureInfo.InvariantCulture)).Append("}");
    }

    private static void AppendBackgroundCss(StringBuilder css, string id, TerminalStoryLayout layout) {
        var animationName = id + "-motion-tab-background";
        css.Append("@keyframes ").Append(animationName).Append('{');
        AppendBackgroundKeyframe(css, layout, 0);
        foreach (var transition in layout.Transitions) {
            AppendBackgroundKeyframe(css, layout, transition.StartSeconds);
            AppendBackgroundKeyframe(css, layout, transition.StartSeconds + Math.Max(0.0001, transition.DurationSeconds));
        }
        AppendBackgroundKeyframe(css, layout, layout.DurationSeconds);
        css.Append('}');
        css.Append('#').Append(id).Append(" .cfx-terminal-tab-background{animation:").Append(animationName).Append(' ')
            .Append(Math.Max(0.001, layout.DurationSeconds).ToString("0.######", CultureInfo.InvariantCulture))
            .Append("s linear 0s both}");
    }

    private static void AppendBackgroundKeyframe(StringBuilder css, TerminalStoryLayout layout, double seconds) {
        var boundedSeconds = Math.Max(0, Math.Min(layout.DurationSeconds, seconds));
        var percentage = layout.DurationSeconds <= 0 ? 100 : boundedSeconds / layout.DurationSeconds * 100;
        css.Append(percentage.ToString("0.######", CultureInfo.InvariantCulture)).Append("%{fill:")
            .Append(layout.TabBackground(boundedSeconds).ToCss()).Append("}");
    }

    private static string AccessibleDescription(TerminalStoryLayout layout) {
        var description = new StringBuilder("Terminal transcript:");
        foreach (var line in layout.TranscriptLines) {
            description.Append('\n').Append(line.TrimEnd());
        }

        description.Append("\nMotion is decorative; the complete transcript remains available when animation is unsupported, reduced, or printed.");
        return description.ToString();
    }

    internal static string ToneColor(TerminalTheme theme, TerminalTextTone tone) {
        switch (tone) {
            case TerminalTextTone.Default: return theme.Text.ToCss();
            case TerminalTextTone.Muted: return theme.Muted.ToCss();
            case TerminalTextTone.Accent: return theme.Accent.ToCss();
            case TerminalTextTone.Success: return theme.Success.ToCss();
            case TerminalTextTone.Warning: return theme.Warning.ToCss();
            case TerminalTextTone.Error: return theme.Error.ToCss();
            default: throw new ArgumentOutOfRangeException(nameof(tone));
        }
    }
}
