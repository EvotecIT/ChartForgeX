using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

public sealed partial class SequenceArtifact : IVisualRenderable {
    /// <summary>Prepares ordinary sequence participants and call/return messages directly into the shared static scene.</summary>
    /// <remarks>Uses the existing lane/step layout with the fixed common viewport and measured explicit multiline text. Activations, notes, blocks, special participants, asynchronous/event arrows, self messages, host links, and oversized diagrams are outside this initial subset and throw explicitly.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        ValidatePreparedSubset();
        context = VisualDiagramPrimitives.WithFrame(context, Title, Subtitle);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var plot = VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
        var snapshot = Create(Id).WithSize(plot.Width, plot.Height);
        snapshot.Padding = context.Theme.Spacing;
        foreach (var participant in Participants) snapshot.AddParticipant(participant.Id, participant.Label, participant.Kind);
        foreach (var message in Messages) snapshot.AddMessage(message.SourceId, message.TargetId, message.Text, message.LineStyle, message.Kind);
        var layout = SequenceArtifactRendering.SequenceLayout.Calculate(snapshot);
        if (layout.Width > plot.Width + 0.001 || layout.Height > plot.Height + 0.001)
            throw new NotSupportedException($"Prepared sequence requires at least {layout.Width:0.##} x {layout.Height:0.##} logical content pixels; available {plot.Width:0.##} x {plot.Height:0.##}. Enlarge the common layout size or reduce the participants/messages.");
        var colors = context.Theme.Resolve(context.ThemeMode);
        foreach (var participant in layout.Participants) {
            var bounds = new ChartRect(plot.X + participant.BoxX, plot.Y + layout.ParticipantBoxY, participant.BoxWidth, SequenceArtifactRendering.SequenceLayout.ParticipantBoxHeight);
            var centerX = plot.X + participant.CenterX;
            var halfStroke = context.Theme.AxisStrokeWidth / 2;
            VisualDiagramPrimitives.RequireInside(new ChartRect(bounds.X - halfStroke, bounds.Y - halfStroke, bounds.Width + halfStroke * 2, bounds.Height + halfStroke * 2), plot, participant.Participant.Id);
            using (builder.PushGroup(participant.Participant.Id, "sequence-participant", new Dictionary<string, string> { ["data-kind"] = participant.Participant.Kind.ToString() })) {
                builder.Line(centerX, bounds.Bottom, centerX, plot.Bottom - 8, colors.Border, context.Theme.AxisStrokeWidth, "sequence-lifeline", dash: new[] { 6d, 6d });
                builder.Rect(bounds, colors.Surface, colors.Border, context.Theme.AxisStrokeWidth, context.Theme.BarRadius);
                VisualDiagramPrimitives.Text(builder, participant.Participant.Label, new ChartRect(bounds.X + 8, bounds.Y + 4, bounds.Width - 16, bounds.Height - 8),
                    context.Theme.Typography.DataLabelSize, colors.Foreground, 400, "sequence-participant-label");
            }
            builder.AddRegion(new VisualSemanticRegion(participant.Participant.Id, "sequence-participant", bounds, participant.Participant.Label));
        }
        for (var i = 0; i < layout.Messages.Count; i++) {
            var message = layout.Messages[i]; var model = message.Message;
            var x1 = plot.X + message.X1; var x2 = plot.X + message.X2; var y = plot.Y + message.Y;
            var id = Id + "-message-" + i;
            var bounds = new ChartRect(Math.Min(x1, x2), y - 40, Math.Abs(x2 - x1), 48);
            VisualDiagramPrimitives.RequireInside(bounds, plot, id);
            var lineMargin = Math.Max(8, context.Theme.SeriesStrokeWidth / 2);
            VisualDiagramPrimitives.RequireInside(new ChartRect(Math.Min(x1, x2) - lineMargin, y - lineMargin, Math.Abs(x2 - x1) + lineMargin * 2, lineMargin * 2), plot, id + "-stroke");
            using (builder.PushGroup(id, "sequence-message", new Dictionary<string, string> { ["data-source"] = model.SourceId, ["data-target"] = model.TargetId, ["data-kind"] = model.Kind.ToString() })) {
                builder.Line(x1, y, x2, y, colors.Accent, context.Theme.SeriesStrokeWidth,
                    dash: model.LineStyle == SequenceArtifactMessageLineStyle.Dashed ? new[] { 6d, 5d } : null);
                VisualDiagramPrimitives.Arrow(builder, new ChartPoint(x1, y), new ChartPoint(x2, y), colors.Accent, "sequence-arrow");
                VisualDiagramPrimitives.Text(builder, model.Text, new ChartRect(bounds.X + 8, bounds.Y, Math.Max(0, bounds.Width - 16), 32),
                    context.Theme.Typography.DataLabelSize, colors.Foreground, 400, "sequence-message-label");
            }
            builder.AddRegion(new VisualSemanticRegion(id, "sequence-message", bounds, model.Text));
        }
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= string.IsNullOrWhiteSpace(context.Frame.Title) ? Title ?? Id : context.Frame.Title;
        accessibility.Description ??= string.IsNullOrWhiteSpace(context.Frame.Subtitle) ? Subtitle : context.Frame.Subtitle;
        return new PreparedVisual(builder.Build(), accessibility);
    }

    private void ValidatePreparedSubset() {
        if (Participants.Count == 0 || Activations.Count != 0 || Notes.Count != 0 || Blocks.Count != 0 || Branches.Count != 0)
            throw new NotSupportedException("Prepared sequence requires participants and currently excludes activations, notes, blocks, and branches.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var participant in Participants) {
            if (!ids.Add(participant.Id)) throw new InvalidOperationException("Duplicate sequence participant id: " + participant.Id);
            if (participant.Kind != SequenceArtifactParticipantKind.Participant || participant.Href != null)
                throw new NotSupportedException("Prepared sequence supports ordinary participants without host links: " + participant.Id);
        }
        foreach (var message in Messages) {
            if (!ids.Contains(message.SourceId) || !ids.Contains(message.TargetId)) throw new InvalidOperationException("Sequence messages must reference existing participants.");
            if (message.SourceId == message.TargetId || message.ActivatesTarget || message.Deactivates ||
                message.Kind is not (SequenceArtifactMessageKind.Call or SequenceArtifactMessageKind.Return) ||
                message.LineStyle is not (SequenceArtifactMessageLineStyle.Solid or SequenceArtifactMessageLineStyle.Dashed))
                throw new NotSupportedException("Prepared sequence supports interparticipant call/return messages without activation changes.");
        }
    }
}
