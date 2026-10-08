using System;
using System.Net;

namespace ChartForgeX.Terminal;

/// <summary>
/// Renders terminal stories as self-contained HTML.
/// </summary>
public sealed class HtmlTerminalStoryRenderer {
    /// <summary>Renders an inline SVG terminal story fragment.</summary>
    public string RenderFragment(TerminalStory story) => RenderFragment(story, string.Empty);

    /// <summary>Renders an inline SVG terminal story fragment with a deterministic ID scope.</summary>
    public string RenderFragment(TerminalStory story, string idScope) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        return "<div class=\"chartforgex-terminal-story\">" + new SvgTerminalStoryRenderer().Render(story, idScope) + "</div>";
    }

    /// <summary>Renders a complete responsive HTML document.</summary>
    public string RenderPage(TerminalStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var title = WebUtility.HtmlEncode(story.Title);
        var background = story.Theme.PageBackground.ToCss();
        var surface = story.Theme.Background.ToCss();
        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + title + "</title><style>html,body{margin:0;min-height:100%;background:linear-gradient(180deg," + background + " 0%," + surface + " 140%)}html{-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}body{display:grid;place-items:center;padding:24px;box-sizing:border-box;overflow:visible}.chartforgex-terminal-story{width:min(100%," + story.Width + "px)}.chartforgex-terminal-story svg{display:block;width:100%;height:auto}@media print{body{padding:0;background:transparent}.chartforgex-terminal-story{width:100%}}</style></head><body>" + RenderFragment(story, "html-page") + "</body></html>";
    }
}
