using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Themes;

namespace ChartForgeX.Stories;

/// <summary>Renders visual stories as embeddable HTML or complete HTML pages.</summary>
internal sealed class HtmlVisualStoryRenderer {
    /// <summary>Renders an embeddable HTML fragment.</summary>
    public string RenderFragment(VisualStory story) => RenderFragment(story, string.Empty);

    /// <summary>Renders an embeddable HTML fragment with a deterministic ID scope.</summary>
    public string RenderFragment(VisualStory story, string idScope) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        var auto = AutoTheme(story);
        var svg = new SvgVisualStoryRenderer().Render(story, idScope);
        return "<figure class=\"chartforgex-visual-story\"" + auto?.RootAttributes + ">" +
               auto?.StyleElement + (auto == null ? svg : auto.Variables.Apply(svg)) +
               "<figcaption class=\"chartforgex-visual-story-caption\">" +
               Escape(story.Description.Length == 0 ? story.Title : story.Description) +
               "</figcaption></figure>";
    }

    /// <summary>Renders a complete dependency-free HTML document.</summary>
    public string RenderPage(VisualStory story) {
        if (story == null) throw new ArgumentNullException(nameof(story));
        return story.Prepare().ToHtmlPage();
    }

    internal string RenderPage(VisualStory story, string animatedSvg) {
        var light = story.ThemeMode != ChartForgeX.ThemeMode.Dark;
        // An Auto page takes its background, gradient and text per mode from the scoped rule on its body.
        const string GradientEnd = "--cfx-auto-story-panel", Text = "--cfx-auto-story-text";
        var auto = AutoTheme(story)?.WithPageBackground(mode => ResolvedVisualStoryStyle.FromTheme(story.Theme, mode, story.Style).Background.ToCss())
            .WithRule("&", mode => {
                var resolved = ResolvedVisualStoryStyle.FromTheme(story.Theme, mode, story.Style);
                return GradientEnd + ":" + resolved.Panel.ToCss() + ";" + Text + ":" + resolved.Text.ToCss();
            });
        string Paint(string property, ChartColor color) => auto == null ? color.ToCss() : AutoThemeStyle.Var(property, color.ToCss());
        var style = story.ResolvedStyle;
        if (auto != null) animatedSvg = auto.Variables.Apply(animatedSvg);
        var html = new StringBuilder();
        html.Append("<!doctype html><html><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
            .Append("<title>").Append(Escape(story.Title)).Append("</title>")
            .Append("<style>html{color-scheme:").Append(auto != null ? "light dark" : light ? "light" : "dark")
            .Append("}body{margin:0;min-height:100vh;display:grid;place-items:center;background:")
            .Append(Paint(AutoThemeStyle.PageBackground, style.Background))
            .Append(";background-image:linear-gradient(180deg,")
            .Append(Paint(AutoThemeStyle.PageBackground, style.Background))
            .Append(",")
            .Append(Paint(GradientEnd, style.Panel))
            .Append(");color:")
            .Append(Paint(Text, style.Text))
            .Append(";padding:24px;box-sizing:border-box;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}")
            .Append(".chartforgex-visual-story{width:min(")
            .Append(story.Width.ToString(CultureInfo.InvariantCulture))
            .Append("px,100%);margin:0}.chartforgex-visual-story>svg{margin-inline:auto;overflow:visible}")
            .Append(".chartforgex-visual-story-caption{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0)}")
            .Append("@media print{html,body{background:transparent;background-image:none}.chartforgex-visual-story{box-shadow:none}}")
            .Append(auto?.StyleSheet).Append("</style>")
            .Append("</head><body").Append(auto?.RootAttributes)
            .Append("><figure class=\"chartforgex-visual-story\">").Append(animatedSvg)
            .Append("<figcaption class=\"chartforgex-visual-story-caption\">")
            .Append(Escape(story.Description.Length == 0 ? story.Title : story.Description))
            .Append("</figcaption></figure></body></html>");
        return html.ToString();
    }

    /// <summary>The Auto style of a story: theme colours plus the syntax colours its style derives for each mode.</summary>
    internal static AutoThemeStyle? AutoTheme(VisualStory story) {
        var auto = AutoThemeStyle.ForHtml(story.Theme, story.ThemeMode, null);
        if (auto == null) return null;
        var light = ResolvedVisualStoryStyle.FromTheme(story.Theme, ChartForgeX.ThemeMode.Light, story.Style).Syntax;
        var dark = ResolvedVisualStoryStyle.FromTheme(story.Theme, ChartForgeX.ThemeMode.Dark, story.Style).Syntax;
        var pairs = new[] { (light.Plain, dark.Plain), (light.Keyword, dark.Keyword), (light.Type, dark.Type), (light.Command, dark.Command),
            (light.Parameter, dark.Parameter), (light.Variable, dark.Variable), (light.Property, dark.Property), (light.String, dark.String),
            (light.Number, dark.Number), (light.Comment, dark.Comment), (light.Operator, dark.Operator), (light.Punctuation, dark.Punctuation) };
        for (var i = 0; i < pairs.Length; i++) auto.WithColor("story-syntax-" + i.ToString(CultureInfo.InvariantCulture), pairs[i].Item1, pairs[i].Item2);
        return auto;
    }

    private static string Escape(string value) {
        var escaped = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++) {
            var current = value[index];
            if (char.IsHighSurrogate(current) &&
                index + 1 < value.Length &&
                char.IsLowSurrogate(value[index + 1])) {
                var next = value[index + 1];
                if (SvgMarkupWriter.IsMarkupScalar(char.ConvertToUtf32(current, next))) {
                    escaped.Append(current).Append(next);
                } else {
                    escaped.Append('\uFFFD');
                }
                index++;
                continue;
            }
            if (!SvgMarkupWriter.IsMarkupScalar(current)) {
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
