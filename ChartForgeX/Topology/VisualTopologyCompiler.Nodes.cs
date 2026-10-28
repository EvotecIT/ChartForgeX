using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildNode(TopologyNode node) {
        if (TryBuildCapturedNode(node)) return;
        using var textPolicy = UseTextPolicy(node.TextStyle);
        if (node.TextStyle?.Color is ChartColor authoredTextColor) node.LabelColor = authoredTextColor.ToHexRgba();
        var mode = EffectiveNodeDisplayMode(node, _options);
        if (mode == TopologyNodeDisplayMode.Hidden) return;
        var bounds = Bounds(node.X, node.Y, node.Width, node.Height);
        var shapeContent = UseEnclosedShapeContent(node, mode, _options);
        var active = _highlight.IsNodeHighlighted(node);
        var icon = ResolveNodeIcon(node, _options);
        if (_options.RequireResolvedIcons && !string.IsNullOrWhiteSpace(node.IconId) && icon == null && !HasExplicitIconShape(node.IconShape) && ResolveRenderableNodeArtwork(node, _options) == null) throw new InvalidOperationException("Unresolved topology icon: " + node.IconId);
        var authoredAccent = NodeAccentSource(node);
        var accentRole = NodeUsesPalette(node) && string.IsNullOrWhiteSpace(authoredAccent) ? SvgColorRole.Series : AccentRole(authoredAccent);
        var baseAccent = NodeAccent(node);
        var accent = Highlight(baseAccent, active);
        var baseFill = string.IsNullOrWhiteSpace(node.BackgroundColor) && EffectiveNodeSurfaceStyle(_options) != TopologyNodeSurfaceStyle.Card
            ? ChartColorMath.BlendPremultiplied(_colors.Background, baseAccent, IsMonitoringDashboardStyle(_options) ? .065 : .10)
            : Color(NodeFill(node, Theme(), baseAccent.ToHexRgba(), _options), _colors.Surface);
        var fill = Highlight(baseFill, active);
        var accentPaint = NodeAccentPaint(node, accent, active);
        var surfacePaint = new VisualScenePaintBinding(NodeFillPaint(node, baseFill, baseAccent, accentRole, active), accentPaint);
        var strokeWidth = NodeSurfaceStrokeWidth(node) * _scale;
        var surfaceDash=SurfaceDash(node.DashPattern);
        using (Host(node.Href, node.Tooltip, node.Id, "topology-node-host"))
        using (_builder.PushGroup(node.Id, "topology-node", NodeMetadata(node),opacity:node.Opacity??1)) {
            Action? beforeArtwork = mode == TopologyNodeDisplayMode.Artwork && HasArtworkSurfacePolicy(node)
                ? () => BuildNodeSurface(node, mode, bounds, fill, accent, strokeWidth, surfaceDash, surfacePaint, accentPaint) : null;
            var image = mode == TopologyNodeDisplayMode.Artwork && BuildArtwork(node, bounds, beforePaint: beforeArtwork);
            if (!image) {
                BuildNodeSurface(node, mode, bounds, fill, accent, strokeWidth, surfaceDash, surfacePaint, accentPaint);
                if (!shapeContent && UseNodeAccentBand(mode, _options)) _builder.Rect(new ChartRect(bounds.X + strokeWidth / 2, bounds.Y + strokeWidth / 2, 4 * _scale, Math.Max(0, bounds.Height - strokeWidth)), accent, role: "topology-node-accent", paint: new VisualScenePaintBinding(accentPaint));
                var hasGlyph = HasInlineNodeGlyph(node, mode, _options);
                if (node.Shape == TopologyNodeShape.ActorIcon && !hasGlyph) BuildActorIcon(node, accent, active, accentRole);
                if (shapeContent && (node.ShowLabel || node.TableContent != null)) BuildShapeContent(node, accent, active, accentRole);
                if (!shapeContent && hasGlyph && mode != TopologyNodeDisplayMode.Dot) {
                    var center = mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Artwork;
                    if (node.Shape == TopologyNodeShape.ActorIcon) BuildActorIcon(node, accent, active, accentRole);
                    else BuildGlyph(node, center ? node.X + node.Width / 2 : node.X + 22, node.Y + (mode == TopologyNodeDisplayMode.Card && _options.IncludeNodeLabels && node.Details.Count > 0 ? 28 : node.Height / 2), accent, glyphRole: accentRole);
                }
                if (!shapeContent && node.ShowLabel && _options.IncludeNodeLabels && (mode != TopologyNodeDisplayMode.Icon || _options.IncludeIconLabels)) {
                    var caption = mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Artwork or TopologyNodeDisplayMode.Icon;
                    var left = hasGlyph && !caption ? mode == TopologyNodeDisplayMode.Pill ? 34 : 44 : 10;
                    // A label-only card's heading sits above its glyph, so it can use the full heading row.
                    if (mode == TopologyNodeDisplayMode.Card && hasGlyph && node.Details.Count == 0
                        && !node.Shape.HasValue && string.IsNullOrWhiteSpace(node.Symbol) && string.IsNullOrWhiteSpace(node.IconId)
                        && ResolveRenderableNodeArtwork(node, _options) == null
                        && string.IsNullOrWhiteSpace(node.Subtitle) && string.IsNullOrWhiteSpace(node.Badge)
                        && MeasurePolicyText(node.Label, TextPolicySize(_context.Theme.Typography.DataLabelSize), 600).Width > node.Width - left - 12) left = 10;
                    var textInset = mode == TopologyNodeDisplayMode.Pill && !string.IsNullOrWhiteSpace(node.Subtitle) ? 4 : 6;
                    var textBounds = caption ? Bounds(node.X - 17, node.Y + node.Height + 5, node.Width + 34, (_options.MaxNodeLabelLines + 1) * 18) : Bounds(node.X + left, node.Y + textInset, Math.Max(0, node.Width - left - 12), Math.Max(0, node.Height - textInset * 2));
                    if (TopologyExternalCaption.HasPolicy(node)) BuildExternalCaption(node, accent, active);
                    else if (mode == TopologyNodeDisplayMode.Tile) BuildTileCaption(node, accent, active);
                    else if (mode == TopologyNodeDisplayMode.Icon) BuildIconCaption(node, accent, active);
                    else if (TopologyNodeShapeGeometry.IsActor(node)) BuildDiagramCaption(node, active);
                    else if (mode != TopologyNodeDisplayMode.Dot && (node.Shape.HasValue || node.CornerRadius.HasValue) && !hasGlyph && !caption
                        && string.IsNullOrWhiteSpace(node.Subtitle) && node.Details.Count == 0 && string.IsNullOrWhiteSpace(node.Badge))
                        BuildShapeCaption(node, active);
                    else if (mode != TopologyNodeDisplayMode.Dot) BuildNodeText(node, textBounds, accent, active, caption);
                }
            }
            if (image && TopologyExternalCaption.HasPolicy(node)) BuildExternalCaption(node, accent, active);
            if (!shapeContent && _options.IncludeStatusBadges && node.ShowStatusBadge && mode != TopologyNodeDisplayMode.Dot) {
                var status = Highlight(Status(node.Status), active);
                using var statusGroup = _builder.PushGroup(node.Id + "-status", "topology-node-status", new Dictionary<string, string> { ["data-node-id"] = node.Id });
                var statusCenter = node.Shape == TopologyNodeShape.Cloud
                    ? Point(new ChartPoint(node.X + node.Width * .78, node.Y + node.Height * .34))
                    : new ChartPoint(bounds.Right - 10 * _scale, bounds.Y + 10 * _scale);
                using (PinnedState()) _builder.Ellipse(statusCenter.X, statusCenter.Y, 3 * _scale, 3 * _scale, status, role: "topology-status", paint: new VisualScenePaintBinding(StatusPaint(node.Status, status, HighlightFactor(active))));
            }
            if (!shapeContent && !string.IsNullOrWhiteSpace(node.Badge)) {
                var reserved = NodeBadgeBounds(node, mode, _options);
                var badge = Bounds(reserved.X, reserved.Y, reserved.Width, reserved.Height);
                _builder.Rect(badge, accent, radius: 4 * _scale, role: "topology-node-badge-surface", paint: new VisualScenePaintBinding(accentPaint));
                var ink = ChartColorMath.AccessibleTextOnBackground(accent);
                Text(NodeBadge(node), badge, TextPolicySize(_context.Theme.Typography.DataLabelSize) * .75, ink, 600, "topology-node-badge", 1, centered: true,
                    paint: SvgPaint.Contrast(accent, accentPaint), fitSingleLine: true);
            }
        }
        _builder.AddRegion(new VisualSemanticRegion(node.Id, "topology-node", bounds, node.Label));
    }

    private void BuildNodeText(TopologyNode node, ChartRect bounds, ChartColor accent, bool active, bool caption) {
        var size = TextPolicySize(_context.Theme.Typography.DataLabelSize);
        var label = node.MaximumLabelCharacters.HasValue ? TrimTo(node.Label, node.MaximumLabelCharacters.Value) : node.Label;
        if (node.Details.Count > 0 || !string.IsNullOrWhiteSpace(node.Subtitle) && (!caption || _options.IncludeTileSubtitles))
            size = FitNodeTextSize(node, label, bounds, size, caption);
        else if (CanFitNodeLabelToContent(node)) size = FitMeasuredNodeLabelSize(label, size, _ => bounds);
        var titleLines = node.LabelContent.IsStructured ? CaptionRowCount(NodeCaptionContent(node), bounds.Width, size * _scale, _options.MaxNodeLabelLines)
            : TextLineCount(label, bounds.Width, size, 600, _options.MaxNodeLabelLines);
        var height = Math.Min(bounds.Height, CaptionLineHeight(NodeCaptionContent(node), size * _scale) * titleLines);
        var fitSingleLine = !_options.WrapNodeLabels && _options.FitContentToViewport
            || MeasurePolicyText("Ag", size * _scale, 600).LineHeight > height;
        var labelColor = string.IsNullOrWhiteSpace(node.LabelColor) ? _colors.Foreground : Color(node.LabelColor, _colors.Foreground);
        Text(node.LabelContent.IsStructured ? NodeCaptionContent(node) : TextContent.Plain(label), new ChartRect(bounds.X, bounds.Y, bounds.Width, height), size, Highlight(labelColor, active), 600, "topology-node-label", titleLines, caption || node.Shape.HasValue, node.Id + "-label", opacity: HighlightFactor(active), paint: string.IsNullOrWhiteSpace(node.LabelColor) ? null : SourcePaint(node.LabelColor, Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground), fitSingleLine: fitSingleLine);
        var y = bounds.Y + height;
        if (!string.IsNullOrWhiteSpace(node.Subtitle) && (!caption || _options.IncludeTileSubtitles)) {
            var subtitleHeight = MeasureRoleText("Ag", RoleTextSize(size * .85, node.SubtitleTextStyle) * _scale, 400, node.SubtitleTextStyle).LineHeight * TextLineCount(node.Subtitle!, bounds.Width, size * .85, 400, _options.MaxNodeSubtitleLines, node.SubtitleTextStyle);
            var subtitle = new ChartRect(bounds.X, y, bounds.Width, Math.Min(Math.Max(0, bounds.Bottom - y), subtitleHeight));
            if (_options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) {
                BuildSubtitleChip(node, EffectiveNodeDisplayMode(node, _options), node.Y + (EffectiveNodeDisplayMode(node, _options) == TopologyNodeDisplayMode.CompactCard ? 31 : CardSubtitleChipOffset(node, _options)), accent, active);
            } else Text(node.Subtitle!, subtitle, size * .85, NodeSubtitleColor(node, active), 400, "topology-node-subtitle", _options.MaxNodeSubtitleLines, caption, paint: NodeSubtitlePaint(node, active), textStyle: node.SubtitleTextStyle);
            y += subtitle.Height;
        }
        if (node.Details.Count > 0) {
            // The canonical card layout reserves this baseline for the first detail. Keep
            // the separator below the complete measured header when typography wraps it.
            var firstBaseline = Point(new ChartPoint(node.X, node.Y + NodeDetailStartOffset(node, _options))).Y;
            y = Math.Max(y + 12 * _scale, firstBaseline - PolicyTextAscent(size * .8 * _scale, 400));
            var separator = y - 6 * _scale;
            var detailLineHeight = MeasurePolicyText("Ag", size * .8 * _scale, 400).LineHeight;
            if (bounds.Width > 0 && separator >= bounds.Top && y + detailLineHeight <= bounds.Bottom) {
                var color = Highlight(_colors.Border, active);
                _builder.Line(bounds.X, separator, bounds.Right, separator, color, _context.Theme.AxisStrokeWidth * _scale,
                    "topology-node-detail-separator", paint: new VisualScenePaintBinding(stroke: BorderPaint(color, HighlightFactor(active))));
            }
        }
        foreach (var detail in node.Details) {
            var rows = TopologyTextLayout.AuthoredRowCount(NodeDetailText(detail), _options.AllowMultilineNodeLabels);
            var rowHeight = size * _scale * 1.4;
            if (DetailContent(detail).IsStructured || detail.TextStyle?.HasOverrides == true) rowHeight = Math.Max(rowHeight, CaptionLineHeight(DetailContent(detail), size * .8 * _scale, detail.TextStyle));
            var row = new ChartRect(bounds.X, y, bounds.Width, Math.Min(Math.Max(0, bounds.Bottom - y), (detail.BadgeStyle == null ? rowHeight * rows : DetailRowHeight(detail, size * .8, rowHeight / _scale) * _scale)));
            var reserve = 0d;
            if (row.Height > 0 && (!string.IsNullOrWhiteSpace(detail.IconId) || detail.Status.HasValue)) {
                reserve = 14 * _scale;
                var color = Highlight(Color(detail.Color, detail.Status.HasValue ? Status(detail.Status.Value) : _colors.Foreground), active);
                if (!string.IsNullOrWhiteSpace(detail.IconId)) {
                    var glyph = new TopologyNode { IconId = detail.IconId, Color = detail.Color, Status = detail.Status ?? TopologyHealthStatus.Unknown };
                    var fallback = detail.Status.HasValue ? Status(detail.Status.Value) : _colors.Foreground;
                    var authored = detail.Color ?? ResolveNodeIcon(glyph, _options)?.Color;
                    var glyphColor = Highlight(Color(authored, fallback), active);
                    BuildGlyph(glyph, (row.X - _offsetX) / _scale + 5, (row.Y - _offsetY) / _scale + row.Height / _scale / 2, glyphColor, .45,
                        !string.IsNullOrWhiteSpace(authored) ? SvgColorRole.Any : detail.Status.HasValue ? SvgColorRole.Status : SvgColorRole.Text,
                        artworkOpacity: HighlightFactor(active), sourceFallback: fallback);
                }
                else using (PinnedState()) _builder.Ellipse(row.X + 5 * _scale, row.Y + row.Height / 2, 2.5 * _scale, 2.5 * _scale, color, role: "topology-detail-status", paint: new VisualScenePaintBinding(SourcePaint(detail.Color ?? (detail.Status.HasValue ? _svgTheme?.StatusColor(detail.Status.Value) : _svgTheme?.Foreground), color, AccentRole(detail.Color), HighlightFactor(active), detail.Status.HasValue ? Status(detail.Status.Value) : _colors.Foreground)));
            }
            BuildDetailBadgeOrText(detail, new ChartRect(row.X + reserve, row.Y, Math.Max(0, row.Width - reserve), row.Height), size * .8, active);
            y += row.Height;
        }
    }

    private double FitNodeTextSize(TopologyNode node, string label, ChartRect bounds, double preferred, bool caption) {
        if (node.LabelContent.IsStructured || node.SubtitleTextStyle?.HasOverrides == true || node.Details.Any(detail => DetailContent(detail).IsStructured || detail.TextStyle?.HasOverrides == true))
            return FitInlineNodeTextSize(node, bounds, preferred, caption);
        return ChartTextFitting.FitFontSize(Math.Min(8, preferred), preferred, size => {
            var height = MeasurePolicyText("Ag", size * _scale, 600).LineHeight
                * TextLineCount(label, bounds.Width, size, 600, _options.MaxNodeLabelLines);
            if (!string.IsNullOrWhiteSpace(node.Subtitle) && (!caption || _options.IncludeTileSubtitles))
                height += MeasurePolicyText("Ag", size * .85 * _scale, 400).LineHeight
                    * TextLineCount(node.Subtitle!, bounds.Width, size * .85, 400, _options.MaxNodeSubtitleLines);
            if (node.Details.Count == 0) return height <= bounds.Height;
            var firstBaseline = Point(new ChartPoint(node.X, node.Y + NodeDetailStartOffset(node, _options))).Y;
            var detailTop = Math.Max(bounds.Top + height + 12 * _scale,
                firstBaseline - PolicyTextAscent(size * .8 * _scale, 400));
            var rowHeight = Math.Max(size * _scale * 1.4, MeasurePolicyText("Ag", size * .8 * _scale, 400).LineHeight);
            return detailTop + NodeDetailRows(node, _options) * rowHeight <= bounds.Bottom;
        });
    }

    private int TextLineCount(string value, double width, double size, int weight, int maximum, TextStyleOverride? textStyle = null) {
        size = RoleTextSize(size, textStyle);
        var paragraphs = _options.AllowMultilineNodeLabels ? value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n') : new[] { value.Replace('\n', ' ') };
        var count = 0;
        foreach (var paragraph in paragraphs) {
            var measured = MeasureRoleText(paragraph, size * _scale, weight, textStyle).Width;
            count += _options.WrapNodeLabels && width > 0 ? Math.Max(1, (int)Math.Ceiling(measured / width)) : 1;
        }
        return Math.Min(Math.Max(1, maximum), count);
    }

    private ChartRect? Text(string value, ChartRect bounds, double size, ChartColor color, int weight, string role, int maxLines = 1, bool centered = false, string? id = null, SvgPaint? paint = null, double opacity = 1, bool fitSingleLine = false, bool verticallyCentered = false, TextStyleOverride? textStyle = null, double defaultUnits = 1) {
        var minimumFontSize = role == "topology-node-label" && !_options.WrapNodeLabels && _options.FitContentToViewport
            ? DotNodeSymbolFontSize * .9 * defaultUnits * _scale
            : DotNodeSymbolFontSize * defaultUnits * _scale;
        size = RoleTextSize(size, textStyle);
        TextMetrics Measure(string text, double candidate, int candidateWeight) => MeasureRoleText(text, candidate, candidateWeight, textStyle);
        var resolvedWeight = textStyle?.HasOverrides == true ? RoleTextStyle(size, weight, textStyle).Font.Weight : weight;
        var layout = TopologyTextLayout.Prepare(value, bounds.Width, bounds.Height, size * _scale, resolvedWeight, maxLines,
            fitSingleLine, minimumFontSize, _options, Measure,
            (code, message) => _builder.AddDiagnostic(new VisualDiagnostic(code, message)));
        if (layout == null) return null;
        size = layout.Size;
        var lineHeight = layout.LineHeight;
        var lines = layout.Lines;
        var textTop = layout.PositionedTop(bounds.Y, bounds.Height, verticallyCentered);
        var baseline = textTop + RoleTextAscent(size, weight, textStyle);
        var halo = role == "topology-endpoint-label" || role == "topology-edge-label-text" && IsMonitoringDashboardStyle(_options) && !_options.IncludeEdgeLabelBackplates;
        var haloColor = ChartColorMath.WithOpacity(_colors.Background, opacity);
        foreach (var line in lines) {
            if (line.Length == 0) { baseline += lineHeight; continue; }
            var alignment = centered ? TextAlignment.Center : TextAlignment.Left;
            var strokeWidth = halo ? (role == "topology-endpoint-label" ? 3 * defaultUnits * _scale : ChartTextHalo.SvgStrokeWidth(size / (_scale * defaultUnits), weight >= 600) * defaultUnits * _scale) : 0;
            if ((_textPolicy == null || !_textPolicy.HasOverrides) && textStyle?.HasOverrides != true)
                _builder.Text(line, centered ? bounds.X + bounds.Width / 2 : bounds.X, baseline, size, color, weight, role, id,
                    alignment, paint ?? ForegroundPaint(color, opacity), stroke: halo ? haloColor : null,
                    strokeWidth: strokeWidth, strokePaint: halo ? BackgroundPaint(haloColor, opacity) : null);
            else {
                var style = textStyle?.HasOverrides == true ? RoleTextStyle(size, weight, textStyle) : TextPolicyStyle(size, weight); style.Color = color; style.Alignment = alignment;
                _builder.Text(line, centered ? bounds.X + bounds.Width / 2 : bounds.X, baseline, style, role, id,
                    paint: paint ?? ForegroundPaint(color, opacity), stroke: halo ? haloColor : null,
                    strokeWidth: strokeWidth, strokePaint: halo ? BackgroundPaint(haloColor, opacity) : null);
            }
            baseline += lineHeight;
        }
        if (lines.Count == 0) return null;
        return new ChartRect(centered ? bounds.X + (bounds.Width - layout.Width) / 2 : bounds.X, textTop, layout.Width, layout.Height);
    }

    private List<string> FitTextLines(string value, double width, double size, int weight, int maxLines, bool fitSingleLine, bool report, out bool overflow, TextStyleOverride? textStyle = null) {
        if (textStyle?.FontSize is double authored) size = authored * _scale;
        TextMetrics Measure(string text, double candidate, int candidateWeight) => MeasureRoleText(text, candidate, candidateWeight, textStyle);
        Action<string, string>? diagnostic = report ? (code, message) => _builder.AddDiagnostic(new VisualDiagnostic(code, message)) : null;
        return TopologyTextLayout.FitLines(value, width, size, weight, maxLines, fitSingleLine, _options, Measure, diagnostic, out overflow);
    }

    private ChartPath Transform(ChartPath path, Func<ChartPoint, ChartPoint>? projection = null) => path.Transform(projection ?? Point);

    private void BuildGlyph(TopologyNode node, double x, double y, ChartColor color, double glyphScale = 1, SvgColorRole glyphRole = SvgColorRole.Any,
        double? artworkOpacity = null, bool allowArtwork = true, ChartColor? sourceFallback = null, ChartPoint? canonicalAnchor = null) {
        glyphScale *= node.GlyphScale ?? 1;
        var anchor = canonicalAnchor ?? new ChartPoint(x, y);
        ChartPoint Project(ChartPoint p) => Point(new ChartPoint(x + (p.X - anchor.X) * glyphScale, y + (p.Y - anchor.Y) * glyphScale));
        if (allowArtwork && ResolveRenderableNodeArtwork(node, _options) != null &&
            BuildArtwork(node, Bounds(x - 13 * glyphScale, y - 13 * glyphScale, 26 * glyphScale, 26 * glyphScale), selectedOutline: false,
                opacity: artworkOpacity, fallbackColor: color)) return;
        var icon = ResolveNodeIcon(node, _options);
        if (_options.RequireResolvedIcons && !string.IsNullOrWhiteSpace(node.IconId) && icon == null && !HasExplicitIconShape(node.IconShape) && ResolveRenderableNodeArtwork(node, _options) == null) throw new InvalidOperationException("Unresolved topology icon: " + node.IconId);
        var kind = icon?.NodeKind ?? node.Kind;
        var resolved = new TopologyNode { Kind = kind, IconId = node.IconId, IconShape = node.IconShape };
        var shape = EffectiveIconShape(resolved, _options);
        var source = GlyphAccentSource(node);
        var authored = TopologyNodeAccent.Authored(node, icon, source);
        var opacity = artworkOpacity ?? HighlightFactor(_highlight.IsNodeHighlighted(node));
        var fallback = sourceFallback ?? Status(node.Status);
        var glyphPaint = TopologyNodeAccent.UsesPalette(node, source) && string.IsNullOrWhiteSpace(authored)
            ? PalettePaint(node.PaletteIndex.GetValueOrDefault(), color, opacity)
            : SourcePaint(authored ?? (glyphRole == SvgColorRole.Text ? _svgTheme?.Foreground : glyphRole == SvgColorRole.Series ? _svgTheme?.Accent : _svgTheme?.StatusColor(node.Status)), color, glyphRole, opacity, fallback);
        var baseColor = SvgPaint.TryCssVariable(authored, fallback, out var variableFallback, out _) ? variableFallback : Color(authored, color);
        BuildGlyphSurface(node, shape, x, y, color, glyphScale, glyphRole, glyphPaint, baseColor,
            string.IsNullOrWhiteSpace(authored) ? 1 : opacity, fallback, canonicalAnchor);
        if (shape == TopologyIconShape.Cloud || shape == TopologyIconShape.Database || (node.IconShape is null or TopologyIconShape.Auto) && kind == TopologyNodeKind.Database) return;
        var marks = TopologyInfrastructureGlyphs.Build(shape, kind, anchor.X, anchor.Y);
        if (marks == null) { Text(NodeGlyph(node, _options), Bounds(x - 12 * glyphScale, y - 10 * glyphScale, 24 * glyphScale, 22 * glyphScale), TextPolicySize(_context.Theme.Typography.DataLabelSize) * glyphScale / (canonicalAnchor.HasValue && node.TextStyle?.FontSize.HasValue == true ? TopologyPreparedNodeBasis.Units(node) : 1), color, 600, "topology-node-icon", centered: true, paint: glyphPaint, fitSingleLine: true); return; }
        foreach (var mark in marks) {
            if (mark.PathData != null) {
                // Decode canonical authored glyph geometry, never an exported chart or SVG document.
                // A canonical glyph mark may contain disconnected contours with the same paint.
                // Keep them in one native path per closure policy instead of repeating SVG paint
                // and role attributes for every tiny line. MoveTo preserves the separation.
                foreach (var contours in ChartMapPathParser.ParseSubpaths(mark.PathData, 2).GroupBy(contour => contour.IsClosed)) {
                    var commands = contours.SelectMany(contour => contour.Points.Select((point, index) => index == 0
                        ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y))).ToArray();
                    _builder.Path(Transform(new ChartPath(commands), Project), mark.FillNone ? null : color, mark.Filled ? null : color,
                        mark.StrokeWidth * _scale * glyphScale, role: "topology-node-icon", close: contours.Key,
                        cap: mark.RoundCap ? VisualStrokeCap.Round : VisualStrokeCap.Butt,
                        join: mark.RoundJoin ? VisualStrokeJoin.Round : VisualStrokeJoin.Miter,
                        paint: new VisualScenePaintBinding(glyphPaint, glyphPaint));
                }
            } else { var p = Project(new ChartPoint(mark.Cx, mark.Cy)); _builder.Ellipse(p.X, p.Y, mark.Rx * _scale * glyphScale, mark.Ry * _scale * glyphScale, mark.Filled ? color : null, mark.Filled ? null : color, mark.StrokeWidth * _scale * glyphScale, "topology-node-icon", paint: new VisualScenePaintBinding(glyphPaint, glyphPaint)); }
        }
    }

    private IDisposable? Host(string? href, string? tooltip, string id, string role) {
        var safe = SafeHref(href);
        if (safe != null) return _builder.PushLink(safe, id + "-host", role, _options.IncludeTooltips ? tooltip : null);
        return _options.IncludeTooltips && !string.IsNullOrWhiteSpace(tooltip) ? _builder.PushTooltip(tooltip!, id + "-host", role) : null;
    }

    private IDisposable? PinnedState() => _options.PinStateColorsInForcedColors
        ? _builder.PushGroup(null, null, new Dictionary<string, string> { ["data-cfx-pin-state-colors"] = "true" }) : null;
}
