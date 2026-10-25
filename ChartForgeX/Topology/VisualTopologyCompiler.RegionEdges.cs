using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildRegionEdge(TopologyEdge edge) {
        var points = _routes[edge]; var tone = edge.Tone == TopologyTone.Automatic && edge.IsOverlay ? TopologyTone.Info : edge.Tone;
        var color = Color(edge.Color, RegionToneColor(tone, edge.Status));
        var metadata = new Dictionary<string, string> {
            ["class"] = "cfx-edge " + RegionTone(tone, edge.Status) + (edge.LineStyle == TopologyEdgeLineStyle.Dashed ? " dashed" : ""),
            ["data-edge-id"] = edge.Id, ["data-source-node-id"] = edge.SourceNodeId, ["data-target-node-id"] = edge.TargetNodeId,
            ["data-a"] = edge.SourceNodeId, ["data-b"] = edge.TargetNodeId, ["data-edge-label"] = edge.Label ?? "",
            ["data-cfx-status"] = edge.Status.ToString(), ["data-direction"] = edge.Direction.ToString(),
            ["data-region-tip"] = string.Join(" · ", new[] { edge.Label }.Concat(edge.Metadata.Where(pair => !pair.Key.StartsWith("chartforgex.", StringComparison.Ordinal)).Select(pair => pair.Key + ": " + pair.Value)).Where(value => !string.IsNullOrWhiteSpace(value)))
        };
        metadata = RegionRelationshipMetadata(edge, metadata);
        var dash = _options.RegionPrint ? RegionTone(tone, edge.Status) switch { "tone-warn" => new[] { 8d, 6d }, "tone-bad" => new[] { 2d, 4d }, _ => null }
            : edge.LineStyle == TopologyEdgeLineStyle.Dashed ? new[] { 8d, 6d } : edge.LineStyle == TopologyEdgeLineStyle.Dotted ? new[] { 2d, 4d } : null;
        var width = edge.StrokeWidth ?? (edge.IsOverlay ? 2 : 3);
        using (_regionPainter.PushTooltip(metadata["data-region-tip"]))
        using (_regionPainter.PushGroup(edge.Id, "topology-edge", metadata)) {
            var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(points[0].X, points[0].Y) };
            foreach (var point in points.Skip(1)) commands.Add(ChartPathCommand.LineTo(point.X, point.Y));
            _regionPainter.Path(new ChartPath(commands.ToArray()), stroke: color, strokeWidth: width, dash: dash);
            if (edge.Direction == VisualLinkDirection.Forward || edge.Direction == VisualLinkDirection.Bidirectional) RegionArrow(points[points.Count - 2], points[points.Count - 1], color);
            if (edge.Direction == VisualLinkDirection.Bidirectional) RegionArrow(points[1], points[0], color);
        }
        if (!string.IsNullOrWhiteSpace(edge.Label)) BuildRegionEdgeLabel(edge, color);
    }
    private void RegionArrow(ChartPoint a, ChartPoint b, ChartColor color) {
        var length = Math.Max(1, TopologyRegionLayout.Distance(a.X, a.Y, b.X, b.Y));
        var ux = (b.X - a.X) / length; var uy = (b.Y - a.Y) / length;
        _regionPainter.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(b.X, b.Y),
            ChartPathCommand.LineTo(b.X - ux * 7 - uy * 3.5, b.Y - uy * 7 + ux * 3.5),
            ChartPathCommand.LineTo(b.X - ux * 7 + uy * 3.5, b.Y - uy * 7 - ux * 3.5) }), color, close: true);
    }
    private Dictionary<string, string> RegionRelationshipMetadata(TopologyEdge edge, Dictionary<string, string> attributes) {
        foreach (var pair in CommonMetadata(edge.Id, "edge", edge.Status, TopologyScenarioStepKind.Edge, edge.Metadata, edge.Metrics)) attributes[pair.Key] = pair.Value;
        attributes["data-source-group-id"] = _nodesById[edge.SourceNodeId].GroupId ?? "";
        attributes["data-target-group-id"] = _nodesById[edge.TargetNodeId].GroupId ?? "";
        attributes["data-edge-kind"] = edge.Kind.ToString(); attributes["data-direction"] = edge.Direction.ToString();
        return attributes;
    }
    private void BuildRegionEdgeLabel(TopologyEdge edge, ChartColor color) {
        if (!_options.IncludeEdgeLabels) return;
        var points = _routes[edge]; var a = points[0]; var b = points[points.Count - 1];
        var length = Math.Max(1, TopologyRegionLayout.Distance(a.X, a.Y, b.X, b.Y));
        var textWidth = _regionPainter.MeasureText(edge.Label!, 11.5, 500).Width + 6;
        foreach (var fraction in new[] { .5, .35, .65, .2, .8 }) {
            var lengths = Enumerable.Range(1, points.Count - 1).Select(index => TopologyRegionLayout.Distance(points[index - 1].X, points[index - 1].Y, points[index].X, points[index].Y)).ToArray();
            var remaining = lengths.Sum() * fraction; var segment = 0;
            while (segment < lengths.Length - 1 && remaining > lengths[segment]) remaining -= lengths[segment++];
            var middle = TopologyRegionLayout.Along(points[segment], points[segment + 1], remaining / Math.Max(1, lengths[segment]));
            var x = middle.X + (b.Y - a.Y) / length * 10; var y = middle.Y - (b.X - a.X) / length * 10;
            var bounds = new ChartRect(x - textWidth / 2, y - 12, textWidth, 16);
            if (bounds.X < 10 || bounds.Right > _regionUnitWidth - 10 || _regionLabelBounds.Any(other => RegionOverlap(bounds, other))
                || _chart.Nodes.Any(node => RegionOverlap(bounds, new ChartRect(node.X - 12, node.Y - 18, node.Width + 24, node.Height + 8))
                    || !string.IsNullOrWhiteSpace(node.Subtitle) && RegionOverlap(bounds, new ChartRect(node.X + node.Width / 2 - 70, node.Y + node.Height + 4, 140, 18)))
                || _chart.Groups.Any(group => RegionOverlap(bounds, new ChartRect(group.X, group.Y, group.Width, 60)))) continue;
            var values = RegionValues(); values["x"] = Number(x); values["y"] = Number(y); values["label"] = edge.Label!;
            values["tone"] = RegionTone(edge.Tone == TopologyTone.Automatic && edge.IsOverlay ? TopologyTone.Info : edge.Tone, edge.Status); values["status"] = color.ToCss();
            values["halo"] = _chart.Groups.Any(group => RegionOverlap(bounds, new ChartRect(group.X, group.Y, group.Width, group.Height)))
                ? _colors.Surface.ToCss() : _colors.Background.ToCss();
            TopologyRegionTemplate.Draw(_regionPainter, "edge-label", values, new Dictionary<string, string> {
                ["data-a"] = edge.SourceNodeId, ["data-b"] = edge.TargetNodeId
            });
            _regionLabelBounds.Add(bounds); _resolvedLabelBounds[edge] = bounds; return;
        }
        // The full label remains in the edge tooltip and accessible Details when no clear slot exists.
    }
    private static bool RegionOverlap(ChartRect a, ChartRect b) => a.X < b.Right && a.Right > b.X && a.Y < b.Bottom && a.Bottom > b.Y;
    private void BuildRegionLinks(TopologyGroup group) {
        var edges = _regionLayout.ExternalLinks[group.Id]; if (edges.Count == 0) return;
        var y = group.Y + group.Height - edges.Count * 30 - 12;
        _regionPainter.Line(group.X + 20, y - 24, group.X + group.Width - 20, y - 24, _colors.Border);
        _regionPainter.Text("Links to other regions", group.X + 20, y - 8, 12, _colors.MutedForeground, 600);
        foreach (var edge in edges) {
            var source = _nodesById[edge.SourceNodeId]; var target = _nodesById[edge.TargetNodeId];
            var other = source.GroupId == group.Id ? target : source;
            var label = other.Label + (string.IsNullOrWhiteSpace(edge.Label) ? "" : " · " + edge.Label);
            using (_regionPainter.PushGroup(edge.Id + "-" + group.Id, "topology-edge", RegionRelationshipMetadata(edge, new Dictionary<string, string> {
                ["class"] = "cfx-edge " + RegionTone(edge.Tone, edge.Status), ["data-edge-id"] = edge.Id,
                ["data-source-node-id"] = edge.SourceNodeId, ["data-target-node-id"] = edge.TargetNodeId,
                ["data-region-tip"] = source.Label + " → " + target.Label + " · " + edge.Label, ["data-edge-label"] = edge.Label ?? ""
            }))) {
                _regionPainter.Ellipse(group.X + 24, y + 7, 4, 4, Color(edge.Color, RegionToneColor(edge.Tone, edge.Status)));
                _regionPainter.Text(RegionFit(label, group.Width - 64, 12), group.X + 36, y + 11, 12, _colors.Foreground);
            }
            y += 30;
        }
    }
    private void BuildRegionChip(TopologyEdge edge) {
        var points = _routes[edge]; var y = points[0].Y;
        var direction = edge.Direction == VisualLinkDirection.Bidirectional ? " ⇄ " : edge.Direction == VisualLinkDirection.Forward ? " → " : " — ";
        var label = _nodesById[edge.SourceNodeId].Label + direction + _nodesById[edge.TargetNodeId].Label + " · " + edge.Label;
        var tone = edge.Tone == TopologyTone.Automatic ? TopologyTone.Info : edge.Tone;
        var width = Math.Min(_regionUnitWidth - 40, _regionPainter.MeasureText(label, 12).Width + 24);
        using (_regionPainter.PushGroup(edge.Id, "topology-edge", RegionRelationshipMetadata(edge, new Dictionary<string, string> {
            ["class"] = "cfx-edge " + RegionTone(tone, edge.Status), ["data-edge-id"] = edge.Id, ["data-edge-label"] = edge.Label ?? "",
            ["data-source-node-id"] = edge.SourceNodeId, ["data-target-node-id"] = edge.TargetNodeId, ["data-region-tip"] = label
        }))) {
            _regionPainter.Rect(new ChartRect(20, y - 12, width, 28), _colors.ElevatedSurface, _colors.Border, radius: 14);
            _regionPainter.Text(RegionFit(label, width - 24, 12), 32, y + 6, 12, Color(edge.Color, RegionToneColor(tone, edge.Status)));
        }
    }
    private IReadOnlyList<(string Label, TopologyHealthStatus? Status)> RegionLegendItems() {
        TopologyHealthStatus? LegendStatus(TopologyTone tone, TopologyHealthStatus status) => tone switch {
            TopologyTone.Automatic => status, TopologyTone.Good => TopologyHealthStatus.Healthy, TopologyTone.Warn => TopologyHealthStatus.Warning,
            TopologyTone.Bad => TopologyHealthStatus.Critical, TopologyTone.Neutral => TopologyHealthStatus.Unknown, _ => null
        };
        var states = _chart.Nodes.Select(node => LegendStatus(node.Tone, node.Status))
            .Concat(_chart.Edges.Select(edge => LegendStatus(edge.Tone == TopologyTone.Automatic && edge.IsOverlay ? TopologyTone.Info : edge.Tone, edge.Status)));
        var used = new HashSet<TopologyHealthStatus>(states.Where(state => state.HasValue).Select(state => state!.Value));
        var entries = (_legend?.Items.Where(item => item.Status.HasValue && used.Contains(item.Status.Value))
            .Select(item => (item.Label, item.Status)) ?? Enumerable.Empty<(string Label, TopologyHealthStatus? Status)>()).ToList();
        foreach (var status in used.OrderBy(status => status)) if (!entries.Any(entry => entry.Status == status)) entries.Add((status.ToString(), status));
        if (_chart.Nodes.Any(node => node.Tone == TopologyTone.Info) || _chart.Edges.Any(edge => edge.Tone == TopologyTone.Info || edge.IsOverlay && edge.Tone == TopologyTone.Automatic))
            entries.Add((_legend?.Items.FirstOrDefault(item => item.Status == null && item.EdgeKind.HasValue
                && _chart.Edges.Any(edge => edge.Kind == item.EdgeKind.Value && (edge.Tone == TopologyTone.Info || edge.IsOverlay && edge.Tone == TopologyTone.Automatic)))?.Label
                ?? (_chart.Edges.Any(edge => edge.IsOverlay) ? "Shared relationship" : "Informational"), null));
        entries.AddRange(_chart.Nodes.SelectMany(node => node.Badges).Select(badge => (badge.Meaning, (TopologyHealthStatus?)null)).Distinct());
        if (_chart.Nodes.Count > 0 && !string.IsNullOrWhiteSpace(_options.RegionRingLegendLabel)) entries.Add((_options.RegionRingLegendLabel, null));
        return entries;
    }
    private int RegionLegendRows(double width) {
        var rows = 1; var x = 20d;
        foreach (var item in RegionLegendItems()) {
            var size = Math.Min(width - 40, _regionPainter.MeasureText(item.Label, 12.5).Width + 52);
            if (x > 20 && x + size > width - 20) { x = 20; rows++; }
            x += size;
        }
        return rows;
    }
    private void BuildRegionLegend() {
        var x = 20d; var y = _regionLayout.Bottom + 36;
        foreach (var item in RegionLegendItems()) {
            var width = Math.Min(_regionUnitWidth - 40, _regionPainter.MeasureText(item.Label, 12.5).Width + 52);
            if (x > 20 && x + width > _regionUnitWidth - 20) { x = 20; y += 28; }
            using (_regionPainter.PushGroup(null, "topology-legend", new Dictionary<string, string> { ["class"] = "cfx-legend" })) {
                if (item.Status.HasValue) _regionPainter.Line(x, y - 4, x + 22, y - 4, Status(item.Status.Value), 3,
                    dash: item.Status.Value == TopologyHealthStatus.Warning ? new[] { 8d, 6d } : _options.RegionPrint && item.Status.Value == TopologyHealthStatus.Critical ? new[] { 2d, 4d } : null);
                else {
                    var badge = _chart.Nodes.SelectMany(node => node.Badges).FirstOrDefault(badge => badge.Meaning == item.Label);
                    if (badge != null) {
                        var badgeValues = RegionValues(); badgeValues["badge-tone"] = (badge.Informational ? _colors.Info : _colors.Warning).ToCss();
                        using (_regionPainter.PushTranslation(x + 4, y - 10)) TopologyRegionTemplate.Draw(_regionPainter, TopologyRegionTemplate.BadgeSymbol(badge.Glyph), badgeValues);
                    } else if (item.Label == _options.RegionRingLegendLabel)
                        _regionPainter.Ellipse(x + 11, y - 4, 6, 6, _colors.ElevatedSurface, _colors.MutedForeground, 3);
                    else _regionPainter.Line(x, y - 4, x + 22, y - 4, _colors.Info, 2);
                }
                var values = RegionValues(); values["x"] = Number(x + 30); values["y"] = Number(y);
                values["label"] = RegionFit(item.Label, _regionUnitWidth - 90, 12.5);
                TopologyRegionTemplate.Draw(_regionPainter, "legend-item", values);
            }
            x += width;
        }
    }
}
