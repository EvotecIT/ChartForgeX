using System;
using System.Globalization;
using System.Threading;
using ChartForgeX.Html;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual blocks as dependency-free static HTML.
/// </summary>
public sealed class HtmlVisualBlockRenderer {
    private static long ScopeCounter;
    private readonly SvgVisualBlockRenderer _svg = new();

    /// <summary>Renders a visual block as an embeddable HTML fragment.</summary>
    public string RenderFragment(IVisualBlock block) {
        VisualBlockRendering.Validate(block);
        return new HtmlMarkupWriter()
            .StartElement("div")
            .Attribute("class", "chartforgex-visual-block")
            .Attribute("style", "width:100%;max-width:" + block.Options.Size.Width.ToString(CultureInfo.InvariantCulture) + "px;box-sizing:border-box;overflow:visible")
            .EndStartElement()
            .RawTrusted(_svg.Render(block, NextScope()))
            .EndElement()
            .Build();
    }

    /// <summary>Renders a visual block as a complete HTML document.</summary>
    public string RenderPage(IVisualBlock block) {
        VisualBlockRendering.Validate(block);
        var theme = block.Options.Theme;
        var bg = block.Options.TransparentBackground ? theme.CardBackground : theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        var title = string.IsNullOrWhiteSpace(block.AccessibleName) ? "ChartForgeX visual block" : block.AccessibleName;
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, HtmlSurfacePolish.CenteredBodyCss(bg, VisualBlockRendering.CssFontFamily(theme.FontFamily)) + ".chartforgex-visual-block svg{max-width:100%;height:auto;display:block;overflow:visible}" + HtmlSurfacePolish.ResponsiveCenteredBodyCss + HtmlSurfacePolish.PrintBodyCss("0"));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(RenderFragment(block)).Line()
            .EndElement().Line()
            .EndElement();
        return writer.Build();
    }

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "html-visual-block-" + value.ToString(CultureInfo.InvariantCulture);
    }

}
