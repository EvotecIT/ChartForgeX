using ChartForgeX.Core;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    /// <summary>The icon tile, gap and status dot reserve beside a card label.</summary>
    internal const double CardIconTile = 26, CardTextLeft = 46, CardTextRight = 18, CardSubtitleRatio = .82, CardRadius = 4;

    /// <summary>Draws a participant as a topology-style card: icon tile, label, subtitle and status dot.</summary>
    private static void CardParticipant(VisualSceneBuilder builder, SequenceArtifactParticipant participant, SequencePreparedLayout.Item item,
        SequencePreparedLayout layout, VisualThemeColors colors, VisualTheme theme, ChartColor fill, ChartColor border) {
        var card = participant.Card!;
        var b = item.Bounds;
        var stroke = theme.AxisStrokeWidth;
        using (ParticipantSurface(builder, participant, b, stroke)) builder.Rect(b, fill, border, stroke, CardRadius, "sequence-participant-box");
        var iconColor = card.IconColor ?? border;
        var tile = new ChartRect(b.X + 10, b.Y + (b.Height - CardIconTile) / 2, CardIconTile, CardIconTile);
        builder.Rect(tile, null, ChartColorMath.WithOpacity(iconColor, .45), stroke, 5, "sequence-participant-icon-tile");
        CardGlyph(builder, card, tile.X + tile.Width / 2, tile.Y + tile.Height / 2, iconColor);
        if (card.StatusColor is ChartColor status) builder.Ellipse(b.Right - 9, b.Y + 9, 3, 3, status, null, 0, "sequence-participant-status");

        var size = layout.FontSize;
        var width = System.Math.Max(1, b.Width - CardTextLeft - CardTextRight);
        var label = item.Text;
        var labelHeight = layout.Presentation.Measure(label, builder, SequenceTextRole.Participant, size).Height;
        var subtitle = string.IsNullOrWhiteSpace(card.Subtitle) ? string.Empty : layout.Presentation.Wrap(card.Subtitle!.Trim(), width, builder, SequenceTextRole.Participant, size, CardSubtitleRatio);
        var subtitleHeight = subtitle.Length == 0 ? 0 : layout.Presentation.Measure(subtitle, builder, SequenceTextRole.Participant, size, CardSubtitleRatio).Height;
        var top = b.Y + (b.Height - labelHeight - (subtitleHeight > 0 ? subtitleHeight + 2 : 0)) / 2;
        if (layout.Presentation.Customized) Text(builder, label, new ChartRect(b.X + CardTextLeft, top, width, labelHeight), layout, SequenceTextRole.Participant, colors.Foreground);
        else {
            // Card labels are semibold, like topology node cards.
            var lines = label.Split('\n');
            var lineHeight = labelHeight / lines.Length;
            for (var line = 0; line < lines.Length; line++)
                builder.Text(lines[line], b.X + CardTextLeft, top + line * lineHeight + builder.TextAscent(size), size, colors.Foreground, 600, role: "sequence-label");
        }
        if (subtitle.Length > 0) Text(builder, subtitle, new ChartRect(b.X + CardTextLeft, top + labelHeight + 2, width, subtitleHeight), layout, SequenceTextRole.Participant, colors.MutedForeground, ratio: CardSubtitleRatio);
    }

    private static void CardGlyph(VisualSceneBuilder builder, SequenceParticipantCard card, double cx, double cy, ChartColor color) {
        var probe = new TopologyNode { Kind = card.IconKind, IconShape = card.IconShape };
        var shape = TopologyRenderPrimitives.EffectiveIconShape(probe, new TopologyRenderOptions());
        var marks = TopologyInfrastructureGlyphs.Build(shape, card.IconKind, cx, cy);
        if (marks == null) {
            builder.Text(TopologyRenderPrimitives.KindGlyph(card.IconKind), cx, cy + 4, 10, color, role: "sequence-participant-icon", alignment: TextAlignment.Center);
            return;
        }

        const double scale = .8;
        foreach (var mark in marks) {
            if (mark.PathData != null) {
                foreach (var contours in ChartMapPathParser.ParseSubpaths(mark.PathData, 2).GroupBy(contour => contour.IsClosed)) {
                    var commands = contours.SelectMany(contour => contour.Points.Select((point, index) => {
                        var x = cx + (point.X - cx) * scale;
                        var y = cy + (point.Y - cy) * scale;
                        return index == 0 ? ChartPathCommand.MoveTo(x, y) : ChartPathCommand.LineTo(x, y);
                    })).ToArray();
                    builder.Path(new ChartPath(commands), mark.FillNone ? null : color, mark.Filled ? null : color, mark.StrokeWidth * scale,
                        role: "sequence-participant-icon", close: contours.Key,
                        cap: mark.RoundCap ? VisualStrokeCap.Round : VisualStrokeCap.Butt, join: mark.RoundJoin ? VisualStrokeJoin.Round : VisualStrokeJoin.Miter);
                }
            } else {
                builder.Ellipse(cx + (mark.Cx - cx) * scale, cy + (mark.Cy - cy) * scale, mark.Rx * scale, mark.Ry * scale,
                    mark.Filled ? color : null, mark.Filled ? null : color, mark.StrokeWidth * scale, "sequence-participant-icon");
            }
        }
    }
}
