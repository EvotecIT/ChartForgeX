using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildIconCaption(TopologyNode node, ChartColor accent, bool active) {
        var size = _options.ResolvedIconLabelFontSize;
        var text = IconLabelText(node, _chart.TextMeasurement, size);
        var width = IconLabelPlateWidth(node, _chart.TextMeasurement, size) * _scale;
        var height = IconLabelPlateHeight(_chart.TextMeasurement, size) * _scale;
        var origin = Point(new ChartPoint(node.X + node.Width / 2, IconLabelPlateY(node)));
        var bounds = new ChartRect(origin.X - width / 2, origin.Y, width, height);
        _builder.Rect(bounds, Highlight(_colors.Surface, active), accent.WithOpacity(.4), _context.Theme.AxisStrokeWidth * _scale,
            _context.Theme.BarRadius * _scale, "topology-node-icon-label", paint: Paint(Highlight(_colors.Surface, active), SvgColorRole.Surface, accent.WithOpacity(.4), AccentRole(node.Color)));
        Text(text, bounds, size, Highlight(_colors.Foreground, active), 700, "topology-node-label", centered: true, id: node.Id + "-label");
    }

    private void BuildDiagramCaption(TopologyNode node, bool active) {
        var lines = DiagramNodeLabelLines(node, _options);
        var size = _context.Theme.Typography.DataLabelSize;
        var lineHeight = Math.Max(14 * _scale, _builder.MeasureText("Ag", size * _scale, 600).LineHeight);
        var center = Point(new ChartPoint(node.X + node.Width / 2, DiagramNodeLabelCenterY(node)));
        for (var i = 0; i < lines.Count; i++) _builder.Text(lines[i], center.X,
            center.Y - _builder.MeasureText("Ag", size * _scale, 600).Height / 2 + _builder.TextAscent(size * _scale, 600) + i * lineHeight,
            size * _scale, Highlight(_colors.Foreground, active), 600, "topology-node-label", node.Id + "-label-" + i,
            TextAlignment.Center, SvgPaint.Of(Highlight(_colors.Foreground, active), SvgColorRole.Text));
    }

    private void BuildTileCaption(TopologyNode node, ChartColor accent, bool active) {
        // The canonical caption owner also supplies routing obstacles and layout footprints.
        var lines = TileCaptionLines(node, _options);
        var size = _context.Theme.Typography.DataLabelSize;
        var lineHeight = Math.Max(14 * _scale, _builder.MeasureText("Ag", size * _scale, 700).LineHeight);
        var origin = Point(new ChartPoint(node.X + node.Width / 2, node.Y + node.Height + 5));
        for (var index = 0; index < lines.Count; index++) _builder.Text(lines[index], origin.X,
            origin.Y + _builder.TextAscent(size * _scale, 700) + index * lineHeight, size * _scale,
            Highlight(_colors.Foreground, active), 700, "topology-node-label", node.Id + "-label-" + index,
            TextAlignment.Center, SvgPaint.Of(Highlight(_colors.Foreground, active), SvgColorRole.Text));
        if (_options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle))
            BuildSubtitleChip(node, TopologyNodeDisplayMode.Tile, node.Y + node.Height + 7 + lines.Count * lineHeight / _scale, accent, active);
    }

    private void BuildSubtitleChip(TopologyNode node, TopologyNodeDisplayMode mode, double y, ChartColor accent, bool active) {
        var chip = SubtitleChip(node, mode, _options);
        var size = _context.Theme.Typography.DataLabelSize * (9.5 / 11);
        var metrics = _builder.MeasureText(chip.Text, size * _scale, 700);
        var width = Math.Max(chip.Width * _scale, metrics.Width + 18 * _scale);
        var height = Math.Max(17 * _scale, metrics.Height + 4 * _scale);
        var tile = mode == TopologyNodeDisplayMode.Tile;
        var origin = Point(new ChartPoint(tile ? node.X + node.Width / 2 : node.X + 42, y));
        var bounds = new ChartRect(tile ? origin.X - width / 2 : origin.X, origin.Y, width, height);
        var role = tile ? "topology-node-subtitle" : "topology-node-card-subtitle";
        using (_builder.PushGroup(node.Id + "-subtitle", role, new Dictionary<string, string> { ["data-node-id"] = node.Id })) {
            _builder.Rect(bounds, accent.WithOpacity(.1), accent.WithOpacity(.45), _context.Theme.AxisStrokeWidth * _scale,
                height / 2, "topology-subtitle-chip", paint: Paint(accent.WithOpacity(.1), AccentRole(node.Color ?? ResolveNodeIcon(node, _options)?.Color), accent.WithOpacity(.45), AccentRole(node.Color ?? ResolveNodeIcon(node, _options)?.Color)));
            _builder.Text(chip.Text, bounds.X + width / 2,
                bounds.Y + (height - metrics.Height) / 2 + _builder.TextAscent(size * _scale, 700), size * _scale,
                Highlight(_colors.MutedForeground, active), 700, "topology-subtitle-chip-text", alignment: TextAlignment.Center,
                paint: SvgPaint.Of(Highlight(_colors.MutedForeground, active), SvgColorRole.Text));
        }
    }
}
