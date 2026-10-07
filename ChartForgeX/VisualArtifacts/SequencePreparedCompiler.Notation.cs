using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    private static void Participant(VisualSceneBuilder builder, SequenceArtifactParticipantKind kind, SequencePreparedLayout.Item item,
        double size, VisualThemeColors colors, VisualTheme theme) {
        var b = item.Bounds; double x = b.X + b.Width / 2, y = b.Y + 21, stroke = theme.AxisStrokeWidth;
        if (kind == SequenceArtifactParticipantKind.Participant) {
            builder.Rect(b, colors.Surface, colors.Border, stroke, theme.BarRadius, "sequence-participant-box");
        } else {
            using (builder.PushGroup(null, "sequence-notation", new System.Collections.Generic.Dictionary<string, string> { ["data-kind"] = kind.ToString() })) {
                switch (kind) {
                    case SequenceArtifactParticipantKind.Actor:
                        builder.Ellipse(x, y - 11, 6, 6, colors.Surface, colors.Accent, stroke);
                        builder.Line(x, y - 5, x, y + 10, colors.Accent, stroke);
                        builder.Line(x - 12, y, x + 12, y, colors.Accent, stroke);
                        builder.Line(x, y + 10, x - 11, y + 20, colors.Accent, stroke);
                        builder.Line(x, y + 10, x + 11, y + 20, colors.Accent, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Boundary:
                        builder.Ellipse(x + 4, y + 2, 13, 13, colors.Surface, colors.Accent, stroke);
                        builder.Line(x - 20, y - 12, x - 20, y + 16, colors.Accent, stroke);
                        builder.Line(x - 20, y + 2, x - 9, y + 2, colors.Accent, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Control:
                        builder.Ellipse(x, y + 2, 13, 13, colors.Surface, colors.Accent, stroke);
                        builder.Line(x - 4, y - 11, x + 10, y - 11, colors.Accent, stroke);
                        builder.Line(x - 4, y - 11, x + 1, y - 16, colors.Accent, stroke);
                        builder.Line(x - 4, y - 11, x + 1, y - 6, colors.Accent, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Entity:
                        builder.Ellipse(x, y, 13, 13, colors.Surface, colors.Accent, stroke);
                        builder.Line(x - 16, y + 17, x + 16, y + 17, colors.Accent, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Database:
                        builder.Rect(new ChartRect(x - 17, y - 9, 34, 23), colors.Surface, colors.Accent, stroke);
                        builder.Ellipse(x, y + 14, 17, 5, colors.Surface, colors.Accent, stroke);
                        builder.Rect(new ChartRect(x - 17 + stroke, y, 34 - stroke * 2, 14), colors.Surface);
                        builder.Ellipse(x, y - 9, 17, 5, colors.Surface, colors.Accent, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Collections:
                        for (int i = 2; i >= 0; i--) builder.Rect(new ChartRect(x - 18 + i * 4, y - 13 - i * 3, 30, 26), colors.Surface, colors.Accent, stroke, 2);
                        break;
                    case SequenceArtifactParticipantKind.Queue:
                        builder.Rect(new ChartRect(x - 22, y - 10, 44, 23), colors.Surface, colors.Accent, stroke);
                        for (int i = 0; i < 3; i++) builder.Line(x - 10 + i * 10, y - 10, x - 10 + i * 10, y + 13, colors.Accent, stroke);
                        builder.Line(x - 22, y + 17, x + 22, y + 17, colors.Accent, stroke);
                        break;
                }
            }
        }
        double labelY = b.Y + (kind == SequenceArtifactParticipantKind.Participant ? 8 : 48);
        Text(builder, item.Text, new ChartRect(b.X + 8, labelY, b.Width - 16, b.Bottom - labelY - 8), size, colors.Foreground, center: true);
    }

    private static void MessageArrows(VisualSceneBuilder builder, SequenceArtifactMessage message, ChartPoint[] points, VisualThemeColors colors, double stroke) {
        var kind = message.Kind;
        bool cross = false, reverse = false, arrow = true;
        if (message.Metadata.TryGetValue("mermaid.operator", out var notation)) {
            if (notation.Length > 0 && notation != "--" && (notation[notation.Length - 1] == '+' || notation[notation.Length - 1] == '-')) notation = notation.Substring(0, notation.Length - 1);
            switch (notation) {
                case "--": arrow = false; break;
                case "-x": case "--x": cross = true; break;
                case "->": case "-->": kind = SequenceArtifactMessageKind.Async; break;
                case "->>": case "-->>": kind = SequenceArtifactMessageKind.Call; break;
                case "-)": case "--)": kind = SequenceArtifactMessageKind.Async; break;
                case "<<->>": case "<<-->>": kind = SequenceArtifactMessageKind.Call; reverse = true; break;
                default: throw new System.NotSupportedException("Unknown sequence message operator: " + notation);
            }
        }
        if (arrow) Arrow(builder, points[points.Length - 2], points[points.Length - 1], kind, colors.Accent, stroke, cross);
        if (reverse) Arrow(builder, points[1], points[0], kind, colors.Accent, stroke);
        if (message.Metadata.TryGetValue("mermaid.centralConnection", out var central) && central == "true") {
            double x = points.Length == 4 ? points[1].X : (points[0].X + points[1].X) / 2;
            double y = points.Length == 4 ? (points[1].Y + points[2].Y) / 2 : points[0].Y;
            builder.Ellipse(x, y, 4, 4, colors.Surface, colors.Accent, stroke, "sequence-central-connection");
        }
    }

    private static void Arrow(VisualSceneBuilder builder, ChartPoint from, ChartPoint to, SequenceArtifactMessageKind kind, ChartColor color, double stroke, bool cross = false) {
        double direction = to.X >= from.X ? 1 : -1;
        double x = to.X, y = to.Y;
        if (cross) {
            builder.Line(x - 5, y - 5, x + 5, y + 5, color, stroke, "sequence-event-cross");
            builder.Line(x - 5, y + 5, x + 5, y - 5, color, stroke, "sequence-event-cross");
        } else if (kind == SequenceArtifactMessageKind.Call) {
            builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(x, y), ChartPathCommand.LineTo(x - direction * 9, y - 5), ChartPathCommand.LineTo(x - direction * 9, y + 5) }), color, role: "sequence-call-arrow", close: true);
        } else {
            builder.Line(x - direction * 9, y - 5, x, y, color, stroke, kind == SequenceArtifactMessageKind.Async ? "sequence-async-arrow" : kind == SequenceArtifactMessageKind.Event ? "sequence-event-arrow" : "sequence-return-arrow");
            builder.Line(x - direction * 9, y + 5, x, y, color, stroke);
        }
    }
}
