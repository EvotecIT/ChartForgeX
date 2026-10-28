using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void TopologyRoutes(VisualArtifactInterchangeJsonWriter writer, TopologyRoutePresentation? routes) {
        if (routes == null) return;
        writer.Property("routes"); writer.StartObject();
        Boolean(writer, "directionMarkers", routes.IncludeDirectionMarkers); Boolean(writer, "edgeLabels", routes.IncludeEdgeLabels);
        Boolean(writer, "endpointLabels", routes.IncludeEndpointLabels); Boolean(writer, "backplates", routes.IncludeEdgeLabelBackplates);
        Boolean(writer, "leaders", routes.IncludeEdgeLabelLeaders); Boolean(writer, "groups", routes.IncludeGroups); Boolean(writer, "groupLabels", routes.IncludeGroupLabels);
        Boolean(writer, "trunks", routes.ShareIncomingTrunks); Boolean(writer, "force", routes.UseForceGraphPresentation);
        String(writer, "visualStyle", routes.VisualStyle.ToString()); String(writer, "arrow", routes.ArrowMarkerStyle.ToString());
        String(writer, "corners", routes.EdgeCornerStyle.ToString()); Number(writer, "radius", routes.EdgeCornerRadius);
        String(writer, "measurement", routes.TextMeasurementMode.ToString());
        var style = routes.LineStyle;
        if (style != null) {
            writer.Property("lineStyle"); writer.StartObject();
            Number(writer, "ambientOpacity", style.AmbientHaloOpacity); Number(writer, "ambientExtra", style.AmbientHaloStrokeExtra);
            Number(writer, "haloOpacity", style.HaloOpacity); Number(writer, "haloExtra", style.HaloStrokeExtra);
            Number(writer, "highlightOpacity", style.HighlightOpacity); Number(writer, "highlightRatio", style.HighlightStrokeRatio);
            writer.EndObject();
        }
        writer.EndObject();
    }
    private static TopologyRoutePresentation? ReadTopologyRoutes(Dictionary<string, GeoJsonValue> parent) {
        var item = OptionalObject(parent, "routes"); if (item == null) return null;
        bool Flag(string name) => OptionalBool(item, name) ?? throw new ArgumentException("Captured route policy requires boolean " + name + ".");
        var ink = OptionalObject(item, "lineStyle");
        var style = ink == null ? null : new ChartLineVisualStyle {
            AmbientHaloOpacity = RequiredNumber(ink, "ambientOpacity"), AmbientHaloStrokeExtra = RequiredNumber(ink, "ambientExtra"),
            HaloOpacity = RequiredNumber(ink, "haloOpacity"), HaloStrokeExtra = RequiredNumber(ink, "haloExtra"),
            HighlightOpacity = RequiredNumber(ink, "highlightOpacity"), HighlightStrokeRatio = RequiredNumber(ink, "highlightRatio")
        };
        return new TopologyRoutePresentation(Flag("directionMarkers"), Flag("edgeLabels"), Flag("endpointLabels"), Flag("backplates"),
            Flag("leaders"), Flag("groups"), Flag("groupLabels"), Flag("trunks"), Flag("force"),
            RequiredEnum<TopologyVisualStyle>(item, "visualStyle"), RequiredEnum<TopologyArrowMarkerStyle>(item, "arrow"),
            RequiredEnum<TopologyEdgeCornerStyle>(item, "corners"), RequiredNumber(item, "radius"),
            RequiredEnum<TextMeasurementMode>(item, "measurement"), style);
    }
}
