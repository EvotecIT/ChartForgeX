using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Html;

/// <summary>
/// Renders charts as dependency-free HTML with inline SVG.
/// </summary>
public sealed partial class HtmlChartRenderer {
    /// <summary>
    /// Renders a chart as an embeddable HTML fragment.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>An HTML fragment containing inline SVG.</returns>
    public string RenderFragment(Chart chart) => RenderFragment(chart, string.Empty);

    /// <summary>
    /// Renders a chart as an embeddable HTML fragment with a caller-provided deterministic ID scope.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="idScope">A stable scope used to keep IDs unique when embedding equivalent fragments together.</param>
    /// <returns>An HTML fragment containing inline SVG.</returns>
    public string RenderFragment(Chart chart, string idScope) => RenderFragment(chart, idScope, constrainMaxWidth: true);

    private string RenderFragment(Chart chart, string idScope, bool constrainMaxWidth) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (idScope == null) throw new ArgumentNullException(nameof(idScope));
        var prefix = VisualSvgOptions.NamespaceFromExternalId(idScope);
        return RenderFragment(chart, new VisualSvgOptions(prefix, chart.Options.SvgColorVariables, responsive: true), ChartDataTableMode.None, constrainMaxWidth);
    }

    /// <summary>Renders inline SVG with an optional accessible table of authored display observations.</summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="svgOptions">The SVG export policy, including its deterministic ID scope.</param>
    /// <param name="dataTable">Whether data tables are omitted, disclosed in details, or visually hidden on screen and visible in print.</param>
    /// <returns>An HTML fragment containing SVG and, when requested, one captioned table per series.</returns>
    /// <exception cref="NotSupportedException">A requested table contains a typed series without a table representation.</exception>
    public string RenderFragment(Chart chart, VisualSvgOptions svgOptions, ChartDataTableMode dataTable = ChartDataTableMode.None) =>
        RenderFragment(chart, svgOptions, dataTable, constrainMaxWidth: true);

    private string RenderFragment(Chart chart, VisualSvgOptions svgOptions, ChartDataTableMode dataTable, bool constrainMaxWidth) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (svgOptions == null) throw new ArgumentNullException(nameof(svgOptions));
        if (dataTable < ChartDataTableMode.None || dataTable > ChartDataTableMode.VisuallyHidden) throw new ArgumentOutOfRangeException(nameof(dataTable));
        var request = VisualExportRequest.ForChart(chart);
        var prepared = chart.Prepare(request.Context);
        var svg = prepared.ToSvg(svgOptions);
        var style = (constrainMaxWidth ? "width:100%;max-width:" + chart.Options.Size.Width.ToString(CultureInfo.InvariantCulture) + "px;" : string.Empty) + "box-sizing:border-box;overflow:visible";
        return new HtmlMarkupWriter()
            .StartElement("div")
            .Attribute("class", "chartforgex-chart")
            .Attribute("style", style)
            .EndStartElement()
            .RawTrusted(svg)
            .RawTrusted(dataTable == ChartDataTableMode.None ? null : RenderDataTables(chart, dataTable))
            .EndElement()
            .Build();
    }

    /// <summary>
    /// Renders a chart as a complete HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var theme = chart.Options.ResolvedTheme;
        var bg = HtmlSurfacePolish.PageBackground(chart.Options.TransparentBackground ? theme.CardBackground : theme.Background, theme.CardBackground);
        var title = string.IsNullOrWhiteSpace(chart.Title) ? chart.Options.Labels.UntitledChart : chart.Title;
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", chart.Accessibility.Language ?? "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        WriteDocumentHead(writer, title, HtmlSurfacePolish.CenteredBodyCss(bg, CssFontFamily(chart.Options.ResolvedTheme.FontFamily), true) + ".chartforgex-chart{width:min(100%," + chart.Options.Size.Width.ToString(CultureInfo.InvariantCulture) + "px);box-sizing:border-box;overflow:visible}.chartforgex-chart svg{max-width:100%;height:auto;display:block;overflow:visible}" + HtmlSurfacePolish.ResponsiveCenteredBodyCss + HtmlSurfacePolish.PrintBodyCss("0", ".chartforgex-chart{width:100%;max-width:none}.chartforgex-chart svg{width:100%;height:auto}"));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(RenderFragment(chart, "html-page", constrainMaxWidth: false)).Line()
            .EndElement().Line()
            .EndElement();
        return writer.Build();
    }

    internal static void WriteDocumentHead(HtmlMarkupWriter writer, string title, string? css) {
        writer.StartElement("meta").Attribute("charset", "utf-8").EndVoidElement().Line()
            .StartElement("meta").Attribute("name", "viewport").Attribute("content", "width=device-width, initial-scale=1").EndVoidElement().Line()
            .StartElement("title").EndStartElement().Text(title).EndElement().Line();
        if (css != null) writer.StartElement("style").EndStartElement().RawTrusted(css).EndElement().Line();
    }

    internal static string CssFontFamily(string value) {
        if (string.IsNullOrWhiteSpace(value)) return "system-ui, sans-serif";
        return value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");
    }

}
