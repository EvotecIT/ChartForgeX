using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void ArtifactSemantics(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeEnvelope envelope) {
        if (envelope.Topology != null) {
            writer.Property("topology");
            writer.StartObject();
            String(writer, "layoutMode", envelope.Topology.LayoutMode.ToString());
            String(writer, "layoutDirection", envelope.Topology.LayoutDirection.ToString());
            writer.EndObject();
        }
        if (envelope.Flow != null) {
            writer.Property("flow");
            writer.StartObject();
            String(writer, "layoutMode", envelope.Flow.LayoutMode.ToString());
            String(writer, "layoutDirection", envelope.Flow.LayoutDirection.ToString());
            writer.EndObject();
        }
        if (envelope.Sequence != null) {
            writer.Property("sequence");
            writer.StartObject();
            writer.EndObject();
        }
    }

    private static void Presentation(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangePresentation? presentation) {
        if (presentation == null) return;
        writer.Property("presentation");
        writer.StartObject();
        Theme(writer, presentation.Theme);
        MapViewport(writer, presentation.MapViewport);
        Legend(writer, presentation.Legend);
        writer.EndObject();
    }

    private static void Theme(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTheme? theme) {
        if (theme == null) return;
        writer.Property("theme");
        writer.StartObject();
        String(writer, "background", theme.Background);
        String(writer, "foreground", theme.Foreground);
        String(writer, "mutedForeground", theme.MutedForeground);
        String(writer, "card", theme.Card);
        String(writer, "surface", theme.Surface);
        String(writer, "border", theme.Border);
        String(writer, "accent", theme.Accent);
        String(writer, "healthy", theme.Healthy);
        String(writer, "warning", theme.Warning);
        String(writer, "critical", theme.Critical);
        String(writer, "unknown", theme.Unknown);
        String(writer, "disabled", theme.Disabled);
        String(writer, "fontFamily", theme.FontFamily);
        writer.EndObject();
    }

    private static void MapViewport(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeMapViewport? viewport) {
        if (viewport == null) return;
        writer.Property("mapViewport");
        writer.StartObject();
        OptionalString(writer, "name", viewport.Name);
        String(writer, "projection", viewport.Projection);
        Number(writer, "minimumLongitude", viewport.MinimumLongitude);
        Number(writer, "maximumLongitude", viewport.MaximumLongitude);
        Number(writer, "minimumLatitude", viewport.MinimumLatitude);
        Number(writer, "maximumLatitude", viewport.MaximumLatitude);
        writer.EndObject();
    }

    private static void Legend(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeLegend? legend) {
        if (legend == null) return;
        writer.Property("legend");
        writer.StartObject();
        OptionalString(writer, "title", legend.Title);
        writer.Property("items");
        writer.StartArray();
        foreach (var item in legend.Items) {
            writer.StartObject();
            String(writer, "label", item.Label);
            String(writer, "kind", item.Kind.ToString());
            OptionalEnum(writer, "status", item.Status);
            OptionalEnum(writer, "nodeKind", item.NodeKind);
            OptionalEnum(writer, "edgeKind", item.EdgeKind);
            OptionalString(writer, "symbol", item.Symbol);
            OptionalString(writer, "iconId", item.IconId);
            OptionalString(writer, "color", item.Color);
            OptionalString(writer, "backgroundColor", item.BackgroundColor);
            String(writer, "lineStyle", item.LineStyle.ToString());
            writer.EndObject();
        }
        writer.EndArray();
        writer.EndObject();
    }

    private static void TopologyGroup(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTopologyGroup? topology) {
        if (topology == null) return;
        writer.Property("topology");
        writer.StartObject();
        String(writer, "status", topology.Status.ToString());
        String(writer, "layoutPolicy", topology.LayoutPolicy.ToString());
        String(writer, "appliedLayoutPolicy", topology.AppliedLayoutPolicy.ToString());
        OptionalNumber(writer, "longitude", topology.Longitude);
        OptionalNumber(writer, "latitude", topology.Latitude);
        OptionalString(writer, "iconId", topology.IconId);
        OptionalString(writer, "symbol", topology.Symbol);
        writer.EndObject();
    }

    private static void TopologyNode(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTopologyNode? topology) {
        if (topology == null) return;
        writer.Property("topology");
        writer.StartObject();
        String(writer, "kind", topology.Kind.ToString());
        String(writer, "status", topology.Status.ToString());
        String(writer, "displayMode", topology.DisplayMode.ToString());
        OptionalNumber(writer, "longitude", topology.Longitude);
        OptionalNumber(writer, "latitude", topology.Latitude);
        Boolean(writer, "showStatusBadge", topology.ShowStatusBadge);
        OptionalNumber(writer, "maximumLabelCharacters", topology.MaximumLabelCharacters);
        Artwork(writer, topology.Artwork);
        writer.EndObject();
    }

    private static void Artwork(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeArtwork? artwork) {
        if (artwork == null) return;
        writer.Property("artwork");
        writer.StartObject();
        String(writer, "status", artwork.Status.ToString());
        OptionalString(writer, "svgViewBox", artwork.SvgViewBox);
        OptionalString(writer, "preserveAspectRatio", artwork.PreserveAspectRatio);
        OptionalString(writer, "svgBody", artwork.SvgBody);
        OptionalString(writer, "svgPath", artwork.SvgPath);
        OptionalString(writer, "previewPath", artwork.PreviewPath);
        OptionalString(writer, "imageHref", artwork.ImageHref);
        writer.EndObject();
    }

    private static void TopologyEdge(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTopologyEdge? topology) {
        if (topology == null) return;
        writer.Property("topology");
        writer.StartObject();
        String(writer, "kind", topology.Kind.ToString());
        String(writer, "status", topology.Status.ToString());
        String(writer, "direction", topology.Direction.ToString());
        String(writer, "sourcePort", topology.SourcePort.ToString());
        String(writer, "targetPort", topology.TargetPort.ToString());
        String(writer, "lineStyle", topology.LineStyle.ToString());
        String(writer, "routing", topology.Routing.ToString());
        String(writer, "emphasis", topology.Emphasis.ToString());
        OptionalEnum(writer, "sourceMarker", topology.SourceMarker);
        OptionalEnum(writer, "targetMarker", topology.TargetMarker);
        OptionalNumber(writer, "strokeWidth", topology.StrokeWidth);
        OptionalNumber(writer, "opacity", topology.Opacity);
        writer.Property("dashPattern");
        writer.StartArray();
        foreach (double value in topology.DashPattern) writer.Number(value);
        writer.EndArray();
        writer.Property("waypoints");
        writer.StartArray();
        foreach (var point in topology.Waypoints) Point(writer, point);
        writer.EndArray();
        Boolean(writer, "muted", topology.IsMuted);
        Number(writer, "routingPriority", topology.RoutingPriority);
        OptionalNumber(writer, "routeLane", topology.RouteLane);
        Number(writer, "labelOffsetX", topology.LabelOffsetX);
        Number(writer, "labelOffsetY", topology.LabelOffsetY);
        if (topology.LabelAnchor != null) {
            writer.Property("labelAnchor");
            Point(writer, topology.LabelAnchor);
        }
        OptionalString(writer, "labelAnchorNodeId", topology.LabelAnchorNodeId);
        String(writer, "layoutInference", topology.LayoutInference.ToString());
        OptionalNumber(writer, "preferredLength", topology.PreferredLength);
        Number(writer, "minimumRankSpan", topology.MinimumRankSpan);
        writer.EndObject();
    }

    private static void Point(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangePoint point) {
        writer.StartObject();
        Number(writer, "x", point.X);
        Number(writer, "y", point.Y);
        writer.EndObject();
    }

    private static void FlowNode(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeFlowNode? flow) {
        if (flow == null) return;
        writer.Property("flow");
        writer.StartObject();
        String(writer, "kind", flow.Kind.ToString());
        writer.EndObject();
    }

    private static void FlowEdge(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeFlowEdge? flow) {
        if (flow == null) return;
        writer.Property("flow");
        writer.StartObject();
        String(writer, "kind", flow.Kind.ToString());
        String(writer, "direction", flow.Direction.ToString());
        writer.EndObject();
    }

    private static void SequenceNode(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeSequenceNode? sequence) {
        if (sequence == null) return;
        writer.Property("sequence");
        writer.StartObject();
        String(writer, "kind", sequence.Kind.ToString());
        Number(writer, "order", sequence.Order);
        Boolean(writer, "implicit", sequence.IsImplicit);
        writer.EndObject();
    }

    private static void SequenceEdge(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeSequenceEdge? sequence) {
        if (sequence == null) return;
        writer.Property("sequence");
        writer.StartObject();
        String(writer, "kind", sequence.Kind.ToString());
        String(writer, "lineStyle", sequence.LineStyle.ToString());
        Boolean(writer, "activatesTarget", sequence.ActivatesTarget);
        Boolean(writer, "deactivates", sequence.Deactivates);
        writer.EndObject();
    }

    private static void SequenceAnnotation(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeSequenceAnnotation? sequence) {
        if (sequence == null) return;
        writer.Property("sequence");
        writer.StartObject();
        if (sequence.ActivationState.HasValue) Boolean(writer, "activationState", sequence.ActivationState.Value);
        OptionalEnum(writer, "notePlacement", sequence.NotePlacement);
        OptionalEnum(writer, "blockKind", sequence.BlockKind);
        OptionalEnum(writer, "parentBlockKind", sequence.ParentBlockKind);
        OptionalString(writer, "branchKind", sequence.BranchKind);
        Number(writer, "depth", sequence.Depth);
        Boolean(writer, "empty", sequence.IsEmpty);
        writer.EndObject();
    }

    private static VisualArtifactInterchangeTopologyArtifact? ReadTopologyArtifact(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "topology");
        return item == null ? null : new VisualArtifactInterchangeTopologyArtifact {
            LayoutMode = RequiredEnum<ChartForgeX.Topology.TopologyLayoutMode>(item, "layoutMode"),
            LayoutDirection = RequiredEnum<ChartForgeX.Topology.TopologyLayoutDirection>(item, "layoutDirection")
        };
    }

    private static VisualArtifactInterchangeFlowArtifact? ReadFlowArtifact(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "flow");
        return item == null ? null : new VisualArtifactInterchangeFlowArtifact {
            LayoutMode = RequiredEnum<FlowArtifactLayoutMode>(item, "layoutMode"),
            LayoutDirection = RequiredEnum<FlowArtifactDirection>(item, "layoutDirection")
        };
    }

    private static VisualArtifactInterchangeSequenceArtifact? ReadSequenceArtifact(Dictionary<string, GeoJsonValue> root) =>
        OptionalObject(root, "sequence") == null ? null : new VisualArtifactInterchangeSequenceArtifact();

    private static VisualArtifactInterchangePresentation? ReadPresentation(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "presentation");
        if (item == null) return null;
        return new VisualArtifactInterchangePresentation {
            Theme = ReadTheme(item),
            MapViewport = ReadMapViewport(item),
            Legend = ReadLegend(item)
        };
    }

    private static VisualArtifactInterchangeTheme? ReadTheme(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "theme");
        if (item == null) return null;
        return new VisualArtifactInterchangeTheme {
            Background = RequiredString(item, "background"), Foreground = RequiredString(item, "foreground"),
            MutedForeground = RequiredString(item, "mutedForeground"), Card = RequiredString(item, "card"),
            Surface = RequiredString(item, "surface"), Border = RequiredString(item, "border"), Accent = RequiredString(item, "accent"),
            Healthy = RequiredString(item, "healthy"), Warning = RequiredString(item, "warning"), Critical = RequiredString(item, "critical"),
            Unknown = RequiredString(item, "unknown"), Disabled = RequiredString(item, "disabled"), FontFamily = RequiredString(item, "fontFamily")
        };
    }

    private static VisualArtifactInterchangeMapViewport? ReadMapViewport(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "mapViewport");
        if (item == null) return null;
        return new VisualArtifactInterchangeMapViewport {
            Name = OptionalString(item, "name"), Projection = RequiredString(item, "projection"),
            MinimumLongitude = RequiredNumber(item, "minimumLongitude"),
            MaximumLongitude = RequiredNumber(item, "maximumLongitude"),
            MinimumLatitude = RequiredNumber(item, "minimumLatitude"),
            MaximumLatitude = RequiredNumber(item, "maximumLatitude")
        };
    }

    private static VisualArtifactInterchangeLegend? ReadLegend(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "legend");
        if (item == null) return null;
        var legend = new VisualArtifactInterchangeLegend { Title = OptionalString(item, "title") };
        foreach (GeoJsonValue value in OptionalArray(item, "items")) {
            var legendItem = value.AsObject("legend item");
            legend.Items.Add(new VisualArtifactInterchangeLegendItem {
                Label = RequiredString(legendItem, "label"),
                Kind = RequiredEnum<ChartForgeX.Topology.TopologyLegendItemKind>(legendItem, "kind"),
                Status = OptionalEnum<ChartForgeX.Topology.TopologyHealthStatus>(legendItem, "status"),
                NodeKind = OptionalEnum<ChartForgeX.Topology.TopologyNodeKind>(legendItem, "nodeKind"),
                EdgeKind = OptionalEnum<ChartForgeX.Topology.TopologyEdgeKind>(legendItem, "edgeKind"),
                Symbol = OptionalString(legendItem, "symbol"), IconId = OptionalString(legendItem, "iconId"),
                Color = OptionalString(legendItem, "color"), BackgroundColor = OptionalString(legendItem, "backgroundColor"),
                LineStyle = RequiredEnum<ChartForgeX.Topology.TopologyEdgeLineStyle>(legendItem, "lineStyle")
            });
        }
        return legend;
    }

    private static VisualArtifactInterchangeTopologyGroup? ReadTopologyGroup(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "topology");
        if (item == null) return null;
        return new VisualArtifactInterchangeTopologyGroup {
            Status = RequiredEnum<ChartForgeX.Topology.TopologyHealthStatus>(item, "status"),
            LayoutPolicy = RequiredEnum<ChartForgeX.Topology.TopologyGroupLayoutPolicy>(item, "layoutPolicy"),
            AppliedLayoutPolicy = RequiredEnum<ChartForgeX.Topology.TopologyGroupLayoutPolicy>(item, "appliedLayoutPolicy"),
            Longitude = OptionalNumber(item, "longitude"), Latitude = OptionalNumber(item, "latitude"),
            IconId = OptionalString(item, "iconId"), Symbol = OptionalString(item, "symbol")
        };
    }

    private static VisualArtifactInterchangeTopologyNode? ReadTopologyNode(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "topology");
        if (item == null) return null;
        return new VisualArtifactInterchangeTopologyNode {
            Kind = RequiredEnum<ChartForgeX.Topology.TopologyNodeKind>(item, "kind"),
            Status = RequiredEnum<ChartForgeX.Topology.TopologyHealthStatus>(item, "status"),
            DisplayMode = RequiredEnum<ChartForgeX.Topology.TopologyNodeDisplayMode>(item, "displayMode"),
            Longitude = OptionalNumber(item, "longitude"), Latitude = OptionalNumber(item, "latitude"),
            ShowStatusBadge = OptionalBool(item, "showStatusBadge") ?? true,
            MaximumLabelCharacters = OptionalInt(item, "maximumLabelCharacters"), Artwork = ReadArtwork(item)
        };
    }

    private static VisualArtifactInterchangeArtwork? ReadArtwork(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "artwork");
        if (item == null) return null;
        return new VisualArtifactInterchangeArtwork {
            Status = RequiredEnum<VisualArtifactInterchangeArtworkStatus>(item, "status"),
            SvgViewBox = OptionalString(item, "svgViewBox"), PreserveAspectRatio = OptionalString(item, "preserveAspectRatio"),
            SvgBody = OptionalString(item, "svgBody"), SvgPath = OptionalString(item, "svgPath"),
            PreviewPath = OptionalString(item, "previewPath"), ImageHref = OptionalString(item, "imageHref")
        };
    }

    private static VisualArtifactInterchangeTopologyEdge? ReadTopologyEdge(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "topology");
        if (item == null) return null;
        var result = new VisualArtifactInterchangeTopologyEdge {
            Kind = RequiredEnum<ChartForgeX.Topology.TopologyEdgeKind>(item, "kind"),
            Status = RequiredEnum<ChartForgeX.Topology.TopologyHealthStatus>(item, "status"),
            Direction = RequiredEnum<ChartForgeX.Primitives.VisualLinkDirection>(item, "direction"),
            SourcePort = RequiredEnum<ChartForgeX.Topology.TopologyEdgePort>(item, "sourcePort"),
            TargetPort = RequiredEnum<ChartForgeX.Topology.TopologyEdgePort>(item, "targetPort"),
            LineStyle = RequiredEnum<ChartForgeX.Topology.TopologyEdgeLineStyle>(item, "lineStyle"),
            Routing = RequiredEnum<ChartForgeX.Topology.TopologyEdgeRouting>(item, "routing"),
            Emphasis = RequiredEnum<ChartForgeX.Topology.TopologyEdgeEmphasis>(item, "emphasis"),
            SourceMarker = OptionalEnum<ChartForgeX.Topology.TopologyMarkerKind>(item, "sourceMarker"),
            TargetMarker = OptionalEnum<ChartForgeX.Topology.TopologyMarkerKind>(item, "targetMarker"),
            StrokeWidth = OptionalNumber(item, "strokeWidth"), Opacity = OptionalNumber(item, "opacity"),
            IsMuted = OptionalBool(item, "muted") ?? false, RoutingPriority = OptionalInt(item, "routingPriority") ?? 0,
            RouteLane = OptionalNumber(item, "routeLane"), LabelOffsetX = OptionalNumber(item, "labelOffsetX") ?? 0,
            LabelOffsetY = OptionalNumber(item, "labelOffsetY") ?? 0, LabelAnchorNodeId = OptionalString(item, "labelAnchorNodeId"),
            LayoutInference = RequiredFlagsEnum<ChartForgeX.Topology.TopologyEdgeLayoutInference>(item, "layoutInference"),
            PreferredLength = OptionalNumber(item, "preferredLength"), MinimumRankSpan = OptionalInt(item, "minimumRankSpan") ?? 0
        };
        foreach (GeoJsonValue value in OptionalArray(item, "dashPattern")) result.DashPattern.Add(value.AsNumber("dash pattern value"));
        foreach (GeoJsonValue value in OptionalArray(item, "waypoints")) result.Waypoints.Add(ReadPoint(value.AsObject("waypoint")));
        Dictionary<string, GeoJsonValue>? anchor = OptionalObject(item, "labelAnchor");
        if (anchor != null) result.LabelAnchor = ReadPoint(anchor);
        return result;
    }

    private static VisualArtifactInterchangePoint ReadPoint(Dictionary<string, GeoJsonValue> item) => new() {
        X = RequiredNumber(item, "x"),
        Y = RequiredNumber(item, "y")
    };

    private static VisualArtifactInterchangeFlowNode? ReadFlowNode(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "flow");
        return item == null ? null : new VisualArtifactInterchangeFlowNode { Kind = RequiredEnum<FlowArtifactStepKind>(item, "kind") };
    }

    private static VisualArtifactInterchangeFlowEdge? ReadFlowEdge(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "flow");
        return item == null ? null : new VisualArtifactInterchangeFlowEdge {
            Kind = RequiredEnum<FlowArtifactConnectorKind>(item, "kind"),
            Direction = RequiredEnum<ChartForgeX.Primitives.VisualLinkDirection>(item, "direction")
        };
    }

    private static VisualArtifactInterchangeSequenceNode? ReadSequenceNode(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "sequence");
        return item == null ? null : new VisualArtifactInterchangeSequenceNode {
            Kind = RequiredEnum<SequenceArtifactParticipantKind>(item, "kind"),
            Order = OptionalInt(item, "order") ?? 0,
            IsImplicit = OptionalBool(item, "implicit") ?? false
        };
    }

    private static VisualArtifactInterchangeSequenceEdge? ReadSequenceEdge(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "sequence");
        return item == null ? null : new VisualArtifactInterchangeSequenceEdge {
            Kind = RequiredEnum<SequenceArtifactMessageKind>(item, "kind"),
            LineStyle = RequiredEnum<SequenceArtifactMessageLineStyle>(item, "lineStyle"),
            ActivatesTarget = OptionalBool(item, "activatesTarget") ?? false,
            Deactivates = OptionalBool(item, "deactivates") ?? false
        };
    }

    private static VisualArtifactInterchangeSequenceAnnotation? ReadSequenceAnnotation(Dictionary<string, GeoJsonValue> root) {
        Dictionary<string, GeoJsonValue>? item = OptionalObject(root, "sequence");
        return item == null ? null : new VisualArtifactInterchangeSequenceAnnotation {
            ActivationState = OptionalBool(item, "activationState"),
            NotePlacement = OptionalEnum<SequenceArtifactNotePlacement>(item, "notePlacement"),
            BlockKind = OptionalEnum<SequenceArtifactBlockKind>(item, "blockKind"),
            ParentBlockKind = OptionalEnum<SequenceArtifactBlockKind>(item, "parentBlockKind"),
            BranchKind = OptionalString(item, "branchKind"), Depth = OptionalInt(item, "depth") ?? 0,
            IsEmpty = OptionalBool(item, "empty") ?? false
        };
    }

    private static Dictionary<string, GeoJsonValue>? OptionalObject(Dictionary<string, GeoJsonValue> values, string name) {
        return values.TryGetValue(name, out GeoJsonValue? value) && !value.IsNull ? value.AsObject(name) : null;
    }

    private static void OptionalEnum<TEnum>(VisualArtifactInterchangeJsonWriter writer, string name, TEnum? value) where TEnum : struct {
        if (value.HasValue) String(writer, name, value.Value.ToString() ?? string.Empty);
    }
}
