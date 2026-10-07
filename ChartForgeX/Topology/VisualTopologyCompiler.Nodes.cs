using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
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
        var accent = Highlight(Color(node.Color ?? icon?.Color, Status(node.Status)), active);
        var fill = Highlight(Color(NodeFill(node, Theme(), accent.ToCss(), _options), _colors.Surface), active);
        var strokeWidth = (_options.SelectedNodeIds.Contains(node.Id) ? 2.8 : _context.Theme.AxisStrokeWidth) * _scale;
        using (Host(node.Href, node.Tooltip, node.Id, "topology-node-host"))
        using (_builder.PushGroup(node.Id, "topology-node", new Dictionary<string, string> { ["data-status"] = node.Status.ToString(), ["data-kind"] = node.Kind.ToString(), ["data-display"] = mode.ToString() })) {
            var image = mode == TopologyNodeDisplayMode.Artwork && BuildArtwork(node, bounds);
            if (!image) {
                if (mode == TopologyNodeDisplayMode.Dot) _builder.Ellipse(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, bounds.Width / 2, bounds.Height / 2, accent, role: "topology-node-surface");
                else if (node.Shape.HasValue) {
                    foreach (var part in TopologyNodeShapeGeometry.SceneParts(node)) _builder.Path(Transform(part.Path), part.Fill ? fill : null, accent, strokeWidth, role: "topology-node-surface", close: part.Close);
                } else _builder.Rect(bounds, fill, accent, strokeWidth, mode == TopologyNodeDisplayMode.Pill ? bounds.Height / 2 : _context.Theme.BarRadius * _scale, role: "topology-node-surface");
                if (UseNodeAccentBand(mode, _options)) _builder.Rect(new ChartRect(bounds.X + strokeWidth / 2, bounds.Y + strokeWidth / 2, 4 * _scale, Math.Max(0, bounds.Height - strokeWidth)), accent, role: "topology-node-accent");
                var hasGlyph = node.Kind != TopologyNodeKind.Generic || node.Symbol != null || node.IconId != null || mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Artwork;
                if (hasGlyph && mode != TopologyNodeDisplayMode.Dot) {
                    var center = mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Artwork;
                    BuildGlyph(node, center ? node.X + node.Width / 2 : node.X + 22, node.Y + (center ? node.Height / 2 : Math.Min(node.Height / 2, 26)), accent);
                }
                if (_options.IncludeNodeLabels && (mode != TopologyNodeDisplayMode.Icon || _options.IncludeIconLabels)) {
                    var caption = mode is TopologyNodeDisplayMode.Tile or TopologyNodeDisplayMode.Artwork or TopologyNodeDisplayMode.Icon;
                    var left = hasGlyph && !caption && mode != TopologyNodeDisplayMode.Pill ? 44 : 10;
                    var textBounds = caption ? Bounds(node.X - 17, node.Y + node.Height + 5, node.Width + 34, (_options.MaxNodeLabelLines + 1) * 18) : Bounds(node.X + left, node.Y + 6, Math.Max(0, node.Width - left - 12), Math.Max(0, node.Height - 12));
                    if (mode != TopologyNodeDisplayMode.Dot) BuildNodeText(node, textBounds, accent, active, caption);
                }
            }
            if (_options.IncludeStatusBadges && node.ShowStatusBadge && mode != TopologyNodeDisplayMode.Dot) {
                using (PinnedState()) _builder.Ellipse(bounds.Right - 10 * _scale, bounds.Y + 10 * _scale, 3 * _scale, 3 * _scale, Highlight(Status(node.Status), active), role: "topology-status");
            }
            if (!string.IsNullOrWhiteSpace(node.Badge)) {
                var badge = Bounds(node.X + node.Width - 42, node.Y + node.Height - 18, 38, 16);
                _builder.Rect(badge, accent, radius: 4 * _scale, role: "topology-node-badge-surface");
                Text(node.Badge!, badge, _context.Theme.Typography.DataLabelSize * .75, _colors.Surface, 600, "topology-node-badge", 1);
            }
        }
        _builder.AddRegion(new VisualSemanticRegion(node.Id, "topology-node", bounds, node.Label));
    }

    private void BuildNodeText(TopologyNode node, ChartRect bounds, ChartColor accent, bool active, bool caption) {
        var size = _context.Theme.Typography.DataLabelSize;
        var label = node.MaximumLabelCharacters.HasValue ? TrimTo(node.Label, node.MaximumLabelCharacters.Value) : node.Label;
        var titleLines = TextLineCount(label, bounds.Width, size, 600, _options.MaxNodeLabelLines);
        var height = Math.Min(bounds.Height, size * _scale * 1.3 * titleLines);
        Text(label, new ChartRect(bounds.X, bounds.Y, bounds.Width, height), size, Highlight(_colors.Foreground, active), 600, "topology-node-label", titleLines, caption || node.Shape.HasValue, node.Id + "-label");
        var y = bounds.Y + height;
        if (!string.IsNullOrEmpty(node.Subtitle) && (!caption || _options.IncludeTileSubtitles)) {
            var subtitleHeight = size * _scale * 1.2 * TextLineCount(node.Subtitle!, bounds.Width, size * .85, 400, _options.MaxNodeSubtitleLines);
            var subtitle = new ChartRect(bounds.X, y, bounds.Width, Math.Min(Math.Max(0, bounds.Bottom - y), subtitleHeight));
            if (_options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) _builder.Rect(subtitle, accent.WithOpacity(.1), radius: _context.Theme.BarRadius * _scale, role: "topology-subtitle-chip");
            Text(node.Subtitle!, subtitle, size * .85, Highlight(_colors.MutedForeground, active), 400, "topology-node-subtitle", _options.MaxNodeSubtitleLines, caption);
            y += subtitle.Height;
        }
        foreach (var detail in node.Details) {
            var row = new ChartRect(bounds.X, y, bounds.Width, Math.Min(Math.Max(0, bounds.Bottom - y), size * _scale * 1.4));
            var reserve = 0d;
            if (row.Height > 0 && (!string.IsNullOrWhiteSpace(detail.IconId) || detail.Status.HasValue)) {
                reserve = 14 * _scale;
                var color = Highlight(Color(detail.Color, detail.Status.HasValue ? Status(detail.Status.Value) : _colors.Foreground), active);
                if (!string.IsNullOrWhiteSpace(detail.IconId)) BuildGlyph(new TopologyNode { IconId = detail.IconId },
                    (row.X - _offsetX) / _scale + 5, (row.Y - _offsetY) / _scale + row.Height / _scale / 2, color, .45);
                else using (PinnedState()) _builder.Ellipse(row.X + 5 * _scale, row.Y + row.Height / 2, 2.5 * _scale, 2.5 * _scale, color, role: "topology-detail-status");
            }
            Text(detail.Text ?? (detail.Label + " " + detail.Value), new ChartRect(row.X + reserve, row.Y, Math.Max(0, row.Width - reserve), row.Height), size * .8,
                Highlight(Color(detail.Color, _colors.Foreground), active), 400, "topology-node-detail", 1);
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

    private ChartRect? Text(string value, ChartRect bounds, double size, ChartColor color, int weight, string role, int maxLines = 1, bool centered = false, string? id = null) {
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
        foreach (var line in lines) { _builder.Text(line, centered ? bounds.X + bounds.Width / 2 : bounds.X, baseline, size, color, weight, role, id, centered ? TextAlignment.Center : TextAlignment.Left); baseline += lineHeight; }
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

    private void BuildGlyph(TopologyNode node, double x, double y, ChartColor color, double glyphScale = 1) {
        ChartPoint Project(ChartPoint p) => Point(new ChartPoint(x + (p.X - x) * glyphScale, y + (p.Y - y) * glyphScale));
        var icon = ResolveNodeIcon(node, _options);
        if (_options.RequireResolvedIcons && !string.IsNullOrWhiteSpace(node.IconId) && icon == null) throw new InvalidOperationException("Unresolved topology icon: " + node.IconId);
        var kind = icon?.NodeKind ?? node.Kind;
        var resolved = new TopologyNode { Kind = kind, IconId = node.IconId };
        var marks = string.IsNullOrWhiteSpace(node.Symbol) ? TopologyInfrastructureGlyphs.Build(EffectiveIconShape(resolved, _options), kind, x, y) : null;
        if (marks == null) { Text(NodeGlyph(node, _options), Bounds(x - 12 * glyphScale, y - 10 * glyphScale, 24 * glyphScale, 22 * glyphScale), _context.Theme.Typography.DataLabelSize * glyphScale, color, 600, "topology-node-icon", centered: true); return; }
        foreach (var mark in marks) {
            if (mark.PathData != null) {
                // Decode canonical authored glyph geometry, never an exported chart or SVG document.
                foreach (var contour in ChartMapPathParser.ParseSubpaths(mark.PathData, 2)) {
                    var commands = contour.Points.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToArray();
                    _builder.Path(Transform(new ChartPath(commands), Project), mark.FillNone ? null : color, mark.Filled ? null : color, mark.StrokeWidth * _scale * glyphScale, role: "topology-node-icon", close: contour.IsClosed);
                }
            } else { var p = Project(new ChartPoint(mark.Cx, mark.Cy)); _builder.Ellipse(p.X, p.Y, mark.Rx * _scale * glyphScale, mark.Ry * _scale * glyphScale, mark.Filled ? color : null, mark.Filled ? null : color, mark.StrokeWidth * _scale * glyphScale, "topology-node-icon"); }
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
        if (!string.IsNullOrWhiteSpace(href)) return _builder.PushLink(href!, id + "-host", role, _options.IncludeTooltips ? tooltip : null);
        return _options.IncludeTooltips && !string.IsNullOrWhiteSpace(tooltip) ? _builder.PushTooltip(tooltip!, id + "-host", role) : null;
    }

    private IDisposable? PinnedState() => _options.PinStateColorsInForcedColors
        ? _builder.PushGroup(null, null, new Dictionary<string, string> { ["data-cfx-pin-state-colors"] = "true" }) : null;
}
