using System;
using System.Globalization;
using System.Text;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX.Composition;

/// <summary>
/// Renders visual canvases to self-contained SVG.
/// </summary>
public sealed class SvgVisualCanvasRenderer {
    private static long ScopeCounter;

    /// <summary>Renders a visual canvas to SVG markup.</summary>
    public string Render(VisualCanvas canvas) => Render(canvas, NextScope());

    /// <summary>Renders a visual canvas to SVG markup with a caller-provided ID scope.</summary>
    public string Render(VisualCanvas canvas, string idScope) {
        if (canvas == null) throw new ArgumentNullException(nameof(canvas));
        VisualCanvas.ValidateEnum(canvas.BackdropStyle, nameof(canvas.BackdropStyle));
        var id = "cfx-visual-canvas-" + StableHash(idScope ?? string.Empty, canvas.Title, canvas.Width.ToString(CultureInfo.InvariantCulture), canvas.Height.ToString(CultureInfo.InvariantCulture));
        var writer = new SvgMarkupWriter(8192);
        writer.StartElement("svg")
            .Attribute("xmlns", "http://www.w3.org/2000/svg")
            .Attribute("id", id)
            .Attribute("width", canvas.Width)
            .Attribute("height", canvas.Height)
            .Attribute("viewBox", "0 0 " + canvas.Width.ToString(CultureInfo.InvariantCulture) + " " + canvas.Height.ToString(CultureInfo.InvariantCulture))
            .Attribute("role", "img")
            .Attribute("aria-labelledby", id + "-title " + id + "-desc")
            .Attribute("preserveAspectRatio", "xMidYMid meet")
            .Attribute("shape-rendering", "geometricPrecision")
            .Attribute("text-rendering", "geometricPrecision")
            .Attribute("style", "max-width:100%;height:auto;display:block")
            .EndStartElement()
            .Line()
            .StartElement("title").Attribute("id", id + "-title").Text(canvas.Title).EndElement()
            .Line()
            .StartElement("desc").Attribute("id", id + "-desc").Text("Layered static visual canvas.").EndElement()
            .Line();
        writer.StartElement("defs").EndStartElement().Line();
        writer.StartElement("linearGradient").Attribute("id", id + "-background").Attribute("x1", "0").Attribute("y1", "0").Attribute("x2", "0").Attribute("y2", "1").EndStartElement().Line();
        writer.StartElement("stop").Attribute("offset", "0%").Attribute("stop-color", canvas.BackgroundTop.ToCss()).EndEmptyElement().Line();
        writer.StartElement("stop").Attribute("offset", "100%").Attribute("stop-color", canvas.BackgroundBottom.ToCss()).EndEmptyElement().Line();
        writer.EndElement().Line();
        var theme = canvas.Theme ?? new VisualCanvasTheme();
        writer.StartElement("linearGradient").Attribute("id", id + "-tile-glass").Attribute("x1", "0").Attribute("y1", "0").Attribute("x2", "0").Attribute("y2", "1").EndStartElement().Line();
        writer.StartElement("stop").Attribute("offset", "0%").Attribute("stop-color", theme.TileGlassTop.ToCss()).EndEmptyElement().Line();
        writer.StartElement("stop").Attribute("offset", "100%").Attribute("stop-color", theme.TileGlassBottom.ToCss()).EndEmptyElement().Line();
        writer.EndElement().Line();
        writer.StartElement("linearGradient").Attribute("id", id + "-hero-badge").Attribute("x1", "0").Attribute("y1", "0").Attribute("x2", "0").Attribute("y2", "1").EndStartElement().Line();
        writer.StartElement("stop").Attribute("offset", "0%").Attribute("stop-color", theme.HeroBadgeTop.ToCss()).EndEmptyElement().Line();
        writer.StartElement("stop").Attribute("offset", "100%").Attribute("stop-color", theme.HeroBadgeBottom.ToCss()).EndEmptyElement().Line();
        writer.EndElement().Line();
        writer.StartElement("filter").Attribute("id", id + "-soft-glow").Attribute("x", "-30%").Attribute("y", "-30%").Attribute("width", "160%").Attribute("height", "160%").EndStartElement().Line();
        writer.StartElement("feGaussianBlur").Attribute("stdDeviation", 5).Attribute("result", "blur").EndEmptyElement().Line();
        writer.StartElement("feMerge").EndStartElement()
            .StartElement("feMergeNode").Attribute("in", "blur").EndEmptyElement()
            .StartElement("feMergeNode").Attribute("in", "SourceGraphic").EndEmptyElement()
            .EndElement().Line();
        writer.EndElement().Line();
        writer.StartElement("filter").Attribute("id", id + "-raised-depth").Attribute("x", "-18%").Attribute("y", "-18%").Attribute("width", "136%").Attribute("height", "148%").EndStartElement().Line();
        writer.StartElement("feDropShadow").Attribute("dx", 0).Attribute("dy", 11).Attribute("stdDeviation", 8).Attribute("flood-color", "#000000").Attribute("flood-opacity", 0.42).EndEmptyElement().Line();
        writer.StartElement("feDropShadow").Attribute("dx", 0).Attribute("dy", 0).Attribute("stdDeviation", 6).Attribute("flood-color", theme.SecondaryAccent.ToCss()).Attribute("flood-opacity", 0.24).EndEmptyElement().Line();
        writer.EndElement().Line();
        writer.EndElement().Line();
        if (canvas.BackdropStyle != VisualCanvasBackdropStyle.Transparent) {
            writer.StartElement("rect").Attribute("data-cfx-role", "visual-canvas-background").Attribute("width", "100%").Attribute("height", "100%").Attribute("fill", "url(#" + id + "-background)").EndEmptyElement().Line();
        }
        if (canvas.BackdropStyle == VisualCanvasBackdropStyle.TechHorizon) RenderTechBackdrop(writer, canvas, id, theme);
        for (var i = 0; i < canvas.Layers.Count; i++) RenderLayer(writer, canvas.Layers[i], id, theme, i);
        writer.EndElement().Line();
        return writer.Build();
    }

