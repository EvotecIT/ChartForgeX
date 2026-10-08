using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Measured sequence geometry in common-frame coordinates; no export backend participates in layout.</summary>
internal sealed class SequencePreparedLayout {
    internal sealed class Item {
        internal string Id = string.Empty;
        internal string Text = string.Empty;
        internal ChartRect Bounds;
        internal ChartRect LabelBounds;
        internal int Index;
        internal double X1, X2, Y;
    }
    internal readonly List<Item> Participants = new(), Messages = new(), Notes = new(), Blocks = new(), Branches = new(), Activations = new();
    internal readonly Dictionary<string, double> Lanes = new(StringComparer.Ordinal);
    internal double[] Steps = Array.Empty<double>(), Ends = Array.Empty<double>();
    internal double LifelineTop, Bottom;
    internal double FontSize;

    internal static SequencePreparedLayout Calculate(SequenceArtifact model, VisualArtifactInterchangeEnvelope semantics,
        VisualSceneBuilder builder, VisualRenderContext context, ChartRect plot, bool allowHeightOverflow = false) {
        var layout = new SequencePreparedLayout { FontSize = context.Theme.Typography.DataLabelSize };
        double size = layout.FontSize, gap = Math.Max(10, context.Theme.Spacing), stroke = Math.Max(2, context.Theme.SeriesStrokeWidth);
        double nesting = Math.Max(model.Blocks.Select(b => b.Depth).DefaultIfEmpty(0).Max(), model.Branches.Select(b => b.Depth).DefaultIfEmpty(0).Max());
        double inset = gap + nesting * 10 + stroke + (model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.Over) ? 32 : 0);
        double leftNote = model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.LeftOf) ? 120 + gap : 0;
        double rightNote = model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.RightOf) ? 120 + gap : 0;
        bool self = model.Messages.Any(m => m.SourceId == m.TargetId);
        double left = plot.X + inset + leftNote, right = plot.Right - inset - rightNote - (self ? 70 : 0);
        double laneWidth = (right - left) / model.Participants.Count;
        if (laneWidth < 64) throw new NotSupportedException("Prepared sequence participant lanes require a wider common viewport.");
        double boxWidth = Math.Min(160, laneWidth - 12), top = plot.Y + gap + stroke, participantHeight = 0;
        for (int i = 0; i < model.Participants.Count; i++) {
            var participant = model.Participants[i];
            string text = Wrap(participant.Label, boxWidth - 16, builder, size);
            double iconHeight = participant.Kind == SequenceArtifactParticipantKind.Participant ? 0 : 44;
            double height = Math.Max(38, builder.MeasureText(text, size).Height + 16 + iconHeight);
            participantHeight = Math.Max(participantHeight, height);
            double x = left + laneWidth * (i + 0.5);
            layout.Lanes.Add(participant.Id, x);
            layout.Participants.Add(new Item { Id = semantics.Nodes[i].Id, Index = i, Text = text, Bounds = new ChartRect(x - boxWidth / 2, top, boxWidth, height) });
        }
        layout.LifelineTop = top + participantHeight;
        foreach (var participant in layout.Participants) participant.Bounds = new ChartRect(participant.Bounds.X, top, boxWidth, participantHeight);
        int count = model.Messages.Count;
        foreach (var note in model.Notes) count = Math.Max(count, Step(note.StepIndex) + 1);
        foreach (var activation in model.Activations) count = Math.Max(count, Step(activation.StepIndex) + 1);
        foreach (var block in model.Blocks) count = Math.Max(count, End(block.StartStepIndex, block.EndStepIndex) + 1);
        foreach (var branch in model.Branches) count = Math.Max(count, End(branch.StartStepIndex, branch.EndStepIndex) + 1);
        count = Math.Max(1, count);
        layout.Steps = new double[count]; layout.Ends = new double[count];
        double y = layout.LifelineTop + gap * 2;
        for (int step = 0; step < count; step++) {
            foreach (var block in model.Blocks.Select((value, index) => new { value, index }).Where(b => Step(b.value.StartStepIndex) == step).OrderBy(b => b.value.Depth)) {
                var bounds = new ChartRect(plot.X + gap + block.value.Depth * 10, y, plot.Width - gap * 2 - block.value.Depth * 20, 0);
                string text = Wrap(block.value.Kind.ToString().ToLowerInvariant() + "  " + block.value.Text, bounds.Width - 20, builder, size);
                double header = builder.MeasureText(text, size).Height + 14;
                layout.Blocks.Add(new Item { Id = Annotation(semantics, VisualArtifactInterchangeAnnotationRole.SequenceBlock, block.index), Index = block.index, Text = text, Bounds = bounds, Y = header });
                y += header + (block.value.IsEmpty ? gap : 0);
            }
            foreach (var branch in model.Branches.Select((value, index) => new { value, index }).Where(b => Step(b.value.StartStepIndex) == step).OrderBy(b => b.value.Depth)) {
                var bounds = new ChartRect(plot.X + gap + branch.value.Depth * 10 + 8, y, plot.Width - gap * 2 - branch.value.Depth * 20 - 16, 0);
                string text = Wrap(branch.value.Kind.ToLowerInvariant() + (branch.value.Text.Length == 0 ? "" : "  [" + branch.value.Text + "]"), bounds.Width - 16, builder, size);
                double header = builder.MeasureText(text, size).Height + 12;
                layout.Branches.Add(new Item { Id = Annotation(semantics, VisualArtifactInterchangeAnnotationRole.SequenceBranch, branch.index), Index = branch.index, Text = text, Bounds = bounds, Y = header });
                y += header + (branch.value.IsEmpty ? gap : 0);
            }
            foreach (var note in model.Notes.Select((value, index) => new { value, index }).Where(n => Step(n.value.StepIndex) == step)) {
                double min = note.value.ParticipantIds.Min(id => layout.Lanes[id]), max = note.value.ParticipantIds.Max(id => layout.Lanes[id]);
                double width = note.value.Placement == SequenceArtifactNotePlacement.Over ? Math.Max(120, max - min + boxWidth) : 120;
                double x = note.value.Placement == SequenceArtifactNotePlacement.LeftOf ? min - width - gap
                    : note.value.Placement == SequenceArtifactNotePlacement.RightOf ? max + gap : (min + max - width) / 2;
                string text = Wrap(note.value.Text, width - 24, builder, size);
                double height = Math.Max(36, builder.MeasureText(text, size).Height + 20);
                layout.Notes.Add(new Item { Id = Annotation(semantics, VisualArtifactInterchangeAnnotationRole.SequenceNote, note.index), Index = note.index, Text = text, Bounds = new ChartRect(x, y + 4, width, height) });
                y += height + gap;
            }
            layout.Steps[step] = y;
            if (step < model.Messages.Count) {
                var message = model.Messages[step];
                double x1 = layout.Lanes[message.SourceId], x2 = layout.Lanes[message.TargetId];
                double width = x1 == x2 ? Math.Min(laneWidth - 8, 160) : Math.Abs(x2 - x1) - 16;
                string prefix = Number(model, step);
                string text = Wrap(prefix + message.Text, Math.Max(32, width), builder, size);
                var metrics = builder.MeasureText(text, size);
                double height = metrics.Height;
                double lineY = y + height + 12;
                bool loop = x1 == x2;
                layout.Messages.Add(new Item { Id = semantics.Edges[step].Id, Index = step, Text = text, X1 = x1, X2 = x2, Y = lineY,
                    LabelBounds = new ChartRect(0, y, metrics.Width, metrics.Height),
                    Bounds = new ChartRect(loop ? x1 : Math.Min(x1, x2), y, loop ? width + 8 : Math.Abs(x2 - x1), height + (loop ? 42 : 20)) });
                y = lineY + (loop ? 34 : 12) + gap;
            } else y += gap * 2;
            layout.Ends[step] = y;
            int ending = model.Blocks.Count(b => !b.IsEmpty && End(b.StartStepIndex, b.EndStepIndex) == step);
            y += ending * 8;
        }
        layout.Bottom = y + gap;
        foreach (var item in layout.Blocks) {
            var block = model.Blocks[item.Index];
            double bottom = block.IsEmpty ? item.Bounds.Y + item.Y + gap : layout.Ends[End(block.StartStepIndex, block.EndStepIndex)] + 4;
            item.Bounds = new ChartRect(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, bottom - item.Bounds.Y);
        }
        foreach (var item in layout.Branches) {
            var branch = model.Branches[item.Index];
            double bottom = branch.IsEmpty ? item.Bounds.Y + item.Y + gap : layout.Ends[End(branch.StartStepIndex, branch.EndStepIndex)];
            item.Bounds = new ChartRect(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, Math.Max(item.Y, bottom - item.Bounds.Y));
        }
        if (layout.Bottom > plot.Bottom - stroke && !allowHeightOverflow) throw new NotSupportedException("Prepared sequence requires at least " + (layout.Bottom - plot.Y + gap).ToString("0.##", CultureInfo.InvariantCulture) + " logical content pixels of height; enlarge the common viewport.");
        if (allowHeightOverflow) plot = new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(plot.Height, layout.Bottom - plot.Y + stroke));
        foreach (var item in layout.Participants.Concat(layout.Messages).Concat(layout.Notes).Concat(layout.Blocks).Concat(layout.Branches))
            VisualDiagramPrimitives.RequireInside(item.Bounds, plot, item.Id);
        layout.LayoutActivations(model, semantics);
        foreach (var item in layout.Messages) {
            bool loop = model.Messages[item.Index].SourceId == model.Messages[item.Index].TargetId;
            double x = loop ? item.Bounds.X + 8 : item.Bounds.X + (item.Bounds.Width - item.LabelBounds.Width) / 2;
            item.LabelBounds = new ChartRect(x, item.LabelBounds.Y, item.LabelBounds.Width, item.LabelBounds.Height);
        }
        foreach (var item in layout.Activations) VisualDiagramPrimitives.RequireInside(item.Bounds, plot, item.Id);
        foreach (var item in layout.Messages) VisualDiagramPrimitives.RequireInside(item.Bounds, plot, item.Id);
        return layout;
    }

    private static string Annotation(VisualArtifactInterchangeEnvelope envelope, VisualArtifactInterchangeAnnotationRole role, int index) =>
        envelope.Annotations.Where(a => a.Role == role).ElementAt(index).Id;

    private static int Step(int index) => Math.Max(0, index);
    private static int End(int start, int end) => Math.Max(Step(start), end);

    private static string Number(SequenceArtifact model, int index) {
        if (!model.Metadata.TryGetValue("mermaid.autonumber", out var enabled) || enabled != "true") return string.Empty;
        decimal start = 1, increment = 1;
        if (model.Metadata.TryGetValue("mermaid.autonumber.start", out var startText) && !decimal.TryParse(startText, NumberStyles.Float, CultureInfo.InvariantCulture, out start))
            throw new NotSupportedException("Sequence autonumber start must be a finite decimal number.");
        if (model.Metadata.TryGetValue("mermaid.autonumber.increment", out var incrementText) && !decimal.TryParse(incrementText, NumberStyles.Float, CultureInfo.InvariantCulture, out increment))
            throw new NotSupportedException("Sequence autonumber increment must be a finite decimal number.");
        return checked(start + index * increment).ToString("0.############################", CultureInfo.InvariantCulture) + ". ";
    }

    internal static string Wrap(string text, double width, VisualSceneBuilder builder, double size) {
        var lines = new List<string>();
        foreach (string paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
            string line = string.Empty;
            var elements = StringInfo.GetTextElementEnumerator(paragraph);
            while (elements.MoveNext()) {
                string element = elements.GetTextElement();
                if (builder.MeasureText(line + element, size).Width > width && line.Length > 0) { lines.Add(line); line = string.Empty; }
                if (builder.MeasureText(element, size).Width > width) throw new NotSupportedException("A sequence text glyph exceeds the available lane width.");
                line += element;
            }
            lines.Add(line);
        }
        return string.Join("\n", lines);
    }

    private void LayoutActivations(SequenceArtifact model, VisualArtifactInterchangeEnvelope envelope) {
        var open = new Dictionary<string, Stack<Item>>(StringComparer.Ordinal);
        foreach (var id in Lanes.Keys) open.Add(id, new Stack<Item>());
        for (int step = 0; step < Steps.Length; step++) {
            foreach (var activation in model.Activations.Select((value, index) => new { value, index }).Where(a => Step(a.value.StepIndex) == step))
                Change(activation.value.ParticipantId, activation.value.Active, Steps[step], Annotation(envelope, VisualArtifactInterchangeAnnotationRole.SequenceActivation, activation.index), activation.index);
            if (step < model.Messages.Count) {
                var message = model.Messages[step]; var item = Messages[step];
                int sourceDepth = open[message.SourceId].Count;
                if (message.ActivatesTarget) Change(message.TargetId, true, item.Y + (message.SourceId == message.TargetId ? 24 : 0), item.Id + "-activation", -1);
                int targetDepth = open[message.TargetId].Count;
                bool self = message.SourceId == message.TargetId;
                double sourceLane = Lanes[message.SourceId], targetLane = Lanes[message.TargetId];
                item.X1 = sourceLane + (sourceDepth == 0 ? 0 : (sourceDepth - 1) * 5 + (self || targetLane > sourceLane ? 5 : -5));
                item.X2 = targetLane + (targetDepth == 0 ? 0 : (targetDepth - 1) * 5 + (self || targetLane < sourceLane ? 5 : -5));
                double left = Math.Min(item.Bounds.X, Math.Min(item.X1, item.X2));
                double right = Math.Max(item.Bounds.Right, self ? Math.Max(item.X1, item.X2) + 56 : Math.Max(item.X1, item.X2));
                item.Bounds = new ChartRect(left, item.Bounds.Y, right - left, item.Bounds.Height);
                if (message.Deactivates) Change(message.SourceId, false, item.Y, item.Id + "-deactivation", -1);
            }
        }
        foreach (var stack in open.Values) while (stack.Count > 0) Finish(stack.Pop(), Bottom);
        void Change(string id, bool active, double y, string sourceId, int index) {
            var stack = open[id];
            if (active) {
                double spacing = Lanes.Values.Where(x => x != Lanes[id]).Select(x => Math.Abs(x - Lanes[id])).DefaultIfEmpty(160).Min();
                if (stack.Count * 5 > spacing / 3) throw new NotSupportedException("Sequence activation nesting exceeds the available participant lane width.");
                stack.Push(new Item { Id = sourceId, Index = index, Bounds = new ChartRect(Lanes[id] - 5 + stack.Count * 5, y, 10, 0), Text = "activate " + id });
            }
            else {
                if (stack.Count == 0) throw new InvalidOperationException("Sequence deactivation has no matching activation: " + id);
                var item = stack.Pop(); Finish(item, y);
                Activations.Add(new Item { Id = sourceId, Index = index, Bounds = new ChartRect(item.Bounds.X, y - 2, 10, 4), Text = "deactivate " + id });
            }
        }
        void Finish(Item item, double y) {
            item.Bounds = new ChartRect(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, Math.Max(4, y - item.Bounds.Y));
            Activations.Add(item);
        }
    }
}
