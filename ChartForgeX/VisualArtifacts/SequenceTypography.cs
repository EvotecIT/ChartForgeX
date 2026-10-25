using ChartForgeX.Typography;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Immutable optional per-role sequence typography captured from the canonical text override owner.</summary>
/// <remarks>Constructor inputs and getter values are detached. Null/empty roles inherit the active native context.
/// Participant stereotypes retain their existing 0.85 size ratio; In Inline presentation, Number stays part of the message caption unless explicitly styled. Badge presentation resolves its automatic numeric role before matching explicit overrides.</remarks>
public sealed class SequenceTypography {
    private readonly TextStyleOverride? _participant;
    private readonly TextStyleOverride? _message;
    private readonly TextStyleOverride? _note;
    private readonly TextStyleOverride? _fragment;
    private readonly TextStyleOverride? _number;
    /// <summary>Captures optional participant, message, note, fragment/group and number styles without retaining mutable inputs.</summary>
    public SequenceTypography(TextStyleOverride? participant = null, TextStyleOverride? message = null, TextStyleOverride? note = null,
        TextStyleOverride? fragment = null, TextStyleOverride? number = null) {
        _participant = participant?.HasOverrides == true ? participant.Clone() : null;
        _message = message?.HasOverrides == true ? message.Clone() : null;
        _note = note?.HasOverrides == true ? note.Clone() : null;
        _fragment = fragment?.HasOverrides == true ? fragment.Clone() : null;
        _number = number?.HasOverrides == true ? number.Clone() : null;
    }
    /// <summary>Gets a detached optional participant role override.</summary>
    public TextStyleOverride? Participant => _participant?.Clone();
    /// <summary>Gets a detached optional message role override.</summary>
    public TextStyleOverride? Message => _message?.Clone();
    /// <summary>Gets a detached optional note role override.</summary>
    public TextStyleOverride? Note => _note?.Clone();
    /// <summary>Gets a detached optional fragment role override.</summary>
    public TextStyleOverride? Fragment => _fragment?.Clone();
    /// <summary>Gets a detached optional number role override.</summary>
    public TextStyleOverride? Number => _number?.Clone();
    /// <summary>Gets whether any role contains authored values.</summary>
    public bool HasOverrides => _participant != null || _message != null || _note != null || _fragment != null || _number != null;
}
