using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

internal static partial class MermaidClassParser {
    private static readonly Regex Relationship = new(@"^(?<source>[^\s<|>*:]+)(?:\s+""(?<sourceMultiplicity>[^""]+)"")?\s*(?<connector>(?:<\||[<*o])?(?:--|\.\.)(?:\|>|[>*o])?)\s*(?:""(?<targetMultiplicity>[^""]+)""\s+)?(?<target>\S+?)(?:\s*:\s*(?<label>.*))?$", RegexOptions.CultureInvariant);
    private static readonly Regex Note = new(@"^note(?:\s+for\s+(?<id>\S+))?\s+""(?<text>.*)""$", RegexOptions.CultureInvariant);

    public static void ParseStatements(MermaidClassDocument document, string[] lines, int startLine, MermaidParseResult<MermaidDocument> result) {
        var classes = new Dictionary<string, MermaidClassNode>(StringComparer.Ordinal);
        MermaidClassNode? activeClass = null;
        MermaidSourceSpan activeClassOpening = document.HeaderSpan;
        var namespaces = new Stack<string>();
        var namespaceOpenings = new Stack<MermaidSourceSpan>();
        for (var line = Math.Max(1, startLine); line <= lines.Length; line++) {
            MermaidResourceGuard.Checkpoint();
            var raw = lines[line - 1];
            var trimmed = MermaidParserUtilities.StripInlineComment(raw.Trim());
            if (MermaidParserUtilities.IsSkippable(trimmed)) continue;
            var span = new MermaidSourceSpan(line, MermaidParserUtilities.LeadingWhitespace(raw) + 1, trimmed.Length);

            if (activeClass != null) {
                if (trimmed == "}") {
                    activeClass = null;
                    continue;
                }

                AddMember(activeClass, trimmed, span);
                continue;
            }

            if (TryNamespace(document, trimmed, span, namespaces, namespaceOpenings, result)) continue;
            if (trimmed == "}") {
                if (namespaces.Count > 0) { namespaces.Pop(); namespaceOpenings.Pop(); }
                else MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Class diagram closing brace has no open class or namespace.");
                continue;
            }

            if (MermaidParserUtilities.TryReadDirection(trimmed, span, result, out var direction)) {
                if (namespaces.Count == 0) document.Direction = direction;
                else MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "namespace direction");
                continue;
            }

            if (MermaidParserUtilities.StartsStatement(trimmed, "note")) {
                var note = Note.Match(trimmed);
                if (note.Success) document.Notes.Add(new MermaidDiagramNote(MermaidNotationText.Note(note.Groups["text"].Value),
                    note.Groups["id"].Success ? CleanEndpoint(note.Groups["id"].Value) : null, false, span));
                else MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "class note syntax");
                continue;
            }

            if (TryLollipop(trimmed, span, out var lollipop)) {
                document.Relationships.Add(lollipop);
                var concrete = EnsureClass(document, classes, lollipop.SourceIsInterface ? lollipop.TargetId : lollipop.SourceId, span);
                if (concrete.Namespace == null && namespaces.Count > 0) concrete.Namespace = namespaces.Peek();
                continue;
            }

            if (TryStyle(document, classes, trimmed, span, result)) continue;
            if (TryLink(document, classes, trimmed, span, result)) continue;

