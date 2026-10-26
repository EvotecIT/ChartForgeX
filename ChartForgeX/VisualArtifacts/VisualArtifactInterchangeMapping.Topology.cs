using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Restores an editable Manual topology from validated portable graph data and prepared bounds.</summary>
    /// <param name="envelope">A typed Topology-family envelope. Common metadata and source-language facts remain on this envelope.</param>
    /// <returns>A new graph with detached mutable entities, metadata, detail styles, artwork, legend and scenarios.</returns>
    /// <remarks>Envelope IDs are authoritative; opaque extension values never replace identity or native properties.
    /// Prepared node/group bounds and explicit authored route hints are restored in scene coordinates.
    /// Default Manual preparation retains that origin without applying the title frame a second time.
    /// Explicit fitting and automatic layout may change those coordinates. Sampled resolved routes and
    /// label diagnostics remain on the envelope. Missing authoring options, padding, theme palette and typography
    /// sizes are not inferred. Set LayoutMode explicitly to request a fresh automatic layout after editing.
    /// Captured interpolation retains its numeric path and measured caption allocations while the exact graph/input
    /// witness matches. Edits recompute through the native owner; automatic layout also resets retained default units.
    /// This does not rebuild source text or promise byte-identical paint for every native presentation. No SVG fallback is consulted.</remarks>
    /// <exception cref="NotSupportedException">The family or a carried field has no faithful native topology representation.</exception>
    public static TopologyChart ToTopologyChart(this VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        envelope.Validate();
        RequireEditableSourcePresentation(envelope);
        if (envelope.Family != VisualArtifactInterchangeFamily.Topology || envelope.Topology == null)
            throw new NotSupportedException("Only a typed Topology-family envelope can restore an editable topology.");
        var chart = TopologyChart.Create();
        chart.Id = envelope.Id; chart.Title = envelope.Title; chart.Subtitle = envelope.Subtitle;
        chart.LayoutMode = TopologyLayoutMode.Manual; chart.LayoutDirection = envelope.Topology.LayoutDirection;
        chart.UsesPreparedSceneCoordinates = true;
        if (envelope.Width.HasValue) chart.Viewport.Width = envelope.Width.Value;
        if (envelope.Height.HasValue) chart.Viewport.Height = envelope.Height.Value;
        if (envelope.Topology.ViewportPadding.HasValue) {
            var layout = new VisualLayoutOptions(new VisualSize(chart.Viewport.Width, chart.Viewport.Height), envelope.Topology.ViewportPadding.Value);
            chart.Viewport.Padding = layout.Padding;
            chart.PreparedFramePadding = TopologyPreparedFramePadding.Capture(layout, envelope.Topology.ContentClip);
        }
        chart.Accessibility.Name = envelope.AccessibleName; chart.Accessibility.Description = envelope.AccessibleDescription;
        chart.Accessibility.Language = envelope.Language; chart.Accessibility.IsDecorative = envelope.IsDecorative;
        RestoreTopologyPresentation(chart, envelope.Presentation);
        RestoreTopologyAlignments(chart, envelope.Topology.Alignments);
        chart.CrossingPaint = envelope.Topology.CrossingPaint;
        chart.PathDecorations.AddRange(envelope.Topology.PathDecorations);
        foreach (var source in envelope.Groups) chart.Groups.Add(RestoreTopologyGroup(source));
        foreach (var source in envelope.Nodes) chart.Nodes.Add(RestoreTopologyNode(source));
        if (envelope.Edges.Select(edge => edge.Order).Distinct().Count() != envelope.Edges.Count)
            throw new NotSupportedException("Topology edge order must be unique to restore the native ordered edge collection.");
        foreach (var source in envelope.Edges.OrderBy(edge => edge.Order)) chart.Edges.Add(RestoreTopologyEdge(source));
        foreach (var source in envelope.Scenarios) chart.Scenarios.Add(RestoreTopologyScenario(source));
        foreach (var source in envelope.Annotations) chart.Annotations.Add(RestoreTopologyAnnotation(source));
        var validation = new TopologyChartValidator().Validate(chart);
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        if (envelope.Topology.ContentClip != null && chart.PreparedFramePadding == null)
            chart.PreparedFramePadding = TopologyPreparedFramePadding.Capture(new VisualLayoutOptions(
                new VisualSize(chart.Viewport.Width, chart.Viewport.Height), chart.Viewport.Padding), envelope.Topology.ContentClip);
        if (chart.Annotations.Exists(note => note.PreparedDefaultUnits != 1) && chart.PreparedFramePadding == null)
            chart.PreparedFramePadding = TopologyPreparedFramePadding.Capture(new VisualLayoutOptions(
                new VisualSize(chart.Viewport.Width, chart.Viewport.Height), chart.Viewport.Padding), retainForAnnotationUnits: true);
        TopologyPreparedFramePadding.AttachPreparedFrame(chart);
        TopologyPreparedRouteBasis.Attach(chart);
        return chart;
    }

    private static TopologyGroup RestoreTopologyGroup(VisualArtifactInterchangeGroup source) {
        var native = source.Topology!;
        RequireTopologyToken(source.Kind, "TopologyGroup", "group kind");
        RequireTopologyToken(source.Status, native.Status.ToString(), "group status");
        var group = new TopologyGroup {
            StrokeWidth=native.StrokeWidth,Opacity=native.Opacity,SurfaceOpacity=native.SurfaceOpacity,
            TextStyle = native.TextStyle?.Clone(),
            Id = source.Id, LabelContent = native.LabelContent ?? ChartForgeX.Typography.TextContent.Plain(source.Label), Subtitle = source.Subtitle,
            X = TopologyBound(source.X, "group x"), Y = TopologyBound(source.Y, "group y"),
            Width = TopologyBound(source.Width, "group width"), Height = TopologyBound(source.Height, "group height"),
            Href = source.Href, Tooltip = source.Tooltip, Color = source.Color,
            BackgroundColor = source.BackgroundColor, BorderColor = source.BorderColor, LabelColor = source.LabelColor,
            ParentGroupId = native.ParentGroupId, Presentation = native.Presentation, LayoutDirection = native.LayoutDirection, ShowLabel = native.ShowLabel,
            Status = native.Status, LayoutPolicy = native.LayoutPolicy, AppliedLayoutPolicy = native.AppliedLayoutPolicy,
            Longitude = native.Longitude, Latitude = native.Latitude, IconId = native.IconId, Symbol = native.Symbol
        };
        group.DashPattern.AddRange(native.DashPattern);
        RestoreTopologyMetadata(source.Extensions, group.Metadata);
        return group;
    }

    private static TopologyNode RestoreTopologyNode(VisualArtifactInterchangeNode source) {
        var native = source.Topology!;
        if (native.PreparedNodeBasis != null && !TopologyPreparedNodeBasis.Supported(native.Shape, native.DisplayMode))
            throw new NotSupportedException("Captured node-unit restoration requires a qualified Actor Card/Compact or native Card/Compact/Icon/Tile/Pill/Dot/explicit-Artwork presentation; other defined native modes remain available for static SVG/PNG and portable capture.");
        RequireTopologyToken(source.Kind, native.Kind.ToString(), "node kind");
        RequireTopologyToken(source.Status, native.Status.ToString(), "node status");
        var node = new TopologyNode {
            BorderColor=native.BorderColor,
            CaptionPlacement=native.CaptionPlacement,CaptionSurface=native.CaptionSurface,CaptionGap=native.CaptionGap,
            GlyphScale=native.GlyphScale,CornerRadius=native.CornerRadius,CaptionTextStyle=native.CaptionTextStyle?.Clone(),
            PreparedNodeBasis = native.PreparedNodeBasis,
            StrokeWidth=native.StrokeWidth,Opacity=native.Opacity,SurfaceOpacity=native.SurfaceOpacity,
            SubtitleTextStyle = native.SubtitleTextStyle?.Clone(),
            TextStyle = native.TextStyle?.Clone(), MaximumTextWidth = native.MaximumTextWidth, ContentPadding = native.ContentPadding,
            TableContent = RestoreTableContent(native.TableContent),
            Id = source.Id, LabelContent = native.LabelContent ?? ChartForgeX.Typography.TextContent.Plain(source.Label), Subtitle = source.Subtitle, GroupId = source.GroupId,
            X = TopologyBound(source.X, "node x"), Y = TopologyBound(source.Y, "node y"),
            Width = TopologyBound(source.Width, "node width"), Height = TopologyBound(source.Height, "node height"),
            IconId = source.IconId, Symbol = source.Symbol, Badge = source.Badge,
            Color = source.Color, BackgroundColor = source.BackgroundColor, LabelColor = source.LabelColor,
            Href = source.Href, Tooltip = source.Tooltip, Kind = native.Kind, PaletteIndex = native.PaletteIndex,
            Status = native.Status, DisplayMode = native.DisplayMode, Shape = native.Shape, IconShape = native.IconShape,
            PreserveDisplayModeSize = true, FitLabelToContent = native.FitLabelToContent,
            Longitude = native.Longitude, Latitude = native.Latitude, ShowStatusBadge = native.ShowStatusBadge,
            ShowLabel = native.ShowLabel, MaximumLabelCharacters = native.MaximumLabelCharacters,
            Artwork = RestoreTopologyArtwork(native.Artwork)
        };
        node.SurfaceElementAppearance = native.SurfaceElementAppearance;
        node.DashPattern.AddRange(native.DashPattern);
        RestoreTopologyMetadata(source.Extensions, node.Metadata);
        foreach (var metric in source.Metrics) node.Metrics.Add(metric.Name, metric.Value);
        foreach (var sourcePort in source.Ports) {
            var port = new TopologyNodePort { Id = sourcePort.Id, Side = sourcePort.Side, Offset = sourcePort.Offset, Label = sourcePort.Label };
            RestoreTopologyMetadata(sourcePort.Extensions, port.Metadata); node.Ports.Add(port);
        }
        foreach (var sourceDetail in source.Details) {
            TopologyHealthStatus? status = null;
            if (sourceDetail.Status != null) {
                if (!Enum.TryParse<TopologyHealthStatus>(sourceDetail.Status, out var parsed) || !Enum.IsDefined(typeof(TopologyHealthStatus), parsed)
                    || parsed.ToString() != sourceDetail.Status)
                    throw new NotSupportedException("Topology detail status must be a native health-status name.");
                status = parsed;
            }
            var detail = new TopologyNodeDetail {
                Content = sourceDetail.Content ?? (sourceDetail.Text == null ? null : ChartForgeX.Typography.TextContent.Plain(sourceDetail.Text)), Label = sourceDetail.Label,
                ValueContent = sourceDetail.ValueContent ?? ChartForgeX.Typography.TextContent.Plain(sourceDetail.Value), IconId = sourceDetail.IconId,
                Status = status, Color = sourceDetail.Color, TextStyle = sourceDetail.TextStyle?.Clone(), BadgeStyle = sourceDetail.BadgeStyle?.Clone()
            };
            RestoreTopologyMetadata(sourceDetail.Extensions, detail.Metadata); node.Details.Add(detail);
        }
        return node;
    }

    private static TopologyEdge RestoreTopologyEdge(VisualArtifactInterchangeEdge source) {
        var native = source.Topology!;
        RequireTopologyToken(source.Kind, native.Kind.ToString(), "edge kind");
        RequireTopologyToken(source.Status, native.Status.ToString(), "edge status");
        if (native.MinimumRankSpan < 1 || native.StrokeWidth == 0)
            throw new NotSupportedException("Native topology edges require a positive stroke width when set and a minimum rank span of at least one.");
        var edge = new TopologyEdge {
            PathGeometry = native.PathGeometry, Interpolation = native.Interpolation, InterpolationCornerBlendLength = native.InterpolationCornerBlendLength, LabelPosition = native.LabelPosition, PreparedEdgePlan = native.PreparedEdgePlan,
            SourceMarkerStyle = native.SourceMarkerStyle?.Clone(), TargetMarkerStyle = native.TargetMarkerStyle?.Clone(),
            TextStyle = native.TextStyle?.Clone(),
            Id = source.Id, SourceNodeId = source.SourceId, TargetNodeId = source.TargetId,
            SourcePortId = source.SourcePortId, TargetPortId = source.TargetPortId,
            LabelContent = native.LabelContent ?? (source.Label == null ? null : ChartForgeX.Typography.TextContent.Plain(source.Label)), SecondaryLabel = source.SecondaryLabel, TertiaryLabel = source.TertiaryLabel,
            SourceLabel = source.SourceLabel, TargetLabel = source.TargetLabel, Href = source.Href, Tooltip = source.Tooltip,
            Color = source.Color, LabelColor = source.LabelColor, LabelBackgroundColor = native.LabelBackgroundColor,
            Kind = native.Kind, PaletteIndex = native.PaletteIndex,
            Status = native.Status, Direction = native.Direction, SourcePort = native.SourcePort, TargetPort = native.TargetPort,
            LineStyle = native.LineStyle, Routing = native.Routing, Emphasis = native.Emphasis,
            SourceMarker = native.SourceMarker, TargetMarker = native.TargetMarker,
            StrokeWidth = native.StrokeWidth, Opacity = native.Opacity, IsMuted = native.IsMuted, RoutingPriority = native.RoutingPriority,
            LabelOffsetX = native.LabelOffsetX, LabelOffsetY = native.LabelOffsetY, LabelAnchorNodeId = native.LabelAnchorNodeId,
            LayoutInference = native.LayoutInference, PreferredLength = native.PreferredLength, MinimumRankSpan = native.MinimumRankSpan
        };
        if (edge.LabelAnchorNodeId != native.LabelAnchorNodeId)
            throw new NotSupportedException("Native topology label-anchor IDs cannot contain surrounding whitespace.");
        if (native.RouteLane.HasValue) { edge.RouteLane = native.RouteLane.Value; edge.HasRouteLaneOverride = true; }
        if (native.LabelAnchor != null) {
            edge.LabelAnchorX = native.LabelAnchor.X; edge.LabelAnchorY = native.LabelAnchor.Y; edge.HasLabelAnchorOverride = true;
        }
        edge.DashPattern.AddRange(native.DashPattern);
        foreach (var point in native.Waypoints) edge.Waypoints.Add(new ChartPoint(point.X, point.Y));
        RestoreTopologyMetadata(source.Extensions, edge.Metadata);
        foreach (var metric in source.Metrics) edge.Metrics.Add(metric.Name, metric.Value);
        return edge;
    }

    private static TopologyScenario RestoreTopologyScenario(VisualArtifactInterchangeScenario source) {
        var scenario = new TopologyScenario {
            Id = source.Id, Label = source.Label, Description = source.Description, Color = source.Color,
            PlaybackDelayMilliseconds = source.PlaybackDelayMilliseconds, LoopPlayback = source.LoopPlayback,
            AutoPlay = source.AutoPlay, Spotlight = source.Spotlight
        };
        if (scenario.Id != source.Id || scenario.Label != source.Label)
            throw new NotSupportedException("Native topology scenario IDs and labels cannot contain surrounding whitespace.");
        RestoreTopologyMetadata(source.Extensions, scenario.Metadata);
        foreach (var item in source.Steps) {
            var step = new TopologyScenarioStep { Id = item.TargetId, Kind = item.Kind, Label = item.Label,
                Description = item.Description, DurationMilliseconds = item.DurationMilliseconds };
            if (step.Id != item.TargetId) throw new NotSupportedException("Native topology scenario target IDs cannot contain surrounding whitespace.");
            RestoreTopologyMetadata(item.Extensions, step.Metadata); scenario.Steps.Add(step);
        }
        return scenario;
    }

    private static TopologyAnnotation RestoreTopologyAnnotation(VisualArtifactInterchangeAnnotation source) {
        if (source.Kind != "TopologyNote" || source.TargetIds.Count > 1 || source.Extensions.Count != 0
            || source.StartIndex.HasValue || source.EndIndex.HasValue
            || !Enum.TryParse<TopologyAnnotationSide>(source.Placement, out var side)
            || !Enum.IsDefined(typeof(TopologyAnnotationSide), side) || side.ToString() != source.Placement)
            throw new NotSupportedException("Native topology annotations require a TopologyNote, Left/Right placement, at most one attachment, and no indexed spans or extension data.");
        return new TopologyAnnotation(source.Id, source.Text, source.TargetIds.Count == 0 ? null : source.TargetIds[0], side) { PreparedDefaultUnits = source.TopologyDefaultUnits ?? 1, PreparedRoutingViewport = source.TopologyRoutingViewport };
    }

    private static void RestoreTopologyMetadata(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, string> target) {
        foreach (var entry in source) target.Add(entry.Key, entry.Value);
    }

    private static double TopologyBound(double? value, string name) => value
        ?? throw new NotSupportedException("Topology restoration requires prepared " + name + " bounds.");

    private static void RequireTopologyToken(string? value, string expected, string name) {
        if (value != null && value != expected)
            throw new NotSupportedException("Topology " + name + " must agree with its typed native semantics.");
    }
}
