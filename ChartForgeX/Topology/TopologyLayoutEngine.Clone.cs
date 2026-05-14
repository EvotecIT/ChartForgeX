namespace ChartForgeX.Topology;

internal static partial class TopologyLayoutEngine {
    private static TopologyChart Clone(TopologyChart chart) {
        var copy = new TopologyChart {
            Id = chart.Id,
            Title = chart.Title,
            Subtitle = chart.Subtitle,
            LayoutMode = chart.LayoutMode,
            LayoutDirection = chart.LayoutDirection,
            Viewport = new TopologyViewport { Width = chart.Viewport.Width, Height = chart.Viewport.Height, Padding = chart.Viewport.Padding },
            MapViewport = chart.MapViewport,
            Legend = chart.Legend,
            Theme = chart.Theme
        };
        foreach (var group in chart.Groups) copy.Groups.Add(Clone(group));
        foreach (var node in chart.Nodes) copy.Nodes.Add(Clone(node));
        foreach (var edge in chart.Edges) copy.Edges.Add(Clone(edge));
        foreach (var scenario in chart.Scenarios) copy.Scenarios.Add(TopologyScenarioCloner.Clone(scenario));
        return copy;
    }

    private static TopologyGroup Clone(TopologyGroup group) {
        var copy = new TopologyGroup {
            Id = group.Id,
            Label = group.Label,
            Subtitle = group.Subtitle,
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
            LayoutPolicy = group.LayoutPolicy,
            AppliedLayoutPolicy = group.AppliedLayoutPolicy,
            HasPositionOverride = group.HasPositionOverride
        };
        foreach (var item in group.Metadata) copy.Metadata[item.Key] = item.Value;
        return copy;
    }

    private static TopologyNode Clone(TopologyNode node) {
        var copy = new TopologyNode {
            Id = node.Id,
            Label = node.Label,
            Subtitle = node.Subtitle,
            Kind = node.Kind,
            Symbol = node.Symbol,
            IconId = node.IconId,
            Artwork = node.Artwork,
            DisplayMode = node.DisplayMode,
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
            BackgroundColor = node.BackgroundColor
        };
        foreach (var item in node.Metrics) copy.Metrics[item.Key] = item.Value;
        foreach (var item in node.Metadata) copy.Metadata[item.Key] = item.Value;
        return copy;
    }

    private static TopologyEdge Clone(TopologyEdge edge) {
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
            RouteLane = edge.RouteLane,
            HasRouteLaneOverride = edge.HasRouteLaneOverride,
            LabelOffsetX = edge.LabelOffsetX,
            LabelOffsetY = edge.LabelOffsetY,
            LabelAnchorX = edge.LabelAnchorX,
            LabelAnchorY = edge.LabelAnchorY,
            HasLabelAnchorOverride = edge.HasLabelAnchorOverride,
            LabelAnchorNodeId = edge.LabelAnchorNodeId,
            LayoutInference = edge.LayoutInference,
            Label = edge.Label,
            SecondaryLabel = edge.SecondaryLabel,
            TertiaryLabel = edge.TertiaryLabel,
            Href = edge.Href,
            Tooltip = edge.Tooltip,
            CssClass = edge.CssClass,
            Color = edge.Color,
            IsMuted = edge.IsMuted
        };
        foreach (var item in edge.Metrics) copy.Metrics[item.Key] = item.Value;
        foreach (var item in edge.Metadata) copy.Metadata[item.Key] = item.Value;
        copy.Waypoints.AddRange(edge.Waypoints);
        return copy;
    }
}
