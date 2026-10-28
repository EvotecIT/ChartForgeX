using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    private static void MapSequencePaint(VisualArtifactInterchangeEnvelope envelope, SequenceArtifact sequence) {
        if (sequence.Paint.ParticipantFill.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.participantFill", sequence.Paint.ParticipantFill.Value.ToCss());
        if (sequence.Paint.ParticipantBorder.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.participantBorder", sequence.Paint.ParticipantBorder.Value.ToCss());
        if (sequence.Paint.ParticipantText.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.participantText", sequence.Paint.ParticipantText.Value.ToCss());
        if (sequence.Paint.Lifeline.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.lifeline", sequence.Paint.Lifeline.Value.ToCss());
        if (sequence.Paint.MessageLine.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.messageLine", sequence.Paint.MessageLine.Value.ToCss());
        if (sequence.Paint.MessageText.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.messageText", sequence.Paint.MessageText.Value.ToCss());
        if (sequence.Paint.NumberText.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.numberText", sequence.Paint.NumberText.Value.ToCss());
        if (sequence.Paint.NumberFill.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.numberFill", sequence.Paint.NumberFill.Value.ToCss());
        if (sequence.Paint.NoteFill.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.noteFill", sequence.Paint.NoteFill.Value.ToCss());
        if (sequence.Paint.NoteBorder.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.noteBorder", sequence.Paint.NoteBorder.Value.ToCss());
        if (sequence.Paint.NoteText.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.noteText", sequence.Paint.NoteText.Value.ToCss());
        if (sequence.Paint.ActivationFill.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.activationFill", sequence.Paint.ActivationFill.Value.ToCss());
        if (sequence.Paint.ActivationBorder.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.activationBorder", sequence.Paint.ActivationBorder.Value.ToCss());
        if (sequence.Paint.FragmentFill.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.fragmentFill", sequence.Paint.FragmentFill.Value.ToCss());
        if (sequence.Paint.FragmentBorder.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.fragmentBorder", sequence.Paint.FragmentBorder.Value.ToCss());
        if (sequence.Paint.FragmentText.HasValue) TrySetBoundedExtension(envelope.Extensions, "chartforgex.sequence.paint.fragmentText", sequence.Paint.FragmentText.Value.ToCss());
        if(sequence.Paint.FragmentTitleText.HasValue)TrySetBoundedExtension(envelope.Extensions,"chartforgex.sequence.paint.fragmentTitleText",sequence.Paint.FragmentTitleText.Value.ToCss());
    }

    private static void MapSequenceParticipantGroups(VisualArtifactInterchangeEnvelope envelope, SequenceArtifact sequence, InterchangeIdScope ids) {
        foreach (var source in sequence.ParticipantGroups) {
            var group = new VisualArtifactInterchangeGroup {
                Id = ids.AddGroup(source.Id), Role = VisualArtifactInterchangeGroupRole.SequenceParticipantGroup,
                Kind = "SequenceParticipantGroup", Label = source.Label, Color = source.Fill?.ToCss()
            };
            Copy(source.Metadata, group.Extensions);
            envelope.Groups.Add(group);
            for (var index = source.FirstParticipantIndex; index <= source.LastParticipantIndex; index++) envelope.Nodes[index].GroupId = group.Id;
        }
    }

    /// <summary>Projects authored sequence semantics without invoking the legacy preview layout.</summary>
    internal static VisualArtifactInterchangeEnvelope FromPreparedSequence(SequenceArtifact sequence, VisualRenderContext context) {
        var artifact = VisualArtifact.Create(sequence.Id, VisualArtifactKind.Sequence, sequence);
        var envelope = Common(artifact, out var metadataKeys);
        MapSequence(envelope, sequence, metadataKeys, new VisualArtifactSize(context.Layout.Size.Width, context.Layout.Size.Height));
        envelope.Presentation = new VisualArtifactInterchangePresentation {
            Theme = MapTheme(context.Theme.Resolve(context.ThemeMode), context.Font.Family)
        };
        MapSequenceNativePresentation(envelope,sequence,context);
        envelope.Validate();
        return envelope;
    }

    private static VisualArtifactInterchangeTheme MapTheme(VisualThemeColors colors, string fontFamily) {
        var status = colors.Status;
        return new VisualArtifactInterchangeTheme {
            Background = colors.Background.ToCss(),
            Foreground = colors.Foreground.ToCss(),
            MutedForeground = colors.MutedForeground.ToCss(),
            Card = colors.ElevatedSurface.ToCss(),
            Surface = colors.Surface.ToCss(),
            Border = colors.Border.ToCss(),
            Accent = colors.Accent.ToCss(),
            Healthy = status.Pass.Fill.ToCss(),
            Warning = status.Medium.Fill.ToCss(),
            Critical = status.Critical.Fill.ToCss(),
            Unknown = status.Neutral.Fill.ToCss(),
            Disabled = status.Maintenance.Fill.ToCss(),
            FontFamily = fontFamily
        };
    }
}
