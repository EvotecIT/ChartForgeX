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
        var mode = EffectiveNodeDisplayMode(node, _options);
        if (mode == TopologyNodeDisplayMode.Hidden) return;
        var bounds = Bounds(node.X, node.Y, node.Width, node.Height);
        var active = _highlight.IsNodeHighlighted(node);
        var icon = ResolveNodeIcon(node, _options);
        if (_options.RequireResolvedIcons && node.IconId != null && icon == null) throw new InvalidOperationException("Unresolved topology icon: " + node.IconId);
        var authoredAccent = node.Color ?? icon?.Color;
        var accentRole = AccentRole(authoredAccent);
        var baseAccent = Color(authoredAccent, Status(node.Status));
        var accent = Highlight(baseAccent, active);
        var fill = Highlight(Color(NodeFill(node, Theme(), baseAccent.ToHexRgba(), _options), _colors.Surface), active);
        var surfacePaint = new VisualScenePaintBinding(NodeFillPaint(node, fill, baseAccent, accentRole), SvgPaint.Of(accent, accentRole));
        var strokeWidth = (_options.SelectedNodeIds.Contains(node.Id) ? 2.8 : _context.Theme.AxisStrokeWidth) * _scale;
        using (Host(node.Href, node.Tooltip, node.Id, "topology-node-host"))
        using (_builder.PushGroup(node.Id, "topology-node", NodeMetadata(node))) {
            var image = mode == TopologyNodeDisplayMode.Artwork && BuildArtwork(node, bounds);
            if (!image) {
                if (mode == TopologyNodeDisplayMode.Dot) {
                    using (PinnedState()) _builder.Ellipse(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, bounds.Width / 2, bounds.Height / 2, accent, role: "topology-node-surface", paint: Paint(accent, accentRole));
                    if (!string.IsNullOrWhiteSpace(node.Symbol)) Text(node.Symbol!, bounds, Math.Min(_context.Theme.Typography.DataLabelSize, node.Height * .65), _colors.Surface, 700, "topology-node-symbol", centered: true, paint: SvgPaint.Of(_colors.Surface, SvgColorRole.Surface));
                }
                else if (node.Shape.HasValue) {
                    foreach (var part in TopologyNodeShapeGeometry.SceneParts(node)) _builder.Path(Transform(part.Path), part.Fill ? fill : null, accent, strokeWidth, role: "topology-node-surface", close: part.Close, paint: surfacePaint);
                } else _builder.Rect(bounds, fill, accent, strokeWidth, mode == TopologyNodeDisplayMode.Pill ? bounds.Height / 2 : _context.Theme.BarRadius * _scale, role: "topology-node-surface", paint: surfacePaint);
                if (UseNodeAccentBand(mode, _options)) _builder.Rect(new ChartRect(bounds.X + strokeWidth / 2, bounds.Y + strokeWidth / 2, 4 * _scale, Math.Max(0, bounds.Height - strokeWidth)), accent, role: "topology-node-accent", paint: Paint(accent, accentRole));
                var hasGlyph = node.Kind != TopologyNodeKind.Generic || node.Symbol != null || node.IconId != null || mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Artwork;
                if (hasGlyph && mode != TopologyNodeDisplayMode.Dot) {
                    var center = mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Artwork;
                    BuildGlyph(node, center ? node.X + node.Width / 2 : node.X + 22, node.Y + (mode == TopologyNodeDisplayMode.Card && _options.IncludeNodeLabels && node.Details.Count > 0 ? 28 : node.Height / 2), accent, glyphRole: accentRole);
                }
                if (_options.IncludeNodeLabels && (mode != TopologyNodeDisplayMode.Icon || _options.IncludeIconLabels)) {
                    var caption = mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Artwork or TopologyNodeDisplayMode.Icon;
                    var left = hasGlyph && !caption && mode != TopologyNodeDisplayMode.Pill ? 44 : 10;
                    var textBounds = caption ? Bounds(node.X - 17, node.Y + node.Height + 5, node.Width + 34, (_options.MaxNodeLabelLines + 1) * 18) : Bounds(node.X + left, node.Y + 6, Math.Max(0, node.Width - left - 12), Math.Max(0, node.Height - 12));
                    if (mode == TopologyNodeDisplayMode.Tile) BuildTileCaption(node, accent, active);
                    else if (mode == TopologyNodeDisplayMode.Icon) BuildIconCaption(node, accent, active);
                    else if (node.Shape == TopologyNodeShape.Actor) BuildDiagramCaption(node, active);
                    else if (mode != TopologyNodeDisplayMode.Dot) BuildNodeText(node, textBounds, accent, active, caption);
                }
            }
            if (_options.IncludeStatusBadges && node.ShowStatusBadge && mode != TopologyNodeDisplayMode.Dot) {
                var status = Highlight(Status(node.Status), active);
                using var statusGroup = _builder.PushGroup(node.Id + "-status", "topology-node-status", new Dictionary<string, string> { ["data-node-id"] = node.Id });
                using (PinnedState()) _builder.Ellipse(bounds.Right - 10 * _scale, bounds.Y + 10 * _scale, 3 * _scale, 3 * _scale, status, role: "topology-status", paint: Paint(status, SvgColorRole.Status));
            }
            if (!string.IsNullOrWhiteSpace(node.Badge)) {
                var badge = Bounds(node.X + node.Width - 42, node.Y + node.Height - 18, 38, 16);
                _builder.Rect(badge, accent, radius: 4 * _scale, role: "topology-node-badge-surface", paint: Paint(accent, accentRole));
                Text(node.Badge!, badge, _context.Theme.Typography.DataLabelSize * .75, _colors.Surface, 600, "topology-node-badge", 1, paint: SvgPaint.Of(_colors.Surface, SvgColorRole.Surface));
            }
        }
        _builder.AddRegion(new VisualSemanticRegion(node.Id, "topology-node", bounds, node.Label));
    }

    private void BuildNodeText(TopologyNode node, ChartRect bounds, ChartColor accent, bool active, bool caption) {
        var size = _context.Theme.Typography.DataLabelSize;
        var label = node.MaximumLabelCharacters.HasValue ? TrimTo(node.Label, node.MaximumLabelCharacters.Value) : node.Label;
        var titleLines = TextLineCount(label, bounds.Width, size, 600, _options.MaxNodeLabelLines);
        var height = Math.Min(bounds.Height, _builder.MeasureText("Ag", size * _scale, 600).LineHeight * titleLines);
        Text(label, new ChartRect(bounds.X, bounds.Y, bounds.Width, height), size, Highlight(_colors.Foreground, active), 600, "topology-node-label", titleLines, caption || node.Shape.HasValue, node.Id + "-label");
        var y = bounds.Y + height;
        if (!string.IsNullOrEmpty(node.Subtitle) && (!caption || _options.IncludeTileSubtitles)) {
            var subtitleHeight = _builder.MeasureText("Ag", size * .85 * _scale, 400).LineHeight * TextLineCount(node.Subtitle!, bounds.Width, size * .85, 400, _options.MaxNodeSubtitleLines);
            var subtitle = new ChartRect(bounds.X, y, bounds.Width, Math.Min(Math.Max(0, bounds.Bottom - y), subtitleHeight));
            if (_options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) {
                BuildSubtitleChip(node, EffectiveNodeDisplayMode(node, _options), node.Y + (EffectiveNodeDisplayMode(node, _options) == TopologyNodeDisplayMode.CompactCard ? 31 : CardSubtitleChipOffset(node, _options)), accent, active);
            } else Text(node.Subtitle!, subtitle, size * .85, Highlight(_colors.MutedForeground, active), 400, "topology-node-subtitle", _options.MaxNodeSubtitleLines, caption);
            y += subtitle.Height;
        }
        if (node.Details.Count > 0) {
            // The canonical card layout reserves this baseline for the first detail. Keep
            // the separator below the complete measured header when typography wraps it.
            var firstBaseline = Point(new ChartPoint(node.X, node.Y + NodeDetailStartOffset(node, _options))).Y;
            y = Math.Max(y + 12 * _scale, firstBaseline - _builder.TextAscent(size * .8 * _scale, 400));
            var separator = y - 6 * _scale;
            var detailLineHeight = _builder.MeasureText("Ag", size * .8 * _scale, 400).LineHeight;
            if (bounds.Width > 0 && separator >= bounds.Top && y + detailLineHeight <= bounds.Bottom) {
                var color = Highlight(_colors.Border, active);
                _builder.Line(bounds.X, separator, bounds.Right, separator, color, _context.Theme.AxisStrokeWidth * _scale,
                    "topology-node-detail-separator", paint: Paint(stroke: color, strokeRole: SvgColorRole.Surface));
            }
        }
        foreach (var detail in node.Details) {
            var row = new ChartRect(bounds.X, y, bounds.Width, Math.Min(Math.Max(0, bounds.Bottom - y), size * _scale * 1.4));
            var reserve = 0d;
            if (row.Height > 0 && (!string.IsNullOrWhiteSpace(detail.IconId) || detail.Status.HasValue)) {
                reserve = 14 * _scale;
                var color = Highlight(Color(detail.Color, detail.Status.HasValue ? Status(detail.Status.Value) : _colors.Foreground), active);
                if (!string.IsNullOrWhiteSpace(detail.IconId)) BuildGlyph(new TopologyNode { IconId = detail.IconId },
                    (row.X - _offsetX) / _scale + 5, (row.Y - _offsetY) / _scale + row.Height / _scale / 2, color, .45, !string.IsNullOrWhiteSpace(detail.Color) ? SvgColorRole.Any : detail.Status.HasValue ? SvgColorRole.Status : SvgColorRole.Text);
                else using (PinnedState()) _builder.Ellipse(row.X + 5 * _scale, row.Y + row.Height / 2, 2.5 * _scale, 2.5 * _scale, color, role: "topology-detail-status", paint: Paint(color, AccentRole(detail.Color)));
            }
            Text(detail.Text ?? (detail.Label + " " + detail.Value), new ChartRect(row.X + reserve, row.Y, Math.Max(0, row.Width - reserve), row.Height), size * .8,
                Highlight(Color(detail.Color, _colors.Foreground), active), 400, "topology-node-detail", 1,
                paint: SvgPaint.Of(Highlight(Color(detail.Color, _colors.Foreground), active), string.IsNullOrWhiteSpace(detail.Color) ? SvgColorRole.Text : SvgColorRole.Any));
            y += row.Height;
        }
    }

    private int TextLineCount(string value, double width, double size, int weight, int maximum) {
        var paragraphs = _options.AllowMultilineNodeLabels ? value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n') : new[] { value.Replace('\n', ' ') };
        var count = 0;
        foreach (var paragraph in paragraphs) {
            var measured = _builder.MeasureText(paragraph, size * _scale, weight).Width;
            count += _options.WrapNodeLabels && width > 0 ? Math.Max(1, (int)Math.Ceiling(measured / width)) : 1;
        }
        return Math.Min(Math.Max(1, maximum), count);
    }

    private ChartRect? Text(string value, ChartRect bounds, double size, ChartColor color, int weight, string role, int maxLines = 1, bool centered = false, string? id = null, SvgPaint? paint = null, double opacity = 1) {
        if (string.IsNullOrEmpty(value) || bounds.Width <= 0 || bounds.Height <= 0) return null;
        size *= _scale;
        var lineHeight = _builder.MeasureText("Ag", size, weight).LineHeight;
        maxLines = Math.Min(Math.Max(1, maxLines), (int)Math.Floor(bounds.Height / lineHeight));
        if (maxLines < 1) {
            _builder.AddDiagnostic(new VisualDiagnostic("topology.label-height", "A topology label has no complete text line in its allocated bounds; full source text remains in semantic interchange."));
            return null;
        }
        var source = value.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!_options.AllowMultilineNodeLabels) source = source.Replace('\n', ' ');
        var queue = new Queue<string>(source.Split('\n'));
        var lines = new List<string>();
        while (queue.Count > 0 && lines.Count < maxLines) {
            var text = queue.Dequeue();
            if (_builder.MeasureText(text, size, weight).Width <= bounds.Width) { lines.Add(text); continue; }
            var elements = StringInfo.ParseCombiningCharacters(text); var count = elements.Length;
            while (count > 0 && _builder.MeasureText(text.Substring(0, count == elements.Length ? text.Length : elements[count]) + "…", size, weight).Width > bounds.Width) count--;
            var length = count == 0 ? 0 : count == elements.Length ? text.Length : elements[count];
            if (_options.WrapNodeLabels && lines.Count + 1 < maxLines && length > 0) {
                var separator = text.LastIndexOf(' ', length - 1, length); if (separator > 0) length = separator;
                lines.Add(text.Substring(0, length));
                queue = new Queue<string>(new[] { text.Substring(length).TrimStart() }.Concat(queue));
            } else { lines.Add(length == 0 ? string.Empty : text.Substring(0, length) + "…"); _builder.AddDiagnostic(new VisualDiagnostic("topology.label-truncated", "A topology label was shortened; complete source text remains in semantic interchange.")); }
        }
        if (queue.Count > 0) _builder.AddDiagnostic(new VisualDiagnostic("topology.label-lines", "A topology label exceeds its configured line count; complete source text remains in semantic interchange."));
        var baseline = bounds.Y + _builder.TextAscent(size, weight);
        var halo = role == "topology-endpoint-label" || role == "topology-edge-label-text" && IsMonitoringDashboardStyle(_options) && !_options.IncludeEdgeLabelBackplates;
        var haloColor = ChartColorMath.WithOpacity(_colors.Background, opacity);
        foreach (var line in lines) {
            if (line.Length == 0) { baseline += lineHeight; continue; }
            _builder.Text(line, centered ? bounds.X + bounds.Width / 2 : bounds.X, baseline, size, color, weight, role, id,
                centered ? TextAlignment.Center : TextAlignment.Left, paint ?? SvgPaint.Of(color, SvgColorRole.Text),
                stroke: halo ? haloColor : null,
                strokeWidth: halo ? (role == "topology-endpoint-label" ? 3 * _scale : ChartTextHalo.SvgStrokeWidth(size / _scale, weight >= 600) * _scale) : 0,
                strokePaint: halo ? SvgPaint.Of(haloColor, SvgColorRole.Surface) : null);
            baseline += lineHeight;
        }
        if (lines.Count == 0) return null;
        var width = lines.Max(line => _builder.MeasureText(line, size, weight).Width);
        return new ChartRect(centered ? bounds.X + (bounds.Width - width) / 2 : bounds.X, bounds.Y, width, lineHeight * lines.Count);
    }

    private ChartPath Transform(ChartPath path, Func<ChartPoint, ChartPoint>? projection = null) => new(path.Commands.Select(command => {
        var map = projection ?? Point;
        var p = map(new ChartPoint(command.X, command.Y));
        if (command.Kind == ChartPathCommandKind.MoveTo) return ChartPathCommand.MoveTo(p.X, p.Y);
        if (command.Kind == ChartPathCommandKind.LineTo) return ChartPathCommand.LineTo(p.X, p.Y);
        var a = map(new ChartPoint(command.Control1X, command.Control1Y)); var b = map(new ChartPoint(command.Control2X, command.Control2Y));
        return ChartPathCommand.CubicTo(a.X, a.Y, b.X, b.Y, p.X, p.Y);
    }).ToArray());

    private void BuildGlyph(TopologyNode node, double x, double y, ChartColor color, double glyphScale = 1, SvgColorRole glyphRole = SvgColorRole.Any) {
        ChartPoint Project(ChartPoint p) => Point(new ChartPoint(x + (p.X - x) * glyphScale, y + (p.Y - y) * glyphScale));
        if (ResolveRenderableNodeArtwork(node, _options) != null &&
            BuildArtwork(node, Bounds(x - 13 * glyphScale, y - 13 * glyphScale, 26 * glyphScale, 26 * glyphScale))) return;
        var icon = ResolveNodeIcon(node, _options);
        if (_options.RequireResolvedIcons && !string.IsNullOrWhiteSpace(node.IconId) && icon == null) throw new InvalidOperationException("Unresolved topology icon: " + node.IconId);
        var kind = icon?.NodeKind ?? node.Kind;
        var resolved = new TopologyNode { Kind = kind, IconId = node.IconId };
        var shape = EffectiveIconShape(resolved, _options);
        BuildGlyphSurface(node, shape, x, y, color, glyphScale, glyphRole);
        if (shape == TopologyIconShape.Cloud || shape == TopologyIconShape.Database || kind == TopologyNodeKind.Database) return;
        var marks = TopologyInfrastructureGlyphs.Build(shape, kind, x, y);
        if (marks == null) { Text(NodeGlyph(node, _options), Bounds(x - 12 * glyphScale, y - 10 * glyphScale, 24 * glyphScale, 22 * glyphScale), _context.Theme.Typography.DataLabelSize * glyphScale, color, 600, "topology-node-icon", centered: true, paint: SvgPaint.Of(color, glyphRole)); return; }
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
                        paint: Paint(color, glyphRole, color, glyphRole));
                }
            } else { var p = Project(new ChartPoint(mark.Cx, mark.Cy)); _builder.Ellipse(p.X, p.Y, mark.Rx * _scale * glyphScale, mark.Ry * _scale * glyphScale, mark.Filled ? color : null, mark.Filled ? null : color, mark.StrokeWidth * _scale * glyphScale, "topology-node-icon", paint: Paint(color, glyphRole, color, glyphRole)); }
        }
    }

    private bool BuildArtwork(TopologyNode node, ChartRect bounds) {
        var artwork = ResolveRenderableNodeArtwork(node, _options);
        if (artwork?.ImageHref is string href && href.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)) {
            var separator = href.IndexOf(',');
            if (separator > 0 && href.Substring(0, separator).EndsWith(";base64", StringComparison.OrdinalIgnoreCase)) {
                try {
                    if (RasterImageDecoder.TryDecode(Convert.FromBase64String(href.Substring(separator + 1)), out var image)) {
                        _builder.Image(image, bounds, "topology-node-artwork"); return true;
                    }
                } catch (FormatException) { }
            }
        }
        if (artwork?.HasSvgBody == true) {
            // Artwork sampling is bounded by its visible destination and the shared raster budget.
            var factor = Math.Min(2, 2048d / Math.Max(bounds.Width, bounds.Height));
            var width = Math.Max(1, (int)Math.Ceiling(bounds.Width * factor)); var height = Math.Max(1, (int)Math.Ceiling(bounds.Height * factor));
            if (SvgRasterRenderer.TryRenderFragment(artwork.SvgBody!, artwork.SvgViewBox, artwork.PreserveAspectRatio, width, height, out var pixels)) {
                _builder.Image(new RgbaImage(width, height, pixels), bounds, "topology-node-artwork"); return true;
            }
        }
        _builder.AddDiagnostic(new VisualDiagnostic("topology.artwork-fallback", "Artwork could not be resolved into the immutable scene; the canonical node glyph is used."));
        return false;
    }

    private IDisposable? Host(string? href, string? tooltip, string id, string role) {
        var safe = SafeHref(href);
        if (safe != null) return _builder.PushLink(safe, id + "-host", role, _options.IncludeTooltips ? tooltip : null);
        return _options.IncludeTooltips && !string.IsNullOrWhiteSpace(tooltip) ? _builder.PushTooltip(tooltip!, id + "-host", role) : null;
    }

    private IDisposable? PinnedState() => _options.PinStateColorsInForcedColors
        ? _builder.PushGroup(null, null, new Dictionary<string, string> { ["data-cfx-pin-state-colors"] = "true" }) : null;
}