    private static void RenderLayer(SvgMarkupWriter writer, VisualCanvasLayer layer, string id, VisualCanvasTheme theme, int layerIndex) {
        if (layer is VisualCanvasTextLayer text) {
            RenderText(writer, text);
        } else if (layer is VisualCanvasHeroTitleLayer hero) {
            RenderHeroTitle(writer, hero);
        } else if (layer is VisualCanvasKeyValueBlockLayer keyValue) {
            RenderKeyValueBlock(writer, keyValue, theme);
        } else if (layer is VisualCanvasInfoTileLayer tile) {
            RenderInfoTile(writer, tile, id, theme);
        } else if (layer is VisualCanvasHeroBadgeLayer badge) {
            RenderHeroBadge(writer, badge, id, theme);
        } else if (layer is VisualCanvasImageLayer image) {
            RenderImage(writer, image, id, layerIndex, theme);
        } else if (layer is VisualCanvasFeatureStripLayer strip) {
            RenderFeatureStrip(writer, strip, theme);
        } else {
            throw new NotSupportedException("Unsupported visual canvas layer: " + layer.GetType().FullName);
        }
    }

    private static void RenderTechBackdrop(SvgMarkupWriter writer, VisualCanvas canvas, string id, VisualCanvasTheme theme) {
        var accent = theme.SecondaryAccent;
        writer.StartElement("g").Attribute("data-cfx-role", "visual-canvas-tech-backdrop").EndStartElement().Line();
        for (var i = 0; i < 56; i++) {
            var x = (canvas.Width * ((i * 37) % 101)) / 100.0;
            var y = canvas.Height * (0.05 + (((i * 19) % 67) / 100.0) * 0.44);
            var opacity = 0.14 + ((i % 5) * 0.035);
            writer.StartElement("circle").Attribute("cx", x).Attribute("cy", y).Attribute("r", i % 11 == 0 ? 4.2 : 1.8).Attribute("fill", accent.WithOpacity(opacity).ToCss()).EndEmptyElement().Line();
        }

        for (var i = 0; i < 10; i++) {
            var y = canvas.Height * (0.18 + i * 0.045);
            writer.StartElement("path")
                .Attribute("d", "M " + F(canvas.Width * 0.08) + " " + F(y) + " C " + F(canvas.Width * 0.28) + " " + F(y - 80) + ", " + F(canvas.Width * 0.48) + " " + F(y + 120) + ", " + F(canvas.Width * 0.68) + " " + F(y - 10))
                .Attribute("fill", "none")
                .Attribute("stroke", accent.WithOpacity(0.11).ToCss())
                .Attribute("stroke-width", 1.1)
                .EndEmptyElement().Line();
        }

        writer.StartElement("path")
            .Attribute("data-cfx-role", "visual-canvas-horizon")
            .Attribute("d", "M 0 " + F(canvas.Height * 0.78) + " C " + F(canvas.Width * 0.20) + " " + F(canvas.Height * 0.72) + ", " + F(canvas.Width * 0.38) + " " + F(canvas.Height * 0.82) + ", " + F(canvas.Width * 0.55) + " " + F(canvas.Height * 0.76) + " C " + F(canvas.Width * 0.74) + " " + F(canvas.Height * 0.70) + ", " + F(canvas.Width * 0.84) + " " + F(canvas.Height * 0.85) + ", " + F(canvas.Width) + " " + F(canvas.Height * 0.73) + " L " + F(canvas.Width) + " " + F(canvas.Height) + " L 0 " + F(canvas.Height) + " Z")
            .Attribute("fill", theme.TechHorizonFill.ToCss())
            .EndEmptyElement().Line();
        writer.StartElement("path")
            .Attribute("data-cfx-role", "visual-canvas-road-glow")
            .Attribute("d", "M " + F(canvas.Width * 0.50) + " " + F(canvas.Height * 0.82) + " C " + F(canvas.Width * 0.72) + " " + F(canvas.Height * 0.86) + ", " + F(canvas.Width * 0.80) + " " + F(canvas.Height * 0.93) + ", " + F(canvas.Width * 0.92) + " " + F(canvas.Height))
            .Attribute("fill", "none")
            .Attribute("stroke", accent.ToCss())
            .Attribute("stroke-opacity", 0.76)
            .Attribute("stroke-width", 5)
            .Attribute("filter", "url(#" + id + "-soft-glow)")
            .EndEmptyElement().Line();
        writer.EndElement().Line();
    }

    private static void RenderText(SvgMarkupWriter writer, VisualCanvasTextLayer text) {
        var anchor = Anchor(text.Alignment);
        var x = AlignedX(text.X, text.Width, text.Alignment);
        var fitted = FitText(text.Text, text.FontSize, Math.Max(4, text.Width), text.Emphasized);
        writer.StartElement("text")
            .Attribute("data-cfx-role", "visual-canvas-text")
            .Attribute("x", x)
            .Attribute("y", text.Y + text.FontSize)
            .Attribute("text-anchor", anchor)
            .Attribute("fill", text.Color.ToCss())
            .Attribute("font-family", "Segoe UI, Arial, sans-serif")
            .Attribute("font-size", text.FontSize)
            .Attribute("font-weight", text.Emphasized ? "800" : "500")
            .Text(fitted)
            .EndElement()
            .Line();
    }

