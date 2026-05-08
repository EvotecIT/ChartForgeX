using System;
using System.Globalization;
using System.Text;
using System.Threading;
using ChartForgeX.Html;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual grids as dependency-free static HTML.
/// </summary>
public sealed class HtmlVisualGridRenderer {
    private static long ScopeCounter;
    private readonly SvgChartRenderer _chartRenderer = new();
    private readonly SvgVisualBlockRenderer _blockRenderer = new();

    /// <summary>Renders a visual grid as an embeddable HTML fragment.</summary>
    public string RenderFragment(VisualGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Items.Count == 0) throw new InvalidOperationException("Visual grids must contain at least one item.");
        var scope = NextScope();
        var layout = VisualGridLayout.FromGrid(grid);
        var columns = Math.Min(grid.Columns, grid.Items.Count);
        var writer = new HtmlMarkupWriter();
        writer.StartElement("section")
            .Attribute("class", GridClass(grid))
            .Attribute("style", GridStyle(grid, layout, columns))
            .EndStartElement();
        if (grid.Title.Length > 0 || grid.Subtitle.Length > 0) {
            writer.StartElement("header").Attribute("class", "chartforgex-visual-grid-header").EndStartElement();
            if (grid.Title.Length > 0) writer.StartElement("h1").EndStartElement().Text(grid.Title).EndElement();
            if (grid.Subtitle.Length > 0) writer.StartElement("p").EndStartElement().Text(grid.Subtitle).EndElement();
            writer.EndElement();
        }

        writer.StartElement("div").Attribute("class", "chartforgex-visual-grid-body").EndStartElement();
        for (var i = 0; i < grid.Items.Count; i++) {
            var item = grid.Items[i];
            var columnSpan = Math.Min(item.ColumnSpan, columns);
            var childSvg = item.Chart != null ? _chartRenderer.Render(item.Chart, scope + "-chart-" + i.ToString(CultureInfo.InvariantCulture)) : _blockRenderer.Render(item.Block!, scope + "-block-" + i.ToString(CultureInfo.InvariantCulture));
            writer.StartElement("article")
                .Attribute("class", "chartforgex-visual-grid-panel")
                .Attribute("aria-label", ItemTitle(item))
                .Attribute("style", PanelSpanStyle(columnSpan, item.RowSpan))
                .EndStartElement()
                .RawTrusted(PrepareChildSvg(childSvg, grid.PanelSize.HasValue && grid.PanelFit == VisualGridPanelFit.Stretch))
                .EndElement();
        }

