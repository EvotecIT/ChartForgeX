using ChartForgeX.Rendering;
namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    internal static TopologyChart Clone(TopologyChart chart) {
        var copy = new TopologyChart {
            Id = chart.Id,
            Title = chart.Title,
            Subtitle = chart.Subtitle,
            LayoutMode = chart.LayoutMode,
            CrossingPaint = chart.CrossingPaint,
            LayoutDirection = chart.LayoutDirection,
            GridLayout = chart.GridLayout,
            HierarchyPreparation = chart.HierarchyPreparation,
            UsesPreparedSceneCoordinates = chart.UsesPreparedSceneCoordinates,
            PreparedFramePadding = TopologyPreparedFramePadding.ForRequest(chart),
            PreparedFrame = chart.PreparedFrame?.RequestValues(retainOwnedFonts: true),
            PreparedFont = PreparedDiagramFont.ForRequest(chart),
            Viewport = new TopologyViewport { Width = chart.Viewport.Width, Height = chart.Viewport.Height, Padding = chart.Viewport.Padding },
            MapViewport = chart.MapViewport,
            Legend = chart.Legend == null ? null : TopologyLegend.Clone(chart.Legend),
            Theme = chart.Theme, ThemeMode = chart.ThemeMode,
            DefaultRenderOptions = chart.DefaultRenderOptions?.Clone(),
            // Prepared geometry depends on the resolved routing policy and measured font.
            // Prepare replaces these values for a fresh layout; detached prepared clones retain them.
            RenderOptions = chart.RenderOptions?.Clone(),
            TextMeasurement = chart.TextMeasurement,
            PreparedGroupHeaderParts = chart.PreparedGroupHeaderParts,
            PreparedGroupHeaderLayoutParts = chart.PreparedGroupHeaderLayoutParts
        };
        copy.Accessibility.Name = chart.Accessibility.Name;
        copy.Accessibility.Description = chart.Accessibility.Description;
        copy.Accessibility.Language = chart.Accessibility.Language;
        copy.Accessibility.IsDecorative = chart.Accessibility.IsDecorative;
        copy.Labels = chart.Labels.Clone();
        foreach (var alignment in chart.Alignments) copy.Alignments.Add(alignment.Clone());
        foreach (var group in chart.Groups) copy.Groups.Add(Clone(group));
        foreach (var node in chart.Nodes) copy.Nodes.Add(Clone(node));
        foreach (var edge in chart.Edges) copy.Edges.Add(Clone(edge));
        foreach (var note in chart.Annotations) copy.Annotations.Add(new TopologyAnnotation(note.Id, note.Text, note.TargetId, note.Side) { PreparedDefaultUnits = note.PreparedDefaultUnits, PreparedRoutingViewport = note.PreparedRoutingViewport });
        copy.PathDecorations.AddRange(chart.PathDecorations); // Immutable path and paint captures may be shared safely.
        foreach (var scenario in chart.Scenarios) copy.Scenarios.Add(TopologyScenarioCloner.Clone(scenario));
        return copy;
    }

    private static TopologyGroup Clone(TopologyGroup group) {
        var copy = new TopologyGroup {
            Id = group.Id,
            LabelContent = group.LabelContent,
            TextStyle = group.TextStyle?.Clone(),
            ShowLabel = group.ShowLabel,
            Subtitle = group.Subtitle,
            ParentGroupId = group.ParentGroupId,
            LayoutDirection = group.LayoutDirection,
            Presentation = group.Presentation,
            Status = group.Status,
            X = group.X,
            Y = group.Y,
            Longitude = group.Longitude,
            Latitude = group.Latitude,
            Width = group.Width,
            Height = group.Height,
            Href = group.Href,
            Tooltip = group.Tooltip,
            CssClass = group.CssClass,
            Symbol = group.Symbol,
            IconId = group.IconId,
            Color = group.Color,
            BackgroundColor = group.BackgroundColor,
            BorderColor = group.BorderColor,
            LabelColor = group.LabelColor,
            StrokeWidth=group.StrokeWidth,Opacity=group.Opacity,SurfaceOpacity=group.SurfaceOpacity,
            LayoutPolicy = group.LayoutPolicy,
            AppliedLayoutPolicy = group.AppliedLayoutPolicy,
            HasPositionOverride = group.HasPositionOverride
        };
        copy.DashPattern.AddRange(group.DashPattern);
        foreach (var item in group.Metadata) copy.Metadata[item.Key] = item.Value;
        return copy;
    }

    internal static TopologyNode Clone(TopologyNode node) {
        var copy = new TopologyNode {
            Id = node.Id,
            LabelContent = node.LabelContent,
            TableContent = ChartForgeX.VisualArtifacts.VisualArtifactInterchangeMapping.SnapshotTableContent(node.TableContent),
            TextStyle = node.TextStyle?.Clone(),
            SubtitleTextStyle = node.SubtitleTextStyle?.Clone(),
            MaximumTextWidth = node.MaximumTextWidth,
            ContentPadding = node.ContentPadding,
            Subtitle = node.Subtitle,
            Kind = node.Kind,
            Symbol = node.Symbol,
            IconId = node.IconId, IconShape = node.IconShape,
            Artwork = node.Artwork?.Clone(),
            DisplayMode = node.DisplayMode,
            Shape = node.Shape,
            Badge = node.Badge,
            Status = node.Status,
            GroupId = node.GroupId,
            X = node.X,
            Y = node.Y,
            Longitude = node.Longitude,
            Latitude = node.Latitude,
            Width = node.Width,
            Height = node.Height,
            Href = node.Href,
            Tooltip = node.Tooltip,
            CssClass = node.CssClass,
            Color = node.Color,
            LabelColor = node.LabelColor,
            BorderColor=node.BorderColor,StrokeWidth=node.StrokeWidth,Opacity=node.Opacity,SurfaceOpacity=node.SurfaceOpacity,
            CaptionPlacement=node.CaptionPlacement, CaptionSurface=node.CaptionSurface, CaptionGap=node.CaptionGap,
            CaptionTextStyle=node.CaptionTextStyle?.Clone(),
            GlyphScale=node.GlyphScale, CornerRadius=node.CornerRadius,
            PaletteIndex = node.PaletteIndex,
            BackgroundColor = node.BackgroundColor,
            PreparedNodeBasis = node.PreparedNodeBasis,
            ShowStatusBadge = node.ShowStatusBadge,
            ShowLabel = node.ShowLabel,
            PreserveDisplayModeSize = node.PreserveDisplayModeSize,
            FitLabelToContent = node.FitLabelToContent,
            MaximumLabelCharacters = node.MaximumLabelCharacters,
            HasPositionOverride = node.HasPositionOverride
        };
        copy.DashPattern.AddRange(node.DashPattern);
        foreach (var item in node.Metrics) copy.Metrics[item.Key] = item.Value;
        foreach (var item in node.Metadata) copy.Metadata[item.Key] = item.Value;
        foreach (var port in node.Ports) {
            var portCopy = new TopologyNodePort { Id = port.Id, Side = port.Side, Offset = port.Offset, Label = port.Label };
            foreach (var item in port.Metadata) portCopy.Metadata[item.Key] = item.Value;
            copy.Ports.Add(portCopy);
        }
        foreach (var detail in node.Details) {
            var detailCopy = new TopologyNodeDetail { Content = detail.Content, Label = detail.Label, ValueContent = detail.ValueContent, IconId = detail.IconId, Status = detail.Status, Color = detail.Color, TextStyle = detail.TextStyle?.Clone(), BadgeStyle = detail.BadgeStyle?.Clone() };
            foreach (var item in detail.Metadata) detailCopy.Metadata[item.Key] = item.Value;
            copy.Details.Add(detailCopy);
        }
        return copy;
    }

    internal static TopologyEdge Clone(TopologyEdge edge) {
        var copy = new TopologyEdge {
            Id = edge.Id,
            SourceNodeId = edge.SourceNodeId,
            TargetNodeId = edge.TargetNodeId,
            Kind = edge.Kind,
            Status = edge.Status,
            Direction = edge.Direction,
            Routing = edge.Routing,
            LineStyle = edge.LineStyle,
            Emphasis = edge.Emphasis,
            SourcePort = edge.SourcePort,
            TargetPort = edge.TargetPort,
            SourcePortId = edge.SourcePortId,
            TargetPortId = edge.TargetPortId,
            PathGeometry = edge.PathGeometry, Interpolation = edge.Interpolation, InterpolationCornerBlendLength = edge.InterpolationCornerBlendLength, PreparedEdgePlan = edge.PreparedEdgePlan,
            LabelPosition = edge.LabelPosition, SourceMarkerStyle = edge.SourceMarkerStyle?.Clone(), TargetMarkerStyle = edge.TargetMarkerStyle?.Clone(),
            SourceMarker = edge.SourceMarker,
            TargetMarker = edge.TargetMarker,
            StrokeWidth = edge.StrokeWidth,
            Opacity = edge.Opacity,
            PreferredLength = edge.PreferredLength,
            MinimumRankSpan = edge.MinimumRankSpan,
            RoutingPriority = edge.RoutingPriority,
            RouteLane = edge.RouteLane,
            HasRouteLaneOverride = edge.HasRouteLaneOverride,
            LabelOffsetX = edge.LabelOffsetX,
            LabelOffsetY = edge.LabelOffsetY,
            LabelAnchorX = edge.LabelAnchorX,
            LabelAnchorY = edge.LabelAnchorY,
            HasLabelAnchorOverride = edge.HasLabelAnchorOverride,
            LabelAnchorNodeId = edge.LabelAnchorNodeId,
            LayoutInference = edge.LayoutInference,
            LabelContent = edge.LabelContent,
            TextStyle = edge.TextStyle?.Clone(),
            SecondaryLabel = edge.SecondaryLabel,
            TertiaryLabel = edge.TertiaryLabel,
            SourceLabel = edge.SourceLabel,
            TargetLabel = edge.TargetLabel,
            Href = edge.Href,
            Tooltip = edge.Tooltip,
            CssClass = edge.CssClass,
            Color = edge.Color,
            LabelColor = edge.LabelColor, LabelBackgroundColor = edge.LabelBackgroundColor,
            PaletteIndex = edge.PaletteIndex,
            IsMuted = edge.IsMuted
        };
        foreach (var item in edge.Metrics) copy.Metrics[item.Key] = item.Value;
        foreach (var item in edge.Metadata) copy.Metadata[item.Key] = item.Value;
        copy.Waypoints.AddRange(edge.Waypoints);
        copy.DashPattern.AddRange(edge.DashPattern);
        return copy;
    }
}
