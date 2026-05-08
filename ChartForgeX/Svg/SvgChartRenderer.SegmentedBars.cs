using System;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static SvgMarkupWriter WriteSvgSegmentedCapLayers(SvgMarkupWriter writer, string rolePrefix, int seriesIndex, int pointIndex, ChartSegmentedBarGeometry geometry, ChartBarVisualStyle style, string color, Action<SvgMarkupWriter>? commonAttributes = null, Action<SvgMarkupWriter>? capAttributes = null) {
        WriteSvgSegmentedCapLine(writer, rolePrefix + "-cap-shadow-soft", seriesIndex, pointIndex, geometry.SoftShadow, color, geometry.CapThickness + style.CapShadowSpread * 2.0, style.CapShadowOpacity * 0.36, commonAttributes);
        WriteSvgSegmentedCapLine(writer, rolePrefix + "-cap-shadow", seriesIndex, pointIndex, geometry.Shadow, color, geometry.CapThickness, style.CapShadowOpacity, commonAttributes);
        WriteSvgSegmentedCapLine(writer, rolePrefix + "-cap", seriesIndex, pointIndex, geometry.Cap, color, geometry.CapThickness, style.CapOpacity, capAttributes ?? commonAttributes);
        WriteSvgSegmentedCapLine(writer, rolePrefix + "-cap-highlight", seriesIndex, pointIndex, geometry.Highlight, "#fff", Math.Max(1.0, geometry.CapThickness * ChartVisualPrimitives.SegmentedCapHighlightStrokeRatio), style.CapHighlightOpacity, capAttributes ?? commonAttributes);
        return writer;
    }

    private static void WriteSvgSegmentedCapLine(SvgMarkupWriter writer, string role, int seriesIndex, int pointIndex, ChartSegmentedLine line, string stroke, double strokeWidth, double strokeOpacity, Action<SvgMarkupWriter>? extraAttributes) {
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex);
        extraAttributes?.Invoke(writer);
        writer
            .Attribute("x1", line.X1)
            .Attribute("y1", line.Y1)
            .Attribute("x2", line.X2)
            .Attribute("y2", line.Y2)
            .Attribute("stroke", stroke)
            .Attribute("stroke-width", strokeWidth)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-opacity", strokeOpacity)
            .EndEmptyElement();
    }
}