    private static void RenderHeroTitle(SvgMarkupWriter writer, VisualCanvasHeroTitleLayer hero) {
        var anchor = Anchor(hero.Alignment);
        var x = AlignedX(hero.X, hero.Width, hero.Alignment);
        writer.StartElement("text")
            .Attribute("data-cfx-role", "visual-canvas-hero-title")
            .Attribute("x", x)
            .Attribute("y", hero.Y + hero.FontSize)
            .Attribute("text-anchor", anchor)
            .Attribute("font-family", "Segoe UI, Arial, sans-serif")
            .Attribute("font-size", hero.FontSize)
            .Attribute("font-weight", "850")
            .Attribute("letter-spacing", "0")
            .EndStartElement();
        foreach (var run in hero.Runs) writer.StartElement("tspan").Attribute("fill", run.Color.ToCss()).Text(run.Text).EndElement();
        writer.EndElement().Line();
    }

    private static void RenderKeyValueBlock(SvgMarkupWriter writer, VisualCanvasKeyValueBlockLayer block, VisualCanvasTheme theme) {
        var layout = VisualCanvasKeyValueBlockLayout.Build(block);
        var labelColor = block.LabelColorOverride ?? theme.TileLabelColor;
        var valueColor = block.ValueColorOverride ?? theme.TileValueColor;
        var fontFamily = string.IsNullOrWhiteSpace(block.FontFamilyName) ? "Segoe UI, Arial, sans-serif" : block.FontFamilyName;
        writer.StartElement("g").Attribute("data-cfx-role", "visual-canvas-key-value-block").EndStartElement().Line();
        foreach (var row in layout.Rows) {
            writer.StartElement("g").Attribute("data-cfx-role", "visual-canvas-key-value-row").EndStartElement().Line();
            writer.StartElement("text")
                .Attribute("x", row.LabelX)
                .Attribute("y", row.Y + block.LabelFontSize)
                .Attribute("fill", (row.Item.LabelColor ?? labelColor).ToCss())
                .Attribute("font-family", fontFamily)
                .Attribute("font-size", block.LabelFontSize)
                .Attribute("font-weight", block.LabelEmphasized ? "800" : "500")
                .Text(row.LabelText)
                .EndElement()
                .Line();
            if (!row.LabelOnly) {
                for (var i = 0; i < row.ValueLines.Count; i++) {
                    writer.StartElement("text")
                        .Attribute("x", row.ValueX)
                        .Attribute("y", row.Y + block.ValueFontSize + row.ValueLineHeight * i)
                        .Attribute("fill", (row.Item.ValueColor ?? valueColor).ToCss())
                        .Attribute("font-family", fontFamily)
                        .Attribute("font-size", block.ValueFontSize)
                        .Attribute("font-weight", block.ValueEmphasized ? "700" : "500")
                        .Text(row.ValueLines[i])
                        .EndElement()
                        .Line();
                }
            }

            writer.EndElement().Line();
        }

        writer.EndElement().Line();
    }

