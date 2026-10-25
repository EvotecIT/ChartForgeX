using System;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualArtifacts;

internal sealed partial class SequencePreparedLayout {
    // Footer occurrences share their existing participant identity; the source participant list is never filtered or duplicated.
    private void PlaceParticipantFooters(SequenceArtifact model, double gap) {
        var margin = Presentation.Layout?.FragmentMargin ?? gap;
        var commonTop = BodyBottom + margin * 2;
        foreach (var participant in Participants) {
            var source = model.Participants[participant.Index];
            var top = source.DestroyedAtMessageIndex is int death
                ? MirroredDeathTop(model, source.Id, death)
                : commonTop;
            ParticipantFooters.Add(new Item { Id = participant.Id, Index = participant.Index, Text = participant.Text,IsFooter=true,
                Bounds = new ChartRect(participant.Bounds.X, top, participant.Bounds.Width, participant.Bounds.Height) });
        }
        foreach (var footer in ParticipantFooters) {
            var source = model.Participants[footer.Index];
            if (source.DestroyedAtMessageIndex is not int death) continue;
            var message = model.Messages[death]; var item = Messages[death]; var self = message.SourceId == message.TargetId;
            if (message.SourceId == source.Id) item.X1 = self || Lanes[message.TargetId] > Lanes[source.Id] ? footer.Bounds.Right : footer.Bounds.Left;
            if (message.TargetId == source.Id) item.X2 = self || Lanes[message.SourceId] > Lanes[source.Id] ? footer.Bounds.Right : footer.Bounds.Left;
            var left = Math.Min(item.Bounds.Left, Math.Min(item.X1, item.X2));
            var right = Math.Max(item.Bounds.Right, self ? Math.Max(footer.Bounds.Right + 8 + item.LabelBounds.Width, Math.Max(item.X1, item.X2) + 56) : Math.Max(item.X1, item.X2));
            item.Bounds = new ChartRect(left, item.Bounds.Top, right - left, item.Bounds.Height);
        }
        if (ParticipantFooters.Count > 0) Bottom = Math.Max(BodyBottom, ParticipantFooters.Max(item => item.Bounds.Bottom) + (Presentation.Layout?.FooterBottomMargin ?? margin));
    }
    private double SelfLabelLeft(SequenceArtifact model, Item item) {
        if (item.NumberBadgeBounds is ChartRect badge) return badge.Right + Presentation.WrapSides;
        if (Presentation.MirrorsParticipants) {
            var footer = ParticipantFooters.FirstOrDefault(candidate => model.Participants[candidate.Index].DestroyedAtMessageIndex == item.Index
                && model.Participants[candidate.Index].Id == model.Messages[item.Index].SourceId);
            if (footer != null) return footer.Bounds.Right + 8;
        }
        return item.Bounds.X + 8;
    }
    private double MirroredDeathTop(SequenceArtifact model, string id, int index) {
        var participant = Participants.First(item => model.Participants[item.Index].Id == id);
        return MessageLifetimeEnd(model, index) - participant.Bounds.Height / 2;
    }
    private double MessageLifetimeEnd(SequenceArtifact model, int index) =>
        Messages[index].Y + (model.Messages[index].SourceId == model.Messages[index].TargetId ? Messages[index].SelfDrop : 0);
    internal double ParticipantLifelineEnd(SequenceArtifact model, Item item) {
        if (Presentation.MirrorsParticipants) return ParticipantFooters.First(footer => footer.Index == item.Index).Bounds.Top;
        var participant = model.Participants[item.Index];
        return participant.DestroyedAtMessageIndex is int death ? MessageLifetimeEnd(model, death) : Bottom;
    }
}
