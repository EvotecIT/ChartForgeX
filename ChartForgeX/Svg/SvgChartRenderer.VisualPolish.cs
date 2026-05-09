using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void WriteSvgSurfaceGradient(StringBuilder sb, string id, string suffix, ChartColor color) {
        var top = ChartSurfacePolish.GradientTop(color);
        var bottom = ChartSurfacePolish.GradientBottom(color);
        AppendSvg(sb, writer => writer
            .StartElement("linearGradient")
            .Attribute("id", $"{id}-{suffix}")
            .Attribute("x1", "0")
            .Attribute("x2", "0")
            .Attribute("y1", "0")
            .Attribute("y2", "1")
            .EndStartElement()
            .StartElement("stop")
            .Attribute("offset", "0%")
            .Attribute("stop-color", top.ToHex())
            .Attribute("stop-opacity", top.A / 255.0)
            .EndEmptyElement()
            .StartElement("stop")
            .Attribute("offset", "100%")
            .Attribute("stop-color", bottom.ToHex())
            .Attribute("stop-opacity", bottom.A / 255.0)
            .EndEmptyElement()
            .EndElement()
            .Line());
    }

    private static void DrawSvgSurfaceHighlight(StringBuilder sb, double x, double y, double width, double height, double radius, double inset, double opacity, string role) {
        if (width <= inset * 2 || height <= inset * 2 || opacity <= 0) return;
        AppendSvg(sb, writer => writer.StartElement("rect")
            .Attribute("data-cfx-role", role)
            .Attribute("class", "cfx-crisp-stroke")
            .Attribute("x", x + inset)
            .Attribute("y", y + inset)
            .Attribute("width", Math.Max(0, width - inset * 2))
            .Attribute("height", Math.Max(0, height - inset * 2))
            .Attribute("rx", Math.Max(0, radius - inset))
            .Attribute("fill", "none")
            .Attribute("stroke", ChartColor.White.ToCss())
            .Attribute("stroke-opacity", opacity)
            .EndEmptyElement()
            .Line());
    }

    private static void WriteSvgGridLine(StringBuilder sb, double x1, double y1, double x2, double y2, string stroke, double strokeWidth, double opacity, ChartGridLineStyle style) =>
        WriteSvgGuideLine(sb, null, x1, y1, x2, y2, stroke, strokeWidth, opacity, style);

    private static void WriteSvgGuideLine(StringBuilder sb, string? role, double x1, double y1, double x2, double y2, string stroke, double strokeWidth, double? opacity = null, ChartGridLineStyle? style = null) {
        var horizontal = Math.Abs(y1 - y2) < 0.000001;
        var vertical = Math.Abs(x1 - x2) < 0.000001;
        if (horizontal) y1 = y2 = CrispStrokeCoordinate(y1, strokeWidth);
        if (vertical) x1 = x2 = CrispStrokeCoordinate(x1, strokeWidth);
        AppendSvg(sb, writer => {
            writer.StartElement("line")
                .Attribute("data-cfx-role", role)
                .Attribute("class", ChartVisualPrimitives.SvgGuideStrokeClass)
                .Attribute("x1", x1)
                .Attribute("y1", y1)
                .Attribute("x2", x2)
                .Attribute("y2", y2)
                .Attribute("stroke", stroke)
                .Attribute("stroke-width", strokeWidth);
            if (opacity.HasValue) writer.Attribute("opacity", opacity.Value);
            if (style != null && style.Dash > 0 && style.Gap > 0) writer.Attribute("stroke-dasharray", $"{F(style.Dash)} {F(style.Gap)}");
            writer.EndEmptyElement().Line();
        });
    }

    private static double CrispStrokeCoordinate(double value, double strokeWidth) {
        if (double.IsNaN(value) || double.IsInfinity(value)) return value;
        var roundedStroke = Math.Max(1, (int)Math.Round(strokeWidth));
        return Math.Round(value) + (roundedStroke % 2 == 0 ? 0 : 0.5);
    }

}
