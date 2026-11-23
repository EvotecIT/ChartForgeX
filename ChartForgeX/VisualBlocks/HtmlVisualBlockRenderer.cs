using System;
using System.Globalization;
using ChartForgeX.Html;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual blocks as dependency-free static HTML.
/// </summary>
internal sealed class HtmlVisualBlockRenderer {
    private readonly SvgVisualBlockRenderer _svg = new();

    /// <summary>Renders a visual block as an embeddable HTML fragment.</summary>
    public string RenderFragment(IVisualBlock block) => RenderFragment(block, string.Empty);

    /// <summary>Renders a visual block fragment with a caller-provided deterministic ID scope.</summary>
    /// <param name="block">The visual block to render.</param>
    /// <param name="idScope">A stable scope used to keep IDs unique when embedding equivalent fragments together.</param>
    /// <returns>An HTML fragment containing inline SVG.</returns>
    public string RenderFragment(IVisualBlock block, string idScope) => RenderFragment(block, idScope, constrainMaxWidth: true);

    private string RenderFragment(IVisualBlock block, string idScope, bool constrainMaxWidth) {
        VisualBlockRendering.Validate(block);
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        var auto = AutoThemeStyle.ForHtml(block.Options.Theme, block.Options.ThemeMode, block.Options.SvgColorVariables);
        var svg = _svg.Render(block, idScope);
        return new HtmlMarkupWriter()
            .StartElement("div")
            .Attribute("class", "chartforgex-visual-block").Attribute(ChartForgeX.Themes.AutoThemeStyle.ScopeAttribute, auto?.Id)
            .Attribute(AutoThemeStyle.Attribute, auto == null ? null : AutoThemeStyle.AttributeValue)
            .Attribute("style", (constrainMaxWidth ? "width:100%;max-width:" + block.Options.Size.Width.ToString(CultureInfo.InvariantCulture) + "px;" : string.Empty) + "box-sizing:border-box;overflow:visible")
            .EndStartElement()
            .RawTrusted(auto?.StyleElement)
            // Blocks map host colours by value after rendering (see VisualBlockOptions.SvgColorVariables); Auto does the same.
            .RawTrusted(auto == null ? svg : auto.Variables.Apply(svg))
            .EndElement()
            .Build();
    }

    /// <summary>Renders a visual block as a complete HTML document.</summary>
    public string RenderPage(IVisualBlock block) {
        VisualBlockRendering.Validate(block);
        var theme = block.Options.ResolvedTheme;
        string Background(ResolvedVisualTheme resolved) => HtmlSurfacePolish.BackgroundCss(HtmlSurfacePolish.PageBackground(
            block.Options.TransparentBackground ? resolved.CardBackground : resolved.Background, resolved.CardBackground), flat: true);
        var page = AutoThemeStyle.ForHtml(block.Options.Theme, block.Options.ThemeMode, block.Options.SvgColorVariables)?
            .WithPageBackground(mode => Background(block.Options.Theme.ResolvePresentation(mode)));
        var bg = page == null ? Background(theme) : AutoThemeStyle.Var(AutoThemeStyle.PageBackground, Background(theme));
        var title = string.IsNullOrWhiteSpace(block.AccessibleName) ? "ChartForgeX visual block" : block.AccessibleName;
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", block.Options.Accessibility.Language ?? "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, HtmlSurfacePolish.CenteredBodyCss(bg, VisualBlockRendering.CssFontFamily(theme.FontFamily)) + ".chartforgex-visual-block{width:min(100%," + block.Options.Size.Width.ToString(CultureInfo.InvariantCulture) + "px);box-sizing:border-box;overflow:visible}.chartforgex-visual-block svg{max-width:100%;height:auto;display:block;overflow:visible}" + HtmlSurfacePolish.ResponsiveCenteredBodyCss + HtmlSurfacePolish.PrintBodyCss("0", ".chartforgex-visual-block{width:100%;max-width:none}.chartforgex-visual-block svg{width:100%;height:auto}") + page?.StyleSheet);
        writer.EndElement().Line()
            .StartElement("body").Attribute(ChartForgeX.Themes.AutoThemeStyle.ScopeAttribute, page?.Id).Attribute(AutoThemeStyle.Attribute, page == null ? null : AutoThemeStyle.AttributeValue).EndStartElement().Line()
            .RawTrusted(RenderFragment(block, "html-page", constrainMaxWidth: false)).Line()
            .EndElement().Line()
            .EndElement();
        return writer.Build();
    }

}
