using System;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    /// <summary>Fits the caption once to the icon width policy shared by painting and obstacle bounds.</summary>
    public static double IconLabelFontSize(TopologyNode node, TextMeasurementContext? measurement, double preferred) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) return IconLabelFontSize(basis.OriginalNode(node), measurement, preferred) * basis.Factor;
        measurement = IconLabelMeasurement(node, measurement);
        // Node typography is a base size; retain the existing Icon role ratio.
        if (node.TextStyle?.FontSize.HasValue == true) preferred = TopologyTextPolicy.Size(node.TextStyle, 10.5, 11);
        var label = TrimTo(node.Label, NodeTitleMaxLength(node, TopologyNodeDisplayMode.Icon));
        var maximumWidth = IconLabelMaxWidth(node);
        return ChartTextFitting.FitFontSize(Math.Min(10.5, preferred), preferred,
            size => node.LabelContent.IsStructured
                ? StructuredIconLabelMetrics(node, measurement, size).Width <= maximumWidth
                : EstimateTextWidth(label, size, true, measurement, IconLabelWeight(node, measurement)) <= maximumWidth);
    }

    public static string IconLabelText(TopologyNode node, TextMeasurementContext? measurement = null, double fontSize = 10.5) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) return IconLabelText(basis.OriginalNode(node), measurement, fontSize);
        measurement = IconLabelMeasurement(node, measurement);
        fontSize = IconLabelFontSize(node, measurement, fontSize);
        return TrimToEstimatedWidth(TrimTo(node.Label, NodeTitleMaxLength(node, TopologyNodeDisplayMode.Icon)), IconLabelMaxWidth(node), fontSize, true, measurement, IconLabelWeight(node, measurement));
    }

    public static double IconLabelMaxWidth(TopologyNode node) => TopologyPreparedNodeBasis.ForPresentation(node) is {} basis
        ? IconLabelMaxWidth(basis.OriginalNode(node)) * basis.Factor : Math.Max(node.Width + 46, 72);

    public static double IconLabelPlateWidth(TopologyNode node, TextMeasurementContext? measurement = null, double fontSize = 10.5) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) return IconLabelPlateWidth(basis.OriginalNode(node), measurement, fontSize) * basis.Factor;
        measurement = IconLabelMeasurement(node, measurement);
        fontSize = IconLabelFontSize(node, measurement, fontSize);
        if (node.LabelContent.IsStructured) return Math.Max(34,
            Math.Min(IconLabelMaxWidth(node), StructuredIconLabelMetrics(node, measurement, fontSize).Width) + 12);
        return Math.Max(34, EstimateTextWidth(IconLabelText(node, measurement, fontSize), fontSize, true, measurement, IconLabelWeight(node, measurement)) + 12);
    }

    public static double IconLabelPlateHeight(TopologyNode node, TextMeasurementContext? measurement = null, double fontSize = 10.5) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) return IconLabelPlateHeight(basis.OriginalNode(node), measurement, fontSize) * basis.Factor;
        measurement = IconLabelMeasurement(node, measurement);
        fontSize = IconLabelFontSize(node, measurement, fontSize);
        if (node.LabelContent.IsStructured) return Math.Max(15,
            StructuredIconLabelMetrics(node, measurement, fontSize).Height) + 1e-8;
        var weight = IconLabelWeight(node, measurement);
        return Math.Max(15, weight.HasValue && measurement != null
            ? measurement.MeasureMetrics("Ag", fontSize, weight.Value).LineHeight
            : measurement?.MeasureLineHeight(fontSize, true) ?? fontSize * 1.5);
    }

    /// <summary>Keeps node-caption measurement on the same resolved native face as its painter.</summary>
    private static TextMeasurementContext? IconLabelMeasurement(TopologyNode node, TextMeasurementContext? measurement) =>
        node.TextStyle?.HasOverrides == true
            ? TopologyTextPolicy.Measurement(node.TextStyle, measurement ?? new TextMeasurementContext(TopologyPaint.Default.FontFamily), 700)
            : measurement;

    private static int? IconLabelWeight(TopologyNode node, TextMeasurementContext? measurement) =>
        node.TextStyle?.FontWeight != null ? measurement?.Font.Weight : null;

    // Plate, obstacle and painter use every captured sibling face, including its ascender.
    private static (double Width, double Height) StructuredIconLabelMetrics(TopologyNode node,
        TextMeasurementContext? measurement, double size) {
        var style = TopologyTextPolicy.ResolveStructured(node.TextStyle,
            measurement?.Font ?? FontSpec.FromFamily(TopologyPaint.Default.FontFamily), size);
        style.FontSize = size;
        var metrics = TextLayoutEngine.Measure(node.LabelContent, style);
        return (metrics.Width, metrics.LineHeight);
    }

    public static double IconLabelPlateY(TopologyNode node) => node.Y + node.Height
        + (string.IsNullOrWhiteSpace(NodeBadge(node)) ? 5 : 25) * TopologyPreparedNodeBasis.Units(node);
}
