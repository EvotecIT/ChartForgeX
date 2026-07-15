using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

/// <summary>
/// Renders chart grids to self-contained SVG.
/// </summary>
public sealed class SvgChartGridRenderer {
    private readonly SvgChartRenderer _chartRenderer = new();

    /// <summary>
    /// Renders a chart grid to SVG markup.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>SVG markup.</returns>
    public string Render(ChartGrid grid) => Render(grid, string.Empty);

    /// <summary>
    /// Renders a chart grid to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVG grids in one document.</param>
    /// <returns>SVG markup.</returns>
    public string Render(ChartGrid grid, string idScope) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var provisionalId = SvgRenderedIdentity.CreateProvisionalId("cfx-grid", idScope, grid.Title, grid.Charts.Count.ToString(CultureInfo.InvariantCulture));
        var svg = RenderCore(grid, provisionalId);
        return SvgRenderedIdentity.Bind(svg, provisionalId, "cfx-grid", idScope);
    }

    private string RenderCore(ChartGrid grid, string id) {
        var layout = ChartGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var background = theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("id", id)
            .Attribute("width", layout.Width)
            .Attribute("height", layout.Height)
            .Attribute("viewBox", "0 0 " + layout.Width.ToString(CultureInfo.InvariantCulture) + " " + layout.Height.ToString(CultureInfo.InvariantCulture))
            .Attribute("role", "img")
            .Attribute("aria-labelledby", id + "-title " + id + "-desc")
            .Attribute("preserveAspectRatio", "xMidYMid meet")
            .Attribute("shape-rendering", "geometricPrecision")
            .Attribute("text-rendering", "geometricPrecision")
            .Attribute("style", "max-width:100%;height:auto;display:block")
            .EndStartElement()
            .Line()
            .StartElement("title")
            .Attribute("id", id + "-title")
            .Text(grid.Title.Length == 0 ? "ChartForgeX chart grid" : grid.Title)
            .EndElement()
            .Line()
            .StartElement("desc")
            .Attribute("id", id + "-desc")
            .Text("Static chart grid containing " + grid.Charts.Count.ToString(CultureInfo.InvariantCulture) + " charts.")
            .EndElement()
            .Line()
            .StartElement("defs")
            .EndStartElement()
            .Line();
        SvgSurfacePolish.WriteScopedStrokeStyle(writer, id);
        SvgSurfacePolish.WriteSurfaceGradient(writer, id, "gridSurface", background);
        writer.EndElement()
            .Line()
            .StartElement("rect")
            .Attribute("width", "100%")
            .Attribute("height", "100%")
            .Attribute("fill", background.A == 0 ? "transparent" : "url(#" + id + "-gridSurface)")
            .EndEmptyElement()
            .Line();
        if (layout.HeaderHeight > 0) {
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            var titleFontSize = StyleFontSize(grid.TitleStyle, theme.TitleFontSize);
            var subtitleFontSize = StyleFontSize(grid.SubtitleStyle, theme.SubtitleFontSize);
            if (grid.Title.Length > 0) WriteGridText(writer, "grid-title", grid.Padding, grid.Padding + titleFontSize * 0.62, StyleColor(grid.TitleStyle, theme.Text).ToCss(), StyleFontFamily(grid.TitleStyle, theme.FontFamily), titleFontSize, StyleWeight(grid.TitleStyle, "800"), grid.TitleStyle, ChartTextFitting.TrimEnd(grid.Title, titleFontSize, headerWidth, EstimateTextWidth));
            if (grid.Subtitle.Length > 0) WriteGridText(writer, "grid-subtitle", grid.Padding + 2, grid.Padding + titleFontSize + subtitleFontSize, StyleColor(grid.SubtitleStyle, theme.MutedText).ToCss(), StyleFontFamily(grid.SubtitleStyle, theme.FontFamily), subtitleFontSize, StyleWeight(grid.SubtitleStyle, "400"), grid.SubtitleStyle, ChartTextFitting.TrimEnd(grid.Subtitle, subtitleFontSize, headerWidth, EstimateTextWidth));
        }

        for (var i = 0; i < layout.Cells.Count; i++) {
            var cell = layout.Cells[i];
            var childSvg = _chartRenderer.Render(cell.Chart, id + "-cell-" + i.ToString(CultureInfo.InvariantCulture));
            writer.Raw(PositionChildSvg(childSvg, cell.X, cell.Y, cell.Width, cell.Height)).Line();
        }

        writer.EndElement().Line();
        return writer.Build();
    }

    private static void WriteGridText(SvgMarkupWriter writer, string role, double x, double y, string fill, string fontFamily, double fontSize, string fontWeight, TextStyleOverride style, string text) {
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", role)
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("fill", fill)
            .Attribute("font-family", fontFamily)
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", fontWeight)
            .Raw(SvgTextStyleAttributes(style))
            .Text(text)
            .EndElement()
            .Line();
    }

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static ChartColor StyleColor(TextStyleOverride style, ChartColor fallback) => style.Color ?? fallback;

    private static double StyleFontSize(TextStyleOverride style, double fallback) => style.FontSize ?? fallback;

    private static string StyleFontFamily(TextStyleOverride style, string fallback) => style.FontFamily ?? fallback;

    private static string StyleWeight(TextStyleOverride style, string fallback) => style.FontWeight ?? fallback;

    private static string SvgTextStyleAttributes(TextStyleOverride style) {
        var value = string.Empty;
        if (style.Italic) value += " font-style=\"italic\"";
        if (style.Underline) value += " text-decoration=\"underline\"";
        return value;
    }

    private static string PositionChildSvg(string svg, double x, double y, double width, double height) {
        var tagEnd = svg.IndexOf('>');
        if (tagEnd < 0) return svg;
        var open = svg.Substring(0, tagEnd);
        open = SetSvgAttribute(open, "x", x.ToString(CultureInfo.InvariantCulture));
        open = SetSvgAttribute(open, "y", y.ToString(CultureInfo.InvariantCulture));
        open = SetSvgAttribute(open, "width", width.ToString(CultureInfo.InvariantCulture));
        open = SetSvgAttribute(open, "height", height.ToString(CultureInfo.InvariantCulture));
        open = SetSvgAttribute(open, "data-cfx-role", "grid-panel");
        return open + svg.Substring(tagEnd);
    }

    private static string SetSvgAttribute(string openTag, string name, string value) {
        var attribute = " " + name + "=\"";
        var start = openTag.IndexOf(attribute, StringComparison.Ordinal);
        if (start < 0) return openTag + attribute + Escape(value) + "\"";
        var valueStart = start + attribute.Length;
        var valueEnd = openTag.IndexOf('"', valueStart);
        if (valueEnd < 0) return openTag;
        return openTag.Substring(0, valueStart) + Escape(value) + openTag.Substring(valueEnd);
    }

    private static double EstimateTextWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? 0.32 : char.IsUpper(ch) || char.IsDigit(ch) ? 0.62 : 0.54;
        return width * fontSize;
    }

}
