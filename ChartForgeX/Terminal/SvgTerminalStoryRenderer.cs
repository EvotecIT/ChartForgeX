using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Svg;

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
            .StartElement("style").EndStartElement().Raw(BuildCss(id)).EndElement().Line()
            .EndElement().Line()
            .StartElement("rect").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", theme.PageBackground.ToCss()).EndEmptyElement().Line()
            .StartElement("rect").Attribute("data-cfx-role", "terminal-frame").Attribute("x", 8).Attribute("y", 8).Attribute("width", layout.Width - 16).Attribute("height", layout.Height - 16).Attribute("rx", 14).Attribute("fill", theme.Background.ToCss()).Attribute("stroke", theme.Border.ToCss()).Attribute("stroke-width", 1.2).Attribute("filter", "url(#" + id + "-shadow)").EndEmptyElement().Line()
            .StartElement("path").Attribute("data-cfx-role", "terminal-titlebar").Attribute("d", HeaderPath(layout.Width, layout.HeaderHeightValue)).Attribute("fill", theme.HeaderBackground.ToCss()).EndEmptyElement().Line()
            .StartElement("line").Attribute("x1", 8).Attribute("y1", layout.HeaderHeightValue + 8).Attribute("x2", layout.Width - 8).Attribute("y2", layout.HeaderHeightValue + 8).Attribute("stroke", theme.Border.ToCss()).Attribute("stroke-width", 1).EndEmptyElement().Line();

        WriteTrafficLight(writer, 29, 29, "#FF5F57");
        WriteTrafficLight(writer, 49, 29, "#FEBC2E");
        WriteTrafficLight(writer, 69, 29, "#28C840");
        var visibleTitle = TerminalStoryLayout.FitTitle(story.Title, layout.Width);
        writer.StartElement("text")
            .Attribute("x", layout.Width / 2.0)
            .Attribute("y", 33)
            .Attribute("fill", theme.Muted.ToCss())
            .Attribute("font-family", theme.FontFamily)
            .Attribute("font-size", 12)
            .Attribute("font-weight", 600)
            .Attribute("text-anchor", "middle")
            .Text(visibleTitle)
            .EndElement().Line();

        for (var index = 0; index < layout.Lines.Count; index++) {
            var line = layout.Lines[index];
            var y = layout.ContentTop + index * story.LineHeight + story.FontSize;
            WriteLine(writer, story, layout, id, line, index, y);
        }

        writer.EndElement().Line();
        return writer.Build().Replace("\r\n", "\n");
    }

    private static void WriteLine(SvgMarkupWriter writer, TerminalStory story, TerminalStoryLayout layout, string id, TerminalRenderedLine line, int index, double y) {
        var isFinalPrompt = story.ShowFinalPrompt && index == layout.Lines.Count - 1;
        var isTypedCommand = line.IsCommand && !isFinalPrompt;
        var cssClass = isTypedCommand ? "cfx-terminal-line cfx-terminal-type" : "cfx-terminal-line cfx-terminal-appear";
        var style = "--cfx-start:" + line.StartSeconds.ToString("0.###", CultureInfo.InvariantCulture) +
                    "s;--cfx-duration:" + Math.Max(0.01, line.DurationSeconds).ToString("0.###", CultureInfo.InvariantCulture) + "s";
        writer.StartElement("text")
            .Attribute("data-cfx-role", line.IsCommand ? "terminal-command" : "terminal-output")
            .Attribute("class", cssClass)
            .Attribute("x", layout.ContentX)
            .Attribute("y", y)
            .Attribute("fill", ToneColor(story.Theme, line.Tone))
            .Attribute("font-family", story.Theme.FontFamily)
            .Attribute("font-size", story.FontSize)
            .Attribute("style", style)
            .Attribute("xml:space", "preserve")
            .EndStartElement();
        if (isTypedCommand) {
            WriteTypedCommand(writer, story, line);
        } else if (line.IsCommand) {
            var prompt = line.Text.Substring(0, line.PromptLength);
            var command = line.Text.Substring(line.PromptLength);
            writer.StartElement("tspan").Attribute("fill", story.Theme.Accent.ToCss()).Attribute("font-weight", "650").Text(prompt).EndElement();
            if (command.Length > 0) writer.StartElement("tspan").Attribute("fill", story.Theme.Text.ToCss()).Text(command).EndElement();
        } else {
            writer.Text(line.Text);
        }

        if (isFinalPrompt) {
            writer.StartElement("tspan")
                .Attribute("data-cfx-role", "terminal-cursor")
                .Attribute("class", "cfx-terminal-cursor")
                .Attribute("dx", 2)
                .Attribute("fill", story.Theme.Cursor.ToCss())
                .Attribute("font-family", "monospace")
                .Attribute("font-weight", 400)
                .Attribute("style", "--cfx-start:" + line.StartSeconds.ToString("0.###", CultureInfo.InvariantCulture) + "s")
                .Text("▌")
                .EndElement();
        }

        writer.EndElement().Line();
    }

    private static void WriteTypedCommand(SvgMarkupWriter writer, TerminalStory story, TerminalRenderedLine line) {
        var elementCount = Math.Max(1, TerminalStoryLayout.TextElementCount(line.Text));
        var elementIndex = 0;
        var characterOffset = 0;
        foreach (var element in TerminalTextWidth.Elements(line.Text)) {
            var isPrompt = characterOffset < line.PromptLength;
            var revealSeconds = line.StartSeconds + line.DurationSeconds * (elementIndex + 1) / elementCount;
            writer.StartElement("tspan")
                .Attribute("class", "cfx-terminal-glyph")
                .Attribute("fill", isPrompt ? story.Theme.Accent.ToCss() : story.Theme.Text.ToCss());
            if (isPrompt) {
                writer.Attribute("font-weight", "650");
            }
            writer.Attribute("style", "--cfx-glyph-start:" + revealSeconds.ToString("0.######", CultureInfo.InvariantCulture) + "s")
                .Text(element)
                .EndElement();
            characterOffset += element.Length;
            elementIndex++;
        }
    }

    private static string BuildCss(string id) {
        var css = new StringBuilder();
        css.Append("@keyframes ").Append(id).Append("-motion-appear{0%{opacity:0;transform:translateY(3px)}100%{opacity:1;transform:none}}");
        css.Append("@keyframes ").Append(id).Append("-motion-glyph{0%{opacity:0}100%{opacity:1}}");
        css.Append("@keyframes ").Append(id).Append("-motion-cursor{0%{opacity:0}.01%,46%{opacity:1}47%,100%{opacity:0}}");
        css.Append("#").Append(id).Append(" .cfx-terminal-appear{animation:").Append(id).Append("-motion-appear var(--cfx-duration) ease-out var(--cfx-start) both}");
        css.Append("#").Append(id).Append(" .cfx-terminal-glyph{animation:").Append(id).Append("-motion-glyph 0s linear var(--cfx-glyph-start) both}");
        css.Append("#").Append(id).Append(" .cfx-terminal-cursor{animation:").Append(id).Append("-motion-cursor 1s steps(1,end) var(--cfx-start) infinite both}");
        css.Append("@media (prefers-reduced-motion:reduce){#").Append(id).Append(" .cfx-terminal-line,#").Append(id).Append(" .cfx-terminal-glyph,#").Append(id).Append(" .cfx-terminal-cursor{opacity:1;clip-path:none;transform:none;animation:none}}");
        css.Append("@media print{#").Append(id).Append(" .cfx-terminal-line,#").Append(id).Append(" .cfx-terminal-glyph,#").Append(id).Append(" .cfx-terminal-cursor{opacity:1;clip-path:none;transform:none;animation:none}}");
        return css.ToString();
    }

    private static string AccessibleDescription(TerminalStoryLayout layout) {
        var description = new StringBuilder("Terminal transcript:");
        foreach (var line in layout.TranscriptLines) {
            description.Append('\n').Append(line.TrimEnd());
        }

        description.Append("\nMotion is decorative; the complete transcript remains available when animation is unsupported, reduced, or printed.");
        return description.ToString();
    }

    private static string HeaderPath(int width, double headerHeight) {
        return "M22 8H" + (width - 22).ToString(CultureInfo.InvariantCulture) + "A14 14 0 0 1 " + (width - 8).ToString(CultureInfo.InvariantCulture) + " 22V" + (headerHeight + 8).ToString(CultureInfo.InvariantCulture) + "H8V22A14 14 0 0 1 22 8Z";
    }

    private static void WriteTrafficLight(SvgMarkupWriter writer, double x, double y, string color) {
        writer.StartElement("circle").Attribute("cx", x).Attribute("cy", y).Attribute("r", 5.5).Attribute("fill", color).Attribute("fill-opacity", 0.92).EndEmptyElement().Line();
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