    private static void RenderInfoTile(SvgMarkupWriter writer, VisualCanvasInfoTileLayer tile, string id, VisualCanvasTheme theme) {
        VisualCanvas.ValidateEnum(tile.SurfaceStyle, nameof(tile.SurfaceStyle));
        VisualCanvas.ValidateEnum(tile.IconKind, nameof(tile.IconKind));
        VisualCanvas.ValidateEnum(tile.MiniChartKind, nameof(tile.MiniChartKind));
        var x = Math.Round(tile.X);
        var y = Math.Round(tile.Y);
        var width = Math.Round(tile.Width);
        var height = Math.Round(tile.Height);
        var accent = tile.AccentOverride ?? theme.Accent;
        var radius = Math.Min(16, height * 0.18);
        var isRaised = tile.SurfaceStyle == VisualCanvasInfoTileSurfaceStyle.Raised;
        var isFilled = tile.SurfaceStyle == VisualCanvasInfoTileSurfaceStyle.Glass || isRaised;
        writer.StartElement("g").Attribute("data-cfx-role", "visual-canvas-info-tile").EndStartElement().Line();
        if (isRaised) {
            writer.StartElement("rect").Attribute("x", x + 6).Attribute("y", y + 8).Attribute("width", width).Attribute("height", height).Attribute("rx", radius + 2).Attribute("fill", ChartColor.Black.WithOpacity(0.34).ToCss()).Attribute("filter", "url(#" + id + "-raised-depth)").EndEmptyElement().Line();
            writer.StartElement("rect").Attribute("x", x - 5).Attribute("y", y - 5).Attribute("width", width + 10).Attribute("height", height + 10).Attribute("rx", radius + 5).Attribute("fill", accent.WithOpacity(0.11).ToCss()).EndEmptyElement().Line();
            writer.StartElement("rect").Attribute("x", x - 2).Attribute("y", y - 2).Attribute("width", width + 4).Attribute("height", height + 4).Attribute("rx", radius + 2).Attribute("fill", accent.WithOpacity(0.13).ToCss()).EndEmptyElement().Line();
            writer.StartElement("rect").Attribute("x", x + 2).Attribute("y", y + 2).Attribute("width", Math.Max(1, width - 4)).Attribute("height", Math.Max(1, height * 0.52)).Attribute("rx", Math.Max(1, radius - 2)).Attribute("fill", ChartColor.White.WithOpacity(0.20).ToCss()).EndEmptyElement().Line();
            writer.StartElement("rect").Attribute("x", x - 2).Attribute("y", y - 2).Attribute("width", width + 4).Attribute("height", height + 4).Attribute("rx", radius + 2).Attribute("fill", "none").Attribute("stroke", accent.WithOpacity(0.42).ToCss()).Attribute("stroke-width", 3.2).EndEmptyElement().Line();
            writer.StartElement("path").Attribute("d", "M " + F(x + radius) + " " + F(y + 3) + " L " + F(x + width - radius) + " " + F(y + 3) + " M " + F(x + radius) + " " + F(y + height - 2) + " L " + F(x + width - radius) + " " + F(y + height - 2)).Attribute("fill", "none").Attribute("stroke", ChartColor.White.WithOpacity(0.26).ToCss()).Attribute("stroke-width", 1.4).EndEmptyElement().Line();
        }
        writer.StartElement("rect").Attribute("x", x + 0.5).Attribute("y", y + 0.5).Attribute("width", Math.Max(1, width - 1)).Attribute("height", Math.Max(1, height - 1)).Attribute("rx", radius).Attribute("fill", isFilled ? "url(#" + id + "-tile-glass)" : "none").Attribute("stroke", accent.WithOpacity(isRaised ? 0.92 : 0.72).ToCss()).Attribute("stroke-width", isRaised ? 2.2 : 1.4).EndEmptyElement().Line();
        if (isFilled) {
            writer.StartElement("rect").Attribute("x", x + 2.5).Attribute("y", y + 2.5).Attribute("width", Math.Max(1, width - 5)).Attribute("height", Math.Max(1, height - 5)).Attribute("rx", Math.Max(1, radius - 2)).Attribute("fill", "none").Attribute("stroke", theme.TileInnerStroke.ToCss()).EndEmptyElement().Line();
        }
        var padX = Math.Max(20, Math.Min(28, width * 0.06));
        var iconBox = Math.Max(44, Math.Min(54, height - 34));
        var iconX = x + padX;
        var iconY = y + (height - iconBox) / 2;
        var iconFont = Math.Min(25, iconBox * (tile.Icon.Length > 3 ? 0.34 : 0.42));
        writer.StartElement("rect").Attribute("x", iconX).Attribute("y", iconY).Attribute("width", iconBox).Attribute("height", iconBox).Attribute("rx", Math.Min(13, iconBox * 0.25)).Attribute("fill", isFilled ? accent.WithOpacity(isRaised ? 0.25 : 0.18).ToCss() : "none").Attribute("stroke", isRaised ? accent.WithOpacity(0.32).ToCss() : (tile.SurfaceStyle == VisualCanvasInfoTileSurfaceStyle.Outline ? accent.WithOpacity(0.38).ToCss() : "none")).EndEmptyElement().Line();
        RenderTileIcon(writer, tile.IconKind, tile.Icon, iconX, iconY, iconBox, iconFont, accent);
        var textX = iconX + iconBox + 22;
        var hasMiniChart = tile.MiniChartKind != VisualCanvasInfoTileMiniChartKind.None && tile.MiniChartValues.Count > 0;
        var chartW = hasMiniChart ? Math.Min(width * 0.24, Math.Max(82, width * 0.20)) : 0;
        var chartX = x + width - padX - chartW;
        var chartY = y + Math.Max(24, height * 0.30);
        var chartH = Math.Max(28, Math.Min(46, height * 0.42));
        var hasDetail = tile.Detail.Length > 0;
        var labelY = y + (hasDetail ? 30 : Math.Max(32, (height - 52) / 2 + 14));
        var valueY = labelY + 28;
        var detailY = valueY + 25;
        var textMax = hasMiniChart ? Math.Max(24, chartX - textX - 16) : Math.Max(24, width - (textX - x) - padX);
        writer.StartElement("text").Attribute("x", textX).Attribute("y", labelY).Attribute("fill", theme.TileLabelColor.ToCss()).Attribute("font-family", "Segoe UI, Arial, sans-serif").Attribute("font-size", 14).Attribute("font-weight", "700").Text(FitText(tile.Label, 14, textMax, true)).EndElement().Line();
        writer.StartElement("text").Attribute("x", textX).Attribute("y", valueY).Attribute("fill", theme.TileValueColor.ToCss()).Attribute("font-family", "Segoe UI, Arial, sans-serif").Attribute("font-size", height < 92 ? 21 : 22).Attribute("font-weight", "650").Text(FitText(tile.Value, height < 92 ? 21 : 22, textMax, true)).EndElement().Line();
        if (hasDetail) writer.StartElement("text").Attribute("x", textX).Attribute("y", detailY).Attribute("fill", theme.TileDetailColor.ToCss()).Attribute("font-family", "Segoe UI, Arial, sans-serif").Attribute("font-size", 13).Text(FitText(tile.Detail, 13, textMax, false)).EndElement().Line();
        if (tile.Progress.HasValue) {
            var railX = textX;
            var railY = y + height - 16;
            var railW = hasMiniChart ? Math.Max(24, chartX - railX - 16) : Math.Max(24, width - (railX - x) - padX);
            writer.StartElement("rect").Attribute("x", railX).Attribute("y", railY).Attribute("width", railW).Attribute("height", 8).Attribute("rx", 4).Attribute("fill", theme.TileProgressTrackColor.ToCss()).EndEmptyElement().Line();
            writer.StartElement("rect").Attribute("x", railX).Attribute("y", railY).Attribute("width", railW * tile.Progress.Value).Attribute("height", 8).Attribute("rx", 4).Attribute("fill", accent.ToCss()).EndEmptyElement().Line();
        }
        if (hasMiniChart) {
            RenderTileMiniChart(writer, tile, theme, accent, chartX, chartY, chartW, chartH);
        }

        writer.EndElement().Line();
    }

