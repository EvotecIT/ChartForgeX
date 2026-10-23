using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Lowers a typed ZenUML occurrence tree directly to the canonical native sequence owner.</summary>
public static partial class MermaidZenUmlRendering {
    /// <summary>Creates a sequence retaining caller identities, occurrence activations, return directions, branches and participant creation.</summary>
    public static SequenceArtifact ToSequenceArtifact(this MermaidZenUmlDocument document, MermaidSequenceRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        MermaidZenUmlModel.Validate(document); options ??= new MermaidSequenceRenderOptions();
        var sequence = SequenceArtifact.Create(options.Id ?? "mermaid-zenuml")
            .WithTitle(MermaidHeading.Title(document, options.Title, document.Title, "ZenUML sequence"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle)).WithSize(options.Width, options.Height);
        sequence.Padding = options.Padding;
        sequence.Metadata["mermaid.kind"] = document.Kind.ToString(); sequence.Metadata["mermaid.header"] = document.Header;
        var participants = new Dictionary<string, SequenceArtifactParticipant>(StringComparer.Ordinal);
        var external = ExternalId(document);
        if (RequiresExternal(document.Statements)) {
            sequence.AddParticipant(external, "Starter");
            var starter = sequence.Participants[0]; starter.IsImplicit = true;
            starter.Metadata["mermaid.zenuml.external"] = "true"; participants.Add(external, starter);
        }
        foreach (var participant in document.Participants) {
            MermaidResourceGuard.Checkpoint(); sequence.AddParticipant(participant.Id, participant.Alias ?? participant.Id, participant.Kind);
            var target = sequence.Participants[sequence.Participants.Count - 1]; target.IsImplicit = participant.IsImplicit;
            Source(target.Metadata, participant.Span); target.Metadata["mermaid.id"] = participant.Id;
            foreach (var attribute in participant.Attributes) target.Metadata["mermaid.zenuml." + attribute.Key] = attribute.Value;
            participants.Add(participant.Id, target);
        }
        for (var index = 0; index < document.ParticipantGroups.Count; index++) {
            var group = document.ParticipantGroups[index];
            var first = Index(sequence, group.ParticipantIds[0]); var last = Index(sequence, group.ParticipantIds[group.ParticipantIds.Count - 1]);
            sequence.AddParticipantGroup("zenuml-group-" + (index + 1).ToString(CultureInfo.InvariantCulture), group.Label, first, last);
            Source(sequence.ParticipantGroups[sequence.ParticipantGroups.Count - 1].Metadata, group.Span);
        }
        var lowering = new Lowering(sequence, participants, external, document); lowering.Statements(document.Statements, 0);
        return MermaidPresentation.Apply(sequence, document, options.Presentation);
    }

