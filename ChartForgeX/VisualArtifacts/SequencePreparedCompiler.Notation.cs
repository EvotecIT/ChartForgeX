using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

internal static partial class SequencePreparedCompiler {
    private static void Participant(VisualSceneBuilder builder, SequenceArtifactParticipant participant, SequencePreparedLayout.Item item,
        SequencePreparedLayout layout, VisualThemeColors colors, VisualTheme theme, SequenceArtifactPaint paint) {
        var size = layout.FontSize; var sides = layout.Presentation.WrapSides;
        var kind=participant.Kind;
        var fill = participant.Fill ?? paint.ParticipantFill ?? colors.Surface;
        var border = participant.Border ?? paint.ParticipantBorder ?? (kind == SequenceArtifactParticipantKind.Participant ? colors.Border : colors.Accent);
        var b = item.Bounds; double x = b.X + b.Width / 2, y = b.Y + 21, stroke = theme.AxisStrokeWidth;
        if(item.BandAllocation!=null){BandedParticipant(builder,participant,item,layout,colors,theme,paint);return;}
        if (participant.Card != null && kind == SequenceArtifactParticipantKind.Participant) { CardParticipant(builder, participant, item, layout, colors, theme, fill, border); return; }
        using (ParticipantSurface(builder, participant, b, stroke)) {
            ParticipantShape(builder, kind, b, x, y, fill, border, stroke, theme);
        }
        double labelY = b.Y + (kind == SequenceArtifactParticipantKind.Participant ? 8 : 48);
        if (kind == SequenceArtifactParticipantKind.Participant) {
            var textHeight = layout.Presentation.Measure(item.Text, builder, SequenceTextRole.Participant, size).Height;
            labelY = b.Y + (b.Height - textHeight) / 2;
        }
        if (!string.IsNullOrEmpty(participant.Stereotype)) {
            var stereotype=layout.Presentation.Wrap("«"+participant.Stereotype+"»",b.Width-sides*2,builder,SequenceTextRole.Participant,size,.85);
            var height=layout.Presentation.Measure(stereotype,builder,SequenceTextRole.Participant,size,.85).Height;
            if (kind==SequenceArtifactParticipantKind.Participant) labelY=b.Y+8;
            Text(builder,stereotype,new ChartRect(b.X+sides,labelY,b.Width-sides*2,height),layout,SequenceTextRole.Participant,ParticipantInk(participant,paint,colors,kind==SequenceArtifactParticipantKind.Participant),center:true,ratio:.85);
            labelY+=height+4;
        }
        Text(builder, item.Text, new ChartRect(b.X + sides, labelY, b.Width - sides * 2, b.Bottom - labelY - 8), layout, SequenceTextRole.Participant, ParticipantInk(participant,paint,colors,kind==SequenceArtifactParticipantKind.Participant), center: true);
    }

