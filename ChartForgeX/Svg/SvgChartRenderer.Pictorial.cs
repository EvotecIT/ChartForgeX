using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPictorial(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Pictorial);
        if (series == null || series.Points.Count == 0) return;
        var values = series.Points.ToArray();
        var max = Math.Max(0.000001, chart.Options.PictorialMaximum ?? values.Max(point => point.Y));
        var t = chart.Options.Theme;
        var columns = chart.Options.PictorialColumns;
        var valuePerSymbol = chart.Options.PictorialValuePerSymbol;
        var symbolRows = BuildPictorialSymbolRows(values, columns, valuePerSymbol);
        var totalSymbolRows = Math.Max(1, symbolRows.Sum());
        var itemGap = Math.Min(12, Math.Max(4, plot.Height * 0.018));
        var rowHeight = Math.Max(16, Math.Min(54, (plot.Height - itemGap * (values.Length - 1)) / totalSymbolRows));
        var labelWidth = Math.Min(160, Math.Max(84, plot.Width * 0.26));
        var showValues = chart.Options.ShowPictorialValues;
        var valueWidth = showValues ? Math.Min(88, Math.Max(50, plot.Width * 0.14)) : 0;
        var symbolArea = Math.Max(1, plot.Width - labelWidth - valueWidth - (showValues ? 22 : 10));
        var gap = Math.Max(1.2, Math.Min(7, symbolArea / columns * 0.16));
        var maxSymbolSize = Math.Max(1, (symbolArea - gap * (columns - 1)) / columns);
        var baseSymbolSize = Math.Min(rowHeight * 0.72, maxSymbolSize);
        var symbolSize = Math.Max(3, Math.Min(maxSymbolSize, baseSymbolSize * chart.Options.PictorialSymbolScale));
        var startX = plot.Left + labelWidth + 10;
        var totalHeight = rowHeight * totalSymbolRows + itemGap * (values.Length - 1);
        var startY = plot.Top + Math.Max(0, (plot.Height - totalHeight) / 2);
        var customSymbol = chart.Options.PictorialSvgPathData != null;
        var valuePerSymbolMetadata = valuePerSymbol.HasValue ? F(valuePerSymbol.Value) : "proportional";
        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "pictorial-chart")
            .Attribute("data-cfx-shape", chart.Options.PictorialShape.ToString())
            .Attribute("data-cfx-custom-symbol", customSymbol)
            .Attribute("data-cfx-png-fallback-shape", chart.Options.PictorialPngFallbackShape.ToString())
            .Attribute("data-cfx-columns", columns)
            .Attribute("data-cfx-maximum", max)
            .Attribute("data-cfx-value-per-symbol", valuePerSymbolMetadata)
            .Attribute("data-cfx-show-values", showValues)
            .Attribute("data-cfx-symbol-scale", chart.Options.PictorialSymbolScale)
            .Attribute("data-cfx-empty-opacity", chart.Options.PictorialEmptyOpacity)
            .EndStartElement()
            .Line();
        var symbolRow = 0;
        for (var i = 0; i < values.Length; i++) {
            var rowsForItem = symbolRows[i];
            var itemTop = startY + symbolRow * rowHeight + i * itemGap;
            var labelY = itemTop + rowsForItem * rowHeight / 2;
            var labelMaxWidth = Math.Max(8, labelWidth - 8);
            var rawLabel = FormatX(chart, values[i].X);
            var labelFontSize = TextFontSizeForSvgWidth(rawLabel, labelMaxWidth, t.TickLabelFontSize);
            var label = TrimSvgLabelToWidth(rawLabel, labelFontSize, labelMaxWidth);
            if (label.Length > 0) {
                writer
                    .StartElement("text")
                    .Attribute("data-cfx-role", "pictorial-label")
                    .Attribute("data-cfx-point", i)
                    .Attribute("x", plot.Left + labelWidth - 8)
                    .Attribute("y", labelY + labelFontSize / 3.0)
                    .Attribute("text-anchor", "end")
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("font-family", SvgFontFamily(t.FontFamily))
                    .Attribute("font-size", labelFontSize)
                    .Attribute("font-weight", "700")
                    .Raw(Escape(label))
                    .EndElement()
                    .Line();
            }
            var color = PictorialItemColor(series, t, i);
            var filled = valuePerSymbol.HasValue ? values[i].Y / valuePerSymbol.Value : values[i].Y / max * columns;
            for (var row = 0; row < rowsForItem; row++) {
                var y = itemTop + row * rowHeight + rowHeight / 2;
                for (var column = 0; column < columns; column++) {
                    var amount = Clamp(filled - row * columns - column, 0, 1);
                    var x = startX + column * (symbolSize + gap);
                    var emptyColor = PictorialOpacity(t.Grid, chart.Options.PictorialEmptyOpacity);
                    var symbolId = id + "-pictorial-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    DrawPictorialSymbol(writer, chart.Options, symbolId, x + symbolSize / 2, y, symbolSize / 2, color, emptyColor, amount, row);
                }
            }

            if (showValues) {
                var valueMaxWidth = Math.Max(8, valueWidth - 4);
                var rawValue = FormatValue(chart, values[i].Y);
                var valueFontSize = TextFontSizeForSvgWidth(rawValue, valueMaxWidth, t.DataLabelFontSize);
                var value = TrimSvgLabelToWidth(rawValue, valueFontSize, valueMaxWidth);
                if (value.Length > 0) {
                    writer
                        .StartElement("text")
                        .Attribute("data-cfx-role", "pictorial-value")
                        .Attribute("data-cfx-point", i)
                        .Attribute("data-cfx-label", label)
                        .Attribute("data-cfx-value", values[i].Y)
                        .Attribute("x", startX + symbolArea + 12)
                        .Attribute("y", labelY + valueFontSize / 3.0)
                        .Attribute("fill", t.Text.ToCss())
                        .Attribute("font-family", SvgFontFamily(t.FontFamily))
                        .Attribute("font-size", valueFontSize)
                        .Attribute("font-weight", "800")
                        .Raw(Escape(value))
                        .EndElement()
                        .Line();
                }
            }

            symbolRow += rowsForItem;
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static int[] BuildPictorialSymbolRows(ChartPoint[] values, int columns, double? valuePerSymbol) {
        var rows = new int[values.Length];
        for (var i = 0; i < values.Length; i++) {
            rows[i] = valuePerSymbol.HasValue ? Math.Max(1, (int)Math.Ceiling(values[i].Y / valuePerSymbol.Value / columns)) : 1;
        }

        return rows;
    }

    private static void DrawPictorialSymbol(SvgMarkupWriter writer, ChartOptions options, string symbolId, double cx, double cy, double radius, ChartColor fillColor, ChartColor emptyColor, double fillAmount, int row) {
        var fill = F(fillAmount);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "pictorial-symbol")
            .Attribute("data-cfx-fill", fill)
            .Attribute("data-cfx-row", row)
            .Attribute("data-cfx-partial-fill", fillAmount > 0 && fillAmount < 1 ? "clip" : "none")
            .EndStartElement()
            .Line();
        if (fillAmount <= 0) {
            AppendPictorialSymbolShape(writer, options, cx, cy, radius, emptyColor, "empty");
        } else if (fillAmount >= 1) {
            AppendPictorialSymbolShape(writer, options, cx, cy, radius, fillColor, "fill");
        } else {
            AppendPictorialSymbolShape(writer, options, cx, cy, radius, emptyColor, "empty");
            writer
                .StartElement("clipPath")
                .Attribute("id", symbolId + "-clip")
                .EndStartElement()
                .StartElement("rect")
                .Attribute("x", cx - radius)
                .Attribute("y", cy - radius)
                .Attribute("width", radius * 2 * fillAmount)
                .Attribute("height", radius * 2)
                .EndEmptyElement()
                .EndElement()
                .Line()
                .StartElement("g")
                .Attribute("clip-path", "url(#" + symbolId + "-clip)")
                .EndStartElement()
                .Line();
            AppendPictorialSymbolShape(writer, options, cx, cy, radius, fillColor, "partial-fill");
            writer.EndElement().Line();
        }

        writer.EndElement().Line();
    }

    private static void AppendPictorialSymbolShape(SvgMarkupWriter writer, ChartOptions options, double cx, double cy, double radius, ChartColor color, string layer) {
        if (options.PictorialSvgPathData != null) {
            var vb = options.PictorialSvgPathViewBox;
            var scaleX = radius * 2 / vb.Width;
            var scaleY = radius * 2 / vb.Height;
            writer
                .StartElement("path")
                .Attribute("data-cfx-symbol-layer", layer)
                .Attribute("data-cfx-custom-symbol", "true")
                .Attribute("d", options.PictorialSvgPathData)
                .Attribute("transform", $"translate({F(cx - radius)} {F(cy - radius)}) scale({F(scaleX)} {F(scaleY)}) translate({F(-vb.X)} {F(-vb.Y)})")
                .Attribute("fill", color.ToCss())
                .EndEmptyElement()
                .Line();
            return;
        }

        var shape = options.PictorialShape;
        if (shape == ChartPictorialShape.Square) {
            writer
                .StartElement("rect")
                .Attribute("data-cfx-symbol-layer", layer)
                .Attribute("x", cx - radius)
                .Attribute("y", cy - radius)
                .Attribute("width", radius * 2)
                .Attribute("height", radius * 2)
                .Attribute("rx", radius * 0.26)
                .Attribute("fill", color.ToCss())
                .EndEmptyElement()
                .Line();
        } else if (shape == ChartPictorialShape.Diamond) {
            WritePictorialPath(writer, layer, $"M {F(cx)} {F(cy - radius)} L {F(cx + radius)} {F(cy)} L {F(cx)} {F(cy + radius)} L {F(cx - radius)} {F(cy)} Z", color);
        } else if (shape == ChartPictorialShape.Triangle) {
            WritePictorialPath(writer, layer, $"M {F(cx)} {F(cy - radius)} L {F(cx + radius)} {F(cy + radius)} L {F(cx - radius)} {F(cy + radius)} Z", color);
        } else if (shape == ChartPictorialShape.Star) {
            WritePictorialPath(writer, layer, BuildStarPath(cx, cy, radius, radius * 0.44), color);
        } else if (shape == ChartPictorialShape.Heart) {
            WritePictorialPath(writer, layer, BuildHeartPath(cx, cy, radius), color);
        } else if (shape == ChartPictorialShape.Shield) {
            WritePictorialPath(writer, layer, $"M {F(cx)} {F(cy - radius)} C {F(cx + radius * 0.72)} {F(cy - radius * 0.72)} {F(cx + radius * 0.82)} {F(cy - radius * 0.64)} {F(cx + radius * 0.82)} {F(cy - radius * 0.22)} C {F(cx + radius * 0.82)} {F(cy + radius * 0.48)} {F(cx + radius * 0.35)} {F(cy + radius * 0.85)} {F(cx)} {F(cy + radius)} C {F(cx - radius * 0.35)} {F(cy + radius * 0.85)} {F(cx - radius * 0.82)} {F(cy + radius * 0.48)} {F(cx - radius * 0.82)} {F(cy - radius * 0.22)} C {F(cx - radius * 0.82)} {F(cy - radius * 0.64)} {F(cx - radius * 0.72)} {F(cy - radius * 0.72)} {F(cx)} {F(cy - radius)} Z", color);
        } else if (shape == ChartPictorialShape.Check) {
            writer
                .StartElement("path")
                .Attribute("data-cfx-symbol-layer", layer)
                .Attribute("d", $"M {F(cx - radius * 0.72)} {F(cy - radius * 0.02)} L {F(cx - radius * 0.22)} {F(cy + radius * 0.52)} L {F(cx + radius * 0.76)} {F(cy - radius * 0.56)}")
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", Math.Max(2, radius * 0.34))
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement()
                .Line();
        } else if (shape == ChartPictorialShape.Person) {
            writer
                .StartElement("g")
                .Attribute("data-cfx-symbol-layer", layer)
                .Attribute("fill", color.ToCss())
                .EndStartElement()
                .StartElement("circle")
                .Attribute("cx", cx)
                .Attribute("cy", cy - radius * 0.48)
                .Attribute("r", radius * 0.34)
                .EndEmptyElement()
                .StartElement("path")
                .Attribute("d", $"M {F(cx - radius * 0.64)} {F(cy + radius * 0.74)} C {F(cx - radius * 0.56)} {F(cy + radius * 0.02)} {F(cx - radius * 0.34)} {F(cy - radius * 0.04)} {F(cx)} {F(cy - radius * 0.04)} C {F(cx + radius * 0.34)} {F(cy - radius * 0.04)} {F(cx + radius * 0.56)} {F(cy + radius * 0.02)} {F(cx + radius * 0.64)} {F(cy + radius * 0.74)} Z")
                .EndEmptyElement()
                .EndElement()
                .Line();
        } else if (shape == ChartPictorialShape.PersonDress) {
            writer
                .StartElement("g")
                .Attribute("data-cfx-symbol-layer", layer)
                .Attribute("fill", color.ToCss())
                .EndStartElement()
                .StartElement("circle")
                .Attribute("cx", cx)
                .Attribute("cy", cy - radius * 0.55)
                .Attribute("r", radius * 0.30)
                .EndEmptyElement()
                .StartElement("path")
                .Attribute("d", $"M {F(cx)} {F(cy - radius * 0.12)} L {F(cx + radius * 0.62)} {F(cy + radius * 0.72)} L {F(cx - radius * 0.62)} {F(cy + radius * 0.72)} Z")
                .EndEmptyElement()
                .StartElement("rect")
                .Attribute("x", cx - radius * 0.30)
                .Attribute("y", cy - radius * 0.18)
                .Attribute("width", radius * 0.60)
                .Attribute("height", radius * 0.36)
                .Attribute("rx", radius * 0.14)
                .EndEmptyElement()
                .EndElement()
                .Line();
        } else {
            writer
                .StartElement("circle")
                .Attribute("data-cfx-symbol-layer", layer)
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", radius)
                .Attribute("fill", color.ToCss())
                .EndEmptyElement()
                .Line();
        }
    }

    private static void WritePictorialPath(SvgMarkupWriter writer, string layer, string path, ChartColor color) {
        writer
            .StartElement("path")
            .Attribute("data-cfx-symbol-layer", layer)
            .Attribute("d", path)
            .Attribute("fill", color.ToCss())
            .EndEmptyElement()
            .Line();
    }

    private static ChartColor PictorialOpacity(ChartColor color, double opacity) =>
        ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(255 * Clamp(opacity, 0, 1)));

    private static ChartColor PictorialItemColor(ChartSeries series, ChartTheme theme, int index) {
        if (index < series.PointColors.Count && series.PointColors[index].HasValue) return series.PointColors[index]!.Value;
        return series.Color ?? theme.Palette[index % theme.Palette.Length];
    }

    private static string BuildStarPath(double cx, double cy, double outer, double inner) {
        var sb = new StringBuilder();
        for (var i = 0; i < 10; i++) {
            var angle = -Math.PI / 2 + i * Math.PI / 5;
            var radius = i % 2 == 0 ? outer : inner;
            var x = cx + Math.Cos(angle) * radius;
            var y = cy + Math.Sin(angle) * radius;
            sb.Append(i == 0 ? "M " : " L ").Append(F(x)).Append(' ').Append(F(y));
        }

        sb.Append(" Z");
        return sb.ToString();
    }

    private static string BuildHeartPath(double cx, double cy, double radius) =>
        $"M {F(cx)} {F(cy + radius * 0.72)} C {F(cx - radius * 1.12)} {F(cy - radius * 0.08)} {F(cx - radius * 0.9)} {F(cy - radius * 0.82)} {F(cx - radius * 0.34)} {F(cy - radius * 0.64)} C {F(cx - radius * 0.12)} {F(cy - radius * 0.56)} {F(cx)} {F(cy - radius * 0.34)} {F(cx)} {F(cy - radius * 0.18)} C {F(cx)} {F(cy - radius * 0.34)} {F(cx + radius * 0.12)} {F(cy - radius * 0.56)} {F(cx + radius * 0.34)} {F(cy - radius * 0.64)} C {F(cx + radius * 0.9)} {F(cy - radius * 0.82)} {F(cx + radius * 1.12)} {F(cy - radius * 0.08)} {F(cx)} {F(cy + radius * 0.72)} Z";
}
