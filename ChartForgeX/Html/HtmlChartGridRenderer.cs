using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Html;

/// <summary>Embeds a prepared chart comparison in dependency-free static HTML.</summary>
public sealed class HtmlChartGridRenderer {
    /// <summary>Renders a chart grid as an embeddable HTML fragment containing its prepared SVG.</summary>
    public string RenderFragment(ChartGrid grid) => RenderFragment(grid, string.Empty);

    /// <summary>Renders a chart grid fragment with a deterministic host ID scope.</summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="idScope">A stable scope used to keep IDs unique when embedding equivalent fragments together.</param>
    /// <returns>An HTML fragment containing the complete prepared chart comparison.</returns>
    public string RenderFragment(ChartGrid grid, string idScope) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        var request = VisualExportRequest.ForGrid(grid);
        var prepared = grid.Prepare(request.Context);
        var prefix = VisualSvgOptions.NamespaceFromExternalId(idScope);
        return new HtmlMarkupWriter().StartElement("section").Attribute("class", "chartforgex-grid")
            .Attribute("style", "width:100%;max-width:" + prepared.Size.Width.ToString(CultureInfo.InvariantCulture) + "px;box-sizing:border-box")
            .EndStartElement().RawTrusted(prepared.ToSvg(new VisualSvgOptions(prefix, grid.SvgColorVariables, responsive: true))).EndElement().Build();
    }

    /// <summary>Renders a complete script-free HTML document with the same grid layout as static exports.</summary>
    public string RenderPage(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var request = VisualExportRequest.ForGrid(grid);
        var colors = request.Context.Theme.Resolve(request.Context.ThemeMode);
        var title = grid.Title.Length == 0 ? "ChartForgeX report" : grid.Title;
        var css = HtmlSurfacePolish.CenteredBodyCss(colors.Background,
            HtmlChartRenderer.CssFontFamily(request.Context.Font.Family), flat: true)
            + ".chartforgex-grid{margin:0 auto}.chartforgex-grid svg{width:100%;height:auto;display:block}"
            + HtmlSurfacePolish.ResponsiveCenteredBodyCss
            + HtmlSurfacePolish.PrintBodyCss("0", ".chartforgex-grid{max-width:none}");
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line().StartElement("html").Attribute("lang", grid.Accessibility.Language ?? "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, css);
        writer.EndElement().Line().StartElement("body").EndStartElement().Line()
            .RawTrusted(RenderFragment(grid, "html-page")).Line().EndElement().Line().EndElement();
        return writer.Build();
    }
}
