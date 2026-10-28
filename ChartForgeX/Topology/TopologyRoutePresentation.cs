using System;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

/// <summary>Detached finite native edge presentation required to repaint and recompute a prepared route.</summary>
/// <remarks>The existing shared caption capture owns its eight node/multiline/readable fields. This adds only
/// actual route, edge-ink and visibility choices; request fitting, selection and host callbacks remain caller owned.</remarks>
internal sealed class TopologyRoutePresentation {
    private readonly ChartLineVisualStyle? _lineStyle;
    internal TopologyRoutePresentation(bool directionMarkers, bool edgeLabels, bool endpointLabels, bool backplates,
        bool leaders, bool groups, bool groupLabels, bool trunks, bool force, TopologyVisualStyle visualStyle,
        TopologyArrowMarkerStyle arrow, TopologyEdgeCornerStyle corners, double radius,
        TextMeasurementMode measurementMode, ChartLineVisualStyle? lineStyle) {
        IncludeDirectionMarkers = directionMarkers; IncludeEdgeLabels = edgeLabels; IncludeEndpointLabels = endpointLabels;
        IncludeEdgeLabelBackplates = backplates; IncludeEdgeLabelLeaders = leaders; IncludeGroups = groups; IncludeGroupLabels = groupLabels;
        ShareIncomingTrunks = trunks; UseForceGraphPresentation = force; VisualStyle = visualStyle;
        ArrowMarkerStyle = arrow; EdgeCornerStyle = corners; EdgeCornerRadius = radius; TextMeasurementMode = measurementMode;
        _lineStyle = lineStyle?.Clone();
    }
    internal bool IncludeDirectionMarkers { get; }
    internal bool IncludeEdgeLabels { get; }
    internal bool IncludeEndpointLabels { get; }
    internal bool IncludeEdgeLabelBackplates { get; }
    internal bool IncludeEdgeLabelLeaders { get; }
    internal bool IncludeGroups { get; }
    internal bool IncludeGroupLabels { get; }
    internal bool ShareIncomingTrunks { get; }
    internal bool UseForceGraphPresentation { get; }
    internal TopologyVisualStyle VisualStyle { get; }
    internal TopologyArrowMarkerStyle ArrowMarkerStyle { get; }
    internal TopologyEdgeCornerStyle EdgeCornerStyle { get; }
    internal double EdgeCornerRadius { get; }
    internal TextMeasurementMode TextMeasurementMode { get; }
    internal ChartLineVisualStyle? LineStyle => _lineStyle?.Clone();

    internal static TopologyRoutePresentation? Capture(TopologyRenderOptions options) {
        var ordinary = new TopologyRenderOptions();
        if (options.IncludeDirectionMarkers == ordinary.IncludeDirectionMarkers && options.IncludeEdgeLabels == ordinary.IncludeEdgeLabels
            && options.IncludeEndpointLabels == ordinary.IncludeEndpointLabels && options.IncludeEdgeLabelBackplates == ordinary.IncludeEdgeLabelBackplates
            && options.IncludeEdgeLabelLeaders == ordinary.IncludeEdgeLabelLeaders && options.IncludeGroups == ordinary.IncludeGroups
            && options.IncludeGroupLabels == ordinary.IncludeGroupLabels && options.ShareIncomingTrunks == ordinary.ShareIncomingTrunks
            && options.UseForceGraphPresentation == ordinary.UseForceGraphPresentation && options.VisualStyle == ordinary.VisualStyle
            && options.ArrowMarkerStyle == ordinary.ArrowMarkerStyle && options.EdgeCornerStyle == ordinary.EdgeCornerStyle
            && options.EdgeCornerRadius == ordinary.EdgeCornerRadius && options.TextMeasurementMode == ordinary.TextMeasurementMode && options.EdgeVisualStyle == null) return null;
        return new TopologyRoutePresentation(options.IncludeDirectionMarkers, options.IncludeEdgeLabels, options.IncludeEndpointLabels,
            options.IncludeEdgeLabelBackplates, options.IncludeEdgeLabelLeaders, options.IncludeGroups, options.IncludeGroupLabels,
            options.ShareIncomingTrunks, options.UseForceGraphPresentation, options.VisualStyle, options.ArrowMarkerStyle,
            options.EdgeCornerStyle, options.EdgeCornerRadius, options.TextMeasurementMode, options.EdgeVisualStyle);
    }
    internal void Apply(TopologyRenderOptions options) {
        options.IncludeDirectionMarkers = IncludeDirectionMarkers; options.IncludeEdgeLabels = IncludeEdgeLabels; options.IncludeEndpointLabels = IncludeEndpointLabels;
        options.IncludeEdgeLabelBackplates = IncludeEdgeLabelBackplates; options.IncludeEdgeLabelLeaders = IncludeEdgeLabelLeaders;
        options.IncludeGroups = IncludeGroups; options.IncludeGroupLabels = IncludeGroupLabels; options.ShareIncomingTrunks = ShareIncomingTrunks;
        options.UseForceGraphPresentation = UseForceGraphPresentation; options.VisualStyle = VisualStyle; options.ArrowMarkerStyle = ArrowMarkerStyle;
        options.EdgeCornerStyle = EdgeCornerStyle; options.EdgeCornerRadius = EdgeCornerRadius; options.TextMeasurementMode = TextMeasurementMode;
        options.EdgeVisualStyle = _lineStyle;
    }
    internal void ValidatePortable() {
        TopologyModelGuards.EnumDefined(VisualStyle, nameof(VisualStyle)); TopologyModelGuards.EnumDefined(ArrowMarkerStyle, nameof(ArrowMarkerStyle));
        TopologyModelGuards.EnumDefined(EdgeCornerStyle, nameof(EdgeCornerStyle)); TopologyModelGuards.EnumDefined(TextMeasurementMode, nameof(TextMeasurementMode));
        Bounded(EdgeCornerRadius);
        if (_lineStyle != null) { Bounded(_lineStyle.AmbientHaloStrokeExtra); Bounded(_lineStyle.HaloStrokeExtra); }
    }
    private static void Bounded(double value) {
        TopologyModelGuards.NonNegativeFinite(value, nameof(value));
        if (value > TopologyPathGeometry.MaximumCoordinate) throw new ArgumentOutOfRangeException(nameof(value), "Captured route dimensions exceed the portable coordinate limit.");
    }
}
