using System;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Maps topology presentation semantics into the reusable visual artifact interchange contract.</summary>
public static partial class VisualArtifactInterchangeMapping {
    private static VisualArtifactInterchangeTopologyGroup MapGroupPresentation(TopologyGroup group) {
        var mapped=new VisualArtifactInterchangeTopologyGroup {
            StrokeWidth=group.StrokeWidth,Opacity=group.Opacity,SurfaceOpacity=group.SurfaceOpacity,
            LabelContent = group.LabelContent.IsStructured ? group.LabelContent : null,
            Status = group.Status,
            TextStyle = group.TextStyle?.Clone(),
            ShowLabel = group.ShowLabel,
            Presentation = group.Presentation,
            LayoutDirection = group.LayoutDirection,
            IconId = group.IconId,
            Symbol = group.Symbol,
            LayoutPolicy = group.LayoutPolicy,
            AppliedLayoutPolicy = group.AppliedLayoutPolicy,
            Longitude = group.Longitude,
            Latitude = group.Latitude
        };
        mapped.DashPattern.AddRange(group.DashPattern);return mapped;
    }

    private static VisualArtifactInterchangeTopologyNode MapNodePresentation(TopologyNode node, TopologyNodeDisplayMode displayMode, bool showStatusBadge) {
        var mapped=new VisualArtifactInterchangeTopologyNode {
            CaptionPlacement=node.CaptionPlacement,CaptionSurface=node.CaptionSurface,CaptionGap=node.CaptionGap,
            GlyphScale=node.GlyphScale,CornerRadius=node.CornerRadius,CaptionTextStyle=node.CaptionTextStyle?.Clone(),
            BorderColor=node.BorderColor,StrokeWidth=node.StrokeWidth,Opacity=node.Opacity,SurfaceOpacity=node.SurfaceOpacity,
            PreparedNodeBasis = TopologyPreparedNodeBasis.Resolve(node),
            Kind = node.Kind,
            TableContent = MapTableContent(node.TableContent),
            TextStyle = node.TextStyle?.Clone(),
            SubtitleTextStyle = node.SubtitleTextStyle?.Clone(),
            LabelContent = node.LabelContent.IsStructured ? node.LabelContent : null,
            MaximumTextWidth = node.MaximumTextWidth,
            ContentPadding = node.ContentPadding,
            PaletteIndex = node.PaletteIndex,
            Status = node.Status,
            DisplayMode = displayMode,
            Shape = node.Shape, IconShape = node.IconShape,
            FitLabelToContent = node.FitLabelToContent,
            Artwork = MapArtwork(node.Artwork),
            Longitude = node.Longitude,
            Latitude = node.Latitude,
            ShowStatusBadge = showStatusBadge,
            ShowLabel = node.ShowLabel,
            MaximumLabelCharacters = node.MaximumLabelCharacters
        };
        mapped.DashPattern.AddRange(node.DashPattern);return mapped;
    }

