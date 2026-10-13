using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildIconCaption(TopologyNode node, ChartColor accent, bool active) {
        var size = IconLabelFontSize(node, _chart.TextMeasurement, _options.ResolvedIconLabelFontSize);
        var text = IconLabelText(node, _chart.TextMeasurement, size);
        var width = IconLabelPlateWidth(node, _chart.TextMeasurement, size) * _scale;
        var height = IconLabelPlateHeight(node, _chart.TextMeasurement, size) * _scale;
        var origin = Point(new ChartPoint(node.X + node.Width / 2, IconLabelPlateY(node)));
        var bounds = new ChartRect(origin.X - width / 2, origin.Y, width, height);
        var stroke = ChartForgeX.Rendering.ChartColorMath.WithOpacity(accent, .4);
        _builder.Rect(bounds, Highlight(_colors.Surface, active), stroke, _context.Theme.AxisStrokeWidth * _scale,
            _context.Theme.BarRadius * _scale, "topology-node-icon-label", paint: new ChartForgeX.Rendering.VisualScenePaintBinding(CardPaint(Highlight(_colors.Surface, active), HighlightFactor(active)), NodeAccentPaint(node, stroke, active, .4)));
        Text(text, bounds, size, Highlight(_colors.Foreground, active), 700, "topology-node-label", centered: true, id: node.Id + "-label", opacity: HighlightFactor(active));
    }

    private void BuildDiagramCaption(TopologyNode node, bool active) {
        var lines = DiagramNodeLabelLines(node, _options);
        var size = FitCaptionFontSize(lines, node.Width * .65, _context.Theme.Typography.DataLabelSize, 600);
        var lineHeight = Math.Max(14 * _scale, _builder.MeasureText("Ag", size * _scale, 600).LineHeight);
        var center = Point(new ChartPoint(node.X + node.Width / 2, DiagramNodeLabelCenterY(node)));
        for (var i = 0; i < lines.Count; i++) _builder.Text(lines[i], center.X,
            center.Y - _builder.MeasureText("Ag", size * _scale, 600).Height / 2 + _builder.TextAscent(size * _scale, 600) + i * lineHeight,
            size * _scale, Highlight(_colors.Foreground, active), 600, "topology-node-label", node.Id + "-label-" + i,
            TextAlignment.Center, ForegroundPaint(Highlight(_colors.Foreground, active), HighlightFactor(active)));
    }

    private void BuildTileCaption(TopologyNode node, ChartColor accent, bool active) {
        // The canonical caption owner also supplies routing obstacles and layout footprints.
        var lines = TileCaptionLines(node, _options);
        var size = FitCaptionFontSize(lines, node.Width, _context.Theme.Typography.DataLabelSize, 700);
        var lineHeight = Math.Max(14 * _scale, _builder.MeasureText("Ag", size * _scale, 700).LineHeight);
        var origin = Point(new ChartPoint(node.X + node.Width / 2, node.Y + node.Height + 5));
        for (var index = 0; index < lines.Count; index++) _builder.Text(lines[index], origin.X,
            origin.Y + _builder.TextAscent(size * _scale, 700) + index * lineHeight, size * _scale,
            Highlight(_colors.Foreground, active), 700, "topology-node-label", node.Id + "-label-" + index,
            TextAlignment.Center, ForegroundPaint(Highlight(_colors.Foreground, active), HighlightFactor(active)));
        if (_options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle))
            BuildSubtitleChip(node, TopologyNodeDisplayMode.Tile, node.Y + node.Height + 7 + lines.Count * lineHeight / _scale, accent, active);
    }

    // Caption footprints and routing reserve the ordinary 11px lines. Fit authored
    // typography within those cells using actual native metrics, including bitmap faces.
    private double FitCaptionFontSize(IReadOnlyList<string> lines, double width, double preferred, int weight) {
        var height = Math.Max(14 * _scale, _builder.MeasureText("Ag", 11 * _scale, weight).LineHeight);
        var maximumWidth = Math.Max(0, width * _scale);
        foreach (var line in lines) maximumWidth = Math.Max(maximumWidth, _builder.MeasureText(line, 11 * _scale, weight).Width);
        return ChartTextFitting.FitFontSize(Math.Min(11, preferred), preferred, candidate => {
            if (_builder.MeasureText("Ag", candidate * _scale, weight).LineHeight > height) return false;
            foreach (var line in lines) if (_builder.MeasureText(line, candidate * _scale, weight).Width > maximumWidth) return false;
            return true;
        });
    }

    private void BuildSubtitleChip(TopologyNode node, TopologyNodeDisplayMode mode, double y, ChartColor accent, bool active) {
        var chip = SubtitleChip(node, mode, _options);
        var ordinary = _builder.MeasureText(chip.Text, 9.5 * _scale, 700);
        var width = Math.Max(chip.Width * _scale, ordinary.Width + 18 * _scale);
        var height = Math.Max(17 * _scale, ordinary.Height + 4 * _scale);
        var preferred = _context.Theme.Typography.DataLabelSize * (9.5 / 11);
        var size = ChartTextFitting.FitFontSize(Math.Min(9.5, preferred), preferred, candidate => {
            var measured = _builder.MeasureText(chip.Text, candidate * _scale, 700);
            return measured.Width <= width - 18 * _scale && measured.Height <= height - 4 * _scale;
        });
        var metrics = _builder.MeasureText(chip.Text, size * _scale, 700);
        var tile = mode == TopologyNodeDisplayMode.Tile;
        var origin = Point(new ChartPoint(tile ? node.X + node.Width / 2 : node.X + 42, y));
        var bounds = new ChartRect(tile ? origin.X - width / 2 : origin.X, origin.Y, width, height);
        var role = tile ? "topology-node-subtitle" : "topology-node-card-subtitle";
        using (_builder.PushGroup(node.Id + "-subtitle", role, new Dictionary<string, string> { ["data-node-id"] = node.Id })) {
            var fill = ChartForgeX.Rendering.ChartColorMath.WithOpacity(accent, .1); var stroke = ChartForgeX.Rendering.ChartColorMath.WithOpacity(accent, .45);
            _builder.Rect(bounds, fill, stroke, _context.Theme.AxisStrokeWidth * _scale,
                height / 2, "topology-subtitle-chip", paint: new ChartForgeX.Rendering.VisualScenePaintBinding(NodeAccentPaint(node, fill, active, .1), NodeAccentPaint(node, stroke, active, .45)));
            _builder.Text(chip.Text, bounds.X + width / 2,
                bounds.Y + (height - metrics.Height) / 2 + _builder.TextAscent(size * _scale, 700), size * _scale,
                Highlight(_colors.MutedForeground, active), 700, "topology-subtitle-chip-text", alignment: TextAlignment.Center,
                paint: MutedPaint(Highlight(_colors.MutedForeground, active), HighlightFactor(active)));
        }
    }
}
