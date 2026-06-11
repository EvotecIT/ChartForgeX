using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Defines participant kinds for reusable sequence artifacts.
/// </summary>
public enum SequenceArtifactParticipantKind {
    /// <summary>A standard participant.</summary>
    Participant,
    /// <summary>An actor participant.</summary>
    Actor,
    /// <summary>A boundary participant.</summary>
    Boundary,
    /// <summary>A control participant.</summary>
    Control,
    /// <summary>An entity participant.</summary>
    Entity,
    /// <summary>A database participant.</summary>
    Database,
    /// <summary>A collections participant.</summary>
    Collections,
    /// <summary>A queue participant.</summary>
    Queue
}

/// <summary>
/// Defines sequence message line styles.
/// </summary>
public enum SequenceArtifactMessageLineStyle {
    /// <summary>A solid line.</summary>
    Solid,
    /// <summary>A dashed line.</summary>
    Dashed
}

/// <summary>
/// Defines note placement for sequence artifacts.
/// </summary>
public enum SequenceArtifactNotePlacement {
    /// <summary>Note appears to the right of a participant.</summary>
    RightOf,
    /// <summary>Note appears to the left of a participant.</summary>
    LeftOf,
    /// <summary>Note appears over one or more participants.</summary>
    Over
}

/// <summary>
/// Defines sequence block kinds.
/// </summary>
public enum SequenceArtifactBlockKind {
    /// <summary>A loop block.</summary>
    Loop,
    /// <summary>An alternative block.</summary>
    Alt,
    /// <summary>An optional block.</summary>
    Opt,
    /// <summary>A parallel block.</summary>
    Par,
    /// <summary>A critical block.</summary>
    Critical,
    /// <summary>A background highlight block.</summary>
    Rect,
    /// <summary>A break or exception-flow block.</summary>
    Break
}

/// <summary>
/// Describes a reusable product-neutral sequence or interaction diagram artifact.
/// </summary>
public sealed class SequenceArtifact {
    private readonly List<SequenceArtifactParticipant> _participants = new();
    private readonly List<SequenceArtifactMessage> _messages = new();
    private readonly List<SequenceArtifactNote> _notes = new();
    private readonly List<SequenceArtifactBlock> _blocks = new();
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private double _width = 960;
    private double _height = 560;
    private double _padding = 32;
    private VisualArtifactExportFormat _exportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png;

    /// <summary>Gets or sets a stable sequence artifact id.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the sequence title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the sequence subtitle.</summary>
    public string Subtitle { get => _subtitle; set => _subtitle = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the natural preview width.</summary>
    public double Width { get => _width; set { VisualArtifactGuards.PositiveFinite(value, nameof(value)); _width = value; } }

    /// <summary>Gets or sets the natural preview height.</summary>
    public double Height { get => _height; set { VisualArtifactGuards.PositiveFinite(value, nameof(value)); _height = value; } }

    /// <summary>Gets or sets the preview padding.</summary>
    public double Padding { get => _padding; set { VisualArtifactGuards.NonNegativeFinite(value, nameof(value)); _padding = value; } }

    /// <summary>Gets or sets supported static export formats.</summary>
    public VisualArtifactExportFormat ExportFormats {
        get => _exportFormats;
        set {
            VisualArtifactGuards.ExportFormatsDefined(value, nameof(value));
            _exportFormats = value;
        }
    }

    /// <summary>Gets participants in display order.</summary>
    public IReadOnlyList<SequenceArtifactParticipant> Participants => _participants;

    /// <summary>Gets messages in chronological order.</summary>
    public IReadOnlyList<SequenceArtifactMessage> Messages => _messages;

    /// <summary>Gets notes in chronological order.</summary>
    public IReadOnlyList<SequenceArtifactNote> Notes => _notes;

    /// <summary>Gets block spans.</summary>
    public IReadOnlyList<SequenceArtifactBlock> Blocks => _blocks;

    /// <summary>Gets artifact metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);

    /// <summary>Creates a sequence artifact.</summary>
    public static SequenceArtifact Create(string id) => new() { Id = id ?? throw new ArgumentNullException(nameof(id)) };

    /// <summary>Sets the title.</summary>
    public SequenceArtifact WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>Sets the subtitle.</summary>
    public SequenceArtifact WithSubtitle(string subtitle) { Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle)); return this; }

    /// <summary>Sets the natural preview size.</summary>
    public SequenceArtifact WithSize(double width, double height) { Width = width; Height = height; return this; }

    /// <summary>Adds or updates a participant.</summary>
    public SequenceArtifact AddParticipant(string id, string? label = null, SequenceArtifactParticipantKind kind = SequenceArtifactParticipantKind.Participant) {
        var existing = FindParticipant(id);
        if (existing != null) {
            if (label != null) existing.Label = label;
            existing.Kind = kind;
            return this;
        }

        _participants.Add(new SequenceArtifactParticipant(id, label ?? id, kind));
        return this;
    }

    /// <summary>Adds a message.</summary>
    public SequenceArtifact AddMessage(string sourceId, string targetId, string? text = null, SequenceArtifactMessageLineStyle lineStyle = SequenceArtifactMessageLineStyle.Solid) {
        EnsureParticipant(sourceId);
        EnsureParticipant(targetId);
        _messages.Add(new SequenceArtifactMessage(sourceId, targetId, text ?? string.Empty, lineStyle));
        return this;
    }

    /// <summary>Adds a note.</summary>
    public SequenceArtifact AddNote(SequenceArtifactNotePlacement placement, IEnumerable<string> participantIds, string text) {
        if (participantIds == null) throw new ArgumentNullException(nameof(participantIds));
        var note = new SequenceArtifactNote(placement, text ?? throw new ArgumentNullException(nameof(text))) { StepIndex = _messages.Count + _notes.Count };
        foreach (var participantId in participantIds) {
            EnsureParticipant(participantId);
            note.ParticipantIds.Add(participantId);
        }

        _notes.Add(note);
        return this;
    }

    /// <summary>Adds a block span.</summary>
    public SequenceArtifact AddBlock(SequenceArtifactBlockKind kind, string text, int startStepIndex, int endStepIndex) {
        _blocks.Add(new SequenceArtifactBlock(kind, text ?? string.Empty, startStepIndex, endStepIndex));
        return this;
    }

    private SequenceArtifactParticipant? FindParticipant(string id) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Participant id is required.", nameof(id));
        for (var index = 0; index < _participants.Count; index++) {
            if (string.Equals(_participants[index].Id, id, StringComparison.Ordinal)) return _participants[index];
        }

        return null;
    }

    private void EnsureParticipant(string id) {
        if (FindParticipant(id) == null) _participants.Add(new SequenceArtifactParticipant(id, id, SequenceArtifactParticipantKind.Participant) { IsImplicit = true });
    }
}