    private static void RenderTileMiniChart(SvgMarkupWriter writer, VisualCanvasInfoTileLayer tile, VisualCanvasTheme theme, ChartColor accent, double x, double y, double width, double height) {
        writer.StartElement("g").Attribute("data-cfx-role", "visual-canvas-info-tile-mini-chart").EndStartElement().Line();
        writer.StartElement("rect").Attribute("x", x).Attribute("y", y).Attribute("width", width).Attribute("height", height).Attribute("rx", Math.Min(8, height * 0.24)).Attribute("fill", theme.TileMiniChartTrackColor.WithOpacity(0.20).ToCss()).EndEmptyElement().Line();
        writer.StartElement("path").Attribute("d", "M " + F(x + 4) + " " + F(y + height * 0.72) + " L " + F(x + width - 4) + " " + F(y + height * 0.72) + " M " + F(x + 4) + " " + F(y + height * 0.38) + " L " + F(x + width - 4) + " " + F(y + height * 0.38)).Attribute("fill", "none").Attribute("stroke", theme.TileMiniChartTrackColor.ToCss()).Attribute("stroke-width", 1).EndEmptyElement().Line();

        var values = tile.MiniChartValues;
        var min = 0.0;
        var max = tile.MiniChartMaximum ?? 0.0;
        for (var i = 0; i < values.Count; i++) {
            if (values[i] < min) min = values[i];
            if (!tile.MiniChartMaximum.HasValue && values[i] > max) max = values[i];
        }
        if (max <= min) max = min + 1;

        var plotX = x + 7;
        var plotY = y + 6;
        var plotW = Math.Max(1, width - 14);
        var plotH = Math.Max(1, height - 12);
        var baseY = plotY + plotH;
        if (tile.MiniChartKind == VisualCanvasInfoTileMiniChartKind.Bars) {
            var gap = values.Count > 1 ? Math.Min(Math.Max(1, plotW * 0.035), plotW / (values.Count * 3.0)) : 0;
            var barW = Math.Max(0.5, (plotW - gap * (values.Count - 1)) / values.Count);
            for (var i = 0; i < values.Count; i++) {
                var ratio = Math.Max(0, Math.Min(1, (values[i] - min) / (max - min)));
                var barH = Math.Max(2, plotH * ratio);
                writer.StartElement("rect").Attribute("x", plotX + i * (barW + gap)).Attribute("y", baseY - barH).Attribute("width", barW).Attribute("height", barH).Attribute("rx", Math.Min(4, barW * 0.42)).Attribute("fill", accent.WithOpacity(0.82).ToCss()).EndEmptyElement().Line();
            }
            writer.EndElement().Line();
            return;
        }

        var line = new StringBuilder();
        var area = new StringBuilder();
        for (var i = 0; i < values.Count; i++) {
            var px = values.Count == 1 ? plotX + plotW / 2 : plotX + plotW * i / (values.Count - 1);
            var ratio = Math.Max(0, Math.Min(1, (values[i] - min) / (max - min)));
            var py = plotY + plotH - plotH * ratio;
            if (i == 0) {
                line.Append("M ").Append(F(px)).Append(' ').Append(F(py));
                area.Append("M ").Append(F(px)).Append(' ').Append(F(baseY)).Append(" L ").Append(F(px)).Append(' ').Append(F(py));
            } else {
                line.Append(" L ").Append(F(px)).Append(' ').Append(F(py));
                area.Append(" L ").Append(F(px)).Append(' ').Append(F(py));
            }
            if (i == values.Count - 1) area.Append(" L ").Append(F(px)).Append(' ').Append(F(baseY)).Append(" Z");
        }
        if (tile.MiniChartKind == VisualCanvasInfoTileMiniChartKind.Area && values.Count > 1) {
            writer.StartElement("path").Attribute("d", area.ToString()).Attribute("fill", theme.TileMiniChartFillColor.ToCss()).EndEmptyElement().Line();
        }
        writer.StartElement("path").Attribute("d", line.ToString()).Attribute("fill", "none").Attribute("stroke", accent.ToCss()).Attribute("stroke-width", 2.2).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndEmptyElement().Line();
        writer.EndElement().Line();
    }

