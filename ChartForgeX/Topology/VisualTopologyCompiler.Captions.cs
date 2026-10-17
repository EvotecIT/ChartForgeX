using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private void BuildShapeCaption(TopologyNode node, bool active) {
        var label = node.MaximumLabelCharacters.HasValue ? TrimTo(node.Label, node.MaximumLabelCharacters.Value) : node.Label;
        var size = _context.Theme.Typography.DataLabelSize;
        if (CanFitNodeLabelToContent(node) || size > VisualTextFitting.MaximumOrdinaryFontSize) size = FitMeasuredNodeLabelSize(label, size, candidate => {
            var bounds = ShapeCaptionBounds(node, label, candidate);
            return Bounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        });
        var content = ShapeCaptionBounds(node, label, size);
        var labelColor = string.IsNullOrWhiteSpace(node.LabelColor) ? _colors.Foreground : Color(node.LabelColor, _colors.Foreground);
        Text(label, Bounds(content.X, content.Y, content.Width, content.Height), size, Highlight(labelColor, active), 600,
            "topology-node-label", _options.MaxNodeLabelLines, centered: true, id: node.Id + "-label", opacity: HighlightFactor(active),
            paint: string.IsNullOrWhiteSpace(node.LabelColor) ? null : SourcePaint(node.LabelColor, Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground), verticallyCentered: true);
    }

    private ChartRect ShapeCaptionBounds(TopologyNode node, string label, double size) {
        var lineHeight = _builder.MeasureText("Ag", size * _scale, 600).LineHeight / _scale;
        var maximum = (int)Math.Min(Math.Max(1, _options.MaxNodeLabelLines), Math.Min(Math.Max(1, label.Length), Math.Max(1, Math.Floor(node.Height / lineHeight))));
        var best = TopologyNodeShapeGeometry.CaptionBounds(node, lineHeight);
        var retained = -1;
        // A row limit is a ceiling, not a requested allocation. Use the same measured
        // wrapping as the painter, and keep the most source text if no complete layout fits.
        for (var rows = 1; rows <= maximum; rows++) {
            var bounds = TopologyNodeShapeGeometry.CaptionBounds(node, rows * lineHeight + 1e-8);
            if (bounds.Width <= 0 || bounds.Height + 1e-8 < rows * lineHeight) break;
            var lines = FitTextLines(label, bounds.Width * _scale, size * _scale, 600, rows, false, false, out var overflow);
            if (!overflow) return bounds;
            var characters = 0;
            foreach (var line in lines) characters += line.Length;
            if (characters > retained) { retained = characters; best = bounds; }
        }
        return best;
    }

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
        var labelColor = string.IsNullOrWhiteSpace(node.LabelColor) ? _colors.Foreground : Color(node.LabelColor, _colors.Foreground);
        Text(text, bounds, size, Highlight(labelColor, active), 700, "topology-node-label", centered: true, id: node.Id + "-label",
            paint: string.IsNullOrWhiteSpace(node.LabelColor) ? null : SourcePaint(node.LabelColor, Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground), opacity: HighlightFactor(active));
    }

    private void BuildDiagramCaption(TopologyNode node, bool active) {
        var lines = DiagramNodeLabelLines(node, _options);
        var size = FitCaptionFontSize(lines, node.Width * .65, _context.Theme.Typography.DataLabelSize, 600);
        var lineHeight = Math.Max(14 * _scale, _builder.MeasureText("Ag", size * _scale, 600).LineHeight);
        var center = Point(new ChartPoint(node.X + node.Width / 2, DiagramNodeLabelCenterY(node)));
        var labelColor = string.IsNullOrWhiteSpace(node.LabelColor) ? _colors.Foreground : Color(node.LabelColor, _colors.Foreground);
        var paint = string.IsNullOrWhiteSpace(node.LabelColor)
            ? ForegroundPaint(Highlight(labelColor, active), HighlightFactor(active))
            : SourcePaint(node.LabelColor, Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground);
        for (var i = 0; i < lines.Count; i++) _builder.Text(lines[i], center.X,
            center.Y - _builder.MeasureText("Ag", size * _scale, 600).Height / 2 + _builder.TextAscent(size * _scale, 600) + i * lineHeight,
            size * _scale, Highlight(labelColor, active), 600, "topology-node-label", node.Id + "-label-" + i,
            TextAlignment.Center, paint);
    }

    private void BuildTileCaption(TopologyNode node, ChartColor accent, bool active) {
        // The canonical caption owner also supplies routing obstacles and layout footprints.
        var lines = TileCaptionLines(node, _options);
        var size = FitCaptionFontSize(lines, node.Width, _context.Theme.Typography.DataLabelSize, 700);
        var lineHeight = Math.Max(14 * _scale, _builder.MeasureText("Ag", size * _scale, 700).LineHeight);
        var origin = Point(new ChartPoint(node.X + node.Width / 2, node.Y + node.Height + 5));
        var labelColor = string.IsNullOrWhiteSpace(node.LabelColor) ? _colors.Foreground : Color(node.LabelColor, _colors.Foreground);
        var paint = string.IsNullOrWhiteSpace(node.LabelColor)
            ? ForegroundPaint(Highlight(labelColor, active), HighlightFactor(active))
            : SourcePaint(node.LabelColor, Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground);
        for (var index = 0; index < lines.Count; index++) _builder.Text(lines[index], origin.X,
            origin.Y + _builder.TextAscent(size * _scale, 700) + index * lineHeight, size * _scale,
            Highlight(labelColor, active), 700, "topology-node-label", node.Id + "-label-" + index,
            TextAlignment.Center, paint);
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
