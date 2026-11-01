using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Measured sequence geometry in common-frame coordinates; no export backend participates in layout.</summary>
internal sealed partial class SequencePreparedLayout {
    internal sealed class Item {
        internal string Id = string.Empty;
        internal string Text = string.Empty;
        internal string NumberText = string.Empty;
        internal double NumberWidth;
        internal double NumberBadgeRadius;
        internal ChartRect? NumberBadgeBounds, NumberTextBounds;
        internal double SelfDrop = 24;
        internal double? PresentedLoopX;
        internal SequenceSelfRoutePlan? SelfRoute;
        internal CapturedSequenceRoute? CapturedRoute;
        internal ChartRect Bounds;
        internal ChartRect LabelBounds;
        internal ChartRect? FragmentTabBounds, FragmentTitleBounds;
        internal string FragmentKindText=string.Empty;
        internal int Index;
        internal string? ActivationParticipantId;
        internal bool IsFooter;
        internal SequenceParticipantBandAllocation? BandAllocation;
        internal double X1, X2, Y;
        internal double? ClosingY;
    }
    internal readonly List<Item> Participants = new(), Messages = new(), Notes = new(), Blocks = new(), Branches = new(), Activations = new(), ParticipantGroups = new(), ParticipantFooters = new();
    internal readonly Dictionary<string, double> Lanes = new(StringComparer.Ordinal);
    internal double[] Steps = Array.Empty<double>(), Ends = Array.Empty<double>();
    internal double LifelineTop, Bottom, BodyBottom;
    internal double FontSize;
    internal SequencePreparedPresentation Presentation = null!;

