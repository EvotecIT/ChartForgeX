using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class SequencePreparedLayout {
    private sealed class AnnotationEvent {
        internal int Kind, Index, Depth;
        internal long? Order;
    }
    private readonly Dictionary<int, Item> _openBlocks = new(), _openBranches = new();
    private readonly Dictionary<Item, double> _fragmentContent = new();
    private readonly Dictionary<int,double> _activationOccurrenceY=new();

    // Native AddBlock/AddBranch callers keep their established block/branch/note fallback order.
    // Located adapters also supply close occurrences so a note-only branch stays inside its frame.
    private void PlaceAnnotations(SequenceArtifact model, VisualArtifactInterchangeEnvelope semantics,
        VisualSceneBuilder builder, ChartRect plot, int step, double size, double gap, double noteWidth, double boxWidth, ref double y) {
        var margin = Presentation.Layout?.FragmentMargin ?? gap;
        var nestingMargin = Presentation.Layout?.FragmentMargin ?? 10;
        var labelPadding = Presentation.Layout?.FragmentLabelPadding;
        var noteSpacing = Presentation.Layout?.NoteSpacing ?? gap;
        var notePadding = Presentation.Layout?.NoteSpacing;
        var events = new List<AnnotationEvent>();
        for (var index = 0; index < model.Blocks.Count; index++) {
            var block = model.Blocks[index];
            if (Step(block.StartStepIndex) == step) events.Add(new AnnotationEvent { Kind = 0, Index = index, Depth = block.Depth, Order = block.SourceOrder });
            if (block.EndSourceOrder.HasValue && block.CloseStepIndex == step) events.Add(new AnnotationEvent { Kind = -1, Index = index, Order = block.EndSourceOrder });
        }
        for (var index = 0; index < model.Branches.Count; index++) {
            var branch = model.Branches[index];
            if (Step(branch.StartStepIndex) == step) events.Add(new AnnotationEvent { Kind = 1, Index = index, Depth = branch.Depth, Order = branch.SourceOrder });
            if (branch.EndSourceOrder.HasValue && branch.CloseStepIndex == step) events.Add(new AnnotationEvent { Kind = -2, Index = index, Order = branch.EndSourceOrder });
        }
        for (var index = 0; index < model.Notes.Count; index++)
            if (Step(model.Notes[index].StepIndex) == step) events.Add(new AnnotationEvent { Kind = 2, Index = index, Order = model.Notes[index].SourceOrder });
        for(var index=0;index<model.Activations.Count;index++) {
            var activation=model.Activations[index];
            if(activation.SourceOrder.HasValue&&Step(activation.StepIndex)==step)events.Add(new AnnotationEvent { Kind=3,Index=index,Order=activation.SourceOrder });
        }
        foreach (var occurrence in events.OrderBy(item => item.Order ?? long.MaxValue).ThenBy(item => item.Kind).ThenBy(item => item.Depth).ThenBy(item => item.Index)) {
            if (occurrence.Kind < 0) { CloseSourceFragment(model, occurrence.Kind, occurrence.Index, gap, ref y); continue; }
            if (occurrence.Kind == 0) {
                var block = model.Blocks[occurrence.Index];
                var bounds = new ChartRect(plot.X + margin + block.Depth * nestingMargin, y, plot.Width - margin * 2 - block.Depth * nestingMargin * 2, 0);
                if (IsInstant(block.Kind)) {
                    if (block.Kind == SequenceArtifactBlockKind.Reference && block.ParticipantIds.Count > 0) {
                        var selected=block.ParticipantIds.Select(id=>Participants.First(item=>model.Participants[item.Index].Id==id));
                        var left=selected.Min(item=>item.Bounds.Left); var right=selected.Max(item=>item.Bounds.Right);
                        bounds=new ChartRect(left,y,right-left,0);
                    }
                    var instantText=Presentation.Wrap(block.Text,Math.Max(16,bounds.Width-(block.Kind==SequenceArtifactBlockKind.Reference?ReferenceTabWidth(builder,size,bounds.Width, Presentation)+(labelPadding.HasValue?labelPadding.Value*2:24):(labelPadding.HasValue?labelPadding.Value*2:24))),builder,SequenceTextRole.Fragment,size);
                    var height=Math.Max(36,Presentation.Measure(instantText,builder,SequenceTextRole.Fragment,size).Height+(labelPadding.HasValue?labelPadding.Value*2:20));
                    if(block.Kind==SequenceArtifactBlockKind.Reference)height=Math.Max(height,Presentation.Measure("ref",builder,SequenceTextRole.Fragment,size).Height+(labelPadding.HasValue?labelPadding.Value*2:20));
                    Blocks.Add(new Item { Id=Annotation(semantics,VisualArtifactInterchangeAnnotationRole.SequenceBlock,occurrence.Index),
                        Index=occurrence.Index,Text=instantText,Bounds=bounds,Y=height,ClosingY=y+height });
                    y+=height+gap; RecordFragmentContent(y); continue;
                }
                var backgroundOnly = block.Kind == SequenceArtifactBlockKind.Rect && block.BackgroundColor.HasValue;
                var tab = backgroundOnly ? null : AllocateFragmentHeader(block,bounds,builder,size);
                var text = tab?.Title ?? (backgroundOnly ? string.Empty : Presentation.Wrap((block.Kind == SequenceArtifactBlockKind.ParOver ? "par" : block.Kind.ToString().ToLowerInvariant()) + "  " + block.Text, bounds.Width - (labelPadding.HasValue ? labelPadding.Value * 2 : 20), builder, SequenceTextRole.Fragment, size));
                var header = tab?.Height ?? (backgroundOnly ? 0 : Presentation.Measure(text, builder, SequenceTextRole.Fragment, size).Height + (labelPadding.HasValue ? labelPadding.Value * 2 : 14));
                var item = new Item { Id = Annotation(semantics, VisualArtifactInterchangeAnnotationRole.SequenceBlock, occurrence.Index),
                    Index = occurrence.Index, Text = text, Bounds = bounds, Y = header,
                    FragmentTabBounds=tab?.Tab,FragmentTitleBounds=tab?.TitleBounds,FragmentKindText=tab?.Keyword??string.Empty };
                Blocks.Add(item); if (block.SourceOrder.HasValue) _openBlocks.Add(occurrence.Index, item);
                y += header + (block.IsEmpty ? gap : 0);
            } else if (occurrence.Kind == 1) {
                var branch = model.Branches[occurrence.Index];
                var bounds = new ChartRect(plot.X + margin + branch.Depth * nestingMargin + 8, y, plot.Width - margin * 2 - branch.Depth * nestingMargin * 2 - 16, 0);
                var primary = string.Equals(branch.Kind, "Primary", StringComparison.OrdinalIgnoreCase);
                var implicitCaption = primary && model.Blocks.Any(block => block.Kind == branch.ParentKind && block.Depth == branch.Depth
                    && block.StartStepIndex == branch.StartStepIndex && block.Text == branch.Text);
                var caption = primary ? (implicitCaption || branch.Text.Length == 0 ? "" : "[" + branch.Text + "]")
                    : branch.Kind.ToLowerInvariant() + (branch.Text.Length == 0 ? "" : "  [" + branch.Text + "]");
                var text = Presentation.Wrap(caption, bounds.Width - (labelPadding.HasValue ? labelPadding.Value * 2 : 16), builder, SequenceTextRole.Fragment, size);
                var header = text.Length == 0 ? 0 : Presentation.Measure(text, builder, SequenceTextRole.Fragment, size).Height + (labelPadding.HasValue ? labelPadding.Value * 2 : 12);
                var item = new Item { Id = Annotation(semantics, VisualArtifactInterchangeAnnotationRole.SequenceBranch, occurrence.Index),
                    Index = occurrence.Index, Text = text, Bounds = bounds, Y = header };
                Branches.Add(item); if (branch.SourceOrder.HasValue) _openBranches.Add(occurrence.Index, item);
                y += header + (branch.IsEmpty ? gap : 0);
            } else if(occurrence.Kind==2) {
                var note = model.Notes[occurrence.Index];
                var min = note.ParticipantIds.Min(id => Lanes[id]); var max = note.ParticipantIds.Max(id => Lanes[id]);
                var width = note.Placement == SequenceArtifactNotePlacement.Over ? Math.Max(noteWidth, max - min + boxWidth) : noteWidth;
                var x = note.Placement == SequenceArtifactNotePlacement.LeftOf ? min - width - noteSpacing
                    : note.Placement == SequenceArtifactNotePlacement.RightOf ? max + noteSpacing : (min + max - width) / 2;
                var text = Presentation.Wrap(note.Text, width - (notePadding.HasValue ? notePadding.Value * 2 + 14 : 24), builder, SequenceTextRole.Note, size);
                var height = Math.Max(36, Presentation.Measure(text, builder, SequenceTextRole.Note, size).Height + (notePadding.HasValue ? notePadding.Value * 2 : 20));
                Notes.Add(new Item { Id = Annotation(semantics, VisualArtifactInterchangeAnnotationRole.SequenceNote, occurrence.Index),
                    Index = occurrence.Index, Text = text, Bounds = new ChartRect(x, y + 4, width, height) });
                y += height + noteSpacing;
            }
            if(occurrence.Kind==3) { _activationOccurrenceY[occurrence.Index]=y; if(!model.Activations[occurrence.Index].Active)y+=4; }
            RecordFragmentContent(y);
        }
    }

    private void CloseSourceFragment(SequenceArtifact model, int kind, int index, double gap, ref double y) {
        var open = kind == -1 ? _openBlocks : _openBranches;
        if (!open.TryGetValue(index, out var item)) return;
        var content = _fragmentContent.TryGetValue(item, out var recorded) ? recorded : item.Bounds.Y + item.Y;
        var empty = kind == -1 ? model.Blocks[index].IsEmpty : model.Branches[index].IsEmpty;
        var start = kind == -1 ? model.Blocks[index].StartStepIndex : model.Branches[index].StartStepIndex;
        var end = kind == -1 ? model.Blocks[index].EndStepIndex : model.Branches[index].EndStepIndex;
        var baseline = empty ? item.Bounds.Y + item.Y + gap : Ends[End(start, end)] + (kind == -1 ? 4 : 0);
        item.ClosingY = Math.Max(baseline, content + (kind == -1 && !empty ? 4 : 0));
        open.Remove(index); _fragmentContent.Remove(item);
        // Existing message-only fragments already have their trailing gap. Only expanded
        // terminal content needs to reserve additional space before the following message.
        if (kind == -1 && item.ClosingY.Value > y) y = item.ClosingY.Value + 4;
        RecordFragmentContent(item.ClosingY.Value);
    }

    private void RecordFragmentContent(double bottom) {
        foreach (var item in _openBlocks.Values.Concat(_openBranches.Values)) {
            _fragmentContent.TryGetValue(item, out var previous);
            _fragmentContent[item] = Math.Max(previous, bottom);
        }
    }
}