    private static void RenderTileIcon(SvgMarkupWriter writer, VisualCanvasInfoTileIconKind kind, string text, double x, double y, double size, double iconFont, ChartColor color) {
        var stroke = color.ToCss();
        var thick = Math.Max(1.6, size * 0.045);
        var cx = x + size / 2;
        var cy = y + size / 2;
        var left = x + size * 0.24;
        var right = x + size * 0.76;
        var top = y + size * 0.24;
        var bottom = y + size * 0.76;
        if (kind == VisualCanvasInfoTileIconKind.Text) {
            writer.StartElement("text").Attribute("x", cx).Attribute("y", cy + iconFont * 0.36).Attribute("text-anchor", "middle").Attribute("fill", stroke).Attribute("font-family", "Segoe UI, Arial, sans-serif").Attribute("font-size", iconFont).Attribute("font-weight", "800").Text(text).EndElement().Line();
            return;
        }

        writer.StartElement("g").Attribute("fill", "none").Attribute("stroke", stroke).Attribute("stroke-width", thick).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round").EndStartElement().Line();
        switch (kind) {
            case VisualCanvasInfoTileIconKind.Computer:
            case VisualCanvasInfoTileIconKind.OperatingSystem:
                writer.StartElement("rect").Attribute("x", left).Attribute("y", top).Attribute("width", size * 0.52).Attribute("height", size * 0.36).Attribute("rx", 3).EndEmptyElement().Line();
                writer.StartElement("path").Attribute("d", "M " + F(cx) + " " + F(top + size * 0.36) + " L " + F(cx) + " " + F(bottom - size * 0.08) + " M " + F(cx - size * 0.16) + " " + F(bottom - size * 0.08) + " L " + F(cx + size * 0.16) + " " + F(bottom - size * 0.08)).EndEmptyElement().Line();
                if (kind == VisualCanvasInfoTileIconKind.OperatingSystem) writer.StartElement("path").Attribute("d", "M " + F(cx) + " " + F(top) + " L " + F(cx) + " " + F(top + size * 0.36) + " M " + F(left) + " " + F(top + size * 0.18) + " L " + F(right) + " " + F(top + size * 0.18)).Attribute("opacity", 0.75).EndEmptyElement().Line();
                break;
            case VisualCanvasInfoTileIconKind.Network:
                writer.StartElement("circle").Attribute("cx", cx).Attribute("cy", cy).Attribute("r", size * 0.27).EndEmptyElement().Line();
                writer.StartElement("path").Attribute("d", "M " + F(cx - size * 0.27) + " " + F(cy) + " L " + F(cx + size * 0.27) + " " + F(cy) + " M " + F(cx) + " " + F(cy - size * 0.27) + " L " + F(cx) + " " + F(cy + size * 0.27)).EndEmptyElement().Line();
                writer.StartElement("circle").Attribute("cx", cx).Attribute("cy", cy).Attribute("r", size * 0.12).Attribute("opacity", 0.65).EndEmptyElement().Line();
                break;
            case VisualCanvasInfoTileIconKind.Cpu:
                writer.StartElement("rect").Attribute("x", x + size * 0.31).Attribute("y", y + size * 0.31).Attribute("width", size * 0.38).Attribute("height", size * 0.38).Attribute("rx", 4).EndEmptyElement().Line();
                for (var i = 0; i < 4; i++) {
                    var p = y + size * (0.24 + i * 0.17);
                    var q = x + size * (0.24 + i * 0.17);
                    writer.StartElement("path").Attribute("d", "M " + F(x + size * 0.22) + " " + F(p) + " L " + F(x + size * 0.31) + " " + F(p) + " M " + F(x + size * 0.69) + " " + F(p) + " L " + F(x + size * 0.78) + " " + F(p) + " M " + F(q) + " " + F(y + size * 0.22) + " L " + F(q) + " " + F(y + size * 0.31) + " M " + F(q) + " " + F(y + size * 0.69) + " L " + F(q) + " " + F(y + size * 0.78)).EndEmptyElement().Line();
                }
                break;
            case VisualCanvasInfoTileIconKind.Memory:
                writer.StartElement("rect").Attribute("x", left).Attribute("y", y + size * 0.35).Attribute("width", size * 0.52).Attribute("height", size * 0.30).Attribute("rx", 3).EndEmptyElement().Line();
                for (var i = 0; i < 5; i++) writer.StartElement("path").Attribute("d", "M " + F(x + size * (0.28 + i * 0.10)) + " " + F(y + size * 0.65) + " L " + F(x + size * (0.28 + i * 0.10)) + " " + F(y + size * 0.72)).EndEmptyElement().Line();
                break;
            case VisualCanvasInfoTileIconKind.User:
                writer.StartElement("circle").Attribute("cx", cx).Attribute("cy", y + size * 0.38).Attribute("r", size * 0.11).EndEmptyElement().Line();
                writer.StartElement("path").Attribute("d", "M " + F(cx - size * 0.22) + " " + F(bottom) + " L " + F(cx - size * 0.12) + " " + F(y + size * 0.58) + " L " + F(cx + size * 0.12) + " " + F(y + size * 0.58) + " L " + F(cx + size * 0.22) + " " + F(bottom)).EndEmptyElement().Line();
                break;
            case VisualCanvasInfoTileIconKind.Domain:
                writer.StartElement("path").Attribute("d", "M " + F(x + size * 0.22) + " " + F(bottom) + " L " + F(x + size * 0.78) + " " + F(bottom) + " M " + F(x + size * 0.32) + " " + F(bottom) + " L " + F(x + size * 0.32) + " " + F(top) + " L " + F(x + size * 0.60) + " " + F(top) + " L " + F(x + size * 0.60) + " " + F(bottom) + " M " + F(x + size * 0.60) + " " + F(y + size * 0.44) + " L " + F(x + size * 0.74) + " " + F(y + size * 0.44) + " L " + F(x + size * 0.74) + " " + F(bottom)).EndEmptyElement().Line();
                break;
            case VisualCanvasInfoTileIconKind.Terminal:
                writer.StartElement("path").Attribute("d", "M " + F(x + size * 0.30) + " " + F(y + size * 0.36) + " L " + F(x + size * 0.44) + " " + F(cy) + " L " + F(x + size * 0.30) + " " + F(y + size * 0.64) + " M " + F(x + size * 0.52) + " " + F(y + size * 0.64) + " L " + F(x + size * 0.72) + " " + F(y + size * 0.64)).EndEmptyElement().Line();
                break;
            case VisualCanvasInfoTileIconKind.Storage:
                writer.StartElement("ellipse").Attribute("cx", cx).Attribute("cy", y + size * 0.32).Attribute("rx", size * 0.19).Attribute("ry", size * 0.09).EndEmptyElement().Line();
                writer.StartElement("path").Attribute("d", "M " + F(cx - size * 0.19) + " " + F(y + size * 0.32) + " L " + F(cx - size * 0.19) + " " + F(y + size * 0.68) + " C " + F(cx - size * 0.19) + " " + F(y + size * 0.80) + ", " + F(cx + size * 0.19) + " " + F(y + size * 0.80) + ", " + F(cx + size * 0.19) + " " + F(y + size * 0.68) + " L " + F(cx + size * 0.19) + " " + F(y + size * 0.32)).EndEmptyElement().Line();
                writer.StartElement("path").Attribute("d", "M " + F(cx - size * 0.19) + " " + F(y + size * 0.50) + " C " + F(cx - size * 0.19) + " " + F(y + size * 0.62) + ", " + F(cx + size * 0.19) + " " + F(y + size * 0.62) + ", " + F(cx + size * 0.19) + " " + F(y + size * 0.50)).Attribute("opacity", 0.72).EndEmptyElement().Line();
                break;
            case VisualCanvasInfoTileIconKind.Shield:
                writer.StartElement("path").Attribute("d", "M " + F(cx) + " " + F(top) + " L " + F(right) + " " + F(y + size * 0.36) + " L " + F(x + size * 0.68) + " " + F(bottom) + " L " + F(cx) + " " + F(y + size * 0.82) + " L " + F(x + size * 0.32) + " " + F(bottom) + " L " + F(left) + " " + F(y + size * 0.36) + " Z").EndEmptyElement().Line();
                break;
            default:
                writer.EndElement().Line();
                RenderTileIcon(writer, VisualCanvasInfoTileIconKind.Text, text, x, y, size, iconFont, color);
                return;
        }
        writer.EndElement().Line();
    }

