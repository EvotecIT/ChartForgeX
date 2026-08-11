using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Maps topology presentation semantics into the reusable visual artifact interchange contract.</summary>
public static partial class VisualArtifactInterchangeMapping {
    private static void MapGroupPresentation(TopologyGroup group, IDictionary<string, string> metadata) {
        RemoveMetadataKeys(metadata, "iconId", "symbol", "topology.layoutPolicy", "topology.appliedLayoutPolicy", "topology.longitude", "topology.latitude");
        if (!string.IsNullOrWhiteSpace(group.IconId)) metadata["iconId"] = group.IconId!;
        if (!string.IsNullOrWhiteSpace(group.Symbol)) metadata["symbol"] = group.Symbol!;
        metadata["topology.layoutPolicy"] = group.LayoutPolicy.ToString();
        metadata["topology.appliedLayoutPolicy"] = group.AppliedLayoutPolicy.ToString();
        if (group.Longitude.HasValue) metadata["topology.longitude"] = InvariantNumber(group.Longitude.Value);
        if (group.Latitude.HasValue) metadata["topology.latitude"] = InvariantNumber(group.Latitude.Value);
    }

    private static void MapNodePresentation(TopologyNode node, TopologyNodeDisplayMode displayMode, IDictionary<string, string> metadata) {
        RemoveMetadataKeys(metadata, "topology.displayMode", "topology.longitude", "topology.latitude", "topology.showStatusBadge", "topology.maximumLabelCharacters");
        metadata["topology.displayMode"] = displayMode.ToString();
        MapArtwork(node.Artwork, metadata);
        if (node.Longitude.HasValue) metadata["topology.longitude"] = InvariantNumber(node.Longitude.Value);
        if (node.Latitude.HasValue) metadata["topology.latitude"] = InvariantNumber(node.Latitude.Value);
        if (!node.ShowStatusBadge) metadata["topology.showStatusBadge"] = bool.FalseString;
        if (node.MaximumLabelCharacters.HasValue) metadata["topology.maximumLabelCharacters"] = node.MaximumLabelCharacters.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static void MapEdgePresentation(TopologyEdge edge, InterchangeIdScope ids, IDictionary<string, string> metadata) {
        RemoveMetadataKeys(metadata,
            "topology.routing", "topology.emphasis", "topology.sourceMarker", "topology.targetMarker", "topology.strokeWidth", "topology.opacity",
            "topology.dashPattern", "topology.waypoints", "topology.muted", "topology.routingPriority", "topology.routeLane", "topology.labelOffsetX",
            "topology.labelOffsetY", "topology.labelAnchor", "topology.labelAnchorNodeId", "topology.layoutInference", "topology.preferredLength",
            "topology.minimumRankSpan");
        metadata["topology.routing"] = edge.Routing.ToString();
        metadata["topology.emphasis"] = edge.Emphasis.ToString();
        if (edge.SourceMarker.HasValue) metadata["topology.sourceMarker"] = edge.SourceMarker.Value.ToString();
        if (edge.TargetMarker.HasValue) metadata["topology.targetMarker"] = edge.TargetMarker.Value.ToString();
        if (edge.StrokeWidth.HasValue) metadata["topology.strokeWidth"] = InvariantNumber(edge.StrokeWidth.Value);
        if (edge.Opacity.HasValue) metadata["topology.opacity"] = InvariantNumber(edge.Opacity.Value);
        if (edge.DashPattern.Count > 0) metadata["topology.dashPattern"] = InvariantNumbers(edge.DashPattern);
        if (edge.Waypoints.Count > 0) metadata["topology.waypoints"] = InvariantWaypoints(edge);
        if (edge.IsMuted) metadata["topology.muted"] = bool.TrueString;
        if (edge.RoutingPriority != 0) metadata["topology.routingPriority"] = edge.RoutingPriority.ToString(CultureInfo.InvariantCulture);
        if (edge.HasRouteLaneOverride) metadata["topology.routeLane"] = InvariantNumber(edge.RouteLane);
        if (edge.LabelOffsetX != 0) metadata["topology.labelOffsetX"] = InvariantNumber(edge.LabelOffsetX);
        if (edge.LabelOffsetY != 0) metadata["topology.labelOffsetY"] = InvariantNumber(edge.LabelOffsetY);
        if (edge.HasLabelAnchorOverride) metadata["topology.labelAnchor"] = InvariantNumber(edge.LabelAnchorX) + "," + InvariantNumber(edge.LabelAnchorY);
        if (!string.IsNullOrWhiteSpace(edge.LabelAnchorNodeId) && ids.TryNode(edge.LabelAnchorNodeId!, out string labelAnchorNodeId)) metadata["topology.labelAnchorNodeId"] = labelAnchorNodeId;
        if (edge.LayoutInference != TopologyEdgeLayoutInference.None) metadata["topology.layoutInference"] = edge.LayoutInference.ToString();
        if (edge.PreferredLength.HasValue) metadata["topology.preferredLength"] = InvariantNumber(edge.PreferredLength.Value);
        metadata["topology.minimumRankSpan"] = edge.MinimumRankSpan.ToString(CultureInfo.InvariantCulture);
    }

    private static void RefreshTopologyAccessibility(
        VisualArtifactInterchangeEnvelope envelope,
        VisualArtifact artifact,
        TopologyChart prepared) {
        if (!artifact.HasModelAccessibilitySnapshot) return;
        envelope.AccessibleName = string.Equals(artifact.Accessibility.Name, artifact.ModelAccessibilitySnapshot.Name, StringComparison.Ordinal)
            ? prepared.Accessibility.Name
            : artifact.Accessibility.Name;
        envelope.AccessibleDescription = string.Equals(artifact.Accessibility.Description, artifact.ModelAccessibilitySnapshot.Description, StringComparison.Ordinal)
            ? prepared.Accessibility.Description
            : artifact.Accessibility.Description;
        envelope.Language = string.Equals(artifact.Accessibility.Language, artifact.ModelAccessibilitySnapshot.Language, StringComparison.Ordinal)
            ? prepared.Accessibility.Language
            : artifact.Accessibility.Language;
        envelope.IsDecorative = artifact.Accessibility.IsDecorative == artifact.ModelAccessibilitySnapshot.IsDecorative
            ? prepared.Accessibility.IsDecorative
            : artifact.Accessibility.IsDecorative;
    }

    private static void MapLegend(VisualArtifactInterchangeEnvelope envelope, TopologyLegend? legend, InterchangeIdScope ids) {
        if (legend == null) return;
        envelope.Annotations.Add(new VisualArtifactInterchangeAnnotation {
            Id = ids.AddAnnotation("topology-legend"),
            Kind = "TopologyLegend",
            Text = legend.Title ?? string.Empty
        });
        for (var index = 0; index < legend.Items.Count; index++) {
            TopologyLegendItem item = legend.Items[index];
            var annotation = new VisualArtifactInterchangeAnnotation {
                Id = ids.AddAnnotation("topology-legend-item-" + (index + 1).ToString(CultureInfo.InvariantCulture)),
                Kind = "TopologyLegendItem:" + item.Kind,
                Text = item.Label
            };
            if (item.Status.HasValue) annotation.Metadata["topology.legend.status"] = item.Status.Value.ToString();
            if (item.NodeKind.HasValue) annotation.Metadata["topology.legend.nodeKind"] = item.NodeKind.Value.ToString();
            if (item.EdgeKind.HasValue) annotation.Metadata["topology.legend.edgeKind"] = item.EdgeKind.Value.ToString();
            if (!string.IsNullOrWhiteSpace(item.Symbol)) annotation.Metadata["topology.legend.symbol"] = item.Symbol!;
            if (!string.IsNullOrWhiteSpace(item.IconId)) annotation.Metadata["topology.legend.iconId"] = item.IconId!;
            if (!string.IsNullOrWhiteSpace(item.Color)) annotation.Metadata["topology.legend.color"] = item.Color!;
            if (!string.IsNullOrWhiteSpace(item.BackgroundColor)) annotation.Metadata["topology.legend.backgroundColor"] = item.BackgroundColor!;
            annotation.Metadata["topology.legend.lineStyle"] = item.LineStyle.ToString();
            envelope.Annotations.Add(annotation);
        }
    }

    private static void MapArtwork(TopologyIconArtwork? artwork, IDictionary<string, string> metadata) {
        var reservedKeys = new List<string>();
        foreach (string key in metadata.Keys) {
            if (key.StartsWith("topology.artwork.", StringComparison.Ordinal)) reservedKeys.Add(key);
        }
        foreach (string key in reservedKeys) metadata.Remove(key);
        if (artwork == null) return;
        if (!artwork.IsSafe) {
            metadata["topology.artwork.unsupported"] = "unsafe";
            return;
        }
        metadata["topology.artwork.svgViewBox"] = artwork.SvgViewBox;
        metadata["topology.artwork.preserveAspectRatio"] = artwork.PreserveAspectRatio;
        if (!string.IsNullOrWhiteSpace(artwork.SvgBody)) metadata["topology.artwork.svgBody"] = artwork.SvgBody!;
        if (!string.IsNullOrWhiteSpace(artwork.SvgPath)) metadata["topology.artwork.svgPath"] = artwork.SvgPath!;
        if (!string.IsNullOrWhiteSpace(artwork.PreviewPath)) metadata["topology.artwork.previewPath"] = artwork.PreviewPath!;
        if (!string.IsNullOrWhiteSpace(artwork.ImageHref)) metadata["topology.artwork.imageHref"] = artwork.ImageHref!;
    }

    private static void RemoveMetadataKeys(IDictionary<string, string> metadata, params string[] keys) {
        foreach (string key in keys) metadata.Remove(key);
    }

    private static void MapTheme(IDictionary<string, string> metadata, TopologyTheme theme) {
        metadata["topology.theme.background"] = theme.Background;
        metadata["topology.theme.foreground"] = theme.Foreground;
        metadata["topology.theme.mutedForeground"] = theme.MutedForeground;
        metadata["topology.theme.card"] = theme.Card;
        metadata["topology.theme.surface"] = theme.Surface;
        metadata["topology.theme.border"] = theme.Border;
        metadata["topology.theme.accent"] = theme.Accent;
        metadata["topology.theme.healthy"] = theme.Healthy;
        metadata["topology.theme.warning"] = theme.Warning;
        metadata["topology.theme.critical"] = theme.Critical;
        metadata["topology.theme.unknown"] = theme.Unknown;
        metadata["topology.theme.disabled"] = theme.Disabled;
        metadata["topology.theme.fontFamily"] = theme.FontFamily;
    }
}
