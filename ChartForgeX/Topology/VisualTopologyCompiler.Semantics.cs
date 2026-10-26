using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private VisualArtifactInterchangeEnvelope SemanticSnapshot(ChartForgeX.Accessibility.VisualAccessibility accessibility) {
        var snapshot = TopologyLayoutEngine.Clone(_chart);
        snapshot.Annotations.RemoveAll(note => !_annotations.Exists(prepared => prepared.Note.Id == note.Id));
        snapshot.Legend = _legend;
        snapshot.Title = HeadingOrSource(_context.Frame.Title, SourceTitle); snapshot.Subtitle = HeadingOrSource(_context.Frame.Subtitle, SourceSubtitle);
        snapshot.Viewport = new TopologyViewport { Width = _context.Layout.Size.Width, Height = _context.Layout.Size.Height, Padding = _context.Layout.Padding };
        foreach (var note in snapshot.Annotations) {
            note.PreparedDefaultUnits *= _scale;
            if (note.PreparedDefaultUnits == 1 || note.TargetId == null) { note.PreparedRoutingViewport = null; continue; }
            var viewport = note.PreparedRoutingViewport ?? TopologyEdgeRouter.RoutingViewport(_chart);
            note.PreparedRoutingViewport = Bounds(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        }
        snapshot.PreparedFont = PreparedDiagramFont.Capture(_context);
        snapshot.PreparedFrame = PreparedDiagramFrame.Capture(_frameRequest, _options.IncludeLegend, _context.Theme.Resolve(_context.ThemeMode).Background.A == 0);
        if (snapshot.PreparedFrame != null) { snapshot.Title = SourceTitle; snapshot.Subtitle = SourceSubtitle; }
        snapshot.PreparedFramePadding = TopologyPreparedFramePadding.Capture(_context.Layout,
            TopologyPreparedContentClip.Capture(_plot, _contentClip), snapshot.Annotations.Exists(note => note.PreparedDefaultUnits != 1));
        foreach (var group in snapshot.Groups) {
            var b = Bounds(group.X, group.Y, group.Width, group.Height);
            group.X = b.X; group.Y = b.Y; group.Width = b.Width; group.Height = b.Height;
            PreserveCssClass(group.Metadata, group.CssClass);
            ScaleCapturedFontSize(group.TextStyle);
            if(group.StrokeWidth.HasValue)group.StrokeWidth*=_scale;
            for(var i=0;i<group.DashPattern.Count;i++)group.DashPattern[i]*=_scale;
        }
        foreach (var node in snapshot.Nodes) {
            var surfaceElements = TopologySurfaceElementAppearance.Resolve(node);
            var basis = TopologyPreparedNodeBasis.Resolve(node);
            var units = basis?.Factor ?? 1;
            var originalBody = basis?.OriginalBody ?? new ChartRect(node.X, node.Y, node.Width, node.Height);
            var mode = EffectiveNodeDisplayMode(node, _options);
            var actorScale = TopologyNodeShapeGeometry.ActorScale(node);
            var glyphAnchor = basis?.GlyphAnchor ?? new ChartPoint(node.X + node.Width / 2,
                node.Shape == TopologyNodeShape.ActorIcon ? node.Y + 32 * actorScale : node.Y + node.Height / 2);
            var b = Bounds(node.X, node.Y, node.Width, node.Height);
            var translated = b.X != node.X || b.Y != node.Y;
            var originalOffset = basis == null ? new ChartPoint(_offsetX, _offsetY)
                : basis.OriginalOffset(node, _scale, new ChartPoint(_offsetX, _offsetY));
            node.X = b.X; node.Y = b.Y; node.Width = b.Width; node.Height = b.Height;
            node.PreparedNodeBasis = TopologyPreparedNodeBasis.Recognized(node.Shape, mode)
                && (TopologyPreparedNodeBasis.IsActor(node.Shape)
                    || mode != TopologyNodeDisplayMode.Artwork && TopologyPreparedNodeBasis.Supported(node.Shape, mode)
                    || mode == TopologyNodeDisplayMode.Artwork && TopologyExternalCaption.HasPolicy(node))
                && (basis != null || units * _scale != 1
                    || translated && (node.Shape.HasValue || node.CornerRadius.HasValue || HasInlineNodeGlyph(node, mode, _options)))
                ? new TopologyPreparedNodeBasis(units * _scale, b.Width, b.Height, node.Shape, mode,
                    node.Kind, node.IconShape, node.IconId, node.Symbol, glyphAnchor.X, glyphAnchor.Y, originalBody, node.ContentPadding, node.MaximumTextWidth, node.CornerRadius * _scale, node.GlyphScale, !TopologyPreparedNodeBasis.IsActor(node.Shape)
                        ? new TopologyPreparedNodeProjection(originalOffset, new ChartPoint(b.X, b.Y)) : null) : null;
            if (node.PreparedNodeBasis != null) node.DisplayMode = mode;
            if (node.TableContent != null) node.TableContent.CapturedPresentationScale *= _scale;
            PreserveCssClass(node.Metadata, node.CssClass);
            ScaleCapturedFontSize(node.TextStyle); ScaleCapturedFontSize(node.SubtitleTextStyle); ScaleCapturedFontSize(node.CaptionTextStyle);
            if (node.CaptionGap.HasValue) node.CaptionGap *= _scale;
            if (node.CornerRadius.HasValue) node.CornerRadius *= _scale;
            if(node.StrokeWidth.HasValue)node.StrokeWidth*=_scale;
            for(var i=0;i<node.DashPattern.Count;i++)node.DashPattern[i]*=_scale;
            node.SurfaceElementAppearance = surfaceElements?.RebindAfterCapture(node, mode);
            foreach (var detail in node.Details) {
                ScaleCapturedFontSize(detail.TextStyle);
                if (detail.BadgeStyle != null) detail.BadgeStyle = detail.BadgeStyle.Scale(_scale);
            }
        }
        var edgeIndex = 0;
        foreach (var edge in snapshot.Edges) {
            var original = _chart.Edges[edgeIndex++];
            edge.PreparedEdgePlan = TopologyPreparedEdgePlan.CaptureResolved(original, _scale, _options, _highlight,
                _numericRoutes.Count > 0, CaptureNumericGeometry(original));
            if (edge.InterpolationCornerBlendLength.HasValue) edge.InterpolationCornerBlendLength *= _scale;
            PreserveCssClass(edge.Metadata, edge.CssClass);
            ScaleCapturedFontSize(edge.TextStyle);
            if (edge.PathGeometry != null) edge.PathGeometry = edge.PathGeometry.Transform(Point);
            if (edge.LabelPosition != null) edge.LabelPosition = edge.LabelPosition.Transform(Point);
            edge.SourceMarkerStyle = edge.SourceMarkerStyle?.Capture(_scale, _context.Theme.AxisStrokeWidth);
            edge.TargetMarkerStyle = edge.TargetMarkerStyle?.Capture(_scale, _context.Theme.AxisStrokeWidth);
            for (var i = 0; i < edge.Waypoints.Count; i++) edge.Waypoints[i] = Point(edge.Waypoints[i]);
            if (edge.HasLabelAnchorOverride) {
                var p = Point(new ChartPoint(edge.LabelAnchorX, edge.LabelAnchorY)); edge.LabelAnchorX = p.X; edge.LabelAnchorY = p.Y;
            }
            // Force paint already owns authored widths in output units; its geometry and marker units still project normally.
            if (edge.StrokeWidth.HasValue && !_options.UseForceGraphPresentation) edge.StrokeWidth *= _scale;
            edge.LabelOffsetX *= _scale; edge.LabelOffsetY *= _scale;
            for (var i = 0; i < edge.DashPattern.Count; i++) edge.DashPattern[i] *= _scale;
        }
        snapshot.CrossingPaint = _chart.CrossingPaint?.Scale(_scale);
        snapshot.PathDecorations.Clear();
        foreach (var path in _chart.PathDecorations) snapshot.PathDecorations.Add(path.Transform(Point, _scale));
        var envelope = VisualArtifactInterchangeMapping.FromPreparedTopology(snapshot, _options);
        for (var i = 0; i < _chart.Edges.Count; i++) {
            if (_resolvedLabelBounds.TryGetValue(_chart.Edges[i], out var labelBounds)) envelope.Edges[i].ResolvedLabelBounds = labelBounds;
            foreach (var point in _routes[_chart.Edges[i]]) {
                var p = Point(point);
                envelope.Edges[i].ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = p.X, Y = p.Y });
            }
        }
        envelope.AccessibleName = accessibility.Name;
        envelope.AccessibleDescription = accessibility.Description;
        envelope.Extensions["chartforgex.geometry"] = "prepared-scene";
        if (_chart.HierarchyPreparation != null)
            envelope.Topology!.Hierarchy = VisualArtifactInterchangeMapping.MapTopologyHierarchy(_chart.HierarchyPreparation);
        return envelope;
    }

    private void ScaleCapturedFontSize(ChartForgeX.Typography.TextStyleOverride? style) {
        if (style?.FontSize is double size) style.FontSize = size * _scale;
        if (style?.LetterSpacing is double spacing) style.LetterSpacing = spacing * _scale;
    }

    private static void PreserveCssClass(System.Collections.Generic.Dictionary<string, string> metadata, string? cssClass) {
        if (!string.IsNullOrWhiteSpace(cssClass)) metadata["chartforgex.sourceCssClass"] = cssClass!;
    }
}