    internal static SequencePreparedLayout Calculate(SequenceArtifact model, VisualArtifactInterchangeEnvelope semantics,
        VisualSceneBuilder builder, VisualRenderContext context, ChartRect plot, bool allowHeightOverflow = false) {
        var layout = new SequencePreparedLayout { FontSize = context.Theme.Typography.DataLabelSize, Presentation = new SequencePreparedPresentation(model, context) };
        double size = layout.FontSize, gap = Math.Max(10, context.Theme.Spacing), stroke = Math.Max(2, context.Theme.SeriesStrokeWidth);
        var policy = layout.Presentation.Layout;
        double minimumLabelWidth = layout.Presentation.MinimumLabelWidth(model, builder, size), noteWidth = layout.Presentation.NoteWidth(minimumLabelWidth);
        double participantSpacing = policy?.ParticipantSpacing ?? 12, wrapSides = layout.Presentation.WrapSides;
        double fragmentMargin = policy?.FragmentMargin ?? gap, nestingMargin = policy?.FragmentMargin ?? 10;
        double noteSpacing = policy?.NoteSpacing ?? gap;
        double nesting = Math.Max(model.Blocks.Select(b => b.Depth).DefaultIfEmpty(0).Max(), model.Branches.Select(b => b.Depth).DefaultIfEmpty(0).Max());
        double inset = fragmentMargin + nesting * nestingMargin + stroke + (model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.Over) ? 32 : 0);
        double leftNote = model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.LeftOf) ? noteWidth + noteSpacing : 0;
        double rightNote = model.Notes.Any(n => n.Placement == SequenceArtifactNotePlacement.RightOf) ? noteWidth + noteSpacing : 0;
        bool self = model.Messages.Any(m => m.SourceId == m.TargetId);
        double left = plot.X + inset + leftNote, right = plot.Right - inset - rightNote - (self ? layout.Presentation.SelfMessageReserve(model, minimumLabelWidth) : 0);
        var visible = layout.Presentation.VisibleParticipantIndices;
        double laneWidth = (right - left) / Math.Max(1, visible.Count);
        if (laneWidth < 64) throw new NotSupportedException("Prepared sequence participant lanes require a wider common viewport.");
        double boxWidth = Math.Min(Math.Max(policy?.ParticipantMinimumWidth ?? 160, minimumLabelWidth + wrapSides * 2), laneWidth - participantSpacing), top = plot.Y + gap + stroke, participantHeight = 0;
        double groupsTop = top;
        top += layout.MeasureParticipantGroups(model, semantics, builder, laneWidth, left, top, size);
        for (int column = 0; column < visible.Count; column++) {
            var i = visible[column]; var participant = model.Participants[i];
            var participantWidth = Math.Max(boxWidth, participant.MinimumWidth ?? 0);
            if (participantWidth > laneWidth - participantSpacing + 1e-8 || policy?.ParticipantMinimumWidth > participantWidth + 1e-8) throw new NotSupportedException("The explicit context cannot fit the authored participant minimum width.");
            var card = participant.Card != null && participant.Kind == SequenceArtifactParticipantKind.Participant;
            var textWidth = card ? Math.Max(24, participantWidth - SequencePreparedCompiler.CardTextLeft - SequencePreparedCompiler.CardTextRight) : participantWidth - wrapSides * 2;
            string text = layout.Presentation.Wrap(ParticipantCaption(participant), textWidth, builder, SequenceTextRole.Participant, size);
            var stereotypeHeight=string.IsNullOrEmpty(participant.Stereotype) ? 0 : layout.Presentation.Measure(layout.Presentation.Wrap("«"+participant.Stereotype+"»", participantWidth-wrapSides*2, builder, SequenceTextRole.Participant, size, .85), builder, SequenceTextRole.Participant, size, .85).Height+4;
            double iconHeight = participant.Kind == SequenceArtifactParticipantKind.Participant ? 0 : 44;
            double height = policy?.ParticipantBands is {} bands
                ? Math.Max(policy.ParticipantMinimumHeight??38,bands.StackHeight(layout.Presentation.Measure(text,builder,SequenceTextRole.Participant,size).Height+stereotypeHeight))
                : Math.Max(policy?.ParticipantMinimumHeight ?? 38, layout.Presentation.Measure(text, builder, SequenceTextRole.Participant, size).Height + 16 + iconHeight + stereotypeHeight);
            if (card && policy?.ParticipantBands == null) {
                var subtitle = string.IsNullOrWhiteSpace(participant.Card!.Subtitle) ? 0
                    : layout.Presentation.Measure(layout.Presentation.Wrap(participant.Card.Subtitle!.Trim(), textWidth, builder, SequenceTextRole.Participant, size, SequencePreparedCompiler.CardSubtitleRatio), builder, SequenceTextRole.Participant, size, SequencePreparedCompiler.CardSubtitleRatio).Height + 2;
                height = Math.Max(Math.Max(policy?.ParticipantMinimumHeight ?? 38, 48), layout.Presentation.Measure(text, builder, SequenceTextRole.Participant, size).Height + subtitle + 18);
            }
            participantHeight = Math.Max(participantHeight, height);
            double x = left + laneWidth * (column + 0.5);
            layout.Lanes.Add(participant.Id, x);
            layout.Participants.Add(new Item { Id = semantics.Nodes[i].Id, Index = i, Text = text, Bounds = new ChartRect(x - participantWidth / 2, top, participantWidth, height) });
        }
        layout.LifelineTop = top + participantHeight;
        foreach (var participant in layout.Participants) participant.Bounds = new ChartRect(participant.Bounds.X, top, participant.Bounds.Width, participantHeight);
        var births = layout.Participants.Where(item => model.Participants[item.Index].CreatedAtMessageIndex.HasValue)
            .ToLookup(item => model.Participants[item.Index].CreatedAtMessageIndex!.Value);
        int count = model.Messages.Count;
        foreach (var note in model.Notes) count = Math.Max(count, Step(note.StepIndex) + 1);
        foreach (var activation in model.Activations) count = Math.Max(count, Step(activation.StepIndex) + 1);
        foreach (var block in model.Blocks) count = Math.Max(count, End(block.StartStepIndex, block.EndStepIndex) + 1);
        foreach (var branch in model.Branches) count = Math.Max(count, End(branch.StartStepIndex, branch.EndStepIndex) + 1);
        count = Math.Max(1, count);
        layout.Steps = new double[count]; layout.Ends = new double[count];
        double y = layout.LifelineTop + gap * 2;
        for (int step = 0; step < count; step++) {
            layout.PlaceAnnotations(model, semantics, builder, plot, step, size, gap, noteWidth, boxWidth, ref y);
            layout.Steps[step] = y;
            if (step < model.Messages.Count) {
                foreach (var participant in births[step])
                    participant.Bounds = new ChartRect(participant.Bounds.X, y, participant.Bounds.Width, participant.Bounds.Height);
                if (births[step].Any()) y += participantHeight + gap;
                var message = model.Messages[step];
                double x1 = layout.Lanes[message.SourceId], x2 = layout.Lanes[message.TargetId];
                double width = x1 == x2 ? Math.Min(laneWidth - 8, Math.Max(160, minimumLabelWidth)) : Math.Abs(x2 - x1) - wrapSides * 2;
                var terminalHalfHeight = 0d;
                if (layout.Presentation.MirrorsParticipants) {
                    var dying = layout.Participants.Where(item => model.Participants[item.Index].DestroyedAtMessageIndex == step).ToArray();
                    terminalHalfHeight = dying.Select(item => item.Bounds.Height / 2).DefaultIfEmpty(0).Max();
                    if (x1 != x2) {
                        width -= dying.Sum(item => item.Bounds.Width / 2);
                        if (width <= 0) throw new NotSupportedException("Mirrored terminal headers require wider native participant lanes for the message caption.");
                    }
                }
                string prefix = Number(model, step);
                var badgeLabel = layout.Presentation.UsesNumberBadges ? SequencePreparedPresentation.NumberLabel(prefix) : string.Empty;
                var badgeRadius = badgeLabel.Length == 0 ? 0 : layout.Presentation.BadgeRadius(badgeLabel, builder);
                var badgeGutter = badgeRadius == 0 ? 0 : badgeRadius * 2 + layout.Presentation.BadgeClearance * 2;
                SequenceSelfRoutePlan? selfRoute = null;
                var selfDrop = message.SourceId == message.TargetId ? SequenceSelfRoutePlan.ReturnDrop(policy,badgeRadius == 0 ? null : badgeRadius * 2 + wrapSides) : 24;
                if (badgeGutter > 0 && message.SourceId != message.TargetId && width - badgeGutter < minimumLabelWidth)
                    throw new NotSupportedException("The explicit native message span cannot contain its number badge and complete caption.");
                if (badgeGutter > 0 && message.SourceId == message.TargetId)
                    width = Math.Min(laneWidth - 8, Math.Max(160, minimumLabelWidth + badgeGutter + wrapSides * 2));
                bool separateNumber = !layout.Presentation.UsesNumberBadges && layout.Presentation.SeparateNumber && prefix.Length > 0;
                var numberWidth = separateNumber ? layout.Presentation.Measure(prefix, builder, SequenceTextRole.Number, size).Width : 0;
                string text = layout.Presentation.Wrap(separateNumber || badgeLabel.Length > 0 ? message.Text : prefix + message.Text,
                    Math.Max(32, width) - numberWidth - badgeGutter, builder, SequenceTextRole.Message, size);
                var metrics = layout.Presentation.Measure(text, builder, SequenceTextRole.Message, size);
                double height = separateNumber ? Math.Max(metrics.Height, layout.Presentation.Measure(prefix, builder, SequenceTextRole.Number, size).Height) : metrics.Height;
                if (message.SourceId == message.TargetId) selfRoute = SequenceSelfRoutePlan.Create(policy,
                    layout.Participants.First(item => model.Participants[item.Index].Id == message.SourceId).Bounds.Width,
                    metrics.Width, badgeRadius == 0 ? null : badgeRadius * 2 + wrapSides);
                var rise = selfRoute?.MinimumY ?? 0;
                double lineY = y + Math.Max(Math.Max(height + 12 - rise, terminalHalfHeight), badgeRadius);
                bool loop = x1 == x2;
                layout.Messages.Add(new Item { Id = semantics.Edges[step].Id, Index = step, Text = text, NumberText = badgeLabel.Length > 0 ? badgeLabel : separateNumber ? prefix : string.Empty, NumberWidth = numberWidth, NumberBadgeRadius = badgeRadius, SelfDrop = selfDrop, SelfRoute = selfRoute, X1 = x1, X2 = x2, Y = lineY,
                    LabelBounds = new ChartRect(0, y, metrics.Width + numberWidth, height),
                    Bounds = new ChartRect(loop ? x1 : Math.Min(x1, x2), y, loop ? width + 8 : Math.Abs(x2 - x1), badgeRadius > 0 ? Math.Max(height + (loop ? selfDrop + 18 : 20), lineY - y + badgeRadius) : height + (loop ? 42 : 20)) });
                y = lineY + Math.Max(Math.Max(loop ? (selfRoute?.MaximumY + 10 ?? 34) : 12, terminalHalfHeight + (loop && terminalHalfHeight > 0 ? selfDrop : 0)), badgeRadius) + (policy?.MessageSpacing ?? gap);
            } else y += gap * 2;
            layout.RecordFragmentContent(y);
            layout.Ends[step] = y;
            int ending = model.Blocks.Count(b => !b.IsEmpty && End(b.StartStepIndex, b.EndStepIndex) == step);
            y += ending * 8;
        }
        layout.PlaceAnnotations(model, semantics, builder, plot, count, size, gap, noteWidth, boxWidth, ref y);
        layout.BodyBottom = layout.Bottom = y + gap;
        foreach (var item in layout.ParticipantGroups) item.Bounds = new ChartRect(item.Bounds.X, groupsTop, item.Bounds.Width, layout.Bottom - groupsTop);
        foreach (var item in layout.Blocks) {
            var block = model.Blocks[item.Index];
            double bottom = item.ClosingY ?? (block.IsEmpty ? item.Bounds.Y + item.Y + gap : layout.Ends[End(block.StartStepIndex, block.EndStepIndex)] + 4);
            item.Bounds = new ChartRect(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, bottom - item.Bounds.Y);
        }
        foreach (var item in layout.Branches) {
            var branch = model.Branches[item.Index];
            double bottom = item.ClosingY ?? (branch.IsEmpty ? item.Bounds.Y + item.Y + gap : layout.Ends[End(branch.StartStepIndex, branch.EndStepIndex)]);
            item.Bounds = new ChartRect(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, Math.Max(item.Y, bottom - item.Bounds.Y));
        }
        if (layout.Bottom > plot.Bottom - stroke && !allowHeightOverflow) throw new NotSupportedException("Prepared sequence requires at least " + (layout.Bottom - plot.Y + gap).ToString("0.##", CultureInfo.InvariantCulture) + " logical content pixels of height; enlarge the common viewport.");
        if (allowHeightOverflow) plot = new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(plot.Height, layout.Bottom - plot.Y + stroke));
        foreach (var item in layout.Participants.Concat(layout.Messages).Concat(layout.Notes).Concat(layout.Blocks).Concat(layout.Branches).Concat(layout.ParticipantGroups))
            VisualDiagramPrimitives.RequireInside(item.Bounds, plot, item.Id);
        layout.LayoutActivations(model, semantics);
        if (layout.Presentation.MirrorsParticipants) {
            layout.PlaceParticipantFooters(model, gap);
            if (layout.Bottom > plot.Bottom - stroke && !allowHeightOverflow)
                throw new NotSupportedException("Mirrored sequence participants require a taller explicit native viewport.");
            if (allowHeightOverflow) plot = new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(plot.Height, layout.Bottom - plot.Y + stroke));
            foreach (var item in layout.ParticipantFooters) VisualDiagramPrimitives.RequireInside(item.Bounds, plot, item.Id);
            foreach (var item in layout.ParticipantGroups) item.Bounds = new ChartRect(item.Bounds.X, groupsTop, item.Bounds.Width, layout.Bottom - groupsTop);
        }
        layout.CaptureParticipantBands(model,builder);
        layout.PlaceMessagePresentation(model, builder, plot);
        layout.CaptureSelfRoutes(plot);
        foreach (var item in layout.Messages) {
            bool loop = model.Messages[item.Index].SourceId == model.Messages[item.Index].TargetId;
            double x = loop ? layout.SelfLabelLeft(model, item) : (layout.Presentation.MirrorsParticipants || layout.Presentation.UsesNumberBadges)
                ? Math.Min(item.X1, item.X2) + (Math.Abs(item.X2 - item.X1) - item.LabelBounds.Width) / 2
                : item.Bounds.X + (item.Bounds.Width - item.LabelBounds.Width) / 2;
            x = layout.AlignedMessageLeft(item, x, loop);
            item.LabelBounds = new ChartRect(x, item.LabelBounds.Y, item.LabelBounds.Width, item.LabelBounds.Height);
            if (item.NumberBadgeBounds.HasValue || policy?.MessageAlignment != null) layout.CaptureMessageFootprint(item);
        }
        foreach (var item in layout.Activations) VisualDiagramPrimitives.RequireInside(item.Bounds, plot, item.Id);
        foreach (var item in layout.Messages) VisualDiagramPrimitives.RequireInside(item.Bounds, plot, item.Id);
        return layout;
    }

    private static string Annotation(VisualArtifactInterchangeEnvelope envelope, VisualArtifactInterchangeAnnotationRole role, int index) =>
        envelope.Annotations.Where(a => a.Role == role).ElementAt(index).Id;

    private static int Step(int index) => Math.Max(0, index);
    private static int End(int start, int end) => Math.Max(Step(start), end);

    internal static string Number(SequenceArtifact model, int index) {
        var message = model.Messages[index];
        if (message.Metadata.TryGetValue("mermaid.autonumber.visible", out var visible)) {
            if (visible == "false") return string.Empty;
            if (visible != "true" || !message.Metadata.TryGetValue("mermaid.autonumber.value", out var text)
                || !decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new NotSupportedException("Sequence occurrence numbering requires a finite decimal value and boolean visibility.");
            return value.ToString("0.############################", CultureInfo.InvariantCulture) + ". ";
        }
        if (!model.Metadata.TryGetValue("mermaid.autonumber", out var enabled) || enabled != "true") return string.Empty;
        decimal start = 1, increment = 1;
        if (model.Metadata.TryGetValue("mermaid.autonumber.start", out var startText) && !decimal.TryParse(startText, NumberStyles.Float, CultureInfo.InvariantCulture, out start))
            throw new NotSupportedException("Sequence autonumber start must be a finite decimal number.");
        if (model.Metadata.TryGetValue("mermaid.autonumber.increment", out var incrementText) && !decimal.TryParse(incrementText, NumberStyles.Float, CultureInfo.InvariantCulture, out increment))
            throw new NotSupportedException("Sequence autonumber increment must be a finite decimal number.");
        return checked(start + index * increment).ToString("0.############################", CultureInfo.InvariantCulture) + ". ";
    }

    internal static string Wrap(string text, double width, VisualSceneBuilder builder, double size) => WrapCore(text, width, builder, size, null, true);
    internal static string Wrap(string text, double width, VisualSceneBuilder builder, ChartForgeX.Typography.TextStyle style, bool wrap) =>
        WrapCore(text, width, builder, style.EffectiveFontSize, style, wrap);

    private static string WrapCore(string text, double width, VisualSceneBuilder builder, double size, ChartForgeX.Typography.TextStyle? style, bool wrap) {
        var lines = new List<string>();
        foreach (string paragraph in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
            if (!wrap) {
                if (Measure(paragraph) > width + 1e-8) throw new NotSupportedException("An unwrapped sequence line exceeds the explicit native allocation; enlarge its viewport or minima.");
                lines.Add(paragraph); continue;
            }
            string line = string.Empty;
            var elements = StringInfo.GetTextElementEnumerator(paragraph);
            while (elements.MoveNext()) {
                string element = elements.GetTextElement();
                if (Measure(line + element) > width && line.Length > 0) { lines.Add(line); line = string.Empty; }
                if (Measure(element) > width) throw new NotSupportedException("A sequence text glyph exceeds the available lane width.");
                line += element;
            }
            lines.Add(line);
        }
        return string.Join("\n", lines);
        double Measure(string value) => style == null ? builder.MeasureText(value, size).Width : builder.MeasureText(value, style).Width;
    }

    private void LayoutActivations(SequenceArtifact model, VisualArtifactInterchangeEnvelope envelope) {
        var activationWidth = Presentation.Layout?.ActivationWidth ?? 10;
        var halfWidth = activationWidth / 2;
        var open = new Dictionary<string, Stack<Item>>(StringComparer.Ordinal);
        var deaths = model.Participants.Where(participant => participant.DestroyedAtMessageIndex.HasValue)
            .ToLookup(participant => participant.DestroyedAtMessageIndex!.Value);
        foreach (var id in Lanes.Keys) open.Add(id, new Stack<Item>());
        for (int step = 0; step < Steps.Length; step++) {
            foreach (var activation in model.Activations.Select((value, index) => new { value, index }).Where(a => Step(a.value.StepIndex) == step))
                Change(activation.value.ParticipantId, activation.value.Active, _activationOccurrenceY.TryGetValue(activation.index,out var occurrenceY)?occurrenceY:Steps[step], Annotation(envelope, VisualArtifactInterchangeAnnotationRole.SequenceActivation, activation.index), activation.index);
            if (step < model.Messages.Count) {
                var message = model.Messages[step]; var item = Messages[step];
                int sourceDepth = open[message.SourceId].Count;
                if (message.ActivatesTarget && Presentation.MirrorsParticipants && model.Participants.First(item => item.Id == message.TargetId).DestroyedAtMessageIndex == step)
                    throw new NotSupportedException("A mirrored terminal participant cannot start an activation at the destroying message.");
                if (message.ActivatesTarget) Change(message.TargetId, true, item.Y + (message.SourceId == message.TargetId ? item.SelfDrop : 0), item.Id + "-activation", -1);
                int targetDepth = open[message.TargetId].Count;
                bool self = message.SourceId == message.TargetId;
                double sourceLane = Lanes[message.SourceId], targetLane = Lanes[message.TargetId];
                item.X1 = sourceLane + (sourceDepth == 0 ? 0 : (sourceDepth - 1) * halfWidth + (self || targetLane > sourceLane ? halfWidth : -halfWidth));
                item.X2 = targetLane + (targetDepth == 0 ? 0 : (targetDepth - 1) * halfWidth + (self || targetLane < sourceLane ? halfWidth : -halfWidth));
                double left = Math.Min(item.Bounds.X, Math.Min(item.X1, item.X2));
                double right = Math.Max(item.Bounds.Right, self ? Math.Max(item.X1, item.X2) + 56 : Math.Max(item.X1, item.X2));
                item.Bounds = new ChartRect(left, item.Bounds.Y, right - left, item.Bounds.Height);
                if (message.Deactivates) {
                    var source = model.Participants.First(participant => participant.Id == message.SourceId);
                    Change(message.SourceId, false, Presentation.MirrorsParticipants && source.DestroyedAtMessageIndex == step
                        ? MirroredDeathTop(model, source.Id, step) : item.Y, item.Id + "-deactivation", -1);
                }
                foreach (var participant in deaths[step]) {
                    var stack = open[participant.Id];
                    while (stack.Count > 0) Finish(stack.Pop(), Presentation.MirrorsParticipants ? MirroredDeathTop(model, participant.Id, step) : item.Y + (self ? item.SelfDrop : 0));
                }
            }
        }
        foreach (var stack in open.Values) while (stack.Count > 0) Finish(stack.Pop(), Bottom);
        void Change(string id, bool active, double y, string sourceId, int index) {
            var stack = open[id];
            if (active) {
                double spacing = Lanes.Values.Where(x => x != Lanes[id]).Select(x => Math.Abs(x - Lanes[id])).DefaultIfEmpty(160).Min();
                if (stack.Count * halfWidth > spacing / 3) throw new NotSupportedException("Sequence activation nesting exceeds the available participant lane width.");
                stack.Push(new Item { Id = sourceId, Index = index, Bounds = new ChartRect(Lanes[id] - halfWidth + stack.Count * halfWidth, y, activationWidth, 0), ActivationParticipantId=id,Text = "activate " + id });
            }
            else {
                if (stack.Count == 0) throw new InvalidOperationException("Sequence deactivation has no matching activation: " + id);
                var item = stack.Pop(); Finish(item, y);
                Activations.Add(new Item { Id = sourceId, Index = index, Bounds = new ChartRect(item.Bounds.X, y - 2, activationWidth, 4), ActivationParticipantId=id,Text = "deactivate " + id });
            }
        }
        void Finish(Item item, double y) {
            item.Bounds = new ChartRect(item.Bounds.X, item.Bounds.Y, item.Bounds.Width, Math.Max(4, y - item.Bounds.Y));
            Activations.Add(item);
        }
    }
}
