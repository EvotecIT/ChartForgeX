using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

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
        var light = ChartColorMath.RelativeLuminance(story.Theme.Background) > 0.5;
        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
            .Append("<title>").Append(Escape(story.Title)).Append("</title>")
            .Append("<style>html{color-scheme:").Append(light ? "light" : "dark")
            .Append("}body{margin:0;min-height:100vh;display:grid;place-items:center;background:")
            .Append(story.Theme.Background.ToCss())
            .Append(";background-image:linear-gradient(180deg,")
            .Append(story.Theme.Background.ToCss())
            .Append(",")
            .Append(story.Theme.Panel.ToCss())
            .Append(");color:")
            .Append(story.Theme.Text.ToCss())
            .Append(";padding:24px;box-sizing:border-box;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}")
            .Append(".chartforgex-visual-story{width:min(")
            .Append(story.Width.ToString(CultureInfo.InvariantCulture))
            .Append("px,100%);margin:0}.chartforgex-visual-story>svg{margin-inline:auto;overflow:visible}")
            .Append(".chartforgex-visual-story-caption{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}")
            .Append("@media print{html,body{background:transparent;background-image:none}.chartforgex-visual-story{box-shadow:none}}</style>")
            .Append("</head><body>").Append(RenderFragment(story)).Append("</body></html>");
        return html.ToString();
    }

    private static string Escape(string value) {
        var escaped = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++) {
            var current = value[index];
            if (char.IsHighSurrogate(current) &&
                index + 1 < value.Length &&
                char.IsLowSurrogate(value[index + 1])) {
                escaped.Append(current).Append(value[++index]);
                continue;
            }
            if (char.IsSurrogate(current) || !SvgMarkupWriter.IsXmlCharacter(current)) {
                escaped.Append('\uFFFD');
                continue;
            }
            switch (current) {
                case '&':
                    escaped.Append("&amp;");
                    break;
                case '<':
                    escaped.Append("&lt;");
                    break;
                case '>':
                    escaped.Append("&gt;");
                    break;
                case '"':
                    escaped.Append("&quot;");
                    break;
                default:
                    escaped.Append(current);
                    break;
            }
        }
        return escaped.ToString();
    }
}
