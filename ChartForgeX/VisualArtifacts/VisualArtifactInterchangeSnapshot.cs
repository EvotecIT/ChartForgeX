using System;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Detaches typed semantic values without applying portable serialization budgets to static preparation.</summary>
internal static class VisualArtifactInterchangeSnapshot {
    internal static VisualArtifactInterchangeEnvelope Capture(VisualArtifactInterchangeEnvelope source) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Copy(source)!;
    }

    private static VisualArtifactInterchangeEnvelope? Copy(VisualArtifactInterchangeEnvelope? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeEnvelope {
            Kind = source.Kind,
            Family = source.Family,
            SourceLanguage = source.SourceLanguage,
            Id = source.Id,
            Title = source.Title,
            Subtitle = source.Subtitle,
            Topology = Copy(source.Topology),
            Flow = Copy(source.Flow),
            Sequence = Copy(source.Sequence),
            Width = source.Width,
            Height = source.Height,
            AccessibleName = source.AccessibleName,
            AccessibleDescription = source.AccessibleDescription,
            Language = source.Language,
            IsDecorative = source.IsDecorative,
            Presentation = Copy(source.Presentation),
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        foreach (var item in source.Groups) copy.Groups.Add(Copy(item)!);
        foreach (var item in source.Nodes) copy.Nodes.Add(Copy(item)!);
        foreach (var item in source.Edges) copy.Edges.Add(Copy(item)!);
        foreach (var item in source.Scenarios) copy.Scenarios.Add(Copy(item)!);
        foreach (var item in source.Annotations) copy.Annotations.Add(Copy(item)!);
        return copy;
    }

    private static VisualArtifactInterchangeGroup? Copy(VisualArtifactInterchangeGroup? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeGroup {
            Id = source.Id,
            Role = source.Role,
            Kind = source.Kind,
            Label = source.Label,
            Subtitle = source.Subtitle,
            Status = source.Status,
            Color = source.Color,
            Href = source.Href,
            Tooltip = source.Tooltip,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            Topology = Copy(source.Topology),
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        return copy;
    }

    private static VisualArtifactInterchangeNode? Copy(VisualArtifactInterchangeNode? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeNode {
            Id = source.Id,
            Role = source.Role,
            Kind = source.Kind,
            Label = source.Label,
            Subtitle = source.Subtitle,
            GroupId = source.GroupId,
            Status = source.Status,
            IconId = source.IconId,
            Symbol = source.Symbol,
            Badge = source.Badge,
            Color = source.Color,
            BackgroundColor = source.BackgroundColor,
            Href = source.Href,
            Tooltip = source.Tooltip,
            X = source.X,
            Y = source.Y,
            Width = source.Width,
            Height = source.Height,
            Topology = Copy(source.Topology),
            Flow = Copy(source.Flow),
            Sequence = Copy(source.Sequence),
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        foreach (var item in source.Ports) copy.Ports.Add(Copy(item)!);
        foreach (var item in source.Details) copy.Details.Add(Copy(item)!);
        foreach (var item in source.Metrics) copy.Metrics.Add(Copy(item)!);
        return copy;
    }

    private static VisualArtifactInterchangePort? Copy(VisualArtifactInterchangePort? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangePort {
            Id = source.Id,
            Side = source.Side,
            Offset = source.Offset,
            Label = source.Label,
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        return copy;
    }

    private static VisualArtifactInterchangeDetail? Copy(VisualArtifactInterchangeDetail? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeDetail {
            Text = source.Text,
            Label = source.Label,
            Value = source.Value,
            IconId = source.IconId,
            Status = source.Status,
            Color = source.Color,
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        return copy;
    }

    private static VisualArtifactInterchangeEdge? Copy(VisualArtifactInterchangeEdge? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeEdge {
            ResolvedLabelBounds = source.ResolvedLabelBounds,
            Id = source.Id,
            Role = source.Role,
            Kind = source.Kind,
            SourceId = source.SourceId,
            TargetId = source.TargetId,
            Label = source.Label,
            SecondaryLabel = source.SecondaryLabel,
            TertiaryLabel = source.TertiaryLabel,
            SourceLabel = source.SourceLabel,
            TargetLabel = source.TargetLabel,
            Status = source.Status,
            SourcePortId = source.SourcePortId,
            TargetPortId = source.TargetPortId,
            Color = source.Color,
            Href = source.Href,
            Tooltip = source.Tooltip,
            Order = source.Order,
            Topology = Copy(source.Topology),
            Flow = Copy(source.Flow),
            Sequence = Copy(source.Sequence),
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        foreach (var item in source.ResolvedRoute) copy.ResolvedRoute.Add(Copy(item)!);
        foreach (var item in source.Metrics) copy.Metrics.Add(Copy(item)!);
        return copy;
    }

    private static VisualArtifactInterchangeMetric? Copy(VisualArtifactInterchangeMetric? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeMetric {
            Name = source.Name,
            Value = source.Value,
        };
        return copy;
    }

    private static VisualArtifactInterchangeScenario? Copy(VisualArtifactInterchangeScenario? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeScenario {
            Id = source.Id,
            Label = source.Label,
            Description = source.Description,
            Color = source.Color,
            PlaybackDelayMilliseconds = source.PlaybackDelayMilliseconds,
            LoopPlayback = source.LoopPlayback,
            AutoPlay = source.AutoPlay,
            Spotlight = source.Spotlight,
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        foreach (var item in source.Steps) copy.Steps.Add(Copy(item)!);
        return copy;
    }

    private static VisualArtifactInterchangeScenarioStep? Copy(VisualArtifactInterchangeScenarioStep? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeScenarioStep {
            TargetId = source.TargetId,
            Kind = source.Kind,
            Label = source.Label,
            Description = source.Description,
            DurationMilliseconds = source.DurationMilliseconds,
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        return copy;
    }

    private static VisualArtifactInterchangeAnnotation? Copy(VisualArtifactInterchangeAnnotation? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeAnnotation {
            Id = source.Id,
            Role = source.Role,
            Kind = source.Kind,
            Text = source.Text,
            Placement = source.Placement,
            StartIndex = source.StartIndex,
            EndIndex = source.EndIndex,
            Sequence = Copy(source.Sequence),
        };
        foreach (var item in source.Extensions) copy.Extensions.Add(item.Key, item.Value);
        foreach (var item in source.TargetIds) copy.TargetIds.Add(item);
        return copy;
    }

    private static VisualArtifactInterchangeTopologyArtifact? Copy(VisualArtifactInterchangeTopologyArtifact? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeTopologyArtifact {
            LayoutMode = source.LayoutMode,
            LayoutDirection = source.LayoutDirection,
        };
        return copy;
    }

    private static VisualArtifactInterchangeFlowArtifact? Copy(VisualArtifactInterchangeFlowArtifact? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeFlowArtifact {
            LayoutMode = source.LayoutMode,
            LayoutDirection = source.LayoutDirection,
        };
        return copy;
    }

    private static VisualArtifactInterchangeSequenceArtifact? Copy(VisualArtifactInterchangeSequenceArtifact? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeSequenceArtifact();
        return copy;
    }

    private static VisualArtifactInterchangePresentation? Copy(VisualArtifactInterchangePresentation? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangePresentation {
            Theme = Copy(source.Theme),
            MapViewport = Copy(source.MapViewport),
            Legend = Copy(source.Legend),
        };
        return copy;
    }

    private static VisualArtifactInterchangeTheme? Copy(VisualArtifactInterchangeTheme? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeTheme {
            Background = source.Background,
            Foreground = source.Foreground,
            MutedForeground = source.MutedForeground,
            Card = source.Card,
            Surface = source.Surface,
            Border = source.Border,
            Accent = source.Accent,
            Healthy = source.Healthy,
            Warning = source.Warning,
            Critical = source.Critical,
            Unknown = source.Unknown,
            Disabled = source.Disabled,
            FontFamily = source.FontFamily,
        };
        return copy;
    }

    private static VisualArtifactInterchangeMapViewport? Copy(VisualArtifactInterchangeMapViewport? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeMapViewport {
            Name = source.Name,
            Projection = source.Projection,
            MinimumLongitude = source.MinimumLongitude,
            MaximumLongitude = source.MaximumLongitude,
            MinimumLatitude = source.MinimumLatitude,
            MaximumLatitude = source.MaximumLatitude,
        };
        return copy;
    }

    private static VisualArtifactInterchangeLegend? Copy(VisualArtifactInterchangeLegend? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeLegend {
            Title = source.Title,
        };
        foreach (var item in source.Items) copy.Items.Add(Copy(item)!);
        return copy;
    }

    private static VisualArtifactInterchangeLegendItem? Copy(VisualArtifactInterchangeLegendItem? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeLegendItem {
            Label = source.Label,
            Kind = source.Kind,
            Status = source.Status,
            NodeKind = source.NodeKind,
            EdgeKind = source.EdgeKind,
            Symbol = source.Symbol,
            IconId = source.IconId,
            Color = source.Color,
            BackgroundColor = source.BackgroundColor,
            LineStyle = source.LineStyle,
        };
        return copy;
    }

    private static VisualArtifactInterchangeTopologyGroup? Copy(VisualArtifactInterchangeTopologyGroup? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeTopologyGroup {
            Status = source.Status,
            LayoutPolicy = source.LayoutPolicy,
            AppliedLayoutPolicy = source.AppliedLayoutPolicy,
            Longitude = source.Longitude,
            Latitude = source.Latitude,
            IconId = source.IconId,
            Symbol = source.Symbol,
        };
        return copy;
    }

    private static VisualArtifactInterchangeTopologyNode? Copy(VisualArtifactInterchangeTopologyNode? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeTopologyNode {
            Kind = source.Kind,
            Status = source.Status,
            DisplayMode = source.DisplayMode,
            Shape = source.Shape,
            Longitude = source.Longitude,
            Latitude = source.Latitude,
            ShowStatusBadge = source.ShowStatusBadge,
            MaximumLabelCharacters = source.MaximumLabelCharacters,
            Artwork = Copy(source.Artwork),
        };
        return copy;
    }

    private static VisualArtifactInterchangeArtwork? Copy(VisualArtifactInterchangeArtwork? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeArtwork {
            Status = source.Status,
            SvgViewBox = source.SvgViewBox,
            PreserveAspectRatio = source.PreserveAspectRatio,
            SvgBody = source.SvgBody,
            SvgPath = source.SvgPath,
            PreviewPath = source.PreviewPath,
            ImageHref = source.ImageHref,
        };
        return copy;
    }

    private static VisualArtifactInterchangePoint? Copy(VisualArtifactInterchangePoint? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangePoint {
            X = source.X,
            Y = source.Y,
        };
        return copy;
    }

    private static VisualArtifactInterchangeTopologyEdge? Copy(VisualArtifactInterchangeTopologyEdge? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeTopologyEdge {
            Kind = source.Kind,
            Status = source.Status,
            Direction = source.Direction,
            SourcePort = source.SourcePort,
            TargetPort = source.TargetPort,
            LineStyle = source.LineStyle,
            Routing = source.Routing,
            Emphasis = source.Emphasis,
            SourceMarker = source.SourceMarker,
            TargetMarker = source.TargetMarker,
            StrokeWidth = source.StrokeWidth,
            Opacity = source.Opacity,
            IsMuted = source.IsMuted,
            RoutingPriority = source.RoutingPriority,
            RouteLane = source.RouteLane,
            LabelOffsetX = source.LabelOffsetX,
            LabelOffsetY = source.LabelOffsetY,
            LabelAnchor = Copy(source.LabelAnchor),
            LabelAnchorNodeId = source.LabelAnchorNodeId,
            LayoutInference = source.LayoutInference,
            PreferredLength = source.PreferredLength,
            MinimumRankSpan = source.MinimumRankSpan,
        };
        foreach (var item in source.DashPattern) copy.DashPattern.Add(item);
        foreach (var item in source.Waypoints) copy.Waypoints.Add(Copy(item)!);
        return copy;
    }

    private static VisualArtifactInterchangeFlowNode? Copy(VisualArtifactInterchangeFlowNode? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeFlowNode {
            Kind = source.Kind,
        };
        return copy;
    }

    private static VisualArtifactInterchangeFlowEdge? Copy(VisualArtifactInterchangeFlowEdge? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeFlowEdge {
            Kind = source.Kind,
            Direction = source.Direction,
        };
        return copy;
    }

    private static VisualArtifactInterchangeSequenceNode? Copy(VisualArtifactInterchangeSequenceNode? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeSequenceNode {
            Kind = source.Kind,
            Order = source.Order,
            IsImplicit = source.IsImplicit,
        };
        return copy;
    }

    private static VisualArtifactInterchangeSequenceEdge? Copy(VisualArtifactInterchangeSequenceEdge? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeSequenceEdge {
            Kind = source.Kind,
            LineStyle = source.LineStyle,
            ActivatesTarget = source.ActivatesTarget,
            Deactivates = source.Deactivates,
        };
        return copy;
    }

    private static VisualArtifactInterchangeSequenceAnnotation? Copy(VisualArtifactInterchangeSequenceAnnotation? source) {
        if (source == null) return null;
        var copy = new VisualArtifactInterchangeSequenceAnnotation {
            ActivationState = source.ActivationState,
            NotePlacement = source.NotePlacement,
            BlockKind = source.BlockKind,
            ParentBlockKind = source.ParentBlockKind,
            BranchKind = source.BranchKind,
            Depth = source.Depth,
            IsEmpty = source.IsEmpty,
        };
        return copy;
    }
}