        writer.EndElement().EndElement();
        return writer.Build();
    }

    /// <summary>Renders a visual grid as a complete HTML document.</summary>
    public string RenderPage(VisualGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Items.Count == 0) throw new InvalidOperationException("Visual grids must contain at least one item.");
        var theme = grid.Theme ?? VisualGridLayout.ItemTheme(grid.Items[0]);
        var background = theme.Background.A == 0 ? theme.CardBackground.ToCss() : theme.Background.ToCss();
        var title = grid.Title.Length == 0 ? "ChartForgeX visual grid" : grid.Title;
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, BuildCss(background, theme.Text.ToCss(), theme.MutedText.ToCss(), VisualBlockRendering.CssFontFamily(theme.FontFamily), theme.TitleFontSize, theme.SubtitleFontSize));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(RenderFragment(grid)).Line()
            .EndElement().Line()
            .EndElement();
        return writer.Build();
    }

    private static string BuildCss(string background, string text, string mutedText, string fontFamily, double titleFontSize, double subtitleFontSize) {
        return "body{margin:0;min-height:100vh;background:" + background + ";font-family:" + fontFamily + ";padding:0;box-sizing:border-box;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-visual-grid{display:block;width:min(100%,1440px);margin:0 auto;padding:var(--cfx-visual-grid-padding,24px);box-sizing:border-box}.chartforgex-visual-grid-header{margin:0 0 18px}.chartforgex-visual-grid-header h1{margin:0;color:" + text + ";font-size:" + titleFontSize.ToString(CultureInfo.InvariantCulture) + "px;line-height:1.15;font-weight:800}.chartforgex-visual-grid-header p{margin:6px 0 0;color:" + mutedText + ";font-size:" + subtitleFontSize.ToString(CultureInfo.InvariantCulture) + "px;line-height:1.45}.chartforgex-visual-grid-body{display:grid;grid-template-columns:repeat(var(--cfx-visual-grid-columns),var(--cfx-visual-grid-panel-width,minmax(0,1fr)));grid-auto-rows:var(--cfx-visual-grid-panel-height,auto);grid-auto-flow:row dense;gap:var(--cfx-visual-grid-gap)}.chartforgex-visual-grid-panel{min-width:0;width:100%;min-height:var(--cfx-visual-grid-panel-height,auto);display:grid;place-items:center;overflow:hidden}.chartforgex-visual-grid-panel svg{width:auto;height:auto;max-width:100%;max-height:100%;display:block}.chartforgex-visual-grid.has-fixed-panels .chartforgex-visual-grid-panel svg{width:100%;height:100%}.chartforgex-visual-grid.has-fixed-panels.fit-stretch .chartforgex-visual-grid-panel svg{width:100%;height:100%;max-width:none;max-height:none}@media(max-width:900px){.chartforgex-visual-grid{padding:16px}.chartforgex-visual-grid-body{grid-template-columns:1fr;grid-auto-rows:auto}.chartforgex-visual-grid-panel{grid-column:auto!important;grid-row:auto!important;min-height:0}.chartforgex-visual-grid-header h1{font-size:" + Math.Max(18, titleFontSize * 0.85).ToString(CultureInfo.InvariantCulture) + "px}}";
    }

    private static string ItemTitle(VisualGridItem item) {
        if (item.Chart != null) return item.Chart.Title.Length == 0 ? "Chart" : item.Chart.Title;
        return item.Block?.AccessibleName ?? "Visual block";
    }

    private static string GridClass(VisualGrid grid) {
        var value = "chartforgex-visual-grid";
        if (grid.PanelSize.HasValue) value += " has-fixed-panels";
        if (grid.PanelSize.HasValue && grid.PanelFit == VisualGridPanelFit.Stretch) value += " fit-stretch";
        return value;
    }

    private static string GridStyle(VisualGrid grid, VisualGridLayout layout, int columns) {
        var sb = new StringBuilder();
        sb.Append("--cfx-visual-grid-columns:").Append(columns.ToString(CultureInfo.InvariantCulture));
        sb.Append(";--cfx-visual-grid-gap:").Append(grid.Gap.ToString(CultureInfo.InvariantCulture)).Append("px");
        sb.Append(";--cfx-visual-grid-padding:").Append(grid.Padding.ToString(CultureInfo.InvariantCulture)).Append("px");
        sb.Append(";--cfx-visual-grid-panel-width:").Append(layout.PanelWidth.ToString(CultureInfo.InvariantCulture)).Append("px");
        sb.Append(";--cfx-visual-grid-panel-height:").Append(layout.PanelHeight.ToString(CultureInfo.InvariantCulture)).Append("px");

        return sb.ToString();
    }

    private static string? PanelSpanStyle(int columnSpan, int rowSpan) {
        if (columnSpan == 1 && rowSpan == 1) return null;
        return "grid-column:span " + columnSpan.ToString(CultureInfo.InvariantCulture) + ";grid-row:span " + rowSpan.ToString(CultureInfo.InvariantCulture);
    }

    private static string PrepareChildSvg(string svg, bool stretch) {
        if (!stretch) return svg;
        var tagEnd = svg.IndexOf('>');
        if (tagEnd < 0) return svg;
        var open = svg.Substring(0, tagEnd);
        open = SetSvgAttribute(open, "preserveAspectRatio", "none");
        return open + svg.Substring(tagEnd);
    }

    private static string SetSvgAttribute(string openTag, string name, string value) {
        var attribute = " " + name + "=\"";
        var start = openTag.IndexOf(attribute, StringComparison.Ordinal);
        if (start < 0) return openTag + attribute + VisualBlockRendering.Escape(value) + "\"";
        var valueStart = start + attribute.Length;
        var valueEnd = openTag.IndexOf('"', valueStart);
        if (valueEnd < 0) return openTag;
        return openTag.Substring(0, valueStart) + VisualBlockRendering.Escape(value) + openTag.Substring(valueEnd);
    }

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "html-visual-grid-" + value.ToString(CultureInfo.InvariantCulture);
    }
}
