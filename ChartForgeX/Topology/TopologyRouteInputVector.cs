using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>Conservative finite authoring/policy prefix for the exact existing routing-input comparison.</summary>
/// <remarks>This records primitive values, never geometry algorithms or live graph/options references.</remarks>
internal sealed class TopologyRouteInputVector : IEquatable<TopologyRouteInputVector> {
    private readonly long[] _numbers;
    private readonly string?[] _texts;
    private TopologyRouteInputVector(List<long> numbers, List<string?> texts) { _numbers = numbers.ToArray(); _texts = texts.ToArray(); }
    internal static TopologyRouteInputVector Capture(TopologyChart chart, TopologyRenderOptions options, FontSpec font, bool recordedInputs = false) {
        var n = new List<long>(); var t = new List<string?>();
        void Number(double value) => n.Add(BitConverter.DoubleToInt64Bits(value));
        void Optional(double? value) { n.Add(value.HasValue ? 1 : 0); if (value.HasValue) Number(value.Value); }
        void Point(ChartPoint value) { Number(value.X); Number(value.Y); n.Add(value.BreakBefore ? 1 : 0); }
        void Font(FontSpec value) {
            t.Add(value.Family); t.Add(value.FilePath); t.Add(value.FaceName); n.Add(value.CollectionIndex ?? -1);
            n.Add(value.Weight); n.Add(value.Italic ? 1 : 0); n.Add(value.ColorPaletteIndex); t.Add(value.Variations.Key);
        }
        void Style(TextStyleOverride? policy) {
            n.Add(policy == null ? 0 : 1); if (policy == null) return;
            var style = policy.Resolve(new TextStyle { Font = font.Clone(), FontSize = chart.Theme.Typography.DataLabelSize, LineHeight = 1 });
            Font(style.Font); Number(style.FontSize); Number(style.LineHeight); t.Add(style.OpenTypeLanguageTag);
            n.Add((long)style.UnderlineStyle); n.Add((long)style.StrikethroughStyle); n.Add((long)style.Baseline); n.Add((long)style.TextCase); n.Add((long)style.Hinting);
        }
        void Content(TextContent? value) {
            n.Add(value == null ? 0 : 1); if (value == null) return;
            n.Add(value.IsStructured ? 1 : 0); n.Add(value.Runs.Count);
            foreach (var run in value.Runs) { t.Add(run.Text); n.Add((long)run.Emphasis); }
        }
        void Marker(TopologyMarkerStyle? style) {
            n.Add(style == null ? 0 : 1); if (style == null) return;
            Number(style.Scale); Number(style.Offset); Optional(style.StrokeWidth);
        }
        Number(chart.Viewport.Width); Number(chart.Viewport.Height); Number(chart.Viewport.Padding);
        n.Add((long)chart.LayoutMode); n.Add((long)chart.LayoutDirection); n.Add(chart.UsesPreparedSceneCoordinates ? 1 : 0);
        t.Add(chart.Title); t.Add(chart.Subtitle); Font(font); Number(chart.Theme.Typography.DataLabelSize);
        n.Add(options.IncludeNodeLabels ? 1 : 0); n.Add(options.IncludeIconLabels ? 1 : 0); n.Add(options.IncludeTileSubtitles ? 1 : 0);
        n.Add(options.WrapNodeLabels ? 1 : 0); n.Add(options.AllowMultilineNodeLabels ? 1 : 0); n.Add(options.ReadableDenseLayout ? 1 : 0);
        n.Add(options.MaxNodeLabelLines); n.Add(options.MaxNodeSubtitleLines); n.Add(options.IncludeGroups ? 1 : 0); n.Add(options.IncludeGroupLabels ? 1 : 0);
        n.Add(options.IncludeEdgeLabels ? 1 : 0); n.Add(options.IncludeEndpointLabels ? 1 : 0); n.Add(options.IncludeDirectionMarkers ? 1 : 0);
        n.Add(options.IncludeEdgeLabelBackplates ? 1 : 0); n.Add(options.IncludeEdgeLabelLeaders ? 1 : 0); n.Add(options.ShareIncomingTrunks ? 1 : 0);
        n.Add((long)options.NodeDisplayMode); n.Add((long)options.EdgeCornerStyle); Number(options.EdgeCornerRadius);
        n.Add((long)options.ArrowMarkerStyle); n.Add((long)options.VisualStyle); n.Add(options.UseForceGraphPresentation ? 1 : 0); n.Add((long)options.TextMeasurementMode);
        n.Add((long)options.CardSubtitleMode); n.Add((long)options.HeaderStyle); n.Add((long)options.GroupSurfaceStyle); n.Add((long)options.NodeSurfaceStyle);
        var lineStyle = options.EdgeVisualStyle; n.Add(lineStyle == null ? 0 : 1);
        if (lineStyle != null) { Number(lineStyle.AmbientHaloOpacity); Number(lineStyle.AmbientHaloStrokeExtra); Number(lineStyle.HaloOpacity);
            Number(lineStyle.HaloStrokeExtra); Number(lineStyle.HighlightOpacity); Number(lineStyle.HighlightStrokeRatio); }
        n.Add(chart.Groups.Count);
        foreach (var group in chart.Groups) {
            t.Add(group.Id); t.Add(group.ParentGroupId); t.Add(group.Label); t.Add(group.Subtitle);
            Number(group.X); Number(group.Y); Number(group.Width); Number(group.Height); Optional(group.Longitude); Optional(group.Latitude);
            n.Add(group.ShowLabel ? 1 : 0); n.Add((long)group.Presentation); Style(group.TextStyle); Content(group.LabelContent);
        }
        n.Add(chart.Nodes.Count);
        foreach (var node in chart.Nodes) {
            t.Add(node.Id); t.Add(node.GroupId); t.Add(node.Label); t.Add(node.Subtitle); t.Add(node.IconId); t.Add(node.Symbol);
            Number(node.X); Number(node.Y); Number(node.Width); Number(node.Height); Optional(node.Longitude); Optional(node.Latitude);
            n.Add((long?)node.Shape ?? -1); n.Add((long?)node.IconShape ?? -1); n.Add((long)EffectiveNodeDisplayMode(node, options)); n.Add((long)node.Kind);
            n.Add(node.ShowLabel ? 1 : 0); Optional(node.ContentPadding); Optional(node.MaximumTextWidth); n.Add(node.MaximumLabelCharacters ?? -1);
            Number(TopologyPreparedNodeBasis.Units(node)); Style(node.TextStyle); Style(node.SubtitleTextStyle); Content(node.LabelContent);
            Optional(node.CornerRadius); Optional(node.GlyphScale); Optional(node.CaptionGap);
            n.Add((long?)node.CaptionPlacement ?? -1); n.Add((long?)node.CaptionSurface ?? -1); Style(node.CaptionTextStyle);
            n.Add(node.Ports.Count); foreach (var port in node.Ports) { t.Add(port.Id); n.Add((long)port.Side); Number(port.Offset); }
        }
        n.Add(chart.Edges.Count);
        var highlight = TopologyHighlightState.From(chart, options);
        foreach (var edge in chart.Edges) {
            t.Add(edge.Id); t.Add(edge.SourceNodeId); t.Add(edge.TargetNodeId); t.Add(edge.SourcePortId); t.Add(edge.TargetPortId);
            t.Add(edge.Label); t.Add(edge.SecondaryLabel); t.Add(edge.TertiaryLabel); t.Add(edge.SourceLabel); t.Add(edge.TargetLabel); t.Add(edge.LabelAnchorNodeId);
            n.Add((long)edge.Routing); n.Add((long)edge.SourcePort); n.Add((long)edge.TargetPort); n.Add((long?)edge.Interpolation ?? -1); Optional(edge.InterpolationCornerBlendLength);
            n.Add(edge.HasRouteLaneOverride ? 1 : 0); Number(edge.RouteLane); n.Add((long)edge.LayoutInference); n.Add(edge.RoutingPriority); n.Add(edge.MinimumRankSpan); Optional(edge.PreferredLength);
            n.Add(edge.Waypoints.Count); foreach (var point in edge.Waypoints) Point(point);
            n.Add(edge.LabelPosition == null ? 0 : 1);
            if (edge.LabelPosition != null) { Point(edge.LabelPosition.Point); n.Add((long)edge.LabelPosition.VerticalAnchor); }
            n.Add(edge.HasLabelAnchorOverride ? 1 : 0); Number(edge.LabelAnchorX); Number(edge.LabelAnchorY); Number(edge.LabelOffsetX); Number(edge.LabelOffsetY);
            Content(edge.LabelContent); t.Add(EdgeLabel(edge, options.EdgeLabelMetricKey, edge.Label));
            t.Add(EdgeLabel(edge, options.EdgeSecondaryLabelMetricKey, edge.SecondaryLabel)); t.Add(EdgeLabel(edge, options.EdgeTertiaryLabelMetricKey, edge.TertiaryLabel));
            t.Add(edge.PathGeometry?.PathData); Marker(edge.SourceMarkerStyle); Marker(edge.TargetMarkerStyle); Style(edge.TextStyle);
            n.Add((long?)edge.SourceMarker ?? -1); n.Add((long?)edge.TargetMarker ?? -1); n.Add((long)edge.Direction); n.Add((long)edge.Kind); n.Add((long)edge.Status);
            n.Add((long)edge.LineStyle); n.Add((long)edge.Emphasis); Optional(edge.StrokeWidth); Optional(edge.Opacity); n.Add(edge.IsMuted ? 1 : 0); n.Add(edge.PaletteIndex ?? -1);
            t.Add(edge.Color); n.Add(edge.DashPattern.Count); foreach (var value in edge.DashPattern) Number(value);
            Number(TopologyPreparedEdgePlan.Units(edge)); n.Add((recordedInputs ? edge.PreparedEdgePlan?.SelectedInput ?? false : options.SelectedEdgeIds.Contains(edge.Id)) ? 1 : 0);
            n.Add((recordedInputs ? edge.PreparedEdgePlan?.HighlightedInput ?? true : highlight.IsEdgeHighlighted(edge)) ? 1 : 0);
        }
        return new TopologyRouteInputVector(n, t);
    }
    public bool Equals(TopologyRouteInputVector? other) {
        if (other == null || _numbers.Length != other._numbers.Length || _texts.Length != other._texts.Length) return false;
        for (var i = 0; i < _numbers.Length; i++) if (_numbers[i] != other._numbers[i]) return false;
        for (var i = 0; i < _texts.Length; i++) if (!string.Equals(_texts[i], other._texts[i], StringComparison.Ordinal)) return false;
        return true;
    }
    public override bool Equals(object? other) => other is TopologyRouteInputVector value && Equals(value);
    public override int GetHashCode() => _numbers.Length ^ _texts.Length;
}
