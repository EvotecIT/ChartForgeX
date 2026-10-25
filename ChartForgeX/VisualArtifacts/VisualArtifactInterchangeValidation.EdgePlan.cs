using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidatePreparedEdgeUnits(VisualArtifactInterchangeEdge edge, VisualArtifactInterchangeEnvelope envelope) {
        var native = edge.Topology!; var plan = native.PreparedEdgePlan;
        if (plan == null) return;
        plan.ValidatePortable();
        if (edge.ResolvedRoute.Count < 2) throw new ArgumentException("Captured edge appearance requires its resolved endpoint observations.");
        var options = new TopologyRenderOptions(); envelope.Presentation?.TopologyCaptions?.Apply(options);
        var units = plan.DefaultUnits; var complete = envelope.Presentation?.Theme?.Definition; var theme = complete ?? VisualTheme.Graphite();
        var dataLabelSize = complete?.Typography.DataLabelSize ?? envelope.Presentation?.Theme?.DataLabelSize ?? theme.Typography.DataLabelSize;
        var model = new TopologyEdge { Kind = native.Kind, Direction = native.Direction, IsMuted = native.IsMuted,
            SourceMarker = native.SourceMarker, TargetMarker = native.TargetMarker };
        if (!native.StrokeWidth.HasValue) BoundedPreparedEdgeCoordinate(theme.SeriesStrokeWidth * units);
        var style = TopologyRenderPrimitives.EdgeVisualStyle(model, false, options);
        if (style.AmbientHaloOpacity > 0) BoundedPreparedEdgeCoordinate(style.AmbientHaloStrokeExtra * units);
        if (style.HaloOpacity > 0) BoundedPreparedEdgeCoordinate(style.HaloStrokeExtra * units);
        if (style.HighlightOpacity > 0) BoundedPreparedEdgeCoordinate(units);
        foreach (var point in edge.ResolvedRoute) { BoundedPreparedEdgeCoordinate(point.X); BoundedPreparedEdgeCoordinate(point.Y); }
        IncludeMarker(native.SourceMarkerStyle, TopologyRenderPrimitives.RenderedSourceMarker(model, options.IncludeDirectionMarkers), edge.ResolvedRoute[0]);
        IncludeMarker(native.TargetMarkerStyle, TopologyRenderPrimitives.RenderedTargetMarker(model, options.IncludeDirectionMarkers), edge.ResolvedRoute[edge.ResolvedRoute.Count - 1]);
        if (options.IncludeEdgeLabels && (!string.IsNullOrWhiteSpace(edge.Label) || !string.IsNullOrWhiteSpace(edge.SecondaryLabel) || !string.IsNullOrWhiteSpace(edge.TertiaryLabel))) {
            BoundedPreparedEdgeCoordinate((native.TextStyle?.FontSize ?? dataLabelSize * units));
            if (edge.ResolvedLabelBounds is {} label) { BoundedPreparedEdgeCoordinate(label.X); BoundedPreparedEdgeCoordinate(label.Y); BoundedPreparedEdgeCoordinate(label.Width); BoundedPreparedEdgeCoordinate(label.Height); }
        }
        if (options.IncludeEndpointLabels && (!string.IsNullOrWhiteSpace(edge.SourceLabel) || !string.IsNullOrWhiteSpace(edge.TargetLabel)))
            BoundedPreparedEdgeCoordinate(dataLabelSize * .8 * units);

        void IncludeMarker(TopologyMarkerStyle? marker, TopologyMarkerKind kind, VisualArtifactInterchangePoint point) {
            if (kind == TopologyMarkerKind.None) return;
            var extent = marker == null ? TopologyRenderPrimitives.MarkerExtent(null, theme.AxisStrokeWidth) * units
                : TopologyRenderPrimitives.MarkerExtent(marker, theme.AxisStrokeWidth * units);
            BoundedPreparedEdgeCoordinate(point.X - extent); BoundedPreparedEdgeCoordinate(point.X + extent); BoundedPreparedEdgeCoordinate(point.Y - extent); BoundedPreparedEdgeCoordinate(point.Y + extent);
        }
    }
    private static void BoundedPreparedEdgeCoordinate(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > TopologyPathGeometry.MaximumCoordinate)
            throw new ArgumentOutOfRangeException(nameof(value), "Captured edge coordinates and derived ink extents must be finite within one billion units.");
    }
}
