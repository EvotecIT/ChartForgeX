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
        var label = NodeCaptionContent(node);
        var size = TextPolicySize(_context.Theme.Typography.DataLabelSize);
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
        var lineHeight = MeasurePolicyText("Ag", size * _scale, 600).LineHeight / _scale;
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
        Text(node.LabelContent.IsStructured ? NodeCaptionContent(node) : TextContent.Plain(text), bounds, size, Highlight(labelColor, active), 700, "topology-node-label", centered: true, id: node.Id + "-label",
            paint: string.IsNullOrWhiteSpace(node.LabelColor) ? null : SourcePaint(node.LabelColor, Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground), opacity: HighlightFactor(active));
    }

    private void BuildDiagramCaption(TopologyNode node, bool active) {
        if (TryBuildCapturedDiagramCaption(node, active)) return;
        if (TopologyActorCaption.Applies(node, _options)) { BuildActorCaption(node, active); return; }
        var actorUnits = TopologyPreparedNodeBasis.Units(node);
        var actorSize = TextPolicySize(_context.Theme.Typography.DataLabelSize);
        if (node.TextStyle?.FontSize.HasValue != true) actorSize *= actorUnits;
        if (node.LabelContent.IsStructured) {
            var content = NodeCaptionContent(node);
            var captionSize = FitCaptionFontSize(content, DiagramNodeLabelLines(node, _options), node.Width * .65,
                actorSize, 600, actorUnits);
            var captionCenter = Point(new ChartPoint(node.X + node.Width / 2, DiagramNodeLabelCenterY(node)));
            var height = Math.Max(1, _options.MaxNodeLabelLines)
                * Math.Max(14 * actorUnits * _scale, CaptionLineHeight(content, captionSize * _scale)) + 1e-8;
            var color = Highlight(Color(node.LabelColor, _colors.Foreground), active);
            InlineText(content, new ChartRect(captionCenter.X - node.Width * .325 * _scale, captionCenter.Y - height / 2, node.Width * .65 * _scale, height),
                captionSize * _scale, color, "topology-node-label", _options.MaxNodeLabelLines, true, node.Id + "-label");
            return;
        }
        var lines = DiagramNodeLabelLines(node, _options);
        var size = FitCaptionFontSize(lines, node.Width * .65, actorSize, 600, actorUnits);
        var lineHeight = Math.Max(14 * actorUnits * _scale, MeasurePolicyText("Ag", size * _scale, 600).LineHeight);
        var center = Point(new ChartPoint(node.X + node.Width / 2, DiagramNodeLabelCenterY(node)));
        var labelColor = string.IsNullOrWhiteSpace(node.LabelColor) ? _colors.Foreground : Color(node.LabelColor, _colors.Foreground);
        var paint = string.IsNullOrWhiteSpace(node.LabelColor)
            ? ForegroundPaint(Highlight(labelColor, active), HighlightFactor(active))
            : SourcePaint(node.LabelColor, Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground);
        for (var i = 0; i < lines.Count; i++) DrawPolicyText(lines[i], center.X,
            center.Y - MeasurePolicyText("Ag", size * _scale, 600).Height / 2 + PolicyTextAscent(size * _scale, 600) + i * lineHeight,
            size * _scale, Highlight(labelColor, active), 600, "topology-node-label", node.Id + "-label-" + i,
            TextAlignment.Center, paint);
    }

    private void BuildTileCaption(TopologyNode node, ChartColor accent, bool active) {
        // The canonical caption owner also supplies routing obstacles and layout footprints.
        var lines = TileCaptionLines(node, _options);
        var content = NodeCaptionContent(node);
        var size = FitCaptionFontSize(content, lines, node.Width, TextPolicySize(_context.Theme.Typography.DataLabelSize), 700);
        var lineHeight = Math.Max(14 * _scale, content.IsStructured ? CaptionLineHeight(content, size * _scale)
            : MeasurePolicyText("Ag", size * _scale, 700).LineHeight);
        var origin = Point(new ChartPoint(node.X + node.Width / 2, node.Y + node.Height + 5));
        var labelColor = string.IsNullOrWhiteSpace(node.LabelColor) ? _colors.Foreground : Color(node.LabelColor, _colors.Foreground);
        var paint = string.IsNullOrWhiteSpace(node.LabelColor)
            ? ForegroundPaint(Highlight(labelColor, active), HighlightFactor(active))
            : SourcePaint(node.LabelColor, Highlight(labelColor, active), SvgColorRole.Any, HighlightFactor(active), _colors.Foreground);
        if (node.LabelContent.IsStructured) {
            var height = Math.Max(1, _options.MaxNodeLabelLines) * lineHeight + 1e-8;
            Text(content, new ChartRect(origin.X - node.Width * _scale / 2, origin.Y, node.Width * _scale, height), size,
                Highlight(labelColor, active), 700, "topology-node-label", _options.MaxNodeLabelLines, true, node.Id + "-label", paint);
        } else         for (var index = 0; index < lines.Count; index++) DrawPolicyText(lines[index], origin.X,
            origin.Y + PolicyTextAscent(size * _scale, 700) + index * lineHeight, size * _scale,
            Highlight(labelColor, active), 700, "topology-node-label", node.Id + "-label-" + index,
            TextAlignment.Center, paint);
        if (_options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle))
            BuildSubtitleChip(node, TopologyNodeDisplayMode.Tile, node.Y + node.Height + 7 + lines.Count * lineHeight / _scale, accent, active);
    }

    // Caption footprints and routing reserve the ordinary 11px lines. Fit authored
    // typography within those cells using actual native metrics, including bitmap faces.
    private double FitCaptionFontSize(TextContent content, IReadOnlyList<string> lines, double width, double preferred, int weight, double units = 1) {
        if (!content.IsStructured) return FitCaptionFontSize(lines, width, preferred, weight, units);
        var height = Math.Max(14 * units * _scale, CaptionLineHeight(content, 11 * units * _scale));
        var maximumWidth = Math.Max(0, width * _scale);
        foreach (var line in lines) maximumWidth = Math.Max(maximumWidth, MeasurePolicyText(line, 11 * units * _scale, weight).Width);
        return ChartTextFitting.FitFontSize(Math.Min(11 * units, preferred), preferred, candidate => {
            var metrics = TextLayoutEngine.Measure(content, InlineStyle(candidate * _scale, _colors.Foreground));
            return metrics.LineHeight <= height && metrics.Width <= maximumWidth;
        });
    }

    private double FitCaptionFontSize(IReadOnlyList<string> lines, double width, double preferred, int weight, double units = 1) {
        var height = Math.Max(14 * units * _scale, MeasurePolicyText("Ag", 11 * units * _scale, weight).LineHeight);
        var maximumWidth = Math.Max(0, width * _scale);
        foreach (var line in lines) maximumWidth = Math.Max(maximumWidth, MeasurePolicyText(line, 11 * units * _scale, weight).Width);
        return ChartTextFitting.FitFontSize(Math.Min(11 * units, preferred), preferred, candidate => {
            if (MeasurePolicyText("Ag", candidate * _scale, weight).LineHeight > height) return false;
            foreach (var line in lines) if (MeasurePolicyText(line, candidate * _scale, weight).Width > maximumWidth) return false;
            return true;
        });
    }

    private void BuildSubtitleChip(TopologyNode node, TopologyNodeDisplayMode mode, double y, ChartColor accent, bool active) {
        var chip = SubtitleChip(node, mode, _options);
        double width, height, size;
        TextMetrics metrics;
        if (node.SubtitleTextStyle?.HasOverrides == true) {
            var measured = MeasuredSubtitleChip(node, mode, _options, _context.Font, _scale);
            width = measured.Width; height = measured.Height; size = measured.FontSize; metrics = measured.Metrics;
        } else {
            var ordinary = MeasureRoleText(chip.Text, RoleTextSize(9.5, node.SubtitleTextStyle) * _scale, 700, node.SubtitleTextStyle);
            width = Math.Max(chip.Width * _scale, ordinary.Width + 18 * _scale);
            height = Math.Max(17 * _scale, ordinary.Height + 4 * _scale);
            var preferred = RoleTextSize(TextPolicySize(_context.Theme.Typography.DataLabelSize) * (9.5 / 11), node.SubtitleTextStyle);
            size = ChartTextFitting.FitFontSize(Math.Min(9.5, preferred), preferred, candidate => {
                var measured = MeasureRoleText(chip.Text, candidate * _scale, 700, node.SubtitleTextStyle);
                return measured.Width <= width - 18 * _scale && measured.Height <= height - 4 * _scale;
            });
            metrics = MeasureRoleText(chip.Text, size * _scale, 700, node.SubtitleTextStyle);
        }
        var tile = mode == TopologyNodeDisplayMode.Tile;
        var origin = Point(new ChartPoint(tile ? node.X + node.Width / 2 : node.X + 42, y));
        var bounds = new ChartRect(tile ? origin.X - width / 2 : origin.X, origin.Y, width, height);
        var role = tile ? "topology-node-subtitle" : "topology-node-card-subtitle";
        using (_builder.PushGroup(node.Id + "-subtitle", role, new Dictionary<string, string> { ["data-node-id"] = node.Id })) {
            var fill = ChartForgeX.Rendering.ChartColorMath.WithOpacity(accent, .1); var stroke = ChartForgeX.Rendering.ChartColorMath.WithOpacity(accent, .45);
            _builder.Rect(bounds, fill, stroke, _context.Theme.AxisStrokeWidth * _scale,
                height / 2, "topology-subtitle-chip", paint: new ChartForgeX.Rendering.VisualScenePaintBinding(NodeAccentPaint(node, fill, active, .1), NodeAccentPaint(node, stroke, active, .45)));
            DrawRoleText(chip.Text, bounds.X + width / 2,
                bounds.Y + (height - metrics.Height) / 2 + RoleTextAscent(size * _scale, 700, node.SubtitleTextStyle), size * _scale,
                NodeSubtitleColor(node, active), 700, "topology-subtitle-chip-text", alignment: TextAlignment.Center,
                paint: NodeSubtitlePaint(node, active), role: node.SubtitleTextStyle);
        }
    }
}
