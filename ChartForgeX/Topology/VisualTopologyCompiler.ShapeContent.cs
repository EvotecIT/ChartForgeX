using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private sealed class ShapeContentLayout {
        internal ChartRect Bounds;
        internal double Size, TitleHeight, SubtitleHeight, DetailHeight, FooterHeight, FooterWidth, GlyphWidth;
        internal int TitleRows, SubtitleRows;
        internal bool Complete;
        internal double BodyHeight => Math.Max(0, Bounds.Height - FooterHeight - (FooterHeight > 0 ? 4 : 0));
    }

    private string ShapeTitle(TopologyNode node) => node.MaximumLabelCharacters.HasValue ? TrimTo(node.Label, node.MaximumLabelCharacters.Value) : node.Label;

    private ShapeContentLayout ShapeContent(TopologyNode node, double size) {
        var title = ShapeTitle(node);
        var showText = _options.IncludeNodeLabels;
        var subtitle = showText && !string.IsNullOrWhiteSpace(node.Subtitle);
        var chip = _options.CardSubtitleMode == TopologyCardSubtitleMode.Chip;
        var titleLine = showText ? _builder.MeasureText("Ag", size * _scale, 600).LineHeight / _scale : 0;
        var subtitleLine = subtitle ? _builder.MeasureText("Ag", size * .85 * _scale, chip ? 700 : 400).LineHeight / _scale : 0;
        var detailLine = Math.Max(size * .8 * 1.4, _builder.MeasureText("Ag", size * .8 * _scale, 400).LineHeight / _scale);
        var badge = !string.IsNullOrWhiteSpace(node.Badge);
        var status = _options.IncludeStatusBadges && node.ShowStatusBadge;
        var footer = badge ? 18d : status ? 6 : 0;
        var footerWidth = (badge ? NodeBadgeWidth(node, _options) : 0) + (status ? badge ? 10 : 6 : 0);
        var glyph = HasInlineNodeGlyph(node, EffectiveNodeDisplayMode(node, _options), _options) ? 32d : 0;
        ShapeContentLayout? best = null; var retained = -1;
        var maximumTitle = showText ? Math.Max(1, _options.MaxNodeLabelLines) : 1;
        var maximumSubtitle = subtitle && !chip ? Math.Max(1, _options.MaxNodeSubtitleLines) : 1;
        for (var titleRows = 1; titleRows <= maximumTitle; titleRows++) for (var subtitleRows = 1; subtitleRows <= maximumSubtitle; subtitleRows++) {
            var titleHeight = titleRows * titleLine;
            var subtitleHeight = subtitle ? subtitleRows * subtitleLine + (chip ? 4 : 0) : 0;
            var detailHeight = showText && node.Details.Count > 0 ? 6 + node.Details.Count * detailLine : 0;
            var bodyHeight = Math.Max(titleHeight + subtitleHeight + detailHeight, glyph > 0 ? 26 : 0);
            var totalHeight = bodyHeight + footer + (footer > 0 ? 4 : 0);
            var bounds = TopologyNodeShapeGeometry.CaptionBounds(node, totalHeight + 1e-8);
            var width = Math.Max(0, bounds.Width - glyph);
            var count = 0;
            var complete = bounds.Height + 1e-8 >= totalHeight && bounds.Width >= footerWidth && width > 0;
            if (showText) {
                var lines = FitTextLines(title, width * _scale, size * _scale, 600, titleRows, false, false, out var overflow);
                count += lines.Sum(line => line.Length); complete &= !overflow;
                if (subtitle) {
                    lines = FitTextLines(node.Subtitle!, Math.Max(0, width - (chip ? 12 : 0)) * _scale, size * .85 * _scale,
                        chip ? 700 : 400, subtitleRows, false, false, out overflow);
                    count += lines.Sum(line => line.Length); complete &= !overflow;
                }
                foreach (var detail in node.Details) {
                    var reserve = !string.IsNullOrWhiteSpace(detail.IconId) || detail.Status.HasValue ? 14 : 0;
                    lines = FitTextLines(detail.Text ?? detail.Label + " " + detail.Value, Math.Max(0, width - reserve) * _scale,
                        size * .8 * _scale, 400, 1, false, false, out overflow);
                    count += lines.Sum(line => line.Length); complete &= !overflow;
                }
            }
            var candidate = new ShapeContentLayout { Bounds = bounds, Size = size, TitleHeight = titleHeight,
                SubtitleHeight = subtitleHeight, DetailHeight = detailLine, FooterHeight = Math.Min(footer, bounds.Height),
                FooterWidth = footerWidth, GlyphWidth = glyph, TitleRows = titleRows, SubtitleRows = subtitleRows, Complete = complete };
            if (complete) return candidate;
            if (best == null || count > retained) { best = candidate; retained = count; }
        }
        return best!;
    }

    /// <summary>Grows minimum dimensions before layout and routing, using the painter's complete content contract.</summary>
    private void GrowShapeContent(TopologyNode node) {
        var size = _context.Theme.Typography.DataLabelSize;
        var glyph = HasInlineNodeGlyph(node, EffectiveNodeDisplayMode(node, _options), _options) ? 32d : 0;
        double Width(string text, double fontSize, int weight) {
            var source = text.Replace("\r\n", "\n").Replace('\r', '\n');
            if (!_options.AllowMultilineNodeLabels) source = source.Replace('\n', ' ');
            return source.Split('\n').Max(line => _builder.MeasureText(line, fontSize, weight).Width);
        }
        var width = _options.IncludeNodeLabels ? Width(ShapeTitle(node), size, 600) + glyph : glyph;
        if (_options.IncludeNodeLabels && !string.IsNullOrWhiteSpace(node.Subtitle)) width = Math.Max(width,
            Width(node.Subtitle!, size * .85, _options.CardSubtitleMode == TopologyCardSubtitleMode.Chip ? 700 : 400)
                + glyph + (_options.CardSubtitleMode == TopologyCardSubtitleMode.Chip ? 12 : 0));
        if (_options.IncludeNodeLabels) foreach (var detail in node.Details) width = Math.Max(width,
            Width(detail.Text ?? detail.Label + " " + detail.Value, size * .8, 400) + glyph
                + (!string.IsNullOrWhiteSpace(detail.IconId) || detail.Status.HasValue ? 14 : 0));
        if (!string.IsNullOrWhiteSpace(node.Badge)) width = Math.Max(width, NodeBadgeWidth(node, _options)
            + (_options.IncludeStatusBadges && node.ShowStatusBadge ? 10 : 0));
        node.Width = Math.Max(node.Width, width + 20);
        for (var attempt = 0; attempt < 32; attempt++) {
            if (node.Shape is TopologyNodeShape.Circle or TopologyNodeShape.DoubleCircle)
                node.Width = node.Height = Math.Max(node.Width, node.Height);
            var content = ShapeContent(node, size);
            if (content.Complete) return;
            // Explicit paragraph/line ceilings remain authoritative. Once every source
            // paragraph fits its cell width, extra node size cannot restore forbidden rows.
            var titleRows = Math.Min(Math.Max(1, _options.MaxNodeLabelLines), _options.AllowMultilineNodeLabels
                ? ShapeTitle(node).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length : 1);
            var subtitleRows = Math.Min(Math.Max(1, _options.MaxNodeSubtitleLines), _options.AllowMultilineNodeLabels
                ? (node.Subtitle ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Length : 1);
            var intendedHeight = (_options.IncludeNodeLabels ? titleRows * _builder.MeasureText("Ag", size, 600).LineHeight : 0)
                + (_options.IncludeNodeLabels && !string.IsNullOrWhiteSpace(node.Subtitle)
                    ? subtitleRows * _builder.MeasureText("Ag", size * .85, _options.CardSubtitleMode == TopologyCardSubtitleMode.Chip ? 700 : 400).LineHeight
                        + (_options.CardSubtitleMode == TopologyCardSubtitleMode.Chip ? 4 : 0) : 0)
                + (_options.IncludeNodeLabels && node.Details.Count > 0 ? 6 + node.Details.Count * content.DetailHeight : 0);
            var footerHeight = !string.IsNullOrWhiteSpace(node.Badge) ? 18 : _options.IncludeStatusBadges && node.ShowStatusBadge ? 6 : 0;
            intendedHeight = Math.Max(intendedHeight, glyph > 0 ? 26 : 0) + footerHeight + (footerHeight > 0 ? 4 : 0);
            var intended = TopologyNodeShapeGeometry.CaptionBounds(node, intendedHeight);
            if (intended.Width + 1e-8 >= width && intended.Height + 1e-8 >= intendedHeight) return;
            // Growing both axes retains a useful aspect ratio for diamonds and curved surfaces.
            node.Width *= 1.1; node.Height *= 1.1;
        }
    }

    private void BuildShapeContent(TopologyNode node, ChartColor accent, bool active, SvgColorRole glyphRole) {
        var preferred = _context.Theme.Typography.DataLabelSize;
        var size = ChartTextFitting.FitFontSize(Math.Min(11, preferred), preferred,
            candidate => ShapeContent(node, candidate).Complete);
        var layout = ShapeContent(node, size);
        if (!layout.Complete) _builder.AddDiagnostic(new VisualDiagnostic("topology.shape-content-overflow",
            "A fixed topology shape cannot contain every complete content row; full source text remains in semantic interchange. Enable FitLabelToContent or enlarge the node."));
        var area = layout.Bounds;
        var glyphWidth = Math.Min(layout.GlyphWidth, area.Width);
        var text = new ChartRect(area.X + glyphWidth, area.Y, Math.Max(0, area.Width - glyphWidth), layout.BodyHeight);
        if (glyphWidth > 0 && layout.BodyHeight > 0) {
            var glyphScale = Math.Min(1, Math.Min(glyphWidth / 32, layout.BodyHeight / 26));
            BuildGlyph(node, area.X + glyphWidth / 2, area.Y + layout.BodyHeight / 2, accent, glyphScale, glyphRole);
        }
        var y = text.Top;
        if (_options.IncludeNodeLabels) {
            var labelColor = string.IsNullOrWhiteSpace(node.LabelColor) ? _colors.Foreground : Color(node.LabelColor, _colors.Foreground);
            var title = Row(layout.TitleHeight);
            Text(ShapeTitle(node), Project(title), size, Highlight(labelColor, active), 600, "topology-node-label", layout.TitleRows,
                centered: true, id: node.Id + "-label", opacity: HighlightFactor(active),
                paint: string.IsNullOrWhiteSpace(node.LabelColor) ? null : SourcePaint(node.LabelColor,
                    Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground));
            if (!string.IsNullOrWhiteSpace(node.Subtitle)) {
                var subtitle = Row(layout.SubtitleHeight);
                var chip = _options.CardSubtitleMode == TopologyCardSubtitleMode.Chip;
                if (chip && subtitle.Height > 0) {
                    var fill = ChartColorMath.WithOpacity(accent, .1); var stroke = ChartColorMath.WithOpacity(accent, .45);
                    _builder.Rect(Project(subtitle), fill, stroke, _context.Theme.AxisStrokeWidth * _scale, subtitle.Height * _scale / 2,
                        "topology-subtitle-chip", paint: new VisualScenePaintBinding(NodeAccentPaint(node, fill, active, .1), NodeAccentPaint(node, stroke, active, .45)));
                    subtitle = new ChartRect(subtitle.X + Math.Min(6, subtitle.Width / 2), subtitle.Y + Math.Min(2, subtitle.Height / 2),
                        Math.Max(0, subtitle.Width - 12), Math.Max(0, subtitle.Height - 4));
                }
                Text(node.Subtitle!, Project(subtitle), size * .85, Highlight(_colors.MutedForeground, active), chip ? 700 : 400,
                    chip ? "topology-subtitle-chip-text" : "topology-node-subtitle", layout.SubtitleRows, centered: true,
                    paint: MutedPaint(Highlight(_colors.MutedForeground, active), HighlightFactor(active)));
            }
            if (node.Details.Count > 0) y = Math.Min(text.Bottom, y + 6);
            foreach (var detail in node.Details) {
                var row = Row(layout.DetailHeight);
                var reserve = !string.IsNullOrWhiteSpace(detail.IconId) || detail.Status.HasValue ? Math.Min(14, row.Width) : 0;
                var color = Highlight(Color(detail.Color, detail.Status.HasValue ? Status(detail.Status.Value) : _colors.Foreground), active);
                if (reserve > 0 && row.Height > 0) {
                    var factor = Math.Min(1, Math.Min(row.Height / 14, reserve / 14));
                    if (!string.IsNullOrWhiteSpace(detail.IconId)) {
                        var glyph = new TopologyNode { IconId = detail.IconId, Color = detail.Color, Status = detail.Status ?? TopologyHealthStatus.Unknown };
                        var fallback = detail.Status.HasValue ? Status(detail.Status.Value) : _colors.Foreground;
                        var authored = detail.Color ?? ResolveNodeIcon(glyph, _options)?.Color;
                        BuildGlyph(glyph, row.X + reserve / 2, row.Y + row.Height / 2, Highlight(Color(authored, fallback), active),
                            .4 * factor, !string.IsNullOrWhiteSpace(authored) ? SvgColorRole.Any : detail.Status.HasValue ? SvgColorRole.Status : SvgColorRole.Text,
                            artworkOpacity: HighlightFactor(active), sourceFallback: fallback);
                    }
                    else using (PinnedState()) {
                        var center = Point(new ChartPoint(row.X + reserve / 2, row.Y + row.Height / 2));
                        _builder.Ellipse(center.X, center.Y, 2.5 * factor * _scale, 2.5 * factor * _scale, color, role: "topology-detail-status",
                            paint: new VisualScenePaintBinding(SourcePaint(detail.Color ?? (detail.Status.HasValue ? _svgTheme?.StatusColor(detail.Status.Value) : _svgTheme?.Foreground),
                                color, AccentRole(detail.Color), HighlightFactor(active), detail.Status.HasValue ? Status(detail.Status.Value) : _colors.Foreground)));
                    }
                }
                BuildDetailText(detail, Project(new ChartRect(row.X + reserve, row.Y,
                    Math.Max(0, row.Width - reserve), row.Height)), size * .8, active);
            }
        }
        var footer = new ChartRect(area.X, area.Bottom - layout.FooterHeight, area.Width, layout.FooterHeight);
        var status = _options.IncludeStatusBadges && node.ShowStatusBadge;
        var badge = !string.IsNullOrWhiteSpace(node.Badge);
        var footerWidth = Math.Min(area.Width, layout.FooterWidth);
        var left = area.X + (area.Width - footerWidth) / 2;
        if (status && footer.Height >= 6 && footerWidth >= 6) {
            var color = Highlight(Status(node.Status), active);
            var center = Point(new ChartPoint(left + 3, footer.Y + footer.Height / 2));
            using (_builder.PushGroup(node.Id + "-status", "topology-node-status", new Dictionary<string, string> { ["data-node-id"] = node.Id })) using (PinnedState())
                _builder.Ellipse(center.X, center.Y, 3 * _scale, 3 * _scale, color, role: "topology-status",
                    paint: new VisualScenePaintBinding(StatusPaint(node.Status, color, HighlightFactor(active))));
            left += badge ? 10 : 6;
        }
        if (badge && footer.Height > 0 && area.Right > left) {
            var badgeBounds = Project(new ChartRect(left, footer.Y, Math.Max(0, Math.Min(NodeBadgeWidth(node, _options), area.Right - left)), footer.Height));
            var paint = NodeAccentPaint(node, accent, active);
            _builder.Rect(badgeBounds, accent, radius: Math.Min(4 * _scale, badgeBounds.Height / 2), role: "topology-node-badge-surface",
                paint: new VisualScenePaintBinding(paint));
            var ink = ChartColorMath.AccessibleTextOnBackground(accent);
            Text(NodeBadge(node), badgeBounds, preferred * .75, ink, 600, "topology-node-badge", centered: true,
                paint: SvgPaint.Contrast(accent, paint), fitSingleLine: true, verticallyCentered: true);
        }
        ChartRect Row(double height) {
            var result = new ChartRect(text.X, y, text.Width, Math.Min(height, Math.Max(0, text.Bottom - y)));
            y += result.Height; return result;
        }
        ChartRect Project(ChartRect bounds) => Bounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
    }
}
