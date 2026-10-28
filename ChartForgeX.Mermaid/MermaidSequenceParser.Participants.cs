using System;
using System.Collections.Generic;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidSequenceParser {
    private static bool TryParseParticipant(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var command = FirstToken(text); var kind = ParseParticipantKind(command);
        if (!kind.HasValue) return false;
        var body = text.Substring(command.Length).Trim();
        var id = ReadIdentifier(body, out var consumed);
        if (id.Length == 0) { Invalid(result, span, "Sequence participant requires an identifier."); return true; }
        var rest = body.Substring(consumed).Trim();
        var participant = GetOrAddParticipant(document, participants, id, span);
        participant.Kind = kind.Value; participant.IsImplicit = false;
        if (rest.StartsWith("@{", StringComparison.Ordinal) || rest.StartsWith("{", StringComparison.Ordinal)) {
            var marker = rest[0] == '@' ? 1 : 0;
            var config = ReadJsonLike(rest.Substring(marker), out var length);
            participant.Configuration = rest.Substring(0, marker + length);
            MermaidSequenceParticipantData.CheckText(participant.Configuration!, span);
            rest = rest.Substring(marker + length).Trim();
            try {
                var parsed = MermaidParser.ReadMetadata(config, span);
                foreach (var pair in parsed.Properties) {
                    if (pair.Key.Equals("alias", StringComparison.OrdinalIgnoreCase)) {
                        if (pair.Value.Kind != MermaidMetadataKind.String || string.IsNullOrWhiteSpace(pair.Value.Text)) throw new ArgumentException("Sequence participant alias must be non-empty text.");
                        participant.Alias = pair.Value.Text;
                    } else if (pair.Key.Equals("type", StringComparison.OrdinalIgnoreCase)) {
                        if (pair.Value.Kind != MermaidMetadataKind.String) throw new ArgumentException("Sequence participant type must be text.");
                        var configured = ParseParticipantKind(pair.Value.Text!);
                        if (configured.HasValue) participant.Kind = configured.Value;
                        else ParticipantWarning(result, span, "Sequence participant type is retained without a native glyph: " + pair.Value.Text);
                    } else ParticipantWarning(result, span, "Sequence participant configuration property is retained without native application: " + pair.Key);
                }
            } catch (ArgumentException exception) { Invalid(result, span, exception.Message); }
        }
        if (rest.Length > 0) {
            if (!rest.StartsWith("as ", StringComparison.OrdinalIgnoreCase) || rest.Substring(3).Trim().Length == 0)
                Invalid(result, span, "Sequence participant declaration has unexpected trailing text.");
            else participant.Alias = Unquote(rest.Substring(3).Trim());
        }
        return true;
    }

    private static bool TryParseLink(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        var advanced = StartsCommand(text, "links");
        if (!advanced && !StartsCommand(text, "link")) return false;
        var body = text.Substring(advanced ? 5 : 4).Trim(); var colon = body.IndexOf(':');
        if (colon <= 0) { Invalid(result, span, "Sequence link requires a participant and ':' payload."); return true; }
        var id = body.Substring(0, colon).Trim(); var value = body.Substring(colon + 1).Trim();
        if (id.Length == 0 || value.Length == 0) { Invalid(result, span, "Sequence link requires a participant and payload."); return true; }
        GetOrAddParticipant(document, participants, id, span);
        var link = new MermaidSequenceLink(id, span);
        if (advanced) link.RawJson = value;
        else {
            var at = value.IndexOf('@');
            if (at <= 0 || at == value.Length - 1) { Invalid(result, span, "Sequence simple link requires 'label @ URL'."); return true; }
            link.Label = value.Substring(0, at).Trim(); link.Url = value.Substring(at + 1).Trim();
        }
        document.Links.Add(link);
        try {
            _ = MermaidSequenceParticipantData.Links(link, message => ParticipantWarning(result, span, message), reserve: true);
        } catch (ArgumentException exception) { Invalid(result, span, exception.Message); }
        return true;
    }

    private static bool TryParseProperties(MermaidSequenceDocument document, Dictionary<string, MermaidSequenceParticipant> participants,
        string text, MermaidSourceSpan span, MermaidParseResult<MermaidDocument> result) {
        if (StartsCommand(text, "details")) {
            ParticipantWarning(result, span, "Sequence details is retained in the source without DOM lookup or external data resolution.");
            return true;
        }
        if (!StartsCommand(text, "properties")) return false;
        var body = text.Substring(10).Trim(); var colon = body.IndexOf(':');
        if (colon <= 0) { Invalid(result, span, "Sequence properties requires a participant and JSON object."); return true; }
        var id = body.Substring(0, colon).Trim(); var value = body.Substring(colon + 1).Trim();
        GetOrAddParticipant(document, participants, id, span);
        document.PropertyStatements.Add(new MermaidSequenceProperties(id, value, span));
        try {
            var properties = MermaidSequenceParticipantData.Object(value, span);
            MermaidResourceGuard.ReserveElements(properties.Count, span);
            foreach (var pair in properties) {
                if (pair.Key == "class" || pair.Key == "icon" || pair.Value.TryAsObject(out _) || pair.Value.TryAsArray(out _))
                    ParticipantWarning(result, span, "Sequence property is retained as inert source data without CSS, artwork or structured presentation: " + pair.Key);
            }
        } catch (ArgumentException exception) { Invalid(result, span, exception.Message); }
        return true;
    }

    private static void ParticipantWarning(MermaidParseResult<MermaidDocument> result, MermaidSourceSpan span, string message) =>
        MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Warning, message, MermaidDiagnosticCodes.UnsupportedStatement);
}
