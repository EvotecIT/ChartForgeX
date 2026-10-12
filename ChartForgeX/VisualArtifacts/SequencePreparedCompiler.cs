using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Compiles the sequence model directly to native scene primitives and a detached semantic projection.</summary>
internal static partial class SequencePreparedCompiler {
    internal static PreparedVisual Prepare(SequenceArtifact model, VisualRenderContext context) => Prepare(model, context, false);

    internal static PreparedVisual PrepareDefault(SequenceArtifact model) {
        if (model == null) throw new ArgumentNullException(nameof(model));
        var theme = model.Theme;
        double nesting = Math.Max(model.Blocks.Select(b => b.Depth).DefaultIfEmpty(0).Max(), model.Branches.Select(b => b.Depth).DefaultIfEmpty(0).Max());
        double gap = Math.Max(10, theme.Spacing), stroke = Math.Max(2, theme.SeriesStrokeWidth);
        double inset = gap + nesting * 10 + stroke + (model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.Over) ? 32 : 0);
        double noteReserve = (model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.LeftOf) ? 120 + gap : 0)
            + (model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.RightOf) ? 120 + gap : 0);
        double width = Math.Max(model.Width, model.Padding * 2 + inset * 2 + noteReserve + (model.Messages.Any(m => m.SourceId == m.TargetId) ? 70 : 0) + model.Participants.Count * 140);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, Math.Max(model.Height, model.Padding * 2 + 512)), model.Padding),
            theme, model.ThemeMode, frame: new VisualFrame(showLegend: false));
        return Prepare(model, context, true);
    }

    private static PreparedVisual Prepare(SequenceArtifact model, VisualRenderContext context, bool fitContent) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        Validate(model);
        context = VisualDiagramPrimitives.WithFrame(context, model.Title, model.Subtitle);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var plot = VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
        var semantics = VisualArtifactInterchangeMapping.FromPreparedSequence(model, context);
        var layout = SequencePreparedLayout.Calculate(model, semantics, builder, context, plot, fitContent);
        if (fitContent) {
            var resolvedSize = new VisualSize(context.Layout.Size.Width, Math.Max(model.Height, layout.Bottom + model.Padding + Math.Max(2, context.Theme.SeriesStrokeWidth)));
            context = new VisualRenderContext(new VisualLayoutOptions(resolvedSize, model.Padding), context.Theme, context.ThemeMode, context.Frame, context.Font);
            builder = new VisualSceneBuilder(resolvedSize, context.Font);
            VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
            semantics.Width = resolvedSize.Width; semantics.Height = resolvedSize.Height;
        }
        semantics.Extensions["chartforgex.source.width"] = model.Width.ToString("R", CultureInfo.InvariantCulture);
        semantics.Extensions["chartforgex.source.height"] = model.Height.ToString("R", CultureInfo.InvariantCulture);
        var colors = context.Theme.Resolve(context.ThemeMode);
        double size = layout.FontSize, stroke = context.Theme.AxisStrokeWidth;
        foreach (var item in layout.Blocks.OrderBy(i => model.Blocks[i.Index].Depth)) {
            using (Group(builder, item, "sequence-block", model.Blocks[item.Index].Text)) {
                builder.Rect(item.Bounds, null, colors.Border, stroke, role: "sequence-fragment-frame");
                builder.Rect(new ChartRect(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, item.Y), colors.Surface, colors.Border, stroke);
                Text(builder, item.Text, new ChartRect(item.Bounds.X + 8, item.Bounds.Y + 6, item.Bounds.Width - 16, item.Y - 10), size, colors.Foreground);
            }
            Region(builder, item, "sequence-block", model.Blocks[item.Index].Text); AnnotationBounds(semantics, item);
        }
        foreach (var item in layout.Branches) {
            using (Group(builder, item, "sequence-branch", model.Branches[item.Index].Text)) {
                builder.Line(item.Bounds.X, item.Bounds.Y, item.Bounds.Right, item.Bounds.Y, colors.Border, stroke, dash: new[] { 5d, 4d });
                builder.Rect(new ChartRect(item.Bounds.X, item.Bounds.Y + 1, item.Bounds.Width, item.Y - 2), colors.Surface);
                Text(builder, item.Text, new ChartRect(item.Bounds.X + 6, item.Bounds.Y + 5, item.Bounds.Width - 12, item.Y - 8), size, colors.Foreground);
            }
            Region(builder, item, "sequence-branch", model.Branches[item.Index].Text); AnnotationBounds(semantics, item);
        }
        foreach (var item in layout.Participants) {
            var participant = model.Participants[item.Index]; double x = layout.Lanes[participant.Id];
            builder.Line(x, layout.LifelineTop, x, layout.Bottom, colors.Border, stroke, "sequence-lifeline", dash: new[] { 6d, 5d });
        }
        foreach (var item in layout.Activations) {
            using (Group(builder, item, "sequence-activation", item.Text))
                builder.Rect(item.Bounds, colors.Surface, colors.Accent, stroke, role: "sequence-activation-bar");
            Region(builder, item, "sequence-activation", item.Text);
            if (item.Index >= 0) AnnotationBounds(semantics, item);
        }
        foreach (var item in layout.Participants) {
            var participant = model.Participants[item.Index];
            using (Group(builder, item, "sequence-participant", participant.Label)) {
                IDisposable? link = participant.Href == null ? null : builder.PushLink(participant.Href);
                try { Participant(builder, participant.Kind, item, size, colors, context.Theme); }
                finally { link?.Dispose(); }
            }
            Region(builder, item, "sequence-participant", participant.Label);
            var node = semantics.Nodes[item.Index];
            node.X = item.Bounds.X; node.Y = item.Bounds.Y; node.Width = item.Bounds.Width; node.Height = item.Bounds.Height;
        }
        foreach (var item in layout.Messages) {
            var message = model.Messages[item.Index];
            bool self = message.SourceId == message.TargetId;
            double endY = item.Y + (self ? 24 : 0);
            double loopX = Math.Max(item.X1, item.X2) + 56;
            var points = self ? new[] { new ChartPoint(item.X1, item.Y), new ChartPoint(loopX, item.Y), new ChartPoint(loopX, endY), new ChartPoint(item.X2, endY) }
                : new[] { new ChartPoint(item.X1, item.Y), new ChartPoint(item.X2, item.Y) };
            using (Group(builder, item, "sequence-message", message.Text, new Dictionary<string, string> { ["data-source"] = message.SourceId, ["data-target"] = message.TargetId, ["data-kind"] = message.Kind.ToString() })) {
                builder.Path(new ChartPath(points.Select((p, i) => i == 0 ? ChartPathCommand.MoveTo(p.X, p.Y) : ChartPathCommand.LineTo(p.X, p.Y)).ToArray()),
                    stroke: colors.Accent, strokeWidth: context.Theme.SeriesStrokeWidth, role: "sequence-message-line", dash: message.LineStyle == SequenceArtifactMessageLineStyle.Dashed ? new[] { 6d, 5d } : null);
                MessageArrows(builder, message, points, colors, context.Theme.SeriesStrokeWidth);
                Text(builder, item.Text, item.LabelBounds, size, colors.Foreground, center: !self);
            }
            Region(builder, item, "sequence-message", message.Text);
            foreach (var point in points) semantics.Edges[item.Index].ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = point.X, Y = point.Y });
            semantics.Edges[item.Index].ResolvedLabelBounds = item.LabelBounds;
        }
        foreach (var item in layout.Notes) {
            using (Group(builder, item, "sequence-note", model.Notes[item.Index].Text)) {
                var b = item.Bounds; const double fold = 9;
                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(b.X, b.Y), ChartPathCommand.LineTo(b.Right - fold, b.Y), ChartPathCommand.LineTo(b.Right, b.Y + fold), ChartPathCommand.LineTo(b.Right, b.Bottom), ChartPathCommand.LineTo(b.X, b.Bottom) }), colors.Surface, colors.Accent, stroke, close: true);
                builder.Line(b.Right - fold, b.Y, b.Right - fold, b.Y + fold, colors.Accent, stroke);
                builder.Line(b.Right - fold, b.Y + fold, b.Right, b.Y + fold, colors.Accent, stroke);
                Text(builder, item.Text, new ChartRect(b.X + 10, b.Y + 8, b.Width - 24, b.Height - 16), size, colors.Foreground);
            }
            Region(builder, item, "sequence-note", model.Notes[item.Index].Text); AnnotationBounds(semantics, item);
        }
        var accessibility = model.Accessibility.Clone();
        if (string.IsNullOrWhiteSpace(accessibility.Name)) accessibility.Name = !string.IsNullOrWhiteSpace(context.Frame.Title) ? context.Frame.Title
            : !string.IsNullOrWhiteSpace(model.Title) ? model.Title : !string.IsNullOrWhiteSpace(model.Id) ? model.Id : "Sequence";
        if (string.IsNullOrWhiteSpace(accessibility.Description)) accessibility.Description = context.Frame.Subtitle;
        semantics.AccessibleName = accessibility.Name; semantics.AccessibleDescription = accessibility.Description;
        semantics.Language = accessibility.Language; semantics.IsDecorative = accessibility.IsDecorative;
        return new PreparedVisual(builder.Build(), accessibility, semanticInterchange: semantics);
    }

    private static IDisposable Group(VisualSceneBuilder builder, SequencePreparedLayout.Item item, string role, string label, Dictionary<string, string>? metadata = null) {
        metadata ??= new Dictionary<string, string>(); metadata["data-label"] = label;
        return builder.PushGroup(item.Id, role, metadata);
    }
    private static void Region(VisualSceneBuilder builder, SequencePreparedLayout.Item item, string role, string label) => builder.AddRegion(new VisualSemanticRegion(item.Id, role, item.Bounds, label));
    private static void AnnotationBounds(VisualArtifactInterchangeEnvelope envelope, SequencePreparedLayout.Item item) {
        var annotation = envelope.Annotations.First(a => a.Id == item.Id);
        annotation.Extensions["chartforgex.bounds.x"] = item.Bounds.X.ToString("R", CultureInfo.InvariantCulture);
        annotation.Extensions["chartforgex.bounds.y"] = item.Bounds.Y.ToString("R", CultureInfo.InvariantCulture);
        annotation.Extensions["chartforgex.bounds.width"] = item.Bounds.Width.ToString("R", CultureInfo.InvariantCulture);
        annotation.Extensions["chartforgex.bounds.height"] = item.Bounds.Height.ToString("R", CultureInfo.InvariantCulture);
    }
    private static void Text(VisualSceneBuilder builder, string text, ChartRect bounds, double size, ChartColor color, bool center = false) {
        builder.Text(text, center ? bounds.X + bounds.Width / 2 : bounds.X, bounds.Y + builder.TextAscent(size), size, color,
            role: "sequence-label", alignment: center ? TextAlignment.Center : TextAlignment.Left);
    }

    private static void Validate(SequenceArtifact model) {
        if (model.Participants.Count == 0) throw new InvalidOperationException("A sequence requires at least one participant.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var participant in model.Participants) {
            if (!ids.Add(participant.Id)) throw new InvalidOperationException("Duplicate sequence participant: " + participant.Id);
            if (!Enum.IsDefined(typeof(SequenceArtifactParticipantKind), participant.Kind)) throw new ArgumentOutOfRangeException(nameof(model), "Unknown sequence participant kind.");
        }
        foreach (var message in model.Messages) {
            RequireId(message.SourceId); RequireId(message.TargetId);
            if (!Enum.IsDefined(typeof(SequenceArtifactMessageKind), message.Kind) || !Enum.IsDefined(typeof(SequenceArtifactMessageLineStyle), message.LineStyle)) throw new ArgumentOutOfRangeException(nameof(model), "Unknown sequence message notation.");
        }
        foreach (var note in model.Notes) {
            Step(note.StepIndex);
            if (note.ParticipantIds.Count == 0) throw new InvalidOperationException("A sequence note requires participant targets.");
            foreach (var id in note.ParticipantIds) RequireId(id);
            if (!Enum.IsDefined(typeof(SequenceArtifactNotePlacement), note.Placement)) throw new ArgumentOutOfRangeException(nameof(model), "Unknown sequence note placement.");
        }
        foreach (var activation in model.Activations) { RequireId(activation.ParticipantId); Step(activation.StepIndex); }
        foreach (var block in model.Blocks) { Span(block.StartStepIndex, block.EndStepIndex, block.Depth, block.IsEmpty); if (!Enum.IsDefined(typeof(SequenceArtifactBlockKind), block.Kind)) throw new ArgumentOutOfRangeException(nameof(model)); }
        foreach (var branch in model.Branches) { Span(branch.StartStepIndex, branch.EndStepIndex, branch.Depth, branch.IsEmpty); if (!Enum.IsDefined(typeof(SequenceArtifactBlockKind), branch.ParentKind)) throw new ArgumentOutOfRangeException(nameof(model)); }
        void RequireId(string id) { if (!ids.Contains(id)) throw new InvalidOperationException("Sequence target does not exist: " + id); }
        void Step(int step) { if (step > 100000) throw new ArgumentOutOfRangeException(nameof(model), "Sequence step indices must not exceed 100000; negative indices normalize to zero."); }
        void Span(int start, int end, int depth, bool empty) {
            Step(start); Step(end);
            if (depth < 0 || depth > 100) throw new ArgumentOutOfRangeException(nameof(model), "Invalid sequence fragment nesting depth.");
        }
    }
}
