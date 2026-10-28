using System;
using ChartForgeX.Core;
using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Immutable optional native sequence allocations. Null fields keep the incumbent layout.</summary>
/// <remarks>Participant spacing separates measured headers. Note spacing is the margin around note text and its surrounding clearance.
/// CanvasPadding affects convenience/natural exports; an explicit render context owns its viewport and outer insets.
/// Explicit allocations that cannot fit reject before paint rather than shrinking text or dropping facts.</remarks>
public sealed class SequenceLayoutPolicy {
    /// <summary>Creates a captured logical-unit policy; an empty instance is equivalent to no policy.</summary>
    public SequenceLayoutPolicy(ChartPadding? canvasPadding = null,
        double? participantMinimumWidth = null,
        double? participantMinimumHeight = null,
        double? participantSpacing = null,
        double? messageSpacing = null,
        double? noteSpacing = null,
        double? fragmentMargin = null,
        double? fragmentLabelPadding = null,
        double? activationWidth = null,
        double? wrapPadding = null, bool? wrapLabels = null,
        bool? mirrorParticipants = null, bool? hideUnusedParticipants = null,
        TextAlignment? messageAlignment = null, TextAlignment? noteAlignment = null, SequenceNumberPresentation? numberPresentation = null, SequenceSelfMessageRouting? selfMessageRouting = null, double? footerBottomMargin = null, double? fragmentTabMinimumWidth = null, double? fragmentTabMinimumHeight = null, SequenceParticipantBands? participantBands = null) {
        CanvasPadding = canvasPadding;ParticipantBands=participantBands;
        ParticipantMinimumWidth = Validate(participantMinimumWidth, nameof(participantMinimumWidth), true);
        ParticipantMinimumHeight = Validate(participantMinimumHeight, nameof(participantMinimumHeight), true);
        ParticipantSpacing = Validate(participantSpacing, nameof(participantSpacing), false);
        MessageSpacing = Validate(messageSpacing, nameof(messageSpacing), false);
        NoteSpacing = Validate(noteSpacing, nameof(noteSpacing), false);
        FragmentMargin = Validate(fragmentMargin, nameof(fragmentMargin), false);
        FragmentLabelPadding = Validate(fragmentLabelPadding, nameof(fragmentLabelPadding), false);
        ActivationWidth = Validate(activationWidth, nameof(activationWidth), true);
        WrapPadding = Validate(wrapPadding, nameof(wrapPadding), false);
        WrapLabels = wrapLabels;
        MirrorParticipants = mirrorParticipants; HideUnusedParticipants = hideUnusedParticipants;
        MessageAlignment = ValidateEnum(messageAlignment, nameof(messageAlignment)); NoteAlignment = ValidateEnum(noteAlignment, nameof(noteAlignment));
        NumberPresentation = ValidateEnum(numberPresentation, nameof(numberPresentation));
        SelfMessageRouting = ValidateEnum(selfMessageRouting, nameof(selfMessageRouting));
        FooterBottomMargin=Validate(footerBottomMargin,nameof(footerBottomMargin),false);
        FragmentTabMinimumWidth=Validate(fragmentTabMinimumWidth,nameof(fragmentTabMinimumWidth),true);
        FragmentTabMinimumHeight=Validate(fragmentTabMinimumHeight,nameof(fragmentTabMinimumHeight),true);
    }
    /// <summary>Gets optional independent outer insets for natural/convenience preparation.</summary>
    public ChartPadding? CanvasPadding { get; }
    /// <summary>Gets the optional positive ParticipantMinimumWidth in logical units.</summary>
    public double? ParticipantMinimumWidth { get; }
    /// <summary>Gets the optional positive ParticipantMinimumHeight in logical units.</summary>
    public double? ParticipantMinimumHeight { get; }
    /// <summary>Gets the optional nonnegative ParticipantSpacing in logical units.</summary>
    public double? ParticipantSpacing { get; }
    /// <summary>Gets the optional nonnegative MessageSpacing in logical units.</summary>
    public double? MessageSpacing { get; }
    /// <summary>Gets the optional nonnegative NoteSpacing in logical units.</summary>
    public double? NoteSpacing { get; }
    /// <summary>Gets the optional nonnegative FragmentMargin in logical units.</summary>
    public double? FragmentMargin { get; }
    /// <summary>Gets the optional nonnegative FragmentLabelPadding in logical units.</summary>
    public double? FragmentLabelPadding { get; }
    /// <summary>Gets the optional positive ActivationWidth in logical units.</summary>
    public double? ActivationWidth { get; }
    /// <summary>Gets the optional nonnegative WrapPadding in logical units.</summary>
    public double? WrapPadding { get; }
    /// <summary>Gets explicit wrapping; false preserves complete authored lines and rejects insufficient fixed allocations.</summary>
    public bool? WrapLabels { get; }
    /// <summary>Gets whether each visible participant is repeated at its lifetime end using the measured native participant painter.</summary>
    /// <remarks>A destroyed participant is repeated at its destroying message; other footers share the bottom row. Null/false keep ordinary paint.</remarks>
    public bool? MirrorParticipants { get; }
    /// <summary>Gets whether participant columns without a message, note, activation or reference target are omitted from this view.</summary>
    /// <remarks>All source records, IDs, order and group memberships remain in the model and portable facts. Null/false keep every column.</remarks>
    public bool? HideUnusedParticipants { get; }
    /// <summary>Gets optional alignment of message lines inside the measured native message allocation.</summary>
    public TextAlignment? MessageAlignment { get; }
    /// <summary>Gets optional alignment of note lines inside the existing measured note interior.</summary>
    public TextAlignment? NoteAlignment { get; }
    /// <summary>Gets optional native number presentation; null/Inline retain the ordinary prefix/text-run law.</summary>
    public SequenceNumberPresentation? NumberPresentation { get; }
    /// <summary>Gets optional self-route presentation. Null retains legacy 56/24; explicit Orthogonal uses 25-unit and Curved 20-unit return.</summary>
    public SequenceSelfMessageRouting? SelfMessageRouting { get; }
    /// <summary>Gets optional trailing space after the last mirrored footer; null retains the ordinary fragment margin.</summary>
    public double? FooterBottomMargin {get;}
    /// <summary>Gets optional measured fragment keyword-tab minimum width; null with no tab height keeps the ordinary combined header.</summary>
    public double? FragmentTabMinimumWidth {get;}
    /// <summary>Gets optional measured fragment keyword-tab minimum height; null with no tab width keeps the ordinary combined header.</summary>
    public double? FragmentTabMinimumHeight {get;}
    /// <summary>Gets optional measured glyph/label/lifeline bands; null retains ordinary participant anatomy.</summary>
    public SequenceParticipantBands? ParticipantBands {get;}
    /// <summary>Gets whether this immutable policy authors any effective allocation or view change.</summary>
    public bool HasOverrides => CanvasPadding.HasValue || ParticipantMinimumWidth.HasValue || ParticipantMinimumHeight.HasValue || ParticipantSpacing.HasValue || MessageSpacing.HasValue || NoteSpacing.HasValue || FragmentMargin.HasValue || FragmentLabelPadding.HasValue || ActivationWidth.HasValue || WrapPadding.HasValue || WrapLabels.HasValue || MirrorParticipants == true || HideUnusedParticipants == true || MessageAlignment.HasValue || NoteAlignment.HasValue || NumberPresentation == SequenceNumberPresentation.Badge || SelfMessageRouting.HasValue || FooterBottomMargin.HasValue || FragmentTabMinimumWidth.HasValue || FragmentTabMinimumHeight.HasValue || ParticipantBands!=null;
    private static double? Validate(double? value, string name, bool positive) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || (positive ? value.Value <= 0 : value.Value < 0)))
            throw new ArgumentOutOfRangeException(name, "Sequence allocations must be finite and positive, or nonnegative for spacing.");
        return value;
    }
    private static T? ValidateEnum<T>(T? value, string name) where T : struct {
        if (value.HasValue && !Enum.IsDefined(typeof(T), value.Value)) throw new ArgumentOutOfRangeException(name);
        return value;
    }
    internal SequenceLayoutPolicy WithNumberPresentation(SequenceNumberPresentation value) => new(CanvasPadding,
        ParticipantMinimumWidth, ParticipantMinimumHeight, ParticipantSpacing, MessageSpacing, NoteSpacing, FragmentMargin, FragmentLabelPadding,
        ActivationWidth, WrapPadding, WrapLabels, MirrorParticipants, HideUnusedParticipants, MessageAlignment, NoteAlignment, value, SelfMessageRouting, FooterBottomMargin, FragmentTabMinimumWidth, FragmentTabMinimumHeight, ParticipantBands);
    internal SequenceLayoutPolicy WithCanvasPadding(ChartPadding padding) => new(padding,
        ParticipantMinimumWidth, ParticipantMinimumHeight, ParticipantSpacing, MessageSpacing, NoteSpacing, FragmentMargin, FragmentLabelPadding, ActivationWidth, WrapPadding, WrapLabels, MirrorParticipants, HideUnusedParticipants, MessageAlignment, NoteAlignment, NumberPresentation, SelfMessageRouting, FooterBottomMargin, FragmentTabMinimumWidth, FragmentTabMinimumHeight, ParticipantBands);
}