    /// <summary>Creates one native sequence-backed SVG/PNG/HTML/JSON artifact without running ZenUML code.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidZenUmlDocument document, MermaidSequenceRenderOptions? options = null) {
        var sequence = document.ToSequenceArtifact(options);
        var artifact = sequence.ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid); artifact.Kind = VisualArtifactKind.Mermaid;
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Json;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString(); artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["render.model"] = nameof(SequenceArtifact);
        return MermaidPresentation.Apply(artifact, document);
    }
    /// <summary>Exports the native sequence as script-free SVG.</summary>
    public static string ToSvg(this MermaidZenUmlDocument document, MermaidSequenceRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();
    /// <summary>Exports that same completed native sequence as PNG.</summary>
    public static byte[] ToPng(this MermaidZenUmlDocument document, MermaidSequenceRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();

    private static string ExternalId(MermaidZenUmlDocument document) {
        var candidate = "zenuml-starter"; var suffix = 0;
        while (document.Participants.Exists(participant => participant.Id == candidate)) candidate = "zenuml-starter-" + (++suffix).ToString(CultureInfo.InvariantCulture);
        return candidate;
    }
    private static bool RequiresExternal(List<MermaidZenUmlStatement> statements) {
        foreach (var statement in statements) {
            MermaidResourceGuard.Checkpoint();
            if (statement.Kind is MermaidZenUmlStatementKind.Call or MermaidZenUmlStatementKind.Async or MermaidZenUmlStatementKind.Return or MermaidZenUmlStatementKind.Create) {
                if (statement.CallerId == null || statement.TargetId == null) return true;
            }
            if (RequiresExternal(statement.Children)) return true;
            foreach (var branch in statement.Branches) if (RequiresExternal(branch.Statements)) return true;
        }
        return false;
    }
    private static int Index(SequenceArtifact sequence, string id) {
        for (var index = 0; index < sequence.Participants.Count; index++) if (sequence.Participants[index].Id == id) return index;
        throw new ArgumentException("ZenUML group participant does not exist: " + id);
    }
    private static void Source(Dictionary<string, string> metadata, MermaidSourceSpan span) {
        metadata["mermaid.source.line"] = span.Line.ToString(CultureInfo.InvariantCulture);
        metadata["mermaid.source.column"] = span.Column.ToString(CultureInfo.InvariantCulture);
    }

    private sealed partial class Lowering {
        private readonly SequenceArtifact _sequence;
        private readonly Dictionary<string, SequenceArtifactParticipant> _participants;
        private readonly string _external;
        private readonly HashSet<string> _notCreated = new(StringComparer.Ordinal);
        private int _fragment;
        internal Lowering(SequenceArtifact sequence, Dictionary<string, SequenceArtifactParticipant> participants, string external, MermaidZenUmlDocument document) {
            _sequence = sequence; _participants = participants; _external = external;
            CollectCreations(document.Statements);
        }
        private void CollectCreations(List<MermaidZenUmlStatement> statements) {
            foreach (var statement in statements) {
                MermaidResourceGuard.Checkpoint();
                if (statement.Kind == MermaidZenUmlStatementKind.Create) _notCreated.Add(statement.TargetId!);
                CollectCreations(statement.Children);
                foreach (var branch in statement.Branches) CollectCreations(branch.Statements);
            }
        }
        internal void Statements(List<MermaidZenUmlStatement> statements, int depth) {
            foreach (var statement in statements) {
                MermaidResourceGuard.Checkpoint(); MermaidResourceGuard.CheckDepth(depth, statement.Span);
                foreach (var comment in statement.Comments) Note(comment.Text,
                    statement.Kind == MermaidZenUmlStatementKind.Create ? statement.CallerId ?? _external : statement.TargetId, comment.Span);
                switch (statement.Kind) {
                    case MermaidZenUmlStatementKind.Call:
                    case MermaidZenUmlStatementKind.Create: Occurrence(statement, depth); break;
                    case MermaidZenUmlStatementKind.Async: Message(statement, SequenceArtifactMessageKind.Async); break;
                    case MermaidZenUmlStatementKind.Return: Message(statement, SequenceArtifactMessageKind.Return); break;
                    case MermaidZenUmlStatementKind.Reference: Note("ref: " + statement.Text, null, statement.Span); break;
                    case MermaidZenUmlStatementKind.Divider: Note(statement.Text, null, statement.Span); break;
                    default: Fragment(statement, depth); break;
                }
            }
        }
        private void Message(MermaidZenUmlStatement statement, SequenceArtifactMessageKind kind) {
            _sequence.AddMessage(statement.CallerId ?? _external, statement.TargetId ?? _external, statement.Text,
                kind == SequenceArtifactMessageKind.Return ? SequenceArtifactMessageLineStyle.Dashed : SequenceArtifactMessageLineStyle.Solid, kind);
            var message = _sequence.Messages[_sequence.Messages.Count - 1]; Source(message.Metadata, statement.Span);
            message.Metadata["mermaid.zenuml.kind"] = statement.Kind.ToString();
            if (statement.Keyword != null) message.Metadata["mermaid.zenuml.keyword"] = statement.Keyword;
        }
        private void Note(string text, string? target, MermaidSourceSpan span) {
            var targets = new List<string>();
            foreach (var id in _participants.Keys) if (!_notCreated.Contains(id) && (target == null || id == target)) targets.Add(id);
            if (targets.Count == 0) return;
            _sequence.AddNote(SequenceArtifactNotePlacement.Over, targets, text);
            var note = _sequence.Notes[_sequence.Notes.Count - 1]; note.StepIndex = _sequence.Messages.Count;
            Source(_sequence.Metadata, "mermaid.zenuml.note." + _sequence.Notes.Count.ToString(CultureInfo.InvariantCulture), span);
        }
    }
    private static void Source(Dictionary<string, string> metadata, string prefix, MermaidSourceSpan span) {
        metadata[prefix + ".source.line"] = span.Line.ToString(CultureInfo.InvariantCulture);
        metadata[prefix + ".source.column"] = span.Column.ToString(CultureInfo.InvariantCulture);
    }
}