    private static void RenderHeroBadge(SvgMarkupWriter writer, VisualCanvasHeroBadgeLayer badge, string id, VisualCanvasTheme theme) {
        var accent = badge.AccentOverride ?? theme.SecondaryAccent;
        writer.StartElement("g").Attribute("data-cfx-role", "visual-canvas-hero-badge").EndStartElement().Line();
        writer.StartElement("rect").Attribute("x", badge.X).Attribute("y", badge.Y).Attribute("width", badge.Width).Attribute("height", badge.Height).Attribute("rx", Math.Min(22, badge.Height * 0.20)).Attribute("fill", "url(#" + id + "-hero-badge)").Attribute("stroke", accent.ToCss()).Attribute("stroke-width", 2.4).Attribute("filter", "url(#" + id + "-soft-glow)").EndEmptyElement().Line();
        writer.StartElement("text").Attribute("x", badge.X + badge.Width / 2).Attribute("y", badge.Y + badge.Height / 2 + badge.Height * 0.17).Attribute("text-anchor", "middle").Attribute("fill", theme.HeroBadgeTextColor.ToCss()).Attribute("font-family", "Cascadia Mono, Consolas, monospace").Attribute("font-size", Math.Max(24, badge.Height * 0.42)).Attribute("font-weight", "850").Text(badge.Symbol).EndElement().Line();
        writer.EndElement().Line();
    }

    private static void RenderImage(SvgMarkupWriter writer, VisualCanvasImageLayer image, string id, int layerIndex, VisualCanvasTheme theme) {
        VisualCanvas.ValidateEnum(image.Fit, nameof(image.Fit));
        writer.StartElement("g").Attribute("data-cfx-role", "visual-canvas-image").EndStartElement().Line();
        if (image.Href.Length > 0) {
            var preserveAspectRatio = PreserveAspectRatio(image.Fit);
            var imageScope = id + "-image-" + layerIndex.ToString(CultureInfo.InvariantCulture);
            if (image.Fit == VisualCanvasImageFit.Tile && image.SourceWidth > 0 && image.SourceHeight > 0) {
                var patternId = imageScope + "-pattern";
                writer.StartElement("defs").EndStartElement().Line()
                    .StartElement("pattern").Attribute("id", patternId).Attribute("patternUnits", "userSpaceOnUse").Attribute("x", image.X).Attribute("y", image.Y).Attribute("width", image.SourceWidth).Attribute("height", image.SourceHeight).EndStartElement().Line()
                    .StartElement("image").Attribute("x", 0).Attribute("y", 0).Attribute("width", image.SourceWidth).Attribute("height", image.SourceHeight).Attribute("href", image.Href).Attribute("preserveAspectRatio", "none").EndEmptyElement().Line()
                    .EndElement().Line()
                    .EndElement().Line();
                writer.StartElement("rect").Attribute("x", image.X).Attribute("y", image.Y).Attribute("width", image.Width).Attribute("height", image.Height).Attribute("fill", "url(#" + patternId + ")").Attribute("opacity", image.Opacity).EndEmptyElement().Line();
            } else if (image.Fit == VisualCanvasImageFit.Center && image.SourceWidth > 0 && image.SourceHeight > 0) {
                var clipId = imageScope + "-clip";
                writer.StartElement("clipPath").Attribute("id", clipId).EndStartElement()
                    .StartElement("rect").Attribute("x", image.X).Attribute("y", image.Y).Attribute("width", image.Width).Attribute("height", image.Height).EndEmptyElement()
                    .EndElement().Line();
                writer.StartElement("image")
                    .Attribute("x", image.X + (image.Width - image.SourceWidth) / 2)
                    .Attribute("y", image.Y + (image.Height - image.SourceHeight) / 2)
                    .Attribute("width", image.SourceWidth)
                    .Attribute("height", image.SourceHeight)
                    .Attribute("href", image.Href)
                    .Attribute("preserveAspectRatio", "none")
                    .Attribute("opacity", image.Opacity)
                    .Attribute("clip-path", "url(#" + clipId + ")")
                    .EndEmptyElement().Line();
            } else {
                writer.StartElement("image").Attribute("x", image.X).Attribute("y", image.Y).Attribute("width", image.Width).Attribute("height", image.Height).Attribute("href", image.Href).Attribute("preserveAspectRatio", preserveAspectRatio).Attribute("opacity", image.Opacity).EndEmptyElement().Line();
            }
        } else {
            writer.StartElement("rect").Attribute("x", image.X).Attribute("y", image.Y).Attribute("width", image.Width).Attribute("height", image.Height).Attribute("rx", 12).Attribute("fill", theme.ImagePlaceholderFill.ToCss()).Attribute("stroke", theme.ImagePlaceholderStroke.ToCss()).EndEmptyElement().Line();
        }

        writer.EndElement().Line();
    }

