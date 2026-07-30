using System;
using System.Text;

namespace ChartForgeX.Stories;

/// <summary>Renders visual stories as embeddable HTML or complete HTML pages.</summary>
public sealed class HtmlVisualStoryRenderer {
    /// <summary>Renders an embeddable HTML fragment.</summary>
    public string RenderFragment(VisualStory story) => RenderFragment(story, string.Empty);

    /// <summary>Renders an embeddable HTML fragment with a deterministic ID scope.</summary>
    public string RenderFragment(VisualStory story, string idScope) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        return "<figure class=\"chartforgex-visual-story\">" +
               new SvgVisualStoryRenderer().Render(story, idScope) +
               "<figcaption class=\"chartforgex-visual-story-caption\">" +
               Escape(story.Description.Length == 0 ? story.Title : story.Description) +
               "</figcaption></figure>";
    }

    /// <summary>Renders a complete dependency-free HTML document.</summary>
    public string RenderPage(VisualStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
            .Append("<title>").Append(Escape(story.Title)).Append("</title>")
            .Append("<style>html{color-scheme:dark}body{margin:0;min-height:100vh;display:grid;place-items:center;background:#050b16;padding:24px;box-sizing:border-box}")
            .Append(".chartforgex-visual-story{width:min(1200px,100%);margin:0}.chartforgex-visual-story-caption{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}</style>")
            .Append("</head><body>").Append(RenderFragment(story)).Append("</body></html>");
        return html.ToString();
    }

    private static string Escape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