    private static void ParticipantShape(VisualSceneBuilder builder,SequenceArtifactParticipantKind kind,ChartRect b,double x,double y,ChartColor fill,ChartColor border,double stroke,VisualTheme theme) {
        if (kind == SequenceArtifactParticipantKind.Participant) {
            builder.Rect(b, fill, border, stroke, theme.BarRadius, "sequence-participant-box");
        } else {
            using (builder.PushGroup(null, "sequence-notation", new System.Collections.Generic.Dictionary<string, string> { ["data-kind"] = kind.ToString() })) {
                switch (kind) {
                    case SequenceArtifactParticipantKind.Actor:
                        builder.Ellipse(x, y - 11, 6, 6, fill, border, stroke);
                        builder.Line(x, y - 5, x, y + 10, border, stroke);
                        builder.Line(x - 12, y, x + 12, y, border, stroke);
                        builder.Line(x, y + 10, x - 11, y + 20, border, stroke);
                        builder.Line(x, y + 10, x + 11, y + 20, border, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Boundary:
                        builder.Ellipse(x + 4, y + 2, 13, 13, fill, border, stroke);
                        builder.Line(x - 20, y - 12, x - 20, y + 16, border, stroke);
                        builder.Line(x - 20, y + 2, x - 9, y + 2, border, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Control:
                        builder.Ellipse(x, y + 2, 13, 13, fill, border, stroke);
                        builder.Line(x - 4, y - 11, x + 10, y - 11, border, stroke);
                        builder.Line(x - 4, y - 11, x + 1, y - 16, border, stroke);
                        builder.Line(x - 4, y - 11, x + 1, y - 6, border, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Entity:
                        builder.Ellipse(x, y, 13, 13, fill, border, stroke);
                        builder.Line(x - 16, y + 17, x + 16, y + 17, border, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Database:
                        builder.Rect(new ChartRect(x - 17, y - 9, 34, 23), fill, border, stroke);
                        builder.Ellipse(x, y + 14, 17, 5, fill, border, stroke);
                        builder.Rect(new ChartRect(x - 17 + stroke, y, 34 - stroke * 2, 14), fill);
                        builder.Ellipse(x, y - 9, 17, 5, fill, border, stroke);
                        break;
                    case SequenceArtifactParticipantKind.Collections:
                        for (int i = 2; i >= 0; i--) builder.Rect(new ChartRect(x - 18 + i * 4, y - 13 - i * 3, 30, 26), fill, border, stroke, 2);
                        break;
                    case SequenceArtifactParticipantKind.Queue:
                        builder.Rect(new ChartRect(x - 22, y - 10, 44, 23), fill, border, stroke);
                        for (int i = 0; i < 3; i++) builder.Line(x - 10 + i * 10, y - 10, x - 10 + i * 10, y + 13, border, stroke);
                        builder.Line(x - 22, y + 17, x + 22, y + 17, border, stroke);
                        break;
                }
            }
        }
    }

    private static void MessageArrows(VisualSceneBuilder builder, SequenceArtifactMessage message, ChartPoint[] points, VisualThemeColors colors, double stroke, ChartColor messageColor, bool tangentTerminals = false, ChartPoint? centralPoint = null) {
        var kind = message.Kind;
        bool cross = false, reverse = false, arrow = true;
        if (message.Metadata.TryGetValue("mermaid.operator", out var notation)) {
            var half = HalfArrow(builder, notation, points, messageColor, stroke, tangentTerminals);
            if (!half && notation.Length > 0 && notation != "--" && (notation[notation.Length - 1] == '+' || notation[notation.Length - 1] == '-')) notation = notation.Substring(0, notation.Length - 1);
            if (!half) half = HalfArrow(builder, notation, points, messageColor, stroke, tangentTerminals);
            if (half) arrow = false;
            else switch (notation) {
                case "--": arrow = false; break;
                case "-x": case "--x": cross = true; break;
                case "->": case "-->": kind = SequenceArtifactMessageKind.Async; break;
                case "->>": case "-->>": kind = SequenceArtifactMessageKind.Call; break;
                case "-)": case "--)": kind = SequenceArtifactMessageKind.Async; break;
                case "<<->>": case "<<-->>": kind = SequenceArtifactMessageKind.Call; reverse = true; break;
                default: throw new System.NotSupportedException("Unknown sequence message operator: " + notation);
            }
        }
        if (arrow) Arrow(builder, points[points.Length - 2], points[points.Length - 1], kind, messageColor, stroke, cross, tangentTerminals);
        if (reverse) Arrow(builder, points[1], points[0], kind, messageColor, stroke, tangentTerminals: tangentTerminals);
        if (message.Metadata.TryGetValue("mermaid.centralConnection", out var central) && central == "true") {
            double x = points.Length == 4 ? points[1].X : (points[0].X + points[1].X) / 2;
            double y = points.Length == 4 ? (points[1].Y + points[2].Y) / 2 : points[0].Y;
            var center = centralPoint ?? new ChartPoint(x,y);
            builder.Ellipse(center.X, center.Y, 4, 4, colors.Surface, messageColor, stroke, "sequence-central-connection");
        }
    }

    private static System.IDisposable? SequenceTerminalRotation(VisualSceneBuilder builder,ChartPoint from,ChartPoint to,bool enabled) {
        if(!enabled)return null;
        var angle=System.Math.Atan2(to.Y-from.Y,to.X-from.X)*180/System.Math.PI-(to.X>=from.X?0:180);
        return builder.PushRotation(angle,to.X,to.Y);
    }
    private static void Arrow(VisualSceneBuilder builder, ChartPoint from, ChartPoint to, SequenceArtifactMessageKind kind, ChartColor color, double stroke, bool cross = false, bool tangentTerminals = false) {
        double direction = to.X >= from.X ? 1 : -1;
        double x = to.X, y = to.Y;
        using var rotation = SequenceTerminalRotation(builder,from,to,tangentTerminals);
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