    private static VisualArtifactInterchangeTopologyEdge MapEdgePresentation(TopologyEdge edge, InterchangeIdScope ids) {
        var mapped = new VisualArtifactInterchangeTopologyEdge {
            LabelBackgroundColor = edge.LabelBackgroundColor,
            LabelContent = edge.LabelContent?.IsStructured == true ? edge.LabelContent : null,
            Kind = edge.Kind,
            PathGeometry = edge.PathGeometry, Interpolation = edge.Interpolation, InterpolationCornerBlendLength = edge.InterpolationCornerBlendLength, PreparedEdgePlan = edge.PreparedEdgePlan,
            LabelPosition = edge.LabelPosition,
            SourceMarkerStyle = edge.SourceMarkerStyle?.Clone(), TargetMarkerStyle = edge.TargetMarkerStyle?.Clone(),
            TextStyle = edge.TextStyle?.Clone(),
            PaletteIndex = edge.PaletteIndex,
            Status = edge.Status,
            Direction = edge.Direction,
            SourcePort = edge.SourcePort,
            TargetPort = edge.TargetPort,
            LineStyle = edge.LineStyle,
            Routing = edge.Routing,
            Emphasis = edge.Emphasis,
            SourceMarker = edge.SourceMarker,
            TargetMarker = edge.TargetMarker,
            StrokeWidth = edge.StrokeWidth,
            Opacity = edge.Opacity,
            IsMuted = edge.IsMuted,
            RoutingPriority = edge.RoutingPriority,
            RouteLane = edge.HasRouteLaneOverride ? edge.RouteLane : null,
            LabelOffsetX = edge.LabelOffsetX,
            LabelOffsetY = edge.LabelOffsetY,
            LabelAnchor = edge.HasLabelAnchorOverride
                ? new VisualArtifactInterchangePoint { X = edge.LabelAnchorX, Y = edge.LabelAnchorY }
                : null,
            LayoutInference = edge.LayoutInference,
            PreferredLength = edge.PreferredLength,
            MinimumRankSpan = edge.MinimumRankSpan
        };
        if (!string.IsNullOrWhiteSpace(edge.LabelAnchorNodeId) && ids.TryNode(edge.LabelAnchorNodeId!, out string labelAnchorNodeId)) {
            mapped.LabelAnchorNodeId = labelAnchorNodeId;
        }
        foreach (double value in edge.DashPattern) mapped.DashPattern.Add(value);
        foreach (var point in edge.Waypoints) mapped.Waypoints.Add(new VisualArtifactInterchangePoint { X = point.X, Y = point.Y });
        return mapped;
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

    private static VisualArtifactInterchangeLegend? MapLegend(TopologyChart chart) {
        var legend = chart.Legend;
        if (legend == null) return null;
        var mapped = new VisualArtifactInterchangeLegend { Title = legend.Title };
        foreach (TopologyLegendItem item in legend.Items) {
            mapped.Items.Add(new VisualArtifactInterchangeLegendItem {
                Label = item.Label,
                Kind = item.Kind,
                Status = item.Status,
                NodeKind = item.NodeKind,
                EdgeKind = item.EdgeKind,
                Symbol = item.Symbol,
                IconId = item.IconId,
                Color = item.Color,
                BackgroundColor = item.BackgroundColor,
                // Edge items carry the style the renderers draw, so a consumer does not have to resolve Auto itself.
                LineStyle = item.Kind == TopologyLegendItemKind.Edge ? TopologyRenderPrimitives.LegendLineStyle(chart, item) : item.LineStyle
            });
        }
        return mapped;
    }

    private static VisualArtifactInterchangeArtwork? MapArtwork(TopologyIconArtwork? artwork) {
        if (artwork == null) return null;
        if (!artwork.IsSafe) {
            return new VisualArtifactInterchangeArtwork { Status = VisualArtifactInterchangeArtworkStatus.UnsafeOmitted };
        }
        return new VisualArtifactInterchangeArtwork {
            Status = VisualArtifactInterchangeArtworkStatus.Available,
            SvgViewBox = artwork.SvgViewBox,
            PreserveAspectRatio = artwork.PreserveAspectRatio,
            SvgBody = artwork.SvgBody,
            SvgPath = artwork.SvgPath,
            PreviewPath = artwork.PreviewPath,
            ImageHref = artwork.ImageHref
        };
    }

    private static VisualArtifactInterchangeTheme MapTheme(TopologyPaint theme, double titleSize) {
        return new VisualArtifactInterchangeTheme {
            Background = theme.Background,
            Foreground = theme.Foreground,
            MutedForeground = theme.MutedForeground,
            Card = theme.Card,
            Surface = theme.Surface,
            Border = theme.Border,
            Accent = theme.Accent,
            Healthy = theme.Healthy,
            Warning = theme.Warning,
            Critical = theme.Critical,
            Unknown = theme.Unknown,
            Disabled = theme.Disabled,
            TitleSize = titleSize == ChartForgeX.Themes.VisualTheme.Graphite().Typography.TitleSize ? (double?)null : titleSize,
            DataLabelSize = theme.Typography.DataLabelSize == ChartForgeX.Themes.VisualTheme.Graphite().Typography.DataLabelSize ? (double?)null : theme.Typography.DataLabelSize,
            SubtitleSize = theme.Typography.SubtitleSize == ChartForgeX.Themes.VisualTheme.Graphite().Typography.SubtitleSize ? (double?)null : theme.Typography.SubtitleSize,
            LegendSize = theme.Typography.LegendSize == ChartForgeX.Themes.VisualTheme.Graphite().Typography.LegendSize ? (double?)null : theme.Typography.LegendSize,
            FontFamily = theme.FontFamily
        };
    }
}