/// <summary>
/// Describes a sequence participant.
/// </summary>
public sealed class SequenceArtifactParticipant {
    private string _id;
    private string _label;

    /// <summary>Initializes a sequence participant.</summary>
    public SequenceArtifactParticipant(string id, string label, SequenceArtifactParticipantKind kind = SequenceArtifactParticipantKind.Participant) {
        _id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Participant id is required.", nameof(id)) : id;
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Kind = kind;
    }

    /// <summary>Gets or sets the participant id.</summary>
    public string Id { get => _id; set => _id = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Participant id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the participant label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the participant kind.</summary>
    public SequenceArtifactParticipantKind Kind { get; set; }

    /// <summary>Gets or sets whether the participant was inferred from sequence content.</summary>
    public bool IsImplicit { get; set; }

    /// <summary>Gets participant metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Describes a sequence message.
/// </summary>
public sealed class SequenceArtifactMessage {
    private string _sourceId;
    private string _targetId;
    private string _text;

    /// <summary>Initializes a sequence message.</summary>
    public SequenceArtifactMessage(string sourceId, string targetId, string text, SequenceArtifactMessageLineStyle lineStyle = SequenceArtifactMessageLineStyle.Solid) {
        _sourceId = string.IsNullOrWhiteSpace(sourceId) ? throw new ArgumentException("Source participant id is required.", nameof(sourceId)) : sourceId;
        _targetId = string.IsNullOrWhiteSpace(targetId) ? throw new ArgumentException("Target participant id is required.", nameof(targetId)) : targetId;
        _text = text ?? throw new ArgumentNullException(nameof(text));
        LineStyle = lineStyle;
    }

    /// <summary>Gets or sets the source participant id.</summary>
    public string SourceId { get => _sourceId; set => _sourceId = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Source participant id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the target participant id.</summary>
    public string TargetId { get => _targetId; set => _targetId = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Target participant id is required.", nameof(value)) : value; }

    /// <summary>Gets or sets the message text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the message line style.</summary>
    public SequenceArtifactMessageLineStyle LineStyle { get; set; }

    /// <summary>Gets or sets whether the message activates the target.</summary>
    public bool ActivatesTarget { get; set; }

    /// <summary>Gets or sets whether the message deactivates a participant.</summary>
    public bool Deactivates { get; set; }

    /// <summary>Gets message metadata for host adapters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Describes a sequence note.
/// </summary>
public sealed class SequenceArtifactNote {
    private string _text;

    /// <summary>Initializes a sequence note.</summary>
    public SequenceArtifactNote(SequenceArtifactNotePlacement placement, string text) {
        Placement = placement;
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>Gets or sets the note placement.</summary>
    public SequenceArtifactNotePlacement Placement { get; set; }

    /// <summary>Gets referenced participant ids.</summary>
    public List<string> ParticipantIds { get; } = new();

    /// <summary>Gets or sets the note text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the sequence step index for preview placement.</summary>
    public int StepIndex { get; set; }
}

/// <summary>
/// Describes a sequence block span.
/// </summary>
public sealed class SequenceArtifactBlock {
    private string _text;

    /// <summary>Initializes a sequence block span.</summary>
    public SequenceArtifactBlock(SequenceArtifactBlockKind kind, string text, int startStepIndex, int endStepIndex) {
        Kind = kind;
        _text = text ?? throw new ArgumentNullException(nameof(text));
        StartStepIndex = startStepIndex;
        EndStepIndex = endStepIndex;
    }

    /// <summary>Gets or sets the block kind.</summary>
    public SequenceArtifactBlockKind Kind { get; set; }

    /// <summary>Gets or sets the block text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the first covered step index.</summary>
    public int StartStepIndex { get; set; }

    /// <summary>Gets or sets the last covered step index.</summary>
    public int EndStepIndex { get; set; }
}
