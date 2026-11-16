using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologySiblingCardText;

namespace ChartForgeX.Topology;

// A sibling group card: a header with the shared icon, title and member count, then one compact row per member.
internal sealed partial class VisualTopologyCompiler {
    private void SiblingCardMetadata(Dictionary<string, string> data, TopologyNode node) {
        if (node.SiblingGroup is not { } card) return;
        data["data-sibling-count"] = card.Members.Count.ToString(CultureInfo.InvariantCulture);
        data["data-sibling-member-ids"] = string.Join(" ", card.Members.Select(member => member.Id));
        data["data-sibling-parent-id"] = card.ParentId;
        data["data-sibling-collapsed-height"] = Number(TopologySiblingCard.HeaderHeight * _scale);
        data["data-sibling-expanded-height"] = Number(card.ExpandedHeight * _scale);
        data["data-sibling-expanded"] = card.Collapsed ? "false" : "true";
    }

    private void BuildSiblingCard(TopologyNode node, TopologySiblingCard card, ChartColor accent, bool active, SvgColorRole accentRole) {
        var count = card.Members.Count.ToString(CultureInfo.InvariantCulture);
        var countSize = ChipSize + 1;
        var countWidth = MeasureRoleText(count, countSize * _scale, 700, null).Width + 12 * _scale;
        var right = node.X + node.Width - (_options.IncludeStatusBadges && node.ShowStatusBadge ? 22 : 10);
        var toggle = _options.EnableHtmlInteractions;
        if (toggle) right -= 16;
        using (_builder.PushGroup(node.Id + "-sibling-header", "topology-sibling-header", new Dictionary<string, string> { ["data-node-id"] = node.Id })) {
            BuildGlyph(node, node.X + 18, node.Y + TopologySiblingCard.HeaderHeight / 2, accent, .72, accentRole);
            var pill = new ChartRect(_offsetX + right * _scale - countWidth, _offsetY + (node.Y + 10) * _scale, countWidth, 16 * _scale);
            _builder.Rect(pill, accent, radius: pill.Height / 2, role: "topology-sibling-count-surface", paint: new VisualScenePaintBinding(NodeAccentPaint(node, accent, active)));
            var ink = ChartColorMath.AccessibleTextOnBackground(accent);
            Text(count, pill, countSize, ink, 700, "topology-sibling-count", centered: true, verticallyCentered: true, paint: SvgPaint.Contrast(accent, NodeAccentPaint(node, accent, active)));
            var titleLeft = node.X + 34;
            Text(node.Label, Bounds(titleLeft, node.Y + 4, Math.Max(0, (pill.X - _offsetX) / _scale - 6 - titleLeft), TopologySiblingCard.HeaderHeight - 8),
                TitleSize, Highlight(Color(node.LabelColor, _colors.Foreground), active), 700, "topology-sibling-title", verticallyCentered: true, fitSingleLine: true);
            if (toggle) {
                // A chevron for the browser host only. Static output has no control, so it is not drawn there.
                var cx = right + 9; var cy = node.Y + 18;
                var a = Point(new ChartPoint(cx - 3.5, cy - 1.5)); var b = Point(new ChartPoint(cx, cy + 2)); var c = Point(new ChartPoint(cx + 3.5, cy - 1.5));
                var chevron = new ChartPath(new[] { ChartPathCommand.MoveTo(a.X, a.Y), ChartPathCommand.LineTo(b.X, b.Y), ChartPathCommand.LineTo(c.X, c.Y) });
                var muted = Highlight(_colors.MutedForeground, active);
                _builder.Path(chevron, stroke: muted, strokeWidth: 1.6 * _scale, role: "topology-sibling-toggle", paint: new VisualScenePaintBinding(stroke: ForegroundPaint(muted)));
            }
        }
        // A card that starts collapsed keeps its rows in the document for the browser to reveal, but paints none of them.
        using var rows = _builder.PushGroup(node.Id + "-sibling-rows", "topology-sibling-rows", new Dictionary<string, string> { ["data-node-id"] = node.Id }, opacity: card.Collapsed ? 0 : 1);
        var divider = Highlight(_colors.Border, active);
        var top = node.Y + TopologySiblingCard.HeaderHeight;
        var from = Point(new ChartPoint(node.X + 8, top - 2)); var to = Point(new ChartPoint(node.X + node.Width - 8, top - 2));
        _builder.Line(from.X, from.Y, to.X, to.Y, divider, _context.Theme.AxisStrokeWidth * _scale, "topology-sibling-divider", paint: new VisualScenePaintBinding(stroke: BorderPaint(divider)));
        for (var index = 0; index < card.VisibleCount; index++) BuildSiblingRow(node, card.Members[index], index, top + index * TopologySiblingCard.RowHeight, active);
        if (card.VisibleCount < card.Members.Count) {
            var more = (card.Members.Count - card.VisibleCount).ToString(CultureInfo.InvariantCulture);
            var muted = Highlight(_colors.MutedForeground, active);
            Text("+" + more + " more", Bounds(node.X + RowTextLeft, top + card.VisibleCount * TopologySiblingCard.RowHeight, node.Width - RowTextLeft - 10, TopologySiblingCard.RowHeight),
                SecondarySize, muted, 400, "topology-sibling-more", verticallyCentered: true);
        }
    }