    private static string PreserveAspectRatio(VisualCanvasImageFit fit) {
        switch (fit) {
            case VisualCanvasImageFit.Contain:
                return "xMidYMid meet";
            case VisualCanvasImageFit.Cover:
                return "xMidYMid slice";
            case VisualCanvasImageFit.Center:
            case VisualCanvasImageFit.Tile:
            case VisualCanvasImageFit.Stretch:
            default:
                return "none";
        }
    }

    private static void RenderFeatureStrip(SvgMarkupWriter writer, VisualCanvasFeatureStripLayer strip, VisualCanvasTheme theme) {
        writer.StartElement("g").Attribute("data-cfx-role", "visual-canvas-feature-strip").EndStartElement().Line();
        var slot = strip.Width / strip.Items.Count;
        for (var i = 0; i < strip.Items.Count; i++) {
            var item = strip.Items[i];
            var cx = strip.X + slot * i + slot / 2;
            if (i > 0) writer.StartElement("line").Attribute("x1", strip.X + slot * i).Attribute("y1", strip.Y + 4).Attribute("x2", strip.X + slot * i).Attribute("y2", strip.Y + strip.Height - 4).Attribute("stroke", theme.FeatureDividerColor.ToCss()).EndEmptyElement().Line();
            writer.StartElement("text").Attribute("x", cx).Attribute("y", strip.Y + 26).Attribute("text-anchor", "middle").Attribute("fill", strip.Accent.ToCss()).Attribute("font-family", "Segoe UI, Arial, sans-serif").Attribute("font-size", 22).Attribute("font-weight", "800").Text(FitText(item.Icon, 22, Math.Max(8, slot - 12), true)).EndElement().Line();
            writer.StartElement("text").Attribute("x", cx).Attribute("y", strip.Y + 58).Attribute("text-anchor", "middle").Attribute("fill", theme.FeatureLabelColor.ToCss()).Attribute("font-family", "Segoe UI, Arial, sans-serif").Attribute("font-size", 15).Attribute("font-weight", "700").Text(FitText(item.Label, 15, Math.Max(8, slot - 12), true)).EndElement().Line();
        }

        writer.EndElement().Line();
    }

    private static string Anchor(VisualCanvasTextAlignment alignment) {
        VisualCanvas.ValidateEnum(alignment, nameof(alignment));
        switch (alignment) {
            case VisualCanvasTextAlignment.Center: return "middle";
            case VisualCanvasTextAlignment.Right: return "end";
            default: return "start";
        }
    }

    private static double AlignedX(double x, double width, VisualCanvasTextAlignment alignment) {
        switch (alignment) {
            case VisualCanvasTextAlignment.Center: return x + width / 2;
            case VisualCanvasTextAlignment.Right: return x + width;
            default: return x;
        }
    }

    private static string F(double value) => SvgMarkupWriter.FormatNumber(value);

    private static string FitText(string value, double fontSize, double maxWidth, bool emphasized) {
        if (string.IsNullOrEmpty(value) || Measure(value, fontSize, emphasized) <= maxWidth) return value;
        const string suffix = "...";
        if (Measure(suffix, fontSize, emphasized) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (Measure(value.Substring(0, mid) + suffix, fontSize, emphasized) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }

    private static double Measure(string value, double fontSize, bool emphasized) =>
        emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(value, fontSize, null) : RgbaCanvas.MeasureTextWidth(value, fontSize, null);

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "visual-canvas-" + value.ToString(CultureInfo.InvariantCulture);
    }

    private static string StableHash(params string[] values) {
        unchecked {
            var hash = 2166136261u;
            foreach (var value in values) {
                for (var i = 0; i < value.Length; i++) {
                    hash ^= value[i];
                    hash *= 16777619;
                }

                hash ^= 31;
                hash *= 16777619;
            }

            var bytes = BitConverter.GetBytes(hash);
            var builder = new StringBuilder(bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }
}
