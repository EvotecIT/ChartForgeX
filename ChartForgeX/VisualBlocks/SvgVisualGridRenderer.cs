using System;
using System.Globalization;
using System.Threading;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Renders visual grids to self-contained SVG.
/// </summary>
public sealed class SvgVisualGridRenderer {
    private static long ScopeCounter;
    private readonly SvgChartRenderer _chartRenderer = new();
    private readonly SvgVisualBlockRenderer _blockRenderer = new();

    /// <summary>Renders a visual grid to SVG markup.</summary>
    public string Render(VisualGrid grid) => Render(grid, NextScope());

    /// <summary>Renders a visual grid to SVG markup with a caller-provided ID scope.</summary>
    public string Render(VisualGrid grid, string idScope) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = VisualGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? VisualGridLayout.ItemTheme(grid.Items[0]);
        var id = "cfx-visual-grid-" + VisualBlockRendering.StableHash(idScope ?? string.Empty, grid.Title, grid.Items.Count.ToString(CultureInfo.InvariantCulture));
        var writer = new SvgMarkupWriter(4096);
        writer.StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
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
            .StartElement("title").Attribute("id", id + "-title").Text(grid.Title.Length == 0 ? "ChartForgeX visual grid" : grid.Title).EndElement()
            .Line()
            .StartElement("desc").Attribute("id", id + "-desc").Text("Static visual grid containing charts and visual blocks.").EndElement()
            .Line();
        var background = theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        if (background.A > 0) writer.StartElement("rect").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", background.ToCss()).EndEmptyElement().Line();
        if (grid.FrameVisible) {
            var inset = Math.Max(8, grid.Padding * 0.5);
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "visual-grid-frame")
                .Attribute("x", inset)
                .Attribute("y", inset)
                .Attribute("width", Math.Max(1, layout.Width - inset * 2))
                .Attribute("height", Math.Max(1, layout.Height - inset * 2))
                .Attribute("rx", Math.Max(theme.CornerRadius, 26))
                .Attribute("fill", "none")
                .Attribute("stroke", theme.CardBorder.ToCss())
                .Attribute("stroke-width", 1.4)
                .EndEmptyElement()
                .Line();
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "visual-grid-frame-highlight")
                .Attribute("x", inset + 1.5)
                .Attribute("y", inset + 1.5)
                .Attribute("width", Math.Max(1, layout.Width - inset * 2 - 3))
                .Attribute("height", Math.Max(1, layout.Height - inset * 2 - 3))
                .Attribute("rx", Math.Max(theme.CornerRadius - 1.5, 24))
                .Attribute("fill", "none")
                .Attribute("stroke", "#fff")
                .Attribute("stroke-opacity", 0.42)
                .Attribute("stroke-width", 1)
                .EndEmptyElement()
                .Line();
        }
        if (layout.HeaderHeight > 0) {
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            if (grid.Title.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "visual-grid-title").Attribute("x", grid.Padding).Attribute("y", grid.Padding + theme.TitleFontSize * 0.75).Attribute("fill", theme.Text.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", theme.TitleFontSize).Attribute("font-weight", "800").Text(VisualBlockRendering.FitText(grid.Title, theme.TitleFontSize, headerWidth)).EndElement().Line();
            if (grid.Subtitle.Length > 0) writer.StartElement("text").Attribute("data-cfx-role", "visual-grid-subtitle").Attribute("x", grid.Padding + 2).Attribute("y", grid.Padding + theme.TitleFontSize + theme.SubtitleFontSize).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", theme.SubtitleFontSize).Text(VisualBlockRendering.FitText(grid.Subtitle, theme.SubtitleFontSize, headerWidth)).EndElement().Line();
        }

        for (var i = 0; i < layout.Cells.Count; i++) {
            var cell = layout.Cells[i];
            var childScope = id + "-cell-" + i.ToString(CultureInfo.InvariantCulture);
            var childSvg = cell.Item.Chart != null ? _chartRenderer.Render(cell.Item.Chart, childScope) : _blockRenderer.Render(cell.Item.Block!, childScope);
            writer.Raw(PositionChildSvg(childSvg, cell.X, cell.Y, cell.Width, cell.Height, grid.PanelFit == VisualGridPanelFit.Stretch)).Line();
        }

        writer.EndElement().Line();
        return writer.Build();
    }

    private static string PositionChildSvg(string svg, double x, double y, double width, double height, bool stretch) {
        var tagEnd = svg.IndexOf('>');
        if (tagEnd < 0) return svg;
        var open = svg.Substring(0, tagEnd);
        open = SetSvgAttribute(open, "x", x.ToString(CultureInfo.InvariantCulture));
        open = SetSvgAttribute(open, "y", y.ToString(CultureInfo.InvariantCulture));
        open = SetSvgAttribute(open, "width", width.ToString(CultureInfo.InvariantCulture));
        open = SetSvgAttribute(open, "height", height.ToString(CultureInfo.InvariantCulture));
        open = SetSvgAttribute(open, "data-cfx-role", "visual-grid-panel");
        if (stretch) open = SetSvgAttribute(open, "preserveAspectRatio", "none");
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
        return "visual-grid-" + value.ToString(CultureInfo.InvariantCulture);
    }
}