            if (MermaidParserUtilities.StartsStatement(trimmed, "click") ||
                MermaidParserUtilities.StartsStatement(trimmed, "link") || MermaidParserUtilities.StartsStatement(trimmed, "callback")) {
                document.StyleStatements.Add(new MermaidClassStyleStatement(trimmed, span));
                MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "class note or interaction");
                continue;
            }

            if (trimmed.StartsWith("classDef ", StringComparison.Ordinal) || trimmed.StartsWith("class ", StringComparison.Ordinal)) {
                if (trimmed.Substring(6).TrimStart().StartsWith("{", StringComparison.Ordinal)) {
                    MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error, "Class declarations require an identifier.");
                    continue;
                }
                var previousStyleCount = document.StyleStatements.Count;
                var previousRetainedCount = document.RawStatements.Count;
                ParseClassOrStyle(document, classes, trimmed, span, ref activeClass, result);
                if (activeClass != null) activeClassOpening = span;
                if (document.StyleStatements.Count > previousStyleCount && document.RawStatements.Count == previousRetainedCount)
                    MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "class style");
                if (namespaces.Count > 0 && trimmed.StartsWith("class ", StringComparison.Ordinal)) {
                    var ids = trimmed.Substring(6).Split(new[] { ' ', '{', '[' }, StringSplitOptions.RemoveEmptyEntries);
                    if (ids.Length > 0 && classes.TryGetValue(CleanEndpoint(ReadInlineClass(ids[0], out _)), out var declared)) declared.Namespace = namespaces.Peek();
                }
                continue;
            }

            if (trimmed.StartsWith("style ", StringComparison.Ordinal) || trimmed.StartsWith("linkStyle ", StringComparison.Ordinal) || trimmed.StartsWith("cssClass ", StringComparison.Ordinal)) {
                document.StyleStatements.Add(new MermaidClassStyleStatement(trimmed, span));
                MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "class style");
                continue;
            }

            if (trimmed.StartsWith("<<", StringComparison.Ordinal)) {
                ParseAnnotation(document, classes, trimmed, span);
                continue;
            }

            if (TryParseRelationship(trimmed, span, out var relationship)) {
                document.Relationships.Add(relationship);
                var source = EnsureClass(document, classes, relationship.SourceId, span);
                var target = EnsureClass(document, classes, relationship.TargetId, span);
                if (namespaces.Count > 0) {
                    source.Namespace ??= namespaces.Peek();
                    target.Namespace ??= namespaces.Peek();
                }
                continue;
            }

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex > 0) {
                var classId = trimmed.Substring(0, colonIndex).Trim();
                var member = trimmed.Substring(colonIndex + 1).Trim();
                if (classId.Length > 0 && member.Length > 0) {
                    var owner = EnsureClass(document, classes, classId, span);
                    if (owner.Namespace == null && namespaces.Count > 0) owner.Namespace = namespaces.Peek();
                    AddMember(owner, member, span);
                }
                continue;
            }

            MermaidParserUtilities.RetainUnsupported(document, trimmed, span, result, "unrecognized class statement");
            document.StyleStatements.Add(new MermaidClassStyleStatement(trimmed, span));
        }

        if (activeClass != null) Unclosed(activeClassOpening);
        foreach (var opening in namespaceOpenings) Unclosed(opening);
        foreach (var note in document.Notes) if (note.NodeId != null && !classes.ContainsKey(note.NodeId))
            MermaidParserUtilities.Add(result, note.Span, MermaidDiagnosticSeverity.Error, "Class notes must reference a declared class or relationship endpoint.", MermaidDiagnosticCodes.InvalidStatement);
        if (document.Classes.Count == 0 && document.Relationships.Count == 0 && document.Namespaces.Count == 0) MermaidParserUtilities.Add(result, document.HeaderSpan, MermaidDiagnosticSeverity.Error, "Mermaid class diagrams require at least one class, namespace or relationship.");
        ApplyStyles(document);

        void Unclosed(MermaidSourceSpan opening) => MermaidParserUtilities.Add(result, opening, MermaidDiagnosticSeverity.Error,
            "Class definitions and namespaces must close with '}'.");
    }

    private static void ParseClassOrStyle(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string text, MermaidSourceSpan span,
        ref MermaidClassNode? activeClass, MermaidParseResult<MermaidDocument> result) {
        var body = ReadInlineClass(text.Substring(6).Trim(), out var style);
        var declarationText = body.IndexOf('{') >= 0 ? body.Substring(0, body.IndexOf('{')).Trim() : body;
        if (declarationText.IndexOf('~') >= 0 && declarationText.IndexOf(',') >= 0 && declarationText.IndexOf('[') < 0) {
            document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
            MermaidParserUtilities.RetainUnsupported(document, text, span, result, "comma-containing generic type");
            return;
        }
        if (style != null && (style.Length == 0 || style.IndexOf(":::", StringComparison.Ordinal) >= 0)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Class inline styling requires one name after ':::'.", MermaidDiagnosticCodes.InvalidStatement);
            return;
        }
        if (MermaidStyleParser.SplitHead(body, out _, out _) && body.IndexOf("[", StringComparison.Ordinal) < 0 && body.IndexOf("{", StringComparison.Ordinal) < 0) {
            document.StyleStatements.Add(new MermaidClassStyleStatement(text, span));
            return;
        }

        var braceIndex = body.IndexOf('{');
        if (braceIndex >= 0) {
            var declaration = body.Substring(0, braceIndex).Trim();
            activeClass = EnsureClass(document, classes, declaration, span);
            if (style != null) activeClass.Classes.Add(style);
            if (declaration.IndexOf('~') > 0) activeClass.Label = MermaidNotationText.Generics(declaration);
            var remainder = body.Substring(braceIndex + 1).Trim();
            if (remainder.EndsWith("}", StringComparison.Ordinal)) {
                remainder = remainder.Substring(0, remainder.Length - 1).Trim();
                if (remainder.Length > 0) AddMember(activeClass, remainder, span);
                activeClass = null;
            }

            return;
        }

        if (MermaidParserUtilities.TryBracketed(body, out var bracketId, out var label, out _)) {
            var node = EnsureClass(document, classes, bracketId, span);
            node.Label = MermaidParserUtilities.Unquote(label);
            if (style != null) node.Classes.Add(style);
            return;
        }

        if (body.IndexOf(',') >= 0 && !body.StartsWith("`", StringComparison.Ordinal) && !body.StartsWith("\"", StringComparison.Ordinal)) {
            MermaidParserUtilities.Add(result, span, MermaidDiagnosticSeverity.Error,
                "Class declarations require one class identity.", MermaidDiagnosticCodes.InvalidStatement);
            return;
        }
        var plain = EnsureClass(document, classes, body, span);
        if (style != null) plain.Classes.Add(style);
        if (body.IndexOf('~') > 0) plain.Label = MermaidNotationText.Generics(body);
    }

    private static void ParseAnnotation(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string text, MermaidSourceSpan span) {
        var end = text.IndexOf(">>", StringComparison.Ordinal);
        if (end <= 2) return;
        var annotation = text.Substring(2, end - 2).Trim();
        var id = text.Substring(end + 2).Trim();
        if (id.Length == 0) return;
        EnsureClass(document, classes, id, span).Annotations.Add(annotation);
    }

    private static bool TryParseRelationship(string text, MermaidSourceSpan span, out MermaidClassRelationship relationship) {
        relationship = null!;
        var match = Relationship.Match(text);
        if (!match.Success) return false;
        relationship = new MermaidClassRelationship(CleanEndpoint(match.Groups["source"].Value), CleanEndpoint(match.Groups["target"].Value),
            match.Groups["connector"].Value, match.Groups["label"].Success ? match.Groups["label"].Value.Trim() : null, span) {
            SourceMultiplicity = match.Groups["sourceMultiplicity"].Success ? match.Groups["sourceMultiplicity"].Value : null,
            TargetMultiplicity = match.Groups["targetMultiplicity"].Success ? match.Groups["targetMultiplicity"].Value : null
        };
        return true;
    }

    private static string CleanEndpoint(string value) => MermaidNotationText.ClassId(MermaidParserUtilities.Unquote(value));

    private static MermaidClassNode EnsureClass(MermaidClassDocument document, Dictionary<string, MermaidClassNode> classes, string id, MermaidSourceSpan span) {
        id = CleanEndpoint(id);
        if (classes.TryGetValue(id, out var existing)) return existing;
        var node = new MermaidClassNode(id, span);
        classes.Add(id, node);
        document.Classes.Add(node);
        return node;
    }

    private static void AddMember(MermaidClassNode node, string text, MermaidSourceSpan span) {
        if (text.StartsWith("<<", StringComparison.Ordinal) && text.EndsWith(">>", StringComparison.Ordinal)) {
            node.Annotations.Add(text.Substring(2, text.Length - 4).Trim());
            return;
        }

        node.Members.Add(new MermaidClassMember(text, text.IndexOf("(", StringComparison.Ordinal) >= 0 && text.IndexOf(")", StringComparison.Ordinal) > text.IndexOf("(", StringComparison.Ordinal), span));
    }
}
