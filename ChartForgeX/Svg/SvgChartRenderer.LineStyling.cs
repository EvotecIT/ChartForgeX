using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPremiumSvgLineSegment(StringBuilder sb, string role, int seriesIndex, double x1, double y1, double x2, double y2, ChartColor color, double strokeWidth, ChartLineVisualStyle style, string dashArray = "", Action<SvgMarkupWriter>? foregroundAttributes = null) {
        if (style.AmbientHaloOpacity > 0 && style.AmbientHaloStrokeExtra > 0) DrawSvgLineSegmentLayer(sb, role + "-ambient-halo", seriesIndex, x1, y1, x2, y2, color.ToCss(), strokeWidth + style.AmbientHaloStrokeExtra, style.AmbientHaloOpacity, dashArray);
        if (style.HaloOpacity > 0 && style.HaloStrokeExtra > 0) DrawSvgLineSegmentLayer(sb, role + "-halo", seriesIndex, x1, y1, x2, y2, color.ToCss(), strokeWidth + style.HaloStrokeExtra, style.HaloOpacity, dashArray);
        DrawSvgLineSegmentLayer(sb, role, seriesIndex, x1, y1, x2, y2, color.ToCss(), strokeWidth, 1, dashArray, foregroundAttributes);
        if (style.HighlightOpacity > 0) DrawSvgLineSegmentLayer(sb, role + "-highlight", seriesIndex, x1, y1, x2, y2, ChartColor.White.ToCss(), Math.Max(1.0, strokeWidth * style.HighlightStrokeRatio), style.HighlightOpacity, dashArray);
    }

    private static void DrawSvgLineSegmentLayer(StringBuilder sb, string role, int seriesIndex, double x1, double y1, double x2, double y2, string stroke, double strokeWidth, double opacity, string dashArray, Action<SvgMarkupWriter>? extraAttributes = null) {
        AppendSvg(sb, writer => {
            writer
                .StartElement("line")
                .Attribute("data-cfx-role", role)
                .Attribute("data-cfx-series", seriesIndex);
            extraAttributes?.Invoke(writer);
            writer
                .Attribute("x1", x1)
                .Attribute("y1", y1)
                .Attribute("x2", x2)
                .Attribute("y2", y2)
                .Attribute("stroke", stroke)
                .Attribute("stroke-width", strokeWidth)
                .Attribute("stroke-linecap", "round");
            if (!string.IsNullOrWhiteSpace(dashArray)) writer.Attribute("stroke-dasharray", dashArray);
            if (opacity < 1) writer.Attribute("opacity", opacity);
            writer.EndEmptyElement().Line();
        });
    }

    private static void DrawPremiumSvgLinePath(StringBuilder sb, string role, int seriesIndex, int pointCount, string path, ChartColor color, double strokeWidth, ChartLineVisualStyle style) {
        var highlightWidth = Math.Max(1.0, strokeWidth * style.HighlightStrokeRatio);
        if (style.AmbientHaloOpacity > 0 && style.AmbientHaloStrokeExtra > 0) {
            AppendSvg(sb, writer => writer
                .StartElement("path")
                .Attribute("data-cfx-role", role + "-ambient-halo")
                .Attribute("data-cfx-series", seriesIndex)
                .Attribute("data-cfx-point-count", pointCount)
                .Attribute("d", path)
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", strokeWidth + style.AmbientHaloStrokeExtra)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .Attribute("opacity", style.AmbientHaloOpacity)
                .EndEmptyElement()
                .Line());
        }

        if (style.HaloOpacity > 0 && style.HaloStrokeExtra > 0) {
            AppendSvg(sb, writer => writer
                .StartElement("path")
                .Attribute("data-cfx-role", role + "-halo")
                .Attribute("data-cfx-series", seriesIndex)
                .Attribute("data-cfx-point-count", pointCount)
                .Attribute("d", path)
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", strokeWidth + style.HaloStrokeExtra)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .Attribute("opacity", style.HaloOpacity)
                .EndEmptyElement()
                .Line());
        }

        AppendSvg(sb, writer => writer
            .StartElement("path")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point-count", pointCount)
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", strokeWidth)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .EndEmptyElement()
            .Line());
        if (style.HighlightOpacity > 0) {
            AppendSvg(sb, writer => writer
                .StartElement("path")
                .Attribute("data-cfx-role", role + "-highlight")
                .Attribute("data-cfx-series", seriesIndex)
                .Attribute("data-cfx-point-count", pointCount)
                .Attribute("d", path)
                .Attribute("fill", "none")
                .Attribute("stroke", ChartColor.White.ToCss())
                .Attribute("stroke-width", highlightWidth)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round")
                .Attribute("opacity", style.HighlightOpacity)
                .EndEmptyElement()
                .Line());
        }
    }
}
