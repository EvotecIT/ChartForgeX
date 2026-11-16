using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

// Stripes, evidence and value chips: presentation that sits on an already routed edge.
internal sealed partial class VisualTopologyCompiler {
    private void EdgePresentationMetadata(Dictionary<string, string> data, TopologyEdge edge) {
        if (edge.Evidence != TopologyEdgeEvidence.Unspecified) data["data-edge-evidence"] = edge.Evidence.ToString().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(edge.EvidenceText)) data["data-edge-evidence-text"] = edge.EvidenceText!;
        if (edge.Stripes.Count > 1) data["data-edge-stripes"] = string.Join(";", edge.Stripes.Select(stripe => stripe.Color));
        if (edge.UplinkEnd != TopologyEdgeEnd.None) data["data-edge-uplink"] = edge.UplinkEnd.ToString().ToLowerInvariant();
        if (edge.Chips.Count > 0) data["data-edge-chips"] = string.Join(" · ", edge.Chips.Select(chip => chip.Text));
    }

    // Draws one dashed pass per stripe colour. Each pass is shifted by one stripe length, so together they tile the
    // route with repeating bands. A dashed edge (assumed or by status) keeps a short gap after every band.
    private bool DrawStripes(TopologyEdge edge, ChartPath path, double width, double[]? dash, double units, double opacity) {
        if (edge.Stripes.Count < 2) return false;
        var length = EdgeStripeLength * units * _scale;
        var period = length * edge.Stripes.Count;
        var gap = dash is { Length: > 1 } ? Math.Min(length * .45, dash[1]) : 0;
        for (var index = 0; index < edge.Stripes.Count; index++) {
            // The caller's opacity already carries highlight dimming and edge opacity; apply it once.
            var color = Color(edge.Stripes[index].Color, _colors.Border);
            color = color.WithOpacity(color.A / 255d * opacity);
            _builder.Path(path, stroke: color, strokeWidth: width, role: "topology-edge-stripe",
                dash: new[] { length - gap, period - length + gap }, cap: VisualStrokeCap.Butt,
                paint: new VisualScenePaintBinding(stroke: SvgPaint.Of(color, SvgColorRole.Any)),
                dashOffset: index == 0 ? 0 : period - index * length);
        }
        return true;
    }

    // A dashed striped edge keeps its ends attached with a short solid stub in its first band colour.
    private void StripeConnector(TopologyEdge edge, ChartPath connector, double width, double opacity) {
        var color = Color(edge.Stripes[0].Color, _colors.Border);
        color = color.WithOpacity(color.A / 255d * opacity);
        _builder.Path(connector, stroke: color, strokeWidth: width, role: "topology-edge-endpoint-connector", cap: VisualStrokeCap.Butt,
            paint: new VisualScenePaintBinding(stroke: SvgPaint.Of(color, SvgColorRole.Any)));
    }

    private void BuildEdgeChips() {
        var edges = _chart.Edges.Where(edge => edge.Chips.Count > 0 && _paintRoutes.ContainsKey(edge)).OrderBy(edge => _edgeRenderOrders[edge]).ToArray();
        if (edges.Length == 0) return;
        var obstacles = EdgeChipObstacles();
        var segments = _paintRoutes.SelectMany(pair => pair.Value.Select(Point).Zip(pair.Value.Skip(1).Select(Point), (a, b) => (Edge: pair.Key, A: a, B: b))).ToArray();
        var mono = new TextStyleOverride { FontFamily = _context.Theme.Typography.MonospaceFamily };
        foreach (var edge in edges) {
            var units = TopologyPreparedEdgePlan.Units(edge);
            var height = EdgeChipHeight * units * _scale;
            var gap = EdgeChipGap * units * _scale;
            var widths = edge.Chips.Select(chip => EdgeChipWidth(chip, units, mono)).ToArray();
            var route = _paintRoutes[edge].Select(Point).ToArray();
            // Only what lies near this route can collide with its chips: candidates stay within a few rows of the line.
            var reach = widths.Sum() + gap * widths.Length + 6 * (height + gap) * widths.Length;
            var window = new ChartRect(route.Min(point => point.X) - reach, route.Min(point => point.Y) - reach,
                route.Max(point => point.X) - route.Min(point => point.X) + reach * 2, route.Max(point => point.Y) - route.Min(point => point.Y) + reach * 2);
            var near = obstacles.Where(obstacle => OverlapArea(obstacle, window) > 0).ToArray();
            // Runs of other links count against a spot, so a chip names the leg that belongs to its own link.
            var others = segments.Where(segment => !ReferenceEquals(segment.Edge, edge) && SegmentNear(segment.A, segment.B, window))
                .Select(segment => new[] { segment.A, segment.B }).ToArray();
            var boxes = EdgeChipBoxes(route, widths, height, gap, near, _contentClip, others);
            if (boxes == null) {
                // A chip drawn over other ink misleads more than a missing one; the values stay in the edge metadata.
                _builder.AddDiagnostic(new VisualDiagnostic("topology.edge-chips-omitted", "Edge '" + edge.Id + "' has no free place for its value chips; they were not drawn."));
                continue;
            }
            for (var index = 0; index < edge.Chips.Count; index++) {
                BuildEdgeChip(edge, edge.Chips[index], index, boxes[index], units, mono);
                obstacles.Add(boxes[index]);
            }
        }
    }

    private double EdgeChipWidth(TopologyEdgeChip chip, double units, TextStyleOverride mono) {
        var text = MeasureRoleText(chip.Text, EdgeChipFontSize * units * _scale, 600, mono).Width;
        var icon = HasChipIcon(chip) ? (EdgeChipIconSize + 3) * units * _scale : 0;
        // Two extra pixels keep the measured run clear of the fitting tolerance, so the value never ellipsises.
        return Math.Ceiling(text + 2 * _scale) + icon + EdgeChipPadding * 2 * units * _scale;
    }

    private bool HasChipIcon(TopologyEdgeChip chip) => !string.IsNullOrWhiteSpace(chip.IconId) && ResolveNodeIcon(new TopologyNode { IconId = chip.IconId }, _options) != null;

    private void BuildEdgeChip(TopologyEdge edge, TopologyEdgeChip chip, int index, ChartRect box, double units, TextStyleOverride mono) {
        var id = edge.Id + "-chip-" + index.ToString(CultureInfo.InvariantCulture);
        var opacity = EdgeLabelOpacity(edge);
        var tone = Highlight(RegionToneColor(chip.Tone, edge.Status), _highlight.IsEdgeHighlighted(edge));
        using var group = _builder.PushGroup(id, "topology-edge-chip", new Dictionary<string, string> {
            ["data-edge-id"] = edge.Id, ["data-chip-index"] = index.ToString(CultureInfo.InvariantCulture),
            ["data-chip-tone"] = RegionTone(chip.Tone, edge.Status), ["data-chip-text"] = chip.Text,
            ["data-chip-x"] = Number(box.X + box.Width / 2), ["data-chip-y"] = Number(box.Y + box.Height / 2)
        });
        var fill = ChartColorMath.WithOpacity(_colors.Surface, opacity);
        var outline = ChartColorMath.WithOpacity(tone, opacity);
        _builder.Rect(box, fill, outline, _context.Theme.AxisStrokeWidth * units * _scale, box.Height / 2, "topology-edge-chip-surface",
            paint: new VisualScenePaintBinding(CardPaint(fill, opacity), BorderPaint(tone, opacity, SvgColorRole.Status)));
        var left = box.X + EdgeChipPadding * units * _scale;
        if (HasChipIcon(chip)) {
            var size = EdgeChipIconSize * units * _scale;
            var cx = (left + size / 2 - _offsetX) / _scale; var cy = (box.Y + box.Height / 2 - _offsetY) / _scale;
            BuildGlyph(new TopologyNode { IconId = chip.IconId }, cx, cy, tone, EdgeChipIconSize * units / 26, SvgColorRole.Status, allowArtwork: false);
            left += size + 3 * units * _scale;
        }
        var ink = ChartColorMath.WithOpacity(_colors.Foreground, opacity);
        Text(chip.Text, new ChartRect(left, box.Y, Math.Max(0, box.Right - EdgeChipPadding * units * _scale - left), box.Height),
            EdgeChipFontSize * units, ink, 600, "topology-edge-chip-text", verticallyCentered: true, opacity: opacity,
            paint: ForegroundPaint(ink, opacity), textStyle: mono, defaultUnits: units);
        _builder.AddRegion(new VisualSemanticRegion(id, "topology-edge-chip", box, chip.Text));
    }

    // Nodes, endpoint labels and relationship labels are fixed before chips. A chip row slides along its own route to
    // the free position nearest the middle; with no free position it takes the least covered one.
    private List<ChartRect> EdgeChipObstacles() {
        var obstacles = _chart.Nodes.Select(node => Bounds(node.X - 3, node.Y - 3, node.Width + 6, node.Height + 6)).ToList();
        obstacles.AddRange(_edgeLabels.Select(label => Bounds(label.CenterX - label.Width / 2, label.CenterY - label.Height / 2, label.Width, label.Height)));
        if (_options.IncludeEndpointLabels) obstacles.AddRange(_chart.Edges.SelectMany(EndpointLabelBounds));
        if (_options.IncludeGroups && _options.IncludeGroupLabels)
            obstacles.AddRange(_chart.Groups.SelectMany(group => TopologyGroupHeader.PaintedParts(_chart, group, _options))
                .Select(part => Bounds(part.X - 3, part.Y - 3, part.Width + 6, part.Height + 6)));
        return obstacles;
    }

    internal IEnumerable<ChartRect> EndpointLabelBounds(TopologyEdge edge) {
        if (!_routes.TryGetValue(edge, out var route) || route.Count < 2) yield break;
        var full = _numericRoutes.TryGetValue(edge, out var numeric) && edge.Interpolation.HasValue ? numeric.FullPath : edge.PathGeometry?.Capture();
        if (!string.IsNullOrWhiteSpace(edge.SourceLabel)) {
            var layout = ResolvedEndpointLabel(edge, route[0], full == null ? route[1] : full.AdjacentToEndpoint(true), edge.SourceLabel!, true);
            yield return Bounds(layout.Bounds.X, layout.Bounds.Y, layout.Bounds.Width, layout.Bounds.Height);
        }
        if (!string.IsNullOrWhiteSpace(edge.TargetLabel)) {
            var layout = ResolvedEndpointLabel(edge, route[route.Count - 1], full == null ? route[route.Count - 2] : full.AdjacentToEndpoint(false), edge.TargetLabel!, false);
            yield return Bounds(layout.Bounds.X, layout.Bounds.Y, layout.Bounds.Width, layout.Bounds.Height);
        }
    }

    // Chips prefer one row centred on the route near its middle. A row that cannot fit stacks into a column, and as a
    // last resort the arrangement steps off the line. Every candidate is scored by the area it covers.
    private static ChartRect[]? EdgeChipBoxes(IReadOnlyList<ChartPoint> route, double[] widths, double height, double gap, IReadOnlyList<ChartRect> obstacles, ChartRect frame, IReadOnlyList<ChartPoint[]> others) {
        var lengths = new double[route.Count];
        for (var i = 1; i < route.Count; i++) lengths[i] = lengths[i - 1] + Distance(route[i - 1], route[i]);
        var total = lengths[route.Count - 1];
        (ChartPoint Point, ChartPoint Normal) At(double distance) {
            for (var i = 1; i < route.Count; i++) {
                if (lengths[i] < distance && i < route.Count - 1) continue;
                var span = lengths[i] - lengths[i - 1];
                var t = span <= 0 ? 0 : Math.Max(0, Math.Min(1, (distance - lengths[i - 1]) / span));
                var dx = route[i].X - route[i - 1].X; var dy = route[i].Y - route[i - 1].Y; var length = Math.Max(1e-9, Math.Sqrt(dx * dx + dy * dy));
                return (new ChartPoint(route[i - 1].X + dx * t, route[i - 1].Y + dy * t), new ChartPoint(-dy / length, dx / length));
            }
            return (route[0], new ChartPoint(0, 1));
        }
        ChartRect[] Arrange(ChartPoint center, bool column) {
            var boxes = new ChartRect[widths.Length];
            if (column) {
                var top = center.Y - (widths.Length * height + (widths.Length - 1) * gap) / 2;
                for (var i = 0; i < widths.Length; i++) boxes[i] = new ChartRect(center.X - widths[i] / 2, top + i * (height + gap), widths[i], height);
            } else {
                var x = center.X - (widths.Sum() + gap * (widths.Length - 1)) / 2;
                for (var i = 0; i < widths.Length; i++) { boxes[i] = new ChartRect(x, center.Y - height / 2, widths[i], height); x += widths[i] + gap; }
            }
            return boxes;
        }
        ChartRect[]? best = null; var bestScore = double.MaxValue;
        const int steps = 24;
        foreach (var column in widths.Length > 1 ? new[] { false, true } : new[] { false }) {
            foreach (var shift in new[] { 0d, 1, -1, 2, -2, 3, -3, 4, -4 }) {
                for (var step = 0; step <= steps; step++) {
                    var fraction = .5 + (step % 2 == 0 ? 1 : -1) * ((step + 1) / 2) / (double)steps;
                    if (fraction < .08 || fraction > .92) continue;
                    var (point, normal) = At(total * fraction);
                    var offset = shift * (height + gap) * (column ? widths.Length : 1);
                    var boxes = Arrange(new ChartPoint(point.X + normal.X * offset, point.Y + normal.Y * offset), column);
                    var covered = boxes.Sum(box => obstacles.Sum(obstacle => OverlapArea(box, obstacle)) + box.Width * box.Height - OverlapArea(box, frame));
                    var shared = boxes.Sum(box => others.Count(other => Crosses(other, box)));
                    var score = covered * 1000 + shared * 50 + Math.Abs(fraction - .5) + (column ? 1 : 0) + Math.Abs(shift) * 2;
                    // Candidates run from the preferred arrangement outward, so the first free one is the answer.
                    if (covered <= 0 && shared == 0) return boxes;
                    if (score >= bestScore) continue;
                    best = boxes; bestScore = score;
                }
                if (bestScore < 50) break;
            }
            if (bestScore < 50) break;
        }
        // Only a candidate clear of nodes, labels and the frame is drawn; one crossing another link is acceptable.
        return bestScore < 1000 ? best : null;
    }

    private static bool Crosses(ChartPoint[] route, ChartRect box) {
        for (var i = 1; i < route.Length; i++) {
            var a = route[i - 1]; var b = route[i];
            // Liang-Barsky: clip the segment to the box; any remaining part means it runs through.
            double low = 0, high = 1, dx = b.X - a.X, dy = b.Y - a.Y;
            bool Clip(double p, double q) {
                if (Math.Abs(p) < 1e-12) return q >= 0;
                var r = q / p;
                if (p < 0) { if (r > high) return false; if (r > low) low = r; }
                else { if (r < low) return false; if (r < high) high = r; }
                return true;
            }
            if (Clip(-dx, a.X - box.X) && Clip(dx, box.Right - a.X) && Clip(-dy, a.Y - box.Y) && Clip(dy, box.Bottom - a.Y) && low <= high) return true;
        }
        return false;
    }

    private static bool SegmentNear(ChartPoint a, ChartPoint b, ChartRect window) =>
        Math.Max(a.X, b.X) >= window.X && Math.Min(a.X, b.X) <= window.Right && Math.Max(a.Y, b.Y) >= window.Y && Math.Min(a.Y, b.Y) <= window.Bottom;

    private static double Distance(ChartPoint a, ChartPoint b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    private static double OverlapArea(ChartRect a, ChartRect b) {
        var width = Math.Min(a.Right, b.Right) - Math.Max(a.X, b.X);
        var height = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Y, b.Y);
        return width > 0 && height > 0 ? width * height : 0;
    }
}