    private void BuildSiblingRow(TopologyNode card, TopologySiblingMember member, int index, double top, bool active) {
        var height = TopologySiblingCard.RowHeight;
        // A member's own link and tooltip survive grouping on its row.
        using var link = Host(member.Href, member.Tooltip, card.Id + "-sibling-link-" + index.ToString(CultureInfo.InvariantCulture), "topology-sibling-row-host");
        using var row = _builder.PushGroup(card.Id + "-sibling-" + index.ToString(CultureInfo.InvariantCulture), "topology-sibling-row", new Dictionary<string, string> {
            ["data-node-id"] = member.Id, ["data-node-label"] = member.Label, ["data-node-subtitle"] = member.Subtitle ?? string.Empty,
            ["data-cfx-status"] = member.Status.ToString(), ["data-sibling-card-id"] = card.Id
        });
        var tone = Highlight(Color(member.Color, member.Status == TopologyHealthStatus.Unknown ? _colors.MutedForeground : Status(member.Status)), active);
        BuildGlyph(new TopologyNode { Kind = member.Kind, IconId = member.IconId, Symbol = member.Symbol }, card.X + 18, top + height / 2, tone, .5, SvgColorRole.Status, allowArtwork: false);
        var right = card.X + card.Width - 10;
        if (_options.IncludeStatusBadges) {
            var dot = Point(new ChartPoint(right - 3, top + height / 2));
            var status = Highlight(Status(member.Status), active);
            _builder.Ellipse(dot.X, dot.Y, 3 * _scale, 3 * _scale, status, role: "topology-sibling-status", paint: new VisualScenePaintBinding(StatusPaint(member.Status, status, HighlightFactor(active))));
            right -= 12;
        }
        if (!string.IsNullOrWhiteSpace(member.Badge)) {
            var width = MeasureRoleText(member.Badge!, ChipSize * _scale, 600, null).Width / _scale + 10;
            var chip = Bounds(right - width, top + 4, width, height - 8);
            var fill = Highlight(ChartColorMath.Blend(_colors.Surface, _colors.Foreground, .08), active);
            _builder.Rect(chip, fill, radius: chip.Height / 2, role: "topology-sibling-chip-surface", paint: new VisualScenePaintBinding(SvgPaint.Mix(fill, CardPaint(_colors.Surface), ForegroundPaint(_colors.Foreground), .08)));
            Text(member.Badge!, chip, ChipSize, Highlight(_colors.Foreground, active), 600, "topology-sibling-chip", centered: true, verticallyCentered: true, fitSingleLine: true);
            right -= width + 6;
        }
        var left = card.X + RowTextLeft;
        var available = Math.Max(0, right - left);
        var name = Text(member.Label, Bounds(left, top, available, height), NameSize, Highlight(_colors.Foreground, active), 600, "topology-sibling-name", verticallyCentered: true, fitSingleLine: true);
        if (name is { } drawn && !string.IsNullOrWhiteSpace(member.Subtitle)) {
            var after = (drawn.Right - _offsetX) / _scale + 6;
            if (right - after > 18) Text(member.Subtitle!, Bounds(after, top, right - after, height), SecondarySize, Highlight(_colors.MutedForeground, active), 400, "topology-sibling-secondary", verticallyCentered: true);
        }
    }
}
