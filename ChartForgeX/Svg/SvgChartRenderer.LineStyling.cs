using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPremiumSvgLineSegment(StringBuilder sb, string role, int seriesIndex, double x1, double y1, double x2, double y2, ChartColor color, double strokeWidth, ChartLineVisualStyle style, string dashArray = "", Action<SvgMarkupWriter>? foregroundAttributes = null) {
        foreach (var layer in ChartLineVisualLayers.Build(color, strokeWidth, style)) {
            if (!layer.IsVisible) continue;
            DrawSvgLineSegmentLayer(sb, role + layer.RoleSuffix, seriesIndex, x1, y1, x2, y2, layer, dashArray, layer.IsForeground ? foregroundAttributes : null);
        }
    }

    private static void DrawSvgLineSegmentLayer(StringBuilder sb, string role, int seriesIndex, double x1, double y1, double x2, double y2, ChartLineVisualLayer layer, string dashArray, Action<SvgMarkupWriter>? extraAttributes = null) {
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
                .Attribute("class", ChartVisualPrimitives.SvgPremiumStrokeClass)
                .Paint("stroke", LayerPaint(layer))
                .Attribute("stroke-width", layer.StrokeWidth)
                .Attribute("stroke-linecap", "round");
            if (!string.IsNullOrWhiteSpace(dashArray)) writer.Attribute("stroke-dasharray", dashArray);
            if (layer.Opacity < 1) writer.Attribute("opacity", layer.Opacity);
            writer.EndEmptyElement().Line();
        });
    }

    /// <summary>Returns the stroke of a line layer: the sheen stays literal white, the line and its halos keep their colour.</summary>
    private static SvgPaint LayerPaint(ChartLineVisualLayer layer) => layer.IsHighlight ? SvgPaint.Literal(layer.Color) : SvgPaint.Plain(layer.Color);

    private static void DrawPremiumSvgLinePath(StringBuilder sb, string role, int seriesIndex, int pointCount, string path, ChartColor color, double strokeWidth, ChartLineVisualStyle style, Action<SvgMarkupWriter>? foregroundAttributes = null) {
        foreach (var layer in ChartLineVisualLayers.Build(color, strokeWidth, style)) {
            if (!layer.IsVisible) continue;
            AppendSvg(sb, writer => {
                writer
                    .StartElement("path")
                    .Attribute("data-cfx-role", role + layer.RoleSuffix)
                    .Attribute("data-cfx-series", seriesIndex)
                    .Attribute("data-cfx-point-count", pointCount)
                    .Attribute("d", path)
                    .Attribute("class", ChartVisualPrimitives.SvgPremiumStrokeClass)
                    .Attribute("fill", "none")
                    .Paint("stroke", LayerPaint(layer))
                    .Attribute("stroke-width", layer.StrokeWidth)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round");
                if (layer.IsForeground) foregroundAttributes?.Invoke(writer);
                if (layer.Opacity < 1) writer.Attribute("opacity", layer.Opacity);
                writer.EndEmptyElement().Line();
            });
        }
    }
}
