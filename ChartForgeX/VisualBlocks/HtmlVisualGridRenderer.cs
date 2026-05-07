using System;
using System.Globalization;
using System.Text;
using System.Threading;
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
        var sb = new StringBuilder();
        sb.Append("<section class=\"chartforgex-visual-grid");
        if (grid.PanelFit == VisualGridPanelFit.Stretch) sb.Append(" fit-stretch");
        sb.Append("\" style=\"--cfx-visual-grid-columns:").Append(grid.Columns.ToString(CultureInfo.InvariantCulture));
        sb.Append(";--cfx-visual-grid-gap:").Append(grid.Gap.ToString(CultureInfo.InvariantCulture)).Append("px");
        sb.Append(";--cfx-visual-grid-padding:").Append(grid.Padding.ToString(CultureInfo.InvariantCulture)).Append("px");
        if (grid.PanelSize.HasValue) {
            sb.Append(";--cfx-visual-grid-panel-width:").Append(grid.PanelSize.Value.Width.ToString(CultureInfo.InvariantCulture)).Append("px");
            sb.Append(";--cfx-visual-grid-panel-height:").Append(grid.PanelSize.Value.Height.ToString(CultureInfo.InvariantCulture)).Append("px");
        }

        sb.Append("\">");
        if (grid.Title.Length > 0 || grid.Subtitle.Length > 0) {
            sb.Append("<header class=\"chartforgex-visual-grid-header\">");
            if (grid.Title.Length > 0) sb.Append("<h1>").Append(VisualBlockRendering.Escape(grid.Title)).Append("</h1>");
            if (grid.Subtitle.Length > 0) sb.Append("<p>").Append(VisualBlockRendering.Escape(grid.Subtitle)).Append("</p>");
            sb.Append("</header>");
        }

        sb.Append("<div class=\"chartforgex-visual-grid-body\">");
        for (var i = 0; i < grid.Items.Count; i++) {
            var item = grid.Items[i];
            var columnSpan = Math.Min(item.ColumnSpan, grid.Columns);
            sb.Append("<article class=\"chartforgex-visual-grid-panel\" aria-label=\"").Append(VisualBlockRendering.Escape(ItemTitle(item))).Append("\"");
            if (columnSpan > 1 || item.RowSpan > 1) sb.Append(" style=\"grid-column:span ").Append(columnSpan.ToString(CultureInfo.InvariantCulture)).Append(";grid-row:span ").Append(item.RowSpan.ToString(CultureInfo.InvariantCulture)).Append("\"");
            sb.Append(">");
            sb.Append(item.Chart != null ? _chartRenderer.Render(item.Chart, scope + "-chart-" + i.ToString(CultureInfo.InvariantCulture)) : _blockRenderer.Render(item.Block!, scope + "-block-" + i.ToString(CultureInfo.InvariantCulture)));
            sb.Append("</article>");
        }

        sb.Append("</div></section>");
        return sb.ToString();
    }

    /// <summary>Renders a visual grid as a complete HTML document.</summary>
    public string RenderPage(VisualGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Items.Count == 0) throw new InvalidOperationException("Visual grids must contain at least one item.");
        var theme = grid.Theme ?? VisualGridLayout.ItemTheme(grid.Items[0]);
        var background = theme.Background.A == 0 ? theme.CardBackground.ToCss() : theme.Background.ToCss();
        var title = grid.Title.Length == 0 ? "ChartForgeX visual grid" : grid.Title;
        return "<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>" + VisualBlockRendering.Escape(title) + "</title>\n<style>" + BuildCss(background, theme.Text.ToCss(), theme.MutedText.ToCss(), VisualBlockRendering.CssFontFamily(theme.FontFamily), theme.TitleFontSize, theme.SubtitleFontSize) + "</style>\n</head>\n<body>\n" + RenderFragment(grid) + "\n</body>\n</html>";
    }

    private static string BuildCss(string background, string text, string mutedText, string fontFamily, double titleFontSize, double subtitleFontSize) {
        return "body{margin:0;min-height:100vh;background:" + background + ";font-family:" + fontFamily + ";padding:var(--cfx-visual-grid-padding,24px);box-sizing:border-box;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-visual-grid{display:block;width:min(100%,1440px);margin:0 auto}.chartforgex-visual-grid-header{margin:0 0 18px}.chartforgex-visual-grid-header h1{margin:0;color:" + text + ";font-size:" + titleFontSize.ToString(CultureInfo.InvariantCulture) + "px;line-height:1.15;font-weight:800}.chartforgex-visual-grid-header p{margin:6px 0 0;color:" + mutedText + ";font-size:" + subtitleFontSize.ToString(CultureInfo.InvariantCulture) + "px;line-height:1.45}.chartforgex-visual-grid-body{display:grid;grid-template-columns:repeat(var(--cfx-visual-grid-columns),minmax(0,1fr));grid-auto-rows:var(--cfx-visual-grid-panel-height,auto);gap:var(--cfx-visual-grid-gap)}.chartforgex-visual-grid-panel{min-width:0;width:100%;min-height:var(--cfx-visual-grid-panel-height,auto);display:grid;place-items:center;overflow:hidden}.chartforgex-visual-grid-panel svg{width:auto;height:auto;max-width:100%;max-height:100%;display:block}.chartforgex-visual-grid.fit-stretch .chartforgex-visual-grid-panel svg{width:100%;height:100%;max-width:none;max-height:none}@media(max-width:900px){body{padding:16px}.chartforgex-visual-grid-body{grid-template-columns:1fr;grid-auto-rows:auto}.chartforgex-visual-grid-panel{grid-column:auto!important;grid-row:auto!important;min-height:0}.chartforgex-visual-grid-header h1{font-size:" + Math.Max(18, titleFontSize * 0.85).ToString(CultureInfo.InvariantCulture) + "px}}";
    }

    private static string ItemTitle(VisualGridItem item) {
        if (item.Chart != null) return item.Chart.Title.Length == 0 ? "Chart" : item.Chart.Title;
        return item.Block?.AccessibleName ?? "Visual block";
    }

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "html-visual-grid-" + value.ToString(CultureInfo.InvariantCulture);
    }
}
